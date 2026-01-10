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

## PR Strategy

The extensions are designed to be submitted as **4 independent, human-reviewable PRs**. Each PR is self-contained, includes its own tests, and can be merged independently.

### PR Overview

| PR | Title | Files | Lines | Tests |
|----|-------|-------|-------|-------|
| 1 | FilterBuilder Fluent API | 2 | ~760 | 26 unit |
| 2 | SyncQuery Extensions | 2 | ~180 | 8 unit |
| 3 | GraphQL Resource | 2 | ~350 | - |
| 4 | Integration Tests | 1 | ~825 | 11 integration |

### PR 1: FilterBuilder Fluent API

**Purpose**: Add a fluent builder for constructing DMS filters.

**Files**:
- `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` (471 lines)
- `CogniteSdk/test/csharp/FilterBuilderTests.cs` (291 lines)

**Dependencies**: None. Uses existing `IDMSFilter` types.

**Review Focus**:
- Fluent API design patterns
- Null handling and validation
- JSON serialization compatibility with existing filter types

**Tests**: 26 unit tests covering all filter operations (Equals, In, Range, Prefix, Exists, ContainsAny, ContainsAll, HasData, And, Or, Not, Nested, Parameter).

---

### PR 2: SyncQuery Extensions

**Purpose**: Extend `SyncQuery` with sync modes and backfill sorting for the Sync API.

**Files**:
- `CogniteSdk.Types/DataModels/Query/Query.cs` (~50 lines added)
- `CogniteSdk/test/csharp/SyncQueryTests.cs` (134 lines)

**Dependencies**: None. Extends existing `SyncQuery` class.

**Review Focus**:
- `SyncMode` enum JSON serialization (camelCase)
- `SyncBackfillSort` property structure
- Backward compatibility with existing `SyncQuery` usage

**Tests**: 8 unit tests covering serialization and validation.

**Note**: The `mode` field is not yet supported by CDF API on all clusters. Types are added for forward compatibility.

---

### PR 3: GraphQL Resource

**Purpose**: Add a dedicated resource for executing GraphQL queries against Data Models.

**Files**:
- `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` (144 lines)
- `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` (202 lines)

**Dependencies**: None. Standalone resource.

**Review Focus**:
- GraphQL request/response type design
- Error handling and `GraphQLError` structure
- `HttpClient` usage (standalone, not using Oryx pipeline)

**Architectural Note**: `GraphQLResource` uses a standalone `HttpClient` rather than the SDK's Oryx HTTP pipeline. This is because:
1. GraphQL queries use a different URL structure (`/api/v1/projects/{project}/models/spaces/{space}/datamodels/{model}/versions/{version}/graphql`)
2. Request/response formats differ from REST endpoints
3. The resource is instantiated directly rather than through the main `Client`

---

### PR 4: Integration Tests

**Purpose**: Add integration tests validating all extensions against live CDF.

**Files**:
- `CogniteSdk/test/csharp/DataModelsExtensionsIntegrationTests.cs` (825 lines)

**Dependencies**: PRs 1-3 (or can be submitted as a single combined PR).

**Review Focus**:
- Test data setup and cleanup
- CDF API interaction patterns
- Known API limitations documented in test comments

**Tests**: 
- 7 FilterBuilder integration tests
- 2 SyncQuery integration tests  
- 3 GraphQL integration tests (introspection, typed query, error handling)

---

### Submission Order

PRs can be submitted in parallel or sequentially:

**Option A: Sequential** (Recommended for smaller review burden)
1. PR 1 (FilterBuilder) → Merge
2. PR 2 (SyncQuery) → Merge
3. PR 3 (GraphQL) → Merge
4. PR 4 (Integration Tests) → Merge

**Option B: Parallel with PR 4 last**
- Submit PRs 1, 2, 3 in parallel
- Submit PR 4 after PRs 1-3 are merged

**Option C: Single combined PR**
- All changes in one PR (~2200 lines)
- Only recommended if reviewers prefer comprehensive review

### Creating PRs from This Fork

Each PR should be created from this fork's `feature/data-modeling-extensions` branch targeting the official `cognitedata/cognite-sdk-dotnet` repository's `master` branch.

To extract files for individual PRs:
```bash
# Example: Create branch for PR 1 (FilterBuilder)
git checkout master
git checkout -b pr/filterbuilder
git checkout feature/data-modeling-extensions -- CogniteSdk.Types/DataModels/Query/FilterBuilder.cs
git checkout feature/data-modeling-extensions -- CogniteSdk/test/csharp/FilterBuilderTests.cs
git commit -m "feat(datamodels): add FilterBuilder fluent API for DMS filters"
```

### PR Template

Each PR should include:

```markdown
## Summary
[Brief description of the feature]

## Changes
- [List of files changed]

## Testing
- [X] Unit tests added (X tests)
- [ ] Integration tests (separate PR / included)

## AI Assistance Disclosure
This code was developed with AI assistance (Claude via Cursor). 
Human review and validation is recommended.

## Documentation
- See CONTRIBUTING_EXTENSIONS.md for usage examples
- See docs/extensions/DEVELOPMENT_PROCESS.md for development details
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
