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
using Solace.SchemaRegistry.Serdes.Core.Resolver.Strategy;

namespace Snippets.Serdes.Avro
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure the <see cref="AvroSerializer{T}"/>
    /// with different artifact resolution strategies.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item><see cref="ConfigureWithExplicitArtifactCoordinates"/> - Pinning artifact resolution to explicit coordinates</item>
    ///   <item><see cref="ConfigureWithFindLatestArtifact"/> - Resolving the most recently uploaded artifact version</item>
    ///   <item><see cref="ConfigureWithDestinationIdStrategy"/> - Using <see cref="DestinationIdStrategy{T,S}"/> to derive the artifact reference from the destination name</item>
    ///   <item><see cref="ConfigureWithTopicProfile"/> - Using <see cref="SolaceTopicIdStrategy{T,S}"/> with a custom <see cref="ISolaceTopicProfile"/></item>
    /// </list>
    /// </summary>
    public static class HowToConfigureAvroSerializerArtifactResolution
    {
        /// <summary>
        /// Demonstrates how to configure an Avro serializer with explicit artifact coordinates.
        /// Explicit coordinates override the artifact reference resolver strategy and pin every
        /// serialization call to the specified artifact.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="AvroPropertyKeys.ExplicitArtifactArtifactId"/>,
        /// <see cref="AvroPropertyKeys.ExplicitArtifactGroupId"/>, and/or
        /// <see cref="AvroPropertyKeys.ExplicitArtifactVersion"/> overrides the artifact
        /// reference resolver strategy entirely. All serialization calls resolve to the artifact
        /// identified by the configured coordinates, regardless of the destination name.
        /// </remarks>
        public static void ConfigureWithExplicitArtifactCoordinates()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Set explicit artifact id — pins every serialization call to the artifact with this id in the default group
            config[AvroPropertyKeys.ExplicitArtifactArtifactId] = "my-artifact-id";

            // Optionally set explicit group id — pins every serialization call to the artifact in this group
            config[AvroPropertyKeys.ExplicitArtifactGroupId] = "my-group-id";

            // Optionally set explicit version — pins every serialization call to the exact artifact version specified
            // ExplicitArtifactVersion takes precedence over FindLatestArtifact when both are set
            config[AvroPropertyKeys.ExplicitArtifactVersion] = "1.0.0";

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured with explicit artifact coordinates.
                // All serialization calls will resolve to the artifact identified by the configured
                // coordinates, regardless of the destination name.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro serializer to resolve the most recently uploaded
        /// artifact version using the find-latest mode.
        /// </summary>
        /// <remarks>
        /// When <see cref="AvroPropertyKeys.FindLatestArtifact"/> is <c>true</c>, the
        /// serializer resolves the artifact version with the highest GlobalId (most recently uploaded),
        /// which is not necessarily the highest version string. If
        /// <see cref="AvroPropertyKeys.ExplicitArtifactVersion"/> is also set, it takes
        /// precedence over find-latest.
        /// </remarks>
        public static void ConfigureWithFindLatestArtifact()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Enable find-latest mode — resolves the most recently uploaded version (highest GlobalId),
            // not necessarily the highest version string.
            // Note: ExplicitArtifactVersion takes precedence if set.
            config[AvroPropertyKeys.FindLatestArtifact] = true;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured to always resolve the most recently
                // uploaded artifact version from the Schema Registry.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro serializer using <see cref="DestinationIdStrategy{T,S}"/>.
        /// The <see cref="DestinationIdStrategy{T,S}"/> uses the destination name from the record's
        /// metadata as the artifactId and uses the default groupId to build the artifact reference.
        /// </summary>
        public static void ConfigureWithDestinationIdStrategy()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Set the IArtifactReferenceResolverStrategy using a DestinationIdStrategy Type object.
            // Both open-generic (typeof(DestinationIdStrategy<,>)) and closed-generic
            // (typeof(DestinationIdStrategy<GenericRecord, Schema>)) forms are supported.
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor.
            config[AvroPropertyKeys.ArtifactResolverStrategy] = typeof(DestinationIdStrategy<,>);

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured with DestinationIdStrategy.
                // The destination name from each serialization call will be used as the artifact id
                // to locate the correct schema in the registry.
            }
        }

        /// <summary>
        /// Demonstrates how to configure an Avro serializer using <see cref="SolaceTopicIdStrategy{T,S}"/>
        /// with a custom <see cref="ISolaceTopicProfile"/>. The topic profile maps destination names to
        /// artifact references using ordered topic expression patterns.
        /// </summary>
        /// <remarks>
        /// The <see cref="SolaceTopicIdStrategy{T,S}"/> evaluates topic expressions in the order they
        /// were added to the profile; the first matching expression wins. Wildcard characters
        /// (<c>*</c> for single-level and <c>&gt;</c> for multi-level) are supported.
        /// </remarks>
        public static void ConfigureWithTopicProfile()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties
            config[AvroPropertyKeys.RegistryUrl] = "http://localhost:8081/apis/registry/v3";

            // Create a topic profile and add ordered topic-to-artifact mappings.
            // Entries are evaluated in order; the first matching expression wins.
            var profile = SolaceTopicProfile.Create();

            // Literal mapping: matches destination 'solace/samples/avro' exactly
            // The artifact reference uses the topic expression as the artifact id (default group)
            profile.Add(SolaceTopicArtifactMapping.Create("solace/samples/avro"));

            // Wildcard mapping: matches any destination starting with 'solace/'
            // Maps to artifact id 'my-artifact-id' in the default group
            profile.Add(SolaceTopicArtifactMapping.Create("solace/>", "my-artifact-id"));

            // Set the IArtifactReferenceResolverStrategy using a SolaceTopicIdStrategy Type object.
            // Both open-generic (typeof(SolaceTopicIdStrategy<,>)) and closed-generic
            // (typeof(SolaceTopicIdStrategy<GenericRecord, Schema>)) forms are supported.
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor.
            config[AvroPropertyKeys.ArtifactResolverStrategy] = typeof(SolaceTopicIdStrategy<,>);

            // Set the topic profile — must contain at least one mapping, otherwise serialization
            // will always fail with an ArgumentException
            config[AvroPropertyKeys.StrategyTopicProfile] = profile;

            // Create and configure Avro serializer
            using (var serializer = new AvroSerializer<GenericRecord>())
            {
                serializer.Configure(config);

                // At this point, the Avro serializer is configured with SolaceTopicIdStrategy and profile.
                // The topic profile mappings will be used to resolve artifact references during serialization.
                //
                // Example outcomes for given destination names:
                // - 'solace/samples/avro'  → matches literal expression → artifact id: 'solace/samples/avro'
                // - 'solace/other/topic'   → matches wildcard 'solace/>' → artifact id: 'my-artifact-id'
                // - 'other/topic'          → no match → throws ArgumentException
            }
        }
    }
}
