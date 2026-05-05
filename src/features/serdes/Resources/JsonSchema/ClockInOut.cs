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
    /// Represents a clock in or out event for an employee.
    /// </summary>
    public class ClockInOut
    {
        /// <summary>
        /// Gets or sets the region code for clock in or out.
        /// </summary>
        [JsonPropertyName("region_code")]
        public string RegionCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        [JsonPropertyName("store_id")]
        public string StoreId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the employee ID for who clocked in or out.
        /// </summary>
        [JsonPropertyName("employee_id")]
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the clock time.
        /// </summary>
        [JsonPropertyName("datetime")]
        public string Datetime { get; set; } = string.Empty;
    }
}
