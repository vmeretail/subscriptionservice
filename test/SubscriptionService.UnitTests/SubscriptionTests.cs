namespace SubscriptionService.UnitTests
{
    using System;
    using Configuration;
    using Xunit;
    using Shouldly;

    public class SubscriptionTests
    {
        [Fact]
        public void Subscription_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            String streamName = "$ce-Sales";
            String groupName = "Read Model";
            String url = @"127.0.0.1/api/events";

            // 2. Act
            Subscription subscription = Subscription.Create(streamName, groupName, url);

            // 3. Assert
            subscription.ShouldNotBeNull();

            subscription.StreamName.ShouldBe(streamName);
            subscription.GroupName.ShouldBe(groupName);
            subscription.EndPointUrl.ShouldBe(url);
        } 
    }
}