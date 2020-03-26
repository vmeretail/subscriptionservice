namespace SubscriptionService.Builders
{
    using System;
    using EventStore.ClientAPI;

    /// <summary>
    /// </summary>
    /// <seealso cref="SubscriptionService.Builders.SubscriptionBuilder" />
    /// <seealso cref="SubscriptionBuilder" />
    public class PersistentSubscriptionBuilder : SubscriptionBuilder
    {
        #region Fields

        /// <summary>
        /// The automatic ack
        /// </summary>
        internal Boolean AutoAck;

        /// <summary>
        /// The event appeared
        /// </summary>
        internal Action<EventStorePersistentSubscriptionBase, ResolvedEvent> EventAppeared;

        /// <summary>
        /// The group name
        /// </summary>
        internal String GroupName;

        /// <summary>
        /// The in flight limit
        /// </summary>
        internal Int32 InFlightLimit;

        /// <summary>
        /// The persistent subscription settings
        /// </summary>
        internal PersistentSubscriptionSettings PersistentSubscriptionSettings;

        /// <summary>
        /// The stream name
        /// </summary>
        internal String StreamName;

        /// <summary>
        /// The subscription dropped
        /// </summary>
        internal Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> SubscriptionDropped;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscriptionBuilder" /> class.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        private PersistentSubscriptionBuilder(String streamName,
                                              String groupName)
        {
            this.StreamName = streamName;
            this.GroupName = groupName;

            this.PersistentSubscriptionSettings = PersistentSubscriptionSettings
                                                  .Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10)).StartFromBeginning();

            this.AutoAck = false;
            this.InFlightLimit = 10;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the event appeared handler.
        /// </summary>
        /// <param name="eventAppeared">The event appeared.</param>
        /// <returns></returns>
        public PersistentSubscriptionBuilder AddEventAppearedHandler(Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared)
        {
            this.EventAppeared = eventAppeared;

            return this;
        }

        /// <summary>
        /// Adds the subscription dropped handler.
        /// </summary>
        /// <param name="subscriptionDropped">The subscription dropped.</param>
        /// <returns></returns>
        public PersistentSubscriptionBuilder AddSubscriptionDroppedHandler(
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            //This will override the default wiring.
            this.SubscriptionDropped = subscriptionDropped;

            return this;
        }

        /// <summary>
        /// Automatics the ack events.
        /// </summary>
        /// <returns></returns>
        public PersistentSubscriptionBuilder AutoAckEvents()
        {
            this.AutoAck = true;

            return this;
        }

        /// <summary>
        /// Creates the specified stream name.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        public static PersistentSubscriptionBuilder Create(String streamName,
                                                           String groupName)
        {
            return new PersistentSubscriptionBuilder(streamName, groupName);
        }

        /// <summary>
        /// Manuallies the ack events.
        /// </summary>
        /// <returns></returns>
        public PersistentSubscriptionBuilder ManuallyAckEvents()
        {
            this.AutoAck = false;

            return this;
        }

        /// <summary>
        /// Sets the in flight limit.
        /// </summary>
        /// <param name="inflightLimit">The inflight limit.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">inflightLimit - inflightLimit must be greater than zero.</exception>
        public PersistentSubscriptionBuilder SetInFlightLimit(Int32 inflightLimit)
        {
            //We should guard against <= 0
            if (inflightLimit <= 0)
            {
                //NOTE: Zero being passed stops any event delivery, and <0 throws an Exception.
                throw new ArgumentOutOfRangeException(nameof(inflightLimit), "inflightLimit must be greater than zero.");
            }

            this.InFlightLimit = inflightLimit;

            return this;
        }

        /// <summary>
        /// Withes the persistent subscription settings.
        /// </summary>
        /// <param name="persistentSubscriptionSettings">The persistent subscription settings.</param>
        /// <returns></returns>
        public PersistentSubscriptionBuilder WithPersistentSubscriptionSettings(PersistentSubscriptionSettings persistentSubscriptionSettings)
        {
            this.PersistentSubscriptionSettings = persistentSubscriptionSettings;

            return this;
        }

        #endregion
    }
}