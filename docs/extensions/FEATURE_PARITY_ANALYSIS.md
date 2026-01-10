# Technical Reference: Data Modeling Extensions

This document provides technical details about the Data Modeling extensions in this fork.

## Extensions Overview

| Extension        | Purpose                                            | Python SDK Equivalent                  |
| ---------------- | -------------------------------------------------- | -------------------------------------- |
| FilterBuilder    | Fluent API for constructing DMS filters            | `cognite.client.data_modeling.filters` |
| SyncMode         | Sync mode control (onePhase, twoPhase, noBackfill) | `sync_mode` parameter                  |
| SyncBackfillSort | Backfill sort specification for two-phase sync     | `backfill_sort` parameter              |
| GraphQL Resource | Execute GraphQL queries against Data Models        | `client.data_modeling.graphql`         |

## FilterBuilder

### API Reference

```csharp
public class FilterBuilder
{
    // Factory
    public static FilterBuilder Create();
    
    // Leaf Filters
    public FilterBuilder HasData(params ViewIdentifier[] views);
    public FilterBuilder Equals(ViewIdentifier view, string property, string|double|long|bool value);
    public FilterBuilder In(ViewIdentifier view, string property, params string[] values);
    public FilterBuilder Range(ViewIdentifier view, string property, double? gte, double? gt, double? lte, double? lt);
    public FilterBuilder Prefix(ViewIdentifier view, string property, string prefix);
    public FilterBuilder Exists(ViewIdentifier view, string property);
    public FilterBuilder ContainsAny(ViewIdentifier view, string property, params string[] values);
    public FilterBuilder ContainsAll(ViewIdentifier view, string property, params string[] values);
    
    // Logical Operators
    public FilterBuilder And(params FilterBuilder[] filters);
    public FilterBuilder And(FilterBuilder other);
    public FilterBuilder Or(params FilterBuilder[] filters);
    public FilterBuilder Not(FilterBuilder filter);
    public FilterBuilder Nested(IEnumerable<string> scope, FilterBuilder filter);
    public FilterBuilder MatchAll();
    
    // Parameterized Queries
    public static IDMSValue Parameter(string parameterName);
    
    // Build
    public IDMSFilter Build();
    public IDMSFilter BuildOrNull();  // Returns null if no filters added
}
```

### Property Path Format

FilterBuilder uses a 3-element property path: `[space, "view/version", property]`

```csharp
var view = new ViewIdentifier("mySpace", "myView", "1");

// Generates: ["mySpace", "myView/1", "temperature"]
FilterBuilder.Create().Equals(view, "temperature", 25.0);
```

### Integration with Query API

```csharp
var filter = FilterBuilder.Create()
    .Equals(view, "status", "active")
    .Build();

var query = new Query
{
    With = new Dictionary<string, IQueryTableExpression>
    {
        { "nodes", new QueryNodeTableExpression
        {
            Nodes = new QueryNodes { Filter = filter }
        }}
    },
    Select = new Dictionary<string, SelectExpression>
    {
        { "nodes", new SelectExpression
        {
            Sources = new[] { new SelectSource { Source = view } }
        }}
    }
};

var result = await client.DataModels.QueryInstances<MyType>(query);
```

## SyncQuery Extensions

### New Properties

```csharp
public class SyncQuery : Query
{
    /// <summary>Sync mode: onePhase, twoPhase, or noBackfill</summary>
    public SyncMode? Mode { get; set; }
    
    /// <summary>Sort specification for backfill phase (twoPhase only)</summary>
    public IEnumerable<SyncBackfillSort> BackfillSort { get; set; }
    
    /// <summary>Allow expired cursors (may miss soft deletes)</summary>
    public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
}
```

### SyncMode Enum

```csharp
public enum SyncMode
{
    [JsonPropertyName("onePhase")]
    onePhase,
    
    [JsonPropertyName("twoPhase")]
    twoPhase,
    
    [JsonPropertyName("noBackfill")]
    noBackfill
}
```

### SyncBackfillSort Class

```csharp
public class SyncBackfillSort
{
    /// <summary>Property path (validated: not null, not empty, no null segments)</summary>
    public IEnumerable<string> Property { get; set; }
    
    /// <summary>Sort direction</summary>
    public SortDirection Direction { get; set; }
    
    /// <summary>Whether nulls sort first</summary>
    public bool NullsFirst { get; set; }
}
```

## GraphQL Resource

### API Reference

```csharp
public class GraphQLResource
{
    public GraphQLResource(
        HttpClient httpClient,
        string project,
        string baseUrl,
        Func<CancellationToken, Task<string>> tokenProvider);
    
    /// <summary>Execute typed GraphQL query</summary>
    public Task<GraphQLResponse<T>> QueryAsync<T>(
        string space, string externalId, string version, string query,
        Dictionary<string, object> variables = null,
        CancellationToken token = default);
    
    /// <summary>Execute raw GraphQL query (returns JsonElement)</summary>
    public Task<GraphQLRawResponse> QueryRawAsync(
        string space, string externalId, string version, string query,
        Dictionary<string, object> variables = null,
        CancellationToken token = default);
    
    /// <summary>Get schema via introspection</summary>
    public Task<GraphQLRawResponse> IntrospectAsync(
        string space, string externalId, string version,
        CancellationToken token = default);
}
```

### Response Types

```csharp
public class GraphQLResponse<T>
{
    public T Data { get; set; }
    public IEnumerable<GraphQLError> Errors { get; set; }
    public JsonElement? Extensions { get; set; }
    public bool HasErrors { get; }
}

public class GraphQLError
{
    public string Message { get; set; }
    public IEnumerable<GraphQLErrorLocation> Locations { get; set; }
    public IEnumerable<object> Path { get; set; }
    public JsonElement? Extensions { get; set; }
}
```

### URL Format

GraphQL queries are sent to:
```
POST {baseUrl}/api/v1/projects/{project}/models/spaces/{space}/datamodels/{externalId}/versions/{version}/graphql
```

## Test Coverage

| Test File                          | Tests  | Description                                  |
| ---------------------------------- | ------ | -------------------------------------------- |
| `FilterBuilderTests.cs`            | 26     | Unit tests for all filter operations         |
| `SyncQueryTests.cs`                | 13     | Unit tests for SyncMode and SyncBackfillSort |
| `FilterBuilderIntegrationTests.cs` | 7      | Integration tests against live CDF           |
| `SyncGraphQLIntegrationTests.cs`   | 5      | Integration tests for Sync and GraphQL       |
| **Total**                          | **51** |                                              |

## Known Limitations

| Limitation                                       | Reason                                    |
| ------------------------------------------------ | ----------------------------------------- |
| `SyncMode` may not work on all clusters          | API feature not yet universally available |
| GraphQL is standalone (not via `client.GraphQL`) | Would require F#/Oryx layer modifications |
| No `IAsyncEnumerable` streaming                  | SDK targets .NET Standard 2.0 (C# 7.3)    |

## SDK Compatibility

| Aspect                | Status                                     |
| --------------------- | ------------------------------------------ |
| .NET Standard 2.0     | ✅ Compatible                               |
| C# 7.3                | ✅ Compatible                               |
| Paket dependencies    | ✅ No new dependencies                      |
| Namespace conventions | ✅ Uses `CogniteSdk.DataModels`             |
| Return types          | ✅ Uses `IDMSFilter`, `RawPropertyValue<T>` |
