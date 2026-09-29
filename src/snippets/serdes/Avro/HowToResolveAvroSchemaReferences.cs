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
using Avro;
using Avro.Generic;
using Solace.SchemaRegistry.Serdes.Avro;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating how to serialize and deserialize Avro records whose
    /// schema references another schema registered in the Schema Registry.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="SerializeWithAvroSchemaReferences"/> - Serializing a <see cref="GenericRecord"/> whose schema references another registered schema</item>
    ///   <item><see cref="DeserializeWithAvroSchemaReferences"/> - Deserializing bytes for a schema with references and accessing nested fields</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// From a developer's perspective, reference resolution is automatic inside the serializer and
    /// deserializer. The two developer responsibilities are:
    /// <list type="number">
    ///   <item>Pre-registering all referenced schemas in the Schema Registry before the schemas that
    ///   depend on them (e.g. register <c>User</c> before <c>UserAccount</c>).</item>
    ///   <item>Parsing schema dependencies in the correct order when constructing a
    ///   <see cref="GenericRecord"/> for serialization — the referenced schema must be parsed first
    ///   so that Apache Avro can resolve the named type reference.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class HowToResolveAvroSchemaReferences
    {
        /// <summary>
        /// The destination the record is published to. With the default artifact resolver strategy
        /// the destination name is also the artifact id used to resolve the schema.
        /// </summary>
        private const string UserAccountDestination = "solace/samples/user-account/avro";

        /// <summary>
        /// Path to the User AVRO schema document. The samples ship this schema in the Resources project at
        /// <c>Resources/Avro/Schemas/user.avsc</c>; adjust the path for your own application layout.
        /// </summary>
        private static readonly string UserSchemaPath = Path.Combine(AppContext.BaseDirectory, "Avro/Schemas/user.avsc");

        /// <summary>
        /// Path to the UserAccount AVRO schema document. The samples ship this schema in the Resources project at
        /// <c>Resources/Avro/Schemas/user-account.avsc</c>; adjust the path for your own application layout.
        /// </summary>
        private static readonly string UserAccountSchemaPath = Path.Combine(AppContext.BaseDirectory, "Avro/Schemas/user-account.avsc");

        /// <summary>
        /// The sample User AVRO schema, parsed from <see cref="UserSchemaPath"/>.
        /// Must be parsed before <see cref="UserAccountSchema"/> so the named type is available.
        /// </summary>
        private static readonly RecordSchema UserSchema;

        /// <summary>
        /// The sample UserAccount AVRO schema, parsed from <see cref="UserAccountSchemaPath"/>.
        /// References <c>User</c> as a named type, so <see cref="UserSchema"/> must be parsed first.
        /// </summary>
        private static readonly RecordSchema UserAccountSchema;

        static HowToResolveAvroSchemaReferences()
        {
            // Parse User first so Apache Avro registers the named type before parsing UserAccount.
            // This is the standard approach for schemas that reference other named types.
            var names = new SchemaNames();
            UserSchema = (RecordSchema)Schema.Parse(File.ReadAllText(UserSchemaPath), names, "");
            UserAccountSchema = (RecordSchema)Schema.Parse(File.ReadAllText(UserAccountSchemaPath), names, "");
        }

        /// <summary>
        /// Demonstrates how to serialize a <see cref="GenericRecord"/> whose Avro schema references
        /// another schema registered in the registry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All referenced schemas (e.g. <c>User</c>) must be registered in the Schema Registry
        /// before the schemas that depend on them (e.g. <c>UserAccount</c>). The serializer fetches
        /// the <c>UserAccount</c> schema by artifact id and relies on the registry having resolved
        /// the reference to <c>User</c> at registration time.
        /// </para>
        /// <para>
        /// Content-based resolution must be bypassed for Avro schemas with cross-schema references.
        /// The default content-based resolution serializes the schema via <c>Schema.ToString()</c>,
        /// which inlines all referenced type definitions. Because the registry stores schemas in
        /// non-inlined form, the bytes never match and content-based lookup always fails for schemas
        /// with cross-references. Use either <see cref="AvroPropertyKeys.FindLatestArtifact"/> or
        /// <see cref="AvroPropertyKeys.ExplicitArtifactVersion"/> to fetch by artifact coordinates
        /// instead.
        /// </para>
        /// </remarks>
        public static async Task SerializeWithAvroSchemaReferences()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Content-based resolution must be bypassed for Avro schemas with cross-schema references.
            // Schema.ToString() inlines all referenced type definitions, but the registry stores
            // schemas in non-inlined form, so the bytes never match. Use FindLatestArtifact or
            // ExplicitArtifactVersion to fetch the artifact by coordinates instead.
            config[AvroPropertyKeys.FindLatestArtifact] = true;
            // Alternatively, pin to a specific version:
            // config[AvroPropertyKeys.ExplicitArtifactVersion] = "1.0.0";

            // Build the nested User record first, then embed it in UserAccount
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            var userAccount = CreateEmptyUserAccountRecord();
            userAccount.Add("accountId", "-1");
            userAccount.Add("isActive", true);
            userAccount.Add("user", user);

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer resolves the UserAccount schema artifact from the registry and writes
                // its schema id to the headers so the consumer can deserialize the payload.
                var userAccountBytes = await serializer.SerializeAsync(UserAccountDestination, userAccount, headers);

                // At this point, userAccountBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize a payload whose schema references another schema in the
        /// registry, and how to access nested fields from the resulting <see cref="GenericRecord"/>.
        /// </summary>
        /// <param name="destinationName">Destination name from the messaging system.</param>
        /// <param name="payloadBytes">Serialized UserAccount bytes produced by <see cref="AvroSerializer{T}"/>.</param>
        /// <param name="headers">Headers from the messaging system. Must include the schema id header written by the serializer.</param>
        /// <remarks>
        /// Schema references require no additional configuration on the deserialize side. Resolving the
        /// schema id from the headers also fetches every schema it references, transitively, so the full
        /// type hierarchy is available when reading nested fields.
        /// </remarks>
        public static async Task DeserializeWithAvroSchemaReferences(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The deserializer automatically fetches the UserAccount schema and all transitively
                // referenced schemas (e.g. User) from the registry using the schema id in the headers.
                var userAccount = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the userAccount record can be used in processing.
                // The referenced User schema is deserialized as a nested GenericRecord.
                var user = (GenericRecord)userAccount["user"];
            }
        }

        /// <summary>
        /// Creates an empty <see cref="GenericRecord"/> from the sample UserAccount AVRO schema.
        /// </summary>
        /// <returns>An empty <see cref="GenericRecord"/> with the UserAccount schema attached.</returns>
        private static GenericRecord CreateEmptyUserAccountRecord() => new GenericRecord(UserAccountSchema);

        /// <summary>
        /// Creates an empty <see cref="GenericRecord"/> from the sample User AVRO schema.
        /// </summary>
        /// <returns>An empty <see cref="GenericRecord"/> with the User schema attached.</returns>
        private static GenericRecord CreateEmptyUserRecord() => new GenericRecord(UserSchema);
    }
}
