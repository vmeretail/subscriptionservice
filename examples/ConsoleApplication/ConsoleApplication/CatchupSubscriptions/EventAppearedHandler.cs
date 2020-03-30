namespace ConsoleApplication.CatchupSubscriptions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;

    public class EventAppearedHandler
    {
        public static async Task AddEventAppearedHandler(IEventStoreConnection eventStoreConnection)
        {
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            //By adding in the AddEventAppearedHandler, you are overriding the internal EventAppeared.
            //It's important to note though, that other features such as draining on Stop and Acking will still be handled internally
            var subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest").SetName("Test Catchup 1")
                                                         .UseConnection(eventStoreConnection)
                                                         .AddEventAppearedHandler((upSubscription,
                                                                                   @event) =>
                                                                                  {
                                                                                      Console.WriteLine($"Event appeared {@event.OriginalEventNumber}");
                                                                                  })
                                                         .DeliverTo(uri)
                                                         .Build();

            await subscription.Start(CancellationToken.None);
        }
    }
}