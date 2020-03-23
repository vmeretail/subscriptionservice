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
            await CatchupTest();

            Console.ReadKey();

            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.01:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            List<Subscription> subscriptions = new List<Subscription>();
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup1", "https://enyaw1mc4if0j.x.pipedream.net/", 2, 1));
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup1", "https://enr3vi91wr5c.x.pipedream.net/", 2, 1));
            
            ISubscriptionService subscriptionService = new SubscriptionServiceBuilder()
                                                       .UseConnection(eventStoreConnection)
                                                       .Build();

            subscriptionService.OnEventAppeared += Program.SubscriptionService_OnEventAppeared;

            await subscriptionService.Start(subscriptions, CancellationToken.None);

            await subscriptionService.RemoveSubscription("TestGroup", "$ce-TestStream", CancellationToken.None);

            Console.ReadKey();
        }

        private static async Task CatchupTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            CatchUpSubscriptionSettings catchUpSubscriptionSettings = new CatchUpSubscriptionSettings(100,100,true,true,"Test Subscription 1");

            //NOTE: Different way to connect to stream
            //NOTE: Could the UI be notified of this somehow
            eventStoreConnection.SubscribeToStreamFrom("$ce-CatchupTest",
                                                       null,//this is the important part, remembering the lastCheckpoint
                                                            //CatchUpSubscriptionSettings.Default, //Need to review these settings
                                                       catchUpSubscriptionSettings,
                                                       EventAppeared, //NOTE: The event appeared has some different arguments
                                                       LiveProcessingStarted,
                                                       SubscriptionDropped);
        }

        private static void SubscriptionDropped(EventStoreCatchUpSubscription arg1,
                                                SubscriptionDropReason arg2,
                                                Exception arg3)
        {
            //NOTE: What will we do here?
            Console.WriteLine($"SubscriptionDropped: Stream Name: [{arg1.SubscriptionName}] Reason[{arg2}]");

            Console.WriteLine("About to stop");
            arg1.Stop();

            Console.WriteLine("After stop");
        }

        private static void LiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            //NOTE: Once we have caught up, this gets fired - but any new events will then appear in EventAppeared
            //This is for information only (I think)
            Console.WriteLine($"LiveProcessingStarted: Stream Name: [{obj.SubscriptionName}]");
        }

        private static Task EventAppeared(EventStoreCatchUpSubscription arg1,
                                          ResolvedEvent arg2)
        {
            //The trick will be using our existing Event Appeared

            Console.WriteLine($"EventAppeared: Subscription Name: {arg1.SubscriptionName} Event Number: {arg2.OriginalEventNumber}");

            //NOTE: No Acking / Naking!

            //NOTE: Me might offer a "parking" facility here - we could write the event to a stream (added in as part of the initial config for this catchup)

            //throw new Exception("EventAppeared failed to deliver.");

            return Task.CompletedTask;
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