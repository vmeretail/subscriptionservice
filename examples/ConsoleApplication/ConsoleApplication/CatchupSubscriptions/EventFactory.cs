namespace ConsoleApplication.CatchupSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Newtonsoft.Json;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;
    using SubscriptionService.Factories;

    public class EventFactory
    {
        #region Methods

        /// <summary>
        /// Catchups the test with last checkpoint.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        public static async Task CustomEventFactoryExampleTask(IEventStoreConnection eventStoreConnection)
        {
            IEventFactory eventFactory = new CustomEventFactory();

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .UseEventFactory(eventFactory)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);
        }

        #endregion

        #region Others

        private class CustomEventFactory : IEventFactory
        {
            #region Methods

            public String ConvertFrom(PersistedEvent persistedEvent)
            {
                var customEvent = new
                                  {
                                      eventId = persistedEvent.EventId,
                                      payload = persistedEvent.Data
                                  };

                return JsonConvert.SerializeObject(customEvent);
            }

            #endregion
        }

        #endregion
    }
}