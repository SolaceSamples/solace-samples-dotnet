---
layout: tutorials
title: Persistence with Queues
summary: Learn how to send and receive messages in a point-to-point fashion.
icon: persistence-tutorial.png
---

This tutorial builds on the basic concepts introduced in the [publish/subscribe tutorial]({{ site.baseurl }}/publish-subscribe), and will show you how to send and receive persistent messages from a Solace message router queue in a point to point fashion.

![persistence-tutorial]({{ site.baseurl }}/images/persistence-tutorial.png)

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to a running Solace message router with the following configuration:
    *   Enabled message VPN
    *   Enabled client username
    *   Client-profile enabled with guaranteed messaging permissions.

One simple way to get access to a Solace message router is to start a Solace VMR load [as outlined here]({{ site.docs-vmr-setup }}){:target="_top"}. By default the Solace VMR will run with the “default” message VPN configured and ready for messaging. Going forward, this tutorial assumes that you are using the Solace VMR. If you are using a different Solace message router configuration, adapt the instructions to match your configuration.

## Goals

The goal of this tutorial is to understand the following:

*   How to programmatically create a durable queue on the Solace message router
*   How to send a persistent message to a Solace queue
*   How to bind to this queue and receive a persistent message

## Solace message router properties

As with other tutorials, this tutorial will connect to the default message VPN of a Solace VMR which has authentication disabled. So the only required information to proceed is the Solace VMR host string which this tutorial accepts as an argument.

## Obtaining the Solace API

This tutorial depends on you having the Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) downloaded and installed for your project.

The SolClient for .NET can be downloaded and installed via nuget.org or manually.

For the **nuget.org** option use the NuGet console or the NuGet Visual Studio Extension to download the [SolaceSystems.Solclient.Messaging](http://nuget.org/packages/SolaceSystems.Solclient.Messaging/) package for your solution and to install it for your project. It contains the required libraries and brief API documentation.

For the **manual** option download it [from here]({{ site.links-downloads }}){:target="_top"}. That distribution is a zip file containing the required libraries, detailed API documentation, and examples.

The instructions in this tutorial assume you successfully downloaded and installed the SolClient for .NET. If your environment differs then adjust the build instructions appropriately.

## Provisioning a Queue through the API

![message-router-queue]({{ site.baseurl }}/images/message-router-queue.png)

The first requirement for guaranteed messaging using a Solace message router is to provision a guaranteed message endpoint. For this tutorial we will use a point-to-point queue. To learn more about different guaranteed message endpoints see [here]({{ site.docs-endpoints }}){:target="_top"}.

Durable endpoints are not auto created on Solace message routers. However there are two ways to provision them.

*   Using the management interface
*   Using the APIs

Using the Solace APIs to provision an endpoint can be a convenient way of getting started quickly without needing to become familiar with the management interface. This is why it is used in this tutorial. However it should be noted that the management interface provides more options to control the queue properties. So generally it becomes the preferred method over time.

Provisioning an endpoint through the API requires the “Guaranteed Endpoint Create” permission in the client-profile. You can confirm this is enabled by looking at the client profile in SolAdmin. If it is correctly set you will see the following: 

![persistence-dotnet-1]({{ site.baseurl }}/images/persistence-tutorial-image-3.png)  

Provisioning the queue involves three steps.

1.  Obtaining IQueue interface instance representing the queue you wish to create.
2.  Setting the properties that you wish for your queue. This examples permits consumption of messages and sets the queue type to exclusive. More details on queue permissions can be found in the [developer documentation]({{ site.docs-solace-apis }}){:target="_top"}.
3.  Provisioning the Queue on the Solace message router

The following code shows you this for the queue named “Q/tutorial”.

```csharp
string queueName = "Q/tutorial";

IQueue queue = ContextFactory.Instance.CreateQueue(queueName))

EndpointProperties endpointProps = new EndpointProperties()
{
    Permission = EndpointProperties.EndpointPermission.Consume,
    AccessType = EndpointProperties.EndpointAccessType.Exclusive
};

session.Provision(queue, endpointProps,
    ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists & ProvisionFlag.WaitForConfirm, null);
```

The `IgnoreErrorIfEndpointAlreadyExists` flags signals to the API that the application is tolerate of the queue already existing even if it’s properties are different than those specified in the endpoint properties.

The `WaitForConfirm` flags tells the API to wait for the provision complete confirmation before exiting the `Provision` routine.

## Sending a message to a queue

Now it is time to send a message to the queue.  

![sending-message-to-queue]({{ site.baseurl }}/images/sending-message-to-queue.png) 

There is really no difference in the actual calls to the ISession object instance when sending a persistent message as compared to a direct message shown in the publish/subscribe tutorial. The difference in the persistent message is that the Solace message router will acknowledge the message once it is successfully stored on the message router.

To send a message, you must still create a message. The main difference from sending a direct message is that you must set the message delivery mode to persistent. When you send the message you also update the call to send to include your queue object as the destination.

```csharp
using (IMessage message = ContextFactory.Instance.CreateMessage())
{
    message.Destination = queue;
    message.DeliveryMode = MessageDeliveryMode.Persistent;
    message.BinaryAttachment = Encoding.ASCII.GetBytes("Persistent Queue Tutorial");

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
```

At this point the message to the Solace message router is sent and it will be waiting for your consumer on the queue.

## Receiving a message from a queue

Now it is time to receive the messages sent to your queue.  

![receiving-message-from-queue]({{ site.baseurl }}/images/receiving-message-from-queue.png)

You still need to connect a session just as you did with the publisher. With a connected session, you then need to bind to the Solace message router queue with a flow receiver. Flow receivers allow applications to receive messages from a Solace guaranteed message flow. Flows encapsulate all of the acknowledgement behaviour required for guaranteed messaging. Conveniently flow receivers have the same interface as message consumers but flows also require some additional properties on creation.

A flow requires properties. At its most basic, the flow properties require the endpoint (our newly provisioned or existing queue) and an ack mode. In this example you’ll use the client ack mode where the application will explicitly acknowledge each message.

Flows are created from Solace session objects just as direct message consumers are.

Notice HandleMessageEvent and HandleFlowEvent event handlers that are passed in when creating a flow. These handlers are invoked when a message arrives to the endpoint (the queue) or a flow events occurs.

You must start your flow so it can begin receiving messages.

```csharp
Flow = Session.CreateFlow(new FlowProperties()
{
    AckMode = MessageAckMode.ClientAck
},
queue, null, HandleMessageEvent, HandleFlowEvent);
Flow.Start();
```

Both flow properties and endpoint properties are explained in more detail in the [developer documentation]({{ site.docs-dotnet-api }}){:target="_top"}.

The following example shows you a basic flow receiver which receives messages, prints them to the console and acknowledges them as consumed back to the Solace message router so it can remove them.

```csharp
private void HandleMessageEvent(object source, MessageEventArgs args)
{
    Console.WriteLine("Received message.");
    using (IMessage message = args.Message)
    {
        Console.WriteLine("Message content: {0}",
            Encoding.ASCII.GetString(message.BinaryAttachment));
        Flow.Ack(message.ADMessageId);
        // finish the program
        WaitEventWaitHandle.Set();
    }
}
```

## Summarizing

Combining the example source code show above results in the following source code files:

*   [QueueProducer.cs]({{ site.repository }}/blob/master/src/QueueProducer/QueueProducer.cs){:target="_blank"}
*   [QueueConsumer.cs]({{ site.repository }}/blob/master/src/QueueConsumer/QueueConsumer.cs){:target="_blank"}

### Building

Modify the example source code to reflect your Solace messaging router message-vpn name and credentials for connection (client username and optional password) as needed.

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out: QueueProducer.exe QueueProducer.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out: QueueConsumer.exe QueueConsumer.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` at compile and runtime time and `libsolclient_64.dll` at runtime in the same directory where your source and executables are. 

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution. 

### Sample Output

First start the `QueueProducer` to send a message to the queue. Then you can use the `QueueConsumer` sample to receive the messages from the queue. Pass your Solace messaging router host name (or IP address) as parameter.

```
$ ./QueueProducer HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Attempting to provision the queue 'Q/tutorial'...
Queue 'Q/tutorial' has been created and provisioned.
Sending message to queue Q/tutorial...
Done.
Finished.
```

```
$ ./QueryConsumer HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Attempting to provision the queue 'Q/tutorial'...
Queue 'Q/tutorial' has been created and provisioned.
Received Flow Event 'UpNotice' Type: '200' Text: 'OK'
Waiting for a message in the queue 'Q/tutorial'...
Received message.
Message content: Persistent Queue Tutorial
Finished.
```

You have now successfully connected a client, sent persistent messages to a queue and received and acknowledged them.

If you have any issues sending and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues.