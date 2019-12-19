namespace SubscriptionService.UnitTests
{
    using System;
    using System.Collections.Generic;
    using Configuration;

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
        /// The stream name
        /// </summary>
        public static String StreamName = "$ce-Sales";

        /// <summary>
        /// The subscriptions
        /// </summary>
        public static List<Subscription> Subscriptions = new List<Subscription>();

        /// <summary>
        /// The URL
        /// </summary>
        public static String Url = @"http://127.0.0.1/api/events/";

        #endregion
    }
}