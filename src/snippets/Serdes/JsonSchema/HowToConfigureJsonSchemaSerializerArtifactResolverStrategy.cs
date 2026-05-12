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
using System.Text.Json.Nodes;
using Solace.SchemaRegistry.Serdes.Core.Resolver;
using Solace.SchemaRegistry.Serdes.Core.Resolver.Data;
using Solace.SchemaRegistry.Serdes.Core.Resolver.Strategy;
using Solace.SchemaRegistry.Serdes.JsonSchema;

namespace Snippets.Serdes.JsonSchema
{
    /// <summary>
    /// Provides code snippets demonstrating how to configure JSON Schema serializers
    /// with different ArtifactReferenceResolverStrategy implementations.
    /// This class includes scenarios for:
    /// <list type="bullet">
    ///   <item>SerializeWithDestinationIdStrategy - Configuring DestinationIdStrategy using a Type object</item>
    ///   <item>SerializeWithSolaceTopicIdStrategyWithoutProfile - Configuring SolaceTopicIdStrategy without a topic profile</item>
    ///   <item>CreateSolaceTopicArtifactMappingWithTopicExpressionOnly - Creating mappings using only a topic expression</item>
    ///   <item>CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactId - Creating mappings with a topic expression and artifact id</item>
    ///   <item>CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactReference - Creating mappings with a topic expression and artifact reference</item>
    ///   <item>ModifySolaceTopicProfileWithClear - Clearing all mappings from a profile</item>
    ///   <item>ModifySolaceTopicProfileWithAdd - Adding a mapping to a profile</item>
    ///   <item>ModifySolaceTopicProfileWithRemoveByIndex - Removing a mapping by index</item>
    ///   <item>ModifySolaceTopicProfileWithRemoveTopicExpression - Removing a mapping by topic expression</item>
    ///   <item>ModifySolaceTopicProfileWithRemoveMapping - Removing a specific mapping object</item>
    ///   <item>SerializeWithSolaceTopicIdStrategyWithProfile - Configuring SolaceTopicIdStrategy with a topic profile</item>
    ///   <item>SerializeWithExplicitArtifactCoordinates - Pinning artifact coordinates explicitly in configuration</item>
    ///   <item>SerializeWithCustomArtifactResolverStrategy - Configuring a custom strategy using a Type object</item>
    /// </list>
    /// </summary>
    public static class HowToConfigureJsonSchemaSerializerArtifactResolverStrategy
    {
        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer using <see cref="DestinationIdStrategy{T,S}"/> Type object.
        /// The <see cref="DestinationIdStrategy{T,S}"/> uses the destination name from the record's metadata as the artifactId
        /// and default as the groupId to build the <see cref="IArtifactReference"/>.
        /// </summary>
        public static void SerializeWithDestinationIdStrategy()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Set the IArtifactReferenceResolverStrategy using DestinationIdStrategy closed generic Type object
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor
            config[JsonSchemaPropertyKeys.ArtifactResolverStrategy] = typeof(DestinationIdStrategy<JsonNode, NJsonSchema.JsonSchema>);

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                // The DestinationIdStrategy will be used to determine the specific ArtifactReference for a given data record,
                // enabling the system to locate the correct schema in the registry.
            }
        }

        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer using <see cref="SolaceTopicIdStrategy{T,S}"/> Type object
        /// without a topic profile. The <see cref="SolaceTopicIdStrategy{T,S}"/> maps the destination name to the
        /// <see cref="IArtifactReference"/> setting the artifact id as the destination string.
        /// </summary>
        public static void SerializeWithSolaceTopicIdStrategyWithoutProfile()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Set the IArtifactReferenceResolverStrategy using SolaceTopicIdStrategy Type object
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor
            config[JsonSchemaPropertyKeys.ArtifactResolverStrategy] = typeof(SolaceTopicIdStrategy<,>);

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                // The SolaceTopicIdStrategy will be used to determine the specific ArtifactReference for a given data record,
                // enabling the system to locate the correct schema in the registry.
            }
        }

        /// <summary>
        /// Demonstrates how to create a <see cref="SolaceTopicArtifactMapping"/> with only a topic expression to be used to
        /// create a <see cref="SolaceTopicProfile"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to add mappings to.</param>
        public static void CreateSolaceTopicArtifactMappingWithTopicExpressionOnly(SolaceTopicProfile profile)
        {
            // create mapping with literal expression
            ISolaceTopicArtifactMapping mapping1 = SolaceTopicArtifactMapping.Create("solace/samples/jsonschema");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping1.ArtifactReference.GroupId
            //      artifact id: 'solace/samples/jsonschema', accessible with mapping1.ArtifactReference.ArtifactId

            // create mapping with single level wildcard '*'
            ISolaceTopicArtifactMapping mapping2 = SolaceTopicArtifactMapping.Create("solace/*/sample/jsonschema");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping2.ArtifactReference.GroupId
            //      artifact id: 'solace/*/sample/jsonschema', accessible with mapping2.ArtifactReference.ArtifactId

            // create mapping with multi level wildcard '>'
            ISolaceTopicArtifactMapping mapping3 = SolaceTopicArtifactMapping.Create("solace/>");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping3.ArtifactReference.GroupId
            //      artifact id: 'solace/>', accessible with mapping3.ArtifactReference.ArtifactId

            // See, https://docs.solace.com/Messaging/Wildcard-Charaters-Topic-Subs.htm for more details on wildcard rules.

            // At this point the profile can add the mappings using: profile.Add(...)
            profile.Add(mapping1);
            profile.Add(mapping2);
            profile.Add(mapping3);
        }

        /// <summary>
        /// Demonstrates how to create a <see cref="SolaceTopicArtifactMapping"/> with a topic expression and artifact id to be
        /// used to create a <see cref="SolaceTopicProfile"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to add mappings to.</param>
        public static void CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactId(SolaceTopicProfile profile)
        {
            // create mapping with literal expression and artifact id
            ISolaceTopicArtifactMapping mapping1 = SolaceTopicArtifactMapping.Create("solace/samples/jsonschema", "User");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping1.ArtifactReference.GroupId
            //      artifact id: 'User', accessible with mapping1.ArtifactReference.ArtifactId

            // create mapping with single level wildcard '*' and artifact id
            ISolaceTopicArtifactMapping mapping2 = SolaceTopicArtifactMapping.Create("solace/*/sample/jsonschema", "NewUser");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping2.ArtifactReference.GroupId
            //      artifact id: 'NewUser', accessible with mapping2.ArtifactReference.ArtifactId

            // create mapping with multi level wildcard '>' and artifact id
            ISolaceTopicArtifactMapping mapping3 = SolaceTopicArtifactMapping.Create("solace/>", "OldUser");
            // this generates an ArtifactReference with:
            //      group id: 'default', accessible with mapping3.ArtifactReference.GroupId
            //      artifact id: 'OldUser', accessible with mapping3.ArtifactReference.ArtifactId

            // See, https://docs.solace.com/Messaging/Wildcard-Charaters-Topic-Subs.htm for more details on wildcard rules.

            // At this point the profile can add the mappings using: profile.Add(...)
            profile.Add(mapping1);
            profile.Add(mapping2);
            profile.Add(mapping3);
        }

        /// <summary>
        /// Demonstrates how to create a <see cref="SolaceTopicArtifactMapping"/> with a topic expression and
        /// <see cref="IArtifactReference"/> for advanced mapping scenarios to be used to create a <see cref="SolaceTopicProfile"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to add mappings to.</param>
        public static void CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactReference(SolaceTopicProfile profile)
        {
            // create artifact reference for mapping
            IArtifactReference reference1 = new ArtifactReference(
                groupId: "com.solace.samples.serdes.jsonschema.schema",
                artifactId: "User");
            // create mapping with literal expression and artifact reference
            ISolaceTopicArtifactMapping mapping1 = SolaceTopicArtifactMapping.Create("solace/samples/jsonschema", reference1);
            // this generates a mapping with ArtifactReference:
            //      group id: 'com.solace.samples.serdes.jsonschema.schema', accessible with mapping1.ArtifactReference.GroupId
            //      artifact id: 'User', accessible with mapping1.ArtifactReference.ArtifactId

            // create new artifact reference for mapping
            IArtifactReference reference2 = new ArtifactReference(
                groupId: "com.solace.samples.serdes.jsonschema.schema",
                artifactId: "NewUser");
            // create mapping with single level wildcard '*' and artifact reference
            ISolaceTopicArtifactMapping mapping2 = SolaceTopicArtifactMapping.Create("solace/*/sample/jsonschema", reference2);
            // this generates a mapping with ArtifactReference:
            //      group id: 'com.solace.samples.serdes.jsonschema.schema', accessible with mapping2.ArtifactReference.GroupId
            //      artifact id: 'NewUser', accessible with mapping2.ArtifactReference.ArtifactId

            // create new artifact reference for mapping with an explicit version
            IArtifactReference reference3 = new ArtifactReference(
                groupId: "com.solace.samples.serdes.jsonschema.schema",
                artifactId: "User",
                version: "0.0.1");
            // create mapping with multi level wildcard '>' and artifact reference
            ISolaceTopicArtifactMapping mapping3 = SolaceTopicArtifactMapping.Create("solace/>", reference3);
            // this generates a mapping with ArtifactReference:
            //      group id: 'com.solace.samples.serdes.jsonschema.schema', accessible with mapping3.ArtifactReference.GroupId
            //      artifact id: 'User', accessible with mapping3.ArtifactReference.ArtifactId
            //      version: '0.0.1', accessible with mapping3.ArtifactReference.Version

            // See, https://docs.solace.com/Messaging/Wildcard-Charaters-Topic-Subs.htm for more details on wildcard rules.

            // At this point the profile can add the mappings using: profile.Add(...)
            profile.Add(mapping1);
            profile.Add(mapping2);
            profile.Add(mapping3);
        }

        /// <summary>
        /// Demonstrates how to modify a <see cref="SolaceTopicProfile"/> with <see cref="SolaceTopicProfile.Clear"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to modify with one or more mappings already.</param>
        public static void ModifySolaceTopicProfileWithClear(SolaceTopicProfile profile)
        {
            profile.Clear(); // clears the mapping list returned by profile.Mappings
            // profile.Mappings.Count should now be == 0
        }

        /// <summary>
        /// Demonstrates how to modify a <see cref="SolaceTopicProfile"/> with <see cref="SolaceTopicProfile.Add"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to modify with 0 or more mappings already.</param>
        /// <param name="mappingToAdd">The <see cref="ISolaceTopicArtifactMapping"/> to add to the profile.</param>
        public static void ModifySolaceTopicProfileWithAdd(SolaceTopicProfile profile, ISolaceTopicArtifactMapping mappingToAdd)
        {
            profile.Add(mappingToAdd); // appends mapping to the end of the profile.Mappings list
            // now profile.Mappings[profile.Mappings.Count - 1] should equal mappingToAdd
        }

        /// <summary>
        /// Demonstrates how to modify a <see cref="SolaceTopicProfile"/> with <see cref="SolaceTopicProfile.Remove(int)"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to modify with 1 or more mappings already.</param>
        /// <param name="mappingIndexToRemove">The index in the profile for removal.</param>
        public static void ModifySolaceTopicProfileWithRemoveByIndex(SolaceTopicProfile profile, int mappingIndexToRemove)
        {
            // removes mapping at the list index from profile.Mappings
            // the return value is the mapping object removed, or ArgumentException is thrown for an invalid index
            ISolaceTopicArtifactMapping removedMapping = profile.Remove(mappingIndexToRemove);
        }

        /// <summary>
        /// Demonstrates how to modify a <see cref="SolaceTopicProfile"/> with <see cref="SolaceTopicProfile.Remove(string)"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to modify with 1 or more mappings already.</param>
        /// <param name="topicExpression">The string expression for the mapping in the profile for removal.</param>
        public static void ModifySolaceTopicProfileWithRemoveTopicExpression(SolaceTopicProfile profile, string topicExpression)
        {
            // removes the first mapping to match the topic expression in the list from profile.Mappings
            // the return value is the mapping object removed, or null if there are no matching mappings
            // with the given topic expression
            ISolaceTopicArtifactMapping removedMapping = profile.Remove(topicExpression);
        }

        /// <summary>
        /// Demonstrates how to modify a <see cref="SolaceTopicProfile"/> with
        /// <see cref="SolaceTopicProfile.Remove(ISolaceTopicArtifactMapping)"/>.
        /// </summary>
        /// <param name="profile">The <see cref="SolaceTopicProfile"/> to modify with 1 or more mappings already.</param>
        /// <param name="mappingToRemove">The <see cref="ISolaceTopicArtifactMapping"/> in the profile for removal.</param>
        public static void ModifySolaceTopicProfileWithRemoveMapping(SolaceTopicProfile profile, ISolaceTopicArtifactMapping mappingToRemove)
        {
            // removes the first mapping to equal the mappingToRemove object in the list from profile.Mappings
            // the return value is the mapping object removed, or null if there are no matching mappings
            // equal to the mappingToRemove object
            ISolaceTopicArtifactMapping removedMapping = profile.Remove(mappingToRemove);
        }

        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer using <see cref="SolaceTopicIdStrategy{T,S}"/> Type object
        /// with a <see cref="SolaceTopicProfile"/>. The <see cref="SolaceTopicIdStrategy{T,S}"/> maps the destination name
        /// to the <see cref="IArtifactReference"/> using the configured topic mappings.
        /// <para>
        /// For more profile configuration options see,
        /// <list type="bullet">
        ///   <item><see cref="CreateSolaceTopicArtifactMappingWithTopicExpressionOnly"/></item>
        ///   <item><see cref="CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactId"/></item>
        ///   <item><see cref="CreateSolaceTopicArtifactMappingWithTopicExpressionAndArtifactReference"/></item>
        ///   <item><see cref="ModifySolaceTopicProfileWithAdd"/></item>
        /// </list>
        /// </para>
        /// </summary>
        public static void SerializeWithSolaceTopicIdStrategyWithProfile()
        {
            // create Topic profile for mapping of solace smart topics
            SolaceTopicProfile profile = SolaceTopicProfile.Create(); // creates empty profile

            // entries for mappings are ordered and the matching mapping is selected by the SolaceTopicIdStrategy

            // create Mapping to add to the profile
            // mappings require a topic expression to match on the given destination name from serialize
            // topic expressions can be literal expressions without wildcards

            // create mapping using only topic expression which generates an artifact reference
            // with the topic expression string as the artifact id
            profile.Add(SolaceTopicArtifactMapping.Create("solace/samples/jsonschema"));
            // as a literal mapping this topic expression will only match the destination name 'solace/samples/jsonschema'

            // topic expressions can also use wildcards like '*' and '>'
            // See, https://docs.solace.com/Messaging/Wildcard-Charaters-Topic-Subs.htm for wildcard rules.
            // create another mapping with wildcard '>' which maps to custom artifact id 'User'
            profile.Add(SolaceTopicArtifactMapping.Create("solace/>", "User"));
            // as a wildcard mapping this topic expression will match many destination names,
            // for example 'solace/other', 'solace/a', 'solace/a/b', etc

            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Set the IArtifactReferenceResolverStrategy using SolaceTopicIdStrategy Type object
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor
            config[JsonSchemaPropertyKeys.ArtifactResolverStrategy] = typeof(SolaceTopicIdStrategy<,>);
            // Set the topic Profile using the configured profile
            // the profile should contain at least one mapping, otherwise serialization will always fail with an ArgumentException
            config[JsonSchemaPropertyKeys.StrategyTopicProfile] = profile;

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                // The SolaceTopicIdStrategy will be used to determine the specific ArtifactReference for a given data record,
                // enabling the system to locate the correct schema in the registry.

                // for an example serialize call: await serializer.SerializeAsync(destinationName, data, headers);
                // Given the following destination names the following expected coordinate will be selected:
                // - destination name 'solace/samples/jsonschema' matches topic expression: 'solace/samples/jsonschema'
                //   and selects, group id: 'default', artifact id: 'solace/samples/jsonschema'
                // - destination name 'solace/other/sample' matches topic expression: 'solace/>'
                //   and selects, group id: 'default', artifact id: 'User'
                // - destination name 'pubsub/samples' has no matching topic expression,
                //   and cannot select an artifact reference, throwing an ArgumentException

                // Note: order matters for topic profiles as 'solace/>' can also match 'solace/samples/jsonschema', however
                //       there is a match before the wildcard mapping which takes priority instead.
            }
        }

        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer with explicit artifact coordinates.
        /// Explicit coordinates override the artifact reference resolved by the configured
        /// <see cref="IArtifactReferenceResolverStrategy{T,S}"/> and pin every serialization call
        /// to the specified artifact.
        /// </summary>
        public static void SerializeWithExplicitArtifactCoordinates()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Example 1: Set explicit artifact id only
            // This pins every serialization request to the artifact with this id in the default group
            // config[JsonSchemaPropertyKeys.ExplicitArtifactArtifactId] = "User";

            // Example 2: Set explicit artifact id and group id
            // This pins every serialization request to the artifact with this id in the specified group
            // config[JsonSchemaPropertyKeys.ExplicitArtifactArtifactId] = "User";
            // config[JsonSchemaPropertyKeys.ExplicitArtifactGroupId] = "com.solace.samples.serdes.jsonschema.schema";

            // Example 3: Set explicit artifact id, group id, and version
            // This pins every serialization request to the exact artifact version specified
            config[JsonSchemaPropertyKeys.ExplicitArtifactArtifactId] = "User";
            config[JsonSchemaPropertyKeys.ExplicitArtifactGroupId] = "com.solace.samples.serdes.jsonschema.schema";
            config[JsonSchemaPropertyKeys.ExplicitArtifactVersion] = "1.0.0";

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured with explicit artifact coordinates.
                // All serialization calls will resolve to the artifact identified by the configured coordinates,
                // regardless of the destination name.
            }
        }

        /// <summary>
        /// Demonstrates how to configure a JSON Schema serializer with a custom
        /// <see cref="IArtifactReferenceResolverStrategy{T,S}"/> using a Type object.
        /// </summary>
        public static void SerializeWithCustomArtifactResolverStrategy()
        {
            // Create configuration dictionary
            var config = new Dictionary<string, object>();

            // Set required Schema Registry connection properties

            // Set the custom IArtifactReferenceResolverStrategy using a closed generic Type object
            // NOTE: Custom strategies must use a closed generic type (e.g., typeof(MyStrategy<JsonNode, NJsonSchema.JsonSchema>)).
            // Open generic types (e.g., typeof(MyStrategy<,>)) are not supported for custom strategies and will throw.
            // NOTE: The IArtifactReferenceResolverStrategy must have a parameterless constructor
            config[JsonSchemaPropertyKeys.ArtifactResolverStrategy] = typeof(CustomArtifactReferenceResolverStrategy<JsonNode, NJsonSchema.JsonSchema>);

            // Create and configure JSON Schema serializer
            using (var serializer = new JsonSchemaSerializer<JsonNode>())
            {
                serializer.Configure(config);

                // At this point, the JSON Schema serializer is configured and ready to use for serialization.
                // The custom IArtifactReferenceResolverStrategy will be used to determine the specific
                // ArtifactReference for a given data record, enabling the system to locate the correct schema in the registry.
            }
        }

        /// <summary>
        /// A custom implementation of <see cref="IArtifactReferenceResolverStrategy{T,S}"/> that uses the destination name
        /// from the record's metadata as the artifactId.
        /// </summary>
        private class CustomArtifactReferenceResolverStrategy<T, S> : IArtifactReferenceResolverStrategy<T, S>
        {
            /// <summary>
            /// Gets a value indicating whether this strategy requires schema loading during resolution.
            /// </summary>
            /// <value>
            /// Always returns <c>false</c> because this strategy derives artifact references solely from
            /// the record's metadata and does not need schema content.
            /// </value>
            public bool LoadSchema => false;

            /// <summary>
            /// Resolves an artifact reference using the destination name from the record's metadata.
            /// </summary>
            /// <param name="data">The data record containing the metadata with destination name.</param>
            /// <param name="schema">The parsed schema (not used by this strategy).</param>
            /// <returns>
            /// An <see cref="IArtifactReference"/> with null groupId and the destination name as artifactId.
            /// </returns>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> or its metadata is null.</exception>
            public IArtifactReference ResolveArtifactReference(IRecord<T> data, IParsedSchema<S> schema)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                var metadata = data.Metadata;
                if (metadata == null)
                {
                    throw new ArgumentNullException(nameof(data), "Record metadata is null");
                }

                return new ArtifactReference(groupId: null, artifactId: metadata.DestinationName);
            }
        }
    }
}
