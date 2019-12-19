namespace SubscriptionService.UnitTests
{
    using System;
    using Configuration;
    using Factories;
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
            subscription.EndPointUri.AbsoluteUri.ShouldBe(TestData.Url);
        }

        [Fact]
        public void Subscription_EndpointAsString_SubscriptionCreated()
        {
            // 2. Act
            Subscription subscription = Subscription.Create(TestData.StreamName, TestData.GroupName, TestData.Url);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.StreamName.ShouldBe(TestData.StreamName);
            subscription.GroupName.ShouldBe(TestData.GroupName);
            subscription.EndPointUri.AbsoluteUri.ShouldBe(TestData.Url);
        }

        [Fact]
        public void Subscription_EndpointAsUrl_SubscriptionCreated()
        {
            // 1. Arrange
            Uri endpointUri = new Uri(TestData.Url);

            // 2. Act
            Subscription subscription = Subscription.Create(TestData.StreamName, TestData.GroupName, endpointUri);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.StreamName.ShouldBe(TestData.StreamName);
            subscription.GroupName.ShouldBe(TestData.GroupName);
            subscription.EndPointUri.Equals(endpointUri).ShouldBeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Subscription_EndpointString_InvalidValue_ErrorIsThrown(String endpointUrl)
        {
            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(TestData.StreamName, TestData.GroupName, endpointUrl));
        }

        [Fact]
        public void Subscription_EndpointUri_InvalidValue_ErrorIsThrown()
        {
            // 1. Arrange
            Uri endpointUri = null;

            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(TestData.StreamName, TestData.GroupName, endpointUri));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Subscription_GroupName_InvalidValue_ErrorIsThrown(String groupName)
        {
            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(TestData.StreamName, groupName, TestData.Url));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Subscription_StreamName_InvalidValue_ErrorIsThrown(String streamName)
        {
            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(streamName, TestData.GroupName, TestData.Url));
        }

        #endregion
    }
}