namespace SubscriptionService
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using Domain;
    using EventStore.ClientAPI;
    using Factories;
    using Microsoft.Extensions.Logging;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// </summary>
    public class Subscription
    {
        #region Fields

        /// <summary>
        /// The event store persistent subscription base
        /// </summary>
        internal EventStorePersistentSubscriptionBase EventStorePersistentSubscriptionBase;

        //TODO: Could we make the two base types a generic?

        /// <summary>
        /// The event store stream catch up subscription
        /// </summary>
        internal EventStoreStreamCatchUpSubscription EventStoreStreamCatchUpSubscription;

        /// <summary>
        /// The event factory
        /// </summary>
        private readonly IEventFactory EventFactory;

        /// <summary>
        /// The event store connection
        /// </summary>
        private readonly IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The log events settings
        /// </summary>
        private readonly SubscriptionBuilder.LogEvents LogEventsSettings;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// The subscription builder
        /// </summary>
        private readonly SubscriptionBuilder SubscriptionBuilder;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class.
        /// </summary>
        /// <param name="subscriptionBuilder">The subscription builder.</param>
        /// <exception cref="NullReferenceException">
        /// subscriptionBuilder cannot be null
        /// or
        /// EventStoreConnection cannot be null
        /// </exception>
        internal Subscription(SubscriptionBuilder subscriptionBuilder)
        {
            if (subscriptionBuilder == null)
            {
                throw new NullReferenceException("subscriptionBuilder cannot be null");
            }

            if (subscriptionBuilder.EventStoreConnection == null)
            {
                throw new NullReferenceException("EventStoreConnection cannot be null");
            }

            this.SubscriptionBuilder = subscriptionBuilder;

            this.EventFactory = subscriptionBuilder.EventFactory;
            this.EventStoreConnection = subscriptionBuilder.EventStoreConnection;
            this.Logger = subscriptionBuilder.Logger;

            this.LogEventsSettings = subscriptionBuilder.LogEventsSettings;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsStarted { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="InvalidOperationException">Subscription already started.</exception>
        public async Task Start(CancellationToken cancellationToken)
        {
            if (this.IsStarted)
            {
                throw new InvalidOperationException("Subscription already started.");
            }

            //TODO: Fix trace
            Console.WriteLine($"{DateTime.UtcNow}: {this.SubscriptionBuilder.GetType()} Start on managed thread {Thread.CurrentThread.ManagedThreadId}");

            //Choose which start route
            await this.Start((dynamic)this.SubscriptionBuilder, cancellationToken);

            this.IsStarted = true;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            this.IsStarted = false;

            this.Stop((dynamic)this.SubscriptionBuilder);
        }

        /// <summary>
        /// Events the appeared.
        /// </summary>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="consumer">The consumer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="Exception">Event Id {resolvedEvent.Event.EventId} - Response from server was {response}</exception>
        private async Task EventAppeared(ResolvedEvent resolvedEvent,
                                         Consumer consumer,
                                         CancellationToken cancellationToken)
        {
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            String serialisedEvent = null; //Put this out here in-case we need to log the event out for errors.

            try
            {
                if (resolvedEvent.Event == null)
                {
                    return;
                }

                if (resolvedEvent.Event.EventType.StartsWith("$"))
                {
                    //We will ignore event types beginning with the character $.
                    return;
                }

                this.Logger.LogInformation($"EventAppeared - Event Id {resolvedEvent.Event.EventId}");

                RecordedEvent recordedEvent = resolvedEvent.Event;

                //Convert recorded event to PersistedEvent
                //If the Event Store client changes the data type being spat out from EventAppeared, we cna make a small change here to cater for it
                PersistedEvent persistedEvent = PersistedEvent.Create(recordedEvent.Created,
                                                                      recordedEvent.CreatedEpoch,
                                                                      recordedEvent.Data,
                                                                      recordedEvent.EventId,
                                                                      recordedEvent.EventNumber,
                                                                      recordedEvent.EventStreamId,
                                                                      recordedEvent.EventType,
                                                                      recordedEvent.IsJson,
                                                                      recordedEvent.Metadata);

                //Get the serialised data
                serialisedEvent = this.EventFactory.ConvertFrom(persistedEvent);

                if (this.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.All))
                {
                    this.Logger.LogInformation($"Serialised data is {serialisedEvent}");
                }

                //Build a standard WebRequest
                HttpRequestMessage request = new HttpRequestMessage
                                             {
                                                 Method = HttpMethod.Post,
                                                 Content = consumer.GetContent(serialisedEvent),
                                                 RequestUri = consumer.GetUri()
                                             };

                this.Logger.LogInformation($"Event Id {resolvedEvent.Event.EventId} - Using default Event Appeared");

                if (this.SubscriptionBuilder.HttpRequestInterceptor != null)
                {
                    this.Logger.LogInformation($"Event Id {resolvedEvent.Event.EventId} - Http Interceptor used");

                    //Let the caller make some changes to the HttpRequestMessage
                    this.SubscriptionBuilder.HttpRequestInterceptor(request);
                }
                else
                {
                    this.Logger.LogInformation($"Event Id {resolvedEvent.Event.EventId} - Using default Event Appeared");
                }

                HttpClient httpClient = consumer.GetHttpClient();

                HttpResponseMessage postTask = await httpClient.SendAsync(request, linkedTokenSource.Token);

                //Throw exception if not successful
                if (!postTask.IsSuccessStatusCode)
                {
                    String response = await postTask.Content.ReadAsStringAsync();

                    //This would force a NAK
                    throw new Exception($"Event Id {resolvedEvent.Event.EventId} - Response from server was {response}");
                }

                this.Logger.LogInformation($"Event Id {resolvedEvent.Event.EventId} - Event POST successful");
            }
            catch(Exception e)
            {
                // Cancel the call to the server
                linkedTokenSource.Cancel();

                if (this.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.Errors))
                {
                    this.Logger.LogError(e, $"Exception has occured on EventAppeared with Event {serialisedEvent}");
                }

                throw;
            }
        }

        /// <summary>
        /// Events the appeared for persistent subscription.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="consumer">The consumer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task EventAppearedForPersistentSubscription(EventStorePersistentSubscriptionBase subscription,
                                                                  ResolvedEvent resolvedEvent,
                                                                  Consumer consumer,
                                                                  CancellationToken cancellationToken)
        {
            Boolean autoAck = ((PersistentSubscriptionBuilder)this.SubscriptionBuilder).AutoAck;

            //TODO: Temp
            Console.WriteLine($"{DateTime.UtcNow}: EventAppearedForPersistentSubscription  {resolvedEvent.OriginalEventNumber} on managed thread {Thread.CurrentThread.ManagedThreadId}");

            try
            {
                await this.EventAppeared(resolvedEvent, consumer, cancellationToken);

                if (autoAck == false)
                {
                    //If we reach here, safe to ACK
                    subscription.Acknowledge(resolvedEvent);
                }
            }
            catch(Exception e)
            {
                try
                {
                    subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Retry, e.Message);
                }
                catch(Exception ex)
                {
                    this.Logger.LogError(ex, $"Exception has occured when NAKing event id {resolvedEvent.Event.EventId}");
                }
            }
        }

        /// <summary>
        /// Events the appeared from catchup subscription.
        /// </summary>
        /// <param name="eventStoreCatchUpSubscription">The event store catch up subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="consumer">The consumer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task EventAppearedFromCatchupSubscription(EventStoreStreamCatchUpSubscription eventStoreCatchUpSubscription,
                                                                ResolvedEvent resolvedEvent,
                                                                Consumer consumer,
                                                                CancellationToken cancellationToken)
        {
            try
            {
                await this.EventAppeared(resolvedEvent, consumer, cancellationToken);
            }
            catch(Exception e)
            {
                Console.WriteLine($"{DateTime.UtcNow}: EventAppearedFromCatchupSubscription Exception {resolvedEvent.OriginalEventNumber}({resolvedEvent.OriginalEvent.EventType}) on managed thread {Thread.CurrentThread.ManagedThreadId} {e.Message}");

                //TODO: Log out Subscription Name?
                this.Logger.LogError(e, $"Exception occured from CatchupSubscription {resolvedEvent.Event.EventId}");

                //TODO: Do we now flag this subscriptino as beign signalled to stop and block all other events?

                //This eventually fires SubscriptionDropped but some more events will make it through before then
                eventStoreCatchUpSubscription.Stop();
            }
        }

        /// <summary>
        /// Lives the processing started.
        /// </summary>
        /// <param name="eventStoreCatchUpSubscription">The event store catch up subscription.</param>
        private void LiveProcessingStarted(EventStoreCatchUpSubscription eventStoreCatchUpSubscription)
        {
            //NOTE: Once we have caught up, this gets fired - but any new events will then appear in EventAppeared
            //This is for information only (I think)
            this.Logger.LogInformation($"LiveProcessingStarted: Stream Name: [{eventStoreCatchUpSubscription.SubscriptionName}]");

            //TODO: Temp unitl I work out why logger not working
            Console.WriteLine($"{DateTime.UtcNow}: Live processing started on managed thread {Thread.CurrentThread.ManagedThreadId}");
        }

        private async Task Start(CatchupSubscriptionBuilder catchupSubscriptionBuilder,
                                 CancellationToken cancellationToken)
        {
            ConsumerBuilder consumerBuilder = new ConsumerBuilder();

            Consumer consumer = consumerBuilder.AddEndpointUri(catchupSubscriptionBuilder.Uri).Build();

            if (catchupSubscriptionBuilder.LiveProcessingStarted == null)
            {
                //We wire up the default handler.
                catchupSubscriptionBuilder.LiveProcessingStarted = this.LiveProcessingStarted;
            }

            if (catchupSubscriptionBuilder.EventAppeared == null)
            {
                //We wire up the default handler.
                catchupSubscriptionBuilder.EventAppeared = AppearedFromCatchupSubscription;
            }

            if (catchupSubscriptionBuilder.SubscriptionDropped == null)
            {
                //We wire up the default handler.
                catchupSubscriptionBuilder.SubscriptionDropped = SubscriptionDropped;
            }

            async void AppearedFromCatchupSubscription(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                                       ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedFromCatchupSubscription((EventStoreStreamCatchUpSubscription)eventStoreCatchUpSubscription,
                                                                resolvedEvent,
                                                                consumer,
                                                                cancellationToken);
            }

            async void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                           SubscriptionDropReason subscriptionDropReason,
                                           Exception e)
            {
                await this.SubscriptionDropped(eventStoreCatchUpSubscription, subscriptionDropReason, e);
            }

            this.EventStoreStreamCatchUpSubscription = this.EventStoreConnection.SubscribeToStreamFrom(catchupSubscriptionBuilder.StreamName,
                                                                                                       catchupSubscriptionBuilder.LastCheckpoint,
                                                                                                       catchupSubscriptionBuilder.CatchUpSubscriptionSettings,
                                                                                                       catchupSubscriptionBuilder.EventAppeared,
                                                                                                       catchupSubscriptionBuilder.LiveProcessingStarted,
                                                                                                       catchupSubscriptionBuilder.SubscriptionDropped);
        }

        private async Task Start(PersistentSubscriptionBuilder persistentSubscriptionBuilder,
                                 CancellationToken cancellationToken)
        {
            ConsumerBuilder consumerBuilder = new ConsumerBuilder();
            Consumer consumer = consumerBuilder.AddEndpointUri(persistentSubscriptionBuilder.Uri).Build();

            if (persistentSubscriptionBuilder.EventAppeared == null)
            {
                //We wire up the default handler.
                persistentSubscriptionBuilder.EventAppeared = EventAppearedAction;
            }

            if (persistentSubscriptionBuilder.SubscriptionDropped == null)
            {
                //TODO: -create body for default subscription dropped.
                //We wire up the default handler.
                persistentSubscriptionBuilder.SubscriptionDropped = (eventStorePersistentSubscriptionBase,
                                                                     reason,
                                                                     arg3) =>
                                                                    {
                                                                        Console.WriteLine("Default SubscriptionDropped called");
                                                                    };
            }

            async void EventAppearedAction(EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase,
                                           ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedForPersistentSubscription(eventStorePersistentSubscriptionBase, resolvedEvent, consumer, cancellationToken);
            }

            async Task ConnectToPersistentSubscriptionAsync()
            {
                //TODO: buffer
                //Does BufferSize relate to a param in PersistentSubscriptionSettings?

                this.EventStorePersistentSubscriptionBase = await this.EventStoreConnection.ConnectToPersistentSubscriptionAsync(persistentSubscriptionBuilder.StreamName,
                                                                                                                                 persistentSubscriptionBuilder.GroupName,
                                                                                                                                 persistentSubscriptionBuilder
                                                                                                                                     .EventAppeared,
                                                                                                                                 persistentSubscriptionBuilder
                                                                                                                                     .SubscriptionDropped,
                                                                                                                                 persistentSubscriptionBuilder
                                                                                                                                     .UserCredentials,
                                                                                                                                 autoAck:persistentSubscriptionBuilder
                                                                                                                                     .AutoAck);
            }

            try
            {
                await ConnectToPersistentSubscriptionAsync();
            }
            catch(Exception e)
            {
                if (e.InnerException != null && e.InnerException.Message == "Subscription not found")
                {
                    //Create the Group
                    await this.EventStoreConnection.CreatePersistentSubscriptionAsync(persistentSubscriptionBuilder.StreamName,
                                                                                      persistentSubscriptionBuilder.GroupName,
                                                                                      persistentSubscriptionBuilder.PersistentSubscriptionSettings,
                                                                                      persistentSubscriptionBuilder.UserCredentials);

                    await ConnectToPersistentSubscriptionAsync();
                }
                else
                {
                    throw;
                }
            }
        }

        private void Stop(CatchupSubscriptionBuilder catchupSubscriptionBuilder)
        {
            //TODO: Might add the EventStoreStreamCatchUpSubscription to CatchupSubscriptionBuilder
            this.EventStoreStreamCatchUpSubscription.Stop();
        }

        private void Stop(PersistentSubscriptionBuilder subscriptionBuilder)
        {
            //TODO: Review this
            //this.EventStorePersistentSubscriptionBase.Stop(TimeSpan.FromSeconds(30));
        }

        private async Task SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                               SubscriptionDropReason subscriptionDropReason,
                                               Exception e)
        {
            //TODO: Log this out properly
            Console.WriteLine("Default SubscriptionDropped");
        }

        #endregion
    }
}