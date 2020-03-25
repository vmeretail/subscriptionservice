namespace TestHarness
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
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
            //2. Persistent Subscription needs implemented
            //3. We will eventually handle parked / dead letter events here inside EventAppearedFromCatchupSubscription
            //4. Signal catchup has been set to Stop and stop accepting anymore
            //5. remove Console.WriteLine

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                                  .SetName("Test Catchup 1")
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
                                                                  .DeliverTo(uri)
                                                                  .UseHttpInterceptor(message =>
                                                                                      {
                                                                                          //The user can make some changes (like adding headers)
                                                                                          message.Headers.Add("Authorization", "Bearer someToken");
                                                                                      })
                                                                  .AddLogger(logger)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);

            //subscription.Stop();

        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            await Program.CatchupTest();

            Console.ReadKey();

            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.01:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            List<global::SubscriptionService.Configuration.Subscription> subscriptions = new List<global::SubscriptionService.Configuration.Subscription>();
            subscriptions.Add(global::SubscriptionService.Configuration.Subscription.Create("$ce-TestStream",
                                                                                            "TestGroup1",
                                                                                            "https://enyaw1mc4if0j.x.pipedream.net/",
                                                                                            2,
                                                                                            1));
            subscriptions.Add(global::SubscriptionService.Configuration.Subscription.Create("$ce-TestStream",
                                                                                            "TestGroup1",
                                                                                            "https://enr3vi91wr5c.x.pipedream.net/",
                                                                                            2,
                                                                                            1));

            ISubscriptionService subscriptionService = new SubscriptionServiceBuilder().UseConnection(eventStoreConnection).Build();

            subscriptionService.OnEventAppeared += Program.SubscriptionService_OnEventAppeared;

            await subscriptionService.Start(subscriptions, CancellationToken.None);

            await subscriptionService.RemoveSubscription("TestGroup", "$ce-TestStream", CancellationToken.None);

            Console.ReadKey();
        }


        /// <summary>
        /// Subscriptions the service on event appeared.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private static void SubscriptionService_OnEventAppeared(Object sender,
                                                                HttpRequestMessage e)
        {
            //The user can make some changes (like adding headers)
            e.Headers.Add("Authorization", "Bearer someToken");
        }

        #endregion
    }
}