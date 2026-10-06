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
using Avro.Generic;
using Solace.SchemaRegistry.Serdes.Avro;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure schema resolution caching
    /// for the <see cref="AvroDeserializer{T}"/>.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="ConfigureWithCustomCacheTtl"/> - Configuring a custom cache TTL</item>
    ///   <item><see cref="ConfigureWithCachingDisabled"/> - Disabling caching entirely</item>
    ///   <item><see cref="ConfigureWithCacheLatestDisabled"/> - Always re-fetching latest-version lookups from the registry</item>
    ///   <item><see cref="ConfigureWithUseCachedOnError"/> - Falling back to a cached schema on registry outage</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureAvroDeserializerResolutionCaching
    {
        /// <summary>
        /// Demonstrates how to configure an Avro deserializer with a custom schema cache TTL.
        /// </summary>
        /// <remarks>
        /// Resolved schemas are held in memory for the duration of the TTL. A longer TTL reduces
        /// round trips to the registry but means schema updates take longer to propagate.
        /// The default TTL is 30 seconds (30000 ms).
        /// </remarks>
        public static void ConfigureWithCustomCacheTtl()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Schemas are cached for 60 seconds. Default is 30 seconds (30000 ms).
            config[AvroPropertyKeys.CacheTtlMs] = 60000;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured with a 60-second cache TTL.
                // Resolved schemas are served from memory for up to 60 seconds before the
                // registry is contacted again.
            }
        }

        /// <summary>
        /// Demonstrates how to disable schema resolution caching on an Avro deserializer.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="AvroPropertyKeys.CacheTtlMs"/> to <c>0</c> disables caching.
        /// Every deserialization call contacts the registry to resolve the schema. Use this only
        /// when schema updates must propagate immediately; otherwise prefer a short TTL to avoid
        /// unnecessary registry load.
        /// </remarks>
        public static void ConfigureWithCachingDisabled()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Setting CacheTtlMs to 0 disables caching. Schemas are fetched from the registry
            // on every request.
            config[AvroPropertyKeys.CacheTtlMs] = 0;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured with caching disabled.
                // Every deserialization call will contact the registry to resolve the schema.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro deserializer to always re-fetch the latest-version
        /// artifact reference from the registry on every call.
        /// </summary>
        /// <remarks>
        /// When <see cref="AvroPropertyKeys.CacheLatest"/> is <c>false</c>, latest-version lookups
        /// (i.e. calls where no explicit version is pinned) always hit the registry to discover the
        /// current latest version. When <c>true</c> (the default), an additional cache entry is
        /// created for the latest-version lookup so the registry is not contacted again until the
        /// cache TTL expires.
        /// </remarks>
        public static void ConfigureWithCacheLatestDisabled()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // When false, latest-version lookups always hit the registry. When true (default),
            // an additional cache entry is created for latest/no-version lookups.
            config[AvroPropertyKeys.CacheLatest] = false;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured with CacheLatest=false.
                // Every deserialization call will contact the registry to resolve the latest artifact
                // version, ensuring schema updates are picked up immediately.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro deserializer to fall back to a cached schema
        /// when the registry is temporarily unavailable.
        /// </summary>
        /// <remarks>
        /// When <see cref="AvroPropertyKeys.UseCachedOnError"/> is <c>true</c> and the cache
        /// holds a previously resolved schema, a registry failure returns the stale cached value
        /// instead of throwing. If no cached entry exists (e.g. on the very first call), the
        /// error is still propagated. The default is <c>false</c>.
        /// </remarks>
        public static void ConfigureWithUseCachedOnError()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // When the registry is unavailable, fall back to the cached schema instead of throwing.
            // Requires a valid cache entry to exist; has no effect on the very first call.
            config[AvroPropertyKeys.UseCachedOnError] = true;

            // Create and configure Avro deserializer
            using (var deserializer = new AvroDeserializer<GenericRecord>())
            {
                deserializer.Configure(config);

                // At this point, the Avro deserializer is configured with UseCachedOnError=true.
                // If the registry becomes unavailable after the first successful resolution, the
                // deserializer returns the last successfully resolved schema from the cache.
            }
        }
    }
}
