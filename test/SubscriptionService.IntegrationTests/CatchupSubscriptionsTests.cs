using System;
using System.Collections.Generic;
using System.Text;

namespace SubscriptionService.IntegrationTests
{
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using EventStore.ClientAPI;
    using Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;
    using NLog.Extensions.Logging;
    using Shouldly;
    using Xunit;
    using Xunit.Abstractions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using SubscriptionService.Extensions;

    public static class Helper{
        public static List<EventData> GenerateEvents(Int32 numberOfEvents)
        {
            List<EventData> events = new List<EventData>();

            for (Int32 i = 0; i < numberOfEvents; i++)
            {
                var @event = new
                             {
                                 id = i + 1
                             };

                Guid eventId = Guid.NewGuid();
                String eventAsString = JsonConvert.SerializeObject(@event);
                EventData eventData = new EventData(eventId, "Test", true, Encoding.Default.GetBytes(eventAsString), null);

                events.Add(eventData);
            }

            return events;
        }
    }

    [Collection("Database collection")]
    public class CatchupSubscriptionsTests : IClassFixture<TestsFixture>, IDisposable
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

        #region Properties

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
        /// <param name="output">The output.</param>
        public CatchupSubscriptionsTests(TestsFixture data,
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
            this.Logger = loggerFactory.CreateLogger("CatchupSubscriptionsTests");

        }

        #endregion

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

        [Fact]
        public async Task CatchupSubscriptions_EventDelivery_EventIsDelivered()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId:N}";

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

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-SalesTransactionAggregate").SetName("CatchupTest1").UseConnection(eventStoreConnection)
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

        [Fact]
        public async Task CatchupSubscriptions_EventDelivery_DifferentEventsMultipleEndpoints_EventsAreDelivered()
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

            this.TestsFixture.LogMessageToTrace($"Endpoint is {this.EndPointUrl}");
            this.TestsFixture.LogMessageToTrace($"Endpoint1 is {this.EndPointUrl1}");

            // Setup a subscription configuration to deliver the events to the dummy REST
            Subscription subscription1 = CatchupSubscriptionBuilder.Create(streamName1).SetName("CatchupTest1").UseConnection(eventStoreConnection)
                                                                   .DeliverTo(new Uri(this.EndPointUrl)).AddLogger(this.Logger).Build();

            Subscription subscription2 = CatchupSubscriptionBuilder.Create(streamName2).SetName("CatchupTest2").UseConnection(eventStoreConnection)
                                                                   .DeliverTo(new Uri(this.EndPointUrl1)).AddLogger(this.Logger).Build();

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
            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-SalesTransactionAggregate").SetName("CatchupTest1").UseConnection(eventStoreConnection)
                                                                  .DeliverTo(new Uri(this.EndPointUrl))
                                                                  .AddLogger(this.Logger)
                                                                  .UseEventFactory(new TestEventFactory())
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

        [Fact]
        public async Task CatchupSubscriptions_EventDeliveryFailed_EventIsAddedToDeadLetterList()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");

            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId:N}";

            // Setup some dummy events in the Event Store
            var events = Helper.GenerateEvents(10);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, events);

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<ResolvedEvent> failedEvents = new List<ResolvedEvent>();

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-SalesTransactionAggregate").SetName("CatchupTest1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddEventAppearedHandler((upSubscription,
                                                                                            @event) =>
                                                                  {
                                                                      this.TestsFixture.LogMessageToTrace($"{DateTime.UtcNow}: EventAppeared - Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

                                                                      if (@event.OriginalEventNumber == 5) //simulate delivery failure
                                                                      {
                                                                          throw new Exception($"Engineered Exception");
                                                                      }
                                                                      this.TestsFixture.LogMessageToTrace($"DELIVERED Event {@event.OriginalEventNumber}");
                                                                  })
                                                                  .AddFailedEventHandler((streamName,
                                                                                          subscriptionName,
                                                                                          resolvedEvent) =>
                                                                                         {
                                                                                             failedEvents.Add(resolvedEvent);
                                                                                             this.TestsFixture.LogMessageToTrace($"Event [{resolvedEvent.OriginalEvent.EventNumber}] failed");
                                                                                         })
                                                                  .AddLogger(this.Logger).Build();

            // 2. Act
            // Start the subscription service
            await subscription.Start(CancellationToken.None);

            await Task.Delay(5000);

            // 3. Assert
            failedEvents.ShouldHaveSingleItem();
            failedEvents.Single().OriginalEventNumber.ShouldBe(5);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }



        [Fact]
        public async Task CatchupSubscriptions_LastCheckpointChanged_VerifyBroadcasts()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";
            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            Int32 totalEvents = 100;
            Int32 checkPointBroadcastFrequency = 10; //every 10 events we should get a message
            Int32 lastCheckpointBroadcasts = 0;
            Int64 lastCheckpoint = 0;

            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            //Generate 100 events
            var events = Helper.GenerateEvents(totalEvents);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, events);

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            //https://groups.google.com/forum/#!topic/event-store/fGIIDOSGa1Q
            //I think ther read buffersize is important here.
            //if ES has read all the events, it will signal LiveStarted, even though those events will most likely
            //not be processed yet.

            var catchUpSubscriptionSettings = new CatchUpSubscriptionSettings(1, 1, true, true,"$ce-SalesTransactionAggregate");

            // 2. Act
            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-SalesTransactionAggregate")
                                                                  .SetName("CatchupTest1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .WithCatchUpSubscriptionSettings(catchUpSubscriptionSettings)
                                                                  .AddEventAppearedHandler((upSubscription,
                                                                                            @event) =>
                                                                                           {
                                                                                               this.TestsFixture.LogMessageToTrace($"Event appeared {@event.OriginalEventNumber}");
                                                                                           })
                                                                  .AddLogger(this.Logger)
                                                                  .AddLastCheckPointChanged((s,
                                                                                             l) =>
                                                                                            {
                                                                                                this.TestsFixture.LogMessageToTrace($"LastCheckPoint changed {l}");

                                                                                                lastCheckpointBroadcasts++;
                                                                                                lastCheckpoint = l;
                                                                                            }, checkPointBroadcastFrequency)
                                                                  .AddLiveProcessingStartedHandler((upSubscription =>
                                                                                                    {
                                                                                                        this.TestsFixture.LogMessageToTrace($"LiveProcessingStarted");

                                                                                                        //Just signal we caught up.
                                                                                                        manualResetEvent.Set();
                                                                                                    }))
                
                                                                  .Build();

            await subscription.Start(CancellationToken.None);

            manualResetEvent.WaitOne(TimeSpan.FromSeconds(10));

            // 3. Assert
            lastCheckpointBroadcasts.ShouldBe(totalEvents/ checkPointBroadcastFrequency);
            lastCheckpoint.ShouldBe(99 );//TODO:

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }
        
        [Fact]
        //[InlineData(100,49)] - Issue #120
        public async Task CatchupSubscriptions_SetLastCheckpoint_StartsAtSelectedCheckpoint()
        {
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} started");
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";
            this.TestsFixture.LogMessageToTrace($"connectionString is {connectionString}");

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = await this.SetupEventStoreConnection(connectionString);

            // 1. Arrange
            Int32 totalEvents = 100;
            Int32 lastCheckPoint = 49;

            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            //Generate 100 events
            var events = Helper.GenerateEvents(totalEvents);

            await eventStoreConnection.AppendToStreamAsync(streamName, -1, events);

            // 2. Act
            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-SalesTransactionAggregate")
                                                                  .SetName("CatchupTest1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .DeliverTo(new Uri(this.EndPointUrl))
                                                                  .AddLogger(this.Logger)
                                                                  .UseEventFactory(new TestEventFactory())
                                                                  .SetLastCheckpoint(lastCheckPoint)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);

            // 3. Assert
            var expectedEvents = (totalEvents - lastCheckPoint) -1;
            var eventsToCheck = await this.TestsFixture.GetEvents(this.EndPointUrl, this.ReadModelHttpClient, expectedEvents);

            // 4. Cleanup
            subscription.Stop();
            eventStoreConnection.Close();
            this.TestsFixture.LogMessageToTrace($"TestMethod {this.TestName} finished");
        }
    }
}
