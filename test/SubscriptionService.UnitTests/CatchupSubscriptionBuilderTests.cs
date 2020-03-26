namespace SubscriptionService.UnitTests
{
    using System;
    using Builders;
    using EventStore.ClientAPI;
    using Factories;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Shouldly;
    using Xunit;

    /// <summary>
    /// </summary>
    public class CatchupSubscriptionBuilderTests
    {
        #region Fields

        /// <summary>
        /// The event store connection mock
        /// </summary>
        private readonly Mock<IEventStoreConnection> EventStoreConnectionMock;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CatchupSubscriptionBuilderTests" /> class.
        /// </summary>
        public CatchupSubscriptionBuilderTests()
        {
            this.EventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Catchups the subscription builder build called subscription created.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilder_BuildCalled_SubscriptionCreated()
        {
            // 1. Arrange
            CatchupSubscriptionBuilder subscriptionBuilder = CatchupSubscriptionBuilder.Create(TestData.StreamName);

            // 2. Act
            Subscription subscription = subscriptionBuilder.UseConnection(this.EventStoreConnectionMock.Object).Build();

            // 3. Assert
            subscription.ShouldNotBeNull();
        }

        /// <summary>
        /// Catchups the subscription builder default options are set are set.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilder_DefaultOptionsAreSet_AreSet()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = CatchupSubscriptionBuilder.Create(TestData.StreamName);

            // 2. Act

            // 3. Assert
            subscriptionBuilder.Logger.ShouldBeOfType<NullLogger>();
            subscriptionBuilder.EventFactory.ShouldBeOfType<EventFactory>();
            subscriptionBuilder.UserCredentials.Username.ShouldBe("admin");
            subscriptionBuilder.UserCredentials.Password.ShouldBe("changeit");
            subscriptionBuilder.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.None).ShouldBeTrue();

            subscriptionBuilder.HttpRequestInterceptor.ShouldBeNull();
        }

        /// <summary>
        /// Catchups the subscription builder set event factory values updated.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilder_SetEventFactory_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = CatchupSubscriptionBuilder.Create(TestData.StreamName);
            IEventFactory eventFactory = new TestEventFactory();

            // 2. Act
            subscriptionBuilder.UseEventFactory(eventFactory);

            // 3. Assert
            subscriptionBuilder.EventFactory.ShouldBeOfType<TestEventFactory>();
        }

        /// <summary>
        /// Catchups the subscription builder set event logging values updated.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilder_SetEventLogging_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);

            // 2. Act
            subscriptionBuilder.LogAllEvents().LogEventsOnError();

            // 3. Assert
            subscriptionBuilder.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.All).ShouldBeTrue();
            subscriptionBuilder.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.Errors).ShouldBeTrue();
        }

        /// <summary>
        /// Catchups the subscription builder set username and password values updated.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilder_SetUsernameAndPassword_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);
            String username = "TestUser1";
            String password = "TestPassword1";

            // 2. Act
            subscriptionBuilder.WithUserName(username).WithPassword(password);

            // 3. Assert
            subscriptionBuilder.UserCredentials.Username.ShouldBe(username);
            subscriptionBuilder.UserCredentials.Password.ShouldBe(password);
        }

        /// <summary>
        /// Catchups the subscription builderr use connection added subscription created.
        /// </summary>
        [Fact]
        public void CatchupSubscriptionBuilderr_UseConnectionAdded_SubscriptionCreated()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = CatchupSubscriptionBuilder.Create(TestData.StreamName).UseConnection(this.EventStoreConnectionMock.Object);

            // 2. Act

            // 3. Assert
            subscriptionBuilder.ShouldNotBeNull();
            subscriptionBuilder.EventStoreConnection.ShouldNotBeNull();
        }

        #endregion
    }
}