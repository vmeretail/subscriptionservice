namespace SubscriptionService.Configuration
{
    using System;

    /// <summary>
    /// </summary>
    public class CatchupSubscription
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CatchupSubscription" /> class.
        /// </summary>
        /// <param name="subscriptionName">Name of the subscription.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        private CatchupSubscription(String subscriptionName,
                                    String streamName,
                                    Int64? lastCheckpoint = null)
        {
            this.SubscriptionName = subscriptionName;
            this.StreamName = streamName;
            this.LastCheckpoint = lastCheckpoint;
        }

        #endregion

        #region Properties

        //CatchUpSubscriptionSettings

        /// <summary>
        /// Gets the last checkpoint.
        /// </summary>
        /// <value>
        /// The last checkpoint.
        /// </value>
        public Int64? LastCheckpoint { get; }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; }

        /// <summary>
        /// Gets the name of the subscription.
        /// </summary>
        /// <value>
        /// The name of the subscription.
        /// </value>
        public String SubscriptionName { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="subscriptionName">Name of the subscription.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <returns></returns>
        public static CatchupSubscription Create(String subscriptionName,
                                                 String streamName)
        {
            return new CatchupSubscription(subscriptionName, streamName);
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="subscriptionName">Name of the subscription.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        /// <returns></returns>
        public static CatchupSubscription Create(String subscriptionName,
                                                 String streamName,
                                                 Int64 lastCheckpoint)
        {
            return new CatchupSubscription(subscriptionName, streamName, lastCheckpoint);
        }

        #endregion
    }
}