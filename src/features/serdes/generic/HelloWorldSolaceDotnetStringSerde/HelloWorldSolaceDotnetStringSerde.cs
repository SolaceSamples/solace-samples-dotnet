/*
 * Copyright 2026 Solace Corporation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.Serdes;

/// <summary>
/// Solace Systems Messaging API tutorial: HelloWorldSolaceDotnetStringSerde
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// HelloWorldSolaceDotnetStringSerde
    /// This class demonstrates the usage of Solace Messaging API for .NET with StringSerializer and StringDeserializer.
    /// It connects to a Solace message broker, serializes strings with StringSerializer and publishes
    /// the messages to a topic, then consumes the messages and deserializes them with StringDeserializer.
    /// </summary>
    static class HelloWorldSolaceDotnetStringSerde
    {
        // Create a latch to synchronize the main thread with the message consumer
        private static ManualResetEventSlim latch = new ManualResetEventSlim(false);

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with String serialization/deserialization.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host:port&gt; &lt;message-vpn&gt; &lt;client-username&gt; [password]</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: HelloWorldSolaceDotnetStringSerde <host:port> <message-vpn> <client-username> [password]");
                return 1;
            }

            string host = args[0];
            string vpnname = args[1];
            string username = args[2];
            string password = args.Length > 3 ? args[3] : "";

            // Initialize Solace Systems Messaging API with logging to console at Warning level
            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            try
            {
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                {
                    // Create serializer and deserializer
                    var serializer = new StringSerializer();
                    var deserializer = new StringDeserializer();

                    // Create session properties for the Solace message broker connection
                    SessionProperties sessionProps = new SessionProperties()
                    {
                        Host = host,
                        VPNName = vpnname,
                        UserName = username,
                        Password = password,
                    };

                    // Connect to the Solace messaging router
                    Console.WriteLine("Connecting as {0}@{1} on {2}...", username, vpnname, host);

                    // Create a Solace session and set up the message event handler
                    // NOTICE: HandleMessage is passed as the message event handler with the deserializer
                    using (ISession session = context.CreateSession(sessionProps, (source, msgArgs) => HandleMessage(source, msgArgs, deserializer), null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected.");
                            PublishAndWaitForRoundTrip(session, serializer);
                        }
                        else
                        {
                            Console.WriteLine("Error connecting, return code: {0}", returnCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown: {0}", ex.Message);
                return 1;
            }
            finally
            {
                Console.WriteLine("Cleaning up.");
                ContextFactory.Instance.Cleanup();
            }
            Console.WriteLine("Finished.");
            return 0;
        }

        /// <summary>
        /// This event handler is invoked by Solace Systems Messaging API when a message arrives.
        /// It deserializes the received message using String deserialization and displays the data.
        /// </summary>
        /// <param name="source">The source object that raised the event</param>
        /// <param name="args">The message event arguments containing the received message</param>
        /// <param name="deserializer">The String deserializer to use for deserialization</param>
        private static void HandleMessage(object source, MessageEventArgs args, IDeserializer<string> deserializer)
        {
            try
            {
                // Deserialize the received message using the message extension method
                string receivedText = args.Message.Deserialize(deserializer);

                Console.WriteLine("MessageText recv: " + receivedText);
                Console.WriteLine("Recv data:");
                Console.WriteLine(args.Message.Dump());
            }
            finally
            {
                // Signal the main thread that a message has been received
                latch.Set();
            }
        }

        /// <summary>
        /// Main execution method that coordinates the sample workflow.
        /// Publishes messages using String serialization and waits for them to be received.
        /// </summary>
        /// <param name="session">The active Solace session</param>
        /// <param name="serializer">The String serializer to use for message serialization</param>
        static void PublishAndWaitForRoundTrip(ISession session, ISerializer<string> serializer)
        {
            // Set up the topic and subscribe to it
            ITopic topic = ContextFactory.Instance.CreateTopic("topic");
            session.Subscribe(topic, true);

            // Send and receive first message: "Hello World"
            SendAndWaitForMessage(session, topic, serializer, "Hello World");

            // Send and receive second message: "Hello World 2"
            SendAndWaitForMessage(session, topic, serializer, "Hello World 2");
        }

        /// <summary>
        /// Sends a message and waits for it to be received.
        /// </summary>
        /// <param name="session">The active Solace session</param>
        /// <param name="topic">The topic to send the message to</param>
        /// <param name="serializer">The String serializer to use for message serialization</param>
        /// <param name="messageText">The text to serialize and send</param>
        private static void SendAndWaitForMessage(ISession session, ITopic topic,
            ISerializer<string> serializer, string messageText)
        {
            // Reset the latch for this message
            latch.Reset();

            // Create and send the message
            using (IMessage message = ContextFactory.Instance.CreateMessage())
            {
                message.Destination = topic;

                // Serialize the message text using the message extension method
                message.Serialize(serializer, messageText);

                session.Send(message);

                Console.WriteLine("Message Text sent: " + messageText);
                Console.WriteLine("Data sent:");
                Console.WriteLine(message.Dump());
            }

            // Wait for the message to be received
            bool received = latch.Wait(TimeSpan.FromSeconds(10));
            if (!received)
            {
                Console.WriteLine("Timeout waiting for message.");
            }
        }
    }
}
