namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using EventStore.ClientAPI;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// </summary>
    /// <seealso cref="Xunit.IClassFixture{SubscriptionService.IntegrationTests.TestsFixture}" />
    /// <seealso cref="IntegrationTests.TestsFixture" />
    /// <seealso cref="IntegrationTests.TestsFixture" />
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
        /// The end point URL
        /// </summary>
        private readonly String EndPointUrl;

        /// <summary>
        /// The end point url1
        /// </summary>
        private readonly String EndPointUrl1;

        /// <summary>
        /// The test name
        /// </summary>
        private readonly String TestName;

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
        /// <param name="output">The output.</param>
        public PersistentSubscriptionsTests(TestsFixture data,
                                            ITestOutputHelper output)
        {
            this.TestsFixture = data;

            Type type = output.GetType();
            FieldInfo testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            ITest test = (ITest)testMember.GetValue(output);
            this.TestName = test.DisplayName.Split(".").Last(); //Make the name a little more readable.

            this.TestsFixture.LogMessageToTrace($"{this.TestName} starting");

            this.DockerHelper = new DockerHelper();

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun(this.TestName);
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
            this.EndPointUrl1 = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events1";
        }

        #endregion

        #region Properties

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

        #region Methods

        /// <summary>
        /// Checks the subscription has been created.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        public async Task CheckSubscriptionHasBeenCreated(Subscription subscription)
        {
            String uri = $"http://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/subscriptions/{subscription.StreamName}/{subscription.GroupName}/info";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMessage.Headers.Add("Accept", @"application/json");
            HttpResponseMessage responseMessage = await this.EventStoreHttpClient.SendAsync(requestMessage, CancellationToken.None);

            var jester = new
                         {
                             groupName = String.Empty,
                             eventStreamId = String.Empty,
                             config = new
                                      {
                                          startFrom = 0,
                                          maxRetryCount = 0
                                      }
                         };
            String responseContent = await responseMessage.Content.ReadAsStringAsync();
            var subscriptionInfo = JsonConvert.DeserializeAnonymousType(responseContent, jester);

            subscriptionInfo.groupName.ShouldBe(subscription.GroupName);
            subscriptionInfo.eventStreamId.ShouldBe(subscription.StreamName);
            subscriptionInfo.config.ShouldNotBeNull();
            subscriptionInfo.config.maxRetryCount.ShouldBe(subscription.MaxRetryCount);
            subscriptionInfo.config.startFrom.ShouldBe(subscription.StreamStartPosition);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.TestsFixture.LogMessageToTrace($"{this.TestName} about to teardown");

            this.EventStoreConnection.Close();
            this.DockerHelper.StopContainersForScenarioRun();

            this.TestsFixture.LogMessageToTrace($"{this.TestName} stopped.");
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_DifferentEventsMultipleEndpoints_EventsAreDelivered()
        {
            // 1. Arrange
            String aggregateName1 = "SalesTransactionAggregate";
            Guid aggregateId1 = Guid.NewGuid();
            String streamName1 = $"{aggregateName1}-{aggregateId1.ToString("N")}";

            String aggregateName2 = "SalesTransactionAggregate";
            Guid aggregateId2 = Guid.NewGuid();
            String streamName2 = $"{aggregateName2}-{aggregateId2.ToString("N")}";

            // Setup some dummy events in the Event Store
            var sale1 = new
                        {
                            AggregateId = aggregateId1,
                            EventId = Guid.NewGuid()
                        };

            var sale2 = new
                        {
                            AggregateId = aggregateId2,
                            EventId = Guid.NewGuid()
                        };

            await this.TestsFixture.PostEventToEventStore(sale1,
                                                          sale1.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale1.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            await this.TestsFixture.PostEventToEventStore(sale2,
                                                          sale2.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale2.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName1, "TestGroup", this.EndPointUrl));
            subscriptionList.Add(Subscription.Create(streamName2, "TestGroup1", this.EndPointUrl1));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale1.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale2.EventId
                                                },
                                                this.EndPointUrl1,
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
            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
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
        public async Task PersistentSubscriptions_EventDelivery_MultipleEndpoints_EventsAreDelivered()
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
            subscriptionList.Add(Subscription.Create(streamName, "TestGroup1", this.EndPointUrl1));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
                                                },
                                                this.EndPointUrl1,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_StartServiceThenPostEvents_EventIsDelivered()
        {
            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName, "TestGroup", this.EndPointUrl));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(subscriptionList, CancellationToken.None);

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

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
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
        public async Task PersistentSubscriptions_EventDelivery_UpdateSubscriptionConfigurationWhileRunning_EventsAreDelivered()
        {
            // 1. Arrange
            String aggregateName1 = "SalesTransactionAggregate";
            Guid aggregateId1 = Guid.NewGuid();
            String streamName1 = $"{aggregateName1}-{aggregateId1.ToString("N")}";

            String aggregateName2 = "SalesTransactionAggregate";
            Guid aggregateId2 = Guid.NewGuid();
            String streamName2 = $"{aggregateName2}-{aggregateId2.ToString("N")}";

            // Setup some dummy events in the Event Store
            var sale1 = new
                        {
                            AggregateId = aggregateId1,
                            EventId = Guid.NewGuid()
                        };

            var sale2 = new
                        {
                            AggregateId = aggregateId2,
                            EventId = Guid.NewGuid()
                        };

            await this.TestsFixture.PostEventToEventStore(sale1,
                                                          sale1.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale1.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            await this.TestsFixture.PostEventToEventStore(sale2,
                                                          sale2.EventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale2.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName1, "TestGroup", this.EndPointUrl));

            List<Subscription> subscriptionList2 = new List<Subscription>();
            subscriptionList2.Add(Subscription.Create(streamName2, "TestGroup", this.EndPointUrl1));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            await subscriptionService.Start(subscriptionList2, CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale1.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale2.EventId
                                                },
                                                this.EndPointUrl1,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

        /// <summary>
        /// Subscriptions the service custom event factory used translated events emitted.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_CustomEventFactoryUsed_TranslatedEventsEmitted()
        {
            // 1. Arrange
            var sale1 = new
                        {
                            AggregateId = Guid.NewGuid(),
                            id = 1
                        };

            Guid eventId = Guid.NewGuid();

            List<Subscription> subscriptionList = new List<Subscription>();

            subscriptionList.Add(Subscription.Create("$ce-SalesTransactionAggregate", "TestGroup", this.EndPointUrl));

            await this.TestsFixture.PostEventToEventStore(sale1,
                                                          eventId,
                                                          $"{this.EventStoreHttpAddress}/SalesTransactionAggregate-{sale1.AggregateId:N}",
                                                          this.EventStoreHttpClient);

            // 2. Act
            await this.EventStoreConnection.ConnectAsync();

            SubscriptionService subscriptionService = new SubscriptionService(new TestEventFactory(), this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            String eventAsJson = await this.TestsFixture.GetEvent(this.EndPointUrl, this.ReadModelHttpClient, 1);

            //Verify we have our expected fields
            JObject obj = JObject.Parse(eventAsJson);

            obj["AggregateId"].Value<String>().ShouldBe(sale1.AggregateId.ToString());
            obj["id"].Value<Int32>().ShouldBe(1);
            obj["EventId"].Value<String>().ShouldBe(eventId.ToString());

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

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

            subscriptionList.Add(Subscription.Create("$ce-SalesTransactionAggregate", "TestGroup", this.EndPointUrl));

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

            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            //TODO: We could return the events to check new fields.
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
        /// Subscriptions the service optional parameters default persistent subscription created.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_OptionalParametersDefault_PersistentSubscriptionCreated()
        {
            // 1. Arrange
            List<Subscription> subscriptionList = new List<Subscription>();
            String streamName = "$ce-SalesTransactionAggregate";
            String groupName = "TestGroup";
            subscriptionList.Add(Subscription.Create(streamName, groupName, this.EndPointUrl));

            // 2. Act
            await this.EventStoreConnection.ConnectAsync();

            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            subscriptionList.ForEach(async s => await this.CheckSubscriptionHasBeenCreated(s));

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
        }

        /// <summary>
        /// Subscriptions the service optional parameters set persistent subscription created.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_OptionalParametersSet_PersistentSubscriptionCreated()
        {
            // 1. Arrange
            List<Subscription> subscriptionList = new List<Subscription>();
            String streamName = "$ce-SalesTransactionAggregate";
            String groupName = "TestGroup";
            Int32 startFrom = 50;
            Int32 maxRetryCount = 1;
            subscriptionList.Add(Subscription.Create(streamName, groupName, this.EndPointUrl, maxRetryCount:maxRetryCount, streamStartPosition:startFrom));

            // 2. Act
            await this.EventStoreConnection.ConnectAsync();

            SubscriptionService subscriptionService = new SubscriptionService(this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += this.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(subscriptionList, CancellationToken.None);

            // 3. Assert
            subscriptionList.ForEach(async s => await this.CheckSubscriptionHasBeenCreated(s));

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