namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shouldly;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TestsFixture : IDisposable
    {
        #region Methods

        /// <summary>
        /// Checks the events.
        /// </summary>
        /// <param name="eventIds">The event ids.</param>
        /// <param name="endpointUrl">The endpoint URL.</param>
        /// <param name="readmodelHttpClient">The readmodel HTTP client.</param>
        public async Task CheckEvents(List<Guid> eventIds,
                                      String endpointUrl,
                                      HttpClient readmodelHttpClient)
        {
            List<Guid> foundEvents = new List<Guid>();

            await Retry.For(async () =>
                            {
                                HttpResponseMessage responseMessage = await readmodelHttpClient.GetAsync(endpointUrl, CancellationToken.None);

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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //Global teardown
        }

        /// <summary>
        /// Handles the Closed event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientClosedEventArgs" /> instance containing the event data.</param>
        public void EventStoreConnection_Closed(Object sender,
                                                ClientClosedEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Closed [{e.Reason}]");
        }

        /// <summary>
        /// Handles the Connected event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs" /> instance containing the event data.</param>
        public void EventStoreConnection_Connected(Object sender,
                                                   ClientConnectionEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Connected");
        }

        /// <summary>
        /// Handles the ErrorOccurred event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientErrorEventArgs" /> instance containing the event data.</param>
        public void EventStoreConnection_ErrorOccurred(Object sender,
                                                       ClientErrorEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Error Occurred [{e.Exception}]");
        }

        /// <summary>
        /// Handles the Reconnecting event of the EventStoreConnection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientReconnectingEventArgs" /> instance containing the event data.</param>
        public void EventStoreConnection_Reconnecting(Object sender,
                                                      ClientReconnectingEventArgs e)
        {
            this.LogMessageToTrace($"Connection {e.Connection.ConnectionName} Reconnecting");
        }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);

            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient();

            IHttpClientFactory httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

            HttpClient client = httpClientFactory.CreateClient();

            return client;
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
        /// <param name="endpointUrl">The endpoint URL.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public async Task PostEventToEventStore(Object eventData,
                                                Guid eventId,
                                                String endpointUrl,
                                                HttpClient httpClient)
        {
            Console.WriteLine($"PostEventToEventStore - uri is [{endpointUrl}]");

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
            requestMessage.Headers.Add("ES-EventType", eventData.GetType().Name);
            requestMessage.Headers.Add("ES-EventId", eventId.ToString());
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(eventData), Encoding.UTF8, "application/json");

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            Console.WriteLine(responseMessage.StatusCode);

            responseMessage.EnsureSuccessStatusCode();
        }

        #endregion
    }
}