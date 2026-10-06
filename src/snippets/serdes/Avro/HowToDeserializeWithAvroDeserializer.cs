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
using Avro.Generic;
using Avro.Specific;
using com.solace.samples.serdes.avro.schema;
using Solace.SchemaRegistry.Serdes.Avro;
using Solace.SchemaRegistry.Serdes.Core;
using Solace.Serdes;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating the deserialize operation of the <see cref="AvroDeserializer{T}"/>
    /// against a Schema Registry.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="DeserializeToGenericRecord"/> - Deserializing into a <see cref="GenericRecord"/></item>
    ///   <item><see cref="DeserializeToSpecificRecord"/> - Deserializing into a generated specific record</item>
    ///   <item><see cref="DeserializeToSpecificRecordWithRecordTypeConfig"/> - Selecting the record type by configuration</item>
    ///   <item><see cref="DeserializeWithAvroSchemaReferences"/> - Deserializing a schema that references another schema</item>
    ///   <item><see cref="DeserializeWithPrimitiveString"/> - Deserializing the AVRO primitive string type</item>
    ///   <item><see cref="DeserializeWithPrimitiveInt"/> - Deserializing the AVRO primitive int type</item>
    ///   <item><see cref="DeserializeWithPrimitiveLong"/> - Deserializing the AVRO primitive long type</item>
    ///   <item><see cref="DeserializeWithPrimitiveFloat"/> - Deserializing the AVRO primitive float type</item>
    ///   <item><see cref="DeserializeWithPrimitiveDouble"/> - Deserializing the AVRO primitive double type</item>
    ///   <item><see cref="DeserializeWithPrimitiveBoolean"/> - Deserializing the AVRO primitive boolean type</item>
    ///   <item><see cref="DeserializeWithPrimitiveBytes"/> - Deserializing the AVRO primitive bytes type</item>
    ///   <item><see cref="DeserializeWithPrimitiveNull"/> - Deserializing the AVRO primitive null type</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every snippet takes the destination name, payload and headers received from the messaging system of
    /// choice. Where the <see cref="AvroSerializer{T}"/> writes the schema registry headers, the
    /// <see cref="AvroDeserializer{T}"/> reads them, so the headers must be passed through unmodified:
    /// <list type="bullet">
    ///   <item><see cref="SerdeHeaders.SchemaId"/> of type <c>long</c>, or
    ///   <see cref="SerdeHeaders.SchemaIdString"/> of type <c>string</c> - identifies the schema to resolve
    ///   from the registry</item>
    ///   <item><see cref="AvroHeaders.EncodingType"/> of type <c>string</c> - the AVRO encoding the payload
    ///   was written with</item>
    /// </list>
    /// Both are read from the headers rather than configured, so none of these snippets set an encoding or a
    /// schema id: the <see cref="AvroSerializer{T}"/> always writes them, and the deserializer decodes binary
    /// and JSON payloads alike with no encoding configuration at all.
    /// </para>
    /// <para>
    /// The type argument decides the shape of the deserialized value: an <see cref="ISpecificRecord"/> type
    /// produces a specific record, <see cref="GenericRecord"/> produces a generic record, and an AVRO
    /// primitive type produces that primitive. Any other type argument falls back to the configured
    /// <see cref="AvroPropertyKeys.RecordType"/>.
    /// </para>
    /// <para>
    /// The schema must already exist in the registry. Auto-registration and version selection are
    /// serialize-side concerns with no deserialize equivalent, because the schema id in the headers already
    /// identifies an exact version.
    /// </para>
    /// </remarks>
    public static class HowToDeserializeWithAvroDeserializer
    {
        /// <summary>
        /// Demonstrates how to deserialize into a <see cref="GenericRecord"/> with the sample AVRO User schema.
        /// </summary>
        /// <remarks>
        /// A <see cref="GenericRecord"/> needs no generated type, so this works for any record schema in the
        /// registry. Fields are read by name and come back as <c>object</c>.
        /// </remarks>
        public static async Task DeserializeToGenericRecord(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
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
                // The schema is resolved from the schema id carried in the headers.
                var user = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the user record can be used in processing.
                var name = (string)user["name"];
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize into a generated AVRO specific record (<see cref="User"/>) rather
        /// than a <see cref="GenericRecord"/>.
        /// </summary>
        /// <remarks>
        /// Because <see cref="User"/> implements <see cref="ISpecificRecord"/>, the type argument alone selects
        /// the specific record reader and <see cref="AvroPropertyKeys.RecordType"/> is ignored. The generated
        /// type must match the schema resolved from the registry, otherwise a
        /// <see cref="SerializationException"/> is thrown.
        /// </remarks>
        public static async Task DeserializeToSpecificRecord(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Note the type parameter is the generated type: AvroDeserializer<User>.
            using (var deserializer = new AvroDeserializer<User>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                var user = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the user record can be used in processing. Fields are strongly typed.
                var name = user.name;
            }
        }

        /// <summary>
        /// Demonstrates how to select the specific record reader by configuration when the type argument does
        /// not imply it.
        /// </summary>
        /// <remarks>
        /// <see cref="AvroPropertyKeys.RecordType"/> only takes effect when the type argument is neither an
        /// <see cref="ISpecificRecord"/>, nor <see cref="GenericRecord"/>, nor an AVRO primitive type. Use this
        /// form when the deserializer is built generically; otherwise prefer
        /// <see cref="DeserializeToSpecificRecord"/>.
        /// </remarks>
        public static async Task DeserializeToSpecificRecordWithRecordTypeConfig(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Select the specific record reader. This is only consulted because the type argument is object.
            config[AvroPropertyKeys.RecordType] = AvroProperties.AvroRecordType.SpecificRecord;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<object>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // A generated type matching the resolved schema must be present in the application.
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing, once cast to the generated type.
                var user = (User)payload;
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize a payload whose schema references another schema in the registry.
        /// </summary>
        /// <remarks>
        /// Schema references need no configuration on the deserialize side: resolving the schema id also
        /// resolves the schemas it references, transitively. This example assumes a <c>UserAccount</c> schema
        /// whose <c>user</c> field is a reference to the separately registered <c>User</c> schema.
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
                // Resolving the schema also fetches every schema it references.
                var userAccount = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the userAccount record can be used in processing.
                // The referenced User schema is deserialized as a nested GenericRecord.
                var user = (GenericRecord)userAccount["user"];
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type string.
        /// </summary>
        /// <remarks>
        /// The type argument must match the primitive schema the schema id resolves to. Because a destination
        /// name resolves to a single artifact, consume primitives from a destination of their own rather than
        /// one that resolves to a record schema.
        /// </remarks>
        public static async Task DeserializeWithPrimitiveString(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<string>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "string".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type int.
        /// </summary>
        public static async Task DeserializeWithPrimitiveInt(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<int>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "int".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type long.
        /// </summary>
        public static async Task DeserializeWithPrimitiveLong(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<long>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "long".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type float.
        /// </summary>
        public static async Task DeserializeWithPrimitiveFloat(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<float>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "float".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type double.
        /// </summary>
        public static async Task DeserializeWithPrimitiveDouble(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<double>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "double".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type boolean.
        /// </summary>
        public static async Task DeserializeWithPrimitiveBoolean(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<bool>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "boolean".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type bytes.
        /// </summary>
        public static async Task DeserializeWithPrimitiveBytes(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<byte[]>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The type argument selects the primitive schema document "bytes". This is the decoded AVRO
                // bytes value, not the payload passed in.
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, the payload can be used in processing.
            }
        }

        /// <summary>
        /// Demonstrates how to deserialize the AVRO primitive type null.
        /// </summary>
        /// <remarks>
        /// The AVRO null primitive encodes to zero bytes, so the headers alone identify the schema. Declare the
        /// deserializer with <c>object</c>, because there is no value to deserialize into a more specific type.
        /// </remarks>
        public static async Task DeserializeWithPrimitiveNull(string destinationName, byte[] payloadBytes, IDictionary<string, object> headers)
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<object>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
                // The schema id resolves to the primitive schema document "null".
                var payload = await deserializer.DeserializeAsync(destinationName, payloadBytes, headers);

                // At this point, payload is null. Only the headers identified the schema.
            }
        }
    }
}
