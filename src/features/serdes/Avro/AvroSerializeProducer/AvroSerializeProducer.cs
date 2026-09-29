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
using System.IO;
using System.Threading;
using Avro;
using Avro.Generic;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;

/// <summary>
/// Solace Systems Messaging API tutorial: AvroSerializeProducer
/// </summary>

namespace Solace.Samples.Avro
{
    /// <summary>
    /// AvroSerializeProducer
    /// This class demonstrates how to use the Solace CSCSMP API with Avro serialization to produce messages.
    /// It connects to a Solace message broker, serializes User <see cref="GenericRecord"/> messages using Avro,
    /// and publishes them to a topic. The producer continuously sends messages until the user presses Enter to exit.
    /// </summary>
    static class AvroSerializeProducer
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/avro";

        // Path to the User Avro schema file, copied next to the application binary at build time.
        // The same schema must be registered in the Schema Registry under the topic name above.
        private static readonly string UserSchemaPath = Path.Combine(AppContext.BaseDirectory, "user.avsc");

        // Flag to signal when to stop sending messages
        private static volatile bool _keepRunning = true;

        /// <summary>
        /// The main method that demonstrates the Solace CSCSMP API usage with Avro serialization.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username&gt;@&lt;vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: AvroSerializeProducer <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: AvroSerializeProducer <host> <username>@<vpnname> <password>");
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
                // Create and configure the Avro serializer
                using (var serializer = new AvroSerializer<GenericRecord>())
                {
                    // Configure the Schema Registry connection for the serializer
                    var config = GetSchemaRegistryConfig();
                    serializer.Configure(config);

                    // Wrap the async serializer with a synchronous adapter for use with CSCSMP's synchronous message sending.
                    // CSCSMP's Send path is synchronous, but AvroSerializer is async by default.
                    // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations, making them compatible with CSCSMP.
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

                    // Create a CSCSMP session
                    using (ISession session = context.CreateSession(sessionProps, null, null))
                    {
                        // Connect to the session
                        ReturnCode returnCode = session.Connect();
                        if (returnCode == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected.");
                            Console.WriteLine("Press Enter to exit.");
                            Console.WriteLine();

                            // Start a thread to listen for the Enter key press
                            Thread exitThread = new Thread(() =>
                            {
                                Console.ReadLine();
                                _keepRunning = false;
                            });
                            exitThread.Start();

                            // Send messages continuously until the user presses Enter
                            ProduceMessages(session, syncSerializer);

                            // Wait for the exit thread to complete
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
        /// Continuously produces User <see cref="GenericRecord"/> messages and publishes them to the topic
        /// until the user exits.
        /// </summary>
        /// <param name="session">The active CSCSMP session</param>
        /// <param name="serializer">The Avro serializer to use for message serialization</param>
        static void ProduceMessages(ISession session, ISerializer<GenericRecord> serializer)
        {
            // Create the topic
            ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);

            // Create and populate a User GenericRecord with sample data. The record is built from the schema
            // read from user.avsc, so it carries the schema the serializer uses to resolve/register the artifact.
            // The same record instance is reused for every send; only its data changes between iterations.
            var schema = (RecordSchema)Schema.Parse(File.ReadAllText(UserSchemaPath));
            var user = new GenericRecord(schema);
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");
            // id is set at the top of each iteration below, since it changes every send.

            int index = 0;
            while (_keepRunning)
            {
                // Set this message's id
                user.Add("id", index.ToString());

                // Serialize and send the message. A fresh message is created per send inside a using block;
                // the GenericRecord above is what gets reused across iterations.
                using (var message = ContextFactory.Instance.CreateMessage())
                {
                    message.Destination = topic;

                    try
                    {
                        // Serialize the GenericRecord to the message using Avro serialization.
                        // This encodes the record and embeds the schema id in the message header.
                        message.Serialize(serializer, user);

                        Console.WriteLine("Sending Message: Name={0}, Id={1}, Email={2}",
                            user["name"], user["id"], user["email"]);

                        ReturnCode returnCode = session.Send(message);
                        if (returnCode != ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Failed to send message, return code: {0}", returnCode);
                        }
                    }
                    catch (SerializationException ex)
                    {
                        // Handle cases where serialization fails (e.g., schema resolution/registration failure, schema mismatch)
                        Console.WriteLine("Serialization exception: {0}", ex.Message);
                    }
                }

                index++;

                // Limit the send rate to make the sample output easy to observe.
                Thread.Sleep(100);
            }

            Console.WriteLine("Stopped sending messages.");
        }

        /// <summary>
        /// Returns a configuration dictionary for the Avro serializer.
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