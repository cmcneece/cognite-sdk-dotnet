// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Types.DataModels.Search;

namespace CogniteSdk.Resources;

/// <summary>
/// Resource for searching Data Model instances.
/// Provides full-text search capabilities with filtering and relevance ranking.
/// </summary>
/// <remarks>
/// <para>The search endpoint supports:</para>
/// <list type="bullet">
/// <item>Full-text search across text fields</item>
/// <item>Configurable matching with AND/OR operators</item>
/// <item>Prefix-based word matching</item>
/// <item>Filtering on properties</item>
/// <item>Unit conversion in results</item>
/// </list>
/// <para>Note: Search results are eventually consistent. New or updated data
/// may take a few seconds to become searchable.</para>
/// </remarks>
public class SearchResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    private readonly Func<CancellationToken, Task<string>> _tokenProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new Search resource.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for requests.</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL.</param>
    /// <param name="tokenProvider">Function to get a valid access token.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SearchResource(
        HttpClient httpClient,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _baseUrl = (baseUrl ?? throw new ArgumentNullException(nameof(baseUrl))).TrimEnd('/');
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Searches for instances matching a full-text query and/or filter.
    /// </summary>
    /// <param name="view">View to search within.</param>
    /// <param name="query">Full-text search query. Supports wildcards.</param>
    /// <param name="properties">Properties to search within. Null searches all text fields.</param>
    /// <param name="filter">Optional filter (use FilterBuilder or anonymous object).</param>
    /// <param name="limit">Maximum results. Default 100, max 1000.</param>
    /// <param name="instanceType">Instance type: "node" or "edge". Default "node".</param>
    /// <param name="sort">Optional sort specification.</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    /// <exception cref="ArgumentException">Thrown when neither query nor filter is provided, or limit is invalid.</exception>
    public async Task<SearchInstancesResponse> SearchAsync(
        ViewIdentifier view,
        string? query = null,
        IReadOnlyList<string>? properties = null,
        object? filter = null,
        int limit = 100,
        string instanceType = "node",
        IReadOnlyList<SearchSort>? sort = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrEmpty(query) && filter == null)
            throw new ArgumentException("At least one of query or filter must be provided");
        if (limit <= 0 || limit > 1000)
            throw new ArgumentException("Limit must be between 1 and 1000", nameof(limit));

        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier
            {
                Type = "view",
                Space = view.Space,
                ExternalId = view.ExternalId,
                Version = view.Version
            },
            Query = query,
            Properties = properties,
            Filter = filter,
            Limit = limit,
            InstanceType = instanceType,
            Sort = sort
        };

        return await ExecuteSearchAsync(request, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a search request.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when request or request.View is null.</exception>
    /// <exception cref="ArgumentException">Thrown when neither query nor filter is provided, or limit is invalid.</exception>
    public async Task<SearchInstancesResponse> SearchAsync(
        SearchInstancesRequest request,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.View, "request.View");
        if (string.IsNullOrEmpty(request.Query) && request.Filter == null)
            throw new ArgumentException("At least one of Query or Filter must be provided");
        if (request.Limit <= 0 || request.Limit > 1000)
            throw new ArgumentException("Limit must be between 1 and 1000", "request.Limit");

        return await ExecuteSearchAsync(request, token).ConfigureAwait(false);
    }

    private async Task<SearchInstancesResponse> ExecuteSearchAsync(
        SearchInstancesRequest request,
        CancellationToken token)
    {
        var url = $"{_baseUrl}/api/v1/projects/{_project}/models/instances/search";

        var accessToken = await _tokenProvider(token).ConfigureAwait(false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Search request failed: {response.StatusCode} - {responseContent}");
        }

        return ParseSearchResponse(responseContent);
    }

    private SearchInstancesResponse ParseSearchResponse(string json)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
        var result = new SearchInstancesResponse();
        var items = new List<SearchResultItem>();

        if (raw.TryGetProperty("items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                var resultItem = new SearchResultItem
                {
                    InstanceType = item.TryGetProperty("instanceType", out var it) ? it.GetString() ?? "node" : "node",
                    Space = item.TryGetProperty("space", out var space) ? space.GetString() ?? "" : "",
                    ExternalId = item.TryGetProperty("externalId", out var eid) ? eid.GetString() ?? "" : "",
                    Version = item.TryGetProperty("version", out var ver) ? ver.GetInt32() : 0
                };

                if (item.TryGetProperty("lastUpdatedTime", out var lut))
                    resultItem.LastUpdatedTime = lut.GetInt64();

                if (item.TryGetProperty("createdTime", out var ct))
                    resultItem.CreatedTime = ct.GetInt64();

                if (item.TryGetProperty("properties", out var props))
                {
                    foreach (var prop in props.EnumerateObject())
                    {
                        resultItem.Properties[prop.Name] = prop.Value.Clone();
                    }
                }

                if (item.TryGetProperty("startNode", out var startNode))
                {
                    resultItem.StartNode = new NodeReference
                    {
                        Space = startNode.GetProperty("space").GetString() ?? "",
                        ExternalId = startNode.GetProperty("externalId").GetString() ?? ""
                    };
                }

                if (item.TryGetProperty("endNode", out var endNode))
                {
                    resultItem.EndNode = new NodeReference
                    {
                        Space = endNode.GetProperty("space").GetString() ?? "",
                        ExternalId = endNode.GetProperty("externalId").GetString() ?? ""
                    };
                }

                items.Add(resultItem);
            }
        }

        result.Items = items;
        return result;
    }
}
