namespace SubscriptionService
{
    using System;
    using EventStore.ClientAPI;
    using Factories;
    using Microsoft.Extensions.Logging.Abstractions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// 
    /// </summary>
    public sealed class SubscriptionServiceBuilder
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
        /// The username
        /// </summary>
        internal String Username;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionServiceBuilder"/> class.
        /// </summary>
        public SubscriptionServiceBuilder()
        {
            this.Logger = NullLogger.Instance;
            this.EventFactory = Factories.EventFactory.Create();
            this.Username = "admin";
            this.Password = "changeit";
        }

        #region Methods

        /// <summary>
        /// Adds the logger.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder AddLogger(ILogger logger)
        {
            this.Logger = logger;

            return this;
        }

        /// <summary>
        /// Builds this instance.
        /// </summary>
        /// <returns></returns>
        public ISubscriptionService Build()
        {
            return new SubscriptionService(this);
        }

        /// <summary>
        /// Logs the events.
        /// </summary>
        /// <returns></returns>
        public SubscriptionServiceBuilder LogAllEvents()
        {
            this.LogEventsSettings |= LogEvents.All;

            return this;
        }

        /// <summary>
        /// Logs the events on error.
        /// </summary>
        /// <returns></returns>
        public SubscriptionServiceBuilder LogEventsOnError()
        {
            this.LogEventsSettings |= LogEvents.Errors;

            return this;
        }

        /// <summary>
        /// Uses the connection.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder UseConnection(IEventStoreConnection eventStoreConnection)
        {
            this.EventStoreConnection = eventStoreConnection;

            return this;
        }

        /// <summary>
        /// Uses the event factory.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder UseEventFactory(IEventFactory eventFactory)
        {
            this.EventFactory = eventFactory;

            return this;
        }

        /// <summary>
        /// Withes the password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder WithPassword(String password)
        {
            this.Password = password;

            return this;
        }

        /// <summary>
        /// Withes the name of the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns></returns>
        public SubscriptionServiceBuilder WithUserName(String username)
        {
            this.Username = username;

            return this;
        }

        #endregion

        #region Others

        /// <summary>
        /// 
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