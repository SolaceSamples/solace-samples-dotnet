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
using Solace.SchemaRegistry.Serdes.Avro;
using System.Security.Cryptography.X509Certificates;
using Avro.Generic;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating the configuration of Avro deserializers
    /// with Schema Registry connections. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>Authenticated connections (basic auth)</item>
    ///   <item>Secure connections using TLS</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureAvroDeserializerSchemaRegistryConnection
    {
        /// <summary>
        /// Demonstrates how to configure an Avro deserializer with an authenticated Schema Registry endpoint.
        /// </summary>
        public static void ConfigureWithAuthenticatedSchemaRegistryEndpoint()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set Schema Registry URL
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Set authentication credentials
            config[AvroPropertyKeys.AuthUsername] = "sr-readonly";
            config[AvroPropertyKeys.AuthPassword] = "roPassword";

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro deserializer with an authenticated Secure Schema Registry endpoint.
        /// </summary>
        public static void ConfigureWithAuthenticatedSecureSchemaRegistryEndpoint()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set Schema Registry URL
            // NOTE: Use HTTPS for secure communication with the Schema Registry
            config[AvroPropertyKeys.RegistryUrl] = "https://localhost:8081/apis/registry/v3";

            // Set authentication credentials
            config[AvroPropertyKeys.AuthUsername] = "sr-readonly";
            config[AvroPropertyKeys.AuthPassword] = "roPassword";

            // Configure TLS properties for secure connection
            // NOTE: The TrustStore is OPTIONAL and supplements the system trust store with additional
            // root CA certificates. The system trust store is always consulted first. Only configure
            // TrustStore when you need to trust certificates not in the system trust store, such as
            // self-signed certificates or certificates issued by private certificate authorities.
            // Load the trusted root CA certificates into an X509Certificate2Collection
            var trustStore = new X509Certificate2Collection();
#if NET5_0_OR_GREATER
            // .NET 5 and later provide a dedicated API for loading PEM-encoded certificates.
            var certificate = X509Certificate2.CreateFromPemFile("path/to/certificate.pem");
#else
            // .NET Framework has no PEM loader; this constructor expects DER or PFX encoding.
            var certificate = new X509Certificate2("path/to/certificate.cer");
#endif
            trustStore.Add(certificate);
            config[AvroPropertyKeys.TrustStore] = trustStore;

            // Configure certificate validation (by default set to true)
            // NOTE: Disabling certificate validation can be useful for debugging purposes.
            // Never disable certificate validation in production.
            // config[AvroPropertyKeys.ValidateCertificate] = false;

            // Configure certificate hostname validation (by default set to true)
            // NOTE: Disabling hostname validation can be useful for debugging purposes.
            // Never disable hostname validation in production.
            // This has no effect if ValidateCertificate is set to false.
            // config[AvroPropertyKeys.ValidateCertificateHostName] = false;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured and ready to use for deserialization.
            }
        }
    }
}
