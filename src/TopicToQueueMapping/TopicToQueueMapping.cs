
/*
 * Copyright 2010-2026 Solace Corporation. All rights reserved.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

/// <summary>
/// Solace Systems Messaging API tutorial: TopicToQueueMapping
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// Demonstrates how to use Solace Systems Messaging API for mapping a topic to a queue
    /// </summary>
    class TopicToQueueMapping : IDisposable
    {
        string VPNName { get; set; }
        string UserName { get; set; }
        string Password { get; set; }

        const int DefaultReconnectRetries = 3;
        const int TotalMessages = 5;

        CountdownEvent CountdownEvent = new CountdownEvent(TotalMessages);

        IFlow Flow = null;

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
                ReconnectRetries = DefaultReconnectRetries,
                IgnoreDuplicateSubscriptionError = true
            };

            // Connect to the Solace messaging router
            Console.WriteLine("Connecting as {0}@{1} on {2}...", UserName, VPNName, host);
            using (ISession session = context.CreateSession(sessionProps, null, null))
            {
                ReturnCode returnCode = session.Connect();
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected.");

                    if (session.IsCapable(CapabilityType.PUB_GUARANTEED) &&
                        session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED) &&
                        session.IsCapable(CapabilityType.ENDPOINT_MANAGEMENT) &&
                        session.IsCapable(CapabilityType.QUEUE_SUBSCRIPTIONS))
                    {
                        Console.WriteLine("All required capabilities supported.");
                        SubscribeAndSendMessage(session);
                    }
                    else
                    {
                        Console.WriteLine("Required capabilities are not supported.");
                        if (!session.IsCapable(CapabilityType.PUB_GUARANTEED))
                        {
                            throw new InvalidOperationException("Cannot proceed because session's PUB_GUARANTEED capability is not supported.");
                        }
                        if (!session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED))
                        {
                            throw new InvalidOperationException("Cannot proceed because session's SUB_FLOW_GUARANTEED capability is not supported.");
                        }
                        if (!session.IsCapable(CapabilityType.ENDPOINT_MANAGEMENT))
                        {
                            throw new InvalidOperationException("Cannot proceed because session's ENDPOINT_MANAGEMENT capability is not supported.");
                        }
                        if (!session.IsCapable(CapabilityType.QUEUE_SUBSCRIPTIONS))
                        {
                            throw new InvalidOperationException("Cannot proceed because session's QUEUE_SUBSCRIPTIONS capability is not supported.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Error connecting, return code: {0}", returnCode);
                }
            }
        }

        private void SubscribeAndSendMessage(ISession session)
        {
            // Provision the queue
            string queueName = "Q/tutorial/topicToQueueMapping";
            Console.WriteLine("Attempting to provision the queue '{0}'...", queueName);

            // Create the queue
            using (IQueue queue = ContextFactory.Instance.CreateQueue(queueName))
            {
                // Set queue permissions to "consume" and access-type to "exclusive"
                EndpointProperties endpointProps = new EndpointProperties()
                {
                    Permission = EndpointProperties.EndpointPermission.Consume,
                    AccessType = EndpointProperties.EndpointAccessType.Exclusive,
                };
                // Provision it, and do not fail if it already exists
                session.Provision(queue, endpointProps,
                    ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists | ProvisionFlag.WaitForConfirm, null);
                Console.WriteLine("Queue '{0}' has been created and provisioned.", queueName);

                // Add subscription to the topic mapped to the queue
                ITopic tutorialTopic = ContextFactory.Instance.CreateTopic("T/mapped/topic/sample");
                session.Subscribe(queue, tutorialTopic, SubscribeFlag.WaitForConfirm, null);

                // Create the message
                using (IMessage message = ContextFactory.Instance.CreateMessage())
                {
                    // Message's destination is the queue topic and the message is persistent
                    message.Destination = tutorialTopic;
                    message.DeliveryMode = MessageDeliveryMode.Persistent;

                    // Send it to the mapped topic a few times with different content
                    for (int i = 0; i < TotalMessages; i++)
                    {
                        // Create the message content as a binary attachment
                        message.BinaryAttachment = Encoding.ASCII.GetBytes(
                            string.Format("Topic to Queue Mapping Tutorial! Message ID: {0}", i));

                        // Send the message to the queue on the Solace messaging router
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
                Console.WriteLine("{0} messages sent. Processing replies.", TotalMessages);

                // Create and start flow to the newly provisioned queue
                // NOTICE HandleMessageEvent as the message event handler 
                // and HandleFlowEvent as the flow event handler
                Flow = session.CreateFlow(new FlowProperties()
                {
                    AckMode = MessageAckMode.ClientAck
                },
                queue, null, HandleMessageEvent, HandleFlowEvent);
                Flow.Start();

                // block the current thread until a confirmation received
                CountdownEvent.Wait();
            }
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a message arrives
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleMessageEvent(object source, MessageEventArgs args)
        {
            // Received a message
            Console.WriteLine("Received message.");
            using (IMessage message = args.Message)
            {
                // Expecting the message content as a binary attachment
                Console.WriteLine("Message content: {0}", Encoding.ASCII.GetString(message.BinaryAttachment));
                // ACK the message
                Flow.Ack(message.ADMessageId);
                CountdownEvent.Signal();
            }
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a flwo event happens
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void HandleFlowEvent(object sender, FlowEventArgs args)
        {
            // Received a flow event
            Console.WriteLine("Received Flow Event '{0}' Type: '{1}' Text: '{2}'",
                args.Event,
                args.ResponseCode.ToString(),
                args.Info);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Flow.Dispose();
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
                Console.WriteLine("Usage: TopicToQueueMapping <host> <username>@<vpnname> <password>");
                Environment.Exit(1);
            }

            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: TopicToQueueMapping <host> <username>@<vpnname> <password>");
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
                // Create the application
                using (TopicToQueueMapping topicToQueueMapping = new TopicToQueueMapping()
                {
                    VPNName = vpnname,
                    UserName = username,
                    Password = password
                })
                {
                    // Run the application within the context and against the host
                    topicToQueueMapping.Run(context, host);
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
