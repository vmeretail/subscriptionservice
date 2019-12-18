namespace SubscriptionService.UnitTests
{
    using System;
    using Configuration;
    using Shouldly;
    using Xunit;

    public class SubscriptionTests
    {
        #region Methods

        [Fact]
        public void Subscription_CanBeCreated_IsCreated()
        {
            // 2. Act
            Subscription subscription = Subscription.Create(TestData.StreamName, TestData.GroupName, TestData.Url);

            // 3. Assert
            subscription.ShouldNotBeNull();

            subscription.StreamName.ShouldBe(TestData.StreamName);
            subscription.GroupName.ShouldBe(TestData.GroupName);
            subscription.EndPointUrl.ShouldBe(TestData.Url);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData("", null)]
        [InlineData("", "")]
        [InlineData(" ", "")]
        [InlineData("", " ")]
        [InlineData(" ", " ")]
        public void Subscription_InvalidValuesPassIn_ErrorIsThrown(String streamName,
                                                                   String groupName)
        {
            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(streamName, groupName, TestData.Url));
        }

        #endregion
    }
}