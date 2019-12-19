using System;
using System.Collections.Generic;
using System.Text;

namespace SubscriptionService.UnitTests
{
    using Configuration;
    using Shouldly;
    using Xunit;

    public class SubscriptionTests
    {
        [Fact]
        public void Subscription_EndpointAsString_SubscriptionCreated()
        {
            // 1. Arrange
            String endpointUrl = "http://127.0.0.1:2113/";
            String streamName = "TestStream";
            String groupName = "GroupName";
            
            // 2. Act
            Subscription subscription = Subscription.Create(streamName, groupName, endpointUrl);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.EndPointUri.AbsoluteUri.ShouldBe(endpointUrl);
            subscription.GroupName.ShouldBe(groupName);
            subscription.StreamName.ShouldBe(streamName);
        }

        [Fact]
        public void Subscription_EndpointAsUrl_SubscriptionCreated()
        {
            // 1. Arrange
            Uri endpointUri = new Uri("http://127.0.0.1:2113/");
            String streamName = "TestStream";
            String groupName = "GroupName";

            // 2. Act
            Subscription subscription = Subscription.Create(streamName, groupName, endpointUri);

            // 3. Assert
            subscription.ShouldNotBeNull();
            subscription.EndPointUri.Equals(endpointUri).ShouldBeTrue();
            subscription.GroupName.ShouldBe(groupName);
            subscription.StreamName.ShouldBe(streamName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Subscription_EndpointStringInvalidValue_ErrorIsThrown(String endpointUrl)
        {
            // 1. Arrange
            String streamName = "TestStream";
            String groupName = "GroupName";

            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(streamName, groupName, endpointUrl));
        }

        [Fact]
        public void Subscription_EndpointUriInvalidValue_ErrorIsThrown()
        {
            // 1. Arrange
            Uri endpointUri = null;
            String streamName = "TestStream";
            String groupName = "GroupName";

            // 3. Assert
            Should.Throw<ArgumentException>(() => Subscription.Create(streamName, groupName, endpointUri));
        }
    }
}
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