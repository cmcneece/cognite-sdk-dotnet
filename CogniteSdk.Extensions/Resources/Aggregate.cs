// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Types.DataModels.Aggregate;
using CogniteSdk.Types.DataModels.Search;

namespace CogniteSdk.Resources;

/// <summary>
/// Resource for aggregating Data Model instances.
/// Provides summarization, grouping, and statistical analysis capabilities.
/// </summary>
/// <remarks>
/// <para>The aggregate endpoint supports:</para>
/// <list type="bullet">
/// <item>Group by: partition data based on property values</item>
/// <item>Aggregation functions: count, sum, avg, min, max, histogram</item>
/// <item>Filters: refine data before aggregation</item>
/// <item>Full-text search: combine text search with aggregation</item>
/// <item>Unit conversion</item>
/// </list>
/// </remarks>
public class AggregateResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    private readonly Func<CancellationToken, Task<string>> _tokenProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new Aggregate resource.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for requests.</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL.</param>
    /// <param name="tokenProvider">Function to get a valid access token.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AggregateResource(
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
    /// Counts instances matching a filter.
    /// </summary>
    /// <param name="view">View to aggregate within.</param>
    /// <param name="filter">Optional filter (use FilterBuilder or anonymous object).</param>
    /// <param name="instanceType">Instance type: "node" or "edge". Default "node".</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    public async Task<long> CountAsync(
        ViewIdentifier view,
        object? filter = null,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);

        var result = await AggregateAsync(
            view,
            aggregates: new[] { new AggregateOperation { Property = "*", Aggregate = "count" } },
            filter: filter,
            instanceType: instanceType,
            token: token).ConfigureAwait(false);

        return result.Items.FirstOrDefault()?.Aggregates.Values.FirstOrDefault()?.Count ?? 0;
    }

    /// <summary>
    /// Computes the average of a numeric property.
    /// </summary>
    /// <param name="view">View to aggregate within.</param>
    /// <param name="property">Property to average.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="instanceType">Instance type. Default "node".</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    /// <exception cref="ArgumentException">Thrown when property is null or empty.</exception>
    public async Task<double?> AvgAsync(
        ViewIdentifier view,
        string property,
        object? filter = null,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Property cannot be null or empty", nameof(property));

        var result = await AggregateAsync(
            view,
            aggregates: new[] { new AggregateOperation { Property = property, Aggregate = "avg" } },
            filter: filter,
            instanceType: instanceType,
            token: token).ConfigureAwait(false);

        return result.Items.FirstOrDefault()?.Aggregates.Values.FirstOrDefault()?.Avg;
    }

    /// <summary>
    /// Computes the sum of a numeric property.
    /// </summary>
    public async Task<double?> SumAsync(
        ViewIdentifier view,
        string property,
        object? filter = null,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Property cannot be null or empty", nameof(property));

        var result = await AggregateAsync(
            view,
            aggregates: new[] { new AggregateOperation { Property = property, Aggregate = "sum" } },
            filter: filter,
            instanceType: instanceType,
            token: token).ConfigureAwait(false);

        return result.Items.FirstOrDefault()?.Aggregates.Values.FirstOrDefault()?.Sum;
    }

    /// <summary>
    /// Finds the minimum value of a numeric property.
    /// </summary>
    public async Task<double?> MinAsync(
        ViewIdentifier view,
        string property,
        object? filter = null,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Property cannot be null or empty", nameof(property));

        var result = await AggregateAsync(
            view,
            aggregates: new[] { new AggregateOperation { Property = property, Aggregate = "min" } },
            filter: filter,
            instanceType: instanceType,
            token: token).ConfigureAwait(false);

        return result.Items.FirstOrDefault()?.Aggregates.Values.FirstOrDefault()?.Min;
    }

    /// <summary>
    /// Finds the maximum value of a numeric property.
    /// </summary>
    public async Task<double?> MaxAsync(
        ViewIdentifier view,
        string property,
        object? filter = null,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (string.IsNullOrEmpty(property))
            throw new ArgumentException("Property cannot be null or empty", nameof(property));

        var result = await AggregateAsync(
            view,
            aggregates: new[] { new AggregateOperation { Property = property, Aggregate = "max" } },
            filter: filter,
            instanceType: instanceType,
            token: token).ConfigureAwait(false);

        return result.Items.FirstOrDefault()?.Aggregates.Values.FirstOrDefault()?.Max;
    }

    /// <summary>
    /// Performs aggregation operations on instances.
    /// </summary>
    /// <param name="view">View to aggregate within.</param>
    /// <param name="aggregates">Aggregation operations to perform. Maximum 5.</param>
    /// <param name="groupBy">Properties to group by.</param>
    /// <param name="query">Optional full-text search query.</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="limit">Maximum groups to return. Default 25, max 10000.</param>
    /// <param name="instanceType">Instance type. Default "node".</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when view or aggregates is null.</exception>
    /// <exception cref="ArgumentException">Thrown when aggregates is empty or exceeds maximum.</exception>
    public async Task<AggregateInstancesResponse> AggregateAsync(
        ViewIdentifier view,
        IReadOnlyList<AggregateOperation> aggregates,
        IReadOnlyList<string>? groupBy = null,
        string? query = null,
        object? filter = null,
        int limit = 25,
        string instanceType = "node",
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(aggregates);
        if (aggregates.Count == 0)
            throw new ArgumentException("At least one aggregate operation is required", nameof(aggregates));
        if (aggregates.Count > 5)
            throw new ArgumentException("Maximum 5 aggregate operations per request", nameof(aggregates));
        if (limit <= 0 || limit > 10000)
            throw new ArgumentException("Limit must be between 1 and 10000", nameof(limit));

        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier
            {
                Type = "view",
                Space = view.Space,
                ExternalId = view.ExternalId,
                Version = view.Version
            },
            Aggregates = aggregates,
            GroupBy = groupBy,
            Query = query,
            Filter = filter,
            Limit = limit,
            InstanceType = instanceType
        };

        return await ExecuteAggregateAsync(request, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an aggregation request.
    /// </summary>
    /// <param name="request">The aggregation request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when request or request.View is null.</exception>
    /// <exception cref="ArgumentException">Thrown when aggregates is empty, exceeds maximum, or contains invalid operations.</exception>
    public async Task<AggregateInstancesResponse> AggregateAsync(
        AggregateInstancesRequest request,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.View, "request.View");
        ArgumentNullException.ThrowIfNull(request.Aggregates, "request.Aggregates");
        if (request.Aggregates.Count == 0)
            throw new ArgumentException("At least one aggregate operation is required", "request.Aggregates");
        if (request.Aggregates.Count > 5)
            throw new ArgumentException("Maximum 5 aggregate operations per request", "request.Aggregates");
        if (request.Limit <= 0 || request.Limit > 10000)
            throw new ArgumentException("Limit must be between 1 and 10000", "request.Limit");
        
        // Validate each aggregate operation
        foreach (var agg in request.Aggregates)
        {
            if (string.IsNullOrEmpty(agg.Property))
                throw new ArgumentException("AggregateOperation.Property cannot be null or empty");
            if (string.IsNullOrEmpty(agg.Aggregate))
                throw new ArgumentException("AggregateOperation.Aggregate cannot be null or empty");
        }

        return await ExecuteAggregateAsync(request, token).ConfigureAwait(false);
    }

    private async Task<AggregateInstancesResponse> ExecuteAggregateAsync(
        AggregateInstancesRequest request,
        CancellationToken token)
    {
        var url = $"{_baseUrl}/api/v1/projects/{_project}/models/instances/aggregate";

        var accessToken = await _tokenProvider(token).ConfigureAwait(false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Aggregate request failed: {response.StatusCode} - {responseContent}");
        }

        return ParseAggregateResponse(responseContent);
    }

    private AggregateInstancesResponse ParseAggregateResponse(string json)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
        var result = new AggregateInstancesResponse();
        var items = new List<AggregateResultItem>();

        if (raw.TryGetProperty("items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                var resultItem = new AggregateResultItem();

                if (item.TryGetProperty("group", out var group))
                {
                    foreach (var g in group.EnumerateObject())
                    {
                        resultItem.Group[g.Name] = g.Value.Clone();
                    }
                }

                if (item.TryGetProperty("aggregates", out var aggregates))
                {
                    foreach (var agg in aggregates.EnumerateObject())
                    {
                        var aggValue = new AggregateValue();
                        var aggObj = agg.Value;

                        if (aggObj.TryGetProperty("count", out var count))
                            aggValue.Count = count.GetInt64();
                        if (aggObj.TryGetProperty("sum", out var sum))
                            aggValue.Sum = sum.GetDouble();
                        if (aggObj.TryGetProperty("avg", out var avg))
                            aggValue.Avg = avg.GetDouble();
                        if (aggObj.TryGetProperty("min", out var min))
                            aggValue.Min = min.GetDouble();
                        if (aggObj.TryGetProperty("max", out var max))
                            aggValue.Max = max.GetDouble();

                        if (aggObj.TryGetProperty("histogram", out var histogram))
                        {
                            var buckets = new List<HistogramBucket>();
                            foreach (var bucket in histogram.EnumerateArray())
                            {
                                buckets.Add(new HistogramBucket
                                {
                                    Start = bucket.GetProperty("start").GetDouble(),
                                    Count = bucket.GetProperty("count").GetInt64()
                                });
                            }
                            aggValue.Histogram = buckets;
                        }

                        resultItem.Aggregates[agg.Name] = aggValue;
                    }
                }

                items.Add(resultItem);
            }
        }

        result.Items = items;
        return result;
    }
}
