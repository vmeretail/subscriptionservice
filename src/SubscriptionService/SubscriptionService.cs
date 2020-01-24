﻿namespace SubscriptionService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Domain;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Factories;

    /// <summary>
    /// </summary>
    /// <seealso cref="ISubscriptionService" />
    /// <seealso cref="ISubscriptionService" />
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IEventFactory EventFactory;

        #region Fields

        /// <summary>
        /// The event store connection
        /// </summary>
        private readonly IEventStoreConnection EventStoreConnection;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService" /> class.
        /// </summary>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="ArgumentNullException">Value cannot be null - eventStoreConnection</exception>
        public SubscriptionService(IEventStoreConnection eventStoreConnection,
                                   String username = "admin",
                                   String password = "changeit") : this(Factories.EventFactory.Create(), eventStoreConnection,username,password)
        {
            
        }
        public SubscriptionService(IEventFactory eventFactory,
                                   IEventStoreConnection eventStoreConnection,
                                   String username = "admin",
                                   String password = "changeit")
        {

            if (eventStoreConnection == null)
            {
                throw new ArgumentNullException("Value cannot be null", nameof(eventStoreConnection));
            }

            if (eventFactory == null)
            {
                //Create a default factory
                eventFactory = Factories.EventFactory.Create();
            }

            this.EventFactory = eventFactory;

            this.EventStoreConnection = eventStoreConnection;

            // Cache the user credentials
            this.DefaultUserCredentials = new UserCredentials(username, password);
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

        /// <summary>
        /// Occurs when trace is generated.
        /// </summary>
        public event TraceHandler TraceGenerated;

        public event TraceHandler ErrorHasOccured;

        #endregion

        #region Methods

        /// <summary>
        /// Start with no config
        /// </summary>
        /// <param name="subscriptions">The subscriptions.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task Start(List<Configuration.Subscription> subscriptions, CancellationToken cancellationToken)
        {
            if (subscriptions == null || subscriptions.Any() == false)
            {
                throw new ArgumentNullException("Value cannot be null or empty", nameof(subscriptions));
            }

            //Convert the Subscriptions to our internal model
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            List<Subscription> subscriptionsList = new List<Subscription>();

            subscriptions.ForEach(s => subscriptionsList.Add(subscriptionFactory.CreateFrom(s)));

            foreach (Subscription subscription in subscriptionsList)
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
        private async Task ConnectToSubscription(Subscription subscription,
                                                 CancellationToken cancellationToken)
        {
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppearedAction = async (eventStorePersistentSubscriptionBase,
                                                                                                     resolvedEvent) =>
                                                                                              {
                                                                                                  await this.EventAppeared(eventStorePersistentSubscriptionBase,
                                                                                                                           resolvedEvent,
                                                                                                                           subscription,
                                                                                                                           cancellationToken);
                                                                                              };

            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDroppedAction = async (eventStorePersistentSubscriptionBase,
                                                                                                                               subscriptionDropReason,
                                                                                                                               exception) =>
                                                                                                                        {
                                                                                                                            await this
                                                                                                                                .SubscriptionDropped(eventStorePersistentSubscriptionBase,
                                                                                                                                                     subscriptionDropReason,
                                                                                                                                                     exception,
                                                                                                                                                     cancellationToken);
                                                                                                                        };

            async Task ConnectToPersistentSubscriptionAsync()
            {
                await this.EventStoreConnection.ConnectToPersistentSubscriptionAsync(subscription.StreamName,
                                                                                                                                   subscription.GroupName,
                                                                                                                                   eventAppearedAction,
                                                                                                                                   subscriptionDroppedAction,
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
        private async Task CreatePersistentSubscriptionFromBeginningAsync(Subscription subscription,
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

        /// <summary>
        /// Events the appeared.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="subscriptionConfiguration">The subscription configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="Exception">Response from server was {response}</exception>
        private async Task EventAppeared(EventStorePersistentSubscriptionBase subscription,
                                         ResolvedEvent resolvedEvent,
                                         Subscription subscriptionConfiguration,
                                         CancellationToken cancellationToken)
        {
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                //This helps stop sending unused events to our read models etc (which will probably end up parked anyway)
                if (resolvedEvent.Event == null)
                {
                    // This indicates we have a badly formatted event so just ignore it as nothing can be done 
                    subscription.Acknowledge(resolvedEvent);
                    return;
                }

                if (resolvedEvent.Event.EventType.StartsWith("$"))
                {
                    //We will ignore event types beginning with the character $.
                    subscription.Acknowledge(resolvedEvent);
                    return;
                }

                this.Trace($"Event Id {resolvedEvent.Event.EventId} - EventAppearedFromPersistentSubscription");

                RecordedEvent j = resolvedEvent.Event;

                String serialisedData = this.EventFactory.ConvertFrom(resolvedEvent.Event);

                //Build a standard WebRequest
                HttpRequestMessage request = new HttpRequestMessage
                                             {
                                                 Method = HttpMethod.Post,
                                                 Content = new StringContent(serialisedData, Encoding.UTF8, "application/json"),
                                                 RequestUri = subscriptionConfiguration.EndPointUri
                                             };

                if (this.OnEventAppeared != null)
                {
                    //Let the caller make some changes to the HttpRequestMessage
                    this.OnEventAppeared(this, request);
                }
                
                this.Trace($"Event Id {resolvedEvent.Event.EventId} - Using default Event Appeared");

                HttpClient httpClient = subscriptionConfiguration.HttpClient;

                HttpResponseMessage postTask = await httpClient.SendAsync(request, cancellationToken);

                //Throw exception if not successful
                if (!postTask.IsSuccessStatusCode)
                {
                    String response = await postTask.Content.ReadAsStringAsync();

                    //This would force a NAK
                    throw new Exception($"Event Id {resolvedEvent.Event.EventId} - Response from server was {response}");
                }

                this.Trace($"Event Id {resolvedEvent.Event.EventId} - Event POST successful");

                subscription.Acknowledge(resolvedEvent);
            }
            catch(Exception e)
            {
                // Cancel the call to the server
                linkedTokenSource.Cancel();

                this.Trace(e);
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
                this.Trace(ex);
            }
        }

        /// <summary>
        /// Subscriptions the dropped.
        /// </summary>
        /// <param name="eventStorePersistentSubscriptionBase">The event store persistent subscription base.</param>
        /// <param name="subscriptionDropReason">The subscription drop reason.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private Task SubscriptionDropped(EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase,
                                         SubscriptionDropReason subscriptionDropReason,
                                         Exception exception,
                                         CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Traces the specified trace.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void Trace(String trace)
        {
            this.TraceGenerated?.Invoke(trace);
        }

        /// <summary>
        /// Traces the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void Trace(Exception exception)
        {
            if (this.ErrorHasOccured != null)
            {
                this.ErrorHasOccured(exception.Message);

                if (exception.InnerException != null)
                {
                    this.Trace(exception.InnerException);
                }
            }
        }

        #endregion
    }
}