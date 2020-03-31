namespace ConsoleApplication
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    /// <summary>
    /// </summary>
    public class Program
    {
        #region Fields

        /// <summary>
        /// The connection string
        /// </summary>
        public static String ConnectionString = "ConnectTo=tcp://admin:changeit@localhost1113;VerboseLogging=true;";

        /// <summary>
        /// The endpoint1
        /// </summary>
        public static Uri Endpoint1 = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

        #endregion

        #region Methods

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            // This example assumes you have an event store running locally on the default ports with the default username and password
            // If your event store connection information is different then update this connection string variable to point to your event store
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            // The subscription service requires an open connection to operate so create and open the connection here
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            // The subscription service requires an HTTP endpoint to post event data to, for this example we are using RequestBin (https://requestbin.com/)
            // To run the example either replace the Url below with one of your own endpoints or create a new one on RequestBin or a similar service and
            // update the Url below
            String endpointUrl = "https://enyx4bscr5t6k.x.pipedream.net/";

            //Persistent Subscription
            Subscription persistentSubscription = PersistentSubscriptionBuilder.Create("$ce-TestStream", "TestGroup")
                                                                               .UseConnection(eventStoreConnection).DeliverTo(new Uri(endpointUrl))
                                                                               .SetInFlightLimit(50) //50 concurrent events
                                                                               .Build();

            await persistentSubscription.Start(CancellationToken.None);

            //Catchup Subscription
            Subscription catchupSubscription = CatchupSubscriptionBuilder.Create("$ce-TestStream")
                                                                         .UseConnection(eventStoreConnection).DeliverTo(new Uri(endpointUrl)).Build();

            await catchupSubscription.Start(CancellationToken.None);

            Console.ReadKey();
        }

        #endregion
    }
}