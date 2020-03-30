namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using EventStore.ClientAPI;
    using Extensions;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;
    using NLog.Extensions.Logging;
    using Shouldly;
    using UnitTests;
    using Xunit;
    using Xunit.Abstractions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    /// <seealso cref="IntegrationTests.TestsFixture" />
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

        private readonly ILogger Logger;

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

            this.TestName = this.TestsFixture.GenerateTestName(output);

            this.TestsFixture.LogMessageToTrace($"{this.TestName} starting - in constructor");

            this.DockerHelper = new DockerHelper(data);

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun(this.TestName);

            this.ReadModelHttpClient = this.TestsFixture.GetHttpClient();

            this.EndPointUrl = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events";
            this.EndPointUrl1 = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events1";
            
            LogManager.LoadConfiguration("nlog.config");

            NLogLoggerFactory loggerFactory = new NLogLoggerFactory();
            loggerFactory.AddNLog();
            this.Logger = loggerFactory.CreateLogger("PersistentSubscriptionsTests");
        }

        #endregion

        #region Properties

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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.TestsFixture.LogMessageToTrace($"{this.TestName} about to teardown");

            //this.EventStoreConnection.Close();
            this.DockerHelper.StopContainersForScenarioRun();

            this.TestsFixture.LogMessageToTrace($"{this.TestName} stopped.");
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_DifferentEventsMultipleEndpoints_EventsAreDelivered()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

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

            String eventAsString = JsonConvert.SerializeObject(sale1);
            EventData eventData = new EventData(Guid.NewGuid(), "Test", true, Encoding.Default.GetBytes(eventAsString), null);

            await eventStoreConnection.AppendToStreamAsync(streamName1, -1, eventData);

            eventAsString = JsonConvert.SerializeObject(sale2);
            eventData = new EventData(Guid.NewGuid(), "Test", true, Encoding.Default.GetBytes(eventAsString), null);

            await eventStoreConnection.AppendToStreamAsync(streamName2, -1, eventData);

            // Setup a subscription configuration to deliver the events to the dummy REST

            // Create instance of the Subscription Service
            Subscription subscription1 = PersistentSubscriptionBuilder.Create(streamName1, "TestGroup1")
                                                                      .UseConnection(eventStoreConnection).DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger)
                                                                      .Build();

            Subscription subscription2 = PersistentSubscriptionBuilder.Create(streamName2, "TestGroup2").UseConnection(eventStoreConnection).AddLogger(this.Logger)
                                                                      .DeliverTo(new Uri(this.EndPointUrl1)).Build();

            // 2. Act
            // Start the subscriptions
            await subscription1.Start(CancellationToken.None);
            await subscription2.Start(CancellationToken.None);

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
            subscription1.Stop();
            subscription2.Stop();

            eventStoreConnection.Close();

            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_EventIsDelivered()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

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
            String eventAsString = JsonConvert.SerializeObject(sale);
            EventData eventData = new EventData(Guid.NewGuid(), "Test", true, Encoding.Default.GetBytes(eventAsString), null);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, eventData);

            // Setup a subscription configuration to deliver the events to the dummy REST

            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, "TestGroup1").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).Build();

            // 2. Act
            // Start the subscription service
            await subscription.Start(CancellationToken.None);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_StartServiceThenPostEvents_EventIsDelivered()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, "TestGroup1").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).Build();

            // 2. Act
            // Start the subscription service
            await subscription.Start(CancellationToken.None);

            // Setup some dummy events in the Event Store
            var sale = new
                       {
                           AggregateId = aggregateId,
                           EventId = Guid.NewGuid()
                       };

            String eventAsString = JsonConvert.SerializeObject(sale);
            EventData eventData = new EventData(Guid.NewGuid(), "Test", true, Encoding.Default.GetBytes(eventAsString), null);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, eventData);

            // 3. Assert
            await this.TestsFixture.CheckEvents(new List<Guid>
                                                {
                                                    sale.EventId
                                                },
                                                this.EndPointUrl,
                                                this.ReadModelHttpClient);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        /// <summary>
        /// Subscriptions the service custom event factory used translated events emitted.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_CustomEventFactoryUsed_TranslatedEventsEmitted()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";
            var sale = new
                       {
                           AggregateId = aggregateId,
                           id = 1
                       };
            Guid eventId = Guid.NewGuid();

            String eventAsString = JsonConvert.SerializeObject(sale);
            EventData eventData = new EventData(eventId, "Test", true, Encoding.Default.GetBytes(eventAsString), null);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, eventData);

            // 2. Act
            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, "TestGroup1").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).UseEventFactory(new TestEventFactory())
                                                                     .Build();

            await subscription.Start(CancellationToken.None);

            // 3. Assert
            String eventAsJson = await this.TestsFixture.GetEvent(this.EndPointUrl, this.ReadModelHttpClient, 1);

            //Verify we have our expected fields
            JObject obj = JObject.Parse(eventAsJson);

            obj["AggregateId"].Value<String>().ShouldBe(sale.AggregateId.ToString());
            obj["id"].Value<Int32>().ShouldBe(1);
            obj["EventId"].Value<String>().ShouldBe(eventId.ToString());

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }



        /// <summary>
        /// Subscriptions the service optional parameters default persistent subscription created.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_OptionalParametersDefault_PersistentSubscriptionCreated()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange

            // 2. Act
            Subscription subscription = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName).UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).Build();

            await subscription.Start(CancellationToken.None);

            // 3. Assert
            await this.CheckSubscriptionHasBeenCreated(TestData.StreamName, TestData.GroupName, 10, 0);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        /// <summary>
        /// Subscriptions the service optional parameters set persistent subscription created.
        /// </summary>
        [Fact]
        public async Task SubscriptionService_OptionalParametersSet_PersistentSubscriptionCreated()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String streamName = "$ce-SalesTransactionAggregate";
            String groupName = "TestGroup";
            Int32 startFrom = 50;
            Int32 maxRetryCount = 1;

            PersistentSubscriptionSettings persistentSubscriptionSettings = PersistentSubscriptionSettings.Create();

            persistentSubscriptionSettings.MaxRetryCount = maxRetryCount;

            // 2. Act
            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, groupName)
                                                                     .UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl))
                                                                     .WithPersistentSubscriptionSettings(persistentSubscriptionSettings)
                                                                     .AddLogger(this.Logger).Build();

            await subscription.Start(CancellationToken.None);
             
            // 3. Assert
            //TODO: Check this
            await this.CheckSubscriptionHasBeenCreated(streamName, groupName, maxRetryCount, 0);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        [Fact(Skip = "review - should this just be Stop?")]
        public async Task SubscriptionService_RemoveSubscription_RemoveNonExsistantSubscription_ErrorThrown()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            List<Subscription> subscriptionList = new List<Subscription>();
            String streamName = "$ce-SalesTransactionAggregate";
            String groupName = "TestGroup";
            String groupNameToRemove = "TestGroup1";

            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, "TestGroup1").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).Build();

            await subscription.Start(CancellationToken.None);

            //TODO: Check this
            await this.CheckSubscriptionHasBeenCreated(streamName, groupName, 10, 0);

            // 2. Act & Assert
            //Should.Throw<InvalidOperationException>(async () => { await subscription.RemoveSubscription(groupNameToRemove, streamName, CancellationToken.None); });

            // 3. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        /// <summary>
        /// Checks the subscription has been created.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        private async Task CheckSubscriptionHasBeenCreated(String streamName,
                                                           String groupName,
                                                           Int32 maxRetryCount,
                                                           Int32 streamStartPosition)
        {
            String scheme = this.TestsFixture.EventStoreDockerConfiguration.IsLegacyVersion ? "http" : "https";
            String uri = $"{scheme}://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/subscriptions/{streamName}/{groupName}/info";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMessage.Headers.Add("Accept", @"application/json");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));

            HttpClient client = DockerHelper.CreateHttpClient(uri);
            HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, CancellationToken.None);
            responseMessage.IsSuccessStatusCode.ShouldBeTrue($"Response Code [{responseMessage.StatusCode} returned]");
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
            responseContent.ShouldNotBeNullOrEmpty();

            this.TestsFixture.LogMessageToTrace($"Response Content is [{responseContent}]");

            var subscriptionInfo = JsonConvert.DeserializeAnonymousType(responseContent, jester);

            subscriptionInfo.groupName.ShouldBe(groupName);
            subscriptionInfo.eventStreamId.ShouldBe(streamName);
            subscriptionInfo.config.ShouldNotBeNull();
            subscriptionInfo.config.maxRetryCount.ShouldBe(maxRetryCount);

            //TODO: Needs fixed
            //subscriptionInfo.config.startFrom.ShouldBe(streamStartPosition);
        }

        private async Task CheckSubscriptionHasBeenDeleted(String streamName,
                                                           String groupName)
        {
            String scheme = this.TestsFixture.EventStoreDockerConfiguration.IsLegacyVersion ? "http" : "https";
            String uri = $"{scheme}://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/subscriptions/{streamName}/{groupName}/info";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMessage.Headers.Add("Accept", @"application/json");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));

            HttpClient client = DockerHelper.CreateHttpClient(uri);
            HttpResponseMessage responseMessage = await client.SendAsync(requestMessage, CancellationToken.None);
            responseMessage.IsSuccessStatusCode.ShouldBeFalse($"Response Code [{responseMessage.StatusCode} returned]");
            responseMessage.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        private async Task<IEventStoreConnection> SetupEventStoreConnection(String connectionString)
        {
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);

            eventStoreConnection.Connected += (sender,
                                               args) =>
                                              {
                                                  this.TestsFixture.LogMessageToTrace("Connected");
                                              };

            eventStoreConnection.Closed += (sender,
                                            args) =>
                                           {
                                               this.TestsFixture.LogMessageToTrace("Closed");
                                           };

            eventStoreConnection.ErrorOccurred += (sender,
                                                   args) =>
                                                  {
                                                      this.TestsFixture.LogMessageToTrace($"ErrorOccurred {args.Exception}");
                                                  };

            eventStoreConnection.Reconnecting += (sender,
                                                  args) =>
                                                 {
                                                     this.TestsFixture.LogMessageToTrace("Reconnecting");
                                                 };

            await eventStoreConnection.ConnectAsync();
            return eventStoreConnection;
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

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_StopCalled_NotAllEventsDelivered()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            List<Guid> eventsToCheck = new List<Guid>();
            //Int32 expectedVersion = -1;

            List< EventData > events = new List<EventData>();

            //The 2000 could become Inline for ranges?
            // Setup some dummy events in the Event Store
            for (Int32 i = 0; i < 2000; i++)
            {
                var sale = new
                           {
                               AggregateId = aggregateId,
                               EventId = Guid.NewGuid()
                           };

                String eventAsString = JsonConvert.SerializeObject(sale);
                EventData eventData = new EventData(Guid.NewGuid(), "Test", true, Encoding.Default.GetBytes(eventAsString), null);

                events.Add(eventData);
                
                //expectedVersion++;
                eventsToCheck.Add(sale.EventId);
            }

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, events);

            List<ResolvedEvent> eventsDelivered = new List<ResolvedEvent>();

            // Setup a subscription configuration to deliver the events to the dummy REST
            Subscription subscription = PersistentSubscriptionBuilder.Create(streamName, "TestGroup1").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(this.EndPointUrl)).AddEventAppearedHandler((@base,
                                                                                                                                    @event) =>
                                                                                                                                   {
                                                                                                                                       eventsDelivered.Add(@event);
                                                                                                                                       Thread.Sleep(10);
                                                                                                                                   }).AddLogger(this.Logger)
                                                                     .AddSubscriptionDroppedHandler((@base,
                                                                                                     reason,
                                                                                                     arg3) =>
                                                                                                    {
                                                                                                        this.TestsFixture.LogMessageToTrace($"Subscription Dropped {this.TestName}");
                                                                                                    }).Build();

            // 2. Act
            // Start the subscription service
            await subscription.Start(CancellationToken.None);

            await Task.Delay(1000);

            subscription.Stop();

            // 3. Assert
            eventsDelivered.Count.ShouldNotBe(0);
            eventsDelivered.Count.ShouldNotBe(eventsToCheck.Count);

            // 4. Cleanup
            
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }

        #endregion
    }
}