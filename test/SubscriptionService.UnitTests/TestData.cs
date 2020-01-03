namespace SubscriptionService.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Configuration;
    using EventStore.ClientAPI;

    /// <summary>
    /// </summary>
    public class TestData
    {
        #region Fields

        /// <summary>
        /// The group name
        /// </summary>
        public static String GroupName = "Read Model";
        
        /// <summary>
        /// The maximum retry count
        /// </summary>
        public static Int32 MaxRetryCount = 15;
        
        /// <summary>
        /// The number of concurrent messages
        /// </summary>
        public static Int32 NumberOfConcurrentMessages = 25;

        /// <summary>
        /// The stream name
        /// </summary>
        public static String StreamName = "$ce-Sales";
        
        /// <summary>
        /// The stream start position
        /// </summary>
        public static Int32 StreamStartPosition = 50;

        /// <summary>
        /// The URL
        /// </summary>
        public static String Url = @"http://127.0.0.1/api/events/";

        /// <summary>
        /// The subscriptions
        /// </summary>
        public static List<Subscription> Subscriptions = new List<Subscription>
                                                         {
                                                             Subscription.Create(TestData.StreamName,
                                                                                 TestData.GroupName,
                                                                                 TestData.Url,
                                                                                 TestData.NumberOfConcurrentMessages,
                                                                                 TestData.MaxRetryCount,
                                                                                 TestData.StreamStartPosition)
                                                         };

        #endregion
    }
}