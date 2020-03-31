namespace ConsoleApplication.CatchupSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    public class LastCheckpointUpdated
    {
        #region Methods

        public static async Task PersistLastCheckpoint(IEventStoreConnection eventStoreConnection)
        {
            //Add the AddLastCheckPointChanged handler, and set the frequency of how often you want this to broadcast
            Int32 checkPointBroadcastFrequency = 500;
            CheckpointRepository checkpointRepository = new CheckpointRepository();

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddLastCheckPointChanged((subscriptionName,
                                                                                             lastCheckpoint) =>
                                                                                            {
                                                                                                //You need to implement this if there is a requirement
                                                                                                //to resume from a specific checkpoint.
                                                                                                checkpointRepository.UpdateCheckpointForCatchup(subscriptionName,
                                                                                                                                                lastCheckpoint);
                                                                                            },
                                                                                            checkPointBroadcastFrequency)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);
        }

        public static async Task SetLastCheckpoint(IEventStoreConnection eventStoreConnection)
        {
            CheckpointRepository checkpointRepository = new CheckpointRepository();

            //retrieve your lastCheckpoint
            Int64 lastCheckpoint = checkpointRepository.GetCheckpointForCatchup("Test Catchup 1");

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                                  .SetName("Test Catchup 1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .SetLastCheckpoint(lastCheckpoint)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);
        }

        #endregion

        #region Others

        private class CheckpointRepository
        {
            #region Methods

            public Int64 GetCheckpointForCatchup(String streamName)
            {
                //You would return your last checkpoint value for the stream
                return 500;
            }

            public void UpdateCheckpointForCatchup(String streamName,
                                                   Int64 checkpint)
            {
                //persist
            }

            #endregion
        }

        #endregion
    }
}