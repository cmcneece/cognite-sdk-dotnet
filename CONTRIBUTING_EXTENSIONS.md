# Data Modeling Extensions

This fork adds Data Modeling features to bring the .NET SDK closer to feature parity with the [Cognite Python SDK](https://cognite-sdk-python.readthedocs-hosted.com/).

## Motivation

The Cognite Python SDK includes several Data Modeling conveniences that are not present in the official .NET SDK:

- **Fluent filter building** - Python SDK has `Filter.Equals()`, `Filter.And()`, etc.
- **Sync modes** - Python SDK supports `onePhase`, `twoPhase`, and `noBackfill` sync modes
- **GraphQL queries** - Python SDK has a GraphQL client for Data Models

This fork adds these capabilities to the .NET SDK while maintaining compatibility with the existing codebase.

## AI Assistance Disclosure

This code was developed with AI assistance (Claude via Cursor). See `docs/extensions/DEVELOPMENT_PROCESS.md` for the full development history and QA process.

## Features

### 1. FilterBuilder - Fluent Filter API

Build DMS filters using a fluent, type-safe API:

```csharp
using CogniteSdk.DataModels;

var filter = FilterBuilder.Create()
    .HasData(myView)
    .And(FilterBuilder.Create()
        .Equals(myView, "status", "active")
        .Range(myView, "temperature", gte: 20.0, lte: 30.0))
    .Build();  // Returns IDMSFilter

// Use with Query API
var query = new Query
{
    With = new Dictionary<string, IQueryTableExpression>
    {
        { "result", new QueryNodeTableExpression
        {
            Nodes = new QueryNodes { Filter = filter }
        }}
    },
    // ...
};
```

**Supported Methods:**

| Method | Filter Type | Example |
|--------|-------------|---------|
| `HasData()` | HasDataFilter | `.HasData(view1, view2)` |
| `Equals()` | EqualsFilter | `.Equals(view, "prop", value)` |
| `In()` | InFilter | `.In(view, "prop", "a", "b", "c")` |
| `Range()` | RangeFilter | `.Range(view, "prop", gte: 0, lt: 100)` |
| `Prefix()` | PrefixFilter | `.Prefix(view, "name", "test_")` |
| `Exists()` | ExistsFilter | `.Exists(view, "optionalProp")` |
| `ContainsAny()` | ContainsAnyFilter | `.ContainsAny(view, "tags", "a", "b")` |
| `ContainsAll()` | ContainsAllFilter | `.ContainsAll(view, "tags", "x", "y")` |
| `And()` | AndFilter | `.And(filter1, filter2)` |
| `Or()` | OrFilter | `.Or(filter1, filter2)` |
| `Not()` | NotFilter | `.Not(filterToNegate)` |
| `Nested()` | NestedFilter | `.Nested(scope, innerFilter)` |
| `MatchAll()` | MatchAllFilter | `.MatchAll()` |

### 2. SyncQuery Extensions

Extended `SyncQuery` with sync modes and backfill sorting:

```csharp
using CogniteSdk.DataModels;

var syncQuery = new SyncQuery
{
    // Sync mode: onePhase (default), twoPhase, or noBackfill
    Mode = SyncMode.twoPhase,
    
    // Backfill sort (for twoPhase mode)
    BackfillSort = new[] 
    { 
        new SyncBackfillSort 
        { 
            Property = new[] { "mySpace", "myView/1", "timestamp" },
            Direction = SortDirection.ascending,
            NullsFirst = false
        } 
    },
    
    // Allow expired cursors (use with caution)
    AllowExpiredCursorsAndAcceptMissedDeletes = true,
    
    // Standard SyncQuery properties...
    With = { ... },
    Select = { ... }
};

var result = await client.DataModels.SyncInstances<MyType>(syncQuery);
```

**Sync Modes:**

| Mode | Description |
|------|-------------|
| `onePhase` | Default single-pass sync |
| `twoPhase` | Two-stage sync optimized for indexed filters |
| `noBackfill` | Skip backfill, only return new changes |

> **Note:** The `mode` field is not yet supported by all CDF API versions. Types are included for forward compatibility.

### 3. GraphQL Resource

Execute GraphQL queries against CDF Data Models:

```csharp
using CogniteSdk.Resources.DataModels;

// Create GraphQL resource
var graphql = new GraphQLResource(
    httpClient,
    project: "my-project",
    baseUrl: "https://bluefield.cognitedata.com",
    tokenProvider: async (ct) => await GetAccessTokenAsync()
);

// Execute typed query
var result = await graphql.QueryAsync<MyResponseType>(
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

// Or get raw JSON
var raw = await graphql.QueryRawAsync(space, modelId, version, query);

// Schema introspection
var schema = await graphql.IntrospectAsync(space, modelId, version);
```

## Installation

```bash
git clone https://github.com/cmcneece/cognite-sdk-dotnet.git
cd cognite-sdk-dotnet
git checkout feature/data-modeling-extensions
dotnet build
```

## Running Tests

```bash
# Unit tests (no credentials required)
dotnet test CogniteSdk/test/csharp/ --filter "FullyQualifiedName~Test.CSharp.Unit"

# Integration tests (requires .env file)
source test_auth_env.sh
dotnet test CogniteSdk/test/csharp/ --filter "IntegrationTests"
```

### Setting Up Integration Test Credentials

Create a `.env` file in the repository root:

```bash
CDF_CLUSTER=bluefield
CDF_PROJECT=<your-project>
TENANT_ID=<your-azure-tenant-id>
CLIENT_ID=<your-client-id>
CLIENT_SECRET=<your-client-secret>
```

## PR Strategy

These extensions are designed to be submitted as **6 independent PRs** to the official SDK, each under 520 lines for easy review.

### Summary

| PR | Feature | Files | Lines | Dependencies |
|----|---------|-------|-------|--------------|
| 1 | FilterBuilder | `FilterBuilder.cs` | 471 | None |
| 2 | FilterBuilder Tests | `FilterBuilderTests.cs` | 291 | PR 1 |
| 3 | SyncQuery Extensions | `Query.cs`, `SyncQueryTests.cs` | ~257 | None |
| 4 | GraphQL Resource | `GraphQL.cs`, `GraphQLResource.cs` | 346 | None |
| 5 | FilterBuilder Integration Tests | `FilterBuilderIntegrationTests.cs` | 519 | PR 1, PR 2 |
| 6 | Sync + GraphQL Integration Tests | `SyncGraphQLIntegrationTests.cs` | 328 | PR 3, PR 4 |

### Submission Order

```
PR 1 ──┬── PR 2 ──── PR 5
       │
PR 3 ──┼── PR 6
       │
PR 4 ──┘
```

PRs 1, 3, and 4 can be submitted in parallel as they have no dependencies on each other.

---

### PR 1: FilterBuilder - Fluent Filter API

**Title:** `feat(datamodels): add FilterBuilder fluent API for DMS filters`

**Files:**
- `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` (471 lines)

**Description:**
```markdown
## Summary
Adds a fluent API for building Data Model filters, bringing the .NET SDK closer to feature parity with the Python SDK's `cognite.client.data_modeling.filters` module.

## Motivation
The Python SDK provides convenient filter builder methods like `Filter.Equals()`, `Filter.And()`, etc. This PR adds equivalent functionality to the .NET SDK.

## Changes
- Add `FilterBuilder` class with fluent methods for all DMS filter types
- Returns `IDMSFilter` for use with existing Query/Search APIs
- Supports HasData, Equals, In, Range, Prefix, Exists, ContainsAny, ContainsAll, And, Or, Not, Nested, MatchAll
- Supports parameterized queries

## Usage
```csharp
var filter = FilterBuilder.Create()
    .HasData(myView)
    .And(FilterBuilder.Create()
        .Equals(myView, "status", "active")
        .Range(myView, "temperature", gte: 20.0, lte: 30.0))
    .Build();
```

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

---

### PR 2: FilterBuilder Unit Tests

**Title:** `test(datamodels): add FilterBuilder unit tests`

**Files:**
- `CogniteSdk/test/csharp/FilterBuilderTests.cs` (291 lines)

**Dependencies:** PR 1

**Description:**
```markdown
## Summary
Adds comprehensive unit tests for the FilterBuilder fluent API.

## Test Coverage
- 26 unit tests covering all filter methods
- Tests for filter composition (And, Or, Not, Nested)
- Tests for value types (string, double, long, bool)
- Tests for parameterized queries
- Edge cases (empty filters, null handling)

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

---

### PR 3: SyncQuery Extensions

**Title:** `feat(datamodels): add SyncMode and SyncBackfillSort to SyncQuery`

**Files:**
- `CogniteSdk.Types/DataModels/Query/Query.cs` (changes only, ~100 lines added)
- `CogniteSdk/test/csharp/SyncQueryTests.cs` (157 lines)

**Description:**
```markdown
## Summary
Extends `SyncQuery` with sync modes and backfill sorting, matching the Python SDK's sync capabilities.

## Motivation
The Python SDK supports `sync_mode` parameter with values `onePhase`, `twoPhase`, and `noBackfill`. This PR adds equivalent functionality.

## Changes
- Add `SyncMode` enum (onePhase, twoPhase, noBackfill)
- Add `SyncBackfillSort` class with property path validation
- Add `Mode`, `BackfillSort`, and `AllowExpiredCursorsAndAcceptMissedDeletes` properties to `SyncQuery`

## Usage
```csharp
var syncQuery = new SyncQuery
{
    Mode = SyncMode.twoPhase,
    BackfillSort = new[] { new SyncBackfillSort { Property = new[] { "space", "view/1", "prop" } } },
    // ...
};
```

## Note
The `mode` field is forward-compatible; API support may vary by CDF cluster version.

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

---

### PR 4: GraphQL Resource

**Title:** `feat(datamodels): add GraphQL resource for Data Model queries`

**Files:**
- `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` (143 lines)
- `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` (203 lines)

**Description:**
```markdown
## Summary
Adds a GraphQL client for executing queries against CDF Data Models, matching the Python SDK's `client.data_modeling.graphql` functionality.

## Motivation
The Python SDK provides a GraphQL endpoint for Data Models. This PR adds equivalent functionality to the .NET SDK.

## Changes
- Add `GraphQLRequest`, `GraphQLResponse<T>`, `GraphQLRawResponse`, and `GraphQLError` types
- Add `GraphQLResource` with `QueryAsync<T>`, `QueryRawAsync`, and `IntrospectAsync` methods
- Standalone implementation using HttpClient (not integrated into Oryx pipeline)

## Usage
```csharp
var graphql = new GraphQLResource(httpClient, project, baseUrl, tokenProvider);
var result = await graphql.QueryAsync<MyType>(space, modelId, version, query);
```

## Design Decision
Implemented as standalone class to minimize changes to the F#/Oryx layer. The trade-off is that GraphQL is not accessible via `client.GraphQL`.

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

---

### PR 5: FilterBuilder Integration Tests

**Title:** `test(datamodels): add FilterBuilder integration tests`

**Files:**
- `CogniteSdk/test/csharp/FilterBuilderIntegrationTests.cs` (519 lines)

**Dependencies:** PR 1, PR 2

**Description:**
```markdown
## Summary
Adds integration tests for FilterBuilder against a live CDF cluster.

## Test Coverage
- 7 integration tests
- Tests filter queries against actual CDF Data Models
- Creates test space/container/view/instances, cleans up after
- Tests: HasData, Equals, Range, Prefix, And, Or, Not filters

## Prerequisites
Requires `.env` file with CDF credentials (see CONTRIBUTING_EXTENSIONS.md).

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

---

### PR 6: Sync + GraphQL Integration Tests

**Title:** `test(datamodels): add Sync and GraphQL integration tests`

**Files:**
- `CogniteSdk/test/csharp/SyncGraphQLIntegrationTests.cs` (328 lines)

**Dependencies:** PR 3, PR 4

**Description:**
```markdown
## Summary
Adds integration tests for SyncQuery extensions and GraphQL resource against a live CDF cluster.

## Test Coverage
- 5 integration tests (2 Sync, 3 GraphQL)
- Sync: Basic sync, sync with cursor
- GraphQL: Query execution, raw query, schema introspection
- Creates test data model, cleans up after

## Prerequisites
Requires `.env` file with CDF credentials (see CONTRIBUTING_EXTENSIONS.md).

## Note
SyncMode tests are limited as the `mode` field is not yet supported on all clusters.

## AI Disclosure
This code was developed with AI assistance (Claude via Cursor).
```

## File Reference

| File | Description |
|------|-------------|
| `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` | Fluent filter builder |
| `CogniteSdk.Types/DataModels/Query/Query.cs` | SyncMode, SyncBackfillSort additions |
| `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` | GraphQL request/response types |
| `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` | GraphQL query execution |

## Limitations

- **IAsyncEnumerable**: Not implemented (requires C# 8.0+, SDK targets .NET Standard 2.0)
- **GraphQL Resource**: Standalone implementation, not integrated into Oryx pipeline
- **SyncMode**: Forward-compatible types, API support varies by CDF version
