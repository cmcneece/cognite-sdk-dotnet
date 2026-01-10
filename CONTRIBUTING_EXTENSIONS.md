# Data Modeling Extensions

This fork adds Data Modeling features to bring the .NET SDK closer to feature parity with the [Cognite Python SDK](https://cognite-sdk-python.readthedocs-hosted.com/).

## Motivation

The Cognite Python SDK includes several Data Modeling conveniences that are not present in the official .NET SDK:

- **Fluent filter building** - Python SDK has `Filter.Equals()`, `Filter.And()`, etc.
- **Sync modes** - Python SDK supports `onePhase`, `twoPhase`, and `noBackfill` sync modes
- **GraphQL queries** - Python SDK has a GraphQL client for Data Models

This fork adds these capabilities to the .NET SDK while maintaining compatibility with the existing codebase.

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

These extensions are designed to be submitted as **6 independent PRs** to the official SDK:

| PR | Feature | Files | Lines |
|----|---------|-------|-------|
| 1 | FilterBuilder | `FilterBuilder.cs` | 471 |
| 2 | FilterBuilder Tests | `FilterBuilderTests.cs` | 291 |
| 3 | SyncQuery Extensions | `Query.cs`, `SyncQueryTests.cs` | ~257 |
| 4 | GraphQL Resource | `GraphQL.cs`, `GraphQLResource.cs` | 346 |
| 5 | FilterBuilder Integration Tests | `FilterBuilderIntegrationTests.cs` | 519 |
| 6 | Sync + GraphQL Integration Tests | `SyncGraphQLIntegrationTests.cs` | 328 |

### Submission Order

```
PR 1 → PR 2 → PR 3 → PR 4 → PR 5 → PR 6
```

PRs 1, 3, and 4 have no dependencies and can be submitted in parallel after PR 1.

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

## AI Assistance Disclosure

This code was developed with AI assistance (Claude via Cursor). See `docs/extensions/DEVELOPMENT_PROCESS.md` for details.
