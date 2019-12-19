namespace SubscriptionService.Configuration
{
    using System;

    /// <summary>
    /// 
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
        private Subscription(String streamName,
                             String groupName,
                             Uri endPointUri)
        {
            if (endPointUri == null)
            {
                throw new ArgumentException("Value cannot be null", nameof(endPointUri));
            }

            this.StreamName = streamName;
            this.GroupName = groupName;
            this.EndPointUri = endPointUri;
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
        /// Gets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="endPointUrl">The end point URL.</param>
        /// <returns></returns>
        public static Subscription Create(String streamName,
                                          String groupName,
                                          String endPointUrl)
        {
            if (String.IsNullOrWhiteSpace(endPointUrl))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(endPointUrl));
            }

            return new Subscription(streamName, groupName, new Uri(endPointUrl));
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="endPointUri">The end point URI.</param>
        /// <returns></returns>
        public static Subscription Create(String streamName,
                                          String groupName,
                                          Uri endPointUri)
        {
            return new Subscription(streamName, groupName, endPointUri);
        }

        #endregion
    }
}