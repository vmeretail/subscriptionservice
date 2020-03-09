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
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.01:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            List<Subscription> subscriptions = new List<Subscription>();
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup", "https://enyaw1mc4if0j.x.pipedream.net/", 2, 1));
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup", "https://enr3vi91wr5c.x.pipedream.net/", 2, 1));

            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnection);

            subscriptionService.OnEventAppeared += Program.SubscriptionService_OnEventAppeared;

            subscriptionService.TraceGenerated += Program.SubscriptionService_TraceGenerated;
            subscriptionService.ErrorHasOccured += Program.SubscriptionService_ErrorHasOccured;

            await subscriptionService.Start(subscriptions, CancellationToken.None);
            

            Console.ReadKey();
        }

        /// <summary>
        /// Subscriptions the service error has occured.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private static void SubscriptionService_ErrorHasOccured(String trace)
        {
            Console.WriteLine(trace);
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