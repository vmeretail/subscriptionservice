namespace ConsoleApplication
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Common.Log;
    using EventStore.ClientAPI.Projections;
    using EventStore.ClientAPI.SystemData;
    using Newtonsoft.Json;
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

        private static async Task ACLRebuild()
        {
            // This example assumes you have an event store running locally on the default ports with the default username and password
            // If your event store connection information is different then update this connection string variable to point to your event store
            String connectionString = "ConnectTo=tcp://admin:changeit@staging2.eposity.com:1113;VerboseLogging=true;";

            // The subscription service requires an open connection to operate so create and open the connection here
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            String endpointUrl = "http://127.0.0.1:5003/api/events";

            String token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjZCN0FDQzUyMDMwNUJGREI0RjcyNTJEQUVCMjE3N0NDMDkxRkFBRTEiLCJ0eXAiOiJKV1QiLCJ4NXQiOiJhM3JNVWdNRnY5dFBjbExhNnlGM3pBa2ZxdUUifQ.eyJuYmYiOjE1ODg2OTIyNTIsImV4cCI6MTU4ODY5NTg1MiwiaXNzIjoiaHR0cDovL3N0YWdpbmcyLmVwb3NpdHkuY29tOjUwMjAiLCJhdWQiOlsiaHR0cDovL3N0YWdpbmcyLmVwb3NpdHkuY29tOjUwMjAvcmVzb3VyY2VzIiwiYXBpIiwiYXBpMSIsIkNvcmUiLCJkZXZlbG9wZXIiLCJJbnZlbnRvcnkiLCJQcm9kdWN0cyIsInNhbGVzIiwiU3VwcGx5Q2hhaW4iXSwiY2xpZW50X2lkIjoibmdDbGllbnRBdXRoQXBwIiwic2NvcGUiOlsiYXBpIiwiYXBpMSIsIkNvcmUiLCJkZXZlbG9wZXIiLCJJbnZlbnRvcnkiLCJQcm9kdWN0cyIsIlNhbGVzIiwiU3VwcGx5Q2hhaW4iXX0.YZGNvI_FRXKbQe-M2Xk2ufyf6IptnUvvLx3z_rvrXgYNlCws1aSTMsxztyUgUHW4zJO59H5NB8Y60hMdqn7OG3F2Qxq44aYVR9OnIk2nQd_z-OY-tmtjtYUhtOEPOzFPq-efZ7rGwawmWx-8zosq1dPcABYXA4QOqStE4xRus2wQE2H9ihSYrm8qLYDpYsFXNLk2ll_eSL_5v6YGzTwqUbyepsJPkdExd671EeTltDSr95CzaLz7JuhQvTclzAJ_ydNVKE51RmE1w_IgIs18HkzCdSFdAJGAZ7yBt3ngGmn1C457pOYZzbLG-Va8Yh_k-ff52IYU1-M5wZkSgDI-WQ";

            //Subscription persistentSubscription = PersistentSubscriptionBuilder.Create("$ce-OrganisationAggregate", "ACLRebuild")
            //                                                                   .UseConnection(eventStoreConnection)
            //                                                                   .UseHttpInterceptor(message =>
            //                                                                                       {
            //                                                                                           message.Headers.Add("Authorization", $"bearer {token}");
            //                                                                                       })
            //                                                                   .DeliverTo(new Uri(endpointUrl))
            //                                                                   .SetInFlightLimit(50) //50 concurrent events
            //                                                                   .Build();

            //PersistentSubscriptionSettings persistentSubscriptionSettings = PersistentSubscriptionSettings.Create().StartFrom(100000);

            Subscription persistentSubscription = PersistentSubscriptionBuilder.Create("$et-Vme.Eposity.Organisations.DomainEvents.OrganisationProduct.SupplierDetailsAddedToOrganisationProductEvent", "ACLRebuild")
                                                                               .UseConnection(eventStoreConnection)
                                                                               .UseHttpInterceptor(message =>
                                                                                                   {
                                                                                                       message.Headers.Add("Authorization", $"bearer {token}");
                                                                                                   })
                                                                               .DeliverTo(new Uri(endpointUrl))
                                                                                                                       //.AddEventAppearedHandler((subscription,
                                                                                                                       //                          @event) => Console.WriteLine(@event.Event.EventId))
                                                                               //.WithPersistentSubscriptionSettings(persistentSubscriptionSettings)
                                                                               .SetInFlightLimit(50) 
                                                                               .Build();

            //var catchup = CatchupSubscriptionBuilder.Create("$et-Vme.Eposity.Organisations.DomainEvents.OrganisationProduct.OrganisationProductCreatedEvent").SetLastCheckpoint(100000)
            //                                        .UseConnection(eventStoreConnection)
            //                                                                                                           .UseHttpInterceptor(message =>
            //                                                                                                                               {
            //                                                                                                                                   message.Headers.Add("Authorization", $"bearer {token}");
            //                                                                                                                               })
            //                                        .DeliverTo(new Uri(endpointUrl))
            //                                        .AddEventAppearedHandler((subscription,
            //                                                                  @event) => Console.WriteLine(@event.Event.EventId))
            //                                        .Build();

            //await catchup.Start(CancellationToken.None);

            await persistentSubscription.Start(CancellationToken.None);
        }


        private static async Task AddEvents(IEventStoreConnection eventStoreConnection,String stream,Int32 numberOfEvents)
        {
            List<EventData> events = new List<EventData>();
            var @event = new { id = Guid.NewGuid() };

            String json = JsonConvert.SerializeObject(@event);

            for (Int32 i = 0; i < numberOfEvents; i++)
            {
                events.Add(new EventData(Guid.NewGuid(), "AddedEvent", true, Encoding.Default.GetBytes(json), null));
            }

            await eventStoreConnection.AppendToStreamAsync(stream,
                                                           -2,
                                                           events,
                                                           null);
        }

        private static async Task StartProjection()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);

            ProjectionsManager debugProjectionManager = new ProjectionsManager(new ConsoleLogger(), endPoint, TimeSpan.FromSeconds(30), null, "http");

            String projection = @"fromStreams('$ce-Steven','$ce-Stuart','$ce-Dave')
                .when({
                $init: (s, e) => {
                           return { stevenCount: 0,stuartCount: 0, daveCount: 0}
                       },
      
                'StevenAddedEvent' : (s, e) => {
                                         s.stevenCount++;
                                     },
      
                'DaveAddedEvent': (s, e) => {
                                 s.daveCount++;
                             },
                'StuartAddedEvent': (s, e) => {
                                 s.stuartCount++;
                             }
            })";

            try
            {
                await debugProjectionManager.CreateContinuousAsync("TestProjection1", projection, new UserCredentials("admin", "changeit"));
            }
            catch
            {
                //silently handle for now
            }
        }

        private static async Task Scavenge()
        {
            HttpClient httpClient = new HttpClient();
            String uri = $"http://127.0.0.1:2113/admin/scavenge";

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            requestMessage.Headers.Add("Accept", @"application/json");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));

            await httpClient.SendAsync(requestMessage, CancellationToken.None);
        }

        private static async Task TruncateStreamTcp(IEventStoreConnection eventStoreConnection, String stream,
                                                 Int32 truncateBefore)
        {
            await eventStoreConnection.SetStreamMetadataAsync(stream, -2, StreamMetadata.Create(truncateBefore: truncateBefore));
        }

        private static async Task TruncateStreamHttp(String stream,
                                                    Int32 truncateBefore)
        {
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:2113/streams/{stream}/metadata");
            requestMessage.Headers.Add("Accept", @"application/json");
            requestMessage.Headers.Add("Content", @"application/json");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));

            requestMessage.Headers.Add("eventId", Guid.NewGuid().ToString());

            String payload = "[{\"eventId\":\"16768949-8949-8949-8949-159016768949\",\"eventType\":\"truncate\",\"data\":{\"$tb\":5},\"metadata\":{}}]";

            payload = payload.Replace("$tb:5", $"$tb:{truncateBefore}");

            requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(requestMessage, CancellationToken.None);

            var resposne = await response.Content.ReadAsStringAsync();
        }


        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            //await ACLRebuild();

            //Console.ReadKey();

            //return;

            // This example assumes you have an event store running locally on the default ports with the default username and password
            // If your event store connection information is different then update this connection string variable to point to your event store
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            // The subscription service requires an open connection to operate so create and open the connection here
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);

            await eventStoreConnection.ConnectAsync();

            eventStoreConnection.AuthenticationFailed += EventStoreConnection_AuthenticationFailed;
            eventStoreConnection.Connected += EventStoreConnection_Connected;
            eventStoreConnection.Reconnecting += EventStoreConnection_Reconnecting;
            eventStoreConnection.ErrorOccurred += EventStoreConnection_ErrorOccurred;
            eventStoreConnection.Disconnected += EventStoreConnection_Disconnected;
            eventStoreConnection.Closed += EventStoreConnection_Closed;

            await AddEvents(eventStoreConnection, "Steven-1", 100);
            await AddEvents(eventStoreConnection, "Dave-1", 100);
            await AddEvents(eventStoreConnection, "Stuart-1", 100);

            await StartProjection();

            //TODO: Get the state?

            //Truncate stream 1
            await TruncateStreamTcp(eventStoreConnection, "$ce-Steven", 5);

            await Scavenge();

            //Truncate

            Thread.Sleep(5000);

            await AddEvents(eventStoreConnection, "Dave-1", 10);

            await TruncateStreamHttp("$ce-Stuart", 10);

            Console.ReadKey();

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

        private static void EventStoreConnection_Closed(object sender, ClientClosedEventArgs e)
        {
            Console.WriteLine("EventStoreConnection_Closed");
        }

        private static void EventStoreConnection_Disconnected(object sender, ClientConnectionEventArgs e)
        {
            Console.WriteLine("EventStoreConnection_Disconnected");
        }

        private static void EventStoreConnection_AuthenticationFailed(object sender, ClientAuthenticationFailedEventArgs e)
        {
            Console.WriteLine("EventStoreConnection_AuthenticationFailed");
        }

        private static void EventStoreConnection_Connected(object sender, ClientConnectionEventArgs e)
        {
            Console.WriteLine("Connected");
        }

        private static void EventStoreConnection_ErrorOccurred(object sender, ClientErrorEventArgs e)
        {
            Console.WriteLine("Error");
        }

        private static void EventStoreConnection_Reconnecting(object sender, ClientReconnectingEventArgs e)
        {
            Console.WriteLine("Reconencting");
        }

        #endregion
    }
}