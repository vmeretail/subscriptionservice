namespace SubscriptionService
{
    using System;
    using System.Dynamic;
    using System.Threading;
    using System.Threading.Tasks;
    using Domain;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Factories;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class Subscription
    {
        private readonly SubscriptionBuilder SubscriptionBuilder;

        /// <summary>
        /// The event factory
        /// </summary>
        private readonly IEventFactory EventFactory;

        /// <summary>
        /// The event store connection
        /// </summary>
        private readonly IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The log events settings
        /// </summary>
        private readonly SubscriptionServiceBuilder.LogEvents LogEventsSettings;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger Logger;

        private UserCredentials DefaultUserCredentials;

        internal Subscription(SubscriptionBuilder subscriptionBuilder)
        {
            if (subscriptionBuilder == null)
            {
                throw new NullReferenceException("subscriptionBuilder cannot be null");
            }

            if (subscriptionBuilder.EventStoreConnection == null)
            {
                throw new NullReferenceException("EventStoreConnection cannot be null");
            }

            this.SubscriptionBuilder = subscriptionBuilder;

            this.EventFactory = subscriptionBuilder.EventFactory;
            this.EventStoreConnection = subscriptionBuilder.EventStoreConnection;
            this.Logger = subscriptionBuilder.Logger;

            // Cache the user credentials
            this.DefaultUserCredentials = new UserCredentials(subscriptionBuilder.Username, subscriptionBuilder.Password);

            //this.LogEventsSettings = subscriptionBuilder.LogEventsSettings;
        }

        private async Task Start (CatchupSubscriptionBuilder catchupSubscriptionBuilder,CancellationToken cancellationToken)
        {
            //TODO: over time, we might allow more of the settings to be fed in via the CatchupSubscription
            CatchUpSubscriptionSettings catchUpSubscriptionSettings = new CatchUpSubscriptionSettings(CatchUpSubscriptionSettings.Default.MaxLiveQueueSize,
                                                                                                      CatchUpSubscriptionSettings.Default.ReadBatchSize,
                                                                                                      CatchUpSubscriptionSettings.Default.VerboseLogging,
                                                                                                      CatchUpSubscriptionSettings.Default.ResolveLinkTos,
                                                                                                      catchupSubscriptionBuilder.SubscriptionName);
            async void AppearedFromCatchupSubscription(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                                       ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedFromCatchupSubscription((EventStoreStreamCatchUpSubscription)eventStoreCatchUpSubscription, resolvedEvent, null, cancellationToken);
            }

            //NOTE: Different way to connect to stream
            //NOTE: Could the UI be notified of this somehow
            EventStoreStreamCatchUpSubscription e = this.EventStoreConnection.SubscribeToStreamFrom(catchupSubscriptionBuilder.StreamName,
                                                                                                    catchupSubscriptionBuilder.LastCheckpoint, 
                                                                                                    catchUpSubscriptionSettings,
                                                                                                    AppearedFromCatchupSubscription,
                                                                                                    this.LiveProcessingStarted,
                                                                                                    this.SubscriptionDropped);
        }

        private async void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                               SubscriptionDropReason subscriptionDropReason,
                                               Exception e)
        {
            //TODO: remove
            Console.WriteLine("SubscriptionDropped");
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            //NOTE: Once we have caught up, this gets fired - but any new events will then appear in EventAppeared
            //This is for information only (I think)
            this.Logger.LogInformation($"LiveProcessingStarted: Stream Name: [{obj.SubscriptionName}]");

            //TODO: Temp unitl I work out why logger not working
            Console.WriteLine($"{DateTime.UtcNow}: Live processing started on managed thread { Thread.CurrentThread.ManagedThreadId}");
        }

        private void Start(PersistentSubscriptionBuilder catchupSubscriptionBuilder)
        {
            
        }

        private async Task EventAppearedFromCatchupSubscription(EventStoreStreamCatchUpSubscription eventStoreCatchUpSubscription,
                                                                ResolvedEvent resolvedEvent,
                                                                Consumer consumer,
                                                                CancellationToken cancellationToken)
        {
            await this.EventAppeared(resolvedEvent, consumer, cancellationToken);
        }

        private async Task EventAppeared(ResolvedEvent resolvedEvent,
                                         Consumer consumer,
                                         CancellationToken cancellationToken)
        {
            //Standard flow
            Console.WriteLine($"{DateTime.UtcNow}: Received event { resolvedEvent.OriginalEventNumber}({ resolvedEvent.OriginalEvent.EventType}) on managed thread{ Thread.CurrentThread.ManagedThreadId}");
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            //TODO: Guard against this be called more than once

            Console.WriteLine($"{DateTime.UtcNow}: {this.SubscriptionBuilder.GetType()} Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

            //Choose which start route
            await this.Start((dynamic)this.SubscriptionBuilder, cancellationToken);

            this.IsStarted = true;
        }

        public Boolean IsStarted { get; set; }

        public void Stop()
        {
            //TODO: dyanmic?
            this.Stop((dynamic)this.SubscriptionBuilder);
        }

        private void Stop(CatchupSubscriptionBuilder catchupSubscriptionBuilder)
        {
            //TODO: Need tod ecid how we are going to access to the EventStoreStreamCatchUpSubscription
            //Internal Add EventStoreStreamCatchUpSubscription to CatchupSubscriptionBuilder ??? 
        }
    }


    public class PersistentSubscriptionBuilder : SubscriptionBuilder
    {
        public PersistentSubscriptionBuilder WithStreamAndGroup(String streamName,String groupName)
        {
            this.StreamName = streamName;
            this.GroupName = groupName;

            return this;
        }

        internal String StreamName;
        internal String GroupName;
    }

    public abstract class SubscriptionBuilder
    {
        /// <summary>
        /// The logger
        /// </summary>
        internal ILogger Logger;

        /// <summary>
        /// The password
        /// </summary>
        internal String Password;

        /// <summary>
        /// The username
        /// </summary>
        internal String Username;

        /// <summary>
        /// The event factory
        /// </summary>
        internal IEventFactory EventFactory;

        /// <summary>
        /// The event store connection
        /// </summary>
        internal IEventStoreConnection EventStoreConnection;

        public SubscriptionBuilder()
        {
            this.Logger = NullLogger.Instance;
            this.EventFactory = Factories.EventFactory.Create();
            this.Username = "admin";
            this.Password = "changeit";
        }

        public SubscriptionBuilder UseConnection(IEventStoreConnection eventStoreConnection)
        {
            this.EventStoreConnection = eventStoreConnection;

            return this;
        }

        //TODO: Add EventAppeared event handler

        //Add Consumer
        //Consumer consumer = new ConsumerBuilder().AddEndpointUri(catchupSubscription.EndPointUri).Build();


        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public SubscriptionBuilder AddLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public SubscriptionBuilder UseEventFactory(IEventFactory eventFactory)
        {
            this.EventFactory = eventFactory;

            return this;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public SubscriptionBuilder WithPassword(String password)
        {
            this.Password = password;

            return this;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public SubscriptionBuilder WithUserName(String username)
        {
            this.Username = username;

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        public virtual Subscription Build()
        {
            //TODO: Change
            return new Subscription(this);
        }
    }

    /// <summary>
    /// </summary>
    public sealed class SubscriptionServiceBuilder
    {
        #region Fields

        /// <summary>
        /// The event factory
        /// </summary>
        internal IEventFactory EventFactory;

        /// <summary>
        /// The event store connection
        /// </summary>
        internal IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The log events settings
        /// </summary>
        internal LogEvents LogEventsSettings;

        /// <summary>
        /// The logger
        /// </summary>
        internal ILogger Logger;

        /// <summary>
        /// The password
        /// </summary>
        internal String Password;

        /// <summary>
        /// The username
        /// </summary>
        internal String Username;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionServiceBuilder" /> class.
        /// </summary>
        public SubscriptionServiceBuilder()
        {
            this.Logger = NullLogger.Instance;
            this.EventFactory = Factories.EventFactory.Create();
            this.Username = "admin";
            this.Password = "changeit";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder AddLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        public ISubscriptionService Build()
        {
            return new SubscriptionService(this);
        }

        /// <summary>
        /// Logs the events.
        /// </summary>
        /// <returns></returns>
        public SubscriptionServiceBuilder LogAllEvents()
        {
            this.LogEventsSettings |= LogEvents.All;

            return this;
        }

        /// <summary>
        /// Logs the events on error.
        /// </summary>
        /// <returns></returns>
        public SubscriptionServiceBuilder LogEventsOnError()
        {
            this.LogEventsSettings |= LogEvents.Errors;

            return this;
        }

        /// <summary>
        /// Uses the connection.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder UseConnection(IEventStoreConnection eventStoreConnection)
        {
            this.EventStoreConnection = eventStoreConnection;

            return this;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder UseEventFactory(IEventFactory eventFactory)
        {
            this.EventFactory = eventFactory;

            return this;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder WithPassword(String password)
        {
            this.Password = password;

            return this;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder WithUserName(String username)
        {
            this.Username = username;

            return this;
        }

        #endregion

        #region Others

        /// <summary>
        /// </summary>
        [Flags]
        internal enum LogEvents
        {
            /// <summary>
            /// The none
            /// </summary>
            None = 0,

            /// <summary>
            /// The errors
            /// </summary>
            Errors = 1,

            /// <summary>
            /// All
            /// </summary>
            All = 2
        }

        #endregion
    }
}