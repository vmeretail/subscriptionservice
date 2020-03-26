namespace SubscriptionService.Domain
{
    using System;
    using System.Net.Http;
    using System.Text;
    using Builders;

    /// <summary>
    /// </summary>
    internal class Consumer
    {
        #region Fields

        /// <summary>
        /// The consumer builder
        /// </summary>
        private readonly ConsumerBuilder ConsumerBuilder;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="consumerBuilder">The consumer builder.</param>
        internal Consumer(ConsumerBuilder consumerBuilder)
        {
            this.ConsumerBuilder = consumerBuilder;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public HttpContent GetContent(String data)
        {
            //This should make to easier to change the content we send to a consumer.
            StringContent stringContent = new StringContent(data, Encoding.UTF8, "application/json");

            return stringContent;
        }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return this.ConsumerBuilder.HttpClient;
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <returns></returns>
        public Uri GetUri()
        {
            return this.ConsumerBuilder.EndPointUri;
        }

        #endregion
    }
}