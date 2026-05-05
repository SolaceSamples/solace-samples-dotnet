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

using Resources.JsonSchema;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating JSON Schema validation features with JSON Schema serializers
    /// and deserializers. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>SerializeWithValidationEnabled - Serialization with validation enabled (default behavior)</item>
    ///   <item>SerializeWithValidationDisabled - Serialization with validation disabled</item>
    ///   <item>SerializeWithValidationCallbackSuppressException - Serialization validation callback that suppresses errors</item>
    ///   <item>SerializeWithValidationCallbackCustomError - Serialization validation callback with custom error</item>
    ///   <item>DeserializeWithValidationEnabled - Deserialization with validation enabled (default behavior)</item>
    ///   <item>DeserializeWithValidationDisabled - Deserialization with validation disabled</item>
    ///   <item>DeserializeWithValidationCallbackSuppressException - Deserialization validation callback that suppresses errors</item>
    ///   <item>DeserializeWithValidationCallbackCustomError - Deserialization validation callback with custom error</item>
    /// </list>
    /// </summary>
    public static class HowToValidateWithJsonSchema
    {
        /// <summary>
        /// Demonstrates serialization with validation enabled (default behavior).
        /// When validation is enabled, the object is first serialized to JSON, then validated against
        /// the resolved JSON schema before being returned.
        /// If validation fails, a <see cref="JsonSchemaValidationException"/> is thrown.
        /// </summary>
        public static async Task SerializeWithValidationEnabled()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Validation is enabled by default (ValidateSchema = true)
            // You can explicitly set it if needed:
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<User>())
            {
                serializer.Configure(config);
    
                // Create valid User object
                var validUser = new User
                {
                    Name = "John Doe",
                    Id = "12345",
                    Email = "john.doe@example.com"
                };
    
                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();
    
                // At this point, the JSON Schema serializer is configured with validation enabled.
                try
                {
                    byte[] userBytes = await serializer.SerializeAsync("solace/samples/user", validUser, headers);
                    // Serialization succeeds because the data is valid
                }
                catch (JsonSchemaValidationException)
                {
                    // ex.ValidationErrors contains a JsonArray with detailed error information
                    // Handle validation failure (e.g., log error, notify user)
                    throw;
                }
            }
        }
    
        /// <summary>
        /// Demonstrates serialization with validation disabled via configuration.
        /// When validation is disabled, data is serialized without schema validation,
        /// improving performance at the cost of not detecting schema violations.
        /// </summary>
        public static async Task SerializeWithValidationDisabled()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Disable schema validation for performance
            config[JsonSchemaPropertyKeys.ValidateSchema] = false;
    
            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<User>())
            {
                serializer.Configure(config);
    
                // Create User object (could be invalid, validation is disabled)
                var user = new User
                {
                    Name = "Jane Doe",
                    Id = "67890",
                    Email = "jane.doe@example.com"
                };
    
                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();
    
                // At this point, the JSON Schema serializer is configured with validation disabled.
                // Serialization proceeds without validation
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/user", user, headers);
            }
        }
    
        /// <summary>
        /// Demonstrates deserialization with validation enabled (default behavior).
        /// When validation is enabled, the raw JSON payload is validated against the resolved JSON schema
        /// before being deserialized into the target type.
        /// If validation fails, a <see cref="JsonSchemaValidationException"/> is thrown.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized JSON payload.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeWithValidationEnabled(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Validation is enabled by default (ValidateSchema = true)
            // You can explicitly set it if needed:
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);
    
                // At this point, the JSON Schema deserializer is configured with validation enabled.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                try
                {
                    User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);
                    // Deserialization succeeds because the data is valid
                }
                catch (JsonSchemaValidationException)
                {
                    // ex.ValidationErrors contains a JsonArray with detailed error information
                    // Handle validation failure (e.g., log error, skip message, send to dead letter queue)
                    throw;
                }
            }
        }
    
        /// <summary>
        /// Demonstrates deserialization with validation disabled via configuration.
        /// When validation is disabled, data is deserialized without schema validation,
        /// improving performance at the cost of not detecting schema violations.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized JSON payload.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeWithValidationDisabled(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Disable schema validation for performance
            config[JsonSchemaPropertyKeys.ValidateSchema] = false;
    
            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);
    
                // At this point, the JSON Schema deserializer is configured with validation disabled.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                // Deserialization proceeds without validation
                User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);
            }
        }
    
        /// <summary>
        /// Demonstrates serialization with a validation callback that handles validation errors gracefully.
        /// The callback is invoked when validation errors occur but returns null to suppress the exception
        /// and continue serialization.
        /// </summary>
        public static async Task SerializeWithValidationCallbackSuppressException()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Enable validation (default)
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Configure a validation error handler that logs but allows serialization to continue
            config[JsonSchemaPropertyKeys.ValidationErrorHandler] = new Func<JsonValidationErrorArgs, JsonSchemaValidationException>(args =>
            {
                // Access validation error details
                // args.Errors contains the validation errors as a JsonArray
                // args.JsonObject contains the raw JSON bytes that failed validation (ReadOnlyMemory<byte>)
                // args.Schema contains the schema as raw bytes (ReadOnlyMemory<byte>)
                // args.Metadata contains serialization metadata (e.g., destination name)
    
                // Log or handle the validation errors as needed
                // For example, you could log: args.Errors.ToJsonString()
    
                // Return null to suppress the exception and allow serialization to continue
                return null;
            });
    
            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<User>())
            {
                serializer.Configure(config);
    
                // Create User object for demonstration
                var user = new User
                {
                    Name = "Bob Smith",
                    Id = "11111",
                    Email = "bob.smith@example.com"
                };
    
                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();
    
                // At this point, the serializer is configured with a validation error handler.
                // Serialization succeeds with valid data
                // If validation fails, the handler can suppress the exception by returning null
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/user", user, headers);
            }
        }
    
        /// <summary>
        /// Demonstrates serialization with a validation callback that returns a custom error.
        /// The callback is invoked when validation errors occur and returns a custom
        /// <see cref="JsonSchemaValidationException"/> that will be thrown.
        /// </summary>
        public static async Task SerializeWithValidationCallbackCustomError()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Enable validation (default)
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Configure a validation error handler that returns a custom exception
            config[JsonSchemaPropertyKeys.ValidationErrorHandler] = new Func<JsonValidationErrorArgs, JsonSchemaValidationException>(args =>
            {
                // Note: Exceptions thrown from this callback are wrapped in SerializationException.
                // See ValidationErrorHandler property documentation for details.
    
                // Access validation error details
                var errorDetails = args.Errors.ToJsonString();
                var destinationName = args.Metadata.DestinationName;
    
                // Create and return a custom exception with enhanced error message
                var customMessage = $"Validation failed for destination '{destinationName}': {errorDetails}";
                return new JsonSchemaValidationException(customMessage, args.Errors);
            });
    
            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<User>())
            {
                serializer.Configure(config);
    
                // Create User object for demonstration
                var user = new User
                {
                    Name = "Alice Johnson",
                    Id = "22222",
                    Email = "alice.johnson@example.com"
                };
    
                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();
    
                // At this point, the serializer is configured with a validation error handler.
                // Serialization succeeds with valid data
                // If validation fails, the handler returns a custom exception that will be thrown
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/user", user, headers);
            }
        }
    
        /// <summary>
        /// Demonstrates deserialization with a validation callback that handles validation errors gracefully.
        /// The callback is invoked when validation errors occur but returns null to suppress the exception
        /// and continue deserialization.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized JSON payload.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeWithValidationCallbackSuppressException(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Enable validation (default)
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Configure a validation error handler that logs but allows deserialization to continue
            config[JsonSchemaPropertyKeys.ValidationErrorHandler] = new Func<JsonValidationErrorArgs, JsonSchemaValidationException>(args =>
            {
                // Access validation error details
                // args.Errors contains the validation errors as a JsonArray
                // args.JsonObject contains the raw JSON bytes of the payload that failed validation (ReadOnlyMemory<byte>)
                // args.Schema contains the schema as raw bytes (ReadOnlyMemory<byte>)
                // args.Metadata contains deserialization metadata
    
                // Log or handle the validation errors as needed
                // For example, you could log: args.Errors.ToJsonString()
    
                // Return null to suppress the exception and allow deserialization to continue
                return null;
            });
    
            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);
    
                // At this point, the JSON Schema deserializer is configured with validation enabled
                // and a validation error handler.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                // If validation fails, the handler can suppress the exception by returning null
                User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);
            }
        }
    
        /// <summary>
        /// Demonstrates deserialization with a validation callback that returns a custom error.
        /// The callback is invoked when validation errors occur and returns a custom
        /// <see cref="JsonSchemaValidationException"/> that will be thrown.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized JSON payload.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeWithValidationCallbackCustomError(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();
    
            // Set required Schema Registry connection properties
    
            // Enable validation (default)
            config[JsonSchemaPropertyKeys.ValidateSchema] = true;
    
            // Configure a validation error handler that returns a custom exception
            config[JsonSchemaPropertyKeys.ValidationErrorHandler] = new Func<JsonValidationErrorArgs, JsonSchemaValidationException>(args =>
            {
                // Note: Exceptions thrown from this callback are wrapped in SerializationException.
                // See ValidationErrorHandler property documentation for details.
    
                // Access validation error details
                var errorDetails = args.Errors.ToJsonString();
                var payloadString = Encoding.UTF8.GetString(args.JsonObject.ToArray());
    
                // Create and return a custom exception with enhanced error message
                var customMessage = $"Deserialization validation failed. Payload: {payloadString}, Errors: {errorDetails}";
                return new JsonSchemaValidationException(customMessage, args.Errors);
            });
    
            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);
    
                // At this point, the JSON Schema deserializer is configured with validation enabled
                // and a validation error handler.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                // If validation fails, the handler returns a custom exception that will be thrown
                User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);
            }
        }
    }
}
