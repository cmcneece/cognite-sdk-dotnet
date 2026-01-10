// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.Query;

/// <summary>
/// Response from a query request.
/// </summary>
public class QueryInstancesResponse
{
    /// <summary>
    /// Results for each named result set.
    /// </summary>
    [JsonPropertyName("items")]
    public Dictionary<string, QueryResultSet> Items { get; set; } = new();

    /// <summary>
    /// Cursors for pagination of each result set.
    /// </summary>
    [JsonPropertyName("nextCursor")]
    public Dictionary<string, string?>? NextCursor { get; set; }

    /// <summary>
    /// Returns true if any result set has more data.
    /// </summary>
    [JsonIgnore]
    public bool HasNext => NextCursor?.Values.Any(c => c != null) == true;
}

/// <summary>
/// Result set from a query.
/// </summary>
public class QueryResultSet
{
    /// <summary>
    /// The queried instances (nodes or edges) as raw JSON.
    /// </summary>
    public IReadOnlyList<JsonElement> Items { get; set; } = Array.Empty<JsonElement>();
}
