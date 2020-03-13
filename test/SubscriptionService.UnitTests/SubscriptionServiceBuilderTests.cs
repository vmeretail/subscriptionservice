namespace SubscriptionService.UnitTests
{
    using System;
    using EventStore.ClientAPI;
    using Factories;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Shouldly;
    using Xunit;

    public class SubscriptionServiceBuilderTests
    {
        private readonly Mock<IEventStoreConnection> EventStoreConnectionMock;

        public SubscriptionServiceBuilderTests()
        {
            this.EventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
        }

        [Fact]
        public void SubscriptionServiceBuilder_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();

            subscriptionServiceBuilder.ShouldNotBeNull();
        }

        [Fact]
        public void SubscriptionServiceBuilder_BuildCalled_SubscriptionServiceCreated()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();

            // 2. Act
            ISubscriptionService subscriptionService = subscriptionServiceBuilder
                                                       .UseConnection(this.EventStoreConnectionMock.Object)
                                                       .Build();

            // 3. Assert
            subscriptionService.ShouldNotBeNull();
        }

        [Fact]
        public void SubscriptionServiceBuilder_DefaultOptionsAreSet_AreSet()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();

            // 2. Act

            // 3. Assert
            subscriptionServiceBuilder.Logger.ShouldBeOfType<NullLogger>();
            subscriptionServiceBuilder.EventFactory.ShouldBeOfType<EventFactory>();
            subscriptionServiceBuilder.Username.ShouldNotBeNullOrEmpty();
            subscriptionServiceBuilder.Password.ShouldNotBeNullOrEmpty();
            subscriptionServiceBuilder.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.None).ShouldBeTrue();

            // 4. Cleanup
        }

        [Fact]
        public void SubscriptionServiceBuilder_SetUsernameAndPassword_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();
            String username = "TestUser1";
            String password = "TestPassword1";

            // 2. Act
            subscriptionServiceBuilder.WithUserName(username)
                                      .WithPassword(password);

            // 3. Assert
            subscriptionServiceBuilder.Username.ShouldBe(username);
            subscriptionServiceBuilder.Password.ShouldBe(password);
        }


        [Fact]
        public void SubscriptionServiceBuilder_SetEventFactory_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();
            IEventFactory eventFactory = new TestEventFactory();

            // 2. Act
            subscriptionServiceBuilder.UseEventFactory(eventFactory);

            // 3. Assert
            subscriptionServiceBuilder.EventFactory.ShouldBeOfType<TestEventFactory>();
        }

        [Fact]
        public void SubscriptionServiceBuilder_SetEventLogging_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder();

            // 2. Act
            subscriptionServiceBuilder.LogAllEvents().LogEventsOnError();

            // 3. Assert
            subscriptionServiceBuilder.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.All).ShouldBeTrue();
            subscriptionServiceBuilder.LogEventsSettings.HasFlag(SubscriptionServiceBuilder.LogEvents.Errors).ShouldBeTrue();
        }

        [Fact]
        public void SubscriptionServiceBuilder_BuildCalledAndSettingsUpdated_SubscriptionServiceCreated()
        {
            // 1. Arrange
            IEventFactory eventFactory = new TestEventFactory();
            String username = "TestUser1";
            String password = "TestPassword1";

            SubscriptionServiceBuilder subscriptionServiceBuilder = new SubscriptionServiceBuilder()
                .UseConnection(this.EventStoreConnectionMock.Object)
                .LogAllEvents()
                .LogEventsOnError()
                .UseEventFactory(eventFactory)
                .WithUserName(username)
                .WithPassword(password);

            // 2. Act
            ISubscriptionService subscriptionService = subscriptionServiceBuilder.Build();

            // 3. Assert
            subscriptionService.ShouldNotBeNull();

            //NOTE: Slight problem here, as we cant' check these value actually got pass into the SubscriptionService
        }
    }
}