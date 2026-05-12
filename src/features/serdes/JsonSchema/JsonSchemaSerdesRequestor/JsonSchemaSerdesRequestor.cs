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
/// JsonSchemaSerdesRequestor
/// This class demonstrates how to use Solace Messaging API for .NET with JsonSchema serialization and deserialization for Request-Reply messaging.
/// It connects to a Solace message broker, creates and serializes request messages using JsonSchemaSerializer,
/// sends them to a replier, and deserializes the responses using JsonSchemaDeserializer.
/// </summary>
class JsonSchemaSerdesRequestor
{
    // Schema Registry configuration
    public static readonly string RegistryUrl = Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8081/apis/registry/v3";
    public static readonly string RegistryUsername = Environment.GetEnvironmentVariable("REGISTRY_USERNAME") ?? "sr-readonly";
    public static readonly string RegistryPassword = Environment.GetEnvironmentVariable("REGISTRY_PASSWORD") ?? "roPassword";

    // Topics
    const string REQUEST_TOPIC = "solace/samples/create-user/json";
    const string REPLY_TOPIC = "solace/samples/create-user-response/json";

    // Request timeout in milliseconds
    const int REQUEST_TIMEOUT_MS = 5000;

    // Flag to control the main loop
    private static volatile bool _keepRunning = true;

    /// <summary>
    /// The main method that demonstrates the Solace Messaging API for .NET usage with JsonSchema serialization for Request-Reply.
    /// </summary>
    /// <param name="args">Command line arguments: &lt;host:port&gt; &lt;username@vpnname&gt; &lt;password&gt;</param>
    /// <returns>0 on success, 1 on failure</returns>
    static int Main(string[] args)
    {
        // Check if the required command line arguments are provided
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: JsonSchemaSerdesRequestor <host:port> <username>@<vpnname> <password>");
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
            Console.WriteLine("Usage: JsonSchemaSerdesRequestor <host:port> <username>@<vpnname> <password>");
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
            // Create and configure JSON Schema serializer for requests and deserializer for replies
            using (var serializer = new JsonSchemaSerializer<CreateUser>())
            using (var deserializer = new JsonSchemaDeserializer<CreateUserResponse>())
            {
                // Configure the Schema Registry connection for both serializer and deserializer
                var config = GetSchemaRegistryConfig();
                serializer.Configure(config);
                deserializer.Configure(config);

                // Wrap async serializer/deserializer with synchronous adapters for use with Solace's synchronous SendRequest API.
                // SendRequest() is a blocking synchronous call, but JsonSchemaSerializer/Deserializer are async by default.
                // AsSyncOverAsync() creates a synchronous wrapper that blocks on async operations, making them compatible with the Solace Messaging API for .NET.
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

                // Connect to the Solace messaging router
                using (ISession session = context.CreateSession(sessionProps, null, null))
                {
                    // Connect to the session
                    ReturnCode returnCode = session.Connect();
                    if (returnCode == ReturnCode.SOLCLIENT_OK)
                    {
                        // Subscribe to the reply topic
                        ITopic replyTopic = ContextFactory.Instance.CreateTopic(REPLY_TOPIC);
                        session.Subscribe(replyTopic, true);

                        // Run the request-reply loop
                        RunRequestorLoop(session, syncSerializer, syncDeserializer, replyTopic);
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
    /// Runs the main request-reply loop, sending requests and receiving replies until the user presses Enter.
    /// </summary>
    /// <param name="session">The Solace session to use for sending and receiving messages</param>
    /// <param name="serializer">The JSON Schema serializer to use for request serialization</param>
    /// <param name="deserializer">The JSON Schema deserializer to use for reply deserialization</param>
    /// <param name="replyToTopic">The topic that the requestor expects to receive replies on</param>
    private static void RunRequestorLoop(ISession session,
                                        ISerializer<CreateUser> serializer,
                                        IDeserializer<CreateUserResponse> deserializer,
                                        ITopic replyToTopic)
    {
        // Create request topic
        ITopic requestTopic = ContextFactory.Instance.CreateTopic(REQUEST_TOPIC);

        // Create user request object
        var userRequest = new CreateUser
        {
            Name = "John Doe",
            Email = "support@solace.com"
        };

        // Set up exit handler on a separate thread
        Console.WriteLine("Press Enter to exit.");
        var exitThread = new Thread(() =>
        {
            Console.ReadLine();
            _keepRunning = false;
        });
        exitThread.Start();

        // Request-reply loop
        while (_keepRunning)
        {
            try
            {
                // Create request message
                using (IMessage requestMsg = ContextFactory.Instance.CreateMessage())
                {
                    requestMsg.Destination = requestTopic;
                    requestMsg.DeliveryMode = MessageDeliveryMode.Direct;

                    // Serialize request with JSON Schema validation
                    requestMsg.Serialize(serializer, userRequest);

                    // Set reply-to topic
                    requestMsg.ReplyTo = replyToTopic;

                    Console.WriteLine($"Sending Request: {userRequest}");

                    if (requestMsg.BinaryAttachment != null && requestMsg.BinaryAttachment.Length > 0)
                    {
                        Console.WriteLine($"Binary Payload (UTF-8): {System.Text.Encoding.UTF8.GetString(requestMsg.BinaryAttachment)}");
                    }

                    // Send request and wait for reply (blocking call)
                    IMessage replyMsg;
                    ReturnCode returnCode = session.SendRequest(requestMsg, out replyMsg, REQUEST_TIMEOUT_MS);

                    if (returnCode == ReturnCode.SOLCLIENT_OK && replyMsg != null)
                    {
                        using (replyMsg)
                        {
                            // Deserialize reply with JSON Schema validation
                            var userResponse = replyMsg.Deserialize(deserializer);

                            Console.WriteLine($"Received Reply: {userResponse}");
                            Console.WriteLine($"User created with ID: {userResponse.Id}");
                        }
                    }
                    else if (returnCode == ReturnCode.SOLCLIENT_INCOMPLETE)
                    {
                        Console.WriteLine($"Request timed out after {REQUEST_TIMEOUT_MS} ms");
                    }
                    else
                    {
                        Console.WriteLine($"Request failed with return code: {returnCode}");
                    }
                }

                // Limit send rate to facilitate user's observation of sample output.
                Thread.Sleep(1000);
            }
            catch (SerializationException ex)
            {
                // Handle serialization and deserialization errors (e.g., validation error, schema mismatch)
                Console.WriteLine("Serialization exception: {0}", ex.Message);
                Console.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during request-reply: {ex.Message}");
            }
        }

        exitThread.Join();
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
            { JsonSchemaPropertyKeys.RegistryUrl, RegistryUrl },
            { JsonSchemaPropertyKeys.AuthUsername, RegistryUsername },
            { JsonSchemaPropertyKeys.AuthPassword, RegistryPassword },
        };
    }
}
