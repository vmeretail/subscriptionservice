using NUnit.Framework;

namespace NUnitTestProject1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;

    public class Tests
    {
        private DockerHelper DockerHelper;

        [SetUp]
        public void Setup()
        {
            this.DockerHelper = new DockerHelper();

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun();
        }

        [Test]
        public async Task Test1()
        {
            String url = $"http://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/ping";

            HttpClient client = new HttpClient();

            var response = await client.GetAsync(url);

            Console.WriteLine(response.StatusCode);

            Console.WriteLine("after test code");
        }
    }

    public class DockerHelper
    {
        private INetworkService TestNetwork;

        private IContainerService EventStoreContainer;

        private IContainerService DummyRESTContainer;

        private Guid TestId;

        /// <summary>
        /// Gets the event store TCP port.
        /// </summary>
        /// <value>
        /// The event store TCP port.
        /// </value>
        public Int32 EventStoreTcpPort { get; private set; }

        /// <summary>
        /// Gets the event store HTTP port.
        /// </summary>
        /// <value>
        /// The event store HTTP port.
        /// </value>
        public Int32 EventStoreHttpPort { get; private set; }

        public Int32 DummyRESTHttpPort { get; private set; }

        public void StartContainersForScenarioRun()
        {
            this.TestId = Guid.NewGuid();

            this.TestNetwork = new Ductus.FluentDocker.Builders.Builder().UseNetwork($"test-network-{Guid.NewGuid():N}").Build();

            this.EventStoreContainer = CreateEventStoreContainer($"eventstore{this.TestId.ToString("N")}", this.TestNetwork, "");
            this.DummyRESTContainer = DockerHelper.CreateDummyRESTContainer($"vmedummyjson{this.TestId.ToString("N")}", this.TestNetwork, "");

            this.EventStoreContainer.Start();
            this.DummyRESTContainer.Start();

            this.EventStoreTcpPort = this.EventStoreContainer.ToHostExposedEndpoint("1113/tcp").Port;
            this.EventStoreHttpPort = this.EventStoreContainer.ToHostExposedEndpoint("2113/tcp").Port;
            this.DummyRESTHttpPort = this.DummyRESTContainer.ToHostExposedEndpoint("80/tcp").Port;
        }

        public void StopContainersForScenarioRun()
        {
            //if (this.EventStoreContainer != null)
            //{
            //    this.EventStoreContainer.ClearUpContainer();
            //}

            //if (this.DummyRESTContainer != null)
            //{
            //    this.DummyRESTContainer.ClearUpContainer();
            //}
        }

        private static IContainerService CreateEventStoreContainer(String containerName,
                                                                   INetworkService networkService,
                                                                   String mountDirectory)
        {
            IContainerService container = new Ductus.FluentDocker.Builders.Builder()
                                          .UseContainer().UseImage("eventstore/eventstore:release-5.0.5").ExposePort(2113).ExposePort(1113).WithName(containerName)
                                          .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS=all", "EVENTSTORE_START_STANDARD_PROJECTIONS=true")
                                          .Mount(mountDirectory, "/var/log/eventstore/", MountType.ReadWrite).UseNetwork(networkService)
                                          .WaitForPort("2113/tcp", 30000 /*30s*/).Build();

            return container;
        }

        private static IContainerService CreateDummyRESTContainer(String containerName,
                                                                  INetworkService networkService,
                                                                  String mountDirectory)
        {
            IContainerService container = new Ductus.FluentDocker.Builders.Builder().UseContainer().UseImage("vmedummyjsonapi").ExposePort(80)
                                                                                    .WithName(containerName).UseNetwork(networkService)
                                                                                    .WaitForPort("80/tcp", 30000 /*30s*/).Build();

            return container;
        }
    }

    public static class DockerExtension
    {
        /// <summary>
        /// ClearUpContainer the container.
        /// </summary>
        /// <param name="containerService">The container service.</param>
        public static void ClearUpContainer(this IContainerService containerService)
        {
            IList<IVolumeService> volumes = new List<IVolumeService>();
            IList<INetworkService> networks = containerService.GetNetworks();

            foreach (INetworkService networkService in networks)
            {
                networkService.Detatch(containerService, true);
            }

            // Doing a direct call to .GetVolumes throws an exception if there aren't any so we need to check first :|
            IDictionary<String, VolumeMount> configurationVolumes = containerService.GetConfiguration(true).Config.Volumes;
            if (configurationVolumes != null && configurationVolumes.Any())
            {
                volumes = containerService.GetVolumes();
            }

            containerService.StopOnDispose = true;
            containerService.RemoveOnDispose = true;
            containerService.Dispose();

            foreach (IVolumeService volumeService in volumes)
            {
                volumeService.Stop();
                volumeService.Remove(true);
                volumeService.Dispose();
            }
        }
    }
}