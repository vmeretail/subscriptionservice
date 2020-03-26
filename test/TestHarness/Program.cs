namespace TestHarness
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
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

        private static async Task CatchupTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("CatchupLogger");

            //RequestBin
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            //TODO:
            //1. Simple example of writing lastCheckpoint - EventHasBeenProcessed
            //3. We will eventually handle parked / dead letter events here inside EventAppearedFromCatchupSubscription
            //4. Signal catchup has been set to Stop and stop accepting anymore
            //5. remove Console.WriteLine
            //6. Stop persistent subscription
            //7. Stream Start position for persistent

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1")
                                                                  //.SetLastCheckpoint(5000)
                                                                  .UseConnection(eventStoreConnection)
                                                                  //.AddLiveProcessingStartedHandler(upSubscription =>
                                                                  //                                 {
                                                                  //                                     Console.WriteLine("Override LiveProcessingStarted");
                                                                  //                                 } )
                                                                  //.AddEventAppearedHandler((upSubscription,
                                                                  //                          @event) =>{
                                                                  //                             Console.WriteLine($"Override EventAppeared {@event.OriginalEventNumber}");

                                                                  //                          })
                                                                  //.AddSubscriptionDroppedHandler((upSubscription,
                                                                  //                                 reason,
                                                                  //                                 arg3) =>
                                                                  //                                {
                                                                  //                                    Console.WriteLine("Override SubscriptionDropped");
                                                                  //                                }
                                                                  //                                ) 
                                                                  .DeliverTo(uri).UseHttpInterceptor(message =>
                                                                                                     {
                                                                                                         //The user can make some changes (like adding headers)
                                                                                                         message.Headers.Add("Authorization", "Bearer someToken");
                                                                                                     }).AddLogger(logger).Build();

            await subscription.Start(CancellationToken.None);

            //subscription.Stop();
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            //await Program.CatchupTest();
            await Program.PersistentTest();

            Console.ReadKey();
        }

        private static async Task PersistentTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("CatchupLogger");

            //RequestBin
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            PersistentSubscriptionBuilder builder = PersistentSubscriptionBuilder.Create("$ce-CatchupTest", "Persistent Test 1")
                                                                                 .UseConnection(eventStoreConnection).AddEventAppearedHandler((@base,
                                                                                                                                               @event) =>
                                                                                                                                              {
                                                                                                                                                  Console
                                                                                                                                                      .WriteLine("Override EventAppeared called.");
                                                                                                                                              }).AutoAckEvents()
                                                                                 .DeliverTo(uri).UseHttpInterceptor(message =>
                                                                                                                    {
                                                                                                                        //The user can make some changes (like adding headers)
                                                                                                                        message.Headers.Add("Authorization",
                                                                                                                                            "Bearer someToken");
                                                                                                                    }).AddLogger(logger);

            Subscription subscription = builder.Build();

            await subscription.Start(CancellationToken.None);
        }

        #endregion
    }
}