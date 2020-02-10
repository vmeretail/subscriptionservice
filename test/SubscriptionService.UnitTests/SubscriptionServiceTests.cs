using System;
using Xunit;

namespace SubscriptionService.UnitTests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.SystemData;
    using Moq;
    using Shouldly;

    public class SubscriptionServiceTests
    {
        [Fact]
        public void SubscriptionService_CanBeCreated_IsCreated()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);

            // 2. Act
            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnectionMock.Object);

            // 3. Assert
            subscriptionService.ShouldNotBeNull();
        }
        
        [Fact]
        public void SubscriptionService_EventStoreConnection_IsNull_ErrorIsThrown()
        {
            // 1. Arrange
            IEventStoreConnection eventStoreConnection = null;

            // 3. Assert
            Should.Throw<ArgumentNullException>(() =>
                                                {
                                                    ISubscriptionService subscriptionService =
                                                        new SubscriptionService(eventStoreConnection);
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
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task>>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<int>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnectionMock.Object);

            // 2. Act
            await subscriptionService.Start(TestData.Subscriptions, CancellationToken.None);

            // 3. Assert
            subscriptionService.IsStarted.ShouldBeTrue();
        }

        [Fact]
        public async Task SubscriptionService_Start_SubscriptionListIsNull_ErrorThrown()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            eventStoreConnectionMock.Setup(e => e.ConnectAsync()).Returns(Task.CompletedTask);
            eventStoreConnectionMock.Setup(e => e.ConnectToPersistentSubscriptionAsync(It.IsAny<String>(),
                                                                                       It.IsAny<String>(),
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task>>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<int>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnectionMock.Object);
            List<Subscription> subscriptionsList = null;

            // 2. Act
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                                                {
                                                    await subscriptionService.Start(subscriptionsList, CancellationToken.None);
                                                });

        }

        [Fact]
        public async Task SubscriptionService_Start_SubscriptionListIsEmpty_ErrorThrown()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            eventStoreConnectionMock.Setup(e => e.ConnectAsync()).Returns(Task.CompletedTask);
            eventStoreConnectionMock.Setup(e => e.ConnectToPersistentSubscriptionAsync(It.IsAny<String>(),
                                                                                       It.IsAny<String>(),
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task>>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<int>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnectionMock.Object);
            List<Subscription> subscriptionsList = new List<Subscription>();

            // 2. Act
            await Should.ThrowAsync<ArgumentNullException>(async () =>
                                                {
                                                    await subscriptionService.Start(subscriptionsList, CancellationToken.None);
                                                });
        }

        [Fact]
        public async Task SubscriptionService_Stop_SubscriptionServiceIsNotStarted()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            eventStoreConnectionMock.Setup(e => e.ConnectAsync()).Returns(Task.CompletedTask);
            eventStoreConnectionMock.Setup(e => e.ConnectToPersistentSubscriptionAsync(It.IsAny<String>(),
                                                                                       It.IsAny<String>(),
                                                                                       It.IsAny<Func<EventStorePersistentSubscriptionBase, ResolvedEvent, int?, Task>>(),
                                                                                       It.IsAny<Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception>>(),
                                                                                       It.IsAny<UserCredentials>(),
                                                                                       It.IsAny<int>(),
                                                                                       It.IsAny<Boolean>())).ReturnsAsync(default(EventStorePersistentSubscription));

            ISubscriptionService subscriptionService = new SubscriptionService(eventStoreConnectionMock.Object);
            await subscriptionService.Start(TestData.Subscriptions, CancellationToken.None);

            // 2. Act
            await subscriptionService.Stop(CancellationToken.None);

            // 3. Assert
            subscriptionService.IsStarted.ShouldBeFalse();
        }
    }
}
