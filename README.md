# Subscription Service for EventStore

Lightweight and easy to use library, which allows you to manage delivering events from Event Store persistent subscriptions to configured endpoints.

## CI Status
|||
| --- | --- |
| **Build** | [![Build status](https://github.com/vmeretail/subscriptionservice/workflows/Release/badge.svg)](https://github.com/vmeretail/subscriptionservice/actions) |
| **NuGet** | [![nuget](https://img.shields.io/nuget/v/EventStore.SubscriptionService.svg)](https://www.nuget.org/packages/EventStore.SubscriptionService/)

## Nuget

Nuget can be found here:

https://www.nuget.org/packages/EventStore.SubscriptionService/

Alternatively, from the command line:

Install-Package EventStore.SubscriptionService

## Usage

Running the service is straight forward.
The code example below demonstrates how to hook up a Subscription Service instance with an Event Store, and create a single persistent subscription.

```
String connectionString = "ConnectTo=tcp://admin:changeit@127.0.0.1:1113;VerboseLogging=true;";
IEventStoreConnection eventStoreConnection = EventStoreConnection.Create(new Uri(connectionString));

List<Subscription> subscriptions = new List<Subscription>();
subscriptions.Add(Subscription.Create("$ce-Accounts", "Read Model", new Uri("http://127.0.0.1/api/events")));

//You are responsible for the connection.
await eventStoreConnection.ConnectAsync();

SubscriptionService subscriptionService = new SubscriptionService(subscriptions, eventStoreConnection);

await subscriptionService.Start(CancellationToken.None);
```

The library will manage posting the event, and uses the follow default values for sending your events:

Method: Post
Content Type: application/json
Headers: None

If you need to alter any of these, the library exposes the following eventhandler:

```
public event EventHandler<HttpRequestMessage> OnEventAppeared;
```

Before you start the Subscription Service, do the following:

```
subscriptionService.OnEventAppeared += SubscriptionService_OnEventAppeared;
```

And create a method similar to this:

```
private static void SubscriptionService_OnEventAppeared(object sender, System.Net.Http.HttpRequestMessage e)
{
    e.Headers.Add("Authorization", $"Bearer someToken");
}
```
