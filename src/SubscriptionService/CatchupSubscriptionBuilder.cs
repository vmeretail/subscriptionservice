namespace SubscriptionService
{
    using System;

    public class CatchupSubscriptionBuilder : SubscriptionBuilder
    {
        private CatchupSubscriptionBuilder(String streamName)
        {
            //this.SubscriptionName = "";// TODO: Lets see what happens when no name specified. if we get an error perhaps Guid.new
            //Alternatively .Create forces a name
            this.StreamName = streamName;
        }

        //TODO: Add Subscription Dropped event handler
        //TODO: Add Live processing event handler
        

        public static CatchupSubscriptionBuilder Create(String streamName)
        {
            return new CatchupSubscriptionBuilder(streamName);
        }

        public CatchupSubscriptionBuilder SetName(String subscriptionName)
        {
            this.SubscriptionName = subscriptionName;

            return this;
        }

        public CatchupSubscriptionBuilder SetLastCheckpoint(Int64 lastCheckpoint)
        {
            this.LastCheckpoint = lastCheckpoint;

            return this;
        }

        internal String StreamName;
        internal String SubscriptionName;

        internal Int64? LastCheckpoint;
    }
}