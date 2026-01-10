# Feature Analysis: .NET SDK Extensions

This document analyzes the Data Modeling features in the Cognite .NET SDK and the extensions added by this fork.

## Analysis Summary

### Official SDK Already Has

The following features were found to **already exist** in the official SDK:

| Feature       | Official SDK Location   | Method                                                       |
| ------------- | ----------------------- | ------------------------------------------------------------ |
| Search API    | `DataModelsResource`    | `SearchInstances<T>()`                                       |
| Aggregate API | `DataModelsResource`    | `AggregateInstances()`                                       |
| Query API     | `DataModelsResource`    | `QueryInstances<T>()`                                        |
| Sync API      | `DataModelsResource`    | `SyncInstances<T>()`                                         |
| Filter Types  | `CogniteSdk.DataModels` | `EqualsFilter`, `AndFilter`, `HasDataFilter`, etc.           |
| Query Types   | `CogniteSdk.DataModels` | `QueryNodeTableExpression`, `QueryEdgeTableExpression`, etc. |

### Extensions Added by This Fork

| Feature              | Location                                                 | Description                                |
| -------------------- | -------------------------------------------------------- | ------------------------------------------ |
| **FilterBuilder**    | `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs`     | Fluent API for constructing filters        |
| **SyncMode enum**    | `CogniteSdk.Types/DataModels/Query/Query.cs`             | `onePhase`, `twoPhase`, `noBackfill` modes |
| **SyncBackfillSort** | `CogniteSdk.Types/DataModels/Query/Query.cs`             | Sort specification for backfill phase      |
| **GraphQL Types**    | `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs`         | Request/response types                     |
| **GraphQLResource**  | `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` | Execute GraphQL queries                    |

### Not Implemented (Constraints)

| Feature                    | Reason                                                                      |
| -------------------------- | --------------------------------------------------------------------------- |
| IAsyncEnumerable streaming | SDK targets .NET Standard 2.0 (C# 7.3), `IAsyncEnumerable` requires C# 8.0+ |

## FilterBuilder Details

The `FilterBuilder` provides a fluent API for constructing DMS filters:

```csharp
var filter = FilterBuilder.Create()
    .HasData(myView)
    .And(FilterBuilder.Create().Equals(myView, "status", "active"))
    .Build();  // Returns IDMSFilter
```

### Supported Filter Methods

| Method          | Creates                      |
| --------------- | ---------------------------- |
| `HasData()`     | `HasDataFilter`              |
| `Equals()`      | `EqualsFilter`               |
| `In()`          | `InFilter`                   |
| `Range()`       | `RangeFilter`                |
| `Prefix()`      | `PrefixFilter`               |
| `Exists()`      | `ExistsFilter`               |
| `ContainsAny()` | `ContainsAnyFilter`          |
| `ContainsAll()` | `ContainsAllFilter`          |
| `And()`         | `AndFilter`                  |
| `Or()`          | `OrFilter`                   |
| `Not()`         | `NotFilter`                  |
| `Nested()`      | `NestedFilter`               |
| `MatchAll()`    | `MatchAllFilter`             |
| `Parameter()`   | `ParameterizedPropertyValue` |

## SyncQuery Extensions

Extended the existing `SyncQuery` class:

```csharp
public class SyncQuery : Query
{
    // NEW: Sync mode (onePhase, twoPhase, noBackfill)
    public SyncMode? Mode { get; set; }
    
    // NEW: Backfill sort specification for twoPhase mode
    public IEnumerable<SyncBackfillSort> BackfillSort { get; set; }
    
    // NEW: Allow expired cursors (may miss deletes)
    public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
}
```

### SyncMode Values

| Value        | Description                        |
| ------------ | ---------------------------------- |
| `onePhase`   | Default mode, single pass sync     |
| `twoPhase`   | Two-stage sync for indexed filters |
| `noBackfill` | Skip backfill, only new changes    |

## GraphQL Resource

Standalone resource for GraphQL queries:

```csharp
var graphql = new GraphQLResource(httpClient, project, baseUrl, tokenProvider);

// Typed query
var result = await graphql.QueryAsync<MyType>(
    space: "my-space",
    externalId: "my-model",
    version: "1",
    query: "{ listMyView { items { name } } }"
);

// Raw JSON query
var raw = await graphql.QueryRawAsync(space, modelId, version, query);

// Schema introspection
var schema = await graphql.IntrospectAsync(space, modelId, version);
```

## Test Coverage

| Test File               | Tests | Description                              |
| ----------------------- | ----- | ---------------------------------------- |
| `FilterBuilderTests.cs` | 26    | FilterBuilder functionality              |
| `SyncQueryTests.cs`     | 8     | SyncQuery extensions                     |
| **Total**               | 34    | Unit tests (no CDF credentials required) |

## Alignment with SDK Patterns

The extensions follow official SDK patterns:

| Pattern                                           | Status                            |
| ------------------------------------------------- | --------------------------------- |
| Namespace: `CogniteSdk.DataModels`                | ✅ Used                            |
| Return types: `IDMSFilter`, `RawPropertyValue<T>` | ✅ Used                            |
| C# version: 7.3 compatible                        | ✅ Compatible                      |
| .NET Standard 2.0                                 | ✅ Compatible                      |
| Paket dependencies                                | ✅ No new dependencies             |
| XML documentation                                 | ✅ All public APIs documented      |
| Input validation                                  | ✅ ArgumentNullException/Exception |
| Copyright headers                                 | ✅ Apache-2.0 SPDX                 |

### Architectural Deviations

| Component           | SDK Pattern                       | Our Implementation             |
| ------------------- | --------------------------------- | ------------------------------ |
| `GraphQLResource`   | Inherits `Resource`, uses Oryx    | Standalone, uses `HttpClient`  |
| GraphQL access      | `client.GraphQL`                  | Manual instantiation required  |

**Rationale**: Integrating GraphQL into the Oryx pipeline would require modifying the F# layer (`Oryx.Cognite`). The standalone implementation was chosen to minimize scope while maintaining functionality.
