// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.Aggregate;

/// <summary>
/// Response from an aggregation request.
/// </summary>
public class AggregateInstancesResponse
{
    /// <summary>
    /// Aggregation result items, one per group (or single item if no groupBy).
    /// </summary>
    public IReadOnlyList<AggregateResultItem> Items { get; set; } = Array.Empty<AggregateResultItem>();
}

/// <summary>
/// A single aggregation result (representing one group or overall totals).
/// </summary>
public class AggregateResultItem
{
    /// <summary>
    /// Group key values, if groupBy was specified.
    /// Keys are property names, values are the grouped values.
    /// </summary>
    public Dictionary<string, JsonElement> Group { get; set; } = new();

    /// <summary>
    /// Aggregation results keyed by property name.
    /// </summary>
    public Dictionary<string, AggregateValue> Aggregates { get; set; } = new();
}

/// <summary>
/// Aggregated value for a property.
/// </summary>
public class AggregateValue
{
    /// <summary>
    /// Count of items (for count aggregation).
    /// </summary>
    public long? Count { get; set; }

    /// <summary>
    /// Sum of values (for sum aggregation).
    /// </summary>
    public double? Sum { get; set; }

    /// <summary>
    /// Average of values (for avg aggregation).
    /// </summary>
    public double? Avg { get; set; }

    /// <summary>
    /// Minimum value (for min aggregation).
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Maximum value (for max aggregation).
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// Histogram buckets (for histogram aggregation).
    /// </summary>
    public IReadOnlyList<HistogramBucket>? Histogram { get; set; }
}

/// <summary>
/// A histogram bucket.
/// </summary>
public class HistogramBucket
{
    /// <summary>
    /// Start value of the bucket (inclusive).
    /// </summary>
    [JsonPropertyName("start")]
    public double Start { get; set; }

    /// <summary>
    /// Count of items in this bucket.
    /// </summary>
    [JsonPropertyName("count")]
    public long Count { get; set; }
}
