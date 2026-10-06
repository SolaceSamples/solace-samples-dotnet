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
using Avro.Generic;
using Solace.SchemaRegistry.Serdes.Avro;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure schema registry retry
    /// and fault-tolerance properties for the <see cref="AvroSerializer{T}"/>.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="ConfigureWithRetryAndBackoff"/> - Configuring retry count and backoff duration</item>
    ///   <item><see cref="ConfigureWithNoRetries"/> - Disabling retries for fail-fast behavior</item>
    ///   <item><see cref="ConfigureWithUseCachedOnError"/> - Falling back to a cached schema on registry outage</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureAvroSerializerRetryProperties
    {
        /// <summary>
        /// Demonstrates how to configure retry count and backoff duration for schema registry requests.
        /// </summary>
        /// <remarks>
        /// Retries up to 5 times with 1-second backoff between attempts. Default is 3 attempts, 500 ms backoff.
        /// <para>
        /// <see cref="AvroPropertyKeys.RequestAttempts"/> accepts a positive <see cref="long"/> value greater than 0 (default: 3).
        /// <see cref="AvroPropertyKeys.RequestAttemptBackoffMs"/> accepts a non-negative numeric value in milliseconds
        /// or a <see cref="TimeSpan"/> (default: 500 ms).
        /// </para>
        /// </remarks>
        public static void ConfigureWithRetryAndBackoff()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Retries up to 5 times before giving up. Default is 3 attempts.
            // Values less than or equal to 0 will result in an exception during configuration.
            config[AvroPropertyKeys.RequestAttempts] = 5L;

            // Wait 1 second between retry attempts. Default is 500 ms.
            // Can also be expressed as a TimeSpan: TimeSpan.FromSeconds(1)
            config[AvroPropertyKeys.RequestAttemptBackoffMs] = 1000;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured with a retry strategy:
                // - Up to 5 attempts will be made to contact the schema registry
                // - A 1-second backoff period will occur between each attempt
                // - A higher attempt count increases resilience during temporary registry availability issues
            }
        }

        /// <summary>
        /// Demonstrates how to disable retries so the serializer fails immediately on the first registry error.
        /// </summary>
        /// <remarks>
        /// Disables retries. Fails immediately on first error.
        /// <para>
        /// Use this configuration when you need deterministic, low-latency failure behavior
        /// and prefer to handle registry errors in the application rather than waiting through retries.
        /// </para>
        /// </remarks>
        public static void ConfigureWithNoRetries()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Single attempt — no retries. Any registry error immediately throws an exception.
            config[AvroPropertyKeys.RequestAttempts] = 1L;

            // No backoff needed when there are no retries.
            config[AvroPropertyKeys.RequestAttemptBackoffMs] = 0;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured for fail-fast behavior.
                // Any registry communication failure will immediately propagate as an exception.
            }
        }

        /// <summary>
        /// Demonstrates how to configure the serializer to fall back to a cached schema when all retry
        /// attempts are exhausted and the registry is still unavailable.
        /// </summary>
        /// <remarks>
        /// After all retry attempts are exhausted, falls back to a cached schema if one exists.
        /// Combine with a non-zero <see cref="AvroPropertyKeys.CacheTtlMs"/> for effective fault tolerance.
        /// <para>
        /// When <see cref="AvroPropertyKeys.UseCachedOnError"/> is <c>true</c> and the cache holds a
        /// previously resolved schema, a registry failure returns the stale cached value instead of throwing.
        /// If no cached entry exists (e.g. on the very first call), the error is still propagated.
        /// The default is <c>false</c>.
        /// </para>
        /// </remarks>
        public static void ConfigureWithUseCachedOnError()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // A non-zero TTL reduces registry load during normal operation. UseCachedOnError
            // works independently of the TTL: any previous successful resolution is used as
            // fallback, even if the cache entry has since expired.
            config[AvroPropertyKeys.CacheTtlMs] = 60000;

            // After all retry attempts fail, return the last cached schema instead of throwing.
            // Has no effect on the very first call because no cached entry exists yet.
            config[AvroPropertyKeys.UseCachedOnError] = true;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured with UseCachedOnError=true.
                // If the registry becomes unavailable after the first successful resolution, the
                // serializer returns the last successfully resolved schema from the cache.
            }
        }
    }
}
