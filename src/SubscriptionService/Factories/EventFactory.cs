namespace SubscriptionService.Factories
{
    using System;
    using System.Text;
    using EventStore.ClientAPI;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SubscriptionService.IEventFactory" />
    internal class EventFactory : IEventFactory
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="EventFactory"/> class from being created.
        /// </summary>
        private EventFactory()
        {
            
        }

        #region Methods

        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="recordedEvent">The recorded event.</param>
        /// <returns></returns>
        public String ConvertFrom(RecordedEvent recordedEvent)
        {
            //Build a standard WebRequest
            String serialisedData = Encoding.Default.GetString(recordedEvent.Data, 0, recordedEvent.Data.Length);

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