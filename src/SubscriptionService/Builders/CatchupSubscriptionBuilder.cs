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
        /// The last checkpoint
        /// </summary>
        internal Int64? LastCheckpoint;

        /// <summary>
        /// The stream name
        /// </summary>
        internal String StreamName;


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

        //TODO: Add Subscription Dropped event handler
        //TODO: Add Live processing event handler

        public CatchupSubscriptionBuilder AddSubscriptionDroppedHandler(Action subscriptionDropped)
        {
            //TODO: This will override the default wiring
            this.SubscriptionDropped = subscriptionDropped;

            return this;
        }



        internal Action SubscriptionDropped;

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