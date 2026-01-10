// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using CogniteSdk;
using CogniteSdk.DataModels;
using CogniteSdk.Resources;
using CogniteSdk.Types.DataModels.Query;
using CogniteSdk.Types.DataModels.Search;
using CogniteSdk.Types.DataModels.Aggregate;
using CogniteSdk.Types.DataModels.Sync;
using Microsoft.Identity.Client;
using System.Text.Json;

namespace DataModelingExamples;

/// <summary>
/// Examples demonstrating the Data Modeling extensions for the Cognite .NET SDK.
/// 
/// Prerequisites:
/// - A CDF project with Data Modeling enabled
/// - A data model with at least one view (e.g., "Equipment")
/// - Service principal credentials with datamodels:read scope
/// 
/// Set environment variables before running:
///   CDF_PROJECT, CDF_CLUSTER, TENANT_ID, CLIENT_ID, CLIENT_SECRET
/// </summary>
class Program
{
    // Configure these for your data model
    private const string Space = "my_space";
    private const string DataModelId = "MyDataModel";
    private const string DataModelVersion = "1";
    private const string ViewExternalId = "Equipment";

    static async Task Main(string[] args)
    {
        // Load configuration from environment
        var project = Environment.GetEnvironmentVariable("CDF_PROJECT") 
            ?? throw new InvalidOperationException("Set CDF_PROJECT environment variable");
        var cluster = Environment.GetEnvironmentVariable("CDF_CLUSTER") ?? "api";
        var tenantId = Environment.GetEnvironmentVariable("TENANT_ID")
            ?? throw new InvalidOperationException("Set TENANT_ID environment variable");
        var clientId = Environment.GetEnvironmentVariable("CLIENT_ID")
            ?? throw new InvalidOperationException("Set CLIENT_ID environment variable");
        var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET")
            ?? throw new InvalidOperationException("Set CLIENT_SECRET environment variable");

        var baseUrl = $"https://{cluster}.cognitedata.com";
        var scopes = new[] { $"https://{cluster}.cognitedata.com/.default" };

        // Create MSAL app ONCE - it's designed for reuse and handles token caching internally
        var msalApp = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        // Token provider captures the app instance - does not recreate it per call
        Func<CancellationToken, Task<string>> tokenProvider = async (ct) =>
        {
            var result = await msalApp.AcquireTokenForClient(scopes)
                .ExecuteAsync(ct)
                .ConfigureAwait(false);
            return result.AccessToken;
        };

        // Create SDK client and get extension resources
        var client = new Client.Builder()
            .SetAppId("DataModelingExamples")
            .SetProject(project)
            .SetBaseUrl(new Uri(baseUrl))
            .Build();

        // Get extension resources using extension methods
        var graphQL = client.GraphQL(project, baseUrl, tokenProvider);
        var sync = client.Sync(project, baseUrl, tokenProvider);
        var query = client.QueryBuilder(project, baseUrl, tokenProvider);
        var search = client.Search(project, baseUrl, tokenProvider);
        var aggregate = client.Aggregate(project, baseUrl, tokenProvider);

        Console.WriteLine($"Connected to CDF Project: {project}");
        Console.WriteLine();

        // Run examples
        await GraphQLExample(graphQL);
        await QueryBuilderExample(query);
        await SearchExample(search);
        await AggregateExample(aggregate);
        await SyncExample(sync);
    }

    /// <summary>
    /// Example: Execute GraphQL queries against a Data Model.
    /// </summary>
    static async Task GraphQLExample(GraphQLResource graphQL)
    {
        Console.WriteLine("=== GraphQL Example ===");

        var query = @"
            query ListEquipment {
                listEquipment(first: 5) {
                    items {
                        externalId
                        name
                        status
                    }
                }
            }";

        try
        {
            var result = await graphQL.QueryRawAsync(
                space: Space,
                externalId: DataModelId,
                version: DataModelVersion,
                query: query
            );

            if (result.HasErrors)
            {
                Console.WriteLine("GraphQL Errors:");
                foreach (var error in result.Errors!)
                    Console.WriteLine($"  - {error.Message}");
            }
            else
            {
                Console.WriteLine($"Response: {JsonSerializer.Serialize(result.Data)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Build and execute queries using the fluent QueryBuilder.
    /// </summary>
    static async Task QueryBuilderExample(QueryBuilderResource queryBuilder)
    {
        Console.WriteLine("=== QueryBuilder Example ===");

        var equipmentView = new ViewIdentifier(Space, ViewExternalId, DataModelVersion);

        try
        {
            // Example 1: Simple query - list all items
            Console.WriteLine("Query 1: List all equipment");
            var result = await queryBuilder
                .WithNodes("equipment", equipmentView)
                .Select("equipment", equipmentView, "name", "status")
                .ExecuteAsync();

            Console.WriteLine($"  Found {result.Items["equipment"].Items.Count} items");

            // Reset builder for next query
            queryBuilder.Reset();

            // Example 2: Filtered query using FilterBuilder
            Console.WriteLine("\nQuery 2: Filter by status");
            var filter = FilterBuilder.Create()
                .Equals(equipmentView, "status", "Running")
                .Build();

            var filteredResult = await queryBuilder
                .WithNodes("running", equipmentView, filter: filter)
                .Select("running", equipmentView, "name", "status")
                .ExecuteAsync();

            Console.WriteLine($"  Found {filteredResult.Items["running"].Items.Count} running items");

            // Reset for next query
            queryBuilder.Reset();

            // Example 3: Complex filter with AND/OR
            Console.WriteLine("\nQuery 3: Complex filter (status=Running OR status=Standby)");
            var complexFilter = FilterBuilder.Create()
                .Or(
                    FilterBuilder.Create().Equals(equipmentView, "status", "Running"),
                    FilterBuilder.Create().Equals(equipmentView, "status", "Standby")
                )
                .Build();

            var complexResult = await queryBuilder
                .WithNodes("active", equipmentView, filter: complexFilter)
                .ExecuteAsync();

            Console.WriteLine($"  Found {complexResult.Items["active"].Items.Count} active items");

            // Reset for next query
            queryBuilder.Reset();

            // Example 4: Query with parameters (for query plan reuse)
            Console.WriteLine("\nQuery 4: Parameterized query");
            var paramFilter = FilterBuilder.Create()
                .Equals(equipmentView, "status", FilterBuilder.Parameter("statusParam"))
                .Build();

            var paramResult = await queryBuilder
                .WithParameter("statusParam", "Running")
                .WithNodes("byStatus", equipmentView, filter: paramFilter)
                .ExecuteAsync();

            Console.WriteLine($"  Found {paramResult.Items["byStatus"].Items.Count} items (parameterized)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Search for instances using full-text search.
    /// </summary>
    static async Task SearchExample(SearchResource search)
    {
        Console.WriteLine("=== Search Example ===");

        var equipmentView = new ViewIdentifier(Space, ViewExternalId, DataModelVersion);

        try
        {
            // Example 1: Simple full-text search
            Console.WriteLine("Search 1: Full-text search for 'pump'");
            var searchRequest = new SearchInstancesRequest
            {
                View = equipmentView,
                Query = "pump",
                Limit = 10
            };

            var results = await search.SearchAsync(searchRequest);
            Console.WriteLine($"  Found {results.Items.Count} matching items");

            // Example 2: Property-scoped search with filter
            Console.WriteLine("\nSearch 2: Search in 'name' field with filter");
            var scopedRequest = new SearchInstancesRequest
            {
                View = equipmentView,
                Query = "motor",
                Properties = new[] { "name", "description" },
                Filter = FilterBuilder.Create()
                    .Equals(equipmentView, "status", "Running")
                    .Build(),
                Limit = 5
            };

            var scopedResults = await search.SearchAsync(scopedRequest);
            Console.WriteLine($"  Found {scopedResults.Items.Count} running items matching 'motor'");

            // Example 3: Wildcard search
            Console.WriteLine("\nSearch 3: Wildcard search 'equip*'");
            var wildcardRequest = new SearchInstancesRequest
            {
                View = equipmentView,
                Query = "equip*",
                Limit = 10
            };

            var wildcardResults = await search.SearchAsync(wildcardRequest);
            Console.WriteLine($"  Found {wildcardResults.Items.Count} items matching 'equip*'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Aggregate data using count, avg, histogram.
    /// </summary>
    static async Task AggregateExample(AggregateResource aggregate)
    {
        Console.WriteLine("=== Aggregate Example ===");

        var equipmentView = new ViewIdentifier(Space, ViewExternalId, DataModelVersion);

        try
        {
            // Example 1: Simple count
            Console.WriteLine("Aggregate 1: Count all equipment");
            var countRequest = new AggregateInstancesRequest
            {
                View = equipmentView,
                Aggregates = new[]
                {
                    new AggregateOperation { Property = "*", Aggregate = "count" }
                }
            };

            var countResult = await aggregate.AggregateAsync(countRequest);
            Console.WriteLine($"  Total items: {countResult.Items.Count} aggregation result(s)");
            foreach (var item in countResult.Items)
            {
                Console.WriteLine($"    {item}");
            }

            // Example 2: Group by status
            Console.WriteLine("\nAggregate 2: Count by status");
            var groupByRequest = new AggregateInstancesRequest
            {
                View = equipmentView,
                Aggregates = new[]
                {
                    new AggregateOperation { Property = "*", Aggregate = "count" }
                },
                GroupBy = new[] { "status" }
            };

            var groupByResult = await aggregate.AggregateAsync(groupByRequest);
            Console.WriteLine($"  Groups: {groupByResult.Items.Count}");
            foreach (var item in groupByResult.Items)
            {
                Console.WriteLine($"    {item}");
            }

            // Example 3: Average with filter
            Console.WriteLine("\nAggregate 3: Average temperature of running equipment");
            var avgRequest = new AggregateInstancesRequest
            {
                View = equipmentView,
                Aggregates = new[]
                {
                    new AggregateOperation { Property = "temperature", Aggregate = "avg" }
                },
                Filter = FilterBuilder.Create()
                    .Equals(equipmentView, "status", "Running")
                    .Build()
            };

            var avgResult = await aggregate.AggregateAsync(avgRequest);
            Console.WriteLine($"  Result: {avgResult.Items.Count} aggregation(s)");
            foreach (var item in avgResult.Items)
            {
                Console.WriteLine($"    {item}");
            }

            // Example 4: Histogram
            Console.WriteLine("\nAggregate 4: Histogram of temperature (interval: 10)");
            var histogramRequest = new AggregateInstancesRequest
            {
                View = equipmentView,
                Aggregates = new[]
                {
                    new AggregateOperation 
                    { 
                        Property = "temperature", 
                        Aggregate = "histogram",
                        Interval = 10
                    }
                }
            };

            var histogramResult = await aggregate.AggregateAsync(histogramRequest);
            Console.WriteLine($"  Buckets: {histogramResult.Items.Count}");
            foreach (var item in histogramResult.Items)
            {
                Console.WriteLine($"    {item}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Example: Sync instances with cursor-based pagination and streaming.
    /// </summary>
    static async Task SyncExample(SyncResource sync)
    {
        Console.WriteLine("=== Sync Example ===");

        var equipmentView = new ViewIdentifier(Space, ViewExternalId, DataModelVersion);

        try
        {
            // Example 1: Single sync call (default onePhase mode)
            Console.WriteLine("Sync 1: Initial fetch (onePhase mode - default)");
            var syncResult = await sync.SyncAsync(equipmentView, limit: 10);

            Console.WriteLine($"  Received {syncResult.Items["items"].Nodes.Count} items");
            Console.WriteLine($"  Has more: {syncResult.NextCursor != null}");

            // Example 2: TwoPhase mode for better performance with indexes
            Console.WriteLine("\nSync 2: TwoPhase mode with backfillSort");
            var twoPhaseResult = await sync.SyncAsync(
                equipmentView,
                limit: 10,
                mode: SyncMode.TwoPhase
            );
            Console.WriteLine($"  Received {twoPhaseResult.Items["items"].Nodes.Count} items");

            // Example 3: NoBackfill mode - only changes from now
            Console.WriteLine("\nSync 3: NoBackfill mode (only new changes)");
            var noBackfillResult = await sync.SyncAsync(
                equipmentView,
                limit: 10,
                mode: SyncMode.NoBackfill
            );
            Console.WriteLine($"  Received {noBackfillResult.Items["items"].Nodes.Count} items");
            Console.WriteLine($"  (NoBackfill returns empty initially, use cursor for ongoing changes)");

            // Example 4: Streaming changes (with timeout for demo)
            Console.WriteLine("\nSync 4: Stream changes (5 second demo)");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await foreach (var batch in sync.StreamChangesAsync(
                    equipmentView,
                    pollIntervalMs: 2000,
                    token: cts.Token))
                {
                    Console.WriteLine($"  Batch at {batch.Timestamp:HH:mm:ss}: " +
                        $"{batch.Response.Items["items"].Nodes.Count} items");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("  (Streaming demo completed)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }
}
