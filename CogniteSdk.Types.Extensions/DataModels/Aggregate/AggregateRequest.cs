// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using CogniteSdk.Types.DataModels.Search;

namespace CogniteSdk.Types.DataModels.Aggregate;

/// <summary>
/// Type of aggregation operation.
/// </summary>
public enum AggregateType
{
    /// <summary>Count of items.</summary>
    Count,
    /// <summary>Sum of numeric values.</summary>
    Sum,
    /// <summary>Average of numeric values.</summary>
    Avg,
    /// <summary>Minimum value.</summary>
    Min,
    /// <summary>Maximum value.</summary>
    Max,
    /// <summary>Histogram distribution.</summary>
    Histogram
}

/// <summary>
/// Request body for aggregating Data Model instances.
/// </summary>
public class AggregateInstancesRequest
{
    /// <summary>
    /// The view to aggregate within. Required.
    /// </summary>
    [JsonPropertyName("view")]
    public SearchViewIdentifier View { get; set; } = null!;

    /// <summary>
    /// Aggregation operations to perform. Maximum 5 per request.
    /// </summary>
    [JsonPropertyName("aggregates")]
    public IReadOnlyList<AggregateOperation> Aggregates { get; set; } = Array.Empty<AggregateOperation>();

    /// <summary>
    /// Properties to group results by.
    /// </summary>
    [JsonPropertyName("groupBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? GroupBy { get; set; }

    /// <summary>
    /// Full-text search query to filter instances before aggregation.
    /// </summary>
    [JsonPropertyName("query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; set; }

    /// <summary>
    /// Properties to search within for the query.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Properties { get; set; }

    /// <summary>
    /// Filter to apply before aggregation.
    /// Use FilterBuilder or anonymous object.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Filter { get; set; }

    /// <summary>
    /// Instance type: "node" or "edge".
    /// </summary>
    [JsonPropertyName("instanceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceType { get; set; }

    /// <summary>
    /// Maximum number of groups to return. Default 25, max 10000.
    /// </summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Limit { get; set; } = 25;

    /// <summary>
    /// Target units for unit conversion.
    /// </summary>
    [JsonPropertyName("targetUnits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TargetUnit>? TargetUnits { get; set; }
}

/// <summary>
/// An aggregation operation to perform.
/// </summary>
public class AggregateOperation
{
    /// <summary>
    /// The property to aggregate.
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = null!;

    /// <summary>
    /// Type of aggregation. Use the AggregateType enum or string values:
    /// "count", "sum", "avg", "min", "max".
    /// </summary>
    [JsonPropertyName("aggregate")]
    public string Aggregate { get; set; } = null!;

    /// <summary>
    /// For histogram aggregations: the interval size.
    /// </summary>
    [JsonPropertyName("interval")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Interval { get; set; }
}
