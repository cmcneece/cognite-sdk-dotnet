// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.Search;

/// <summary>
/// Response from a search request.
/// </summary>
public class SearchInstancesResponse
{
    /// <summary>
    /// Search result items.
    /// </summary>
    public IReadOnlyList<SearchResultItem> Items { get; set; } = Array.Empty<SearchResultItem>();

    /// <summary>
    /// Whether there are more results available.
    /// </summary>
    public bool HasMore => Items.Count > 0;
}

/// <summary>
/// A single search result item.
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// Instance type: "node" or "edge".
    /// </summary>
    public string InstanceType { get; set; } = null!;

    /// <summary>
    /// Space of the instance.
    /// </summary>
    public string Space { get; set; } = null!;

    /// <summary>
    /// External ID of the instance.
    /// </summary>
    public string ExternalId { get; set; } = null!;

    /// <summary>
    /// Version of the instance.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// When the instance was last updated.
    /// </summary>
    public long? LastUpdatedTime { get; set; }

    /// <summary>
    /// When the instance was created.
    /// </summary>
    public long? CreatedTime { get; set; }

    /// <summary>
    /// Properties of the instance, keyed by source view.
    /// </summary>
    public Dictionary<string, JsonElement> Properties { get; set; } = new();

    /// <summary>
    /// For edges: the start node reference.
    /// </summary>
    public NodeReference? StartNode { get; set; }

    /// <summary>
    /// For edges: the end node reference.
    /// </summary>
    public NodeReference? EndNode { get; set; }
}

/// <summary>
/// Reference to a node (used for edge start/end nodes).
/// </summary>
public class NodeReference
{
    /// <summary>
    /// Space of the node.
    /// </summary>
    [JsonPropertyName("space")]
    public string Space { get; set; } = null!;

    /// <summary>
    /// External ID of the node.
    /// </summary>
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = null!;
}
