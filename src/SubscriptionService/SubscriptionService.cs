namespace SubscriptionService
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;

    /// <summary>
    /// </summary>
    /// <seealso cref="SubscriptionService.ISubscriptionService" />
    /// <seealso cref="ISubscriptionService" />
    public class SubscriptionService : ISubscriptionService
    {
        #region Fields

        /// <summary>
        /// The connection string
        /// </summary>
        private readonly String ConnectionString;

        /// <summary>
        /// The destroy lock
        /// </summary>
        private readonly Object DestroyLock = new Object();

        /// <summary>
        /// The event store connection
        /// </summary>
        private readonly IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The HTTP client
        /// </summary>
        private readonly HttpClient HttpClient;

        /// <summary>
        /// The lock object
        /// </summary>
        private readonly Object LockObject = new Object();

        /// <summary>
        /// The manual reset event
        /// </summary>
        private readonly ManualResetEvent ManualResetEvent = new ManualResetEvent(true);

        /// <summary>
        /// The subscriptions
        /// </summary>
        private readonly List<Subscription> Subscriptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService" /> class.
        /// </summary>
        /// <param name="subscriptions">The subscriptions.</param>
        /// <param name="eventStoreConnection">The event store connection.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public SubscriptionService(List<Subscription> subscriptions,
                                   IEventStoreConnection eventStoreConnection,
                                   String username = "admin",
                                   String password = "changeit")
        {
            this.Subscriptions = subscriptions;
            this.EventStoreConnection = eventStoreConnection;

            // Wire up the event store connection events
            this.EventStoreConnection.Connected += this.EventStoreConnection_Connected;
            this.EventStoreConnection.Closed += this.EventStoreConnection_Closed;
            this.EventStoreConnection.Reconnecting += this.EventStoreConnection_Reconnecting;
            this.EventStoreConnection.ErrorOccurred += this.EventStoreConnection_ErrorOccurred;
            this.EventStoreConnection.AuthenticationFailed += this.EventStoreConnection_AuthenticationFailed;
            this.EventStoreConnection.Disconnected += this.EventStoreConnection_Disconnected;

            // Cache the user credentials
            this.DefaultUserCredentials = new UserCredentials(username, password);

            // Default value at the moment less than the default Event Store Retry of 10 seconds
            Int32 httpRequestTimeout = 8;

            // Create our Http Client to publish the message to the consumer endpoint
            this.HttpClient = new HttpClient();
            this.HttpClient.Timeout = TimeSpan.FromSeconds(httpRequestTimeout);
            this.HttpClient.DefaultRequestHeaders.Accept.Clear();
            this.HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        private IEventStoreConnection Connection { get; set; }

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
        /// Occurs when trace is generated.
        /// </summary>
        public event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        /// <summary>
        /// Start with no config
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task Start(CancellationToken cancellationToken)
        {
            foreach (Subscription subscription in this.Subscriptions)
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
            Int32 bufferSize = 10;

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
                                                                                     bufferSize,
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
                    await this.CreatePersistentSubscriptionFromBeginningAsync(subscription.StreamName, subscription.GroupName, cancellationToken);

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
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task CreatePersistentSubscriptionFromBeginningAsync(String stream,
                                                                          String groupName,
                                                                          CancellationToken cancellationToken)
        {
            PersistentSubscriptionSettingsBuilder settings = this.GetDefaultPersistentSubscriptionSettingsBuilder().StartFromBeginning();

            await this.EventStoreConnection.CreatePersistentSubscriptionAsync(stream, groupName, settings, this.DefaultUserCredentials);
        }

        /// <summary>
        /// Events the appeared.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="subscriptionConfiguration">The subscription configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task EventAppeared(EventStorePersistentSubscriptionBase subscription,
                                         ResolvedEvent resolvedEvent,
                                         Subscription subscriptionConfiguration,
                                         CancellationToken cancellationToken)
        {
            try
            {
                //If instructed to, we will ignore event types beginning with the character $.
                //This helps stop sending unused events to our read models etc (which will probably end up parked anyway)
                if (resolvedEvent.Event == null)
                {
                    // This indicates we have a badly formatted event so just ignore it as nothing can be done :|
                    subscription.Acknowledge(resolvedEvent);
                    return;
                }

                this.Trace($"EventAppearedFromPersistentSubscription with Event Id {resolvedEvent.Event.EventId}");

                String serialisedData = this.GetSerialisedDataFromEvent(resolvedEvent);

                await this.PublishMessage(subscriptionConfiguration.EndPointUrl, serialisedData, resolvedEvent.Event.EventId, cancellationToken);

                subscription.Acknowledge(resolvedEvent);
            }
            catch(Exception e)
            {
                this.Trace(e);
                this.NakEvent(subscription, resolvedEvent, e);
            }
        }

        /// <summary>
        /// Handles the AuthenticationFailed event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientAuthenticationFailedEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_AuthenticationFailed(Object sender,
                                                               ClientAuthenticationFailedEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} AuthenticationFailed, Reason {e.Reason}");
        }

        /// <summary>
        /// Handles the Closed event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientClosedEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Closed(Object sender,
                                                 ClientClosedEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} Closed, Reason {e.Reason}");
        }

        /// <summary>
        /// Handles the Connected event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Connected(Object sender,
                                                    ClientConnectionEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} Connected to Endpoint {e.RemoteEndPoint.Address}:{e.RemoteEndPoint.Port}");
        }

        /// <summary>
        /// Handles the Disconnected event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Disconnected(Object sender,
                                                       ClientConnectionEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} Disconnected from Endpoint {e.RemoteEndPoint.Address}:{e.RemoteEndPoint.Port}");
        }

        /// <summary>
        /// Handles the ErrorOccurred event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientErrorEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_ErrorOccurred(Object sender,
                                                        ClientErrorEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} ErrorOccurred, Exception {e.Exception}");
        }

        /// <summary>
        /// Handles the Reconnecting event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientReconnectingEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Reconnecting(Object sender,
                                                       ClientReconnectingEventArgs e)
        {
            this.Trace($"Connection {e.Connection.ConnectionName} Reconnecting");
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
        /// Gets the serialised data from event.
        /// </summary>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <returns></returns>
        private String GetSerialisedDataFromEvent(ResolvedEvent resolvedEvent)
        {
            return Encoding.Default.GetString(resolvedEvent.Event.Data, 0, resolvedEvent.Event.Data.Length);
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
        /// Publishes the message.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="content">The content.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="Exception"></exception>
        private async Task PublishMessage(String url,
                                          String content,
                                          Guid eventId,
                                          CancellationToken cancellationToken)
        {
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                using(HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                    this.Trace($"About to Send Event Id {eventId}");

                    using(HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(request, cancellationToken))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            String responseBody = await responseMessage.Content.ReadAsStringAsync();
                            HttpStatusCode statusCode = responseMessage.StatusCode;

                            //We create a nicely formatted string for our Exception, showing the HTTP Status Code and any response body.
                            //At this stage, we have decided to add this message to the CommunicationFailureException and Inner Exception.
                            String errorMessage = $"Request failed [{statusCode}]. Response [{responseBody}]";

                            throw new Exception(errorMessage);
                        }
                    }

                    this.Trace($"Finished Sending Event Id {eventId}");
                }
            }
            catch(Exception e)
            {
                // Cancel the call to the server
                linkedTokenSource.Cancel();
                this.Trace($"Cancelled Processing of Event Id {eventId}");
                throw;
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
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace);
            }
        }

        /// <summary>
        /// Traces the specified connection name.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="trace">The trace.</param>
        private void Trace(IEventStoreConnection connection,
                           String trace)
        {
            this.Trace($"{connection.ConnectionName} : {trace}");
        }

        /// <summary>
        /// Traces the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void Trace(Exception exception)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(exception.Message);
                if (exception.InnerException != null)
                {
                    this.Trace(exception.InnerException);
                }
            }
        }

        #endregion
    }
}