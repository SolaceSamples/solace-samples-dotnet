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
using Solace.SchemaRegistry.Serdes.JsonSchema;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating the configuration of JSON Schema serializers
    /// with Schema Registry connections. This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>Authenticated connections (basic auth)</item>
    ///   <item>Secure connections using TLS</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureJsonSchemaSerializerSchemaRegistryConnection
    {
        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer with an authenticated Schema Registry endpoint.
        /// </summary>
        public static void SerializeWithAuthenticatedSchemaRegistryEndpoint()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set Schema Registry URL
            config[JsonSchemaPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Set authentication credentials
            config[JsonSchemaPropertyKeys.AuthUsername] = "sr-readonly";
            config[JsonSchemaPropertyKeys.AuthPassword] = "roPassword";

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
            }
        }

        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer with an authenticated Secure Schema Registry endpoint.
        /// </summary>
        public static void SerializeWithAuthenticatedSecureSchemaRegistryEndpoint()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set Schema Registry URL
            // NOTE: Use HTTPS for secure communication with the Schema Registry
            config[JsonSchemaPropertyKeys.RegistryUrl] = "https://localhost:8081/apis/registry/v3";

            // Set authentication credentials
            config[JsonSchemaPropertyKeys.AuthUsername] = "sr-readonly";
            config[JsonSchemaPropertyKeys.AuthPassword] = "roPassword";

            // Configure TLS properties for secure connection
            // NOTE: The TrustStore is OPTIONAL and supplements the system trust store with additional
            // root CA certificates. The system trust store is always consulted first. Only configure
            // TrustStore when you need to trust certificates not in the system trust store, such as
            // self-signed certificates or certificates issued by private certificate authorities.
            // Load the trusted root CA certificates into an X509Certificate2Collection
            var trustStore = new X509Certificate2Collection();
            var certificate = new X509Certificate2("path/to/certificate.pem");
            trustStore.Add(certificate);
            config[JsonSchemaPropertyKeys.TrustStore] = trustStore;

            // Configure certificate validation (by default set to true)
            // NOTE: Disabling certificate validation can be useful for debugging purposes,
            // but it's not recommended for production use as it reduces security.
            // config[JsonSchemaPropertyKeys.ValidateCertificate] = false;

            // Configure certificate hostname validation (by default set to true)
            // NOTE: Disabling hostname validation can be useful for debugging purposes,
            // but it's not recommended for production use as it reduces security.
            // This has no effect if ValidateCertificate is set to false.
            // config[JsonSchemaPropertyKeys.ValidateCertificateHostName] = false;

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
            }
        }
    }
}
