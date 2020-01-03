namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shouldly;

    public class TestsFixture : IDisposable
    {
        #region Constructors

        public TestsFixture()
        {
            // Do "global" initialization here; Only called once.
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

        #region Properties

        /// <summary>
        /// The docker helper
        /// </summary>
        public DockerHelper DockerHelper { get; }

        /// <summary>
        /// The event store connection
        /// </summary>
        public IEventStoreConnection EventStoreConnection { get; }

        /// <summary>
        /// The event store HTTP address
        /// </summary>
        public String EventStoreHttpAddress { get; }

        /// <summary>
        /// The event store HTTP client
        /// </summary>
        public HttpClient EventStoreHttpClient { get; }

        /// <summary>
        /// The read model HTTP client
        /// </summary>
        public HttpClient ReadModelHttpClient { get; }

        #endregion

        #region Methods

        public async Task CheckEvents(List<Guid> eventIds)
        {
            String endPointUrl = $"http://localhost:{this.DockerHelper.DummyRESTHttpPort}/events";
            List<Guid> foundEvents = new List<Guid>();

            await Retry.For(async () =>
                            {
                                HttpResponseMessage responseMessage = await this.ReadModelHttpClient.GetAsync(endPointUrl, CancellationToken.None);
                                String responseContent = await responseMessage.Content.ReadAsStringAsync();

                                if (String.IsNullOrEmpty(responseContent))
                                {
                                    throw new Exception();
                                }

                                this.LogMessageToTrace(responseContent);

                                List<String> retrievedEvents = JArray.Parse(responseContent).Select(x => x["EventId"].Value<String>()).ToList();

                                if (retrievedEvents.Any() == false)
                                {
                                    throw new Exception();
                                }

                                foundEvents.AddRange(retrievedEvents.Select(x => Guid.Parse(x)));

                                Boolean matched = eventIds.Count == foundEvents.Count && eventIds.Intersect(foundEvents).Count() == eventIds.Count;

                                matched.ShouldBeTrue();
                            });
        }

        public void Dispose()
        {
            // Do "global" teardown here; Only called once.
            this.EventStoreConnection.Close();

            this.DockerHelper.StopContainersForScenarioRun();
        }

        /// <summary>
        /// Logs the message to trace.
        /// </summary>
        /// <param name="traceMessage">The trace message.</param>
        public void LogMessageToTrace(String traceMessage)
        {
            Console.WriteLine(traceMessage);
        }

        /// <summary>
        /// Posts the event to event store.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="streamName">Name of the stream.</param>
        public async Task PostEventToEventStore(Object eventData,
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
        /// <returns></returns>
        private HttpClient GetHttpClient()
        {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient();

            IHttpClientFactory httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

            HttpClient client = httpClientFactory.CreateClient();

            return client;
        }

        #endregion
    }
}