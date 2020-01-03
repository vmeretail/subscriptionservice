namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;

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
        /// Starts the containers for scenario run.
        /// </summary>
        public void StartContainersForScenarioRun()
        {
            this.TestId = Guid.NewGuid();

            this.TestNetwork = new Builder().UseNetwork($"test-network-{Guid.NewGuid():N}").Build();

            this.EventStoreContainer = DockerHelper.CreateEventStoreContainer($"eventstore{this.TestId.ToString("N")}", this.TestNetwork, "");
            this.DummyRESTContainer = DockerHelper.CreateDummyRESTContainer($"vmedummyjson{this.TestId.ToString("N")}", this.TestNetwork, "");

            this.EventStoreContainer.Start();
            this.DummyRESTContainer.Start();

            this.EventStoreTcpPort = this.EventStoreContainer.ToHostExposedEndpoint("1113/tcp").Port;
            this.EventStoreHttpPort = this.EventStoreContainer.ToHostExposedEndpoint("2113/tcp").Port;
            this.DummyRESTHttpPort = this.DummyRESTContainer.ToHostExposedEndpoint("80/tcp").Port;

            // Verify the Event Store is running
            Retry.For(async () =>
                      {
                          String url = $"http://127.0.0.1:{this.EventStoreHttpPort}/ping";

                          HttpClient client = new HttpClient();

                          HttpResponseMessage response = await client.GetAsync(url);
                      }).Wait();
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
            IContainerService container = new Builder().UseContainer().UseImage(@"vmeretailsystems/vmedummyjson:latest").ExposePort(80)
                                                       .WithName(containerName).UseNetwork(networkService).WaitForPort("80/tcp", 30000 /*30s*/).Build();

            return container;
        }

        /// <summary>
        /// Creates the event store container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="mountDirectory">The mount directory.</param>
        /// <returns></returns>
        private static IContainerService CreateEventStoreContainer(String containerName,
                                                                   INetworkService networkService,
                                                                   String mountDirectory)
        {
            IContainerService container = new Builder().UseContainer().UseImage("eventstore/eventstore:release-5.0.5").ExposePort(2113).ExposePort(1113)
                                                       .WithName(containerName)
                                                       .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS=all", "EVENTSTORE_START_STANDARD_PROJECTIONS=true")
                                                       .Mount(mountDirectory, "/var/log/eventstore/", MountType.ReadWrite).UseNetwork(networkService)
                                                       .WaitForPort("2113/tcp", 30000 /*30s*/).Build();

            return container;
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public static class DockerExtension
    {
        #region Methods

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

        #endregion
    }
}