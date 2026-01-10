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

The extensions are designed to be submitted as **6 independent, human-reviewable PRs**, each under 500 lines for manageable review.

### PR Overview

| PR | Title | Files | Lines | Tests |
|----|-------|-------|-------|-------|
| 1 | FilterBuilder Fluent API | 1 | 471 | - |
| 2 | FilterBuilder Unit Tests | 1 | 291 | 26 unit |
| 3 | SyncQuery Extensions | 2 | ~257 | 13 unit |
| 4 | GraphQL Resource | 2 | 346 | - |
| 5 | FilterBuilder Integration Tests | 1 | 519 | 7 integration |
| 6 | Sync + GraphQL Integration Tests | 1 | 328 | 5 integration |

---

### PR 1: FilterBuilder Fluent API

**Purpose**: Add a fluent builder for constructing DMS filters.

**Files**:
- `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` (471 lines)

**Dependencies**: None. Uses existing `IDMSFilter` types.

**Review Focus**:
- Fluent API design patterns
- Method chaining and immutability
- Null handling and validation
- JSON serialization compatibility with existing filter types

---

### PR 2: FilterBuilder Unit Tests

**Purpose**: Unit tests for FilterBuilder.

**Files**:
- `CogniteSdk/test/csharp/FilterBuilderTests.cs` (291 lines)

**Dependencies**: PR 1

**Review Focus**:
- Test coverage for all filter operations
- Edge case handling
- JSON serialization verification

**Tests**: 26 unit tests covering: Equals, In, Range, Prefix, Exists, ContainsAny, ContainsAll, HasData, And, Or, Not, Nested, Parameter.

---

### PR 3: SyncQuery Extensions

**Purpose**: Extend `SyncQuery` with sync modes and backfill sorting.

**Files**:
- `CogniteSdk.Types/DataModels/Query/Query.cs` (~75 lines added)
- `CogniteSdk/test/csharp/SyncQueryTests.cs` (182 lines)

**Dependencies**: None. Extends existing `SyncQuery` class.

**Review Focus**:
- `SyncMode` enum JSON serialization (camelCase via `JsonPropertyName`)
- `SyncBackfillSort` property structure
- Backward compatibility with existing `SyncQuery` usage

**Tests**: 13 unit tests covering serialization and validation (including 5 property validation tests).

**Note**: The `mode` field is not yet supported by CDF API on all clusters. Types are added for forward compatibility.

---

### PR 4: GraphQL Resource

**Purpose**: Add a resource for executing GraphQL queries against Data Models.

**Files**:
- `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` (144 lines)
- `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` (202 lines)

**Dependencies**: None. Standalone resource.

**Review Focus**:
- GraphQL request/response type design
- Error handling and `GraphQLError` structure
- `HttpClient` usage pattern

**Architectural Note**: `GraphQLResource` uses a standalone `HttpClient` rather than the SDK's Oryx HTTP pipeline because:
1. GraphQL uses a different URL structure
2. Request/response formats differ from REST endpoints
3. The resource is instantiated directly rather than through the main `Client`

---

### PR 5: FilterBuilder Integration Tests

**Purpose**: Integration tests for FilterBuilder against live CDF.

**Files**:
- `CogniteSdk/test/csharp/FilterBuilderIntegrationTests.cs` (519 lines)

**Dependencies**: PR 1, PR 2

**Review Focus**:
- Test data setup and cleanup patterns
- CDF API interaction
- Known limitations documented in comments

**Tests**: 7 integration tests (Equals, And, Range, Prefix, Or, Not filters).

---

### PR 6: Sync + GraphQL Integration Tests

**Purpose**: Integration tests for SyncQuery and GraphQL.

**Files**:
- `CogniteSdk/test/csharp/SyncGraphQLIntegrationTests.cs` (328 lines)

**Dependencies**: PR 3, PR 4

**Review Focus**:
- SyncQuery integration patterns
- GraphQL introspection and query handling
- Error response handling

**Tests**: 2 SyncQuery tests + 3 GraphQL tests (introspection, typed query, error handling).

---

### Submission Order

**Recommended: Sequential with dependencies**

```
PR 1 (FilterBuilder code)
    ↓
PR 2 (FilterBuilder unit tests)  +  PR 3 (SyncQuery)  +  PR 4 (GraphQL)
    ↓                                    ↓                    ↓
PR 5 (FilterBuilder integration)    PR 6 (Sync + GraphQL integration)
```

**Timeline**:
1. Submit PR 1 first (no dependencies)
2. After PR 1 merges: Submit PRs 2, 3, 4 in parallel
3. After PRs 2+3+4 merge: Submit PRs 5, 6

---

### Creating PRs from This Fork

Each PR should be created from this fork targeting `cognitedata/cognite-sdk-dotnet:master`.

```bash
# Example: Create branch for PR 1 (FilterBuilder code only)
git checkout master
git checkout -b pr/filterbuilder-code
git checkout feature/data-modeling-extensions -- CogniteSdk.Types/DataModels/Query/FilterBuilder.cs
git commit -m "feat(datamodels): add FilterBuilder fluent API for DMS filters"
git push origin pr/filterbuilder-code
# Then create PR via GitHub UI
```

---

### PR Template

```markdown
## Summary
[Brief description]

## Changes
- [File list]

## Testing
- [ ] Unit tests (X tests) - included / separate PR
- [ ] Integration tests - included / separate PR

## AI Assistance Disclosure
This code was developed with AI assistance (Claude via Cursor). 
Human review and validation is recommended.

## Related PRs
- Depends on: #XX (if applicable)
- Related: #YY (if applicable)
```

## AI Assistance Disclosure

This code was developed with AI assistance (Claude via Cursor). Human review and validation is recommended before using in production. See `docs/extensions/DEVELOPMENT_PROCESS.md` for details.

## File Summary

| File | Lines | Description |
|------|-------|-------------|
| `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` | 471 | Fluent filter builder |
| `CogniteSdk.Types/DataModels/Query/Query.cs` | +75 | Extended with SyncMode, SyncBackfillSort |
| `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` | 144 | GraphQL request/response types |
| `CogniteSdk/src/Resources/DataModels/GraphQLResource.cs` | 202 | GraphQL query execution |
| `CogniteSdk/test/csharp/FilterBuilderTests.cs` | 291 | FilterBuilder unit tests (26 tests) |
| `CogniteSdk/test/csharp/SyncQueryTests.cs` | 182 | SyncQuery extension unit tests (13 tests) |
| `CogniteSdk/test/csharp/FilterBuilderIntegrationTests.cs` | 519 | FilterBuilder integration tests (7 tests) |
| `CogniteSdk/test/csharp/SyncGraphQLIntegrationTests.cs` | 328 | SyncQuery + GraphQL integration tests (5 tests) |
