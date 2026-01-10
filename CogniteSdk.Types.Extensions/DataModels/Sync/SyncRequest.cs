// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
namespace CogniteSdk.Types.DataModels.Sync;

/// <summary>
/// Sync mode controlling the backfill phase behavior.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Don't distinguish between the backfill phase and subsequent changes.
    /// Use this mode if syncing without a filter or with a single space filter.
    /// This is the default mode.
    /// </summary>
    OnePhase,

    /// <summary>
    /// Split the sync into two stages. This allows the system to take advantage
    /// of an index (default or custom cursorable) in the backfill stage.
    /// Use this when syncing with a hasData filter or other indexed filters.
    /// </summary>
    TwoPhase,

    /// <summary>
    /// Skip the backfill stage and yield only instances that have changed
    /// since the sync started. Useful when you only care about new changes.
    /// </summary>
    NoBackfill
}

/// <summary>
/// Sort specification for backfill phase in two-phase sync.
/// Must match a cursorable index for optimal performance.
/// </summary>
public class SyncBackfillSort
{
    /// <summary>
    /// Property path to sort by, e.g., ["space", "container/version", "property"].
    /// </summary>
    [JsonPropertyName("property")]
    public string[] Property { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Sort direction: "ascending" or "descending".
    /// </summary>
    [JsonPropertyName("direction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; set; }

    /// <summary>
    /// Whether nulls should come first in sort order.
    /// For ascending sorts: set to false (nulls last).
    /// For descending sorts: set to true (nulls first).
    /// </summary>
    [JsonPropertyName("nullsFirst")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NullsFirst { get; set; }
}

/// <summary>
/// Request body for syncing Data Model instances.
/// </summary>
public class SyncInstancesRequest
{
    /// <summary>
    /// Named result set expressions defining what to sync.
    /// </summary>
    [JsonPropertyName("with")]
    public Dictionary<string, SyncResultSetExpression> With { get; set; } = new();

    /// <summary>
    /// Selection of properties to return for each result set.
    /// </summary>
    [JsonPropertyName("select")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, SelectExpression>? Select { get; set; }

    /// <summary>
    /// Cursors from a previous sync response for each result set.
    /// </summary>
    [JsonPropertyName("cursors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Cursors { get; set; }

    /// <summary>
    /// Sync mode controlling the backfill phase.
    /// Default is <see cref="SyncMode.OnePhase"/>.
    /// </summary>
    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mode { get; set; }

    /// <summary>
    /// Sort specification for backfill phase when using <see cref="SyncMode.TwoPhase"/>.
    /// Must match a cursorable index for optimal performance.
    /// </summary>
    [JsonPropertyName("backfillSort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SyncBackfillSort>? BackfillSort { get; set; }

    /// <summary>
    /// When true, allows use of expired cursors (older than 3 days).
    /// Warning: Using expired cursors may miss soft-deleted instances.
    /// </summary>
    [JsonPropertyName("allowExpiredCursorsAndAcceptMissedDeletes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowExpiredCursors { get; set; }
}

/// <summary>
/// A result set expression for sync.
/// </summary>
public class SyncResultSetExpression
{
    /// <summary>
    /// Node query specification.
    /// </summary>
    [JsonPropertyName("nodes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SyncNodesQuery? Nodes { get; set; }

    /// <summary>
    /// Limit on number of items to return.
    /// </summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Limit { get; set; } = 1000;
}

/// <summary>
/// Nodes query for sync.
/// </summary>
public class SyncNodesQuery
{
    /// <summary>
    /// Filter to apply to nodes (use FilterBuilder or anonymous object).
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Filter { get; set; }
}

/// <summary>
/// Selection of properties from a source.
/// </summary>
public class SelectExpression
{
    /// <summary>
    /// Sources and their properties to select.
    /// </summary>
    [JsonPropertyName("sources")]
    public IReadOnlyList<SourceSelection> Sources { get; set; } = Array.Empty<SourceSelection>();
}

/// <summary>
/// Selection from a specific source (view).
/// </summary>
public class SourceSelection
{
    /// <summary>
    /// The source view identifier.
    /// </summary>
    [JsonPropertyName("source")]
    public object Source { get; set; } = null!;

    /// <summary>
    /// Properties to select. Empty array means all properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public IReadOnlyList<string> Properties { get; set; } = Array.Empty<string>();
}
