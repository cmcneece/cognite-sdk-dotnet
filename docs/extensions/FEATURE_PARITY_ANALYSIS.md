# Feature Parity Analysis: Python SDK vs .NET SDK

This document analyzes the Data Modeling feature gap between the [Cognite Python SDK](https://cognite-sdk-python.readthedocs-hosted.com/) and the official .NET SDK, and documents what this fork adds to close that gap.

## Executive Summary

| Feature Area          | Python SDK | .NET SDK (Official) | .NET SDK (This Fork)    |
| --------------------- | ---------- | ------------------- | ----------------------- |
| Fluent Filter Builder | ✅ Full     | ❌ None              | ✅ Full                  |
| Sync Modes            | ✅ Full     | ❌ None              | ✅ Full                  |
| GraphQL Client        | ✅ Full     | ❌ None              | ✅ Full                  |
| Async Streaming       | ✅ Full     | ❌ None              | ❌ Not possible (C# 7.3) |

## Detailed Analysis

### 1. Filter Building

#### Python SDK

The Python SDK provides a fluent filter API in `cognite.client.data_modeling.filters`:

```python
from cognite.client.data_modeling import filters as f

# Fluent filter construction
filter = f.And(
    f.HasData(views=[my_view]),
    f.Equals(my_view.as_property_ref("status"), "active"),
    f.Range(my_view.as_property_ref("temperature"), gte=20.0, lte=30.0)
)

result = client.data_modeling.instances.search(
    view=my_view,
    filter=filter
)
```

#### .NET SDK (Official)

The official .NET SDK has filter *types* but no fluent builder:

```csharp
// Must manually construct filter objects
var filter = new AndFilter
{
    And = new List<IDMSFilter>
    {
        new HasDataFilter { HasData = new[] { myView } },
        new EqualsFilter 
        { 
            Property = new[] { "mySpace", "myView/1", "status" },
            Value = new RawPropertyValue<string>("active")
        },
        // ... verbose and error-prone
    }
};
```

#### .NET SDK (This Fork)

This fork adds `FilterBuilder` for Python-like fluency:

```csharp
using CogniteSdk.DataModels;

var filter = FilterBuilder.Create()
    .HasData(myView)
    .And(FilterBuilder.Create()
        .Equals(myView, "status", "active")
        .Range(myView, "temperature", gte: 20.0, lte: 30.0))
    .Build();
```

#### Comparison

| Python SDK Method             | This Fork's Equivalent             |
| ----------------------------- | ---------------------------------- |
| `f.HasData(views)`            | `.HasData(views)`                  |
| `f.Equals(prop, value)`       | `.Equals(view, prop, value)`       |
| `f.In(prop, values)`          | `.In(view, prop, values)`          |
| `f.Range(prop, gte, lte)`     | `.Range(view, prop, gte, lte)`     |
| `f.Prefix(prop, prefix)`      | `.Prefix(view, prop, prefix)`      |
| `f.Exists(prop)`              | `.Exists(view, prop)`              |
| `f.ContainsAny(prop, values)` | `.ContainsAny(view, prop, values)` |
| `f.ContainsAll(prop, values)` | `.ContainsAll(view, prop, values)` |
| `f.And(*filters)`             | `.And(filters)`                    |
| `f.Or(*filters)`              | `.Or(filters)`                     |
| `f.Not(filter)`               | `.Not(filter)`                     |
| `f.Nested(scope, filter)`     | `.Nested(scope, filter)`           |
| `f.MatchAll()`                | `.MatchAll()`                      |

**Gap Status: ✅ Closed**

---

### 2. Sync Modes

#### Python SDK

The Python SDK supports sync modes for incremental data synchronization:

```python
result = client.data_modeling.instances.sync(
    view=my_view,
    sync_mode="twoPhase",  # or "onePhase", "noBackfill"
    backfill_sort=[{
        "property": ["mySpace", "myView/1", "timestamp"],
        "direction": "ascending",
        "nulls_first": False
    }],
    allow_expired_cursors_and_accept_missed_deletes=True
)
```

#### .NET SDK (Official)

The official .NET SDK has `SyncQuery` but lacks:
- `mode` parameter (sync mode selection)
- `backfill_sort` parameter (backfill ordering)
- `allow_expired_cursors_and_accept_missed_deletes` parameter

```csharp
// Only basic sync is supported
var syncQuery = new SyncQuery
{
    With = { ... },
    Select = { ... },
    Cursors = cursor  // Basic cursor support only
};
```

#### .NET SDK (This Fork)

This fork extends `SyncQuery` with full sync mode support:

```csharp
using CogniteSdk.DataModels;

var syncQuery = new SyncQuery
{
    Mode = SyncMode.twoPhase,
    BackfillSort = new[] 
    { 
        new SyncBackfillSort 
        { 
            Property = new[] { "mySpace", "myView/1", "timestamp" },
            Direction = SortDirection.ascending,
            NullsFirst = false
        } 
    },
    AllowExpiredCursorsAndAcceptMissedDeletes = true,
    With = { ... },
    Select = { ... }
};
```

#### Comparison

| Python SDK Parameter                                   | This Fork's Equivalent                             |
| ------------------------------------------------------ | -------------------------------------------------- |
| `sync_mode="onePhase"`                                 | `Mode = SyncMode.onePhase`                         |
| `sync_mode="twoPhase"`                                 | `Mode = SyncMode.twoPhase`                         |
| `sync_mode="noBackfill"`                               | `Mode = SyncMode.noBackfill`                       |
| `backfill_sort=[...]`                                  | `BackfillSort = new[] { ... }`                     |
| `allow_expired_cursors_and_accept_missed_deletes=True` | `AllowExpiredCursorsAndAcceptMissedDeletes = true` |

**Gap Status: ✅ Closed** (Note: API support for `mode` varies by CDF cluster version)

---

### 3. GraphQL Client

#### Python SDK

The Python SDK provides a GraphQL client for Data Models:

```python
result = client.data_modeling.graphql.query(
    space="my-space",
    external_id="my-data-model",
    version="1",
    query="""
        query {
            listEquipment(limit: 10) {
                items {
                    name
                    manufacturer
                }
            }
        }
    """
)

# Schema introspection
schema = client.data_modeling.graphql.introspect(
    space="my-space",
    external_id="my-data-model",
    version="1"
)
```

#### .NET SDK (Official)

The official .NET SDK has **no GraphQL support**.

#### .NET SDK (This Fork)

This fork adds integrated GraphQL methods to the DataModels resource:

```csharp
// Execute typed query
var result = await client.DataModels.GraphQLQuery<MyResponseType>(
    space: "my-space",
    externalId: "my-data-model",
    version: "1",
    query: @"
        query {
            listEquipment(limit: 10) {
                items {
                    name
                    manufacturer
                }
            }
        }
    "
);

// Raw query (returns JsonElement)
var raw = await client.DataModels.GraphQLQueryRaw(space, modelId, version, query);

// Schema introspection
var schema = await client.DataModels.GraphQLIntrospect(space, modelId, version);
```

#### Comparison

| Python SDK Method                               | This Fork's Equivalent                          |
| ----------------------------------------------- | ----------------------------------------------- |
| `client.data_modeling.graphql.query(...)`       | `client.DataModels.GraphQLQuery<T>(...)`        |
| `client.data_modeling.graphql.query(...)` (raw) | `client.DataModels.GraphQLQueryRaw(...)`        |
| `client.data_modeling.graphql.introspect(...)`  | `client.DataModels.GraphQLIntrospect(...)`      |

**Gap Status: ✅ Closed** (Fully integrated into the Oryx HTTP pipeline)

---

### 4. Async Streaming (IAsyncEnumerable)

#### Python SDK

The Python SDK supports async generators for streaming large result sets:

```python
async for item in client.data_modeling.instances.iterate(
    view=my_view,
    chunk_size=1000
):
    process(item)
```

#### .NET SDK (Official)

No async streaming support.

#### .NET SDK (This Fork)

**Not implemented.** The SDK targets .NET Standard 2.0, which uses C# 7.3. `IAsyncEnumerable` requires C# 8.0+.

**Gap Status: ❌ Cannot close** (would require SDK to target newer .NET version)

---

## Features Already at Parity

The following features already exist in the official .NET SDK and did **not** need to be added:

| Feature       | .NET SDK Method                                              |
| ------------- | ------------------------------------------------------------ |
| Search API    | `client.DataModels.SearchInstances<T>()`                     |
| Aggregate API | `client.DataModels.AggregateInstances()`                     |
| Query API     | `client.DataModels.QueryInstances<T>()`                      |
| Sync API      | `client.DataModels.SyncInstances<T>()`                       |
| Filter Types  | `EqualsFilter`, `AndFilter`, `RangeFilter`, etc.             |
| Query Types   | `QueryNodeTableExpression`, `QueryEdgeTableExpression`, etc. |

---

## Summary

| Gap             | Python SDK Feature                     | Fork Resolution                          |
| --------------- | -------------------------------------- | ---------------------------------------- |
| Fluent filters  | `cognite.client.data_modeling.filters` | `FilterBuilder` class                    |
| Sync modes      | `sync_mode` parameter                  | `SyncMode` enum + `SyncQuery` properties |
| Backfill sort   | `backfill_sort` parameter              | `SyncBackfillSort` class                 |
| GraphQL         | `client.data_modeling.graphql`         | `GraphQLResource` class                  |
| Async streaming | `async for item in ...`                | ❌ Not possible (C# version constraint)   |

This fork closes **4 of 5** identified gaps. The async streaming gap cannot be closed without changes to the SDK's target framework.
