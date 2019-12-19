namespace SubscriptionService.UnitTests
{
    using System;
    using System.Linq;
    using Configuration;
    using Factories;
    using Shouldly;
    using Xunit;

    public class SubscriptionFactoryTests
    {
        [Fact]
        public void SubscriptionFactory_CanBeCreated_IsCreated()
        {
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            subscriptionFactory.ShouldNotBeNull();
        }

        [Fact]
        public void SubscriptionFactory_SubscriptionCanBeCreated_IsCreated()
        {
            // 1. Arrange
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();
            var subscriptionConfiguration = TestData.Subscriptions.First();

            // 2. Act
            var subscription = subscriptionFactory.CreateFrom(subscriptionConfiguration);

            // 3. Assert
            subscription.ShouldNotBeNull();

            subscription.GroupName.ShouldBe(subscriptionConfiguration.GroupName);
            subscription.StreamName.ShouldBe(subscriptionConfiguration.StreamName);
            subscription.EndPointUri.ShouldBe(subscriptionConfiguration.EndPointUri);
            subscription.HttpClient.ShouldNotBeNull();
        }

        [Fact]
        public void SubscriptionFactory_NullPassedAsSusbcription_ErrorThrown()
        {
            // 1. Arrange
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            // 3. Act & Assert
            Should.Throw<ArgumentNullException>(() => { subscriptionFactory.CreateFrom(null); });
        }
    }
}