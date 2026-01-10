// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.GraphQL;

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
    public T? Data { get; set; }

    /// <summary>
    /// Errors that occurred during query execution.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<GraphQLError>? Errors { get; set; }

    /// <summary>
    /// Optional extensions returned by the server.
    /// </summary>
    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    /// <summary>
    /// Returns true if the response contains errors.
    /// </summary>
    [JsonIgnore]
    public bool HasErrors => Errors?.Count > 0;
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
    public IReadOnlyList<GraphQLError>? Errors { get; set; }

    /// <summary>
    /// Optional extensions returned by the server.
    /// </summary>
    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }

    /// <summary>
    /// Returns true if the response contains errors.
    /// </summary>
    [JsonIgnore]
    public bool HasErrors => Errors?.Count > 0;
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
    public IReadOnlyList<GraphQLErrorLocation>? Locations { get; set; }

    /// <summary>
    /// The path to the field that caused the error.
    /// </summary>
    [JsonPropertyName("path")]
    public IReadOnlyList<object>? Path { get; set; }

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
