namespace SubscriptionService.UnitTests
{
    using System;
    using System.Dynamic;
    using System.Text;
    using Factories;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    public class EventFactoryTests
    {
        #region Methods

        [Fact]
        public void EventFactory_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            IEventFactory eventFactory = EventFactory.Create();

            eventFactory.ShouldNotBeNull();
        }

        [Fact]
        public void EventFactory_ConvertPersistedEvent_SerialisedDateReturned()
        {
            // 1. Arrange
            IEventFactory eventFactory = EventFactory.Create();

            DateTime eventCreateDateTime = DateTime.Now;
            Int64 createdEpoch = (Int32)(eventCreateDateTime - new DateTime(1970, 1, 1)).TotalSeconds;
            String serialisedEvent =
                "{\"operatorId\": 19, \"tillNumber\": 3, \"transactionId\": \"293064\",  \"transactionNumber\": 141,  \"organisationId\": \"141f5583-c163-4b97-9c7e-8e4b371b21e2\",\r\n  \"storeId\": \"bd9a1973-e7c8-4658-8dd6-325b87876c99\",\r\n  \"aggregateId\": \"d547edae-2cd9-4d5a-8eb4-371045c79567\"\r\n}";
            Byte[] data = Encoding.Default.GetBytes(serialisedEvent);
            Guid eventId = Guid.NewGuid();
            Int64 eventNumber = 0;
            String eventStreamId = "TestStream1";
            String eventType = "Sale";

            PersistedEvent persistedEvent1 = PersistedEvent.Create(eventCreateDateTime, createdEpoch, data, eventId, eventNumber, eventStreamId, eventType, true, null);

            // 2. Act
            var serialisedString = eventFactory.ConvertFrom(persistedEvent1);

            // 3. Assert
            serialisedString.ShouldNotBeNullOrEmpty();
        }

        #endregion
    }
}