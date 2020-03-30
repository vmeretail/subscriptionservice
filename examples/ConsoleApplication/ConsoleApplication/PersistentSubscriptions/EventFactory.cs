namespace ConsoleApplication.PersistentSubscriptions
{
    using System;
    using System.Dynamic;
    using System.Text;
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
            IEventFactory eventFactory = new WorkerEventFactory();

            Subscription subscription = PersistentSubscriptionBuilder.Create("$ce-CatchupTest", "Test Group")
                                                                     .UseConnection(eventStoreConnection)
                                                                     .UseEventFactory(eventFactory)
                                                                     .Build();

            await subscription.Start(CancellationToken.None);
        }

        #endregion

        #region Others

        internal class WorkerEventFactory : IEventFactory
        {
            public String ConvertFrom(PersistedEvent persistedEvent)
            {
                String json = Encoding.Default.GetString(persistedEvent.Data);
                dynamic expandoObject = new ExpandoObject();
                dynamic temp = JsonConvert.DeserializeAnonymousType(json, expandoObject);

                temp.EventId = persistedEvent.EventId;
                temp.EventType = persistedEvent.EventType;
                temp.EventDateTime = persistedEvent.Created;
                temp.Metadata = Encoding.Default.GetString(persistedEvent.Metadata);

                return JsonConvert.SerializeObject(temp);
            }
        }
        #endregion
    }
}