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
/// Solace Messaging API tutorial: AvroRequestor
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// AvroRequestor
    /// This class demonstrates request/reply messaging over the Solace Messaging API for .NET with Avro serialization. It sends
    /// Avro-serialized CreateUser request messages and blocks for an Avro-serialized CreateUserResponse
    /// reply per request, deserializing and printing each reply. It runs continuously until the user
    /// presses Enter. Pair it with AvroReplier.
    /// </summary>
    static class AvroRequestor
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";

        public const string RequestTopicName = "solace/samples/create-user/avro";
        public const string ReplyTopicName = "solace/samples/create-user-response/avro";
        private const int RequestTimeoutMs = 5000;

        // Path to the CreateUser request schema, linked from the Resources project and copied next to the
        // binary at build time. The requestor builds the request record from it; the reply schema is
        // resolved from the registry, so no reply schema file is needed.
        private static readonly string RequestSchemaPath = Path.Combine(AppContext.BaseDirectory, "create-user.avsc");

        // Flag to signal when to stop sending requests
        private static volatile bool _keepRunning = true;

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with Avro request/reply.
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username&gt;@&lt;vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: AvroRequestor <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: AvroRequestor <host> <username>@<vpnname> <password>");
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
                // The requestor serializes the request and deserializes the reply, so it needs both.
                using (var serializer = new AvroSerializer<GenericRecord>())
                using (var deserializer = new AvroDeserializer<GenericRecord>())
                {
                    // Configure the Schema Registry connection for both
                    var config = GetSchemaRegistryConfig();
                    serializer.Configure(config);
                    deserializer.Configure(config);

                    // Wrap the async serializer/deserializer with synchronous adapters. SendRequest is a
                    // blocking synchronous call, but AvroSerializer/Deserializer are async by default.
                    var syncSerializer = serializer.AsSyncOverAsync();
                    var syncDeserializer = deserializer.AsSyncOverAsync();

                    // Create session properties for the Solace message broker connection
                    SessionProperties sessionProps = new SessionProperties()
                    {
                        Host = host,
                        VPNName = vpnName,
                        UserName = userName,
                        Password = password,
                    };

                    Console.WriteLine("Connecting as {0}@{1} on {2}...", userName, vpnName, host);

                    using (ISession session = context.CreateSession(sessionProps, null, null))
                    {
                        ReturnCode returnCode = session.Connect();
                        if (returnCode != ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Error connecting, return code: {0}", returnCode);
                            return 1;
                        }

                        Console.WriteLine("Session successfully connected.");

                        // Subscribe to the reply topic so the broker routes replies back to this session.
                        ITopic replyTopic = ContextFactory.Instance.CreateTopic(ReplyTopicName);
                        session.Subscribe(replyTopic, true);

                        RunRequestorLoop(session, syncSerializer, syncDeserializer, replyTopic);
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
        /// Runs the request/reply loop: build a CreateUser request, send it and block for the reply,
        /// deserialize the CreateUserResponse and print it, until the user presses Enter.
        /// </summary>
        /// <param name="session">The active Solace session</param>
        /// <param name="serializer">The Avro serializer for the request</param>
        /// <param name="deserializer">The Avro deserializer for the reply</param>
        /// <param name="replyToTopic">The topic replies are delivered on</param>
        static void RunRequestorLoop(ISession session,
                                     ISerializer<GenericRecord> serializer,
                                     IDeserializer<GenericRecord> deserializer,
                                     ITopic replyToTopic)
        {
            ITopic requestTopic = ContextFactory.Instance.CreateTopic(RequestTopicName);

            // Build the CreateUser request record from the request schema. The same record is reused for
            // every request; only its data would change if the sample varied it.
            var requestSchema = (RecordSchema)Schema.Parse(File.ReadAllText(RequestSchemaPath));
            var userRequest = new GenericRecord(requestSchema);
            userRequest.Add("name", "John Doe");
            userRequest.Add("email", "support@solace.com");

            // Start a thread to listen for the Enter key press
            Console.WriteLine("Press Enter to exit.");
            Console.WriteLine();
            var exitThread = new Thread(() =>
            {
                Console.ReadLine();
                _keepRunning = false;
            });
            exitThread.Start();

            while (_keepRunning)
            {
                try
                {
                    using (IMessage requestMsg = ContextFactory.Instance.CreateMessage())
                    {
                        requestMsg.Destination = requestTopic;
                        requestMsg.DeliveryMode = MessageDeliveryMode.Direct;

                        // Serialize the request and set the reply-to so the replier knows where to answer.
                        requestMsg.Serialize(serializer, userRequest);
                        requestMsg.ReplyTo = replyToTopic;

                        Console.WriteLine("Sending Request: Name={0}, Email={1}",
                            userRequest["name"], userRequest["email"]);

                        // Blocking request/reply: send and wait up to RequestTimeoutMs for the matching reply.
                        IMessage replyMsg;
                        ReturnCode returnCode = session.SendRequest(requestMsg, out replyMsg, RequestTimeoutMs);

                        if (returnCode == ReturnCode.SOLCLIENT_OK && replyMsg != null)
                        {
                            using (replyMsg)
                            {
                                // Deserialize the reply using the schema resolved from the registry.
                                GenericRecord userResponse = replyMsg.Deserialize(deserializer);
                                Console.WriteLine("Received Reply: Id={0}", userResponse["id"]);
                            }
                        }
                        else if (returnCode == ReturnCode.SOLCLIENT_INCOMPLETE)
                        {
                            // No reply within the timeout window (e.g. no replier running).
                            Console.WriteLine("Request timed out after {0} ms", RequestTimeoutMs);
                        }
                        else
                        {
                            Console.WriteLine("Request failed with return code: {0}", returnCode);
                        }
                    }
                }
                catch (SerializationException ex)
                {
                    // A serialization/deserialization failure on one request must not stop the loop.
                    Console.WriteLine("Serialization exception: {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    // Any other failure (e.g. the session went down) is logged so the loop keeps running and
                    // the exit thread can still be joined when the user presses Enter.
                    Console.WriteLine("Error during request-reply: {0}", ex.Message);
                }

                // Limit the send rate to make the sample output easy to observe. This also paces retries
                // when a request fails.
                Thread.Sleep(1000);
            }

            exitThread.Join();
            Console.WriteLine("Stopped sending requests.");
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
