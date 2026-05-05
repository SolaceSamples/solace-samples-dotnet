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
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System.Text.Json.Nodes;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure JSON Schema deserializers
    /// with different cache properties. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>Configuring the cache TTL (time-to-live) property</item>
    ///   <item>Configuring the use-cached-on-error property</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureJsonSchemaDeserializerCacheProperties
    {
        /// <summary>
        /// Demonstrates how to configure the cache TTL (time-to-live) property in milliseconds for a JSON Schema deserializer.
        /// The cache TTL determines how long schema artifacts remain valid in the cache before
        /// they need to be fetched again from the registry on the next relevant lookup.
        /// <para>The default value is 30000 ms (30 seconds).</para>
        /// </summary>
        public static void DeserializeWithCacheTtl()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Example 1: Set cache TTL using an integer value (milliseconds)
            config[SchemaResolverPropertyKeys.CacheTtlMs] = 5000;

            // Example 2: Set cache TTL using a TimeSpan
            config[SchemaResolverPropertyKeys.CacheTtlMs] = TimeSpan.FromSeconds(5);

            // Example 3: Set cache TTL using a string value
            config[SchemaResolverPropertyKeys.CacheTtlMs] = "5000";

            // Example 4: Disable caching completely
            config[SchemaResolverPropertyKeys.CacheTtlMs] = 0;

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured with the specified cache TTL.
                // A longer TTL improves performance by reducing registry calls but may use outdated schemas.
                // A shorter TTL ensures more up-to-date schemas but increases load on the registry.
                // A TTL of zero disables caching, so schemas will be fetched from the registry on every request.
            }
        }

        /// <summary>
        /// Demonstrates how to configure the use-cached-on-error property for a JSON Schema deserializer.
        /// This controls whether to use cached schemas when schema registry lookup errors occur.
        /// <para>The default value is false.</para>
        /// </summary>
        public static void DeserializeWithUseCachedOnError()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Example 1: Use cached schemas when registry lookups fail (resilient mode)
            config[SchemaResolverPropertyKeys.UseCachedOnError] = true;

            // Example 2: Throw exceptions when registry lookups fail (strict mode, default value)
            config[SchemaResolverPropertyKeys.UseCachedOnError] = false;

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured with the specified use-cached-on-error property.
                // When enabled, schema resolution will use cached schemas instead of throwing exceptions
                // after retry attempts are exhausted, improving resilience during registry outages.
                // When disabled, exceptions will be thrown when registry lookup errors occur.
            }
        }
    }
}
