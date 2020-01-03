namespace SubscriptionService.UnitTests
{
    using System;
    using System.Runtime.InteropServices;
    using Configuration;
    using EventStore.ClientAPI;
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
            subscription.MaxRetryCount.ShouldBe(Subscription.DefaultMaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(Subscription.DefaultNumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(Subscription.DefaultStreamStartPosition);

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
            subscription.MaxRetryCount.ShouldBe(Subscription.DefaultMaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(Subscription.DefaultNumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(Subscription.DefaultStreamStartPosition);
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
            subscription.MaxRetryCount.ShouldBe(Subscription.DefaultMaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(Subscription.DefaultNumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(Subscription.DefaultStreamStartPosition);
        }

        [Fact]
        public void Subscription_OptionalValuesPassed_EndpointAsString_SubscriptionCreated()
        {
            // 2. Act
            Subscription subscription = Subscription.Create(TestData.StreamName, TestData.GroupName, TestData.Url, TestData.NumberOfConcurrentMessages, TestData.MaxRetryCount, TestData.StreamStartPosition);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.StreamName.ShouldBe(TestData.StreamName);
            subscription.GroupName.ShouldBe(TestData.GroupName);
            subscription.EndPointUri.AbsoluteUri.ShouldBe(TestData.Url);
            subscription.MaxRetryCount.ShouldBe(TestData.MaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(TestData.NumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(TestData.StreamStartPosition);
        }

        [Fact]
        public void Subscription_OptionalValuesPassed_EndpointAsUrl_SubscriptionCreated()
        {
            // 1. Arrange
            Uri endpointUri = new Uri(TestData.Url);

            // 2. Act
            Subscription subscription = Subscription.Create(TestData.StreamName, TestData.GroupName, TestData.Url, TestData.NumberOfConcurrentMessages, TestData.MaxRetryCount, TestData.StreamStartPosition);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.StreamName.ShouldBe(TestData.StreamName);
            subscription.GroupName.ShouldBe(TestData.GroupName);
            subscription.EndPointUri.Equals(endpointUri).ShouldBeTrue();
            subscription.MaxRetryCount.ShouldBe(TestData.MaxRetryCount);
            subscription.NumberOfConcurrentMessages.ShouldBe(TestData.NumberOfConcurrentMessages);
            subscription.StreamStartPosition.ShouldBe(TestData.StreamStartPosition);
        }

        [Theory]
        [InlineData(-1, 1, 1)]
        [InlineData(1, -1, 1)]
        [InlineData(1, 1, -1)]
        [InlineData(-1, -1, 1)]
        [InlineData(1, -1, -1)]
        [InlineData(-1, 1, -1)]
        [InlineData(-1, -1, -1)]
        public void Subscription_OptionalValuesPassed_InvalidValues_EndpointAsString_SubscriptionCreated(Int32 numberOfConcurrentMessages, Int32 maxRetryCount, Int32 streamStartPosition)
        {
            // 2. Act
            Should.Throw<ArgumentOutOfRangeException>(() => Subscription.Create(TestData.StreamName, TestData.GroupName, TestData.Url, numberOfConcurrentMessages, maxRetryCount, streamStartPosition));
        }

        [Theory]
        [InlineData(-1, 1, 1)]
        [InlineData(1, -1, 1)]
        [InlineData(1, 1, -1)]
        [InlineData(-1, -1, 1)]
        [InlineData(1, -1, -1)]
        [InlineData(-1, 1, -1)]
        [InlineData(-1, -1, -1)]
        public void Subscription_OptionalValuesPassed_InvalidValues_EndpointAsUrl_SubscriptionCreated(Int32 numberOfConcurrentMessages, Int32 maxRetryCount, Int32 streamStartPosition)
        {
            // 1. Arrange
            Uri endpointUri = new Uri(TestData.Url);

            // 2. Act
            Should.Throw<ArgumentOutOfRangeException>(() => Subscription.Create(TestData.StreamName, TestData.GroupName, endpointUri, numberOfConcurrentMessages, maxRetryCount, streamStartPosition));
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