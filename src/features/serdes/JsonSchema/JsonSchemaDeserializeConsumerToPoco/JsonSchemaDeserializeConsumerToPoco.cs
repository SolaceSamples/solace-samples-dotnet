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
using System.Threading;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;
using Resources.JsonSchema;

/// <summary>
/// Solace Messaging API tutorial: JsonSchemaDeserializeConsumerToPoco
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// JsonSchemaDeserializeConsumerToPoco
    /// This class demonstrates how to deserialize a JSON message payload to a Plain Old CLR Object (POCO).
    /// The JSON schema (user.json) being deserialized contains the 'customDotnetType' property, which specifies the target
    /// class for deserialization. Refer to the JsonSchemaPropertyKeys.TypeProperty configuration being done
    /// in GetSchemaRegistryConfig().
    /// </summary>
    static class JsonSchemaDeserializeConsumerToPoco
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/json";

        // Create a latch to signal when the user presses Enter to exit
        private static ManualResetEventSlim exitLatch = new ManualResetEventSlim(false);

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with JSON Schema deserialization to a POCO.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username@vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: JsonSchemaDeserializeConsumerToPoco <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: JsonSchemaDeserializeConsumerToPoco <host> <username>@<vpnname> <password>");
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
                // Create and configure JSON Schema deserializer
                using (var deserializer = new JsonSchemaDeserializer<User>())
                {
                    // Configure the Schema Registry connection for the deserializer
                    var config = GetSchemaRegistryConfig();
                    deserializer.Configure(config);

                    // Wrap async deserializer with synchronous adapter for use with synchronous message callbacks.
                    // The HandleMessage callback is invoked synchronously, but JsonSchemaDeserializer is async by default.
                    // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations.
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

                    // Create a Solace session and set up the message event handler
                    using (ISession session = context.CreateSession(sessionProps, (source, msgArgs) => HandleMessage(source, msgArgs, syncDeserializer), null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected.");

                            // Set up the topic and subscribe to it
                            ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);
                            session.Subscribe(topic, true);
                            Console.WriteLine("Subscribed to topic: {0}", TopicName);
                            Console.WriteLine("Waiting for messages... Press Enter to exit.");

                            // Start a thread to listen for Enter key press
                            Thread exitThread = new Thread(() =>
                            {
                                Console.ReadLine();
                                exitLatch.Set();
                            });
                            exitThread.Start();

                            // Wait for exit signal
                            exitLatch.Wait();
                            exitThread.Join();
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
        /// It deserializes the received message using JSON Schema deserialization to a User POCO and displays the data.
        /// </summary>
        /// <param name="source">The source object that raised the event</param>
        /// <param name="args">The message event arguments containing the received message</param>
        /// <param name="deserializer">The JSON Schema deserializer to use for deserialization</param>
        private static void HandleMessage(object source, MessageEventArgs args, IDeserializer<User> deserializer)
        {
            try
            {
                // Deserialize the message payload to a User POCO using JSON Schema validation
                // Note: the 'customDotnetType' property in the schema specifies the 'User' class, so the deserializer returns a User object.
                User user = args.Message.Deserialize(deserializer);
                Console.WriteLine("Got message: Name={0}, Id={1}, Email={2}",
                    user.Name, user.Id, user.Email);
            }
            catch (JsonSchemaValidationException ve)
            {
                Console.WriteLine("Received Message with invalid payload:");
                Console.WriteLine("Validation error: {0}", ve.Message);
            }
            catch (SerializationException ex)
            {
                Console.WriteLine("Received Message with payload that can not be decoded:");
                Console.WriteLine("Decoding error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Returns a configuration dictionary for the JSON Schema deserializer.
        /// Contains the Schema Registry URL and authentication credentials, as well as the type property
        /// that specifies which JSON Schema property to use for determining the target .NET type.
        /// </summary>
        /// <returns>A dictionary containing configuration properties</returns>
        private static Dictionary<string, object> GetSchemaRegistryConfig()
        {
            return new Dictionary<string, object>
            {
                { SchemaResolverPropertyKeys.RegistryUrl, RegistryUrl },
                { SchemaResolverPropertyKeys.AuthUsername, RegistryUsername },
                { SchemaResolverPropertyKeys.AuthPassword, RegistryPassword },
                { JsonSchemaPropertyKeys.TypeProperty, "customDotnetType" }
            };
        }
    }
}
