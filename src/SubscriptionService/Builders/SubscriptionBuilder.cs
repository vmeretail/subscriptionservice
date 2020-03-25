namespace SubscriptionService.Builders
{
    using System;
    using System.Net.Http;
    using EventStore.ClientAPI;
    using Factories;
    using Microsoft.Extensions.Logging.Abstractions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    public abstract class SubscriptionBuilder
    {
        #region Fields

        /// <summary>
        /// The event factory
        /// </summary>
        internal IEventFactory EventFactory;

        /// <summary>
        /// The event store connection
        /// </summary>
        internal IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The HTTP request interceptor
        /// </summary>
        internal Action<HttpRequestMessage> HttpRequestInterceptor;

        /// <summary>
        /// The log events settings
        /// </summary>
        internal LogEvents LogEventsSettings;

        /// <summary>
        /// The logger
        /// </summary>
        internal ILogger Logger;

        /// <summary>
        /// The password
        /// </summary>
        internal String Password;

        /// <summary>
        /// The URI
        /// </summary>
        internal Uri Uri;

        /// <summary>
        /// The username
        /// </summary>
        internal String Username;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionBuilder" /> class.
        /// </summary>
        internal SubscriptionBuilder()
        {
            this.Logger = NullLogger.Instance;
            this.EventFactory = Factories.EventFactory.Create();
            this.Username = "admin";
            this.Password = "changeit";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        public virtual Subscription Build()
        {
            return new Subscription(this);
        }

        /// <summary>
        /// Logs the events.
        /// </summary>
        /// <returns></returns>
        public SubscriptionBuilder LogAllEvents()
        {
            this.LogEventsSettings |= LogEvents.All;

            return this;
        }

        /// <summary>
        /// Logs the events on error.
        /// </summary>
        /// <returns></returns>
        public SubscriptionBuilder LogEventsOnError()
        {
            this.LogEventsSettings |= LogEvents.Errors;

            return this;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public SubscriptionBuilder UseEventFactory(IEventFactory eventFactory)
        {
            this.EventFactory = eventFactory;

            return this;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public SubscriptionBuilder WithPassword(String password)
        {
            this.Password = password;

            return this;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public SubscriptionBuilder WithUserName(String username)
        {
            this.Username = username;

            return this;
        }

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        internal SubscriptionBuilder AddLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }

        /// <summary>
        /// Delivers to.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        internal SubscriptionBuilder DeliverTo(Uri uri)
        {
            //TODO: Will we allow multiple endpoints? - raise an issue
            this.Uri = uri;

            return this;
        }

        /// <summary>
        /// Uses the connection.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <returns></returns>
        internal SubscriptionBuilder UseConnection(IEventStoreConnection eventStoreConnection)
        {
            this.EventStoreConnection = eventStoreConnection;

            return this;
        }

        /// <summary>
        /// Uses the HTTP interceptor.
        /// </summary>
        /// <param name="httpRequestInterceptor">The HTTP request interceptor.</param>
        /// <returns></returns>
        internal SubscriptionBuilder UseHttpInterceptor(Action<HttpRequestMessage> httpRequestInterceptor)
        {
            this.HttpRequestInterceptor = httpRequestInterceptor;
            return this;
        }

        #endregion

        #region Others

        /// <summary>
        /// </summary>
        [Flags]
        internal enum LogEvents
        {
            /// <summary>
            /// The none
            /// </summary>
            None = 0,

            /// <summary>
            /// The errors
            /// </summary>
            Errors = 1,

            /// <summary>
            /// All
            /// </summary>
            All = 2
        }

        #endregion
    }
}