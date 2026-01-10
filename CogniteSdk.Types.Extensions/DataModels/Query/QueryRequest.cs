// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
namespace CogniteSdk.Types.DataModels.Query;

/// <summary>
/// Extended query request with fluent building capabilities.
/// Extends the base SDK Query type with additional functionality.
/// </summary>
public class QueryInstancesRequest
{
    /// <summary>
    /// Named result set expressions defining what to query.
    /// </summary>
    [JsonPropertyName("with")]
    public Dictionary<string, QueryResultSetExpression> With { get; set; } = new();

    /// <summary>
    /// Selection of properties to return for each result set.
    /// </summary>
    [JsonPropertyName("select")]
    public Dictionary<string, QuerySelectExpression> Select { get; set; } = new();

    /// <summary>
    /// Cursors from a previous query for pagination.
    /// </summary>
    [JsonPropertyName("cursors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Cursors { get; set; }

    /// <summary>
    /// Parameters for parameterized queries. Values are substituted into filters
    /// that reference them via <c>{ "parameter": "paramName" }</c>.
    /// </summary>
    /// <remarks>
    /// Parameterized queries enable query plan reuse across queries with different
    /// parameter values, improving performance for read-heavy workloads.
    /// </remarks>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Parameters { get; set; }
}

/// <summary>
/// Expression defining a result set in a query.
/// </summary>
public class QueryResultSetExpression
{
    /// <summary>
    /// Node query specification.
    /// </summary>
    [JsonPropertyName("nodes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QueryNodesExpression? Nodes { get; set; }

    /// <summary>
    /// Edge query specification.
    /// </summary>
    [JsonPropertyName("edges")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QueryEdgesExpression? Edges { get; set; }

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Limit { get; set; } = 1000;
}

/// <summary>
/// Nodes expression in a query.
/// </summary>
public class QueryNodesExpression
{
    /// <summary>
    /// Filter to apply to nodes (use FilterBuilder or anonymous object).
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Filter { get; set; }

    /// <summary>
    /// Reference to another result set to traverse from.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// Chain of result set expressions when traversing.
    /// </summary>
    [JsonPropertyName("chainTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChainTo { get; set; }

    /// <summary>
    /// Direction for traversal.
    /// </summary>
    [JsonPropertyName("direction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; set; }
}

/// <summary>
/// Edges expression in a query.
/// </summary>
public class QueryEdgesExpression
{
    /// <summary>
    /// Filter to apply to edges (use FilterBuilder or anonymous object).
    /// Typically filters on <c>["edge", "type"]</c> property.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Filter { get; set; }

    /// <summary>
    /// Reference to a result set to use as start nodes.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// Maximum traversal depth (number of hops). Default is unlimited.
    /// Setting to 1 can improve performance when only direct connections are needed.
    /// </summary>
    [JsonPropertyName("maxDistance")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxDistance { get; set; }

    /// <summary>
    /// Direction for edge traversal: "outwards" (default) or "inwards".
    /// </summary>
    [JsonPropertyName("direction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; set; }

    /// <summary>
    /// Filter that nodes on the "other" side of the edge must match.
    /// For outwards traversal: end node must match.
    /// For inwards traversal: start node must match.
    /// </summary>
    [JsonPropertyName("nodeFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? NodeFilter { get; set; }

    /// <summary>
    /// Filter to stop traversal. Nodes matching this filter won't be traversed beyond.
    /// Node must also match <see cref="NodeFilter"/> (if any) to reach the termination point.
    /// </summary>
    [JsonPropertyName("terminationFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? TerminationFilter { get; set; }

    /// <summary>
    /// Limit the number of returned edges for each source node.
    /// Only valid when <see cref="MaxDistance"/> is 1 and <see cref="From"/> is specified.
    /// </summary>
    [JsonPropertyName("limitEach")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LimitEach { get; set; }

    /// <summary>
    /// Which side of the edges in <see cref="From"/> to chain to when <see cref="From"/>
    /// is an edge result expression: "source" or "destination".
    /// </summary>
    [JsonPropertyName("chainTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChainTo { get; set; }
}

/// <summary>
/// Selection expression for a result set.
/// </summary>
public class QuerySelectExpression
{
    /// <summary>
    /// Sources to select properties from.
    /// </summary>
    [JsonPropertyName("sources")]
    public IReadOnlyList<QuerySourceSelection> Sources { get; set; } = Array.Empty<QuerySourceSelection>();
}

/// <summary>
/// Selection of properties from a specific source.
/// </summary>
public class QuerySourceSelection
{
    /// <summary>
    /// The source view.
    /// </summary>
    [JsonPropertyName("source")]
    public object Source { get; set; } = null!;

    /// <summary>
    /// Properties to select. Empty means all properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public IReadOnlyList<string> Properties { get; set; } = Array.Empty<string>();
}
