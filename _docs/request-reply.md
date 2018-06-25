---
layout: tutorials
title: Request/Reply
summary: Learn how to set up request/reply messaging.
icon: I_dev_R+R.svg
links:
    - label: BasicRequestor.cs
      link: /blob/master/src/BasicRequestor/BasicRequestor.cs
    - label: BasicReplier.cs
      link: /blob/master/src/BasicReplier/BasicReplier.cs
---


This tutorial outlines both roles in the request-response message exchange pattern. It will show you how to act as the client by creating a request, sending it and waiting for the response. It will also show you how to act as the server by receiving incoming requests, creating a reply and sending it back to the client. It builds on the basic concepts introduced in [publish/subscribe tutorial]({{ site.baseurl }}/publish-subscribe).

## Assumptions

This tutorial assumes the following:

*   You are familiar with Solace [core concepts]({{ site.docs-core-concepts }}){:target="_top"}.
*   You have access to Solace messaging with the following configuration details:
    *   Connectivity information for a Solace message-VPN
    *   Enabled client username and password

One simple way to get access to Solace messaging quickly is to create a messaging service in Solace Cloud [as outlined here]({{ site.links-solaceCloud-setup}}){:target="_top"}. You can find other ways to get access to Solace messaging below.

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

For request-reply messaging to be successful it must be possible for the requestor to correlate the request with the subsequent reply. Solace messages support two fields that are needed to enable request-reply correlation. The reply-to field can be used by the requestor to indicate a Solace Topic or Queue where the reply should be sent. A natural choice for this is often the unique `P2PInboxInUse` topic which is an auto-generated unique topic per client which is accessible as a session property. The second requirement is to be able to detect the reply message from the stream of incoming messages. This is accomplished using the correlation-id field. This field will transit the Solace messaging system unmodified. Repliers can include the same correlation-id in a reply message to allow the requestor to detect the corresponding reply. The figure below outlines this exchange.

![]({{ site.baseurl }}/assets/images/Request-Reply_diagram-1.png)

For direct messages however, this is simplified through the use of the `Requestor` object as shown in this sample.

{% include_relative assets/solaceMessaging.md %}
{% include_relative assets/solaceApi.md %}

## Making a request

First let’s look at the requestor. This is the application that will send the initial request message and wait for the reply.

![]({{ site.baseurl }}/assets/images/Request-Reply_diagram-2.png)

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

![Request-Reply_diagram-3]({{ site.baseurl }}/assets/images/Request-Reply_diagram-3.png)

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

<ul>
{% for item in page.links %}
<li><a href="{{ site.repository }}{{ item.link }}" target="_blank">{{ item.label }}</a></li>
{% endfor %}
</ul>


### Getting the Source

Clone the GitHub repository containing the Solace samples.

```
git clone {{ site.repository }}
cd {{ site.repository | split: '/' | last}}
```

### Building

Build it from Microsoft Visual Studio or command line:

```
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:BasicReplier.exe BasicReplier.cs
> csc /reference:SolaceSystems.Solclient.Messaging_64.dll /optimize /out:BasicRequestor.exe BasicRequestor.cs
```

You need `SolaceSystems.Solclient.Messaging_64.dll` (or `SolaceSystems.Solclient.Messaging.dll`) at compile and runtime time and `libsolclient.dll` at runtime in the same directory where your source and executables are.

Both DLLs are part of the Solace C#/.NET API distribution and located in `solclient-dotnet\lib` directory of that distribution.

### Running the Sample

First start the BasicReplier.exe so that it is up and listening for requests. Then you can use the BasicRequestor.exe sample to send requests and receive replies. Pass your Solace messaging router connection properties as parameters.

```
$ ./BasicReplier <host> <username>@<vpnname> <password>
Connecting as <username>@<vpnname> on <host>...
Session successfully connected.
Waiting for a request to come in...
Received request.
Request content: Sample Request
Sending reply...
Sent.
Finished.
```

```
$ ./BasicRequestor <host> <username>@<vpnname> <password>
Connecting as <username>@<vpnname> on <host>...
Session successfully connected.
Sending request...
Received reply: Sample Reply
Finished.
```

With that you now know how to successfully implement the request-reply message exchange pattern using Direct messages.

If you have any issues sending and receiving a message, check the [Solace community]({{ site.links-community }}){:target="_top"} for answers to common issues.
