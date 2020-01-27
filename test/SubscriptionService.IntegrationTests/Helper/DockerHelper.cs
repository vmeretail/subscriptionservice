﻿namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
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

                                HttpResponseMessage pingResponse = await client.GetAsync(url).ConfigureAwait(false);
                                pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                            }).Wait();

            Retry.For(async () =>
                            {
                                String url = $"http://127.0.0.1:{this.EventStoreHttpPort}/info";
                                HttpClient client = new HttpClient();

                                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Authorization", "Basic YWRtaW46Y2hhbmdlaXQ=");

                                HttpResponseMessage infoResponse = await client.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

                                infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
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
            IContainerService container = new Builder().UseContainer().UseImage(@"vmeretailsystems/vmedummyjson:latest",true).ExposePort(80)
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
            IContainerService container = new Builder().UseContainer().UseImage("eventstore/eventstore:release-5.0.5",true).ExposePort(2113).ExposePort(1113)
                                                       .WithName(containerName)
                                                       .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS=all", "EVENTSTORE_START_STANDARD_PROJECTIONS=true")
                                                       .Mount(mountDirectory, "/var/log/eventstore/", MountType.ReadWrite).UseNetwork(networkService)
                                                       .WaitForPort("2113/tcp", 30000 /*30s*/).Build();

            return container;
        }

        #endregion
    }
}