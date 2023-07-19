﻿#region Copyright & License
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
/// Solace Systems Messaging API tutorial: QueueProducer
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// Demonstrates how to use Solace Systems Messaging API for sending and receiving a guaranteed delivery message
    /// </summary>
    class QueueProducer
    {
        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }

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
                    ProduceMessage(session);
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

        private void ProduceMessage(ISession session)
        {

            // Provision the queue
            string queueName = "Q/tutorial";
            Console.WriteLine("Attempting to provision the queue '{0}'...", queueName);

            // Create the queue
            using (IQueue queue = ContextFactory.Instance.CreateQueue(queueName))
            {
                // Set queue permissions to "consume" and access-type to "exclusive"
                EndpointProperties endpointProps = new EndpointProperties()
                {
                    Permission = EndpointProperties.EndpointPermission.Consume,
                    AccessType = EndpointProperties.EndpointAccessType.Exclusive
                };
                // Provision it, and do not fail if it already exists
                session.Provision(queue, endpointProps,
                    ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists | ProvisionFlag.WaitForConfirm, null);
                Console.WriteLine("Queue '{0}' has been created and provisioned.", queueName);

                // Create the message
                using (IMessage message = ContextFactory.Instance.CreateMessage())
                {
                    // Message's destination is the queue and the message is persistent
                    message.Destination = queue;
                    message.DeliveryMode = MessageDeliveryMode.Persistent;
                    // Create the message content as a binary attachment
                    message.BinaryAttachment = Encoding.ASCII.GetBytes("Persistent Queue Tutorial");

                    // Send the message to the queue on the Solace messaging router
                    Console.WriteLine("Sending message to queue {0}...", queueName);
                    ReturnCode returnCode = session.Send(message);
                    if (returnCode == ReturnCode.SOLCLIENT_OK)
                    {
                        // Delivery not yet confirmed. See ConfirmedPublish.cs
                        Console.WriteLine("Done.");
                    }
                    else
                    {
                        Console.WriteLine("Sending failed, return code: {0}", returnCode);
                    }
                }
            }
        }

        #region Main
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: QueueProducer <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: QueueProducer <host> <username>@<vpnname> <password>");
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
                    QueueProducer queueProducer = new QueueProducer()
                    {
                        VPNName = vpnname,
                        UserName = username,
                        Password = password
                    };

                    // Run the application within the context and against the host
                    queueProducer.Run(context, host);
                    
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
