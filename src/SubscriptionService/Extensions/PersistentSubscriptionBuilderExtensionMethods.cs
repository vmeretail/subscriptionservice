namespace SubscriptionService.Extensions
{
    using System;
    using System.Net.Http;
    using Builders;
    using EventStore.ClientAPI;
    using Factories;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    public static class PersistentSubscriptionBuilderExtensionMethods
    {
        #region Methods

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder AddLogger(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                              ILogger logger)
        {
            return persistentSubscriptionBuilder.AddLogger(logger) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Delivers to.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder DeliverTo(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                              Uri uri)
        {
            return persistentSubscriptionBuilder.DeliverTo(uri) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Logs the events.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder LogAllEvents(this PersistentSubscriptionBuilder persistentSubscriptionBuilder)
        {
            return persistentSubscriptionBuilder.LogAllEvents() as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Logs the events on error.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder LogEventsOnError(this PersistentSubscriptionBuilder persistentSubscriptionBuilder)
        {
            return persistentSubscriptionBuilder.LogEventsOnError() as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Uses the connection.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder UseConnection(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                                  IEventStoreConnection eventStoreConnection)
        {
            return persistentSubscriptionBuilder.UseConnection(eventStoreConnection) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder UseEventFactory(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                                    IEventFactory eventFactory)
        {
            return persistentSubscriptionBuilder.UseEventFactory(eventFactory) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Uses the HTTP interceptor.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="httpRequestInterceptor">The HTTP request interceptor.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder UseHttpInterceptor(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                                       Action<HttpRequestMessage> httpRequestInterceptor)
        {
            return persistentSubscriptionBuilder.UseHttpInterceptor(httpRequestInterceptor) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder WithPassword(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                                 String password)
        {
            return persistentSubscriptionBuilder.WithPassword(password) as PersistentSubscriptionBuilder;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="persistentSubscriptionBuilder">The persistent subscription builder.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder WithUserName(this PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                                                 String username)
        {
            return persistentSubscriptionBuilder.WithUserName(username) as PersistentSubscriptionBuilder;
            ;
        }

        #endregion
    }
}