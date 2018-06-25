---
layout: tutorials
title: Publish/Subscribe
summary: Learn the basis for any publish / subscribe message exchange
icon: I_dev_P+S.svg
links:
    - label: TopicPublisher.cs
      link: /blob/master/src/TopicPublisher/TopicPublisher.cs
    - label: TopicSubscriber.cs
      link: /blob/master/src/TopicSubscriber/TopicSubscriber.cs
---

This tutorial will introduce you to the fundamentals of the Solace API by connecting a client, adding a topic subscription and sending a message matching this topic subscription. This forms the basis for any publish / subscribe message exchange illustrated here:

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to Solace messaging with the following configuration details:
    *   Connectivity information for a Solace message-VPN
    *   Enabled client username and password

One simple way to get access to Solace messaging quickly is to create a messaging service in Solace Cloud [as outlined here]({{ site.links-solaceCloud-setup}}){:target="_top"}. You can find other ways to get access to Solace messaging below.


## Goals

The goal of this tutorial is to demonstrate the most basic messaging interaction using Solace. This tutorial will show you:

1. How to build and send a message on a topic
2. How to subscribe to a topic and receive a message

{% include_relative assets/solaceMessaging.md %}
{% include_relative assets/solaceApi.md %}

For the purposes of this tutorial, you will connect to the default message VPN of a Solace VMR so the only required information to proceed is the Solace VMR host string which this tutorial accepts as an argument.

## Connecting to the Solace message router

In order to send or receive messages, an application must connect a Solace session. The Solace session is the basis for all client communication with the Solace message router.

In the Solace messaging API for C# (CSCSMP) before using the API, the `ContextFactory` must be initialized. This allows the API to initialize required state, threads, and logging. The `ContextFactory` is initialized with a set of `ContextFactoryProperties`.

```csharp
ContextFactoryProperties cfp = new ContextFactoryProperties()
{
SolClientLogLevel = SolLogLevel.Warning
};
cfp.LogToConsoleError();
ContextFactory.Instance.Init(cfp);
```

Then the `ContextFactory` instance can be used to create the context `IContext` (see API [concepts]({{ site.docs-api-concepts }}){:target="_top"}) that is used to create Solace sessions (ISession) from a set of `SessionProperties`.

Notice the optional `HandleMessage` parameter in the `CreateSession` call. This is the message consumer. It needs to be present only for receiving a message (see details on how to receive a message in the next section of this tutorial).

```csharp
SessionProperties sessionProps = new SessionProperties()
{
    Host = host,
    VPNName = VPNName,
    UserName = UserName,
    Password = Password,
    ReconnectRetries = DefaultReconnectRetries
};

Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);

using (IContext context =
                ContextFactory.Instance.CreateContext(new ContextProperties(), null))
using (ISession session = context.CreateSession(sessionProps, HandleMessage, null))
{
    ReturnCode returnCode = session.Connect();
    if (returnCode == ReturnCode.SOLCLIENT_OK)
    {
        // connected to the Solace message router
    }
}
```

At this point your client is connected to the Solace message router. You can use SolAdmin to view the client connection and related details.

## Receiving a message

This tutorial uses “Direct” messages which are at most once delivery messages. So first, let’s express interest in the messages by subscribing to a Solace topic. Then you can look at publishing a matching message and see it received.

![]({{ site.baseurl }}/assets/images/pub-sub-receiving-message-300x134.png)

With a session connected in the previous step, the next step is to create a message consumer. Message consumers enable the asynchronous receipt of messages through callbacks. These callbacks are defined in CSCSMP by the message receive delegate.

```csharp
private void HandleMessage(object source, MessageEventArgs args)
{
    Console.WriteLine("Received published message.");
    // Received a message
    using (IMessage message = args.Message)
    {
        // Expecting the message content as a binary attachment
        Console.WriteLine("Message content: {0}", Encoding.ASCII.GetString(message.BinaryAttachment));
        // finish the program
        WaitEventWaitHandle.Set();
    }
}
```

The message consumer code uses an event wait handle in this example to release the blocked main thread when a single message has been received.

You must subscribe to a topic in order to express interest in receiving messages. This tutorial uses the topic “tutorial/topic”.

```csharp
ReturnCode returnCode = Session.Connect();
if (returnCode == ReturnCode.SOLCLIENT_OK)
{
    Console.WriteLine("Session successfully connected.");

    Session.Subscribe(ContextFactory.Instance.CreateTopic("tutorial/topic"), true);

    Console.WriteLine("Waiting for a message to be published...");
    WaitEventWaitHandle.WaitOne();
}
```

Then after the subscription is added, the consumer is started. At this point the consumer is ready to receive messages and the main thread is blocked until a message is received.

```csharp
WaitEventWaitHandle.WaitOne();
```

## Sending a message

![]({{ site.baseurl }}/assets/images/pub-sub-sending-message-300x134.png)

Now it is time to send a message to the waiting consumer.

## Creating and sending the message

To send a message, you must create a message and a topic. Both of these are created from the ContextFactory. This tutorial will send a Solace message with Text contents “Sample Message”.

```csharp
using (IMessage message = ContextFactory.Instance.CreateMessage())
{
    message.Destination = ContextFactory.Instance.CreateTopic("tutorial/topic");
    message.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Message");

    Console.WriteLine("Publishing message...");
    ReturnCode returnCode = session.Send(message);
    if (returnCode == ReturnCode.SOLCLIENT_OK)
    {
        Console.WriteLine("Done.");
    }
    else
    {
        Console.WriteLine("Publishing failed, return code: {0}", returnCode);
    }
}
```

At this point a message to the Solace message router has been sent and your waiting consumer will have received the message and printed its contents to the screen.

## Summarizing

Combining the example source code shown above results in the following source code files:

<ul>
{% for item in page.links %}
<li><a href="{{ site.repository }}{{ item.link }}" target="_blank">{{ item.label }}</a></li>
{% endfor %}
</ul>


## Building

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicPublisher.exe  TopicPublisher.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicSubscriber.exe  TopicSubscriber.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution.

## Sample Output

First start the `TopicSubscriber.exe` so that it is up and waiting for published messages. Then you can use the `TopicPublisher.exe` sample to publish a message. Pass your Solace messaging router connection properties as parameters.

```
$ ./TopicSubscriber <host> <username>@<vpnname> <password>
Connecting as <username>@<vpnname> on <host>...
Session successfully connected.
Waiting for a message to be published...
Received published message.
Message content: Sample Message
Finished.
```

```
$ ./TopicPublisher <host> <username>@<vpnname> <password>
Connecting as <username>@<vpnname> on <host>...
Session successfully connected.
Publishing message...
Done.
Finished.
```

With that you now know how to successfully implement publish-subscribe message exchange pattern using Direct messages.

If you have any issues publishing and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues seen.
