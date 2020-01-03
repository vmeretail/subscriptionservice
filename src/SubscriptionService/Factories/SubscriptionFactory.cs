namespace SubscriptionService.Factories
{
    using System;
    using System.Net.Http;
    using Domain;

    /// <summary>
    /// </summary>
    internal class SubscriptionFactory
    {
        #region Methods

        /// <summary>
        /// Creates from.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">subscription - Cannot be null</exception>
        internal Subscription CreateFrom(Configuration.Subscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription), "Cannot be null");
            }

            Subscription subscriptionDomain = new Subscription
                                              {
                                                  GroupName = subscription.GroupName,
                                                  StreamName = subscription.StreamName,
                                                  EndPointUri = subscription.EndPointUri,
                                                  HttpClient = new HttpClient
                                                               {
                                                                   BaseAddress = subscription.EndPointUri
                                                               },
                                                  MaxRetryCount = subscription.MaxRetryCount,
                                                  NumberOfConcurrentMessages = subscription.NumberOfConcurrentMessages,
                                                  StreamStartPosition = subscription.StreamStartPosition
                                              };

            return subscriptionDomain;
        }

        #endregion
    }
}