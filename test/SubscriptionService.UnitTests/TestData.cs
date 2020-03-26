namespace SubscriptionService.UnitTests
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// </summary>
    public class TestData
    {
        #region Fields

        public static String CatchupSubscriptionName1 = "Catchup Subscription 1";

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


        #endregion
    }
}