// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.GraphQL;

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
    public Dictionary<string, object?>? Variables { get; set; }

    /// <summary>
    /// Optional operation name when the query contains multiple operations.
    /// </summary>
    [JsonPropertyName("operationName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OperationName { get; set; }
}
