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
using System.Threading;

/// <summary>
/// Solace Systems Messaging API tutorial: BasicReplier
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// Demonstrates how to use Solace Systems Messaging API for receiving a request and sending a reply 
    /// </summary>
    class BasicReplier : IDisposable
    {
        string VPNName { get; set; }
        string UserName { get; set; }

        const int DefaultReconnectRetries = 3;

        private ISession Session = null;
        private EventWaitHandle WaitEventWaitHandle = new AutoResetEvent(false);

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
            // NOTICE HandleRequestMessage as the message event handler
            Session = context.CreateSession(sessionProps, HandleRequestMessage, null);
            ReturnCode returnCode = Session.Connect();
            if (returnCode == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine("Session successfully connected.");

                // This is the topic on Solace messaging router where a request is placed
                // The reply must subscribe to it to receive requests
                Session.Subscribe(ContextFactory.Instance.CreateTopic("tutorial/requests"), true);

                Console.WriteLine("Waiting for a request to come in...");
                WaitEventWaitHandle.WaitOne();
            }
            else
            {
                Console.WriteLine("Error connecting, return code: {0}", returnCode);
            }
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a message arrives
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleRequestMessage(object source, MessageEventArgs args)
        {
            Console.WriteLine("Received request.");
            // Received a request message
            using (IMessage requestMessage = args.Message)
            {
                // Expecting the request content as a binary attachment
                Console.WriteLine("Request content: {0}", Encoding.ASCII.GetString(requestMessage.BinaryAttachment));
                // Create reply message
                using (IMessage replyMessage = ContextFactory.Instance.CreateMessage())
                {
                    // Set the reply content as a binary attachment 
                    replyMessage.BinaryAttachment = Encoding.ASCII.GetBytes("Sample Reply");
                    Console.WriteLine("Sending reply...");
                    ReturnCode returnCode = Session.SendReply(requestMessage, replyMessage);
                    if (returnCode == ReturnCode.SOLCLIENT_OK)
                    {
                        Console.WriteLine("Sent.");
                    }
                    else
                    {
                        Console.WriteLine("Reply failed, return code: {0}", returnCode);
                    }
                    // finish the program
                    WaitEventWaitHandle.Set();
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Session != null)
                    {
                        Session.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        #region Main
        static void Main(string[] args)
        {
            Console.WriteLine("Solace Systems Messaging API Tutorial, Copyright 2008-2015 Solace Systems, Inc.");

            if ((args.Length < 1) || string.IsNullOrWhiteSpace(args[0]))
            {
                Console.WriteLine("Please provide a parameter: non-empty value for the Solace messaging router host name or IP address, e.g. \"BasicReplier 192.168.1.111\"");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address

            const string defaultVPNName = "default"; // Solace messaging router VPN name
            const string defaultUsername = "tutorial"; // client username on the Solace messaging router VPN

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
                    using (BasicReplier basicReplier = new BasicReplier()
                    {
                        VPNName = defaultVPNName,
                        UserName = defaultUsername,
                    })
                    {
                        // Run the application within the context and against the host
                        basicReplier.Run(context, host);
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
