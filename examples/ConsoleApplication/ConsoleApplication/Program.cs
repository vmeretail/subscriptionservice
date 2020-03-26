namespace ConsoleApplication
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    internal class Program
    {
        #region Methods

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            // This example assumes you have an event store running locally on the default ports with the default username and password
            // If your eventstore connection information is different then update this connection string variable to point to your event store
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            // The subscription service requires an open connection to operate so create and open the connection here
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            // The subscription service requires an HTTP endpoint to post event data to, for this example we are using RequestBin (https://requestbin.com/)
            // To run the example either replace the Url below with one of your own endpoints or create a new one on RequestBin or a similar service and
            // update the Url below
            String endpointUrl = "https://enyx4bscr5t6k.x.pipedream.net/";

            //TODO: Review the text
            // Setup a list of persistent subscription objects, each one of these will represent a Persistent Subscription in the Event Store - Competing Consumers tab
            // Each subscription allows the setting of the following values:
            // Stream Name - the name of the Stream that will be listened to for Events
            // Group Name - Name for the Group this will be used for display purposes in the Event Store UI
            // EndPointUrl - HTTP Url that the event data will be POSTed to
            // Number of Concurrent Messages - THe number of messages that will be concurrently processed by this subscription
            // Max Retry Count - Number of times that the message will be retried if the first POST to the endpoint fails before it is NAK'd and parked in Event Store
            // Stream Start Position - Position that the persistent subscription will start form, this will normally be zero but this value can be used to ignore events
            //                         in a stream for example the events are malformed so you wish not to process these
            //List<Subscription> subscriptions = new List<Subscription>();
            //subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup", endpointUrl, numberOfConcurrentMessages:2, maxRetryCount:1));

            // Create instance of the class via the SubscriptionServiceBuilder
            Subscription subscription = PersistentSubscriptionBuilder.Create("$ce-TestStream", "TestGroup").UseConnection(eventStoreConnection)
                                                                     .DeliverTo(new Uri(endpointUrl)).Build();

            // Start the subscription service with the passed in configuration, this will create and connect to the subscriptions defined in your configuration
            await subscription.Start(CancellationToken.None);

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