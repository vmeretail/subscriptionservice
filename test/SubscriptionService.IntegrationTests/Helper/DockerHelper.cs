namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Common;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore.ClientAPI;
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
        /// Initializes a new instance of the <see cref="DockerHelper"/> class.
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

        private ManualResetEvent ManualResetEvent = new ManualResetEvent(false);

        /// <summary>
        /// Starts the containers for scenario run.
        /// </summary>
        /// <param name="testname">The testname.</param>
        public void StartContainersForScenarioRun(String testname)
        {
            this.TestId = Guid.NewGuid();

            this.TestNetwork = new Builder().UseNetwork($"test-network-{Guid.NewGuid():N}").Build();
            String mountDir = string.Empty; //Don't use mounted directories on CI

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

            this.EventStoreContainer = DockerHelper.CreateEventStoreContainer($"eventstore{this.TestId.ToString("N")}", this.TestNetwork, mountDir, this.TestsFixture);
            this.DummyRESTContainer = DockerHelper.CreateDummyRESTContainer($"vmedummyjson{this.TestId.ToString("N")}", this.TestNetwork, ""); //No trace written

            this.EventStoreContainer.Start();
            this.DummyRESTContainer.Start();

            this.EventStoreTcpPort = this.EventStoreContainer.ToHostExposedEndpoint("1113/tcp").Port;
            this.EventStoreHttpPort = this.EventStoreContainer.ToHostExposedEndpoint("2113/tcp").Port;
            this.DummyRESTHttpPort = this.DummyRESTContainer.ToHostExposedEndpoint("80/tcp").Port;

            //// Verify the Event Store is running
            //Retry.For(async () =>
            //          {
            //              String url = $"http://127.0.0.1:{this.EventStoreHttpPort}/ping";

            //              HttpClient client = new HttpClient();

            //              HttpResponseMessage pingResponse = await client.GetAsync(url).ConfigureAwait(false);
            //              pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            //          }).Wait();

            //Retry.For(async () =>
            //          {
            //              String url = $"http://127.0.0.1:{this.EventStoreHttpPort}/info";
            //              HttpClient client = new HttpClient();

            //              HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            //              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Authorization", "Basic YWRtaW46Y2hhbmdlaXQ=");

            //              HttpResponseMessage infoResponse = await client.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

            //              infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            //          }).Wait();

            // Setup the Event Store Connection
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.EventStoreTcpPort};VerboseLogging=true;";
            IEventStoreConnection eventStoreConnection = EventStore.ClientAPI.EventStoreConnection.Create(connectionString);

            eventStoreConnection.Connected += (sender,
                                               args) =>
                                              {
                                                  this.ManualResetEvent.Set();
                                                  this.TestsFixture.LogMessageToTrace($"Ctor - Connected");
                                              };

            eventStoreConnection.Closed += (sender,
                                            args) =>
                                           {
                                               this.TestsFixture.LogMessageToTrace($"Ctor - Closed");
                                           };

            eventStoreConnection.ErrorOccurred += (sender,
                                                   args) =>
                                                  {
                                                      this.TestsFixture.LogMessageToTrace($"Ctor - ErrorOccurred {args.Exception.ToString()}");
                                                  };

            eventStoreConnection.Reconnecting += (sender,
                                                  args) =>
                                                 {
                                                     this.TestsFixture.LogMessageToTrace($"Ctor - Reconnecting");
                                                 };

            eventStoreConnection.ConnectAsync().Wait();

            this.ManualResetEvent.WaitOne(30000);

            this.TestsFixture.LogMessageToTrace($"Ready to Go...");
            eventStoreConnection.Close();
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
            // Determine the ES version from the Environment variable
            String eventstoreVersion = Environment.GetEnvironmentVariable("ESVersion");

            // Support local testing where the environment variable has not been set
            if (string.IsNullOrEmpty(eventstoreVersion))
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
            if (configurationList.ContainsKey(eventstoreVersion))
            {
                esConfig = configurationList[eventstoreVersion];
            }

            String dockerImage = $"{esConfig.Registry}:{esConfig.Tag}";

            testsFixture.LogMessageToTrace($"About to start event store using image {dockerImage}");
            // Create the container
            IContainerService container = new Builder().UseContainer().UseImage(dockerImage, true).ExposePort(2113).ExposePort(1113).WithName(containerName)
                                                       .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS=all", "EVENTSTORE_START_STANDARD_PROJECTIONS=true")
                                                       .Mount(mountDirectory, $"/var/log/eventstore/{DateTime.Now.ToString("yyyy-MM-dd")}/", MountType.ReadWrite)
                                                       .UseNetwork(networkService).WaitForPort("2113/tcp", 30000 /*30s*/).Build();

            return container;
        }

        #endregion
    }
}