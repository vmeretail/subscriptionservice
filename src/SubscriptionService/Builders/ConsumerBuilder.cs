namespace SubscriptionService.Builders
{
    using System;
    using System.Net.Http;
    using Domain;

    /// <summary>
    /// </summary>
    internal class ConsumerBuilder
    {
        #region Fields

        /// <summary>
        /// The end point URI
        /// </summary>
        internal Uri EndPointUri;

        /// <summary>
        /// The HTTP client
        /// </summary>
        internal HttpClient HttpClient;

        #endregion

        #region Constructors

        #endregion

        #region Methods

        /// <summary>
        /// Adds the endpoint URI.
        /// </summary>
        /// <param name="endPointUri">The end point URI.</param>
        /// <returns></returns>
        public ConsumerBuilder AddEndpointUri(Uri endPointUri)
        {
            this.EndPointUri = endPointUri;

            this.HttpClient = new HttpClient
                              {
                                  BaseAddress = endPointUri
                              };

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        public Consumer Build()
        {
            return new Consumer(this);
        }

        #endregion
    }
}