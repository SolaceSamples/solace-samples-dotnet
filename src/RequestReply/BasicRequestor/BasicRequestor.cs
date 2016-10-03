#region Copyright & License
/**
 *  Copyright 2015 Solace Systems, Inc. All rights reserved.
 * 
 *  http://www.solacesystems.com
 * 
 *  This source is distributed under the terms and conditions of
 *  any contract or license agreement between Solace Systems, Inc.
 *  ("Solace") and you or your company. If there are no licenses or
 *  contracts in place use of this source is not authorized. This 
 *  source is provided as is and is not supported by Solace unless
 *  such support is provided for under an agreement signed between 
 *  you and Solace.
 */
#endregion

using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;

/// <summary>
/// Solace Systems Messaging API tutorial: BasicRequestor
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// Demonstrates how to use Solace Systems Messaging API for sending a request and receiving a reply 
    /// </summary>
    class BasicRequestor
    {
        public string Reply { get; private set; }

        string VPNName { get; set; }
        string UserName { get; set; }
        int TimeoutSeconds { get; set; }

        const int DefaultReconnectRetries = 3;

        void Run(IContext context, string host)
        {
            // Validate parameters
            if (context == null)
            {
                throw new ArgumentException("Solace Systems API context Router must be not null.", "context");
            }
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Solace Messaging Router host name must be non-empty.", "host");
            }
            if (string.IsNullOrWhiteSpace(VPNName))
            {
                throw new InvalidOperationException("VPN name must be non-empty.");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Client username must be non-empty.");
            }

            // Create session properties
            SessionProperties sessionProps = new SessionProperties()
            {
                Host = host,
                VPNName = VPNName,
                UserName = UserName,
                ReconnectRetries = DefaultReconnectRetries
            };

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            using (ISession session = context.CreateSession(sessionProps, null, null))
            {
                ReturnCode returnCode = session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected.");
                    // send the request and receive reply
                    Reply = GetReply(session);
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

        private string GetReply(ISession session)
        {
            string reply = null;
            // Create the request message
            using (IMessage requestMessage = ContextFactory.Instance.CreateMessage())
            {
                requestMessage.Destination = ContextFactory.Instance.CreateTopic("tutorial/requests");
                // Create the request content as a binary attachment
                requestMessage.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Request");

                // Send the request message to the Solace messaging router
                IMessage replyMessage = null;
                Console.WriteLine("Sending request...");
                ReturnCode returnCode = session.SendRequest(requestMessage, out replyMessage, TimeoutSeconds * 1000);
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    // Expecting reply as a binary attachment
                    reply = Encoding.ASCII.GetString(replyMessage.BinaryAttachment);
                }
                else
                {
                    Console.WriteLine("Request failed, return code: {0}", returnCode);
                }
            }
            return reply;
        }

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Solace Systems Messaging API Tutorial, Copyright 2008-2015 Solace Systems, Inc.");

            if ((args.Length < 1) || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Please provide a parameter: non-empty value for the Solace messaging router host name or IP address, e.g. \"BasicRequestor 192.168.1.111\"");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address

            const string defaultVPNName = "default"; // Solace messaging router VPN name
            const string defaultUsername = "tutorial"; // client username on the Solace messaging router VPN
            const int defaultTimeoutSeconds = 10; // request timeout

            // Initialize Solace Systems Messaging API with logging to console at Warning level
            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            try
            {
                // Context must be created first
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                {
                    // Create the application
                    BasicRequestor basicRequestor = new BasicRequestor()
                    {
                        VPNName = defaultVPNName,
                        UserName = defaultUsername,
                        TimeoutSeconds = defaultTimeoutSeconds
                    };

                    // Run the application within the context and against the host
                    basicRequestor.Run(context, host);
                    
                    // Write out the received reply
                    if (string.IsNullOrWhiteSpace(basicRequestor.Reply))
                    {
                        Console.WriteLine("Reply was not received in {0} seconds.", defaultTimeoutSeconds);
                    }
                    else
                    {
                        Console.WriteLine("Received reply: {0}", basicRequestor.Reply);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);
            }
            finally
            {
                // Dispose Solace Systems Messaging API
                ContextFactory.Instance.Cleanup();
            }
            Console.WriteLine("Finished.");
        }

        #endregion
    }

}
