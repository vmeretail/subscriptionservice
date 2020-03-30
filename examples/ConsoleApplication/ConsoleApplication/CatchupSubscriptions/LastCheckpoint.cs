namespace ConsoleApplication.CatchupSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    /// <summary>
    /// 
    /// </summary>
    public class LastCheckpoint
    {
        #region Methods

        /// <summary>
        /// Catchups the test with last checkpoint.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        public static async Task SetLastCheckpoint(IEventStoreConnection eventStoreConnection)
        {
            Int64 lastCheckpoint = 5;

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                                  .SetName("Test Catchup 1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .SetLastCheckpoint(lastCheckpoint).
                                                                  Build();

            await subscription.Start(CancellationToken.None);
        }

        #endregion
    }
}