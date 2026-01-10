// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Types.DataModels.Query;

namespace CogniteSdk.Resources;

/// <summary>
/// Edge traversal direction for Data Model queries.
/// </summary>
public enum EdgeDirection
{
    /// <summary>Traverse outgoing edges (from source to target).</summary>
    Outwards,
    /// <summary>Traverse incoming edges (from target to source).</summary>
    Inwards
}

/// <summary>
/// Fluent query builder for Data Model instances.
/// Provides a chainable API for constructing and executing queries.
/// </summary>
/// <remarks>
/// This builder is stateful and not thread-safe. Use <see cref="Reset"/> between queries
/// or create a new instance for each query.
/// </remarks>
public class QueryBuilderResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    private readonly Func<CancellationToken, Task<string>> _tokenProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly Dictionary<string, QueryResultSetExpression> _with = new();
    private readonly Dictionary<string, QuerySelectExpression> _select = new();
    private readonly Dictionary<string, ViewIdentifier> _viewInfo = new();
    private Dictionary<string, string?>? _cursors;
    private Dictionary<string, object?>? _parameters;

    /// <summary>
    /// Creates a new query builder.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for requests.</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL.</param>
    /// <param name="tokenProvider">Function to get a valid access token.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public QueryBuilderResource(
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
    /// Adds a node result set to the query.
    /// </summary>
    /// <param name="name">Name for this result set.</param>
    /// <param name="view">View to query.</param>
    /// <param name="filter">Optional filter (use FilterBuilder).</param>
    /// <param name="limit">Maximum results (must be greater than 0).</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty, or limit is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    public QueryBuilderResource WithNodes(
        string name,
        ViewIdentifier view,
        object? filter = null,
        int limit = 1000)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        ArgumentNullException.ThrowIfNull(view);
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));

        // Build hasData filter for the view
        var hasDataFilter = new
        {
            hasData = new[]
            {
                new { type = "view", space = view.Space, externalId = view.ExternalId, version = view.Version }
            }
        };
        
        object? finalFilter;
        if (filter != null)
        {
            finalFilter = new { and = new object[] { hasDataFilter, filter } };
        }
        else
        {
            finalFilter = hasDataFilter;
        }

        _with[name] = new QueryResultSetExpression
        {
            Nodes = new QueryNodesExpression { Filter = finalFilter },
            Limit = limit
        };
        _viewInfo[name] = view;

        return this;
    }

    /// <summary>
    /// Adds an edge result set to the query.
    /// </summary>
    /// <param name="name">Name for this result set.</param>
    /// <param name="from">Result set to traverse from.</param>
    /// <param name="filter">Optional filter on edges (typically filter on ["edge", "type"]).</param>
    /// <param name="direction">Traversal direction.</param>
    /// <param name="limit">Maximum results (must be greater than 0).</param>
    /// <param name="maxDistance">Maximum traversal depth. Null = unlimited. Use 1 for direct connections only.</param>
    /// <param name="nodeFilter">Filter that nodes on the other side of edges must match.</param>
    /// <param name="terminationFilter">Filter to stop traversal at matching nodes.</param>
    /// <param name="limitEach">Limit edges per source node (only valid when maxDistance=1).</param>
    /// <exception cref="ArgumentException">Thrown when name or from is null or empty.</exception>
    public QueryBuilderResource WithEdges(
        string name,
        string from,
        object? filter = null,
        EdgeDirection direction = EdgeDirection.Outwards,
        int limit = 1000,
        int? maxDistance = null,
        object? nodeFilter = null,
        object? terminationFilter = null,
        int? limitEach = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        if (string.IsNullOrEmpty(from))
            throw new ArgumentException("From cannot be null or empty", nameof(from));
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));
        if (maxDistance.HasValue && maxDistance.Value <= 0)
            throw new ArgumentException("MaxDistance must be greater than 0 when specified", nameof(maxDistance));
        if (limitEach.HasValue && limitEach.Value <= 0)
            throw new ArgumentException("LimitEach must be greater than 0 when specified", nameof(limitEach));
        if (limitEach.HasValue && maxDistance != 1)
            throw new ArgumentException("LimitEach can only be used when maxDistance is 1", nameof(limitEach));

        _with[name] = new QueryResultSetExpression
        {
            Edges = new QueryEdgesExpression
            {
                From = from,
                Filter = filter,
                Direction = direction == EdgeDirection.Outwards ? "outwards" : "inwards",
                MaxDistance = maxDistance,
                NodeFilter = nodeFilter,
                TerminationFilter = terminationFilter,
                LimitEach = limitEach
            },
            Limit = limit
        };

        return this;
    }

    /// <summary>
    /// Adds nodes connected via edges.
    /// </summary>
    /// <param name="name">Name for this result set.</param>
    /// <param name="from">Edge result set to traverse from.</param>
    /// <param name="chainTo">Which end of edge to get: "source" or "destination".</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="limit">Maximum results (must be greater than 0).</param>
    /// <exception cref="ArgumentException">Thrown when name, from, or chainTo is invalid.</exception>
    public QueryBuilderResource WithNodesFrom(
        string name,
        string from,
        string chainTo = "destination",
        object? filter = null,
        int limit = 1000)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        if (string.IsNullOrEmpty(from))
            throw new ArgumentException("From cannot be null or empty", nameof(from));
        if (chainTo != "source" && chainTo != "destination")
            throw new ArgumentException("ChainTo must be 'source' or 'destination'", nameof(chainTo));
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));

        _with[name] = new QueryResultSetExpression
        {
            Nodes = new QueryNodesExpression
            {
                From = from,
                ChainTo = chainTo,
                Filter = filter
            },
            Limit = limit
        };

        return this;
    }

    /// <summary>
    /// Specifies properties to select for a result set.
    /// </summary>
    /// <param name="resultSetName">Result set name.</param>
    /// <param name="view">View to select from.</param>
    /// <param name="properties">Property names (empty = all properties).</param>
    /// <exception cref="ArgumentException">Thrown when resultSetName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    public QueryBuilderResource Select(
        string resultSetName,
        ViewIdentifier view,
        params string[] properties)
    {
        if (string.IsNullOrEmpty(resultSetName))
            throw new ArgumentException("Result set name cannot be null or empty", nameof(resultSetName));
        ArgumentNullException.ThrowIfNull(view);

        _select[resultSetName] = new QuerySelectExpression
        {
            Sources = new[]
            {
                new QuerySourceSelection
                {
                    Source = new { type = "view", space = view.Space, externalId = view.ExternalId, version = view.Version },
                    Properties = properties ?? Array.Empty<string>()
                }
            }
        };

        return this;
    }

    /// <summary>
    /// Sets cursors for pagination.
    /// </summary>
    /// <param name="cursors">Cursor dictionary from a previous query response.</param>
    public QueryBuilderResource WithCursors(Dictionary<string, string?>? cursors)
    {
        _cursors = cursors;
        return this;
    }

    /// <summary>
    /// Adds a parameter for use in parameterized filters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parameters allow query plan reuse across queries with different values,
    /// improving performance for read-heavy workloads.
    /// </para>
    /// <para>
    /// To use a parameter in a filter, use <see cref="FilterBuilder.Parameter(string)"/>
    /// or create an anonymous object: <c>new { parameter = "paramName" }</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await queryBuilder
    ///     .WithParameter("status", "Running")
    ///     .WithNodes("equipment", view, FilterBuilder.Create()
    ///         .Equals(view, "status", FilterBuilder.Parameter("status"))
    ///         .Build())
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <param name="name">Parameter name (must match the name used in filters).</param>
    /// <param name="value">Parameter value.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public QueryBuilderResource WithParameter(string name, object? value)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Parameter name cannot be null or empty", nameof(name));

        _parameters ??= new Dictionary<string, object?>();
        _parameters[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple parameters for use in parameterized filters.
    /// </summary>
    /// <param name="parameters">Dictionary of parameter names and values.</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters is null.</exception>
    public QueryBuilderResource WithParameters(Dictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        _parameters ??= new Dictionary<string, object?>();
        foreach (var (key, value) in parameters)
        {
            _parameters[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Builds the query request without executing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no result sets have been added.</exception>
    public QueryInstancesRequest Build()
    {
        if (_with.Count == 0)
            throw new InvalidOperationException("At least one result set must be added using WithNodes, WithEdges, or WithNodesFrom");

        // Auto-generate select for any result sets that don't have explicit select
        var select = new Dictionary<string, QuerySelectExpression>(_select);
        foreach (var (name, view) in _viewInfo)
        {
            if (!select.ContainsKey(name))
            {
                select[name] = new QuerySelectExpression
                {
                    Sources = new[]
                    {
                        new QuerySourceSelection
                        {
                            Source = new { type = "view", space = view.Space, externalId = view.ExternalId, version = view.Version },
                            Properties = Array.Empty<string>()
                        }
                    }
                };
            }
        }

        return new QueryInstancesRequest
        {
            With = _with,
            Select = select,
            Cursors = _cursors,
            Parameters = _parameters
        };
    }

    /// <summary>
    /// Executes the query and returns results.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when no result sets have been added.</exception>
    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    public async Task<QueryInstancesResponse> ExecuteAsync(CancellationToken token = default)
    {
        var request = Build();
        var url = $"{_baseUrl}/api/v1/projects/{_project}/models/instances/query";

        var accessToken = await _tokenProvider(token).ConfigureAwait(false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Query request failed: {response.StatusCode} - {responseContent}");
        }

        return ParseQueryResponse(responseContent);
    }

    /// <summary>
    /// Resets the builder for reuse.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public QueryBuilderResource Reset()
    {
        _with.Clear();
        _select.Clear();
        _viewInfo.Clear();
        _cursors = null;
        _parameters = null;
        return this;
    }

    private QueryInstancesResponse ParseQueryResponse(string json)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
        var result = new QueryInstancesResponse();

        if (raw.TryGetProperty("items", out var items))
        {
            foreach (var prop in items.EnumerateObject())
            {
                result.Items[prop.Name] = new QueryResultSet
                {
                    Items = prop.Value.EnumerateArray().ToList()
                };
            }
        }

        if (raw.TryGetProperty("nextCursor", out var cursor))
        {
            if (cursor.ValueKind == JsonValueKind.Object)
            {
                result.NextCursor = new Dictionary<string, string?>();
                foreach (var cursorProp in cursor.EnumerateObject())
                {
                    result.NextCursor[cursorProp.Name] = cursorProp.Value.ValueKind == JsonValueKind.String
                        ? cursorProp.Value.GetString()
                        : null;
                }
            }
        }

        return result;
    }
}
