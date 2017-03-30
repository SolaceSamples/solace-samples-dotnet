---
layout: tutorials
title: Publish/Subscribe
summary: Learn the basis for any publish / subscribe message exchange
icon: publish-subscribe.png
---

This tutorial will introduce you to the fundamentals of the Solace API by connecting a client, adding a topic subscription and sending a message matching this topic subscription. This forms the basis for any publish / subscribe message exchange illustrated here:

![]({{ site.baseurl }}/images/publish-subscribe.png)

## Assumptions

This tutorial assumes the following:

* You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
* You have access to a running Solace message router with the following configuration:
    * Enabled message VPN
    * Enabled client username

One simple way to get access to a Solace message router is to start a Solace VMR load [as outlined here]({{ site.docs-vmr-setup }}){:target="_top"}. By default the Solace VMR will run with the “default” message VPN configured and ready for messaging. Going forward, this tutorial assumes that you are using the Solace VMR. If you are using a different Solace message router configuration, adapt the instructions to match your configuration.

## Goals

The goal of this tutorial is to demonstrate the most basic messaging interaction using Solace. This tutorial will show you:

1. How to build and send a message on a topic
2. How to subscribe to a topic and receive a message

## Solace message router properties

In order to send or receive messages to a Solace message router, you need to know a few details of how to connect to the Solace message router. Specifically you need to know the following:

<table>
<tbody>
<tr>
<th>Resource</th>
<th>Value</th>
<th>Description</th>
</tr>
<tr>
<td>Host</td>
<td>String of the form <code>DNS name</code> or <code>IP:Port</code></td>
<td>This is the address client’s use when connecting to the Solace message router to send and receive messages. For a Solace VMR this there is only a single interface so the IP is the same as the management IP address. For Solace message router appliances this is the host address of the message-backbone.</td>
</tr>
<tr>
<td>Message VPN</td>
<td>String</td>
<td>The Solace message router Message VPN that this client should connect to. The simplest option is to use the “default” message-vpn which is present on all Solace message routers and fully enabled for message traffic on Solace VMRs.</td>
</tr>
<tr>
<td>Client Username</td>
<td>String</td>
<td>The client username. For the Solace VMR default message VPN, authentication is disabled by default, so this can be any value.</td>
</tr>
<tr>
<td>Client Password</td>
<td>String</td>
<td>The optional client password. For the Solace VMR default message VPN, authentication is disabled by default, so this can be any value or omitted.</td>
</tr>
</tbody>
</table>

For the purposes of this tutorial, you will connect to the default message VPN of a Solace VMR so the only required information to proceed is the Solace VMR host string which this tutorial accepts as an argument.

## Obtaining the Solace API

This tutorial depends on you having the Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) downloaded and installed for your project, and the instructions in this tutorial assume you successfully done it. If your environment differs then adjust the build instructions appropriately.

Here are a few easy ways to get this API.

### Get the API: Using nuget.org

Use the NuGet console or the NuGet Visual Studio Extension to download the [SolaceSystems.Solclient.Messaging](http://nuget.org/packages/SolaceSystems.Solclient.Messaging/) package for your solution and to install it for your project.

The package contains the required libraries and brief API documentation. It will automatically copy correct libraries from the package to the target directory at build time, but of course if you compile your program from the command line you would need to refer to the API assemblies and libraries locations explicitly.

Notice that in this case both x64 and x86 API assemblies and libraries have the same names.

### Get the API: Using the Solace Developer Portal

The SolClient for .NET can be [downloaded here]({{ site.links-downloads }}){:target="_top"}. That distribution is a zip file containing the required libraries, detailed API documentation, and examples.

You would need either to update your Visual Studio project to point to the extracted assemblies and libraries, or to refer to their locations explicitly.

Notice that in this case x64 and x86 API assemblies and libraries have different names, e.g. the x86 API assembly is SolaceSystems.Solclient.Messaging.dll and the x64 API assembly is SolaceSystems.Solclient.Messaging_64.dll.

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

![]({{ site.baseurl }}/images/pub-sub-receiving-message-300x134.png)

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

![]({{ site.baseurl }}/images/pub-sub-sending-message-300x134.png)

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

*   [TopicPublisher.cs]({{ site.repository }}/blob/master/src/TopicPublisher/TopicPublisher.cs){:target="_blank"}
*   [TopicSubscriber.cs]({{ site.repository }}/blob/master/src/TopicSubscriber/TopicSubscriber.cs){:target="_blank"}

## Building

Modify the example source code to reflect your Solace messaging router message-vpn name and credentials for connection (client username and optional password) as needed.

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicPublisher.exe  TopicPublisher.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicSubscriber.exe  TopicSubscriber.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution.

## Sample Output

First start the `TopicSubscriber.exe` so that it is up and waiting for published messages. Then you can use the `TopicPublisher.exe` sample to publish a message. Pass your Solace messaging router host name (or IP address) as parameter.

```
$ ./TopicSubscriber HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Waiting for a message to be published...
Received published message.
Message content: Sample Message
Finished.
```

```
$ ./TopicPublisher HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Publishing message...
Done.
Finished.
```

With that you now know how to successfully implement publish-subscribe message exchange pattern using Direct messages.

If you have any issues publishing and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues seen.