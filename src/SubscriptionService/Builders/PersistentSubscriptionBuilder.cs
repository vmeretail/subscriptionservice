namespace SubscriptionService.Builders
{
    using System;

    /// <summary>
    /// </summary>
    /// <seealso cref="SubscriptionService.Builders.SubscriptionBuilder" />
    public class PersistentSubscriptionBuilder : SubscriptionBuilder
    {
        #region Fields

        /// <summary>
        /// The group name
        /// </summary>
        internal String GroupName;

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
        }

        #endregion

        #region Methods

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

        #endregion
    }
}