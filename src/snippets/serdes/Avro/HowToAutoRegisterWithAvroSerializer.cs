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
using Avro;
using Avro.Generic;
using com.solace.samples.serdes.avro.schema;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.Serdes;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating how to use auto-registration with the Avro serializer.
    /// Auto-registration allows a serializer with write access to the schema registry to register schemas
    /// on the first serialize call, eliminating the need to pre-upload schemas manually.
    /// <para>
    /// Unlike JSON Schema, an Avro record carries its own schema (a <see cref="GenericRecord"/> is
    /// constructed from a <see cref="Schema"/>, and generated specific records expose one too), so the
    /// serializer reads the schema directly from the record. No schema-location/file is used for Avro.
    /// </para>
    /// <para>
    /// This class includes scenarios for:
    /// </para>
    /// <list type="bullet">
    ///   <item>AutoRegisterWithFindOrCreateVersion - Register schema; reuse an existing version if content matches</item>
    ///   <item>AutoRegisterWithCreateVersion - Register schema; always create a new version</item>
    ///   <item>AutoRegisterWithFail - Register schema; fail if the artifact already exists</item>
    ///   <item>AutoRegisterWithSpecificRecord - Auto-register using a generated specific record instead of a GenericRecord</item>
    /// </list>
    /// <para>
    /// All scenarios require a registry user with write access (not a read-only account).
    /// </para>
    /// </summary>
    public static class HowToAutoRegisterWithAvroSerializer
    {
        /// <summary>
        /// Demonstrates auto-registration using the default <see cref="SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion"/> behavior.
        /// On the first serialize call, the serializer reads the schema from the record and registers it in
        /// the schema registry. If an artifact with the same content already exists, the existing version is
        /// reused. If the content differs, a new version is created.
        /// Subsequent serialize calls use a cached schema reference and do not re-register. The cache expires
        /// after the TTL set by <see cref="AvroPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will re-register the schema.
        /// </summary>
        public static async Task AutoRegisterWithFindOrCreateVersion()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[AvroPropertyKeys.AuthUsername] = "sr-developer";
            config[AvroPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[AvroPropertyKeys.AutoRegisterArtifact] = true;

            // FindOrCreateVersion (default): reuses an existing version if schema content matches,
            // otherwise creates a new version. This is safe to use in deployments where the same
            // schema may already be present in the registry.
            config[AvroPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion;

            // Create the Avro record. The schema is carried by the record itself — the serializer
            // reads it from here to register the artifact (no schema-location/file is used for Avro).
            var user = CreateUserRecord();

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // On the first call, the serializer reads the schema from the record, registers it in
                // the registry under the artifact id matching the topic name, then serializes the data.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/avro", user, headers);

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
        /// after the TTL set by <see cref="AvroPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will re-register the schema.
        /// </summary>
        public static async Task AutoRegisterWithCreateVersion()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[AvroPropertyKeys.AuthUsername] = "sr-developer";
            config[AvroPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[AvroPropertyKeys.AutoRegisterArtifact] = true;

            // CreateVersion: always creates a new version in the registry, regardless of whether
            // an identical version already exists.
            config[AvroPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.CreateVersion;

            // Create the Avro record. The schema is carried by the record itself.
            var user = CreateUserRecord();

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // On the first call, the serializer registers the schema as a new version
                // regardless of whether an identical version already exists, then serializes the data.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/avro", user, headers);

                // userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates auto-registration using <see cref="SchemaResolverProperties.IfArtifactExists.Fail"/>.
        /// The first serialize call registers the schema. If the artifact already exists in the registry,
        /// a <see cref="Solace.Serdes.SerializationException"/> is thrown instead of creating a new version.
        /// Use this when schemas must be registered exactly once and re-registration should be treated as an error.
        /// Subsequent serialize calls use a cached schema reference and do not re-register. The cache expires
        /// after the TTL set by <see cref="AvroPropertyKeys.CacheTtlMs"/> (default 30 seconds), after
        /// which the next serialize call will attempt to re-register and throw if the artifact already exists.
        /// </summary>
        public static async Task AutoRegisterWithFail()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[AvroPropertyKeys.AuthUsername] = "sr-developer";
            config[AvroPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[AvroPropertyKeys.AutoRegisterArtifact] = true;

            // Fail: throws a SerializationException if the artifact already exists in the registry.
            // Use this to enforce that schemas are registered exactly once.
            config[AvroPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.Fail;

            // Create the Avro record. The schema is carried by the record itself.
            var user = CreateUserRecord();

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                try
                {
                    // On the first call, the serializer registers the schema and serializes the data.
                    // userBytes and headers are then ready to be applied to the messaging system of choice.
                    byte[] userBytes = await serializer.SerializeAsync("solace/samples/avro", user, headers);
                }
                catch (SerializationException)
                {
                    // With Fail, if the artifact already exists in the registry, registration fails
                    // and a SerializationException is thrown instead of creating a new version.
                }
            }
        }

        /// <summary>
        /// Demonstrates auto-registration using a generated Avro specific record (<see cref="User"/>)
        /// instead of a <see cref="GenericRecord"/>. Auto-registration works identically for both:
        /// a specific record also carries its schema (via its <c>Schema</c> property), so the serializer
        /// reads the schema from the record and registers it. The only difference is that the data is a
        /// strongly-typed, generated class rather than a dynamically-populated <see cref="GenericRecord"/>.
        /// This example uses the default <see cref="SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion"/>
        /// behavior; the other modes apply the same way as with a <see cref="GenericRecord"/>.
        /// </summary>
        public static async Task AutoRegisterWithSpecificRecord()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties.
            // The registry user must have write access to register schemas.
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";
            config[AvroPropertyKeys.AuthUsername] = "sr-developer";
            config[AvroPropertyKeys.AuthPassword] = "devPassword";

            // Enable automatic schema registration on the first serialize call.
            config[AvroPropertyKeys.AutoRegisterArtifact] = true;
            config[AvroPropertyKeys.AutoRegisterArtifactIfExists] = SchemaResolverProperties.IfArtifactExists.FindOrCreateVersion;

            // Create a generated specific record. Like a GenericRecord, it carries its own schema
            // (via User.Schema), so the serializer registers the schema from the record.
            var user = new User
            {
                name = "John Doe",
                id = "-1",
                email = "support@solace.com"
            };

            // Note the type parameter is the generated type: AvroSerializer<User>.
            using (var serializer = new AvroSerializer<User>())
            {
                serializer.Configure(config);

                // Create headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // On the first call, the serializer reads the schema from the specific record,
                // registers it in the registry, then serializes the data.
                byte[] userBytes = await serializer.SerializeAsync("solace/samples/avro", user, headers);

                // userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Builds a populated <see cref="GenericRecord"/> for the User schema. The record is constructed
        /// from the schema, so it carries the schema the serializer registers. The schema is reused from
        /// the generated <see cref="User"/> type (<see cref="User._SCHEMA"/>) to avoid duplicating it.
        /// </summary>
        private static GenericRecord CreateUserRecord()
        {
            var user = new GenericRecord((RecordSchema)User._SCHEMA);
            user.Add("name", "John Doe");
            user.Add("id", "-1");
            user.Add("email", "support@solace.com");
            return user;
        }
    }
}
