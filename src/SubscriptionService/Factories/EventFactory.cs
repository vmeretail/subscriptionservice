namespace SubscriptionService.Factories
{
    using System;
    using System.Dynamic;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="IEventFactory" />
    internal class EventFactory : IEventFactory
    {
        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="EventFactory" /> class from being created.
        /// </summary>
        private EventFactory()
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="persistedEvent">The persisted event.</param>
        /// <returns></returns>
        public String ConvertFrom(PersistedEvent persistedEvent)
        {
            //Build a standard WebRequest
            String serialisedData = Encoding.Default.GetString(persistedEvent.Data, 0, persistedEvent.Data.Length);

            return serialisedData;
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns></returns>
        public static IEventFactory Create()
        {
            return new EventFactory();
        }

        #endregion
    }
}