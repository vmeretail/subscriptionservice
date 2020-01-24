namespace SubscriptionService.IntegrationTests
{
    using System;
    using System.Dynamic;
    using System.Text;
    using EventStore.ClientAPI;
    using Factories;
    using Newtonsoft.Json;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="SubscriptionService.Factories.IEventFactory" />
    internal class TestEventFactory : IEventFactory
    {
        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="recordedEvent">The recorded event.</param>
        /// <returns></returns>
        public String ConvertFrom(RecordedEvent recordedEvent)
        {
            String serialisedData = Encoding.Default.GetString(recordedEvent.Data, 0, recordedEvent.Data.Length);
            dynamic expandoObject = new ExpandoObject();

            var temp = JsonConvert.DeserializeAnonymousType(serialisedData, expandoObject);

            //Add our new field in
            temp.EventId = recordedEvent.EventId;

            return JsonConvert.SerializeObject(temp);

        }
    }
}