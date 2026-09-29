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
/// Solace Systems Messaging API tutorial: HelloWorldAvroSerdeSMF
/// </summary>

namespace Solace.Samples.Avro
{
    /// <summary>
    /// HelloWorldAvroSerdeSMF
    /// This class demonstrates the usage of the Solace CSCSMP API with Avro serialization and deserialization.
    /// It connects to a Solace message broker, publishes a User message using Avro serialization, and consumes
    /// the message using Avro deserialization to a GenericRecord.
    /// </summary>
    static class HelloWorldAvroSerdeSMF
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";
        public static readonly string TopicName = "solace/samples/avro";

        // Path to the User Avro schema file, copied next to the application binary at build time.
        // The same schema must be registered in the Schema Registry under the topic name above.
        private static readonly string UserSchemaPath = Path.Combine(AppContext.BaseDirectory, "user.avsc");

        // Create a latch to synchronize the main thread with the message consumer
        private static ManualResetEventSlim latch = new ManualResetEventSlim(false);

        /// <summary>
        /// The main method that demonstrates the Solace CSCSMP API usage with Avro serialization/deserialization to a GenericRecord.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username@vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: HelloWorldAvroSerdeSMF <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: HelloWorldAvroSerdeSMF <host> <username>@<vpnname> <password>");
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
                // Create and configure Avro serializer and deserializer
                using (var deserializer = new AvroDeserializer<GenericRecord>())
                using (var serializer = new AvroSerializer<GenericRecord>())
                {
                    // Configure the Schema Registry connection for both serializer and deserializer
                    var config = GetSchemaRegistryConfig();
                    deserializer.Configure(config);
                    serializer.Configure(config);

                    // Wrap async serializer/deserializer with synchronous adapters for use with CSCSMP's synchronous message callbacks.
                    // CSCSMP's HandleMessage callback is invoked synchronously, but AvroSerializer/Deserializer are async by default.
                    // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations, making them compatible with CSCSMP.
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

                    // Create a CSCSMP session and set up the message event handler
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
        /// This event handler is invoked by the Solace Systems Messaging API when a message arrives.
        /// It deserializes the received message using Avro deserialization and displays the data.
        /// </summary>
        /// <param name="source">The source object that raised the event</param>
        /// <param name="args">The message event arguments containing the received message</param>
        /// <param name="deserializer">The Avro deserializer to use for deserialization</param>
        private static void HandleMessage(object source, MessageEventArgs args, IDeserializer<GenericRecord> deserializer)
        {
            try
            {
                Console.WriteLine("Received message, deserializing...");

                // Deserialize the message payload to a GenericRecord using the schema resolved from the registry
                GenericRecord user = args.Message.Deserialize(deserializer);

                // Display the deserialized data
                Console.WriteLine("Got a User GenericRecord: name={0}, id={1}, email={2}",
                    user["name"],
                    user["id"],
                    user["email"]);
            }
            catch (SerializationException ex)
            {
                // Handle cases where deserialization fails (e.g., schema resolution failure, schema mismatch)
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
        /// Creates a message, serializes it with Avro, publishes it to a topic, and waits for it to be received.
        /// </summary>
        /// <param name="session">The active CSCSMP session</param>
        /// <param name="serializer">The Avro serializer to use for message serialization</param>
        static void PublishAndWaitForRoundTrip(ISession session, ISerializer<GenericRecord> serializer)
        {
            // Set up the topic and subscribe to it
            ITopic topic = ContextFactory.Instance.CreateTopic(TopicName);
            session.Subscribe(topic, true);

            // Create and populate a User GenericRecord with sample data. The record is built from the
            // schema read from user.avsc, so it carries the schema the serializer uses to resolve/register
            // the artifact.
            var schema = (RecordSchema)Schema.Parse(File.ReadAllText(UserSchemaPath));
            var user = new GenericRecord(schema);
            user.Add("name", "John Doe");
            user.Add("id", "1");
            user.Add("email", "support@solace.com");

            // Serialize and send the message
            using (var message = ContextFactory.Instance.CreateMessage())
            {
                message.Destination = topic;
                // Serialize the GenericRecord to the message using Avro serialization.
                // This encodes the record and embeds the schema id in the message header.
                try
                {
                    message.Serialize(serializer, user);
                }
                catch (SerializationException ex)
                {
                    // Handle cases where serialization fails (e.g., schema resolution/registration failure)
                    Console.WriteLine("Serialization exception: {0}", ex.Message);
                    Console.WriteLine(ex);
                    return;
                }

                Console.WriteLine("Sending User Message:");
                Console.WriteLine("  Name: {0}", user["name"]);
                Console.WriteLine("  Id: {0}", user["id"]);
                Console.WriteLine("  Email: {0}", user["email"]);
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
        /// Returns a configuration dictionary for the Avro serializer and deserializer.
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
