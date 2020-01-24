namespace SubscriptionService.UnitTests
{
    using System;
    using System.Dynamic;
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

        #endregion
    }
}