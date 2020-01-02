namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    /// <summary>
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class PersistentSubscriptionsTests : IDisposable
    {
        #region Fields

        /// <summary>
        /// The docker helper
        /// </summary>
        private readonly DockerHelper DockerHelper;

        /// <summary>
        /// The event store connection
        /// </summary>
        private readonly IEventStoreConnection EventStoreConnection;

        /// <summary>
        /// The event store HTTP address
        /// </summary>
        private readonly String EventStoreHttpAddress;

        /// <summary>
        /// The event store HTTP client
        /// </summary>
        private readonly HttpClient EventStoreHttpClient;

        /// <summary>
        /// The read model HTTP client
        /// </summary>
        private readonly HttpClient ReadModelHttpClient;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscriptionsTests" /> class.
        /// </summary>
        public PersistentSubscriptionsTests()
        {
            Console.WriteLine("In the ctor");

            this.DockerHelper = new DockerHelper();

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun();

            this.EventStoreHttpAddress = $"http://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/streams";

            this.EventStoreHttpClient = this.GetHttpClient();
            this.EventStoreHttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:changeit")));
            this.ReadModelHttpClient = this.GetHttpClient();

            // Build the Event Store Connection String 
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            // Setup the Event Store Connection
            this.EventStoreConnection = EventStore.ClientAPI.EventStoreConnection.Create(connectionString);

            this.EventStoreConnection.Connected += this.EventStoreConnection_Connected;
            this.EventStoreConnection.Closed += this.EventStoreConnection_Closed;
            this.EventStoreConnection.ErrorOccurred += this.EventStoreConnection_ErrorOccurred;
            this.EventStoreConnection.Reconnecting += this.EventStoreConnection_Reconnecting;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.DockerHelper.StopContainersForScenarioRun();

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Persistents the subscriptions event delivery event is delivered.
        /// </summary>
        [Fact]
        public async Task PersistentSubscriptions_EventDelivery_EventIsDelivered()
        {
            // 1. Arrange
            String aggregateName = "SalesTransactionAggregate";
            Guid aggregateId = Guid.NewGuid();
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            // Setup some dummy events in the Event Store
            var sale = new
                       {
                           AggregateId = aggregateId,
                           EventId = Guid.NewGuid()
                       };
            await this.PostEventToEventStore(sale, sale.EventId, streamName);

            String endPointUrl = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events";

            // Setup a subscription configuration to deliver the events to the dummy REST
            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(Subscription.Create(streamName, "TestGroup", endPointUrl));

            await this.EventStoreConnection.ConnectAsync();

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(subscriptionList, this.EventStoreConnection);
            subscriptionService.TraceGenerated += this.SubscriptionService_TraceGenerated;

            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            // Do a GET on the dummy API to check if events have been delivered
            await Retry.For(async () =>
                            {
                                HttpResponseMessage responseMessage = await this.ReadModelHttpClient.GetAsync(endPointUrl, CancellationToken.None);
                                String responseContent = await responseMessage.Content.ReadAsStringAsync();
                                if (String.IsNullOrEmpty(responseContent))
                                {
                                    throw new Exception();
                                }

                                this.LogMessageToTrace(responseContent);

                                responseContent.Contains(sale.EventId.ToString()).ShouldBeTrue();
                            });

            // 4. Cleanup
            await subscriptionService.Stop(CancellationToken.None);
            this.EventStoreConnection.Close();
        }

        /// <summary>
        /// Handles the Closed event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientClosedEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Closed(Object sender,
                                                 ClientClosedEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Closed [{e.Reason}]");
        }

        /// <summary>
        /// Handles the Connected event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Connected(Object sender,
                                                    ClientConnectionEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Connected");
        }

        /// <summary>
        /// Handles the ErrorOccurred event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientErrorEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_ErrorOccurred(Object sender,
                                                        ClientErrorEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Error Occurred [{e.Exception}]");
        }

        /// <summary>
        /// Handles the Reconnecting event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientReconnectingEventArgs" /> instance containing the event data.</param>
        private void EventStoreConnection_Reconnecting(Object sender,
                                                       ClientReconnectingEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Reconnecting");
        }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <param name="baseAddress">The base address.</param>
        /// <returns></returns>
        private HttpClient GetHttpClient()
        {
            //IServiceCollection services = new ServiceCollection();

            //services.AddHttpClient();

            //Setup.HttpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

            HttpClient client = new HttpClient();

            return client;
        }

        /// <summary>
        /// Logs the message to trace.
        /// </summary>
        /// <param name="traceMessage">The trace message.</param>
        private void LogMessageToTrace(String traceMessage)
        {
            Console.WriteLine(traceMessage);
        }

        /// <summary>
        /// Posts the event to event store.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="streamName">Name of the stream.</param>
        private async Task PostEventToEventStore(Object eventData,
                                                 Guid eventId,
                                                 String streamName)
        {
            String uri = $"{this.EventStoreHttpAddress}/{streamName}";

            Console.WriteLine($"PostEventToEventStore - uri is [{uri}]");

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Headers.Add("ES-EventType", eventData.GetType().Name);
            requestMessage.Headers.Add("ES-EventId", eventId.ToString());
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(eventData), Encoding.UTF8, "application/json");

            HttpResponseMessage responseMessage = await this.EventStoreHttpClient.SendAsync(requestMessage);

            Console.WriteLine(responseMessage.StatusCode);

            responseMessage.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Subscriptions the service trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void SubscriptionService_TraceGenerated(String trace)
        {
            this.LogMessageToTrace(trace);
        }

        #endregion
    }
}