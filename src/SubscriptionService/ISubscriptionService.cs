namespace SubscriptionService
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;

    /// <summary>
    /// </summary>
    public interface ISubscriptionService
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        Boolean IsStarted { get; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [on event appeared].
        /// </summary>
        event EventHandler<HttpRequestMessage> OnEventAppeared;

        #endregion

        #region Methods

        /// <summary>
        /// Start with no config
        /// </summary>
        /// <param name="subscriptions">The subscriptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task Start(List<Subscription> subscriptions,
                   CancellationToken cancellationToken);

        Task StartCatchupSubscriptions(List<CatchupSubscription> catchupSubscriptions,
                                       CancellationToken cancellationToken);

        /// <summary>
        /// Stop the service
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task Stop(CancellationToken cancellationToken);

        /// <summary>
        /// Removes the subscription.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task RemoveSubscription(String groupName, String streamName, CancellationToken cancellationToken);

        #endregion
    }
}