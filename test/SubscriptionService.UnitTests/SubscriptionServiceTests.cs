using System;
using Xunit;

namespace SubscriptionService.UnitTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.ClientAPI;
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
            ISubscriptionService subscriptionService = new SubscriptionService(TestData.Subscriptions, eventStoreConnectionMock.Object);

            // 3. Assert
            subscriptionService.ShouldNotBeNull();
        }

        [Fact]
        public async Task SubscriptionService_Start_SubscriptionServiceIsStarted()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            ISubscriptionService subscriptionService = new SubscriptionService(TestData.Subscriptions, eventStoreConnectionMock.Object);

            // 2. Act
            await subscriptionService.Start(CancellationToken.None);

            // 3. Assert
            subscriptionService.IsStarted.ShouldBeTrue();
        }

        [Fact]
        public async Task SubscriptionService_Stop_SubscriptionServiceIsNotStarted()
        {
            // 1. Arrange
            Mock<IEventStoreConnection> eventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
            ISubscriptionService subscriptionService = new SubscriptionService(TestData.Subscriptions, eventStoreConnectionMock.Object);
            await subscriptionService.Start(CancellationToken.None);

            // 2. Act
            await subscriptionService.Stop(CancellationToken.None);

            // 3. Assert
            subscriptionService.IsStarted.ShouldBeFalse();
        }
    }
}
