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
        string Password { get; set; }
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
                Password = Password,
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
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: BasicRequestor <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: BasicRequestor <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string host = args[0]; // Solace messaging router host name or IP address
            string username = split[0];
            string vpnname = split[1];
            string password = args[2];
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
                        VPNName = vpnname,
                        UserName = username,
                        Password = password,
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
