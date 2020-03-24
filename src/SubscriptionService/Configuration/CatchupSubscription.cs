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
        /// <param name="endPointUri">The end point URI.</param>
        private CatchupSubscription(String subscriptionName,
                                    String streamName,
                                    Int64? lastCheckpoint,
                                    Uri endPointUri)
        {
            this.SubscriptionName = subscriptionName;
            this.StreamName = streamName;
            this.LastCheckpoint = lastCheckpoint;
            this.EndPointUri = endPointUri;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the end point URI.
        /// </summary>
        /// <value>
        /// The end point URI.
        /// </value>
        public Uri EndPointUri { get; }

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
        /// <param name="endPointUri">The end point URI.</param>
        /// <returns></returns>
        public static CatchupSubscription Create(String subscriptionName,
                                                 String streamName,
                                                 Uri endPointUri)
        {
            return new CatchupSubscription(subscriptionName, streamName, null, endPointUri);
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="subscriptionName">Name of the subscription.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        /// <param name="endPointUri">The end point URI.</param>
        /// <returns></returns>
        public static CatchupSubscription Create(String subscriptionName,
                                                 String streamName,
                                                 Int64 lastCheckpoint,
                                                 Uri endPointUri)
        {
            return new CatchupSubscription(subscriptionName, streamName, lastCheckpoint, endPointUri);
        }

        #endregion
    }
}