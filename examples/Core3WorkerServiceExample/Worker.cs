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
    using SubscriptionService.Configuration;

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

        /// <summary>
        /// The subscription service
        /// </summary>
        private ISubscriptionService SubscriptionService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
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

            // New up the Subscription Service instance via SubscriptionServiceBuilder
            this.SubscriptionService = new SubscriptionServiceBuilder().UseConnection(this.Connection).UseEventFactory(new WorkerEventFactory()).Build();

            // Use this event handler to wire up custom processing on each event appearing at the Persistent Subscription, an example use for this is 
            // adding a Authorization token onto the HTTP POST (as demonstrated below)
            // If there is no requirement to adjust the HTTP POST request then wiring up this event handler can be ommitted.
            this.SubscriptionService.OnEventAppeared += this.SubscriptionService_OnEventAppeared;

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Close the ES Connection
            this.Logger.LogInformation("About to close EventStore Connection");
            this.Connection.Close();

            this.Logger.LogInformation("About to Stop Worker Service");
            await base.StopAsync(cancellationToken);
        }

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

            // Setup a list of persistent subscription objects, each one of these will represent a Persistent Subscription in the Event Store - Competing Consumers tab
            // Each subscription allows the setting of the following values:
            // Stream Name - the name of the Stream that will be listened to for Events
            // Group Name - Name for the Group this will be used for display purposes in the Event Store UI
            // EndPointUrl - HTTP Url that the event data will be POSTed to
            // Number of Concurrent Messages - THe number of messages that will be concurrently processed by this subscription
            // Max Retry Count - Number of times that the message will be retried if the first POST to the endpoint fails before it is NAK'd and parked in Event Store
            // Stream Start Position - Position that the persistent subscription will start form, this will normally be zero but this value can be used to ignore events
            //                         in a stream for example the events are malformed so you wish not to process these
            this.Logger.LogInformation("About to Get Subscription Configuration");
            List<Subscription> subscriptions = new List<Subscription>();
            subscriptions.Add(Subscription.Create("$ce-TestStream", "TestGroup", endpointUrl, numberOfConcurrentMessages: 2, maxRetryCount: 1));

            // Start the subscription service, this will create and connect to the subscriptions defined in your configuration
            await this.SubscriptionService.Start(subscriptions, stoppingToken);
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