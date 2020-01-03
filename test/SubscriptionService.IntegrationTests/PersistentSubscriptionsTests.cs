namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using EventStore.ClientAPI;
    using Newtonsoft.Json;
    using Xunit;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{SubscriptionService.IntegrationTests.TestsFixture}" />
    /// <seealso cref="System.IDisposable" />
    [Collection("Database collection")]
    public class PersistentSubscriptionsTests : IClassFixture<TestsFixture>, IDisposable
    {

        #region Fields

        /// <summary>
        /// The docker helper
        /// </summary>
        private readonly DockerHelper DockerHelper;

        /// <summary>
        /// The tests fixture
        /// </summary>
        private readonly TestsFixture TestsFixture;

        /// <summary>
        /// The end point URL
        /// </summary>
        private readonly String EndPointUrl;

        /// <summary>
        /// The event store connection
        /// </summary>
        /// <value>
        /// The event store connection.
        /// </value>
        public IEventStoreConnection EventStoreConnection { get; }

        /// <summary>
        /// The event store HTTP address
        /// </summary>
        /// <value>
        /// The event store HTTP address.
        /// </value>
        public String EventStoreHttpAddress { get; }

        /// <summary>
        /// The event store HTTP client
        /// </summary>
        /// <value>
        /// The event store HTTP client.
        /// </value>
        public HttpClient EventStoreHttpClient { get; }

        /// <summary>
        /// The read model HTTP client
        /// </summary>
        /// <value>
        /// The read model HTTP client.
        /// </value>
        public HttpClient ReadModelHttpClient { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscriptionsTests" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public PersistentSubscriptionsTests(TestsFixture data)
        {
            this.TestsFixture = data;
            Console.WriteLine("In the ctor");

            this.DockerHelper = new DockerHelper();

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun();
            this.EventStoreHttpAddress = $"http://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/streams";

            this.EventStoreHttpClient = this.TestsFixture.GetHttpClient();

            this.EventStoreHttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));
            this.ReadModelHttpClient = this.TestsFixture.GetHttpClient();

            // Build the Event Store Connection String 
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            // Setup the Event Store Connection
            this.EventStoreConnection = EventStore.ClientAPI.EventStoreConnection.Create(connectionString);

            this.EventStoreConnection.Connected += this.TestsFixture.EventStoreConnection_Connected;
            this.EventStoreConnection.Closed += this.TestsFixture.EventStoreConnection_Closed;
            this.EventStoreConnection.ErrorOccurred += this.TestsFixture.EventStoreConnection_ErrorOccurred;
            this.EventStoreConnection.Reconnecting += this.TestsFixture.EventStoreConnection_Reconnecting;

            this.EndPointUrl = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events";
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.EventStoreConnection.Close();
            this.DockerHelper.StopContainersForScenarioRun();
        }

        #region Methods

        /// <summary>
        /// Subscriptions the service multiple events posted all events delivered.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_MultipleEventsPosted_AllEventsDelivered()
        {
            // 1. Arrange
            var sale1 = new
                       {
                           AggregateId = Guid.NewGuid(),
                           EventId = Guid.NewGuid()
                       };

            var sale2 = new
                        {
                            AggregateId = Guid.NewGuid(),
                            EventId = Guid.NewGuid()
                        };

            List<Subscription> subscriptionList = new List<Subscription>();

            subscriptionList.Add(Subscription.Create($"$ce-SalesTransactionAggregate", "TestGroup", this.EndPointUrl));

            await this.TestsFixture.PostEventToEventStore(sale1,
                                                          sale1.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale1.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            await this.TestsFixture.PostEventToEventStore(sale2,
                                                          sale2.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale2.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            // 2. Act
            await this.EventStoreConnection.ConnectAsync();

            SubscriptionService subscriptionService = new SubscriptionService(subscriptionList, this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale1.EventId,
                                                    sale2.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_EventIsDelivered()
        {
            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            // Setup some dummy events in the Event Store
            var sale = new
                       {
                           AggregateId = aggregateId,
                           EventId = Guid.NewGuid()
                       };

            await this.TestsFixture.PostEventToEventStore(sale,
                                                          sale.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName, "TestGroup", this.EndPointUrl));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(subscriptionList, this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId,
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

        /// <summary>
        /// Subscriptions the service error has occured.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void SubscriptionService_ErrorHasOccured(String trace)
        {
            this.TestsFixture.LogMessageToTrace(trace);
        }

        /// <summary>
        /// Subscriptions the service trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void SubscriptionService_TraceGenerated(String trace)
        {
            this.TestsFixture.LogMessageToTrace(trace);
        }

        #endregion
    }
}