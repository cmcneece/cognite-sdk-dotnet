// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using CogniteSdk.Resources;

namespace CogniteSdk;

/// <summary>
/// Extension methods for creating Data Modeling resources.
/// </summary>
/// <remarks>
/// These extensions create GraphQL, Sync, and QueryBuilder resources for working with
/// CDF Data Models. All extensions use a shared HttpClient instance by default for
/// optimal performance and resource management.
/// </remarks>
public static class ClientExtensions
{
    // Shared HttpClient instance - HttpClient is designed for reuse.
    // See: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    private static readonly HttpClient SharedHttpClient = new();

    /// <summary>
    /// Creates a GraphQL resource for querying Data Models.
    /// </summary>
    /// <param name="client">The Cognite SDK client (used as extension anchor).</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
    /// <param name="tokenProvider">Function to provide access tokens.</param>
    /// <param name="httpClient">Optional custom HttpClient. If not provided, uses a shared instance.</param>
    /// <returns>A GraphQL resource for querying data models.</returns>
    /// <example>
    /// <code>
    /// var graphQL = client.GraphQL("myProject", "https://api.cognitedata.com", async ct => await GetTokenAsync(ct));
    /// var result = await graphQL.QueryRawAsync("mySpace", "MyModel", "1", "{ listEquipment { items { name } } }");
    /// </code>
    /// </example>
    public static GraphQLResource GraphQL(
        this Client client,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider,
        HttpClient? httpClient = null)
    {
        return new GraphQLResource(
            httpClient ?? SharedHttpClient,
            project,
            baseUrl,
            tokenProvider);
    }

    /// <summary>
    /// Creates a Sync resource for synchronizing Data Model instances.
    /// </summary>
    /// <param name="client">The Cognite SDK client (used as extension anchor).</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
    /// <param name="tokenProvider">Function to provide access tokens.</param>
    /// <param name="httpClient">Optional custom HttpClient. If not provided, uses a shared instance.</param>
    /// <returns>A Sync resource for streaming data model changes.</returns>
    /// <example>
    /// <code>
    /// var sync = client.Sync("myProject", "https://api.cognitedata.com", async ct => await GetTokenAsync(ct));
    /// var view = new ViewIdentifier("mySpace", "Equipment", "1");
    /// await foreach (var batch in sync.StreamChangesAsync(view))
    /// {
    ///     ProcessBatch(batch);
    /// }
    /// </code>
    /// </example>
    public static SyncResource Sync(
        this Client client,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider,
        HttpClient? httpClient = null)
    {
        return new SyncResource(
            httpClient ?? SharedHttpClient,
            project,
            baseUrl,
            tokenProvider);
    }

    /// <summary>
    /// Creates a QueryBuilder for fluent Data Model queries.
    /// </summary>
    /// <param name="client">The Cognite SDK client (used as extension anchor).</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
    /// <param name="tokenProvider">Function to provide access tokens.</param>
    /// <param name="httpClient">Optional custom HttpClient. If not provided, uses a shared instance.</param>
    /// <returns>A QueryBuilder resource for executing data model queries.</returns>
    /// <example>
    /// <code>
    /// var queryBuilder = client.QueryBuilder("myProject", "https://api.cognitedata.com", async ct => await GetTokenAsync(ct));
    /// var view = new ViewIdentifier("mySpace", "Equipment", "1");
    /// var result = await queryBuilder
    ///     .WithNodes("equipment", view)
    ///     .Select("equipment", view, "name", "status")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public static QueryBuilderResource QueryBuilder(
        this Client client,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider,
        HttpClient? httpClient = null)
    {
        return new QueryBuilderResource(
            httpClient ?? SharedHttpClient,
            project,
            baseUrl,
            tokenProvider);
    }

    /// <summary>
    /// Creates a Search resource for full-text searching Data Model instances.
    /// </summary>
    /// <param name="client">The Cognite SDK client (used as extension anchor).</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
    /// <param name="tokenProvider">Function to provide access tokens.</param>
    /// <param name="httpClient">Optional custom HttpClient. If not provided, uses a shared instance.</param>
    /// <returns>A Search resource for full-text search operations.</returns>
    /// <example>
    /// <code>
    /// var search = client.Search("myProject", "https://api.cognitedata.com", async ct => await GetTokenAsync(ct));
    /// var view = new ViewIdentifier("mySpace", "Equipment", "1");
    /// var results = await search.SearchAsync(view, query: "pump*");
    /// </code>
    /// </example>
    public static SearchResource Search(
        this Client client,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider,
        HttpClient? httpClient = null)
    {
        return new SearchResource(
            httpClient ?? SharedHttpClient,
            project,
            baseUrl,
            tokenProvider);
    }

    /// <summary>
    /// Creates an Aggregate resource for aggregating Data Model instances.
    /// </summary>
    /// <param name="client">The Cognite SDK client (used as extension anchor).</param>
    /// <param name="project">CDF project name.</param>
    /// <param name="baseUrl">CDF base URL (e.g., "https://api.cognitedata.com").</param>
    /// <param name="tokenProvider">Function to provide access tokens.</param>
    /// <param name="httpClient">Optional custom HttpClient. If not provided, uses a shared instance.</param>
    /// <returns>An Aggregate resource for aggregation operations.</returns>
    /// <example>
    /// <code>
    /// var aggregate = client.Aggregate("myProject", "https://api.cognitedata.com", async ct => await GetTokenAsync(ct));
    /// var view = new ViewIdentifier("mySpace", "Equipment", "1");
    /// var count = await aggregate.CountAsync(view);
    /// var avgTemp = await aggregate.AvgAsync(view, "temperature");
    /// </code>
    /// </example>
    public static AggregateResource Aggregate(
        this Client client,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider,
        HttpClient? httpClient = null)
    {
        return new AggregateResource(
            httpClient ?? SharedHttpClient,
            project,
            baseUrl,
            tokenProvider);
    }
}
