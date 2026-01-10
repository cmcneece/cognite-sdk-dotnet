# Contributing: Data Modeling Extensions

This document describes the Data Modeling extensions contributed to this fork of the Cognite .NET SDK.

## Scope of Changes

After analysis of the existing SDK, this contribution focuses on **net new functionality** not present in the official SDK:

### 1. FilterBuilder (Fluent API)

**Location**: `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs`

A fluent builder for constructing DMS filters. The official SDK has filter types (`EqualsFilter`, `AndFilter`, etc.) but no fluent API for building them.

```csharp
var filter = FilterBuilder.Create()
    .HasData(myView)
    .And(FilterBuilder.Create().Equals(myView, "status", "active"))
    .Build();
```

### 2. Sync API Extensions

**Location**: `CogniteSdk.Types/DataModels/Query/Query.cs`

Extensions to `SyncQuery` for sync modes and backfill sorting. The official SDK has `SyncQuery` but lacks:
- `SyncMode` enum (onePhase, twoPhase, noBackfill)
- `SyncBackfillSort` class for optimized backfill ordering
- `AllowExpiredCursorsAndAcceptMissedDeletes` property

```csharp
var syncQuery = new SyncQuery
{
    Mode = SyncMode.twoPhase,
    BackfillSort = new[] { new SyncBackfillSort { Property = propPath, Direction = SortDirection.ascending } },
    AllowExpiredCursorsAndAcceptMissedDeletes = true
};
```

### 3. GraphQL Resource

**Location**: 
- Types: `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs`
- Resource: `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs`

A dedicated resource for executing GraphQL queries against CDF Data Models. The official SDK has no GraphQL wrapper.

```csharp
var graphql = new GraphQLResource(httpClient, project, baseUrl, tokenProvider);
var result = await graphql.QueryAsync<MyType>(space, modelId, version, "{ listMyView { items { name } } }");
```

## What Was NOT Changed

The following features **already exist** in the official SDK and were not modified:
- Search API (`DataModelsResource.SearchInstances<T>()`)
- Aggregate API (`DataModelsResource.AggregateInstances()`)
- Query API (`DataModelsResource.QueryInstances<T>()`)
- Sync API (`DataModelsResource.SyncInstances<T>()`)
- Filter types (`EqualsFilter`, `AndFilter`, `HasDataFilter`, etc.)
- Query types (`QueryNodeTableExpression`, `QueryEdgeTableExpression`, etc.)

## Limitations

### IAsyncEnumerable Streaming

`IAsyncEnumerable` streaming was not implemented because:
- The SDK targets .NET Standard 2.0
- .NET Standard 2.0 uses C# 7.3
- `IAsyncEnumerable` requires C# 8.0+

To add streaming support, the SDK would need to target a newer .NET version or provide a separate extension package targeting .NET 5.0+.

## Running Tests

Unit tests for the new functionality:

```bash
# Run only unit tests (no CDF credentials required)
dotnet test CogniteSdk/test/csharp/CogniteSdk.Test.CSharp.csproj --filter "FullyQualifiedName~Test.CSharp.Unit"
```

Integration tests require CDF credentials. Create a `.env` file with your credentials:

```bash
# .env file format
CDF_CLUSTER=bluefield
CDF_PROJECT=<your-project>
TENANT_ID=<your-tenant-id>
CLIENT_ID=<your-client-id>
CLIENT_SECRET=<your-client-secret>
```

Then run:
```bash
source test_auth_env.sh
dotnet test CogniteSdk/test/csharp/CogniteSdk.Test.CSharp.csproj
```

## Building

```bash
# Restore Paket dependencies
dotnet tool restore

# Build all projects
dotnet build

# Or build specific projects
dotnet build CogniteSdk.Types/CogniteSdk.Types.csproj
dotnet build CogniteSdk/src/CogniteSdk.csproj
```

## AI Assistance Disclosure

This code was developed with AI assistance (Claude via Cursor). Human review and validation is recommended before using in production. See `docs/extensions/DEVELOPMENT_PROCESS.md` for details.

## File Summary

| File | Description |
|------|-------------|
| `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` | Fluent filter builder |
| `CogniteSdk.Types/DataModels/Query/Query.cs` | Extended with SyncMode, SyncBackfillSort |
| `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` | GraphQL request/response types |
| `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` | GraphQL query execution |
| `CogniteSdk/test/csharp/FilterBuilderTests.cs` | FilterBuilder unit tests (26 tests) |
| `CogniteSdk/test/csharp/SyncQueryTests.cs` | SyncQuery extension unit tests (8 tests) |
| `CogniteSdk/test/csharp/DataModelsExtensionsIntegrationTests.cs` | Integration tests (11 tests) |
