namespace TestHarness
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Microsoft.Extensions.Logging;

    using SubscriptionService;
    using SubscriptionService.Builders;
    using SubscriptionService.Extensions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    internal class Program
    {
        #region Methods

        private static async Task CatchupTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("CatchupLogger");

            //RequestBin
            Uri uri = new Uri("https://ennxdwa7hkx8e.x.pipedream.net/");

            Subscription subscription = CatchupSubscriptionBuilder.Create("$ce-OrderAggregate").SetName("Test Catchup 1")
                                                                  //.SetLastCheckpoint(5000)
                                                                  .UseConnection(eventStoreConnection)
                                                                  .AddEventAppearedHandler((upSubscription,
                                                                                            @event) =>
                                                                                           {
                                                                                               Console.WriteLine($"{DateTime.UtcNow}: EventAppeared - Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

                                                                                               if (@event.OriginalEventNumber == 78) //simulate subscription dropped
                                                                                               {
                                                                                                   throw new Exception($"Engineered Exception");

                                                                                                   try
                                                                                                   {
                                                                                                       throw new Exception($"Engineered Exception");
                                                                                                   }
                                                                                                   catch (Exception e)
                                                                                                   {
                                                                                                       Console
                                                                                                           .WriteLine($"{DateTime.UtcNow}: About to call stop {Thread.CurrentThread.ManagedThreadId}");

                                                                                                       //upSubscription.Stop();
                                                                                                   }
                                                                                               }

                                                                                               Console.WriteLine($"DELIVERED Event {@event.OriginalEventNumber}");
                                                                                           })
                                                                //.AddLiveProcessingStartedHandler(upSubscription =>
                                                                //                                 {
                                                                //                                     Console.WriteLine("Override LiveProcessingStarted");
                                                                //                                 } )
                                                                //.AddEventAppearedHandler((upSubscription,
                                                                //                        @event) =>
                                                                //{
                                                                //  Console.WriteLine($"{DateTime.UtcNow}: EventAppeared - Start on managed thread {Thread.CurrentThread.ManagedThreadId}");
                                                                //  Boolean isSignalled = manualResetEvent.WaitOne(TimeSpan.FromMilliseconds(2)); //This blocks the thread which AddSubscriptionDroppedHandler is running on...

                                                                  //    if (isSignalled == false)
                                                                  //    {
                                                                  //        //I think this would carry on receiving events until the read buffer was empty.
                                                                  //        //We can't throw an Exception here as it will bring the process down.
                                                                  //        Console.WriteLine($"{DateTime.UtcNow}: Draining {@event.OriginalEventNumber} {Thread.CurrentThread.ManagedThreadId}");

                                                                  //        //By returning, we will skip the POST and broadcasting lastCheckpoint
                                                                  //        return;
                                                                  //    }

                                                                  //    if (@event.OriginalEventNumber == 78) //simulate subscription dropped
                                                                  //    {
                                                                  //        try
                                                                  //        {
                                                                  //            throw new Exception($"Engineered Exception");
                                                                  //        }
                                                                  //        catch(Exception e)
                                                                  //        {
                                                                  //            Console
                                                                  //                .WriteLine($"{DateTime.UtcNow}: About to call stop {Thread.CurrentThread.ManagedThreadId}");

                                                                  //            manualResetEvent.Reset(); //we don't want anymore events to get through
                                                                  //            upSubscription.Stop();
                                                                  //        }
                                                                  //    }


                                                                  //    Console.WriteLine($"{DateTime.UtcNow}: Posting  {Thread.CurrentThread.ManagedThreadId}");

                                                                  //    //Here is where we would POST the event

                                                                  //    //Here is where we broadcast the lastCheckpoint
                                                                  //    Console.WriteLine($"{DateTime.UtcNow}: LastCheckpoint {@event.OriginalEventNumber}  {Thread.CurrentThread.ManagedThreadId}");


                                                                  //})
                                                                  .DrainEventsAfterSubscriptionDropped()
                                                                  .AddSubscriptionDroppedHandler((upSubscription,
                                                                                                   reason,
                                                                                                   arg3) =>
                                                                                                  {
                                                                                                        Console.WriteLine($"{DateTime.UtcNow}: Override SubscriptionDropped {Thread.CurrentThread.ManagedThreadId}");
                                                                                                      eventStoreConnection.Close();
                                                                                                  }
                                                                                                  )
                                                                  .DeliverTo(uri).UseHttpInterceptor(message =>
                                                                                                     {
                                                                                                         //The user can make some changes (like adding headers)
                                                                                                         message.Headers.Add("Authorization", "Bearer someToken");
                                                                                                     }).AddLogger(logger).Build();

            await subscription.Start(CancellationToken.None);

            //Thread.Sleep(5000);

            //subscription.Stop();
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        private static async Task Main(String[] args)
        {
            await Program.CatchupTest();
            //await Program.PersistentTest();

            Console.ReadKey();
        }

        private static async Task PersistentTest()
        {
            String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            ILogger logger = new LoggerFactory().CreateLogger("PersistentLogger");

            //RequestBin
            Uri uri = new Uri("https://en6m4s6ex2wfg.x.pipedream.net");

            PersistentSubscriptionBuilder builder = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1")
                                                                                 .UseConnection(eventStoreConnection)
                                                                                 //.AddEventAppearedHandler((@base,
                                                                                 //                                                              @event) =>
                                                                                 //                                                             {
                                                                                 //                                                                 Console
                                                                                 //                                                                     .WriteLine("Override EventAppeared called.");
                                                                                 //                                                             }).AutoAckEvents()
                                                                                 .DeliverTo(uri)
                                                                                 .SetInFlightLimit(1)
                                                                                 .UseHttpInterceptor(message =>
                                                                                                                    {
                                                                                                                        //The user can make some changes (like adding headers)
                                                                                                                        message.Headers.Add("Authorization",
                                                                                                                                            "Bearer someToken");
                                                                                                                    }).AddLogger(logger);

            Subscription subscription = builder.Build();

            await subscription.Start(CancellationToken.None);

            Console.ReadKey();

            subscription.Stop();
        }

        #endregion
    }
}