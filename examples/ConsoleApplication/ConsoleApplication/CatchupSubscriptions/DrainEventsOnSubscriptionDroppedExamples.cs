namespace ConsoleApplication.CatchupSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    public class DrainEventsOnSubscriptionDroppedExamples
    {
        public static async Task DrainEventsOnSubscriptionDropped(IEventStoreConnection eventStoreConnection)
        {
            //By setting DrainEventsAfterSubscriptionDropped, you are indicating that when a Catchup Subscription is dropped
            //that no further events be delivered (without this enabled, any events in the read buffer would be sent)
            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                                  .SetName("Test Catchup 1")
                                                                  .UseConnection(eventStoreConnection)
                                                                  .DrainEventsAfterSubscriptionDropped()
                                                                  .DeliverTo(Program.Endpoint1)
                                                                  .Build();

            await subscription.Start(CancellationToken.None);
        }
    }
}