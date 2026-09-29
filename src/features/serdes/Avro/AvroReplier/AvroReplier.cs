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
using Avro;
using Avro.Generic;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;

/// <summary>
/// Solace Messaging API tutorial: AvroReplier
/// </summary>

namespace Tutorial
{
    /// <summary>
    /// AvroReplier
    /// This class demonstrates the reply side of request/reply messaging over the Solace Messaging API for .NET with Avro
    /// serialization. It subscribes to the request topic, deserializes each Avro CreateUser request,
    /// constructs an Avro CreateUserResponse, and sends it back to the request's ReplyTo destination. It
    /// runs continuously until the user presses Enter. Pair it with AvroRequestor.
    /// </summary>
    static class AvroReplier
    {
        public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
        public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
        public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";

        public const string RequestTopicName = "solace/samples/create-user/avro";

        // Path to the CreateUserResponse reply schema, linked from the Resources project and copied next
        // to the binary at build time. The replier builds the reply record from it; the request schema is
        // resolved from the registry, so no request schema file is needed.
        private static readonly string ReplySchemaPath = Path.Combine(AppContext.BaseDirectory, "create-user-response.avsc");

        // Parsed once and reused for every reply.
        private static RecordSchema _replySchema;

        /// <summary>
        /// The main method that demonstrates the Solace Messaging API for .NET usage with Avro request/reply (reply side).
        /// </summary>
        /// <param name="args">Command line arguments: &lt;host&gt; &lt;username&gt;@&lt;vpnname&gt; &lt;password&gt;</param>
        /// <returns>0 on success, 1 on failure</returns>
        static int Main(string[] args)
        {
            // Check if the required command line arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: AvroReplier <host> <username>@<vpnname> <password>");
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
                Console.WriteLine("Usage: AvroReplier <host> <username>@<vpnname> <password>");
                return 1;
            }

            string host = args[0];
            string userName = split[0];
            string vpnName = split[1];
            string password = args[2];

            // Parse the reply schema once, up front.
            _replySchema = (RecordSchema)Schema.Parse(File.ReadAllText(ReplySchemaPath));

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
                // The replier deserializes the request and serializes the reply, so it needs both.
                using (var deserializer = new AvroDeserializer<GenericRecord>())
                using (var serializer = new AvroSerializer<GenericRecord>())
                {
                    // Configure the Schema Registry connection for both
                    var config = GetSchemaRegistryConfig();
                    deserializer.Configure(config);
                    serializer.Configure(config);

                    // Wrap the async serializer/deserializer with synchronous adapters: the Solace message
                    // callback and SendReply are synchronous, but AvroSerializer/Deserializer are async.
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

                    Console.WriteLine("Connecting as {0}@{1} on {2}...", userName, vpnName, host);

                    // Register the message handler: each incoming request is delivered to HandleRequest on
                    // the API's dispatch thread, which deserializes it and sends back a reply.
                    using (ISession session = context.CreateSession(sessionProps,
                        (source, msgArgs) => HandleRequest(source, msgArgs, syncDeserializer, syncSerializer), null))
                    {
                        ReturnCode returnCode = session.Connect();
                        if (returnCode != ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Error connecting, return code: {0}", returnCode);
                            return 1;
                        }

                        Console.WriteLine("Session successfully connected.");

                        // Subscribe to the request topic the requestor publishes to.
                        ITopic requestTopic = ContextFactory.Instance.CreateTopic(RequestTopicName);
                        session.Subscribe(requestTopic, true);
                        Console.WriteLine("Subscribed to request topic: {0}", RequestTopicName);

                        // Block the main thread until Enter; requests are handled on the dispatch thread.
                        Console.WriteLine("Waiting for requests. Press Enter to exit.");
                        Console.WriteLine();
                        Console.ReadLine();

                        Console.WriteLine("Exit requested — unsubscribing and shutting down.");
                        session.Unsubscribe(requestTopic, true);
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
        /// Invoked by the API on its dispatch thread for each request. Deserializes the CreateUser request,
        /// builds a CreateUserResponse with a generated id, and sends it back to the request's ReplyTo. A
        /// serialization failure on one request is logged and swallowed so the replier keeps serving.
        /// </summary>
        /// <param name="source">The source object that raised the event (the session)</param>
        /// <param name="args">The message event arguments containing the received request</param>
        /// <param name="deserializer">The Avro deserializer for the request</param>
        /// <param name="serializer">The Avro serializer for the reply</param>
        private static void HandleRequest(object source, MessageEventArgs args,
                                          IDeserializer<GenericRecord> deserializer,
                                          ISerializer<GenericRecord> serializer)
        {
            try
            {
                // Deserialize the CreateUser request (schema resolved from the registry).
                GenericRecord request = args.Message.Deserialize(deserializer);
                Console.WriteLine("Received Request: Name={0}, Email={1}", request["name"], request["email"]);

                // The ReplyTo comes from the incoming request — never hardcode it.
                if (args.Message.ReplyTo == null)
                {
                    Console.WriteLine("Request has no ReplyTo destination — cannot reply.");
                    return;
                }

                // "Create" the user: generate an id (8-char GUID, matching the reference samples).
                string userId = Guid.NewGuid().ToString().Substring(0, 8);
                var response = new GenericRecord(_replySchema);
                response.Add("id", userId);
                Console.WriteLine("Created user with Id={0}", userId);

                using (IMessage replyMsg = ContextFactory.Instance.CreateMessage())
                {
                    // Serialize the reply targeting the request's ReplyTo destination.
                    replyMsg.Serialize(serializer, response, args.Message.ReplyTo);

                    var session = (ISession)source;
                    ReturnCode returnCode = session.SendReply(args.Message, replyMsg);
                    if (returnCode != ReturnCode.SOLCLIENT_OK)
                    {
                        Console.WriteLine("Failed to send reply, return code: {0}", returnCode);
                    }
                    else
                    {
                        Console.WriteLine("Sent reply: Id={0}", userId);
                    }
                }
            }
            catch (SerializationException ex)
            {
                // A serialization/deserialization failure on one request must not stop the replier.
                Console.WriteLine("Serialization exception: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                // Any other failure is logged here instead of escaping into the API's dispatch thread.
                Console.WriteLine("Error in message processing: {0}", ex.Message);
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
