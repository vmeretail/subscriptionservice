namespace SubscriptionService.Factories
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public interface IEventFactory
    {
        #region Methods

        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="persistedEvent">The persisted event.</param>
        /// <returns></returns>
        String ConvertFrom(PersistedEvent persistedEvent);

        #endregion
    }
}