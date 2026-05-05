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
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System.Text.Json.Nodes;
using SerdesLogging = Solace.SchemaRegistry.Serdes.Core.Logging;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Code snippets for configuring logging with JSON Schema SERDES.
    /// <para>
    /// Log level and provider can also be configured at startup via the
    /// <c>SOLACE_SERDES_LOG_LEVEL</c> and <c>SOLACE_SERDES_LOG_PROVIDER</c> environment variables.
    /// </para>
    /// </summary>
    public static class HowToConfigureJsonSchemaSerdesLogging
    {
        /// <summary>
        /// Sets the global log level at runtime. May be called at any time; takes effect on
        /// subsequent log calls. Available levels: Error, Warn (default), Info, Debug.
        /// </summary>
        public static void SetLogLevelAtRuntime()
        {
            SerdesLogging.LoggerConfiguration.LogLevel = SerdesLogging.LogLevel.Debug;

            var config = new Dictionary<string, object>
            {
                [SchemaResolverPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3",
                [SchemaResolverPropertyKeys.AuthUsername] = "sr-readonly",
                [SchemaResolverPropertyKeys.AuthPassword] = "roPassword"
            };

            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);
            }
        }

        /// <summary>
        /// Enables network trace logging for a serializer. Logs HTTP request and response details
        /// for every Schema Registry call at Debug level. Sensitive headers are redacted.
        /// For debugging only — do not use in production.
        /// </summary>
        public static void EnableNetworkTraceLoggingForSerializer()
        {
            // Network trace messages are emitted at Debug level
            SerdesLogging.LoggerConfiguration.LogLevel = SerdesLogging.LogLevel.Debug;

            var config = new Dictionary<string, object>
            {
                [SchemaResolverPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3",
                [SchemaResolverPropertyKeys.AuthUsername] = "sr-readonly",
                [SchemaResolverPropertyKeys.AuthPassword] = "roPassword",
                [SchemaResolverPropertyKeys.NetworkTraceEnabled] = true, // requires log level Debug to produce output
                // Optional: max body size to log in bytes (default: 10240)
                [SchemaResolverPropertyKeys.NetworkTraceMaxLogSize] = 4096
            };

            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);
            }
        }

        /// <summary>
        /// Enables network trace logging for a deserializer. Logs HTTP request and response details
        /// for every Schema Registry call at Debug level. Sensitive headers are redacted.
        /// For debugging only — do not use in production.
        /// </summary>
        public static void EnableNetworkTraceLoggingForDeserializer()
        {
            // Network trace messages are emitted at Debug level
            SerdesLogging.LoggerConfiguration.LogLevel = SerdesLogging.LogLevel.Debug;

            var config = new Dictionary<string, object>
            {
                [SchemaResolverPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3",
                [SchemaResolverPropertyKeys.AuthUsername] = "sr-readonly",
                [SchemaResolverPropertyKeys.AuthPassword] = "roPassword",
                [SchemaResolverPropertyKeys.NetworkTraceEnabled] = true, // requires log level Debug to produce output
                // Optional: max body size to log in bytes (default: 10240)
                [SchemaResolverPropertyKeys.NetworkTraceMaxLogSize] = 4096
            };

            using (var deserializer = new JsonSchemaDeserializer<JsonNode>())
            {
                deserializer.Configure(config);
            }
        }
    }
}
