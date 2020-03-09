namespace SubscriptionService.IntegrationTests
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public class EventStoreDockerConfiguration
    {
        #region Properties

        /// <summary>
        /// Gets or sets the registry.
        /// </summary>
        /// <value>
        /// The registry.
        /// </value>
        public String Registry { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        public String Tag { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is legacy version.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is legacy version; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsLegacyVersion
        {
            get
            {
                return false;//!Tag.Contains("6.");
            }
        }

        #endregion
    }
}