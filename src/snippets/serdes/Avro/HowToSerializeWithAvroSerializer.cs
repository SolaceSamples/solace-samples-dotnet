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
using Avro.Specific;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating the serialize operation of the <see cref="AvroSerializer{T}"/>
    /// against a Schema Registry.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="SerializeWithUserAvroSchema"/> - Serializing a record with the sample AVRO User schema</item>
    ///   <item><see cref="SerializeWithFindLatest"/> - Serializing with the schema resolver 'find-latest' option</item>
    ///   <item><see cref="SerializeWithExplicitSchemaVersion"/> - Serializing with an explicit schema version from the registry</item>
    ///   <item><see cref="SerializeWithAutoRegister"/> - Registering the schema taken from the record when it is not already in the registry</item>
    ///   <item><see cref="SerializeWithBinaryEncoding"/> - Serializing with the AVRO binary encoding</item>
    ///   <item><see cref="SerializeWithJsonEncoding"/> - Serializing with the AVRO JSON encoding</item>
    ///   <item><see cref="SerializeWithPrimitiveString"/> - Serializing the AVRO primitive string type</item>
    ///   <item><see cref="SerializeWithPrimitiveInt"/> - Serializing the AVRO primitive int type</item>
    ///   <item><see cref="SerializeWithPrimitiveLong"/> - Serializing the AVRO primitive long type</item>
    ///   <item><see cref="SerializeWithPrimitiveFloat"/> - Serializing the AVRO primitive float type</item>
    ///   <item><see cref="SerializeWithPrimitiveDouble"/> - Serializing the AVRO primitive double type</item>
    ///   <item><see cref="SerializeWithPrimitiveBoolean"/> - Serializing the AVRO primitive boolean type</item>
    ///   <item><see cref="SerializeWithPrimitiveBytes"/> - Serializing the AVRO primitive bytes type</item>
    ///   <item><see cref="SerializeWithPrimitiveNull"/> - Serializing the AVRO primitive null type</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every <c>SerializeAsync</c> call on the <see cref="AvroSerializer{T}"/> returns the encoded payload and
    /// writes the schema registry headers into the supplied headers dictionary:
    /// <list type="bullet">
    ///   <item><see cref="SerdeHeaders.SchemaId"/> of type <c>long</c>, identifying the schema used</item>
    ///   <item><see cref="AvroHeaders.EncodingType"/> of type <c>string</c>, holding the AVRO encoding applied</item>
    /// </list>
    /// The payload and headers are then ready to be applied to the messaging system of choice.
    /// </para>
    /// <para>
    /// The serializer resolves a schema per serialize call, so the schema must already exist in the registry
    /// unless auto-registration is enabled. See <see cref="SerializeWithAutoRegister"/>.
    /// </para>
    /// </remarks>
    public static class HowToSerializeWithAvroSerializer
    {
        /// <summary>
        /// The destination the record is published to. With the default artifact resolver strategy
        /// the destination name is also the artifact id used to resolve the schema.
        /// </summary>
        private const string UserDestination = "solace/samples/avro";

        /// <summary>
        /// Path to the AVRO schema document. The samples ship this schema in the Resources project at
        /// <c>Resources/Avro/Schemas/user.avsc</c>; adjust the path for your own application layout.
        /// </summary>
        private static readonly string UserSchemaPath = Path.Combine(AppContext.BaseDirectory, "Avro/Schemas/user.avsc");

        /// <summary>
        /// Demonstrates how to serialize a <see cref="GenericRecord"/> with the sample AVRO User schema.
        /// </summary>
        public static async Task SerializeWithUserAvroSchema()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
                // userBytes holds the user record serialized as bytes, and headers was populated with the
                // schema registry header fields.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize using the most recently uploaded schema version from the registry.
        /// </summary>
        /// <remarks>
        /// Find-latest resolves the version with the highest GlobalId (most recently uploaded), which is not
        /// necessarily the highest version string. <see cref="AvroPropertyKeys.ExplicitArtifactVersion"/>
        /// takes precedence over find-latest when both are set.
        /// </remarks>
        public static async Task SerializeWithFindLatest()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Add the property to resolve the latest schema version from the registry
            config[AvroPropertyKeys.FindLatestArtifact] = true;

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // Each serialize call resolves the latest version of the artifact rather than the version
                // the artifact resolver strategy would otherwise select.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with an explicit schema version from the Schema Registry.
        /// </summary>
        public static async Task SerializeWithExplicitSchemaVersion()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Add the property to resolve an explicit version of the schema from the registry
            config[AvroPropertyKeys.ExplicitArtifactVersion] = "1.0.0";

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // Each serialize call resolves the explicit version of the artifact rather than the version
                // the artifact resolver strategy would otherwise select.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with auto-registration enabled, which registers the schema taken
        /// from the record when the artifact is not already present in the registry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The schema is taken from the record itself: the schema of a <see cref="GenericRecord"/>, the schema
        /// of an <see cref="ISpecificRecord"/>, or the AVRO primitive schema implied by the value's type.
        /// </para>
        /// <para>
        /// Auto-registration requires registry credentials with write access. Use
        /// <see cref="AvroPropertyKeys.AutoRegisterArtifactIfExists"/> to control what happens when the
        /// artifact already exists; it defaults to finding or creating a matching version.
        /// </para>
        /// </remarks>
        public static async Task SerializeWithAutoRegister()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Auto-registration writes to the registry, so credentials with write access are required
            config[AvroPropertyKeys.AuthUsername] = "sr-developer";
            config[AvroPropertyKeys.AuthPassword] = "devPassword";

            // Enable auto-registration of the schema taken from the record
            config[AvroPropertyKeys.AutoRegisterArtifact] = true;

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The schema of the record is registered under the resolved artifact coordinates when it is
                // not already in the registry, and the resulting schema id is written to the headers.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO binary encoding.
        /// </summary>
        /// <remarks>
        /// Binary is the default encoding, so <see cref="AvroPropertyKeys.EncodingType"/> only needs to be set
        /// to state the intent explicitly. The encoding applied is written to the
        /// <see cref="AvroHeaders.EncodingType"/> header as <c>BINARY</c>, which lets the
        /// <see cref="AvroDeserializer{T}"/> decode the payload regardless of how it is configured itself.
        /// </remarks>
        public static async Task SerializeWithBinaryEncoding()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Select the AVRO binary encoding, which is also the default when the property is not set
            config[AvroPropertyKeys.EncodingType] = AvroProperties.AvroEncoding.Binary;

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The record is encoded with the AVRO BinaryEncoder.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
                // headers[AvroHeaders.EncodingType] holds "BINARY".
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO JSON encoding.
        /// </summary>
        /// <remarks>
        /// The JSON encoding writes the record as AVRO JSON rather than the compact binary form, which trades
        /// payload size for a human readable payload. The encoding applied is written to the
        /// <see cref="AvroHeaders.EncodingType"/> header as <c>JSON</c>, which lets the
        /// <see cref="AvroDeserializer{T}"/> decode the payload regardless of how it is configured itself.
        /// </remarks>
        public static async Task SerializeWithJsonEncoding()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Select the AVRO JSON encoding instead of the default binary encoding
            config[AvroPropertyKeys.EncodingType] = AvroProperties.AvroEncoding.Json;

            // Create the User AVRO record
            var user = CreateEmptyUserRecord();
            user.Add("id", "-1");
            user.Add("name", "John Doe");
            user.Add("email", "support@solace.com");

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The record is encoded with the AVRO JsonEncoder.
                var userBytes = await serializer.SerializeAsync(UserDestination, user, headers);

                // At this point, userBytes and headers are ready to be applied to the messaging system of choice.
                // userBytes holds the AVRO JSON encoded record, and headers[AvroHeaders.EncodingType] holds "JSON".
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type string.
        /// </summary>
        /// <remarks>
        /// Primitive schemas are resolved from the registry by their short-form document, for example
        /// <c>"string"</c>. Because the destination name resolves to a single artifact, publish primitives to
        /// a destination of their own rather than reusing a destination that resolves to a record schema.
        /// </remarks>
        public static async Task SerializeWithPrimitiveString()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = "helloworld";

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<string>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "string" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-string", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type int.
        /// </summary>
        public static async Task SerializeWithPrimitiveInt()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = 42;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<int>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "int" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-int", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type long.
        /// </summary>
        public static async Task SerializeWithPrimitiveLong()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = 42L;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<long>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "long" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-long", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type float.
        /// </summary>
        public static async Task SerializeWithPrimitiveFloat()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = 3.14159f;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<float>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "float" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-float", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type double.
        /// </summary>
        public static async Task SerializeWithPrimitiveDouble()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = 2.172D;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<double>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "double" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-double", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type boolean.
        /// </summary>
        public static async Task SerializeWithPrimitiveBoolean()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = true;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<bool>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "boolean" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-boolean", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type bytes.
        /// </summary>
        public static async Task SerializeWithPrimitiveBytes()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            var payload = new byte[] { 0x01, 0x02, 0x03 };

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<byte[]>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "bytes" from the payload type.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-bytes", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
            }
        }

        /// <summary>
        /// Demonstrates how to serialize with the AVRO primitive type null.
        /// </summary>
        /// <remarks>
        /// The AVRO null primitive can only be serialized by an <see cref="AvroSerializer{T}"/> declared with
        /// <c>object</c>, because a null value carries no type of its own to derive a schema from. The AVRO
        /// null primitive encodes to zero bytes.
        /// </remarks>
        public static async Task SerializeWithPrimitiveNull()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create the payload
            object payload = null;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<object>())
            {
                serializer.Configure(config);

                // Create the headers dictionary for serialization
                var headers = new Dictionary<string, object>();

                // At this point, the Avro serializer is configured and ready to use for serialization.
                // The serializer derives the primitive schema document "null" from the null payload.
                var payloadBytes = await serializer.SerializeAsync("solace/samples/avro/primitive-null", payload, headers);

                // At this point, payloadBytes and headers are ready to be applied to the messaging system of choice.
                // payloadBytes is empty, so only the headers identify the schema to the consumer.
            }
        }

        /// <summary>
        /// Creates an empty <see cref="GenericRecord"/> from the sample User AVRO schema.
        /// </summary>
        /// <returns>An empty <see cref="GenericRecord"/> with the User schema attached</returns>
        /// <remarks>
        /// The record is created from the shared <see cref="UserSchema"/>, which declares the fields
        /// <c>id</c>, <c>name</c> and <c>email</c>, all of AVRO type string.
        /// </remarks>
        private static GenericRecord CreateEmptyUserRecord() => new GenericRecord(UserSchema);

        /// <summary>
        /// The sample User AVRO schema, parsed once from <see cref="UserSchemaPath"/> and reused by every
        /// snippet in this class. It declares the fields <c>id</c>, <c>name</c> and <c>email</c>, all of AVRO
        /// type string.
        /// </summary>
        private static readonly RecordSchema UserSchema = (RecordSchema)Schema.Parse(File.ReadAllText(UserSchemaPath));
    }
}
