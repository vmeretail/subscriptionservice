namespace SubscriptionService
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using Domain;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Factories;
    using Microsoft.Extensions.Logging;
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
        private readonly SubscriptionBuilder.LogEvents LogEventsSettings;

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

            this.LogEventsSettings = subscriptionBuilder.LogEventsSettings;
        }

        private async Task Start (CatchupSubscriptionBuilder catchupSubscriptionBuilder,CancellationToken cancellationToken)
        {
            ConsumerBuilder consumerBuilder = new ConsumerBuilder();

            var consumer = consumerBuilder.AddEndpointUri(catchupSubscriptionBuilder.Uri).Build();

            async void AppearedFromCatchupSubscription(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                                       ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedFromCatchupSubscription((EventStoreStreamCatchUpSubscription)eventStoreCatchUpSubscription, resolvedEvent, consumer, cancellationToken);
            }

            async void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, SubscriptionDropReason subscriptionDropReason,
                                           Exception e)
            {
                await this.SubscriptionDropped(eventStoreCatchUpSubscription, subscriptionDropReason, e);

                if (catchupSubscriptionBuilder.SubscriptionDropped != null)
                {
                    catchupSubscriptionBuilder.SubscriptionDropped();
                }
            }

            //TODO: Likely we store this at class or builder
            EventStoreStreamCatchUpSubscription e = this.EventStoreConnection.SubscribeToStreamFrom(catchupSubscriptionBuilder.StreamName,
                                                                                                    catchupSubscriptionBuilder.LastCheckpoint,
                                                                                                    catchupSubscriptionBuilder.CatchUpSubscriptionSettings,
                                                                                                    AppearedFromCatchupSubscription,
                                                                                                    this.LiveProcessingStarted,
                                                                                                    SubscriptionDropped);
        }

        private async Task SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
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
}