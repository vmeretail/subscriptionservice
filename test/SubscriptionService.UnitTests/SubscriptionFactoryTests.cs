namespace SubscriptionService.UnitTests
{
    using System;
    using System.Linq;
    using Configuration;
    using Factories;
    using Shouldly;
    using Xunit;
    using Subscription = global::SubscriptionService.Domain.Subscription;

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
            Subscription subscription = subscriptionFactory.CreateFrom(subscriptionConfiguration);

            // 3. Assert
            subscription.ShouldNotBeNull();

            subscription.HttpClient.ShouldNotBeNull();
            subscription.EndPointUri.AbsolutePath.ShouldBe(subscriptionConfiguration.EndPointUri.AbsolutePath);
            subscription.GroupName.ShouldBe(subscriptionConfiguration.GroupName);
            subscription.StreamName.ShouldBe(subscriptionConfiguration.StreamName);
            subscription.MaxRetryCount.ShouldBe(subscriptionConfiguration.MaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(subscriptionConfiguration.NumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(subscriptionConfiguration.StreamStartPosition);
        }

        [Fact]
        public void SubscriptionFactory_NullPassedAsSubscription_ErrorThrown()
        {
            // 1. Arrange
            SubscriptionFactory subscriptionFactory = new SubscriptionFactory();

            // 3. Act & Assert
            Should.Throw<ArgumentNullException>(() => { subscriptionFactory.CreateFrom(null); });
        }
    }
}