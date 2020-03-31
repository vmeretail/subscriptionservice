namespace TestHarness
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Microsoft.Extensions.Logging;

    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    internal class Program
    {
        #region Methods

        private static async Task CatchupTestWithLastCheckpoint()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@localhost:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();
            Int64 lastCheckpoint = 0;
            Int64 checkPointCount = 5;

            Subscription subscription = null;
            subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1")
                                                     .UseConnection(eventStoreConnection)
                                                     .AddEventAppearedHandler((upSubscription,
                                                                               @event) =>
                                                                              {
                                                                                  Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");
                                                                              })
                                                     .AddLastCheckPointChanged((s,
                                                                                l) =>
                                                                               {
                                                                                   Console.WriteLine($"lastCheckpoint updated {l}");

                                                                                   //This is where the client would persist the lastCheckpoint
                                                                                   lastCheckpoint = l;

                                                                               }, checkPointCount)
                                                     .Build();

            await subscription.Start(CancellationToken.None);

            Console.WriteLine($"About to start from lastCheckpoint {lastCheckpoint}");


            //TODO: Should we allow a Restart (rather than guarding against >sratt being called again
            //If subscription.IsStarted is false, we should make sure this cna be restarted
            //It might mean dirty variables though, but lets test it and see.
            //Maybe a .Resume?
            subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddEventAppearedHandler((upSubscription,
                                                                                            @event) =>
                                                                                           {
                                                                                               Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");
                                                                                           })
                                                                  .SetLastCheckpoint(lastCheckpoint)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);
        }

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
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddEventAppearedHandler((upSubscription,
                                                                                            @event) =>
                                                                                           {
                                                                                               Console.WriteLine($"{DateTime.UtcNow}: EventAppeared - Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

                                                                                               if (@event.OriginalEventNumber == 5) //simulate subscription dropped
                                                                                               {
                                                                                                   throw new Exception($"Engineered Exception");

                                                                                                   try
                                                                                                   {
                                                                                                       throw new Exception($"Engineered Exception");
                                                                                                   }
                                                                                                   catch (Exception e)
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
                                                                                                                                       }).DeliverTo(uri)
                                                                  .UseHttpInterceptor(message =>
                                                                                      {
                                                                                          //The user can make some changes (like adding headers)
                                                                                          message.Headers.Add("Authorization", "Bearer someToken");
                                                                                      }).AddLogger(logger).Build();

            await subscription.Start(CancellationToken.None);

            //Thread.Sleep(5000);

            //subscription.Stop();
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            //await CatchupTestWithLastCheckpoint();
            await Program.CatchupTest();
            //await Program.PersistentTest();

            Console.ReadKey();
        }

        private async Task Test(CancellationToken cancellationToken)
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            Uri uri = new Uri("https://localhost/api/yourAPI");

            var subscription = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1")
                                                            .UseConnection(eventStoreConnection)
                                                            .DeliverTo(uri)
                                                            .Build();

            await subscription.Start(cancellationToken);
        }

        private static async Task PersistentTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("PersistentLogger");

            //RequestBin
            Uri uri = new Uri("https://en6m4s6ex2wfg.x.pipedream.net");

            PersistentSubscriptionBuilder builder = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1")
                                                                                 .UseConnection(eventStoreConnection)
                                                                                 //.AddEventAppearedHandler((@base,
                                                                                 //                                                              @event) =>
                                                                                 //                                                             {
                                                                                 //                                                                 Console
                                                                                 //                                                                     .WriteLine("Override EventAppeared called.");
                                                                                 //                                                             }).AutoAckEvents()
                                                                                 .DeliverTo(uri)
                                                                                 .SetInFlightLimit(1)
                                                                                 .UseHttpInterceptor(message =>
                                                                                                                    {
                                                                                                                        //The user can make some changes (like adding headers)
                                                                                                                        message.Headers.Add("Authorization",
                                                                                                                                            "Bearer someToken");
                                                                                                                    }).AddLogger(logger);

            Subscription subscription = builder.Build();

            await subscription.Start(CancellationToken.None);

            Console.ReadKey();

            subscription.Stop();
        }

        #endregion
    }
}