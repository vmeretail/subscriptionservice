namespace SubscriptionService.Domain
{
    using System;
    using System.Net.Http;

    /// <summary>
    /// </summary>
    internal class Subscription
    {
        #region Properties

        /// <summary>
        /// Gets the end point Uri.
        /// </summary>
        /// <value>
        /// The end point Uri.
        /// </value>
        public Uri EndPointUri { get; set; }

        /// <summary>
        /// Gets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public String GroupName { get; set; }

        /// <summary>
        /// Gets or sets the HTTP client.
        /// </summary>
        /// <value>
        /// The HTTP client.
        /// </value>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; set; }

        /// <summary>
        /// Gets the stream start position.
        /// </summary>
        /// <value>
        /// The stream start position.
        /// </value>
        public Int32 StreamStartPosition { get; set; }

        /// <summary>
        /// Gets the maximum retry count.
        /// </summary>
        /// <value>
        /// The maximum retry count.
        /// </value>
        public Int32 MaxRetryCount { get; set; }

        /// <summary>
        /// Gets the number of concurrent messages.
        /// </summary>
        /// <value>
        /// The number of concurrent messages.
        /// </value>
        public Int32 NumberOfConcurrentMessages { get; set; }

        #endregion
    }
}