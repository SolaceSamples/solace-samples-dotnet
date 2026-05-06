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
using Solace.Serdes;
using Resources.JsonSchema;

/// <summary>
/// Solace Messaging API tutorial: JsonSchemaSerializeProducer
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// JsonSchemaSerializeProducer
    /// This class demonstrates how to use Solace Messaging API for .NET with JSON Schema serialization to produce messages.
    /// It connects to a Solace message broker, serializes User messages using JSON Schema, and publishes them to a topic.
    /// The producer continuously sends messages until the user presses Enter to exit.
    /// </summary>
    static class JsonSchemaSerializeProducer
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/json";

        // Flag to signal when to stop sending messages
        private static volatile bool _keepRunning = true;

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with JSON Schema serialization.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username&gt;@&lt;vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: JsonSchemaSerializeProducer <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: JsonSchemaSerializeProducer <host> <username>@<vpnname> <password>");
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
                // Create and configure JSON Schema serializer
                using (var serializer = new JsonSchemaSerializer<User>())
                {
                    // Configure the Schema Registry connection for the serializer
                    var config = GetSchemaRegistryConfig();
                    serializer.Configure(config);

                    // Wrap async serializer with synchronous adapter for use with Solace's synchronous message sending.
                    // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations.
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

                    // Create a Solace session
                    using (ISession session = context.CreateSession(sessionProps, null, null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected.");
                            Console.WriteLine("Press Enter to exit.");
                            Console.WriteLine();

                            // Start a thread to listen for Enter key press
                            Thread exitThread = new Thread(() =>
                            {
                                Console.ReadLine();
                                _keepRunning = false;
                            });
                            exitThread.Start();

                            // Send messages continuously until user presses Enter
                            ProduceMessages(session, syncSerializer);

                            // Wait for exit thread to complete
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
        /// Continuously produces User messages and publishes them to the topic until the user exits.
        /// </summary>
        /// <param name="session">The active Solace session</param>
        /// <param name="serializer">The JSON Schema serializer to use for message serialization</param>
        static void ProduceMessages(ISession session, ISerializer<User> serializer)
        {
            // Create the topic
            ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);

            // Create and populate a User object with sample data
            var user = new User
            {
                Name = "John Doe",
                Id = "-1",
                Email = "support@solace.com"
            };

            int index = 0;
            while (_keepRunning)
            {
                // Update message with current index
                user.Id = index.ToString();

                // Serialize and send the message
                using (var message = ContextFactory.Instance.CreateMessage())
                {
                    message.Destination = topic;

                    try
                    {
                        // Serialize the User to the message using JSON Schema serialization
                        // This validates the data against the schema and embeds schema metadata in the message header
                        message.Serialize(serializer, user);

                        Console.WriteLine("Sending Message: Name={0}, Id={1}, Email={2}",
                            user.Name, user.Id, user.Email);

                        ReturnCode returnCode = session.Send(message);
                        if (returnCode != ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Failed to send message, return code: {0}", returnCode);
                        }
                    }
                    catch (SerializationException ex)
                    {
                        // Handle cases where serialization fails (e.g., validation error, schema mismatch)
                        Console.WriteLine("Serialization exception: {0}", ex.Message);
                    }
                }

                index++;

                // Limit send rate to facilitate user's observation of sample output.
                Thread.Sleep(100);
            }

            Console.WriteLine("Stopped sending messages.");
        }

        /// <summary>
        /// Returns a configuration dictionary for the JSON Schema serializer.
        /// Contains the Schema Registry URL and authentication credentials.
        /// </summary>
        /// <returns>A dictionary containing configuration properties</returns>
        private static Dictionary<string, object> GetSchemaRegistryConfig()
        {
            return new Dictionary<string, object>
            {
                { JsonSchemaPropertyKeys.RegistryUrl, RegistryUrl },
                { JsonSchemaPropertyKeys.AuthUsername, RegistryUsername },
                { JsonSchemaPropertyKeys.AuthPassword, RegistryPassword }
            };
        }
    }
}
