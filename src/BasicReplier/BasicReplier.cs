#region Copyright & License
/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
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
        string Password { get; set; }

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
                Password = Password,
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
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: BasicReplier <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: BasicReplier <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address
            string username = split[0];
            string vpnname = split[1];
            string password = args[2];

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
                        VPNName = vpnname,
                        UserName = username,
                        Password = password
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
