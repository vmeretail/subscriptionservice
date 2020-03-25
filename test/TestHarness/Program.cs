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

            Uri uri = new Uri("https://envfx96fll0ja.x.pipedream.net");

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-OrderAggregate")
                                                                  .SetName("Test Catchup 1")
                                                                  //.SetLastCheckpoint(5000)
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddSubscriptionDroppedHandler(() => { Console.WriteLine("Subscription Dropped"); }) //
                                                                  .DeliverTo(uri)
                                                                  .AddLogger(logger)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);

            //ISubscriptionService subscriptionService = new SubscriptionServiceBuilder().UseConnection(eventStoreConnection).AddLogger(logger).Build();

            //Uri uri = new Uri("https://envfx96fll0ja.x.pipedream.net");
            //CatchupSubscription  catchupSubscription= CatchupSubscription.Create("Test Catchup1", "$ce-OrderAggregate", uri);

            //TODO: Do we need a different version of this for catchups?
            //It's possible the caller wants to update "lastCheckpoint" at this stage or would we inject that (like the proposed parked stuff?)
            //    subscriptionService.OnEventAppeared += SubscriptionService_OnEventAppeared;

            //    subscriptionService.OnCatchupSubscriptionDropped += (sender, e) =>
            //                                                        {
            //                                                            SubscriptionService_OnCatchupSubscriptionDropped(eventStoreConnection, subscriptionService, sender, e);
            //                                                        };

            //    await subscriptionService.StartCatchupSubscription(catchupSubscription, CancellationToken.None);
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

        private static void SubscriptionService_OnCatchupSubscriptionDropped(IEventStoreConnection eventStoreConnection,
                                                                             ISubscriptionService subscriptionService,
                                                                             Object sender,
                                                                             EventArgs e)
        {
            Console.WriteLine("Closing connection");

            //This seems to be the only way to stop the catchup subscription!
            eventStoreConnection.Close();

            ((SubscriptionService)subscriptionService).Stopwatch.Stop();

            Console.WriteLine($"{DateTime.UtcNow}: Time to stop was {((SubscriptionService)subscriptionService).Stopwatch.ElapsedMilliseconds} on managed thread{Thread.CurrentThread.ManagedThreadId}");
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