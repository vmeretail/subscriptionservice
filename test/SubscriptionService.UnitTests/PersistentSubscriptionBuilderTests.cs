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
    public class PersistentSubscriptionBuilderTests
    {
        #region Fields

        /// <summary>
        /// The event store connection mock
        /// </summary>
        private readonly Mock<IEventStoreConnection> EventStoreConnectionMock;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscriptionBuilderTests" /> class.
        /// </summary>
        public PersistentSubscriptionBuilderTests()
        {
            this.EventStoreConnectionMock = new Mock<IEventStoreConnection>(MockBehavior.Strict);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Persistents the subscription builder build called subscription created.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_BuildCalled_SubscriptionCreated()
        {
            // 1. Arrange
            PersistentSubscriptionBuilder persistentSubscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);

            // 2. Act
            Subscription subscription = persistentSubscriptionBuilder.UseConnection(this.EventStoreConnectionMock.Object).Build();

            // 3. Assert
            subscription.ShouldNotBeNull();
        }

        /// <summary>
        /// Persistents the subscription builder default options are set are set.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_DefaultOptionsAreSet_AreSet()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);

            // 2. Act

            // 3. Assert
            subscriptionBuilder.Logger.ShouldBeOfType<NullLogger>();
            subscriptionBuilder.EventFactory.ShouldBeOfType<EventFactory>();
            subscriptionBuilder.UserCredentials.Username.ShouldBe("admin");
            subscriptionBuilder.UserCredentials.Password.ShouldBe("changeit");
            subscriptionBuilder.LogEventsSettings.HasFlag(SubscriptionBuilder.LogEvents.None).ShouldBeTrue();

            subscriptionBuilder.HttpRequestInterceptor.ShouldBeNull();
        }

        [Fact]
        public void PersistentSubscriptionBuilder_SetInFlightLimit_ValuesUpdated()
        {
            // 1. Arrange
            Int32 inflightLimit = 500;
            PersistentSubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);

            // 2. Act
            subscriptionBuilder.SetInFlightLimit(inflightLimit);
            // 3. Assert
            subscriptionBuilder.InFlightLimit.ShouldBe(inflightLimit);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PersistentSubscriptionBuilder_SetInFlightLimitWithInvalidValues_ValuesNotUpdated(Int32 inflightLimit)
        {
            // 1. Arrange
            PersistentSubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);

            // 2. Act
            Should.Throw<ArgumentOutOfRangeException>(() => subscriptionBuilder.SetInFlightLimit(inflightLimit));
        }

        /// <summary>
        /// Persistents the subscription builder set event factory values updated.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_SetEventFactory_ValuesUpdated()
        {
            // 1. Arrange
            SubscriptionBuilder subscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName);
            IEventFactory eventFactory = new TestEventFactory();

            // 2. Act
            subscriptionBuilder.UseEventFactory(eventFactory);

            // 3. Assert
            subscriptionBuilder.EventFactory.ShouldBeOfType<TestEventFactory>();
        }

        /// <summary>
        /// Subscriptions the service builder set event logging values updated.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_SetEventLogging_ValuesUpdated()
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
        /// Subscriptions the service builder set username and password values updated.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_SetUsernameAndPassword_ValuesUpdated()
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
        /// Persistents the subscription builder use connection added subscription created.
        /// </summary>
        [Fact]
        public void PersistentSubscriptionBuilder_UseConnectionAdded_SubscriptionCreated()
        {
            // 1. Arrange
            SubscriptionBuilder persistentSubscriptionBuilder = PersistentSubscriptionBuilder.Create(TestData.StreamName, TestData.GroupName)
                                                                                             .UseConnection(this.EventStoreConnectionMock.Object);

            // 2. Act

            // 3. Assert
            persistentSubscriptionBuilder.ShouldNotBeNull();
            persistentSubscriptionBuilder.EventStoreConnection.ShouldNotBeNull();
        }

        #endregion
    }
}