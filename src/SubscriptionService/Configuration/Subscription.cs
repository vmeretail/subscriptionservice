namespace SubscriptionService.Configuration
{
    using System;
    using EventStore.ClientAPI;

    /// <summary>
    /// </summary>
    public class Subscription
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="endPointUri">The end point URI.</param>
        /// <param name="numberOfConcurrentMessages">The number of concurrent messages.</param>
        /// <param name="maxRetryCount">The maximum retry count.</param>
        /// <param name="streamStartPosition">The stream start position.</param>
        /// <exception cref="ArgumentException">
        /// Value cannot be null or empty - streamName
        /// or
        /// Value cannot be null or empty - groupName
        /// or
        /// Value cannot be null - endPointUri
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// numberOfConcurrentMessages - Value cannot be less than 0
        /// or
        /// maxRetryCount - Value cannot be less than 0
        /// or
        /// streamStartPosition - Value cannot be less than 0
        /// </exception>
        private Subscription(String streamName,
                             String groupName,
                             Uri endPointUri,
                             Int32 numberOfConcurrentMessages,
                             Int32 maxRetryCount,
                             Int32 streamStartPosition)
        {
            if (String.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(streamName));
            }

            if (String.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(groupName));
            }

            if (endPointUri == null)
            {
                throw new ArgumentException("Value cannot be null", nameof(endPointUri));
            }

            if (numberOfConcurrentMessages < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfConcurrentMessages), "Value cannot be less than 0");
            }

            if (maxRetryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "Value cannot be less than 0");
            }

            if (streamStartPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(streamStartPosition), "Value cannot be less than 0");
            }

            this.StreamName = streamName;
            this.GroupName = groupName;
            this.EndPointUri = endPointUri;
            this.NumberOfConcurrentMessages = numberOfConcurrentMessages;
            this.MaxRetryCount = maxRetryCount;
            this.StreamStartPosition = streamStartPosition;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the end point Uri.
        /// </summary>
        /// <value>
        /// The end point Uri.
        /// </value>
        public Uri EndPointUri { get; }

        /// <summary>
        /// Gets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public String GroupName { get; }

        /// <summary>
        /// Gets the maximum retry count.
        /// </summary>
        /// <value>
        /// The maximum retry count.
        /// </value>
        public Int32 MaxRetryCount { get; }

        /// <summary>
        /// Gets the number of concurrent messages.
        /// </summary>
        /// <value>
        /// The number of concurrent messages.
        /// </value>
        public Int32 NumberOfConcurrentMessages { get; }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; }

        /// <summary>
        /// Gets the stream start position.
        /// </summary>
        /// <value>
        /// The stream start position.
        /// </value>
        public Int32 StreamStartPosition { get; }

        #endregion

        /// <summary>
        /// The maximum retry count
        /// </summary>
        public const Int32 DefaultMaxRetryCount = 10;

        /// <summary>
        /// The number of concurrent messages
        /// </summary>
        public const Int32 DefaultNumberOfConcurrentMessages = EventStorePersistentSubscriptionBase.DefaultBufferSize;

        /// <summary>
        /// The default stream start position
        /// </summary>
        public const Int32 DefaultStreamStartPosition = 0;

        #region Methods

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="endPointUrl">The end point URL.</param>
        /// <param name="numberOfConcurrentMessages">The number of concurrent messages.</param>
        /// <param name="maxRetryCount">The maximum retry count.</param>
        /// <param name="streamStartPosition">The stream start position.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Value cannot be null or empty - endPointUrl</exception>
        public static Subscription Create(String streamName,
                                          String groupName,
                                          String endPointUrl,
                                          Int32 numberOfConcurrentMessages = DefaultNumberOfConcurrentMessages,
                                          Int32 maxRetryCount = DefaultMaxRetryCount,
                                          Int32 streamStartPosition = DefaultStreamStartPosition)
        {
            if (String.IsNullOrWhiteSpace(endPointUrl))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(endPointUrl));
            }

            return new Subscription(streamName, groupName, new Uri(endPointUrl), numberOfConcurrentMessages, maxRetryCount, streamStartPosition);
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="endPointUri">The end point URI.</param>
        /// <param name="numberOfConcurrentMessages">The number of concurrent messages.</param>
        /// <param name="maxRetryCount">The maximum retry count.</param>
        /// <param name="streamStartPosition">The stream start position.</param>
        /// <returns></returns>
        public static Subscription Create(String streamName,
                                          String groupName,
                                          Uri endPointUri,
                                          Int32 numberOfConcurrentMessages = DefaultNumberOfConcurrentMessages,
                                          Int32 maxRetryCount = DefaultMaxRetryCount,
                                          Int32 streamStartPosition = DefaultStreamStartPosition)
        {
            return new Subscription(streamName, groupName, endPointUri, numberOfConcurrentMessages, maxRetryCount, streamStartPosition);
        }

        #endregion
    }
}