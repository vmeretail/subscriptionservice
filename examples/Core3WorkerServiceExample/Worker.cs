namespace Core3WorkerServiceExample
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    public class Worker : BackgroundService
    {
        #region Fields

        /// <summary>
        /// The connection
        /// </summary>
        private IEventStoreConnection Connection;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<Worker> Logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public Worker(ILogger<Worker> logger)
        {
            this.Logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            this.Logger.LogInformation("About to Start Worker Service");

            this.Logger.LogInformation("About to open EventStore Connection");
            // The subscription service requires an open connection to operate so create and open the connection here
            this.Connection = EventStoreConnection.Create(Worker.EventStoreConnectionString);
            await this.Connection.ConnectAsync();

            this.CurrentSubscriptions = new List<Subscription>();

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop all running subscriptions
            foreach (Subscription currentSubscription in this.CurrentSubscriptions)
            {
                currentSubscription.Stop();
            }

            // Close the ES Connection
            this.Logger.LogInformation("About to close EventStore Connection");
            this.Connection.Close();

            this.Logger.LogInformation("About to Stop Worker Service");
            await base.StopAsync(cancellationToken);
        }

        private List<Subscription> CurrentSubscriptions;

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // The subscription service requires an HTTP endpoint to post event data to, for this example we are using RequestBin (https://requestbin.com/)
            // To run the example either replace the Url below with one of your own endpoints or create a new one on RequestBin or a similar service and
            // update the Url below
            String endpointUrl = "https://enyaw1mc4if0j.x.pipedream.net/";

            // Setup a list of subscription objects, each one of these will represent a Persistent Subscription in the Event Store - Competing Consumers tab or 
            // a CatchUp Subscription

            // Each persistent subscription allows the setting of the following values on the Create() method:
            //      Stream Name - the name of the Stream that will be listened to for Events
            //      Group Name - Name for the Group this will be used for display purposes in the Event Store UI

            // Each catchup subscription allows the setting of the following values on the Create() method:
            //      Stream Name - the name of the Stream that will be listened to for Events

            // The other options that can be set on a subscription are as follows:
            // UseConnection - The event store connection to be used by the subscription
            // DeliverTo - The endpoint Uri that the events will be delivered to
            // AddLogger - Allows a custom logger to be injected that implements the Microsoft ILogger interface
            // UseEventFactory - Specify a custom factory that allows making changes to the events before they are posted
            //                   to the consumer
            // AddEventAppearedHandler - Allows the setting of a custom method to be executed when an event has appeared, this allows the addition
            //                           of custom HTTP headers such as authentication tokens
            // AddSubscriptionDroppedHandler - Allows the setting of a custom method that will be called when a subscription has been marked as dropped
            //                                 by EventStore
            // AutoAckEvents - Setup the subscription to automatically acknowledge events
            // ManuallyAckEvents - Setup the subscription to manually acknowledge events
            // SetInFlightLimit - Set the number of in flight messages for the subscription
            // WithPersistentSubscriptionSettings - Allows the passing in of the Event Store Client API PersistentSubscriptionSettings object
            // LogAllEvents - Log all the events to trace as they are being processed
            // LogEventsOnError - Log the events to trace as they are being processed if an error occurs
            // WithUserName - Set the user name to be used for the connection if not the default value
            // WithPassword - Set the password to be used for the connection if not the default value

            this.Logger.LogInformation("About to Get Subscription Configuration");

            Subscription subscription = PersistentSubscriptionBuilder.Create("$ce-TestStream", "TestGroup1")
                                                                     .UseConnection(this.Connection)
                                                                     .DeliverTo(new Uri(endpointUrl))
                                                                     .AddLogger(this.Logger)
                                                                     .UseEventFactory(new WorkerEventFactory()).Build();

            await subscription.Start(stoppingToken);

            this.CurrentSubscriptions.Add(subscription);
        }

        /// <summary>
        /// Subscriptions the service error has occured.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void SubscriptionService_ErrorHasOccured(String trace)
        {
            this.Logger.LogError(trace);
        }

        /// <summary>
        /// Subscriptions the service on event appeared.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void SubscriptionService_OnEventAppeared(Object sender,
                                                         HttpRequestMessage e)
        {
            //The user can make some changes (like adding headers)
            e.Headers.Add("Authorization", "Bearer someToken");
        }

        /// <summary>
        /// Subscriptions the service trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void SubscriptionService_TraceGenerated(String trace)
        {
            this.Logger.LogTrace(trace);
        }

        #endregion

        #region Others

        /// <summary>
        /// The event store connection string
        /// This example assumes you have an event store running locally on the default ports with the default username and password
        /// If your eventstore connection information is different then update this connection string variable to point to your event store
        /// </summary>
        private const String EventStoreConnectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

        #endregion
    }
}