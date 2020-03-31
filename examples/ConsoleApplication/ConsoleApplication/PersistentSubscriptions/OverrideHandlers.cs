namespace ConsoleApplication.PersistentSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    public class OverrideHandlers
    {
        public static async Task AllHandlers(IEventStoreConnection eventStoreConnection)
        {
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            PersistentSubscriptionBuilder builder = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1")
                                                                                 .UseConnection(eventStoreConnection)
                                                                                 .AddEventAppearedHandler((@base,
                                                                                                           @event) =>
                                                                                                          {
                                                                                                              Console.WriteLine("Override EventAppeared called.");
                                                                                                          })
                                                                                 .AddSubscriptionDroppedHandler((uribase,
                                                                                                                 reason,
                                                                                                                 arg3) =>
                                                                                                                {
                                                                                                                    Console.WriteLine("SubscriptionDropped override.");
                                                                                                                })
                                                                                 .DeliverTo(uri);

            Subscription subscription = builder.Build();

            await subscription.Start(CancellationToken.None);
        }
    }
}