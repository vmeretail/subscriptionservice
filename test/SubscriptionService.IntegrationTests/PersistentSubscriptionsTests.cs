namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{SubscriptionService.IntegrationTests.TestsFixture}" />
    /// <seealso cref="System.IDisposable" />
    public class PersistentSubscriptionsTests : IClassFixture<TestsFixture>
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

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscriptionsTests" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public PersistentSubscriptionsTests(TestsFixture data)
        {
            Console.WriteLine("In the ctor");
            this.TestsFixture = data;

            this.DockerHelper = this.TestsFixture.DockerHelper;
        }

        #endregion

        #region Methods

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

            await this.TestsFixture.PostEventToEventStore(sale, sale.EventId, streamName);

            String endPointUrl = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events";

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName, "TestGroup", endPointUrl));

            await this.TestsFixture.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(subscriptionList, this.TestsFixture.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>()
                                                {
                                                    sale.EventId
                                                });

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
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