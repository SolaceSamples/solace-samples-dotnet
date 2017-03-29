---
layout: tutorials
title: Confirmed Delivery
summary: Learn how to confirm that your messages are received by a Solace message router.
icon: confirmed-delivery.png
---

This tutorial builds on the basic concepts introduced in [Persistence with Queues]({{ site.baseurl }}/persistence-with-queues) tutorial and will show you how to properly process publisher acknowledgements. Once an acknowledgement for a message has been received and processed, you have confirmed your persistent messages have been properly accepted by the Solace message router and therefore can be guaranteed of no message loss.

![confirmed-delivery]({{ site.baseurl }}/images/confirmed-delivery.png)

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to a running Solace message router with the following configuration:
    *   Enabled message VPN configured for guaranteed messaging support.
    *   Enabled client username.
    *   Client-profile enabled with guaranteed messaging permissions.

One simple way to get access to a Solace message router is to start a Solace VMR load [as outlined here]({{ site.docs-vmr-setup }}){:target="_top"}. By default the Solace VMR will run with the "default" message VPN configured and ready for messaging. Going forward, this tutorial assumes that you are using the Solace VMR. If you are using a different Solace message router configuration, adapt the instructions to match your configuration.

## Goals

The goal of this tutorial is to understand the following:

*  How to properly handle persistent message acknowledgements on message send.

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

## Message Acknowledgement Correlation

In order send fully persistent messages to a Solace message router with no chance of message loss, it is absolutely necessary to properly process the acknowledgements that come back from the Solace message router. These acknowledgements will let you know if the message was accepted by the Solace message router or if it was rejected. If it is rejected, the acknowledgement will also contain exact details of why it was rejected. For example, you may not have permission to send persistent messages or queue destination may not exist etc.

In order to properly handle message acknowledgements it is also important to know which application event or message is being acknowledged. In other words, applications often need some application context along with the acknowledgement from the Solace message router to properly process the business logic on their end.

The Solace C# API enables this through a callback in the form of the session event handler.

This callback allows applications to attach a correlation object on message send and this correlation object is also returned in the acknowledgement. This allows applications to easily pass the application context to the acknowledgement, handling enabling proper correlation of messages sent and acknowledgements received.

For the purposes of this tutorial, we will track message context using the following simple class. It will keep track of the result of the acknowledgements.

```csharp
class MsgInfo
{
    public bool Acked { get; set; }
    public bool Accepted { get; set; }
    public readonly IMessage Message;
    public readonly int Id;
    public MsgInfo(IMessage message, int id)
    {
        Acked = false;
        Accepted = false;
        Message = message;
        Id = id;
    }
}
```

## Connection setup

First, connect to the Solace message router in exactly the same way as other tutorials.

```csharp
SessionProperties sessionProps = new SessionProperties()
{
    Host = host,
    VPNName = VPNName,
    UserName = UserName,
    ReconnectRetries = DefaultReconnectRetries
};

Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);

// NOTICE HandleSessionEvent as session event handler
using (ISession session = context.CreateSession(
   sessionProps, null, HandleSessionEvent))
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

## Adding Message Correlation on Send

The [Persistence with Queues]({{ site.baseurl }}/persistence-with-queues) tutorial demonstrated how to send persistent messages using code very similar to the following.

```csharp
using (IMessage message = ContextFactory.Instance.CreateMessage())
{
    message.Destination = queue;
    message.DeliveryMode = MessageDeliveryMode.Persistent;
    message.BinaryAttachment = Encoding.ASCII.GetBytes("Persistent Queue Tutorial");

    Console.WriteLine("Sending message to queue {0}...", queueName);
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

Adding a message correlation object to allow an application to easily correlate acknowledgements is accomplished using the `IMessage.CorrelationKey` property where you pass in the object you want returned to your application in the acknowledgement callback. So after augmenting the publish code from above, you're left with the following:

```csharp
using (IMessage message = ContextFactory.Instance.CreateMessage())
{
    message.Destination = queue;
    message.DeliveryMode = MessageDeliveryMode.Persistent;

    for (int i = 0; i &lt; TotalMessages; i++)
    {
        message.BinaryAttachment = Encoding.ASCII.GetBytes(
            string.Format("Confirmed Publish Tutorial! Message ID: {0}", i));

        MsgInfo msgInfo = new MsgInfo(message, i);
        message.CorrelationKey = msgInfo;
        msgList.Add(msgInfo);

        Console.WriteLine("Sending message to queue {0}...", queueName);
        ReturnCode returnCode = session.Send(message);
        if (returnCode != ReturnCode.SOLCLIENT_OK)
            Console.WriteLine("Sending failed, return code: {0}", returnCode);
    }
}
```

## Processing the Solace Acknowledgement

To process the acknowledgements with correlation, you must implement the session event handler.

The following code shows you a basic acknowledgement processing class that will store the result from the Solace message router. When it is done, it will notify the main thread of ack processing.

```csharp
public void HandleSessionEvent(object sender, SessionEventArgs args)
{
    // Received a session event
    Console.WriteLine("Received session event {0}.", args.ToString());
    switch (args.Event)
    {
        case SessionEvent.Acknowledgement:
        case SessionEvent.RejectedMessageError:
            MsgInfo messageRecord = args.CorrelationKey as MsgInfo;
            if (messageRecord != null)
            {
                messageRecord.Acked = true;
                messageRecord.Accepted = args.Event == SessionEvent.Acknowledgement;
                CountdownEvent.Signal();
            }
            break;
        default:
            break;
    }
}
```

## Summarizing

Combining the example source code show above results in the following source code files:

*   [ConfirmedPublish.cs]({{ site.repository }}/blob/master/src/ConfirmedDelivery/ConfirmedPublish.cs){:target="_blank"}

### Building

Modify the example source code to reflect your Solace messaging router message-vpn name and credentials for connection (client username and optional password) as needed.

Build it from Microsoft Visual Studio or command line:

```
csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out: ConfirmedPublish.exe ConfirmedPublish.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution.

### Sample Output

Start the `ConfirmedPublish` to send messages to the queue, passing your Solace messaging router host name (or IP address) as parameter and see confirmations.

```
$ ./ConfirmedPublish HOST
Solace Systems Messaging API Tutorial, Copyright 2008-2015 Solace Systems, Inc.
Connecting as tutorial@default on HOST...
Received session event SessionEventArgs=[info: host 'HOST', IP HOST:55555 (host 1 of 1) (host connection attempt 1 of 1) (total connection attempt 1 of 1) , ResponseCode: 0 , Event: UpNotice].
Session successfully connected.
Attempting to provision the queue 'Q/tutorial'...
Queue 'Q/tutorial' has been created and provisioned.
Sending message to queue Q/tutorial...
Sending message to queue Q/tutorial...
Sending message to queue Q/tutorial...
Sending message to queue Q/tutorial...
Sending message to queue Q/tutorial...
5 messages sent. Processing replies.
Received session event SessionEventArgs=[info: Already Exists , ResponseCode: 400 , Event: ProvisionError].
Received session event SessionEventArgs=[info:  , ResponseCode: 0 , Event: Acknowledgement].
Received session event SessionEventArgs=[info:  , ResponseCode: 0 , Event: Acknowledgement].
Received session event SessionEventArgs=[info:  , ResponseCode: 0 , Event: Acknowledgement].
Received session event SessionEventArgs=[info:  , ResponseCode: 0 , Event: Acknowledgement].
Received session event SessionEventArgs=[info:  , ResponseCode: 0 , Event: Acknowledgement].
Message 0 was accepted by the router.
Message 0 was acknowledged by the router.
Message 1 was accepted by the router.
Message 1 was acknowledged by the router.
Message 2 was accepted by the router.
Message 2 was acknowledged by the router.
Message 3 was accepted by the router.
Message 3 was acknowledged by the router.
Message 4 was accepted by the router.
Message 4 was acknowledged by the router.
Finished.
```

You have now successfully sent persistent messages to a Solace router and confirmed its receipt by correlating the acknowledgement.

If you have any issues sending and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues.

