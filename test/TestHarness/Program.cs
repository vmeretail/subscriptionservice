namespace TestHarness
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Configuration;

    /// <summary>
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            List<Subscription> subscriptions = new List<Subscription>();
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup", "https://enyx4bscr5t6k.x.pipedream.net/", numberOfConcurrentMessages: 2, maxRetryCount:1));
            //subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup1", new Uri("https://enc6iva61l9nl.x.pipedream.net")));

            ISubscriptionService subscriptionService = new SubscriptionService(subscriptions, eventStoreConnection);

            subscriptionService.OnEventAppeared += Program.SubscriptionService_OnEventAppeared;

            subscriptionService.TraceGenerated += Program.SubscriptionService_TraceGenerated;
            await subscriptionService.Start(CancellationToken.None);

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

        /// <summary>
        /// Subscriptions the service trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private static void SubscriptionService_TraceGenerated(String trace)
        {
            Console.WriteLine(trace);
        }

        #endregion
    }
}