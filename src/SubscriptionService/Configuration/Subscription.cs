namespace SubscriptionService.Configuration
{
    using System;

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
        /// <param name="endPointUrl">The end point URL.</param>
        private Subscription(String streamName,
                             String groupName,
                             String endPointUrl)
        {
            if (String.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(streamName));
            }

            if (String.IsNullOrWhiteSpace(groupName))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(groupName));
            }

            this.StreamName = streamName;
            this.GroupName = groupName;
            this.EndPointUrl = endPointUrl;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the end point URL.
        /// </summary>
        /// <value>
        /// The end point URL.
        /// </value>
        public String EndPointUrl { get; }

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
            return new Subscription(streamName, groupName, endPointUrl);
        }

        #endregion
    }
}