// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace CogniteSdk.Types.DataModels.Search;

/// <summary>
/// Request body for searching Data Model instances.
/// </summary>
public class SearchInstancesRequest
{
    /// <summary>
    /// The view to search within. Required.
    /// </summary>
    [JsonPropertyName("view")]
    public SearchViewIdentifier View { get; set; } = null!;

    /// <summary>
    /// Full-text search query string.
    /// Supports wildcards and phrase matching.
    /// </summary>
    [JsonPropertyName("query")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Query { get; set; }

    /// <summary>
    /// Properties to search within. If not specified, searches all text fields.
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Properties { get; set; }

    /// <summary>
    /// Filter to apply to search results.
    /// Use FilterBuilder or anonymous object.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Filter { get; set; }

    /// <summary>
    /// Maximum number of results to return. Default 100, max 1000.
    /// </summary>
    [JsonPropertyName("limit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Instance type to search: "node" or "edge".
    /// </summary>
    [JsonPropertyName("instanceType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceType { get; set; }

    /// <summary>
    /// Sort specification for results.
    /// </summary>
    [JsonPropertyName("sort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<SearchSort>? Sort { get; set; }

    /// <summary>
    /// Target units for unit conversion in results.
    /// </summary>
    [JsonPropertyName("targetUnits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TargetUnit>? TargetUnits { get; set; }
}

/// <summary>
/// View identifier for search requests.
/// </summary>
public class SearchViewIdentifier
{
    /// <summary>
    /// Type of source. Always "view" for search requests.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "view";

    /// <summary>
    /// Space of the view.
    /// </summary>
    [JsonPropertyName("space")]
    public string Space { get; set; } = null!;

    /// <summary>
    /// External ID of the view.
    /// </summary>
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = null!;

    /// <summary>
    /// Version of the view.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = null!;

    /// <summary>
    /// Creates a SearchViewIdentifier (parameterless for serialization).
    /// </summary>
    public SearchViewIdentifier() { }

    /// <summary>
    /// Creates a SearchViewIdentifier from space, external ID, and version.
    /// </summary>
    /// <param name="space">Space of the view. Cannot be null or empty.</param>
    /// <param name="externalId">External ID of the view. Cannot be null or empty.</param>
    /// <param name="version">Version of the view. Cannot be null or empty.</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is null or empty.</exception>
    public SearchViewIdentifier(string space, string externalId, string version)
    {
        if (string.IsNullOrEmpty(space))
            throw new ArgumentException("Space cannot be null or empty", nameof(space));
        if (string.IsNullOrEmpty(externalId))
            throw new ArgumentException("ExternalId cannot be null or empty", nameof(externalId));
        if (string.IsNullOrEmpty(version))
            throw new ArgumentException("Version cannot be null or empty", nameof(version));

        Space = space;
        ExternalId = externalId;
        Version = version;
    }

    /// <summary>
    /// Implicit conversion from ViewIdentifier to SearchViewIdentifier.
    /// </summary>
    public static implicit operator SearchViewIdentifier(CogniteSdk.DataModels.ViewIdentifier view)
    {
        return new SearchViewIdentifier
        {
            Space = view.Space,
            ExternalId = view.ExternalId,
            Version = view.Version
        };
    }
}

/// <summary>
/// Sort specification for search results.
/// </summary>
public class SearchSort
{
    /// <summary>
    /// Property path to sort by.
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
    /// </summary>
    [JsonPropertyName("nullsFirst")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NullsFirst { get; set; }
}

/// <summary>
/// Target unit specification for unit conversion.
/// </summary>
public class TargetUnit
{
    /// <summary>
    /// Property to convert.
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = null!;

    /// <summary>
    /// Target unit for conversion.
    /// </summary>
    [JsonPropertyName("unit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UnitReference? Unit { get; set; }
}

/// <summary>
/// Reference to a unit in the unit catalog.
/// </summary>
public class UnitReference
{
    /// <summary>
    /// External ID of the unit.
    /// </summary>
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = null!;
}
