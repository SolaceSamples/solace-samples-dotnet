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
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Serialization;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;
using Resources.JsonSchema;  // CreateUser, CreateUserResponse

/// <summary>
/// JsonSchemaSerdesReplier
/// This class demonstrates how to use Solace Messaging API for .NET with JsonSchema serialization and deserialization for Request-Reply messaging.
/// It connects to a Solace message broker, receives request messages, deserializes them using JsonSchemaDeserializer,
/// creates a response, serializes it using JsonSchemaSerializer, and sends it back to the requestor.
/// </summary>
class JsonSchemaSerdesReplier
{
    // Schema Registry configuration
    public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
    public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
    public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";

    // Topic for receiving requests
    const string REQUEST_TOPIC = "solace/samples/create-user/json";

    /// <summary>
    /// The main method that demonstrates the Solace Messaging API for .NET usage with JsonSchema deserialization for Request-Reply.
    /// </summary>
    /// <param name="args">Command line arguments: &lt;host:port&gt; &lt;username@vpnname&gt; &lt;password&gt;</param>
    /// <returns>0 on success, 1 on failure</returns>
    static int Main(string[] args)
    {
        // Check if the required command line arguments are provided
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: JsonSchemaSerdesReplier <host:port> <username>@<vpnname> <password>");
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
            Console.WriteLine("Usage: JsonSchemaSerdesReplier <host:port> <username>@<vpnname> <password>");
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
            // Create and configure JSON Schema serializer for replies and deserializer for requests
            using (var deserializer = new JsonSchemaDeserializer<CreateUser>())
            using (var serializer = new JsonSchemaSerializer<CreateUserResponse>())
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
                // Create a Solace session and set up the message event handler
                using (ISession session = context.CreateSession(sessionProps,
                    (source, msgArgs) => HandleMessage(source, msgArgs, syncDeserializer, syncSerializer),
                    null))
                {
                    // Connect to the session
                    ReturnCode returnCode = session.Connect();
                    if (returnCode == ReturnCode.SOLCLIENT_OK)
                    {
                        // Subscribe to the request topic
                        ITopic requestTopic = ContextFactory.Instance.CreateTopic(REQUEST_TOPIC);
                        session.Subscribe(requestTopic, true);

                        Console.WriteLine("JsonSchemaSerdesReplier started. Waiting for requests ... Press enter to exit");
                        Console.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine($"Error: Failed to connect, session return code {returnCode}");
                        return 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
        finally
        {
            ContextFactory.Instance.Cleanup();
        }
        return 0;
    }

    /// <summary>
    /// This event handler is invoked by Solace Messaging API when a message arrives.
    /// It deserializes the request, creates a response with a generated user ID, and sends it back.
    /// </summary>
    /// <param name="source">The source object that raised the event (ISession)</param>
    /// <param name="args">The message event arguments containing the received message</param>
    /// <param name="deserializer">The JSON Schema deserializer to use for request deserialization</param>
    /// <param name="serializer">The JSON Schema serializer to use for response serialization</param>
    private static void HandleMessage(object source, MessageEventArgs args,
                                     IDeserializer<CreateUser> deserializer,
                                     ISerializer<CreateUserResponse> serializer)
    {
        try
        {
            Console.WriteLine("Received request message, processing...");

            // Deserialize the request payload to a CreateUser object using JSON Schema validation
            CreateUser createUserRequest = args.Message.Deserialize(deserializer);

            Console.WriteLine("Deserialized request: {0}", createUserRequest);

            // Extract user information from the request
            string name = createUserRequest.Name;
            string email = createUserRequest.Email;

            Console.WriteLine("Processing user creation request for: {0} ({1})", name, email);

            // Generate a unique user ID (8-character GUID, matching Java implementation)
            string userId = Guid.NewGuid().ToString().Substring(0, 8);

            // Create a response with the generated ID
            CreateUserResponse createUserResponse = new CreateUserResponse { Id = userId };

            Console.WriteLine("Created user with ID: {0}", userId);

            // Check if the request message contains a replyTo destination
            if (args.Message.ReplyTo == null)
            {
                Console.WriteLine("Error: Request message does not contain a ReplyTo destination");
                return;
            }

            // Create a reply message
            using (IMessage replyMessage = ContextFactory.Instance.CreateMessage())
            {
                // Serialize the response with JSON Schema validation
                replyMessage.Serialize(serializer, createUserResponse, args.Message.ReplyTo);

                // Send the reply
                var session = (ISession)source;
                ReturnCode returnCode = session.SendReply(args.Message, replyMessage);
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    throw new Exception($"Send failed with return code: {returnCode}");
                }
                Console.WriteLine("Sent reply message");
            }
        }
        catch (SerializationException ex)
        {
            // Handle serialization and deserialization errors (e.g., validation error, schema mismatch)
            Console.WriteLine("Serialization exception: {0}", ex.Message);
            Console.WriteLine(ex);
        }
        catch (Exception ex)
        {
            // Handle any other errors during message processing
            Console.WriteLine("Error in message processing: {0}", ex.Message);
            Console.WriteLine(ex);
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
