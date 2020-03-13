namespace SubscriptionService.UnitTests
{
    using System;
    using System.Text;
    using Factories;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class TestEventFactory : IEventFactory
    {
        #region Methods

        /// <summary>
        /// Converts from.
        /// </summary>
        /// <param name="persistedEvent">The persisted event.</param>
        /// <returns></returns>
        public String ConvertFrom(PersistedEvent persistedEvent)
        {
            String json = Encoding.Default.GetString(persistedEvent.Data);

            JObject jObject = JObject.Parse(json);

            jObject.Add("EventId", new JValue(persistedEvent.EventId));

            return JsonConvert.SerializeObject(jObject);
        }

        #endregion
    }
}