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
    public static class CatchupSubscriptionBuilderExtensionMethods
    {
        #region Methods

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder AddLogger(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                           ILogger logger)
        {
            return catchupSubscriptionBuilder.AddLogger(logger) as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Delivers to.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder DeliverTo(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                           Uri uri)
        {
            return catchupSubscriptionBuilder.DeliverTo(uri) as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Logs the events.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder LogAllEvents(this CatchupSubscriptionBuilder catchupSubscriptionBuilder)
        {
            return catchupSubscriptionBuilder.LogAllEvents() as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Logs the events on error.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder LogEventsOnError(this CatchupSubscriptionBuilder catchupSubscriptionBuilder)
        {
            return catchupSubscriptionBuilder.LogEventsOnError() as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Uses the connection.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder UseConnection(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                               IEventStoreConnection eventStoreConnection)
        {
            return catchupSubscriptionBuilder.UseConnection(eventStoreConnection) as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder UseEventFactory(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                                 IEventFactory eventFactory)
        {
            return catchupSubscriptionBuilder.UseEventFactory(eventFactory) as CatchupSubscriptionBuilder;
        }

        public static CatchupSubscriptionBuilder UseHttpInterceptor(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                                    Action<HttpRequestMessage> httpRequestInterceptor)
        {
            return catchupSubscriptionBuilder.UseHttpInterceptor(httpRequestInterceptor) as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder WithPassword(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                              String password)
        {
            return catchupSubscriptionBuilder.WithPassword(password) as CatchupSubscriptionBuilder;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="catchupSubscriptionBuilder">The catchup subscription builder.</param>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public static CatchupSubscriptionBuilder WithUserName(this CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                                              String username)
        {
            return catchupSubscriptionBuilder.WithUserName(username) as CatchupSubscriptionBuilder;
            ;
        }

        #endregion
    }
}