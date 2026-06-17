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
using System.Threading.Tasks;
using Resources.JsonSchema;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.SchemaRegistry.Serdes.JsonSchema;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating how to use auto-registration with the JSON Schema serializer.
    /// Auto-registration allows a serializer with write access to the schema registry to register schemas
    /// on the first serialize call, eliminating the need to pre-upload schemas manually.
    /// <para>
    /// This class includes scenarios for:
    /// </para>
    /// <list type="bullet">
    ///   <item>AutoRegisterWithFindOrCreateVersion - Register schema; reuse an existing version if content matches</item>
    ///   <item>AutoRegisterWithCreateVersion - Register schema; always create a new version</item>
    ///   <item>AutoRegisterWithFail - Register schema; fail if the artifact already exists</item>
    /// </list>
    /// <para>
    /// All scenarios require:
    /// </para>
    /// <list type="bullet">
    ///   <item>A registry user with write access (not a read-only account)</item>
    ///   <item>A local schema file pointed to by <see cref="JsonSchemaPropertyKeys.SchemaLocation"/></item>
    /// </list>
    /// </summary>
    public static class HowToAutoRegisterWithJsonSchemaSerializer
    {
        /// <summary>
        /// Demonstrates auto-registration using the default <see cref="SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion"/> behavior.
        /// On the first serialize call, the serializer reads the schema from the file at <c>SchemaLocation</c>
        /// and registers it in the schema registry. If an artifact with the same content already exists, the
        /// existing version is reused. If the content differs, a new version is created.
        /// Subsequent serialize calls use a cached schema reference and do not re-register. The cache expires
        /// after the TTL set by <see cref="JsonSchemaPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will re-register the schema.
        /// </summary>
        public static async Task AutoRegisterWithFindOrCreateVersion()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[JsonSchemaPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[JsonSchemaPropertyKeys.AuthUsername] = "sr-developer";
            config[JsonSchemaPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifact] = true;

            // Path to the local JSON Schema file to read and register.
            // Use AppContext.BaseDirectory so the path resolves correctly relative to the application binary.
            config[JsonSchemaPropertyKeys.SchemaLocation] = Path.Combine(AppContext.BaseDirectory, "Serdes/JsonSchema/Schemas/user.json");

            // FindOrCreateVersion (default): reuses an existing version if schema content matches,
            // otherwise creates a new version. This is safe to use in deployments where the same
            // schema may already be present in the registry.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion;

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

                // On the first call, the serializer reads the schema from SchemaLocation,
                // registers it in the registry under the artifact id matching the topic name,
                // then serializes the data.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/json", user, headers);

                // Subsequent calls use the cached schema reference without re-registering.
                // userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates auto-registration using <see cref="SchemaResolverProperties.IfArtifactExists.CreateVersion"/>.
        /// Every serialize call that triggers registration will create a new version of the artifact,
        /// even if an identical schema version already exists. Use this when schema versioning is
        /// managed externally and a new version is always required on registration.
        /// Subsequent serialize calls use a cached schema reference and do not re-register. The cache expires
        /// after the TTL set by <see cref="JsonSchemaPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will re-register the schema.
        /// </summary>
        public static async Task AutoRegisterWithCreateVersion()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[JsonSchemaPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[JsonSchemaPropertyKeys.AuthUsername] = "sr-developer";
            config[JsonSchemaPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifact] = true;

            // Path to the local JSON Schema file to read and register.
            // Use AppContext.BaseDirectory so the path resolves correctly relative to the application binary.
            config[JsonSchemaPropertyKeys.SchemaLocation] = Path.Combine(AppContext.BaseDirectory, "Serdes/JsonSchema/Schemas/user.json");

            // CreateVersion: always creates a new version in the registry, regardless of whether
            // an identical version already exists.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.CreateVersion;

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

                // On the first call, the serializer registers the schema as a new version
                // regardless of whether an identical version already exists, then serializes the data.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/json", user, headers);

                // userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates auto-registration using <see cref="SchemaResolverProperties.IfArtifactExists.Fail"/>.
        /// The first serialize call registers the schema. If the artifact already exists in the registry,
        /// a <see cref="Solace.Serdes.SerializationException"/> is thrown instead of creating a new version.
        /// Use this when schemas must be registered exactly once and re-registration should be treated as an error.
        /// Subsequent serialize calls use a cached schema reference and do not re-register. The cache expires
        /// after the TTL set by <see cref="JsonSchemaPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will attempt to re-register and throw if the artifact already exists.
        /// </summary>
        public static async Task AutoRegisterWithFail()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[JsonSchemaPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[JsonSchemaPropertyKeys.AuthUsername] = "sr-developer";
            config[JsonSchemaPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifact] = true;

            // Path to the local JSON Schema file to read and register.
            // Use AppContext.BaseDirectory so the path resolves correctly relative to the application binary.
            config[JsonSchemaPropertyKeys.SchemaLocation] = Path.Combine(AppContext.BaseDirectory, "Serdes/JsonSchema/Schemas/user.json");

            // Fail: throws a SerializationException if the artifact already exists in the registry.
            // Use this to enforce that schemas are registered exactly once.
            config[JsonSchemaPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.Fail;

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

                // On the first call, the serializer registers the schema. If the artifact already
                // exists, a SerializationException is thrown.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/json", user, headers);

                // userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }
    }
}
