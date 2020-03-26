namespace SubscriptionService.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Builders;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Moq;
    using Shouldly;
    using Xunit;

    public class SubscriptionTests
    {
        #region Methods

        [Fact]
        public void SubscriptionService_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);

            // 2. Act
            var subscriptionBuilder = PersistentSubscriptionBuilder
                                                                       .Create(TestData.StreamName, TestData.GroupName)
                                                                       .UseConnection(eventStoreConnectionMock.Object);

            Subscription subscription = new Subscription(subscriptionBuilder);

            // 3. Assert
            subscription.ShouldNotBeNull();
        }

        [Fact]
        public void SubscriptionService_EventStoreConnection_IsNull_ErrorIsThrown()
        {
            // 1. Arrange
            // 3. Assert
            Should.Throw<NullReferenceException>(() =>
                                                 {
                                                     var subscriptionBuilder = PersistentSubscriptionBuilder
                                                                                  .Create(TestData.StreamName, TestData.GroupName);

                                                     var subscription = new Subscription(subscriptionBuilder);
                                                 });
        }

        [Fact]
        public async Task SubscriptionService_Start_SubscriptionServiceIsStarted()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            eventStoreConnectionMock.Setup(e => e.ConnectAsync()).Returns(Task.CompletedTask);
            eventStoreConnectionMock.Setup(e => e.ConnectToPersistentSubscriptionAsync(It.IsAny<String>(),
                                                                                       It.IsAny<String>(),
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Int32?, Task
                                                                                       >>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason,
                                                                                           Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<Int32>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            var subscriptionBuilder = PersistentSubscriptionBuilder
                                      .Create(TestData.StreamName, TestData.GroupName)
                                      .UseConnection(eventStoreConnectionMock.Object);

            Subscription subscription = new Subscription(subscriptionBuilder);

            // 2. Act
            await subscription.Start(CancellationToken.None);

            // 3. Assert
            subscription.IsStarted.ShouldBeTrue();
        }

        [Fact]
        public async Task SubscriptionService_Stop_SubscriptionServiceIsNotStarted()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            eventStoreConnectionMock.Setup(e => e.ConnectAsync()).Returns(Task.CompletedTask);
            eventStoreConnectionMock.Setup(e => e.ConnectToPersistentSubscriptionAsync(It.IsAny<String>(),
                                                                                       It.IsAny<String>(),
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, Int32?, Task
                                                                                       >>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason,
                                                                                           Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<Int32>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            var subscriptionBuilder = PersistentSubscriptionBuilder
                                      .Create(TestData.StreamName, TestData.GroupName)
                                      .UseConnection(eventStoreConnectionMock.Object);

            Subscription subscription = new Subscription(subscriptionBuilder);

            await subscription.Start(CancellationToken.None);

            // 2. Act
            subscription.Stop();

            // 3. Assert
            subscription.IsStarted.ShouldBeFalse();
        }

        #endregion
    }
}