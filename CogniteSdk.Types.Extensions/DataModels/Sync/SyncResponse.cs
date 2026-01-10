// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.Sync;

/// <summary>
/// Response from a sync request.
/// </summary>
public class SyncInstancesResponse
{
    /// <summary>
    /// Results for each named result set.
    /// </summary>
    [JsonPropertyName("items")]
    public Dictionary<string, SyncResultSet> Items { get; set; } = new();

    /// <summary>
    /// Cursors for each result set for the next sync request.
    /// Null if there are no more changes.
    /// </summary>
    [JsonPropertyName("nextCursor")]
    public Dictionary<string, string?>? NextCursor { get; set; }

    /// <summary>
    /// Returns true if any result set has more data available.
    /// </summary>
    [JsonIgnore]
    public bool HasNext => NextCursor?.Values.Any(c => c != null) == true;
}

/// <summary>
/// Result set from a sync operation.
/// </summary>
public class SyncResultSet
{
    /// <summary>
    /// The synced node instances.
    /// </summary>
    [JsonPropertyName("nodes")]
    public IReadOnlyList<JsonElement> Nodes { get; set; } = Array.Empty<JsonElement>();
}

/// <summary>
/// A batch of sync results used for streaming.
/// </summary>
public class SyncBatch
{
    /// <summary>
    /// The sync response.
    /// </summary>
    public SyncInstancesResponse Response { get; init; } = null!;

    /// <summary>
    /// The cursor used to fetch this batch.
    /// </summary>
    public Dictionary<string, string?>? Cursors { get; init; }

    /// <summary>
    /// Timestamp when this batch was received.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Returns true if this batch has data.
    /// </summary>
    public bool HasData => Response.Items.Values.Any(r => r.Nodes.Count > 0);
}
