namespace SubscriptionService.Builders
{
    using System;
    using EventStore.ClientAPI;

    /// <summary>
    /// </summary>
    /// <seealso cref="SubscriptionService.Builders.SubscriptionBuilder" />
    /// <seealso cref="SubscriptionService.SubscriptionBuilder" />
    public class CatchupSubscriptionBuilder : SubscriptionBuilder
    {
        #region Fields

        /// <summary>
        /// The catch up subscription settings
        /// </summary>
        internal CatchUpSubscriptionSettings CatchUpSubscriptionSettings;

        /// <summary>
        /// The event appeared
        /// </summary>
        internal Action<EventStoreCatchUpSubscription, ResolvedEvent> EventAppeared;

        /// <summary>
        /// The last checkpoint
        /// </summary>
        internal Int64? LastCheckpoint;

        /// <summary>
        /// The live processing started
        /// </summary>
        internal Action<EventStoreCatchUpSubscription> LiveProcessingStarted;

        /// <summary>
        /// The stream name
        /// </summary>
        internal String StreamName;

        /// <summary>
        /// The subscription dropped
        /// </summary>
        internal Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> SubscriptionDropped;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CatchupSubscriptionBuilder" /> class.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        private CatchupSubscriptionBuilder(String streamName)
        {
            this.StreamName = streamName;
            this.CatchUpSubscriptionSettings = CatchUpSubscriptionSettings.Default;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the event appeared handler.
        /// </summary>
        /// <param name="eventAppeared">The event appeared.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder AddEventAppearedHandler(Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared)
        {
            this.EventAppeared = eventAppeared;

            return this;
        }

        /// <summary>
        /// Adds the live processing started handler.
        /// </summary>
        /// <param name="liveProcessingStarted">The live processing started.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder AddLiveProcessingStartedHandler(Action<EventStoreCatchUpSubscription> liveProcessingStarted)
        {
            this.LiveProcessingStarted = liveProcessingStarted;
            return this;
        }

        /// <summary>
        /// Adds the subscription dropped handler.
        /// </summary>
        /// <param name="subscriptionDropped">The subscription dropped.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder AddSubscriptionDroppedHandler(Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            //This will override the default wiring.
            this.SubscriptionDropped = subscriptionDropped;

            return this;
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder Create(String streamName)
        {
            return new CatchupSubscriptionBuilder(streamName);
        }

        /// <summary>
        /// Sets the last checkpoint.
        /// </summary>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder SetLastCheckpoint(Int64 lastCheckpoint)
        {
            this.LastCheckpoint = lastCheckpoint;

            return this;
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        /// <param name="subscriptionName">Name of the subscription.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder SetName(String subscriptionName)
        {
            this.CatchUpSubscriptionSettings = new CatchUpSubscriptionSettings(this.CatchUpSubscriptionSettings.MaxLiveQueueSize,
                                                                               this.CatchUpSubscriptionSettings.ReadBatchSize,
                                                                               this.CatchUpSubscriptionSettings.VerboseLogging,
                                                                               this.CatchUpSubscriptionSettings.ResolveLinkTos,
                                                                               subscriptionName);

            return this;
        }

        /// <summary>
        /// Withes the catch up subscription settings.
        /// </summary>
        /// <param name="catchUpSubscriptionSettings">The catch up subscription settings.</param>
        /// <returns></returns>
        public CatchupSubscriptionBuilder WithCatchUpSubscriptionSettings(CatchUpSubscriptionSettings catchUpSubscriptionSettings)
        {
            this.CatchUpSubscriptionSettings = catchUpSubscriptionSettings;

            return this;
        }

        #endregion
    }
}