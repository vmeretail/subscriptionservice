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

        public String UserName { get; set; }

        public String Password { get; set; }
        public String RegistryUrl { get; set; }

        public Boolean IsLegacyVersion
        {
            get
            {
                return !Tag.Contains("6.");
            }
        }

        #endregion
    }
}