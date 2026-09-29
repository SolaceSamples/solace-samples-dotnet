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
using System.Collections.Generic;
using Avro.Generic;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;

/// <summary>
/// Solace Systems Messaging API tutorial: AvroDeserializeConsumer
/// </summary>

namespace Solace.Samples.Avro
{
    /// <summary>
    /// AvroDeserializeConsumer
    /// This class demonstrates how to use the Solace CSCSMP API with Avro deserialization to consume messages.
    /// It connects to a Solace message broker, subscribes to a topic, and deserializes each received message into
    /// a User <see cref="GenericRecord"/>. The consumer runs continuously until the user presses Enter to exit.
    /// </summary>
    static class AvroDeserializeConsumer
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/avro";

        /// <summary>
        /// The main method that demonstrates the Solace CSCSMP API usage with Avro deserialization.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username&gt;@&lt;vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: AvroDeserializeConsumer <host> <username>@<vpnname> <password>");
                Console.WriteLine();
                Console.WriteLine("Schema Registry connection can be configured via environment variables:");
                Console.WriteLine("  REGISTRY_URL (default: http://localhost:8081/apis/registry/v3)");
                Console.WriteLine("  REGISTRY_USERNAME (default: sr-readonly)");
                Console.WriteLine("  REGISTRY_PASSWORD (default: roPassword)");
                return 1;
            }

            // Extract connection details from command line arguments
            string[] split = args[1].Split('@');
            if (split.Length != 2)
            {
                Console.WriteLine("Usage: AvroDeserializeConsumer <host> <username>@<vpnname> <password>");
                return 1;
            }

            string host = args[0];
            string userName = split[0];
            string vpnName = split[1];
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
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                // Create and configure the Avro deserializer
                using (var deserializer = new AvroDeserializer<GenericRecord>())
                {
                    // Configure the Schema Registry connection for the deserializer
                    var config = GetSchemaRegistryConfig();
                    deserializer.Configure(config);

                    // Wrap the async deserializer with a synchronous adapter for use with CSCSMP's synchronous
                    // message callback. CSCSMP invokes the message handler synchronously, but AvroDeserializer is
                    // async by default; AsSyncOverAsync() blocks on the async work so it can be called from the callback.
                    var syncDeserializer = deserializer.AsSyncOverAsync();

                    // Create session properties for the Solace message broker connection
                    SessionProperties sessionProps = new SessionProperties()
                    {
                        Host = host,
                        VPNName = vpnName,
                        UserName = userName,
                        Password = password,
                    };

                    // Connect to the Solace messaging router
                    Console.WriteLine("Connecting as {0}@{1} on {2}...", userName, vpnName, host);

                    // Create a CSCSMP session and register the message handler. Unlike a pull-based API, CSCSMP
                    // delivers direct-topic messages by invoking HandleMessage on its own dispatch thread for
                    // every message that arrives — this consumer never asks for a message itself.
                    using (ISession session = context.CreateSession(sessionProps, (source, msgArgs) => HandleMessage(source, msgArgs, syncDeserializer), null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode != ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Error connecting, return code: {0}", returnCode);
                            return 1;
                        }

                        Console.WriteLine("Session successfully connected.");

                        // Subscribe to the topic the producer publishes to. `true` waits for the subscription
                        // to be confirmed by the broker before returning.
                        ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);
                        session.Subscribe(topic, true);
                        Console.WriteLine("Subscribed to topic: {0}", TopicName);

                        // Block the main thread until the user presses Enter. Messages are delivered to
                        // HandleMessage on the API's dispatch thread in the meantime, so nothing needs to
                        // happen here except wait.
                        Console.WriteLine("Waiting for messages. Press Enter to exit.");
                        Console.WriteLine();
                        Console.ReadLine();

                        Console.WriteLine("Exit requested — unsubscribing and shutting down.");
                        session.Unsubscribe(topic, true);
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
        /// This event handler is invoked by the Solace Systems Messaging API on its dispatch thread whenever a
        /// message arrives. It deserializes the received message into a User <see cref="GenericRecord"/> and prints
        /// the field values. A deserialization failure is logged and swallowed so the consumer keeps running.
        /// </summary>
        /// <param name="source">The source object that raised the event</param>
        /// <param name="args">The message event arguments containing the received message</param>
        /// <param name="deserializer">The Avro deserializer to use for deserialization</param>
        private static void HandleMessage(object source, MessageEventArgs args, IDeserializer<GenericRecord> deserializer)
        {
            try
            {
                // Deserialize the message payload into a GenericRecord using the schema resolved from the registry
                // (the schema id travels in the message header, so the consumer needs no local copy of the schema).
                GenericRecord user = args.Message.Deserialize(deserializer);

                Console.WriteLine("Received Message: Name={0}, Id={1}, Email={2}",
                    user["name"], user["id"], user["email"]);
            }
            catch (SerializationException ex)
            {
                // Handle cases where deserialization fails (e.g., schema resolution failure, schema mismatch, a
                // message written with an incompatible schema). Log it and keep consuming — one bad message must
                // not stop the consumer.
                Console.WriteLine("Deserialization exception: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Returns a configuration dictionary for the Avro deserializer.
        /// Contains the Schema Registry URL and authentication credentials.
        /// </summary>
        /// <returns>A dictionary containing configuration properties</returns>
        private static Dictionary<string, object> GetSchemaRegistryConfig()
        {
            return new Dictionary<string, object>
            {
                { SchemaResolverPropertyKeys.RegistryUrl, RegistryUrl },
                { SchemaResolverPropertyKeys.AuthUsername, RegistryUsername },
                { SchemaResolverPropertyKeys.AuthPassword, RegistryPassword }
            };
        }
    }
}
