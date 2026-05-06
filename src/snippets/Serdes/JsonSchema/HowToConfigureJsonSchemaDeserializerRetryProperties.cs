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
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System.Text.Json.Nodes;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure schema registry retry
    /// properties for a JSON Schema deserializer. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>Configuring the number of request attempts</item>
    ///   <item>Configuring the backoff time between retry attempts</item>
    ///   <item>Configuring a complete retry strategy</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureJsonSchemaDeserializerRetryProperties
    {
        /// <summary>
        /// Demonstrates how to configure the number of attempts to make when communicating
        /// with the schema registry before giving up. When used with
        /// <see cref="JsonSchemaPropertyKeys.UseCachedOnError"/>,
        /// this property specifies the number of attempts before falling back to the last cached value.
        /// <para>Valid values are positive long values (1 - <see cref="long.MaxValue"/>).</para>
        /// <para>The default value is 3.</para>
        /// </summary>
        public static void DeserializeWithRequestAttempts()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Example 1: Set number of request attempts using a long value
            config[JsonSchemaPropertyKeys.RequestAttempts] = 5L;

            // Example 2: Set number of request attempts using a string value
            // config[JsonSchemaPropertyKeys.RequestAttempts] = "5";

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured with the specified number of request attempts.
                // A higher value increases resilience during temporary registry availability issues.
                // Values less than or equal to 0 will result in an exception during configuration.
            }
        }

        /// <summary>
        /// Demonstrates how to configure the backoff time in milliseconds between retry attempts
        /// when communicating with the schema registry. This controls how long to wait before
        /// trying again after a failed attempt.
        /// <para>Valid values are non-negative values (0 or greater) or a TimeSpan object.</para>
        /// <para>The default value is 500 milliseconds.</para>
        /// </summary>
        public static void DeserializeWithRequestAttemptBackoff()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Example 1: Set backoff time using an integer value (milliseconds)
            config[JsonSchemaPropertyKeys.RequestAttemptBackoffMs] = 1000;

            // Example 2: Set backoff time using a string value (milliseconds)
            // config[JsonSchemaPropertyKeys.RequestAttemptBackoffMs] = "1000";

            // Example 3: Set backoff time using a TimeSpan object
            // config[JsonSchemaPropertyKeys.RequestAttemptBackoffMs] = TimeSpan.FromSeconds(1);

            // Example 4: No backoff between retry attempts
            // config[JsonSchemaPropertyKeys.RequestAttemptBackoffMs] = 0;

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured with the specified backoff time.
                // Longer backoff times can help during temporary registry overload situations.
                // Shorter backoff times provide faster recovery when issues are brief.
                // A backoff of zero will cause immediate retries with no delay.
            }
        }

        /// <summary>
        /// Demonstrates how to configure both request attempts and backoff time together
        /// for a complete retry strategy.
        /// </summary>
        public static void DeserializeWithCompleteRetryStrategy()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Configure number of request attempts
            config[JsonSchemaPropertyKeys.RequestAttempts] = 5L;

            // Configure backoff time between attempts (2 seconds)
            config[JsonSchemaPropertyKeys.RequestAttemptBackoffMs] = TimeSpan.FromSeconds(2);

            // Optionally configure to use cached values on error
            config[JsonSchemaPropertyKeys.UseCachedOnError] = true;

            // Create and configure JSON Schema deserializer
            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);

                // At this point, the JSON Schema deserializer is configured with a complete retry strategy:
                // - Up to 5 attempts will be made to contact the schema registry
                // - A 2-second backoff period will occur between attempts
                // - If all attempts fail, cached values will be used instead of throwing an exception
            }
        }
    }
}
