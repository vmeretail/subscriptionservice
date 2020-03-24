namespace SubscriptionService.UnitTests
{
    using System;
    using Configuration;
    using Shouldly;
    using Xunit;

    public class CatchupSubscriptionTests
    {
        [Fact]
        public void CatchupSubscription_CanBeCreated_IsCreated()
        {
            // 2. Act
            Uri uri = new Uri(TestData.Url);
            CatchupSubscription catchupSubscription = CatchupSubscription.Create(TestData.CatchupSubscriptionName1, TestData.StreamName, uri);

            // 3. Assert
            catchupSubscription.ShouldNotBeNull();

            catchupSubscription.StreamName.ShouldBe(TestData.StreamName);
            catchupSubscription.SubscriptionName.ShouldBe(TestData.CatchupSubscriptionName1);
            catchupSubscription.EndPointUri.AbsoluteUri.ShouldBe(TestData.Url);
            catchupSubscription.LastCheckpoint.ShouldBeNull();
        }

        [Fact]
        public void CatchupSubscription_CanBeCreatedWithLastCheckpoint_IsCreated()
        {
            // 2. Act
            Uri uri = new Uri(TestData.Url);
            Int64 lastCheckpoint = 10;
            CatchupSubscription catchupSubscription = CatchupSubscription.Create(TestData.CatchupSubscriptionName1, TestData.StreamName, lastCheckpoint, uri);

            // 3. Assert
            catchupSubscription.ShouldNotBeNull();

            catchupSubscription.StreamName.ShouldBe(TestData.StreamName);
            catchupSubscription.SubscriptionName.ShouldBe(TestData.CatchupSubscriptionName1);
            catchupSubscription.EndPointUri.AbsoluteUri.ShouldBe(TestData.Url);
            catchupSubscription.LastCheckpoint.ShouldBe(lastCheckpoint);
        }
    }
}