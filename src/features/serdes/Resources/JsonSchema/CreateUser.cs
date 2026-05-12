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

using System.Text.Json.Serialization;

namespace Resources.JsonSchema
{
    /// <summary>
    /// Represents a request to create a new user.
    /// </summary>
    public class CreateUser
    {
        /// <summary>
        /// Gets or sets the user's name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of the CreateUser object.
        /// </summary>
        /// <returns>A string containing the name and email</returns>
        public override string ToString()
        {
            return $"CreateUser{{name='{Name}', email='{Email}'}}";
        }
    }
}
