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


        public PersistentSubscriptionBuilder AddEventAppearedHandler(Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared)
        {
            this.EventAppeared = eventAppeared;

            return this;
        }

        internal Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> SubscriptionDropped;

        internal Action<EventStorePersistentSubscriptionBase, ResolvedEvent> EventAppeared;


        /// <summary>
        /// Adds the subscription dropped handler.
        /// </summary>
        /// <param name="subscriptionDropped">The subscription dropped.</param>
        /// <returns></returns>
        public PersistentSubscriptionBuilder AddSubscriptionDroppedHandler(Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped)
        {
            //This will override the default wiring.
            this.SubscriptionDropped = subscriptionDropped;

            return this;
        }

        #region Fields

        /// <summary>
        /// The automatic ack
        /// </summary>
        internal Boolean AutoAck;

        /// <summary>
        /// The group name
        /// </summary>
        internal String GroupName;

        /// <summary>
        /// The persistent subscription settings
        /// </summary>
        internal PersistentSubscriptionSettings PersistentSubscriptionSettings;

        /// <summary>
        /// The stream name
        /// </summary>
        internal String StreamName;

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
        }

        #endregion

        #region Methods

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