// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.DataModels
{
    /// <summary>
    /// Request body for GraphQL queries against CDF Data Models.
    /// </summary>
    public class GraphQLRequest
    {
        /// <summary>
        /// The GraphQL query string.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional variables to pass to the query.
        /// </summary>
        [JsonPropertyName("variables")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object> Variables { get; set; }

        /// <summary>
        /// Optional operation name when the query contains multiple operations.
        /// </summary>
        [JsonPropertyName("operationName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string OperationName { get; set; }
    }

    /// <summary>
    /// Response from a GraphQL query.
    /// </summary>
    /// <typeparam name="T">Type of the data payload.</typeparam>
    public class GraphQLResponse<T>
    {
        /// <summary>
        /// The data returned by the query.
        /// </summary>
        [JsonPropertyName("data")]
        public T Data { get; set; }

        /// <summary>
        /// Errors that occurred during query execution.
        /// </summary>
        [JsonPropertyName("errors")]
        public IEnumerable<GraphQLError> Errors { get; set; }

        /// <summary>
        /// Optional extensions returned by the server.
        /// </summary>
        [JsonPropertyName("extensions")]
        public JsonElement? Extensions { get; set; }

        /// <summary>
        /// Returns true if the response contains errors.
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => Errors != null && System.Linq.Enumerable.Any(Errors);
    }

    /// <summary>
    /// Response from a GraphQL query returning raw JSON data.
    /// </summary>
    public class GraphQLRawResponse
    {
        /// <summary>
        /// The raw JSON data returned by the query.
        /// </summary>
        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        /// <summary>
        /// Errors that occurred during query execution.
        /// </summary>
        [JsonPropertyName("errors")]
        public IEnumerable<GraphQLError> Errors { get; set; }

        /// <summary>
        /// Optional extensions returned by the server.
        /// </summary>
        [JsonPropertyName("extensions")]
        public JsonElement? Extensions { get; set; }

        /// <summary>
        /// Returns true if the response contains errors.
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => Errors != null && System.Linq.Enumerable.Any(Errors);
    }

    /// <summary>
    /// A GraphQL error.
    /// </summary>
    public class GraphQLError
    {
        /// <summary>
        /// The error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The locations in the query where the error occurred.
        /// </summary>
        [JsonPropertyName("locations")]
        public IEnumerable<GraphQLErrorLocation> Locations { get; set; }

        /// <summary>
        /// The path to the field that caused the error.
        /// </summary>
        [JsonPropertyName("path")]
        public IEnumerable<object> Path { get; set; }

        /// <summary>
        /// Additional error extensions.
        /// </summary>
        [JsonPropertyName("extensions")]
        public JsonElement? Extensions { get; set; }
    }

    /// <summary>
    /// Location of a GraphQL error in the query.
    /// </summary>
    public class GraphQLErrorLocation
    {
        /// <summary>
        /// Line number (1-indexed).
        /// </summary>
        [JsonPropertyName("line")]
        public int Line { get; set; }

        /// <summary>
        /// Column number (1-indexed).
        /// </summary>
        [JsonPropertyName("column")]
        public int Column { get; set; }
    }
}
