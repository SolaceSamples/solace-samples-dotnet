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
using System.Text.Json.Nodes;
using System.Threading;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;

/// <summary>
/// Solace Messaging API tutorial: HelloWorldSolaceDotNetJsonSchemaSerde
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// HelloWorldSolaceDotNetJsonSchemaSerde
    /// This class demonstrates the usage of Solace Messaging API for .NET with JSON Schema serialization and deserialization.
    /// It connects to a Solace message broker, publishes a ClockInOut message using JSON Schema serialization, and consumes
    /// the message using JSON Schema deserialization to JsonNode.
    /// </summary>
    static class HelloWorldSolaceDotNetJsonSchemaSerde
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/clock-in-out/json";

        // Create a latch to synchronize the main thread with the message consumer
        private static ManualResetEventSlim latch = new ManualResetEventSlim(false);

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with JSON Schema serialization/deserialization to JsonNode.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username@vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: HelloWorldSolaceDotNetJsonSchemaSerde <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: HelloWorldSolaceDotNetJsonSchemaSerde <host> <username>@<vpnname> <password>");
                return 1;
            }

            string host = args[0];
            string userName = split[0];
            string vpnName = split[1];
            string password = args[2];

            // Initialize Solace Messaging API with logging to console at Warning level
            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);

            try
            {
                using (IContext context = ContextFactory.Instance.CreateContext(new ContextProperties(), null))
                // Create and configure JSON Schema serializer and deserializer
                using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
                using (var serializer = new JsonSchemaSerializer<JsonNode>())
                {
                    // Configure the Schema Registry connection for both serializer and deserializer
                    var config = GetSchemaRegistryConfig();
                    deserializer.Configure(config);
                    serializer.Configure(config);

                    // Wrap async serializer/deserializer with synchronous adapters for use with Solace's synchronous message callbacks.
                    // Solace's HandleMessage callback is invoked synchronously, but JsonSchemaSerializer/Deserializer are async by default.
                    // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations, making them compatible with the Solace Messaging API for .NET.
                    var syncDeserializer = deserializer.AsSyncOverAsync();
                    var syncSerializer = serializer.AsSyncOverAsync();

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

                    // Create a Solace session and set up the message event handler
                    // NOTICE: HandleMessage is passed as the message event handler with the synchronous deserializer
                    using (ISession session = context.CreateSession(sessionProps, (source, msgArgs) => HandleMessage(source, msgArgs, syncDeserializer), null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected.");
                            PublishAndWaitForRoundTrip(session, syncSerializer);
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
        /// This event handler is invoked by Solace Messaging API when a message arrives.
        /// It deserializes the received message using JSON Schema deserialization and displays the data.
        /// </summary>
        /// <param name="source">The source object that raised the event</param>
        /// <param name="args">The message event arguments containing the received message</param>
        /// <param name="deserializer">The JSON Schema deserializer to use for deserialization</param>
        private static void HandleMessage(object source, MessageEventArgs args, IDeserializer<JsonNode> deserializer)
        {
            try
            {
                Console.WriteLine("Received message, deserializing...");

                // Deserialize the message payload to a JsonNode using JSON Schema validation
                JsonNode clockInOutData = args.Message.Deserialize(deserializer);

                // Display the deserialized data
                Console.WriteLine("Got a ClockInOut JsonNode: {0}", clockInOutData.ToJsonString());
                Console.WriteLine("Employee {0} clocked in/out at store {1} in region {2} at {3}",
                    clockInOutData["employee_id"]?.GetValue<string>(),
                    clockInOutData["store_id"]?.GetValue<string>(),
                    clockInOutData["region_code"]?.GetValue<string>(),
                    clockInOutData["datetime"]?.GetValue<string>());
            }
            catch (SerializationException ex)
            {
                // Handle cases where deserialization fails (e.g., validation error, schema mismatch)
                Console.WriteLine("Deserialization exception: {0}", ex.Message);
            }
            finally
            {
                // Signal the main thread that a message has been received
                latch.Set();
            }
        }

        /// <summary>
        /// Main execution method that coordinates the sample workflow.
        /// Creates a message, serializes it with JSON Schema, publishes it to a topic, and waits for it to be received.
        /// </summary>
        /// <param name="session">The active Solace session</param>
        /// <param name="serializer">The JSON Schema serializer to use for message serialization</param>
        static void PublishAndWaitForRoundTrip(ISession session, ISerializer<JsonNode> serializer)
        {
            // Set up the topic and subscribe to it
            ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);
            session.Subscribe(topic, true);

            // Create and populate a ClockInOut JsonNode with sample data
            var clockInOut = new JsonObject
            {
                ["region_code"] = "NA-WEST",
                ["store_id"] = "STORE-001",
                ["employee_id"] = "EMP-12345",
                ["datetime"] = "2026-01-14T15:30:00Z"
            };

            // Serialize and send the message
            using (var message = ContextFactory.Instance.CreateMessage())
            {
                message.Destination = topic;
                // Serialize the JsonNode to the message using JSON Schema serialization
                // This validates the data against the schema and embeds schema metadata in the message header
                try
                {
                    message.Serialize(serializer, clockInOut);
                }
                catch (SerializationException ex) {
                    // Handle cases where serialization fails (e.g., validation error, schema mismatch)
                    Console.WriteLine("Serialization exception: {0}", ex.Message);
                    Console.WriteLine(ex);
                    return;
                }

                Console.WriteLine("Sending ClockInOut Message:");
                Console.WriteLine("  Region: {0}", clockInOut["region_code"]?.GetValue<string>());
                Console.WriteLine("  Store: {0}", clockInOut["store_id"]?.GetValue<string>());
                Console.WriteLine("  Employee: {0}", clockInOut["employee_id"]?.GetValue<string>());
                Console.WriteLine("  DateTime: {0}", clockInOut["datetime"]?.GetValue<string>());
                Console.WriteLine("  Payload size: {0} bytes", message.BinaryAttachment.Length);

                ReturnCode returnCode = session.Send(message);
                if (returnCode == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Message sent successfully.");
                }
                else
                {
                    Console.WriteLine("Failed to send message, return code: {0}", returnCode);
                }
            }

            // Wait for the consumer to receive the message
            Console.WriteLine("Waiting for message...");
            bool received = latch.Wait(TimeSpan.FromSeconds(10));
            if (!received)
            {
                Console.WriteLine("Timeout waiting for message.");
            }
        }

        /// <summary>
        /// Returns a configuration dictionary for the JSON Schema serializer and deserializer.
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
