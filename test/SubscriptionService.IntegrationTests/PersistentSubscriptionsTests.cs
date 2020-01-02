using System;
using System.Collections.Generic;
using System.Text;

namespace SubscriptionService.IntegrationTests
{
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Ductus.FluentDocker.Model.Containers;
    using EventStore.ClientAPI;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Sdk;

    public class PersistentSubscriptionsTests
    {
        private HttpClient HttpClient;

        private String EventStoreHttpAddress;

        private DockerHelper DockerHelper;
        public PersistentSubscriptionsTests()
        {
            this.DockerHelper = new DockerHelper();

            // Start the Event Store & Dummy API
            this.DockerHelper.StartContainersForScenarioRun();

            this.EventStoreHttpAddress = $"http://127.0.0.1:{this.DockerHelper.EventStoreHttpPort}/streams";

            this.HttpClient = GetHttpClient(this.EventStoreHttpAddress, "admin", "changeit");
        }

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

            // Build the Event Store Connection String 
            String connectionString = $"ConnectTo=tcp://admin:changeit@127.0.0.1:{this.DockerHelper.EventStoreTcpPort};VerboseLogging=true;";

            // Setup the Event Store Connection
            IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
            await eventStoreConnection.ConnectAsync();

            eventStoreConnection.Connected += EventStoreConnection_Connected;
            eventStoreConnection.Closed += EventStoreConnection_Closed;
            eventStoreConnection.ErrorOccurred += EventStoreConnection_ErrorOccurred;
            eventStoreConnection.Reconnecting += EventStoreConnection_Reconnecting;

            // Create instance of the Subscription Service
            SubscriptionService subscriptionService = new SubscriptionService(subscriptionList, eventStoreConnection);
            subscriptionService.TraceGenerated += SubscriptionService_TraceGenerated;
            // 2. Act
            // Start the subscription service
            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            // Do a GET on the dummy API to check if events have been delivered

            // Check the counts are expected

            // 4. Cleanup (TODO: move to after scenario)
            await subscriptionService.Stop(CancellationToken.None);
            this.DockerHelper.StopContainersForScenarioRun();
        }

        private void LogMessageToTrace(String traceMessage)
        {
            using (StreamWriter sw = new StreamWriter(@"c:\temp\debug.log", true))
            {
                sw.WriteLine(traceMessage);
            }
        }

        private void EventStoreConnection_Reconnecting(object sender, ClientReconnectingEventArgs e)
        {
            LogMessageToTrace($"Connection {e.Connection.ConnectionName} Reconnecting");
        }

        private void EventStoreConnection_ErrorOccurred(object sender, ClientErrorEventArgs e)
        {
            LogMessageToTrace($"Connection {e.Connection.ConnectionName} Error Occurred [{e.Exception}]");
        }

        private void EventStoreConnection_Closed(object sender, ClientClosedEventArgs e)
        {
            LogMessageToTrace($"Connection {e.Connection.ConnectionName} Closed [{e.Reason}]");
        }

        private void EventStoreConnection_Connected(object sender, ClientConnectionEventArgs e)
        {
            LogMessageToTrace($"Connection {e.Connection.ConnectionName} Connected");
        }

        private void SubscriptionService_TraceGenerated(string trace)
        {
            LogMessageToTrace(trace);
        }

        private HttpClient GetHttpClient(String eventStoreAddress,
                                         String username,
                                         String password)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(eventStoreAddress);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                                                                       Convert
                                                                                           .ToBase64String(Encoding
                                                                                                           .ASCII
                                                                                                           .GetBytes($"{username}:{password}")));

            return client;
        }

        private async Task PostEventToEventStore(Object eventData, Guid eventId, String streamName)
        {
            String uri = $"{this.HttpClient.BaseAddress}/{streamName}";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Headers.Add("ES-EventType", eventData.GetType().Name);
            requestMessage.Headers.Add("ES-EventId", eventId.ToString());
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(eventData), Encoding.UTF8, "application/json");

            HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(requestMessage);

            responseMessage.EnsureSuccessStatusCode();
        }

        private async Task PostSaleToEventStore(String eventStoreAddress,
                                                String aggregateName,
                                                Guid aggregateId,
                                                Int32 numberofLines = 1)
        {
            String streamName = $"{aggregateName}-{aggregateId.ToString("N")}";

            // POST the sale started event
            SaleStartedEvent saleStartedEvent = new SaleStartedEvent();
            saleStartedEvent.AggregateId = aggregateId;
            saleStartedEvent.EventDateTime = DateTime.Now;
            saleStartedEvent.EventId = Guid.NewGuid();

            await this.PostEventToEventStore(saleStartedEvent, saleStartedEvent.EventId, streamName);
        }


        private void GetEventsFromEndpoint(String endPointUrl)
        {
            
        }
    }

    public class SaleStartedEvent
    {
        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }

        public DateTime EventDateTime { get; set; }
    }

    public class SaleLineAddedEvent
    {
        public Guid AggregateId { get; set; }

        public DateTime EventDateTime { get; set; }

        public Int32 Quantity { get; set; }

        public Decimal UnitCost { get; set; }

        public Decimal TotalCost { get; set; }
    }

    public class SaleCompletedEvent
    {
        public Guid AggregateId { get; set; }

        public DateTime EventDateTime { get; set; }

        public Int32 Quantity { get; set; }
        public Decimal TotalValue { get; set; }
    }
}
