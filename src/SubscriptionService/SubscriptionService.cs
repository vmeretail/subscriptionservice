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

    /// <summary>
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
        /// <exception cref="NullReferenceException">
        /// SubscriptionServiceBuilder cannot be null
        /// or
        /// EventStoreConnection cannot be null
        /// </exception>
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
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsStarted { get; private set; }

        public Stopwatch Stopwatch { get; set; }

        /// <summary>
        /// The default user credentials
        /// </summary>
        /// <value>
        /// The default user credentials.
        /// </value>
        private UserCredentials DefaultUserCredentials { get; }

        #endregion

        #region Events

        public event EventHandler OnCatchupSubscriptionDropped;

        /// <summary>
        /// Occurs when [on event appeared].
        /// </summary>
        public event EventHandler<HttpRequestMessage> OnEventAppeared;

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
        public async Task Start(List<Configuration.Subscription> subscriptions,
                                CancellationToken cancellationToken)
        {
            if (subscriptions == null || subscriptions.Any() == false)
            {
                throw new ArgumentNullException(nameof(subscriptions), "Value cannot be null or empty");
            }

            //Convert the Subscriptions to our internal model
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            List<Domain.Subscription> subscriptionsList = new List<Domain.Subscription>();

            subscriptions.ForEach(s => subscriptionsList.Add(subscriptionFactory.CreateFrom(s)));

            foreach (Domain.Subscription subscription in subscriptionsList)
            {
                await this.ConnectToSubscription(subscription, cancellationToken);
            }

            this.IsStarted = true;
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
                //if (serialisedEvent.Contains("07d196d4-8be3-4646-8cb2-2719f45f9816"))
                //{
                //    throw new Exception("SIMULATED EXCEPTION OCCURED");
                //}

                if (this.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.All))
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
            Console.WriteLine($"{DateTime.UtcNow}: Live processing started on managed thread {Thread.CurrentThread.ManagedThreadId}");
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