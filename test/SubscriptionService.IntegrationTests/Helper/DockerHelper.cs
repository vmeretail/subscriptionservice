namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Common;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore.ClientAPI.Common.Log;
    using EventStore.ClientAPI.Projections;
    using EventStore.ClientAPI.SystemData;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Shouldly;

    /// <summary>
    /// 
    /// </summary>
    public class DockerHelper
    {
        #region Fields

        /// <summary>
        /// The dummy rest container
        /// </summary>
        private IContainerService DummyRESTContainer;

        /// <summary>
        /// The event store container
        /// </summary>
        private IContainerService EventStoreContainer;

        /// <summary>
        /// The manual reset event
        /// </summary>
        private ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// The test identifier
        /// </summary>
        private Guid TestId;

        /// <summary>
        /// The test network
        /// </summary>
        private INetworkService TestNetwork;

        /// <summary>
        /// The tests fixture
        /// </summary>
        private readonly TestsFixture TestsFixture;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerHelper" /> class.
        /// </summary>
        /// <param name="testsFixture">The tests fixture.</param>
        public DockerHelper(TestsFixture testsFixture)
        {
            this.TestsFixture = testsFixture;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the dummy rest HTTP port.
        /// </summary>
        /// <value>
        /// The dummy rest HTTP port.
        /// </value>
        public Int32 DummyRESTHttpPort { get; private set; }

        /// <summary>
        /// Gets the event store HTTP port.
        /// </summary>
        /// <value>
        /// The event store HTTP port.
        /// </value>
        public Int32 EventStoreHttpPort { get; private set; }

        /// <summary>
        /// Gets the event store TCP port.
        /// </summary>
        /// <value>
        /// The event store TCP port.
        /// </value>
        public Int32 EventStoreTcpPort { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the HTTP client.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static HttpClient CreateHttpClient(String uri)
        {
            HttpClientHandler handler = new HttpClientHandler();

            if (new Uri(uri).Scheme == "https")
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12;

                handler.ServerCertificateCustomValidationCallback += (sender,
                                                                      certificate,
                                                                      chain,
                                                                      sslPolicyErrors) =>
                                                                     {
                                                                         return (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) !=
                                                                                SslPolicyErrors.RemoteCertificateNotAvailable;
                                                                     };
            }

            HttpClient httpClient = new HttpClient(handler);

            return httpClient;
        }

        /// <summary>
        /// Gets the event store docker configuration.
        /// </summary>
        /// <returns></returns>
        public static EventStoreDockerConfiguration GetEventStoreDockerConfiguration()
        {
            // Determine the ES version from the Environment variable
            String eventstoreVersion = Environment.GetEnvironmentVariable("ESVersion");

            // Support local testing where the environment variable has not been set
            if (String.IsNullOrEmpty(eventstoreVersion))
            {
                eventstoreVersion = "default";
            }

            // Now do the version lookup
            // Create an object to read the configuration
            IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

            // Bind the config
            Dictionary<String, EventStoreDockerConfiguration> configurationList = new Dictionary<String, EventStoreDockerConfiguration>();
            config.GetSection("EventStoreDocker").Bind(configurationList);

            // Find the relevant configuration
            EventStoreDockerConfiguration esConfig = null;
            if (configurationList.ContainsKey(eventstoreVersion) == false)
            {
                throw new Exception($"No eventstore configuration fount for version tag [{eventstoreVersion}]");
            }

            esConfig = configurationList[eventstoreVersion];
            

            return esConfig;
        }

        /// <summary>
        /// Starts the containers for scenario run.
        /// </summary>
        /// <param name="testname">The testname.</param>
        public void StartContainersForScenarioRun(String testname)
        {
            this.TestId = Guid.NewGuid();

            this.TestNetwork = new Builder().UseNetwork($"test-network-{Guid.NewGuid():N}").Build();
            String mountDir = String.Empty; //Don't use mounted directories on CI

            String? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Boolean isDevelopment = true;

            if (environment != null)
            {
                isDevelopment = environment == EnvironmentName.Development;
            }

            if (isDevelopment)
            {
                mountDir = FdOs.IsWindows()
                    ? $"C:\\home\\forge\\subscriptionservice\\trace\\{DateTime.Now:yyyyMMdd}\\{testname}"
                    : $"//home//forge//subscriptionservice//trace//{DateTime.Now:yyyyMMdd}//{testname}//";

                //Create the destination directory rather than relying on Docker library.
                Directory.CreateDirectory(mountDir);
            }

            EventStoreDockerConfiguration eventStoreDockerConfiguration = DockerHelper.GetEventStoreDockerConfiguration();
            this.TestsFixture.EventStoreDockerConfiguration = eventStoreDockerConfiguration;

            this.EventStoreContainer = DockerHelper.CreateEventStoreContainer($"eventstore{this.TestId.ToString("N")}", this.TestNetwork, mountDir, this.TestsFixture);
            this.DummyRESTContainer = DockerHelper.CreateDummyRESTContainer($"vmedummyjson{this.TestId.ToString("N")}", this.TestNetwork, ""); //No trace written

            this.EventStoreContainer.Start();
            this.DummyRESTContainer.Start();

            this.EventStoreTcpPort = this.EventStoreContainer.ToHostExposedEndpoint("1113/tcp").Port;
            this.EventStoreHttpPort = this.EventStoreContainer.ToHostExposedEndpoint("2113/tcp").Port;
            this.DummyRESTHttpPort = this.DummyRESTContainer.ToHostExposedEndpoint("80/tcp").Port;

            String scheme = this.TestsFixture.EventStoreDockerConfiguration.IsLegacyVersion ? "http" : "https";

            // Verify the Event Store is running
            Retry.For(async () =>
                      {
                          String url = $"{scheme}://127.0.0.1:{this.EventStoreHttpPort}/ping";

                          HttpClient client = DockerHelper.CreateHttpClient(url);

                          HttpResponseMessage pingResponse = await client.GetAsync(url).ConfigureAwait(false);
                          pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                      }).Wait();

            Retry.For(async () =>
                      {
                          String url = $"{scheme}://127.0.0.1:{this.EventStoreHttpPort}/info";
                          HttpClient client = DockerHelper.CreateHttpClient(url);

                          HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                          requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Authorization", "Basic YWRtaW46Y2hhbmdlaXQ=");

                          HttpResponseMessage infoResponse = await client.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

                          infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                      }).Wait();

            // Load the event debug projection to the Event Store
            try
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), this.EventStoreHttpPort);
                String projectionString = "fromAll()\r\n.when({\r\n$any: function(s, e){\r\nlog(JSON.stringify(e));\r\n}\r\n})";
                ProjectionsManager debugProjectionManager = new ProjectionsManager(new ConsoleLogger(), endPoint, TimeSpan.FromSeconds(30));
                this.TestsFixture.LogMessageToTrace($"Creating projection [{"EventDebugging"}]");
                debugProjectionManager.CreateContinuousAsync("Debugging", projectionString, new UserCredentials("admin", "changeit")).Wait();
            }
            catch (Exception e)
            {
                this.TestsFixture.LogMessageToTrace($"Projection [{"EventDebugging"}] error [{e}]");
            }

            this.TestsFixture.LogMessageToTrace($"Projection [{"EventDebugging"}] Loaded Successfully");
        }

        /// <summary>
        /// Stops the containers for scenario run.
        /// </summary>
        public void StopContainersForScenarioRun()
        {
            if (this.EventStoreContainer != null)
            {
                this.EventStoreContainer.ClearUpContainer();
            }

            if (this.DummyRESTContainer != null)
            {
                this.DummyRESTContainer.ClearUpContainer();
            }

            if (this.TestNetwork != null)
            {
                this.TestNetwork.Stop();
                this.TestNetwork.Dispose();
            }
        }

        /// <summary>
        /// Creates the dummy rest container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="mountDirectory">The mount directory.</param>
        /// <returns></returns>
        private static IContainerService CreateDummyRESTContainer(String containerName,
                                                                  INetworkService networkService,
                                                                  String mountDirectory)
        {
            IContainerService container = new Builder().UseContainer().UseImage(@"vmeretailsystems/vmedummyjson:latest", true).ExposePort(80).WithName(containerName)
                                                       .UseNetwork(networkService).WaitForPort("80/tcp", 30000 /*30s*/).Build();

            return container;
        }

        /// <summary>
        /// Creates the event store container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="mountDirectory">The mount directory.</param>
        /// <param name="testsFixture">The tests fixture.</param>
        /// <returns></returns>
        private static IContainerService CreateEventStoreContainer(String containerName,
                                                                   INetworkService networkService,
                                                                   String mountDirectory,
                                                                   TestsFixture testsFixture)
        {
            EventStoreDockerConfiguration eventStoreDockerConfiguration = DockerHelper.GetEventStoreDockerConfiguration();

            String dockerImage = $"{eventStoreDockerConfiguration.RegistryUrl}{eventStoreDockerConfiguration.Registry}:{eventStoreDockerConfiguration.Tag}";

            testsFixture.LogMessageToTrace($"About to start event store using image {dockerImage}");
            // Create the container

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add("EVENTSTORE_RUN_PROJECTIONS=all");
            environmentVariables.Add("EVENTSTORE_START_STANDARD_PROJECTIONS=true");

            if (eventStoreDockerConfiguration.IsLegacyVersion == false)
            {
                // Add the development mode switch on ES versions > 6 otherwise 
                // SSL cerificate needed to run
                environmentVariables.Add("EVENTSTORE_DEV=true");
                environmentVariables.Add("EVENTSTORE_ENABLE_EXTERNAL_TCP=true");
            }

            ContainerBuilder containerBuilder = new Builder().UseContainer().UseImage(dockerImage, true).ExposePort(2113).ExposePort(1113).WithName(containerName)
                                                             .WithEnvironment(environmentVariables.ToArray())
                                                             .Mount(mountDirectory, $"/var/log/eventstore/{DateTime.Now.ToString("yyyy-MM-dd")}/", MountType.ReadWrite)
                                                             .UseNetwork(networkService).WaitForPort("2113/tcp", 30000 /*30s*/);

            if (String.IsNullOrEmpty(eventStoreDockerConfiguration.UserName) == false && String.IsNullOrEmpty(eventStoreDockerConfiguration.Password) == false)
            {
                Byte[] passwordBytes = Convert.FromBase64String(eventStoreDockerConfiguration.Password);
                String password = Encoding.UTF8.GetString(passwordBytes);
                // We need to build Docker Credentials
                containerBuilder.WithCredential(eventStoreDockerConfiguration.RegistryUrl, eventStoreDockerConfiguration.UserName, password);
            }

            IContainerService container = containerBuilder.Build();

            return container;
        }

        #endregion
    }
}