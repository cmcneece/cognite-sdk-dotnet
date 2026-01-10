// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Types.DataModels.Sync;

namespace CogniteSdk.Resources;

/// <summary>
/// Resource for syncing Data Model instances.
/// Provides cursor-based synchronization and streaming capabilities.
/// </summary>
public class SyncResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    private readonly Func<CancellationToken, Task<string>> _tokenProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new Sync resource.
    /// </summary>
    /// <param name="httpClient">HTTP client to use for requests.</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL.</param>
    /// <param name="tokenProvider">Function to get a valid access token.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SyncResource(
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
    /// Syncs instances from a view.
    /// </summary>
    /// <param name="view">View to sync from.</param>
    /// <param name="cursors">Optional cursors from a previous sync.</param>
    /// <param name="filter">Optional additional filter (use FilterBuilder or anonymous object).</param>
    /// <param name="limit">Maximum items to return per result set (must be greater than 0).</param>
    /// <param name="mode">
    /// Sync mode controlling backfill behavior:
    /// <list type="bullet">
    /// <item><see cref="SyncMode.OnePhase"/> (default): No distinction between backfill and changes.</item>
    /// <item><see cref="SyncMode.TwoPhase"/>: Split into two stages for better index usage.</item>
    /// <item><see cref="SyncMode.NoBackfill"/>: Skip backfill, only get new changes.</item>
    /// </list>
    /// </param>
    /// <param name="backfillSort">Sort specification for backfill phase (only with TwoPhase mode).</param>
    /// <param name="allowExpiredCursors">Allow cursors older than 3 days (may miss deletions).</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    /// <exception cref="ArgumentException">Thrown when limit is less than or equal to 0.</exception>
    public async Task<SyncInstancesResponse> SyncAsync(
        ViewIdentifier view,
        Dictionary<string, string?>? cursors = null,
        object? filter = null,
        int limit = 1000,
        SyncMode mode = SyncMode.OnePhase,
        IReadOnlyList<SyncBackfillSort>? backfillSort = null,
        bool? allowExpiredCursors = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));
        if (backfillSort != null && mode != SyncMode.TwoPhase)
            throw new ArgumentException("BackfillSort can only be used with TwoPhase mode", nameof(backfillSort));

        // Build hasData filter
        var hasDataFilter = new
        {
            hasData = new[]
            {
                new { type = "view", space = view.Space, externalId = view.ExternalId, version = view.Version }
            }
        };
        
        object finalFilter;
        if (filter != null)
        {
            finalFilter = new { and = new object[] { hasDataFilter, filter } };
        }
        else
        {
            finalFilter = hasDataFilter;
        }

        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery { Filter = finalFilter },
                    Limit = limit
                }
            },
            Select = new Dictionary<string, Types.DataModels.Sync.SelectExpression>
            {
                ["items"] = new Types.DataModels.Sync.SelectExpression
                {
                    Sources = new[]
                    {
                        new SourceSelection
                        {
                            Source = view,
                            Properties = Array.Empty<string>()
                        }
                    }
                }
            },
            Cursors = cursors,
            Mode = ConvertSyncMode(mode),
            BackfillSort = backfillSort,
            AllowExpiredCursors = allowExpiredCursors
        };

        return await ExecuteSyncAsync(request, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the enum to API string value.
    /// </summary>
    private static string? ConvertSyncMode(SyncMode mode)
    {
        return mode switch
        {
            SyncMode.OnePhase => null, // Default, don't send
            SyncMode.TwoPhase => "twoPhase",
            SyncMode.NoBackfill => "noBackfill",
            _ => null
        };
    }

    /// <summary>
    /// Streams changes from a view as they occur.
    /// </summary>
    /// <param name="view">View to watch.</param>
    /// <param name="pollIntervalMs">Interval between polls in milliseconds (must be greater than 0).</param>
    /// <param name="filter">Optional filter.</param>
    /// <param name="mode">
    /// Sync mode controlling backfill behavior. Consider using <see cref="SyncMode.TwoPhase"/>
    /// with hasData filters for better performance.
    /// </param>
    /// <param name="allowExpiredCursors">Allow cursors older than 3 days (may miss deletions).</param>
    /// <param name="token">Cancellation token to stop streaming.</param>
    /// <returns>An async enumerable of sync batches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    /// <exception cref="ArgumentException">Thrown when pollIntervalMs is less than or equal to 0.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested via the token.</exception>
    public async IAsyncEnumerable<SyncBatch> StreamChangesAsync(
        ViewIdentifier view,
        int pollIntervalMs = 5000,
        object? filter = null,
        SyncMode mode = SyncMode.OnePhase,
        bool? allowExpiredCursors = null,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (pollIntervalMs <= 0)
            throw new ArgumentException("Poll interval must be greater than 0", nameof(pollIntervalMs));

        Dictionary<string, string?>? cursors = null;

        while (!token.IsCancellationRequested)
        {
            var response = await SyncAsync(view, cursors, filter, mode: mode, 
                allowExpiredCursors: allowExpiredCursors, token: token).ConfigureAwait(false);

            var batch = new SyncBatch
            {
                Response = response,
                Timestamp = DateTimeOffset.UtcNow,
                Cursors = cursors
            };

            yield return batch;

            cursors = response.NextCursor;

            if (!response.HasNext)
            {
                // Task.Delay throws OperationCanceledException when cancelled
                await Task.Delay(pollIntervalMs, token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Executes a sync request.
    /// </summary>
    /// <param name="request">The sync request.</param>
    /// <param name="token">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when request contains invalid backfillSort.</exception>
    public async Task<SyncInstancesResponse> SyncAsync(
        SyncInstancesRequest request,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // Validate backfillSort if present
        if (request.BackfillSort != null)
        {
            foreach (var sort in request.BackfillSort)
            {
                if (sort.Property == null || sort.Property.Length == 0)
                    throw new ArgumentException("SyncBackfillSort.Property cannot be null or empty");
            }
        }

        return await ExecuteSyncAsync(request, token).ConfigureAwait(false);
    }

    private async Task<SyncInstancesResponse> ExecuteSyncAsync(
        SyncInstancesRequest request,
        CancellationToken token)
    {
        var url = $"{_baseUrl}/api/v1/projects/{_project}/models/instances/sync";

        var accessToken = await _tokenProvider(token).ConfigureAwait(false);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        var response = await _httpClient.SendAsync(httpRequest, token).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Sync request failed: {response.StatusCode} - {responseContent}");
        }

        return ParseSyncResponse(responseContent);
    }

    private SyncInstancesResponse ParseSyncResponse(string json)
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
        var result = new SyncInstancesResponse();

        if (raw.TryGetProperty("items", out var items))
        {
            foreach (var prop in items.EnumerateObject())
            {
                var resultSet = new SyncResultSet();
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    resultSet = new SyncResultSet { Nodes = prop.Value.EnumerateArray().ToList() };
                }
                result.Items[prop.Name] = resultSet;
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
