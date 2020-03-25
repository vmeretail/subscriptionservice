namespace SubscriptionService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Domain;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Factories;
    using Microsoft.Extensions.Logging;
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using Subscription = global::SubscriptionService.Configuration.Subscription;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SubscriptionService.ISubscriptionService" />
    public class SubscriptionService : ISubscriptionService
    {
        #region Fields

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
        private readonly SubscriptionServiceBuilder.LogEvents LogEventsSettings;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger Logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService" /> class.
        /// </summary>
        /// <param name="subscriptionServiceBuilder">The subscription service builder.</param>
        /// <exception cref="NullReferenceException">SubscriptionServiceBuilder cannot be null
        /// or
        /// EventStoreConnection cannot be null</exception>
        internal SubscriptionService(SubscriptionServiceBuilder subscriptionServiceBuilder)
        {
            if (subscriptionServiceBuilder == null)
            {
                throw new NullReferenceException("SubscriptionServiceBuilder cannot be null");
            }

            if (subscriptionServiceBuilder.EventStoreConnection == null)
            {
                throw new NullReferenceException("EventStoreConnection cannot be null");
            }

            this.EventFactory = subscriptionServiceBuilder.EventFactory;
            this.EventStoreConnection = subscriptionServiceBuilder.EventStoreConnection;
            this.Logger = subscriptionServiceBuilder.Logger;

            // Cache the user credentials
            this.DefaultUserCredentials = new UserCredentials(subscriptionServiceBuilder.Username, subscriptionServiceBuilder.Password);

            this.LogEventsSettings = subscriptionServiceBuilder.LogEventsSettings;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsStarted { get; private set; }

        /// <summary>
        /// The default user credentials
        /// </summary>
        /// <value>
        /// The default user credentials.
        /// </value>
        private UserCredentials DefaultUserCredentials { get; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [on event appeared].
        /// </summary>
        public event EventHandler<HttpRequestMessage> OnEventAppeared;

        public event EventHandler OnCatchupSubscriptionDropped;

        #endregion

        #region Methods

        /// <summary>
        /// Removes the subscription.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task RemoveSubscription(String groupName,
                                             String streamName,
                                             CancellationToken cancellationToken)
        {
            this.GuardAgainstInvalidGroupName(groupName);
            this.GuardAgainstInvalidStreamName(streamName);

            try
            {
                await this.EventStoreConnection.DeletePersistentSubscriptionAsync(streamName, groupName);
            }
            catch(Exception e)
            {
                this.Logger.LogError(e, $"Error remvoing persistent subscription streamName {streamName} and groupName {groupName}");
                throw;
            }
        }

        /// <summary>
        /// Start with no config
        /// </summary>
        /// <param name="subscriptions">The subscriptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">subscriptions - Value cannot be null or empty</exception>
        public async Task Start(List<global::SubscriptionService.Configuration.Subscription> subscriptions,
                                CancellationToken cancellationToken)
        {
            if (subscriptions == null || subscriptions.Any() == false)
            {
                throw new ArgumentNullException(nameof(subscriptions), "Value cannot be null or empty");
            }

            //Convert the Subscriptions to our internal model
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            List<global::SubscriptionService.Domain.Subscription> subscriptionsList = new List<global::SubscriptionService.Domain.Subscription>();

            subscriptions.ForEach(s => subscriptionsList.Add(subscriptionFactory.CreateFrom(s)));

            foreach (Domain.Subscription subscription in subscriptionsList)
            {
                await this.ConnectToSubscription(subscription, cancellationToken);
            }

            this.IsStarted = true;
        }

        public async Task StartCatchupSubscription(CatchupSubscription catchupSubscription,
                                                   CancellationToken cancellationToken)
        {
            Console.WriteLine($"{DateTime.UtcNow}: StartCatchupSubscription on managed thread{ Thread.CurrentThread.ManagedThreadId}");

            //TODO: over time, we might allow more of the settings to be fed in via the CatchupSubscription
            CatchUpSubscriptionSettings catchUpSubscriptionSettings = new CatchUpSubscriptionSettings(CatchUpSubscriptionSettings.Default.MaxLiveQueueSize,
                                                                                                      CatchUpSubscriptionSettings.Default.ReadBatchSize,
                                                                                                      CatchUpSubscriptionSettings.Default.VerboseLogging,
                                                                                                      CatchUpSubscriptionSettings.Default.ResolveLinkTos,
                                                                                                      catchupSubscription.SubscriptionName);

            Consumer consumer = new ConsumerBuilder().AddEndpointUri(catchupSubscription.EndPointUri).Build();

            async void AppearedFromCatchupSubscription(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                                       ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedFromCatchupSubscription((EventStoreStreamCatchUpSubscription)eventStoreCatchUpSubscription, resolvedEvent, consumer, cancellationToken);
            }

            //NOTE: Different way to connect to stream
            //NOTE: Could the UI be notified of this somehow
            EventStoreStreamCatchUpSubscription e = this.EventStoreConnection.SubscribeToStreamFrom(catchupSubscription.StreamName,
                                                                                                    catchupSubscription.LastCheckpoint,
                                                                                                    catchUpSubscriptionSettings,
                                                                                                    AppearedFromCatchupSubscription,
                                                                                                    this.LiveProcessingStarted,
                                                                                                    this.SubscriptionDropped);

            //TODO: Might want something a bit smarter to allow the user some understanding of what is actually running.
            this.IsStarted = true;
        }

        /// <summary>
        /// Starts the catchup subscriptions.
        /// </summary>
        /// <param name="catchupSubscriptions">The catchup subscriptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">catchupSubscriptions - Value cannot be null or empty</exception>
        public async Task StartCatchupSubscriptions(List<CatchupSubscription> catchupSubscriptions,
                                                    CancellationToken cancellationToken)
        {
            if (catchupSubscriptions == null || catchupSubscriptions.Any() == false)
            {
                throw new ArgumentNullException(nameof(catchupSubscriptions), "Value cannot be null or empty");
            }

            //TODO: Internal factory for catchupSubscriptions?

            foreach (CatchupSubscription catchupSubscription in catchupSubscriptions)
            {
                await this.StartCatchupSubscription(catchupSubscription, cancellationToken);
            }
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task Stop(CancellationToken cancellationToken)
        {
            this.IsStarted = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Connects to subscription.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task ConnectToSubscription(Domain.Subscription subscription,
                                                 CancellationToken cancellationToken)
        {
            Consumer consumer = new ConsumerBuilder().AddEndpointUri(subscription.EndPointUri).Build();

            async void EventAppearedAction(EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase,
                                           ResolvedEvent resolvedEvent)
            {
                await this.EventAppearedForPersistentSubscription(eventStorePersistentSubscriptionBase, resolvedEvent, consumer, cancellationToken);
            }

            async Task ConnectToPersistentSubscriptionAsync()
            {
                await this.EventStoreConnection.ConnectToPersistentSubscriptionAsync(subscription.StreamName,
                                                                                     subscription.GroupName,
                                                                                     EventAppearedAction,
                                                                                     (eventStorePersistentSubscriptionBase,
                                                                                      reason,
                                                                                      arg3) =>
                                                                                     {
                                                                                     },
                                                                                     null,
                                                                                     subscription.NumberOfConcurrentMessages,
                                                                                     false);
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
                    await this.CreatePersistentSubscriptionFromBeginningAsync(subscription, cancellationToken);

                    await ConnectToPersistentSubscriptionAsync();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates the persistent subscription from beginning asynchronous.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task CreatePersistentSubscriptionFromBeginningAsync(Domain.Subscription subscription,
                                                                          CancellationToken cancellationToken)
        {
            PersistentSubscriptionSettingsBuilder settings = this.GetDefaultPersistentSubscriptionSettingsBuilder().WithMaxRetriesOf(subscription.MaxRetryCount);
            if (subscription.StreamStartPosition == 0)
            {
                settings.StartFromBeginning();
            }
            else
            {
                settings.StartFrom(subscription.StreamStartPosition);
            }

            await this.EventStoreConnection.CreatePersistentSubscriptionAsync(subscription.StreamName, subscription.GroupName, settings, this.DefaultUserCredentials);
        }

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

                //TODO: testing
                if (serialisedEvent.Contains("07d196d4-8be3-4646-8cb2-2719f45f9816"))
                {
                    throw new Exception("SIMULATED EXCEPTION OCCURED");
                }

                if (this.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.All))
                {
                    this.Logger.LogInformation($"Serialised data is {serialisedEvent}");
                }

                //TODO: Don't bother with http posting
                return;

                //Build a standard WebRequest
                HttpRequestMessage request = new HttpRequestMessage
                                             {
                                                 Method = HttpMethod.Post,
                                                 Content = consumer.GetContent(serialisedEvent),
                                                 RequestUri = consumer.GetUri()
                                             };

                if (this.OnEventAppeared != null)
                {
                    this.Logger.LogInformation($"Event Id {resolvedEvent.Event.EventId} - Using custom Event Appeared");

                    //Let the caller make some changes to the HttpRequestMessage
                    this.OnEventAppeared(this, request);
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

                if (this.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.Errors))
                {
                    this.Logger.LogError(e, $"Exception has occured on EventAppeared with Event {serialisedEvent}");
                }

                //We will let the caller decide what happens next
                throw;
            }
        }

        private async Task EventAppearedForPersistentSubscription(EventStorePersistentSubscriptionBase subscription,
                                                                  ResolvedEvent resolvedEvent,
                                                                  Consumer consumer,
                                                                  CancellationToken cancellationToken)
        {
            try
            {
                await this.EventAppeared(resolvedEvent, consumer, cancellationToken);

                //If we reach here, safe to ACK
                subscription.Acknowledge(resolvedEvent);
            }
            catch(Exception e)
            {
                this.NakEvent(subscription, resolvedEvent, e);
            }
        }

        private async void SubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                               SubscriptionDropReason subscriptionDropReason,
                                               Exception e)
        {
            //TODO: remove
            Console.WriteLine("SubscriptionDropped");

            //TODO: Auto reconnect will be implemented (some how)
            this.Logger.LogError(e, $"SubscriptionDropped: Stream Name: [{eventStoreCatchUpSubscription.SubscriptionName}] Reason[{subscriptionDropReason}]");


            await Task.Yield();
            eventStoreCatchUpSubscription.Stop(TimeSpan.FromMinutes(1));

            if (OnCatchupSubscriptionDropped != null)
            {
                //TODO: we will need to send more info
                //Let the outside world know a subscription has dropped
                this.OnCatchupSubscriptionDropped(this, new EventArgs());
            }
        }

        private async Task EventAppearedFromCatchupSubscription(EventStoreStreamCatchUpSubscription eventStoreCatchUpSubscription,
                                                                ResolvedEvent resolvedEvent,
                                                                Consumer consumer,
                                                                CancellationToken cancellationToken)
        {
            Console.WriteLine($"{DateTime.UtcNow}: Received event { resolvedEvent.OriginalEventNumber}({ resolvedEvent.OriginalEvent.EventType}) on managed thread{ Thread.CurrentThread.ManagedThreadId}");

            //Would this be useful?
            //eventStoreCatchUpSubscription.LastProcessedEventNumber

            try
            {
                await this.EventAppeared(resolvedEvent, consumer, cancellationToken);
            }
            catch(Exception e)
            {
                Console.WriteLine($"{DateTime.UtcNow}: EventAppearedFromCatchupSubscription Exception { resolvedEvent.OriginalEventNumber}({ resolvedEvent.OriginalEvent.EventType}) on managed thread { Thread.CurrentThread.ManagedThreadId} {e.Message}");

                //TODO: we will eventually handle parked / dead letter events here.
                //TODO: Log out Subscription Name?
                this.Logger.LogError(e, $"Exception occured from CatchupSubscription {resolvedEvent.Event.EventId}");

                this.Stopwatch = Stopwatch.StartNew();

                //This eventually fires SubscriptionDropped but some more events will make it through before then
                eventStoreCatchUpSubscription.Stop();

                //NOTE: Important to throw exception, otherwise we would move onto the next Event!
                //throw;
            }
        }

        public Stopwatch Stopwatch { get; set; }


        /// <summary>
        /// Gets the default persistent subscription settings builder.
        /// </summary>
        /// <returns></returns>
        private PersistentSubscriptionSettingsBuilder GetDefaultPersistentSubscriptionSettingsBuilder()
        {
            //All standard settings in here. We might put configuration in here.
            return PersistentSubscriptionSettings.Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Guards the name of the against invalid group.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <exception cref="ArgumentNullException">groupName</exception>
        private void GuardAgainstInvalidGroupName(String groupName)
        {
            if (String.IsNullOrEmpty(groupName))
            {
                throw new ArgumentNullException(nameof(groupName));
            }
        }

        /// <summary>
        /// Guards the name of the against invalid stream.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <exception cref="ArgumentNullException">streamName</exception>
        private void GuardAgainstInvalidStreamName(String streamName)
        {
            if (String.IsNullOrEmpty(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }
        }

        private void LiveProcessingStarted(EventStoreCatchUpSubscription obj)
        {
            //NOTE: Once we have caught up, this gets fired - but any new events will then appear in EventAppeared
            //This is for information only (I think)
            this.Logger.LogInformation($"LiveProcessingStarted: Stream Name: [{obj.SubscriptionName}]");

            //TODO: Temp unitl I work out why logger not working
            Console.WriteLine($"{DateTime.UtcNow}: Live processing started on managed thread { Thread.CurrentThread.ManagedThreadId}");
        }

        /// <summary>
        /// Naks the event.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="exception">The exception.</param>
        private void NakEvent(EventStorePersistentSubscriptionBase subscription,
                              ResolvedEvent resolvedEvent,
                              Exception exception)
        {
            try
            {
                subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Retry, exception.Message);
            }
            catch(Exception ex)
            {
                this.Logger.LogError(ex, $"Exception has occured when NAKing event id {resolvedEvent.Event.EventId}");
            }
        }




        #endregion
    }
}