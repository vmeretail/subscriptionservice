namespace TestHarness
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;
    using Vme.DomainDrivenDesign.Supporting;
    using Vme.Eposity.Organisations.DomainEvents;
    using CatchUpSubscriptionSettings = EventStore.ClientAPI.CatchUpSubscriptionSettings;
    using EventStoreConnection = EventStore.ClientAPI.EventStoreConnection;
    using IEventStoreConnection = EventStore.ClientAPI.IEventStoreConnection;
    using ILogger = Microsoft.Extensions.Logging.ILogger;


    public class ProjectionPOC
    {

        public async Task Start(CancellationToken cancellationToken)
        {
            //Entry point

            String connectionString = "ConnectTo=tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();
            Int64 lastCheckpoint = 0;
            Int64 checkPointCount = 200;

            //This holds our state and event handlers
            TestProjection projection = new TestProjection();

            //Probably would live inside a Projection class to prevent duplicate
            var eventAppeared = new Action<EventStoreCatchUpSubscription, ResolvedEvent>((arg1, @event) =>
                                                                             {
                                                                                 //TODO filter these events out should be part of the Builder
                                                                                 if (@event.Event == null) return;

                                                                                 if (@event.Event.EventType.StartsWith("$")) return;

                                                                                 TestProjection.EventAppeared(arg1, @event, projection);
                                                                             });

            //NOTE: if we wanted to subscribe to multiple streams, we would create multiple Subscription
            //but importantly, pass in the same projection.EventAppeared
            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-OrganisationAggregate")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .SetLastCheckpoint(lastCheckpoint) //Load this from DB
                                                                  .AddEventAppearedHandler(eventAppeared)
                                                                  .AddLiveProcessingStartedHandler(upSubscription =>
                                                                                                   {
                                                                                                       //Just to show the state has been updated as expected
                                                                                                       projection.OrganisationNames.ForEach(Console.WriteLine);
                                                                                                   } )
                                                                  .AddLastCheckPointChanged((s,
                                                                                             l) =>
                                                                                            {
                                                                                                Console.WriteLine($"lastCheckpoint updated {l}");

                                                                                                //This is where the client would persist the lastCheckpoint
                                                                                                lastCheckpoint = l;
                                                                                            },checkPointCount)
                                                                                            .Build();

            await subscription.Start(cancellationToken);

        }



        public static JsonSerializerSettings GetEventStoreDefaultSettings() => new JsonSerializerSettings()
                                                                               {
                                                                                   ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                                                   TypeNameHandling = TypeNameHandling.All,
                                                                                   Formatting = Formatting.Indented,
                                                                                   DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                                                                   ContractResolver = (IContractResolver)new CamelCasePropertyNamesContractResolver()
                                                                               };


    }

    public class TestProjection
    {
        public List<String> OrganisationNames = new List<String>();


        private static void SaveState()
        {
            Console.WriteLine("State saved");
        }

        public static void EventAppeared(EventStoreCatchUpSubscription arg1,
                                  ResolvedEvent @event,
                                  TestProjection projection)
        {
            if (@event.Event == null) return;

            if (@event.Event.EventType.StartsWith("$")) return;


            //Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");

            Type type = Type.GetType(@event.Event.EventType); //target type

            var json = ASCIIEncoding.Default.GetString(@event.Event.Data);

            JsonConvert.DefaultSettings = ProjectionPOC.GetEventStoreDefaultSettings;

            DomainEvent domainEvent = (DomainEvent)JsonConvert.DeserializeObject(json, type);

            switch (domainEvent)
            {
                case OrganisationCreatedEvent e:

                    UpdateState(() =>
                                {
                                    Console.WriteLine($"OrganisationCreatedEvent appeared");

                                    projection.OrganisationNames.Add(e.OrganisationName);
                                });

                    break;
            }


        }


        private static void  UpdateState(Action a)
        {
            //TODO: Save state if we reach here (and changes)

            a();

            SaveState();
        }
    }


    /// <summary>
    /// </summary>
    internal class Program
    {
        #region Fields

        private readonly static HttpClient HttpClient = new HttpClient();

        #endregion

        #region Methods

        private static async Task CatchupTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("CatchupLogger");

            //RequestBin
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-TestStream").SetName("Test Catchup 1")
                                                                  //.SetLastCheckpoint(5000)
                                                                  .UseConnection(eventStoreConnection).AddEventAppearedHandler((upSubscription,
                                                                      @event) =>
                                                                  {
                                                                      Console
                                                                          .WriteLine($"{DateTime.UtcNow}: EventAppeared - Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

                                                                      if (@event.OriginalEventNumber == 5) //simulate subscription dropped
                                                                      {
                                                                          throw new Exception("Engineered Exception");

                                                                          try
                                                                          {
                                                                              throw new Exception("Engineered Exception");
                                                                          }
                                                                          catch(Exception e)
                                                                          {
                                                                              Console
                                                                                  .WriteLine($"{DateTime.UtcNow}: About to call stop {Thread.CurrentThread.ManagedThreadId}");

                                                                              //upSubscription.Stop();
                                                                          }
                                                                      }

                                                                      Console.WriteLine($"DELIVERED Event {@event.OriginalEventNumber}");
                                                                  })
                                                                  //.AddFailedEventHandler((streamName,
                                                                  //                        subscriptionName,
                                                                  //                        resolvedEvent) =>
                                                                  //                       {
                                                                  //                           Console.WriteLine($"Event [{resolvedEvent.OriginalEvent.EventNumber}] failed");
                                                                  //                       })
                                                                  .DrainEventsAfterSubscriptionDropped().AddSubscriptionDroppedHandler((upSubscription,
                                                                      reason,
                                                                      arg3) =>
                                                                  {
                                                                      Console
                                                                          .WriteLine($"{DateTime.UtcNow}: Override SubscriptionDropped {Thread.CurrentThread.ManagedThreadId}");
                                                                      eventStoreConnection.Close();
                                                                  }).DeliverTo(uri).UseHttpInterceptor(message =>
                                                                                                       {
                                                                                                           //The user can make some changes (like adding headers)
                                                                                                           message.Headers.Add("Authorization", "Bearer someToken");
                                                                                                       }).AddLogger(logger).Build();

            await subscription.Start(CancellationToken.None);

            //Thread.Sleep(5000);

            //subscription.Stop();
        }

        private static async Task CatchupTestWithLastCheckpoint()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();
            Int64 lastCheckpoint = 0;
            Int64 checkPointCount = 5;

            Subscription subscription = null;
            subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1").UseConnection(eventStoreConnection)
                                                     .AddEventAppearedHandler((upSubscription,
                                                                               @event) =>
                                                                              {
                                                                                  Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");
                                                                              }).AddLastCheckPointChanged((s,
                                                                                                           l) =>
                                                                                                          {
                                                                                                              Console.WriteLine($"lastCheckpoint updated {l}");

                                                                                                              //This is where the client would persist the lastCheckpoint
                                                                                                              lastCheckpoint = l;
                                                                                                          },
                                                                                                          checkPointCount).Build();

            await subscription.Start(CancellationToken.None);

            Console.WriteLine($"About to start from lastCheckpoint {lastCheckpoint}");

            //TODO: Should we allow a Restart (rather than guarding against >sratt being called again
            //If subscription.IsStarted is false, we should make sure this cna be restarted
            //It might mean dirty variables though, but lets test it and see.
            //Maybe a .Resume?
            subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1").UseConnection(eventStoreConnection)
                                                     .AddEventAppearedHandler((upSubscription,
                                                                               @event) =>
                                                                              {
                                                                                  Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");
                                                                              }).SetLastCheckpoint(lastCheckpoint).Build();

            await subscription.Start(CancellationToken.None);
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            ProjectionPOC projectionPoc = new ProjectionPOC();

            await projectionPoc.Start(CancellationToken.None);


            //await PersistentTest();

            Console.ReadKey();

            //return;

            String connectionString = "ConnectTo = tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            var eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            eventStoreConnection.Closed += EventStoreConnection_Closed;

            var f = await eventStoreConnection.SubscribeToStreamAsync("$projections-StoreProductManager-result", true, EventAppeared, (subscription,
                                                                                                            reason,
                                                                                                            arg3) => Console.WriteLine("Subscription Dropped"));

            Console.WriteLine($"{f.StreamId} - {f.IsSubscribedToAll} - {f.LastCommitPosition} - {f.LastEventNumber}");

            String streamname = @"$projections-StoreProductManager-result";
            https://65b9074cf7f4b17ee1d109e61af5e9d8.m.pipedream.net
            String url = "http://localhost:5023/api/events";

            //await eventStoreConnection.ConnectToPersistentSubscriptionAsync(streamname,
            //                                                          "Persistent Test 2", EventAppeared
            //                                                          );

            //var r = eventStoreConnection.SubscribeToStreamFrom(streamname,
            //                                                   14400000,
            //                                                   CatchUpSubscriptionSettings.Default,
            //                                                   async (subscription,
            //                                                          @event) =>
            //                                                   {
            //                                                       if (@event.OriginalEvent.EventType == "$>")
            //                                                           return;

            //                                                       var data = Encoding.Default.GetString(@event.OriginalEvent.Data);

            //                                                       HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            //                                                                                    {
            //                                                                                        Content = new StringContent(data, Encoding.UTF8, "application/json")
            //                                                                                    };

            //                                                       try
            //                                                       {
            //                                                           //When this code is hit, the next Event Appears!
            //                                                           var ret = await HttpClient.SendAsync(request, CancellationToken.None);


            //                                                           if(ret.IsSuccessStatusCode)
            //                                                            Console.WriteLine($"Processed {@event.OriginalEventNumber}");
            //                                                           else
            //                                                           {
            //                                                               Console.WriteLine($"Failed with {ret.StatusCode}");
            //                                                           }
            //                                                       }
            //                                                       catch(Exception e)
            //                                                       {
            //                                                           Console.WriteLine(e);
            //                                                           throw;
            //                                                       }

            //                                                   });

            //await CatchupTestWithLastCheckpoint();
            //await Program.CatchupTest();
            //await Program.PersistentTest();

            Console.ReadKey();
        }

        private static void EventStoreConnection_Closed(object sender, ClientClosedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static async Task EventAppeared(EventStoreSubscription arg1,
                                          ResolvedEvent @event)
        {
            Console.WriteLine($"START {@event.OriginalEventNumber} {@event.OriginalStreamId}");

            var json = ASCIIEncoding.Default.GetString(@event.Event.Data);

            Console.WriteLine(json);

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }

        private static void EventAppeared(EventStorePersistentSubscriptionBase arg1,
                                          ResolvedEvent @event)
        {
            Console.WriteLine($"START {@event.OriginalEventNumber} {@event.OriginalStreamId}");


            //Thread.Sleep(TimeSpan.FromSeconds(1));

            //arg1.Fail(@event, PersistentSubscriptionNakEventAction.Park, "Test");

            //Thread.Sleep(TimeSpan.FromSeconds(5));

            //Interlocked.Decrement(ref count);

            Console.WriteLine($"END {@event.OriginalEventNumber} {@event.OriginalStreamId}");
        }

        private static async Task PersistentTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            eventStoreConnection.Closed += (sender,
                                            args) => Console.WriteLine($"Connection close {args.Reason}");

            ILogger logger = new LoggerFactory().CreateLogger("PersistentLogger");

    //RequestBin
            Uri uri = new Uri("https://en6m4s6ex2wfg.x.pipedream.net");

            Int32 count = 0;

            Thread thread = new Thread(() =>
                                       {
                                           while (true)
                                           {
                                               Console.WriteLine($"Number of inflight is {count}");

                                               Thread.Sleep(TimeSpan.FromSeconds(5));
                                           }
                                       });

            //thread.Start();

            PersistentSubscriptionBuilder builder = PersistentSubscriptionBuilder.Create("$ce-order", "Persistent Test 2")
                                                                                 .UseConnection(eventStoreConnection)
                                                                                 .AddEventAppearedHandler((@base, @event) =>
                                                                                                          {
                                                                                                              //Interlocked.Increment(ref count);

                                                                                                                                                  Console.WriteLine($"START {@event.OriginalEventNumber}");

                                                                                                                                                  @base.Fail(@event,PersistentSubscriptionNakEventAction.Park,"Test");

                                                                                                                                                  //Thread.Sleep(TimeSpan.FromSeconds(5));

                                                                                                                                                  //Interlocked.Decrement(ref count);

                                                                                                                                                  Console.WriteLine($"END {@event.OriginalEventNumber}");
                                                                                                          })
                                                                                 // .AutoAckEvents()
                                                                                 .DeliverTo(uri)
                                                                                 .SetInFlightLimit(2)
                                                                                 .UseHttpInterceptor(message =>
                                                                                 {
                                                                                     //The user can make some changes (like adding headers)
                                                                                     message.Headers.Add("Authorization", "Bearer someToken");
                                                                                 }).AddLogger(logger);

            Subscription subscription = builder.Build();

            await subscription.Start(CancellationToken.None);

            Console.ReadKey();

            subscription.Stop();
        }

        private async Task Test(CancellationToken cancellationToken)
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            Uri uri = new Uri("https://localhost/api/yourAPI");

            var subscription = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1").UseConnection(eventStoreConnection).DeliverTo(uri).Build();

            await subscription.Start(cancellationToken);
        }

        #endregion
    }
}