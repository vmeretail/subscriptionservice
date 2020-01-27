namespace Core3WorkerServiceExample
{
    using System;
    using System.Dynamic;
    using System.Text;
    using Newtonsoft.Json;
    using SubscriptionService.Factories;

    internal class WorkerEventFactory : IEventFactory
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
            dynamic expandoObject = new ExpandoObject();
            dynamic temp = JsonConvert.DeserializeAnonymousType(json, expandoObject);

            //Add our new field in
            temp.EventId = persistedEvent.EventId;
            temp.EventType = persistedEvent.EventType;
            temp.EventDateTime = persistedEvent.Created;
            temp.Metadata = Encoding.Default.GetString(persistedEvent.Metadata);

            return JsonConvert.SerializeObject(temp);
        }

        #endregion
    }
}