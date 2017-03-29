---
layout: tutorials
title: Topic to Queue Mapping
summary: Learn how to map existing topics to Solace queues.
icon: topic-to-queue-mapping.png
---

This tutorial builds on the basic concepts introduced in the [Persistence with Queues tutorial]({{ site.baseurl }}/persistence-with-queues) and will show you how to make use of one of Solace’s advanced queueing features called “Topic to Queue Mapping.”

![topic-to-queue-mapping]({{ site.baseurl }}/images/topic-to-queue-mapping.png)

In addition to spooling messages published directly to the queue, it is possible to add one or more topic subscriptions to a durable queue so that messages published to those topics are also delivered to and spooled by the queue. This is a powerful feature that enables queues to participate equally in point to point and publish / subscribe messaging models. More details about the [“Topic to Queue Mapping” feature here]({{ site.docs-topic-queue}}){:target="_top"}.

The following diagram illustrates this feature.

<img src="{{ site.baseurl }}/images/topic-to-queue-mapping-detail.png" width="500" height="206" />

If you have a durable queue named `Q`, it will receive messages published directly to the queue destination named `Q`. However, it is also possible to add subscriptions to this queue in the form of topics. This example adds topics `A` and `B`. Once these subscriptions are added, the queue will start receiving messages published to the topic destinations `A` and `B`. When you combine this with the wildcard support provided by Solace topics this opens up a number of interesting use cases.

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to a running Solace message router with the following configuration:
    *   Enabled message VPN configured for guaranteed messaging support.
    *   Enabled client username
    *   Client-profile enabled with guaranteed messaging permissions.
*   You understand the basics introduced in [Persistence with Queues]({{ site.baseurl }}/persistence-with-queues)

Note that one simple way to get access to a Solace message router is to start a Solace VMR load [as outlined here]({{ site.docs-vmr-setup }}){:target="_top"}. By default the Solace VMR will with the “default” message VPN configured and ready for guaranteed messaging. Going forward, this tutorial assumes that you are using the Solace VMR. If you are using a different Solace message router configuration adapt the tutorial appropriately to match your configuration.

## Goals

The goal of this tutorial is to understand the following:

*   How to add topic subscriptions to a queue
*   How to interrogate the Solace message router to confirm capabilities.

## Solace message router properties

As with other tutorials, this tutorial will connect to the default message VPN of a Solace VMR which has authentication disabled. So the only required information to proceed is the Solace VMR host string which this tutorial accepts as an argument.

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

## Connection setup

First, connect to the Solace message router in almost exactly the same way as other tutorials. The only difference is explained below.

```csharp
SessionProperties sessionProps = new SessionProperties()
{
    Host = host,
    VPNName = VPNName,
    UserName = UserName,
    ReconnectRetries = DefaultReconnectRetries,
    IgnoreDuplicateSubscriptionError = true
};

Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
using (ISession session = context.CreateSession(sessionProps, null, null))
{
    ReturnCode returnCode = session.Connect();
    if (returnCode == ReturnCode.SOLCLIENT_OK)
    {
        Console.WriteLine("Session successfully connected.");
        ProduceMessage(session);
    }
    else
    {
        Console.WriteLine("Error connecting, return code: {0}", returnCode);
    }
}
```

The only difference in the above is the duplicate subscription processing property value `IgnoreDuplicateSubscriptionError` set to true.

One aspect to consider when adding subscriptions is how your application wishes the Solace API to behave in the face of pre-existing duplicate subscriptions. The default behavior is to throw an exception if an application tries to add a subscription that already exists. In this tutorial, we’ll relax that behavior and change our session so that it will tolerate the subscription already existing.

For more details on this session flag, refer to [the product documentation]({{ site.docs-dotnet }}){:target="_top"}.

## Review: Receiving message from a queue

The [Persistence with Queues tutorial]({{ site.baseurl }}/persistence-with-queues) demonstrated how to publish and receive messages from a queue. This sample will do so in the same way. This sample will also depend on the endpoint being provisioned by through the API as was done in the previous tutorial. For clarity, this code is not repeated in the discussion but is included in the [full source available in GitHub]({{ site.repository }}/blob/master/src/TopicToQueueMapping/TopicToQueueMapping.cs){:target="_blank"}.

## Confirming Message Router Capabilities

One convenient feature provided by the C# API is the session capabilities. When `ISession` object instances connect to a Solace message router, they exchange a set of capabilities to determine levels of support for various API features. This enables the Solace APIs to remain compatible with Solace message routers even as they are upgraded to new loads.

Applications can also make use of these capabilities to programmatically check for required features when connecting. The following code is an example of how this is done for the capabilities required by this tutorial.

```csharp
if (session.IsCapable(CapabilityType.PUB_GUARANTEED) &&
    session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED) &&
    session.IsCapable(CapabilityType.ENDPOINT_MANAGEMENT) &&
    session.IsCapable(CapabilityType.QUEUE_SUBSCRIPTIONS))
{
    Console.WriteLine("All required capabilities supported.");
}
else
{
    Console.WriteLine("Required capabilities are not supported.");
    throw new InvalidOperationException("Cannot proceed because session's required capabilities are not supported.");
}
```

In this case the tutorial requires permission to send and receive guaranteed messages, configure endpoints and manage queue subscriptions. If these capabilities are not available on the message router the tutorial will not proceed. If these capabilities are missing, you update the client-profile used by the client-username to enable them. See the [Solace documentation]({{ site.docs-client-profile}}){:target="_top"} for details.

## Adding a Subscription to a Queue

In order to enable a queue to participate in publish/subscribe messaging, you need to add topic subscriptions to the queue to attract messages. You do this from the `ISession` object instance using the `Subscribe` method. The queue destination is passed as the first argument and then topic subscription to add and any flags. This example asks the API to block until the subscription is confirmed to be on the Solace message router. The subscription added in this tutorial is `Q/tutorial/topicToQueueMapping`.

```csharp
ITopic tutorialTopic = ContextFactory.Instance.CreateTopic("T/mapped/topic/sample");
Session.Subscribe(Queue, tutorialTopic, SubscribeFlag.WaitForConfirm, null);
```

## Publish – Subscribe using a Queue

Once the subscription is added to the queue, all that is left to do in this tutorial is to send some messages to your topic and validate that they arrive on the queue. First publish some messages using the following code:

```csharp
using (IMessage message = ContextFactory.Instance.CreateMessage())
{
    message.Destination = tutorialTopic;
    message.DeliveryMode = MessageDeliveryMode.Persistent;

    for (int i = 0; i &lt; TotalMessages; i++)
    {
        message.BinaryAttachment = Encoding.ASCII.GetBytes(
            string.Format("Topic to Queue Mapping Tutorial! Message ID: {0}", i));

        Console.WriteLine("Sending message ID {0} to topic '{1}' mapped to queue '{2}'...",
            i, tutorialTopic.Name, queueName);
        ReturnCode returnCode = session.Send(message);
        if (returnCode == ReturnCode.SOLCLIENT_OK)
        {
            Console.WriteLine("Done.");
        }
        else
        {
            Console.WriteLine("Sending failed, return code: {0}", returnCode);
        }
    }
}
```

These messages are now on your queue. You can validate this through SolAdmin by inspecting the queue. Now receive the messages using a flow consumer as outlined in detail in previous tutorials.

```csharp
Flow = session.CreateFlow(new FlowProperties()
{
    AckMode = MessageAckMode.ClientAck
},
queue, null, HandleMessageEvent, HandleFlowEvent);
Flow.Start();

// block the current thread until a confirmation received
CountdownEvent.Wait();
```

## Summarizing

The full source code for this example is available in [GitHub]({{ site.repository }}){:target="_blank"}. If you combine the example source code shown above results in the following source:

*   [TopicToQueueMapping.cs]({{ site.repository }}/blob/master/src/TopicToQueueMapping/TopicToQueueMapping.cs){:target="_blank"}

### Building

Modify the example source code to reflect your Solace messaging router message-vpn name and credentials for connection (client username and optional password) as needed.

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:TopicToQueueMapping.exe TopicToQueueMapping.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution.

### Sample Output

Start the TopicToQueueMapping to send and receive messages, passing your Solace messaging router host name (or IP address) as parameter.

```
$ ./TopicToQueueMapping HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
All required capabilities supported.
Attempting to provision the queue 'Q/tutorial/topicToQueueMapping'...
Queue 'Q/tutorial/topicToQueueMapping' has been created and provisioned.
Sending message ID 0 to topic 'T/mapped/topic/sample' mapped to queue 'Q/tutorial/topicToQueueMapping'...
Done.
Sending message ID 1 to topic 'T/mapped/topic/sample' mapped to queue 'Q/tutorial/topicToQueueMapping'...
Done.
Sending message ID 2 to topic 'T/mapped/topic/sample' mapped to queue 'Q/tutorial/topicToQueueMapping'...
Done.
Sending message ID 3 to topic 'T/mapped/topic/sample' mapped to queue 'Q/tutorial/topicToQueueMapping'...
Done.
Sending message ID 4 to topic 'T/mapped/topic/sample' mapped to queue 'Q/tutorial/topicToQueueMapping'...
Done.
5 messages sent. Processing replies.
Received Flow Event 'UpNotice' Type: '200' Text: 'OK'
Received message.
Message content: Topic to Queue Mapping Tutorial! Message ID: 0
Received message.
Message content: Topic to Queue Mapping Tutorial! Message ID: 1
Received message.
Message content: Topic to Queue Mapping Tutorial! Message ID: 2
Received message.
Message content: Topic to Queue Mapping Tutorial! Message ID: 3
Received message.
Message content: Topic to Queue Mapping Tutorial! Message ID: 4
Finished.
```

If you have any problems with this tutorial, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues.
