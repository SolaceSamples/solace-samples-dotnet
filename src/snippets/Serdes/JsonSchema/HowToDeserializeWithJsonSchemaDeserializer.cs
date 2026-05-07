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

using System.Collections.Generic;
using System.Threading.Tasks;
using Resources.JsonSchema;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System.Text.Json.Nodes;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating basic deserialization operations with JSON Schema deserializers
    /// and Schema Registry integration. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>DeserializeToUser - Deserialize to a User POCO</item>
    ///   <item>DeserializeToUserWithTypeProperty - Deserialize to a User POCO using the type property in the schema</item>
    ///   <item>DeserializeToJsonNode - Deserialize to a JsonNode object</item>
    /// </list>
    /// </summary>
    public static class HowToDeserializeWithJsonSchemaDeserializer
    {
        /// <summary>
        /// Demonstrates how to deserialize to a User POCO (Plain Old CLR Object).
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized User JSON object using JsonSchemaSerializer.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeToUser(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured and ready to use for deserialization.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);

                // At this point, the user object can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize to a User POCO using the type property within the JSON schema.
        /// The deserializer uses this property to determine the target .NET type.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized User JSON object using JsonSchemaSerializer.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeToUserWithTypeProperty(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // The type property is used to determine the .NET type from the schema.
            // This can be changed by setting the TypeProperty config.
            // The default value is "dotnetType".
            config[JsonSchemaPropertyKeys.TypeProperty] = "customDotnetType";

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<User>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured and ready to use for deserialization.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                // The schema must include a "customDotnetType" property with the fully qualified .NET type name.
                User user = await deserializer.DeserializeAsync(topic, payloadBytes, headers);

                // At this point, the user object can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize to a <see cref="JsonNode"/> for dynamic JSON structures.
        /// When no specific type is needed, the deserializer can return a generic JsonNode
        /// that can be navigated dynamically.
        /// </summary>
        /// <param name="topic">Destination string from the messaging system.</param>
        /// <param name="payloadBytes">Serialized JSON object using JsonSchemaSerializer.</param>
        /// <param name="headers">Header map from the messaging system.</param>
        public static async Task DeserializeToJsonNode(string topic, byte[] payloadBytes, Dictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured and ready to use for deserialization.
                // Using JsonNode as the generic type allows deserializing any JSON payload without
                // needing to know the concrete type at compile time.
                // Note the headers dictionary must include the following:
                // - A schema identifier for schema resolution.
                JsonNode jsonNode = await deserializer.DeserializeAsync(topic, payloadBytes, headers);

                // At this point, the jsonNode object can be used in processing.
            }
        }
    }
}
