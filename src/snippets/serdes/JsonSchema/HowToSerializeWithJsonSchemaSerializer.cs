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
    /// Provides code snippets demonstrating basic serialization operations with JSON Schema serializers
    /// and Schema Registry integration. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>SerializeWithUserJsonSchema - Basic serialization with User POCO</item>
    ///   <item>SerializeWithJsonNode - Serialization with JsonNode object</item>
    /// </list>
    /// </summary>
    public static class HowToSerializeWithJsonSchemaSerializer
    {
        /// <summary>
        /// Demonstrates basic serialization with a User POCO (Plain Old CLR Object).
        /// </summary>
        public static async Task SerializeWithUserJsonSchema()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Create User object
            var user = new User
            {
                Name = "John Doe",
                Id = "-1",
                Email = "support@solace.com"
            };

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<User>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/json", user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice
                // userBytes are the user object serialized as bytes
                // headers were modified to hold the schema registry header fields to include:
                // - SchemaId (type long) for schema identification
            }
        }

        /// <summary>
        /// Demonstrates serialization with a JsonNode object for dynamic JSON structures.
        /// </summary>
        public static async Task SerializeWithJsonNode()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Create JsonNode object for dynamic JSON structure
            var user = new JsonObject
            {
                ["name"] = "John Doe",
                ["id"] = "-1",
                ["email"] = "support@solace.com"
            };

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/json", user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice
                // userBytes are the user object serialized as bytes
                // headers were modified to hold the schema registry header fields to include:
                // - SchemaId (type long) for schema identification
            }
        }
    }
}
