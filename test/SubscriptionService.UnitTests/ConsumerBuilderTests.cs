namespace SubscriptionService.UnitTests
{
    using System;
    using Builders;
    using Domain;
    using Shouldly;
    using Xunit;

    public class ConsumerBuilderTests
    {
        [Fact]
        public void ConsumerBuilder_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            ConsumerBuilder consumerBuilder = new ConsumerBuilder();

            // 2. Act

            // 3. Assert
            consumerBuilder.ShouldNotBeNull();

            // 4. Cleanup
        }

        [Fact]
        public void ConsumerBuilder_BuildCalled_IsCreated()
        {
            // 1. Arrange
            ConsumerBuilder consumerBuilder = new ConsumerBuilder();

            // 2. Act
            Consumer consumer = consumerBuilder
                                .AddEndpointUri(new Uri(TestData.Url))
                                .Build();

            // 3. Assert
            consumer.ShouldNotBeNull();
            consumer.GetUri().AbsoluteUri.ShouldBe(TestData.Url);
            consumer.GetHttpClient().ShouldNotBeNull();
        }
    }
}