namespace SubscriptionService.Factories
{
    using System;
    using EventStore.ClientAPI;

    /// <summary>
    /// </summary>
    public interface IEventFactory
    {
        #region Methods

        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="recordedEvent">The recorded event.</param>
        /// <returns></returns>
        String ConvertFrom(RecordedEvent recordedEvent);

        #endregion
    }
}