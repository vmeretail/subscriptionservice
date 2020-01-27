namespace SubscriptionService.Factories
{
    using System;

    /// <summary>
    /// </summary>
    public class PersistedEvent
    {
        #region Fields

        /// <summary>
        /// The created
        /// </summary>
        public readonly DateTime Created;

        /// <summary>
        /// The created epoch
        /// </summary>
        public readonly Int64 CreatedEpoch;

        /// <summary>
        /// The data
        /// </summary>
        public readonly Byte[] Data;

        /// <summary>
        /// The event identifier
        /// </summary>
        public readonly Guid EventId;

        /// <summary>
        /// The event number
        /// </summary>
        public readonly Int64 EventNumber;

        /// <summary>
        /// The event stream identifier
        /// </summary>
        public readonly String EventStreamId;

        /// <summary>
        /// The event type
        /// </summary>
        public readonly String EventType;

        /// <summary>
        /// The is json
        /// </summary>
        public readonly Boolean IsJson;

        /// <summary>
        /// The metadata
        /// </summary>
        public readonly Byte[] Metadata;

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="PersistedEvent" /> class from being created.
        /// </summary>
        private PersistedEvent(DateTime created,
                               Int64 createdEpoch,
                               Byte[] data,
                               Guid eventId,
                               Int64 eventNumber,
                               String eventStreamId,
                               String eventType,
                               Boolean isJson,
                               Byte[] metadata)
        {
            this.Created = created;
            this.CreatedEpoch = createdEpoch;
            this.Data = data;
            this.EventId = eventId;
            this.EventNumber = eventNumber;
            this.EventStreamId = eventStreamId;
            this.EventType = eventType;
            this.IsJson = isJson;
            this.Metadata = metadata;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns></returns>
        public static PersistedEvent Create(DateTime created,
                                            Int64 createdEpoch,
                                            Byte[] data,
                                            Guid eventId,
                                            Int64 eventNumber,
                                            String eventStreamId,
                                            String eventType,
                                            Boolean isJson,
                                            Byte[] metadata)
        {
            return new PersistedEvent(created, createdEpoch, data, eventId, eventNumber, eventStreamId, eventType, isJson, metadata);
        }

        #endregion
    }
}