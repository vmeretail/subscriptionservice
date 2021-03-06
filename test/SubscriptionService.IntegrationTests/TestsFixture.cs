﻿namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;
    using NLog.Targets;
    using Shouldly;
    using Xunit.Abstractions;

    /// <summary>
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TestsFixture : IDisposable
    {
        #region Fields

        /// <summary>
        /// The event store docker configuration
        /// </summary>
        public EventStoreDockerConfiguration EventStoreDockerConfiguration;

        /// <summary>
        /// The log directory
        /// </summary>
        public String LogDirectory;

        /// <summary>
        /// The logger
        /// </summary>
        private Logger Logger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestsFixture" /> class.
        /// </summary>
        public TestsFixture()
        {
            this.Logger = LogManager.GetLogger("SubscriptionService");

            // Get the file target from nlog config
            FileTarget fileTarget = LogManager.Configuration.FindTargetByName<FileTarget>("logfile");

            // Get the filename
            LogEventInfo logEventInfo = new LogEventInfo
                                        {
                                            TimeStamp = DateTime.Now
                                        };

            String fileName = fileTarget.FileName.Render(logEventInfo);
            FileInfo fi = new FileInfo(fileName);

            // Make sure the trace directory exists
            Directory.CreateDirectory(fi.DirectoryName);

            this.LogDirectory = fi.DirectoryName;
        }

        #endregion

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
            this.LogMessageToTrace($"CheckEvents - looking for {eventIds.Count} events");

            foreach (Guid eventId in eventIds)
            {
                this.LogMessageToTrace($"Event Id [{eventId}]");
            }

            await Retry.For(async () =>
                            {
                                List<Guid> foundEvents = new List<Guid>();

                                HttpResponseMessage responseMessage = await readmodelHttpClient.GetAsync(endpointUrl, CancellationToken.None);

                                String responseContent = await responseMessage.Content.ReadAsStringAsync();

                                if (String.IsNullOrEmpty(responseContent))
                                {
                                    throw new Exception();
                                }

                                this.LogMessageToTrace($"Response from endpoint [{endpointUrl}] is [{responseContent}]");

                                JArray jsonArray = JArray.Parse(responseContent);

                                List<String> retrievedEvents = jsonArray.Select(x => x["EventId"].Value<String>()).ToList();

                                this.LogMessageToTrace($"Found {retrievedEvents.Count} events");

                                if (retrievedEvents.Any() == false)
                                {
                                    throw new Exception("No events returned");
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
            this.LogMessageToTrace("Test teardown");
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
        /// Generates the name of the test.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <returns></returns>
        public String GenerateTestName(ITestOutputHelper output)
        {
            Type type = output.GetType();
            FieldInfo testMember = type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
            ITest test = (ITest)testMember.GetValue(output);
            String testName = test.DisplayName.Split(".").Last(); //Make the name a little more readable.

            testName = testName.Replace("(", "");
            testName = testName.Replace(")", "");
            testName = testName.Replace(":", "_");
            testName = testName.Replace(",", "_");
            testName = testName.Replace(" ", "");

            return testName;
        }

        /// <summary>
        /// Gets the event.
        /// </summary>
        /// <param name="endpointUrl">The endpoint URL.</param>
        /// <param name="readmodelHttpClient">The readmodel HTTP client.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<String> GetEvent(String endpointUrl,
                                           HttpClient readmodelHttpClient,
                                           Int32 id)
        {
            String eventAsString = null;

            await Retry.For(async () =>
                            {
                                HttpResponseMessage responseMessage = await readmodelHttpClient.GetAsync(endpointUrl + $"/{id}", CancellationToken.None);

                                responseMessage.EnsureSuccessStatusCode();

                                String responseContent = await responseMessage.Content.ReadAsStringAsync();

                                if (String.IsNullOrEmpty(responseContent))
                                {
                                    throw new Exception();
                                }

                                eventAsString = responseContent;
                            });

            return eventAsString;
        }

        /// <summary>
        /// Gets the events.
        /// </summary>
        /// <param name="endpointUrl">The endpoint URL.</param>
        /// <param name="readmodelHttpClient">The readmodel HTTP client.</param>
        /// <param name="expectedEventCount">The expected event count.</param>
        /// <returns></returns>
        public async Task<List<String>> GetEvents(String endpointUrl,
                                                  HttpClient readmodelHttpClient,
                                                  Int32 expectedEventCount)
        {
            List<String> eventsAsStrings = null;

            await Retry.For(async () =>
                            {
                                HttpResponseMessage responseMessage = await readmodelHttpClient.GetAsync(endpointUrl, CancellationToken.None);

                                responseMessage.EnsureSuccessStatusCode();

                                String responseContent = await responseMessage.Content.ReadAsStringAsync();

                                if (String.IsNullOrEmpty(responseContent))
                                {
                                    throw new Exception();
                                }

                                JArray array = JArray.Parse(responseContent);

                                array.Count.ShouldBe(expectedEventCount);

                                eventsAsStrings = array.ToObject<List<Object>>().Select(x => x.ToString()).ToList();
                            });

            return eventsAsStrings;
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

            Logger logger = LogManager.GetLogger("SubscriptionService");

            // Write the log
            logger.Info(traceMessage);
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
            await Retry.For(async () =>
                            {
                                this.LogMessageToTrace($"PostEventToEventStore - uri is [{endpointUrl}]");

                                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
                                requestMessage.Headers.Add("ES-EventType", eventData.GetType().Name);
                                requestMessage.Headers.Add("ES-EventId", eventId.ToString());
                                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(eventData), Encoding.UTF8, "application/json");

                                HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

                                this.LogMessageToTrace($"{responseMessage.StatusCode}");

                                responseMessage.EnsureSuccessStatusCode();
                            },
                            TimeSpan.FromSeconds(30),
                            TimeSpan.FromSeconds(10));
        }

        #endregion
    }
}