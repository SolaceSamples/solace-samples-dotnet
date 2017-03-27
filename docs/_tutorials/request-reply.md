---
layout: tutorials
title: Request/Reply
summary: Learn how to set up request/reply messaging.
icon: request-reply.png
---


This tutorial outlines both roles in the request-response message exchange pattern. It will show you how to act as the client by creating a request, sending it and waiting for the response. It will also show you how to act as the server by receiving incoming requests, creating a reply and sending it back to the client. It builds on the basic concepts introduced in [publish/subscribe tutorial]({{ site.baseurl }}/publish-subscribe).

![]({{ site.baseurl }}/images/request-reply.png)

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to a running Solace message router with the following configuration:
    *   Enabled message VPN
    *   Enabled client username

One simple way to get access to a Solace message router is to start a Solace VMR load [as outlined here]({{ site.docs-vmr-setup }}){:target="_top"}. By default the Solace VMR will run with the “default” message VPN configured and ready for messaging. Going forward, this tutorial assumes that you are using the Solace VMR. If you are using a different Solace message router configuration, adapt the instructions to match your configuration.

The build instructions in this tutorial assume you are using a Linux shell. If your environment differs, adapt the instructions.

## Goals

The goal of this tutorial is to understand the following:

*   On the requestor side:
    1.  How to create a request
    2.  How to receive a response
    3.  How to use the Solace API to correlate the request and response
*   On the replier side:
    1.  How to detect a request expecting a reply
    2.  How to generate a reply message

## Overview

Request-reply messaging is supported by the Solace message router for all delivery modes. For direct messaging, the Solace APIs provide the Requestor object for convenience. This object makes it easy to send a request and wait for the reply message. It is a convenience object that makes use of the API provided “inbox” topic that is automatically created for each Solace client and automatically correlates requests with replies using the message correlation ID. (See Message Correlation below for more details). On the reply side another convenience method enables applications to easily send replies for specific requests. Direct messaging request reply is the delivery mode that is illustrated in this sample.

It is also possible to use guaranteed messaging for request reply scenarios. In this case the replier can listen on a queue for incoming requests and the requestor can use a temporary endpoint to attract replies. The requestor and replier must manually correlate the messages. This is explained further in the [Solace documentation]({{ site.docs-gm-rr }}){:target="_top"} and shown in the API samples named `RRGuaranteedRequestor` and `RRGuaranteedReplier`.

### Message Correlation

For request-reply messaging to be successful it must be possible for the requestor to correlate the request with the subsequent reply. Solace messages support two fields that are needed to enable request-reply correlation. The reply-to field can be used by the requestor to indicate a Solace Topic or Queue where the reply should be sent. A natural choice for this is often the unique `P2PINBOX_IN_USE` topic which is an auto-generated unique topic per client which is accessible as a session property. The second requirement is to be able to detect the reply message from the stream of incoming messages. This is accomplished using the correlation-id field. This field will transit the Solace messaging system unmodified. Repliers can include the same correlation-id in a reply message to allow the requestor to detect the corresponding reply. The figure below outlines this exchange.

![]({{ site.baseurl }}/images/Request-Reply_diagram-1.png)

For direct messages however, this is simplified through the use of the `Requestor` object as shown in this sample.

## Trying it yourself

This tutorial is available in [GitHub]({{ site.repository }}){:target="_blank"} along with the other [Solace Developer Getting Started Examples]({{ site.links-get-started }}){:target="_top"}.

To successfully build the samples you must have the Solace Messaging API for C#/.NET (also referred to as SolClient for .NET) downloaded and installed for your project.

The SolClient for .NET can be downloaded and installed via nuget.org or manually.

For the **nuget.org** option use the NuGet console or the NuGet Visual Studio Extension to download the [SolaceSystems.Solclient.Messaging](http://nuget.org/packages/SolaceSystems.Solclient.Messaging/) package for your solution and to install it for your project. It contains the required libraries and brief API documentation.

For the **manual** option download it [from here]({{ site.links-downloads }}){:target="_top"}. That distribution is a zip file containing the required libraries, detailed API documentation, and examples.

The instructions in this tutorial assume you successfully downloaded and installed the SolClient for .NET. If your environment differs then adjust the build instructions appropriately.

At the end, this tutorial walks through downloading and running the sample from source.

## Connecting a session to the message router

As with other tutorials, this tutorial requires an instance of ISession connected to the default message VPN of a Solace VMR which has authentication disabled. So the only required information to proceed is the Solace VMR host string which this tutorial accepts as an argument. Connect the session as outlined in the [publish/subscribe tutorial]({{ site.baseurl }}/publish-subscribe).

## Making a request

First let’s look at the requestor. This is the application that will send the initial request message and wait for the reply.

![]({{ site.baseurl }}/images/Request-Reply_diagram-2.png)

The requestor must create a message and the topic to send the message to:

```csharp
using (IMessage requestMessage = ContextFactory.Instance.CreateMessage())
    requestMessage.Destination = ContextFactory.Instance.CreateTopic("tutorial/requests");
requestMessage.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Request");
}
```

Now the request can be sent. This example demonstrates a blocking call where the method will wait for the response message to be received.

```csharp
IMessage replyMessage = null;
int timeout = 10000; // 10 secs
ReturnCode returnCode = session.SendRequest(requestMessage, out replyMessage, timeout);
```

If the call request was executed successfully then the returned return code is `ReturnCode.SOLCLIENT_OK`.

If the timeout is set to zero then the `SendRequest` call becomes non-blocking and it returns immediately.

## Replying to a request

Now it is time to receive the request and generate an appropriate reply.

![Request-Reply_diagram-3]({{ site.baseurl }}/images/Request-Reply_diagram-3.png)

Just as with previous tutorials, you still need to connect a session and subscribe to the topics that requests are sent on. The following is an example of such reply.

```csharp
private void HandleRequestMessage(object source, MessageEventArgs args)
{
    Console.WriteLine("Received request.");
    // Received a request message
    using (IMessage requestMessage = args.Message)
    {
        // Expecting the request content as a binary attachment
        RequestContent = Encoding.ASCII.GetString(requestMessage.BinaryAttachment);
        // Create reply message
        using (IMessage replyMessage = ContextFactory.Instance.CreateMessage())
        {
            // Set the reply content as a binary attachment
            replyMessage.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Reply");
            Console.WriteLine("Sending reply...");
            ReturnCode returnCode = session.SendReply(requestMessage, replyMessage);
            if (returnCode == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine("Sent.");
            }
            else
            {
                Console.WriteLine("Reply failed, return code: {0}", returnCode);
 
            }
            // finish the program
            waitEventWaitHandle.Set();
        }
    }
}
```

The `HandleRequestMessage` is the routine that is passed as a callback to the CreateSession call:

```csharp
session = context.CreateSession(sessionProps, HandleRequestMessage, null);
```

## Receiving the Reply Message

All that’s left is to receive and process the reply message as it is received at the requestor. If you now update your requestor code to match the following you will see each reply printed to the console.

```csharp
ReturnCode returnCode = session.SendRequest(requestMessage, out replyMessage, timeout);
if (returnCode == ReturnCode.SOLCLIENT_OK)
{
    // Expecting reply as a binary attachment
    Console.WriteLine("Received reply: {0}", Encoding.ASCII.GetString(replyMessage.BinaryAttachment));
}
else
{
    Console.WriteLine("Request failed, return code: {0}", returnCode);
}
```

## Summarizing

The full source code for this example is available in [GitHub]({{ site.repository }}){:target="_blank"}. If you combine the example source code shown above results in the following source:

*   [BasicRequestor.cs]({{ site.repository }}/blob/master/src/BasicRequestor/BasicRequestor.cs){:target="_blank"}
*   [BasicReplier.cs]({{ site.repository }}/blob/master/src/BasicReplier/BasicReplier.cs){:target="_blank"}

### Getting the Source

Clone the GitHub repository containing the Solace samples.

```
git clone {{ site.repository }}
cd {{ site.baseurl | remove: '/'}}
```

### Building

Modify the example source code to reflect your Solace messaging router message-vpn name and credentials for connection (client username and optional password) as needed.

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:BasicReplier.exe BasicReplier.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:BasicRequestor.exe BasicRequestor.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` at compile and runtime time and `libsolclient_64.dll` at runtime in the same directory where your source and executables are. 

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution. 

### Running the Sample

First start the BasicReplier.exe so that it is up and listening for requests. Then you can use the BasicRequestor.exe sample to send requests and receive replies. Pass your Solace messaging router host name (or IP address) as parameter.

```
$ ./BasicReplier HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Waiting for a request to come in...
Received request.
Request content: Sample Request
Sending reply...
Sent.
Finished.
```

```
$ ./BasicRequestor HOST
Connecting as tutorial@default on HOST...
Session successfully connected.
Sending request...
Received reply: Sample Reply
Finished.
```

With that you now know how to successfully implement the request-reply message exchange pattern using Direct messages.

If you have any issues sending and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues.
