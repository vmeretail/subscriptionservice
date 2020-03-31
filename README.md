# Subscription Service for EventStore

Lightweight and easy to use library, which allows you to manage delivering events from Event Store persistent & catchup subscriptions to configured endpoints.

## CI Status
|||
| --- | --- |
| **Build** | [![Build status](https://github.com/vmeretail/subscriptionservice/workflows/Release/badge.svg)](https://github.com/vmeretail/subscriptionservice/actions) |
| **NuGet** | [![nuget](https://img.shields.io/nuget/v/EventStore.SubscriptionService.svg)](https://www.nuget.org/packages/EventStore.SubscriptionService/) 
| **Downloads**| [![nuget](https://img.shields.io/nuget/dt/EventStore.SubscriptionService.svg)](https://www.nuget.org/packages/EventStore.SubscriptionService/) |


## Nuget

Nuget can be found here:

https://www.nuget.org/packages/EventStore.SubscriptionService/

Alternatively, from the command line:

Install-Package EventStore.SubscriptionService

## Usage

### Persistent Subscriptions

The code example below demonstrates how to create a Persistent Subscription:

```
String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";

IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(connectionString);
await eventStoreConnection.ConnectAsync(); //you open the connection

Uri uri = new Uri("https://localhost/api/yourAPI");

var subscription = PersistentSubscriptionBuilder.Create("$ce-PersistentTest", "Persistent Test 1")
                                                .UseConnection(eventStoreConnection)
                                                .DeliverTo(uri)
                                                .Build();

await subscription.Start(cancellationToken);
```

This manages posting the event, and uses the follow default values for sending your events:

**Method**: Post
**Content Type**: application/json
**Headers**: None

The Persistent Subscription offers control over the default behaviour:

#### AutoAckEvents

Instructs the Subscription to auto ACK events.

This applies whether EventAppeared is overriden or default.

```
.AutoAckEvents()
```

If not set, Auto ACKing is off.

#### SetInFlightLimit

Set the number of inflight messages for the subscription.
Values <= 0 are not permitted.

```
.SetInFlightLimit(25)
```

If not set, the default value of **10** is used.

#### WithPersistentSubscriptionSettings

Allows any of the PersistentSubscriptionSettings to be changed.

```
.WithPersistentSubscriptionSettings(persistentSubscriptionSettings)
```

### Catchup Subscriptions

The code example below demonstrates how to create a Catchup Subscription:

```
Uri uri = new Uri("https://localhost/api/yourAPI");

subscription = CatchupSubscriptionBuilder.Create("$ce-CatchupTest")
                                                      .UseConnection(eventStoreConnection)
                                                      .DeliverTo(uri)
                                                      .Build();
                                                      
await subscription.Start(cancellationToken);
```

The code above creates a new Catchup subscription, and will deliver to ths uri specified.

The Persistent Subscription offers control over the default behaviour:

#### AddLiveProcessingStartedHandler

Allows the user to override the LiveProcessingStarted event handler.

```
.AddLiveProcessingStartedHandler((subscription) =>{

})
``` 

#### DrainEventsAfterSubscriptionDropped

If your catcup subscription is stopped, the Catchup subscription (by default) will continue processing events which have already been read (i.e if you have a read buffer size of 500, and the catchup is stopped at 400, another 100 events will be procssed in EventAppeared)
By setting DrainEventsAfterSubscriptionDropped, you are instructing the library to "drain" your events. These events will not be sent to the desried endpoint **and** (arguably just as important) the lastCheckpoint will not be broadcast!

```
.DrainEventsAfterSubscriptionDropped()
``` 

#### SetLastCheckpoint

When you create a Catchup Subscription, and need to resume from a specific checkpoint, simply use this method.

```
.SetLastCheckpoint(500)
``` 

#### WithCatchUpSubscriptionSettings

You can override the default CatchUpSubscriptionSettings with this method.

```
.WithCatchUpSubscriptionSettings(catchUpSubscriptionSettings)
``` 

#### AddLastCheckPointChanged

If you need informaed when the lastCheckpoint has been reached, make a call to this.
You can pass your Action, as well as a frequemcy for how often the event should be fired.

For example, if setting checkPointBroadcastFrequency to 500 will broadcast every 500 events.

```
.AddLastCheckPointChanged((s, l) =>{

                       }, checkPointBroadcastFrequency)
```   

**There is no default behaviour for lastCheckpoint, so if your catchupSubscriptino relies on this, you must implement this**


### Shared 

These methods are common across both Subscriptions:

#### AddEventAppearedHandler

This allows you to override the default EventAppeared.

```
PersistentSubscriptionBuilder.AddEventAppearedHandler((@subscription,                                                                                                                          @event) => {

                                                        })
```                                                  
ACK / NAK is still controlled by default behaviour (you do however, have access to the EventStorePersistentSubscriptionBase)

#### AddSubscriptionDroppedHandler

This allows you to override the default SubscriptionDropped.

```
.AddSubscriptionDroppedHandler((subscription,reason, arg3) =>{

                                                             })
```

#### LogAllEvents

All events are logged out via the ILogger. By default, this is disabled.

```
.LogAllEvents()
```

#### LogEventsOnError

If an Event is not delivered, having this set will log the event to trace. By default this is disabled.

```
.LogEventsOnError()
```

#### UseEventFactory

If you need to convert the underlying ResolvedEvents to a specific format, you can build you own factory inheriting from IEventFactory

```
internal class WorkerEventFactory : IEventFactory
{
    public String ConvertFrom(PersistedEvent persistedEvent)
    {
        String json = Encoding.Default.GetString(persistedEvent.Data);
        dynamic expandoObject = new ExpandoObject();
        dynamic temp = JsonConvert.DeserializeAnonymousType(json, expandoObject);

        temp.EventId = persistedEvent.EventId;
        temp.EventType = persistedEvent.EventType;
        temp.EventDateTime = persistedEvent.Created;
        temp.Metadata = Encoding.Default.GetString(persistedEvent.Metadata);

        return JsonConvert.SerializeObject(temp);
    }
}
```
And call it:

```
IEventFactory eventFactory = new WorkerEventFactory();
.UseEventFactory(eventFactory)
```

It's worth noting that a default EventFactory is created for you which will serialise resolvedEvent.Data

#### DeliverTo

Create a route to deliver events to.

```
.DeliverTo( uri );
```

This supports a single Uri, so further calls to this method will result ni the previous value being overwritten.

#### UseHttpInterceptor

If added to the Builder, allows the user to make changes to the HttRequestMessage before the event is posted.
The example below shows an authorization token being added to the header.

```
.UseHttpInterceptor(message => {
    //The user can make some changes (like adding headers)
    message.Headers.Add("Authorization", "Bearer someToken");
    })
```
