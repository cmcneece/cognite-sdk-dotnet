# Pull Request Breakdown

This document describes the 9 PRs that will be submitted to the official Cognite .NET SDK.

---

## PR Dependency Graph

```
PR1: FilterBuilder ─────┬──► PR4: QueryBuilder (Basic)
                        │           │
PR2: GraphQL ───────────┤           ▼
                        │    PR5: QueryBuilder (Advanced)
PR3: Sync (Basic) ──────┤
        │               │
        ▼               │
PR6: Sync (Advanced)    │
                        │
PR7: Search ────────────┤
                        │
PR8: Aggregate ─────────┘
                        
PR9: Examples ──────────► (depends on all above)
```

**Key**: PRs 1, 2, 3 have NO dependencies and can be submitted/reviewed in parallel.

---

## Submission Order

| Order | PR | Dependencies | Can Submit After |
|-------|-----|--------------|------------------|
| 1 | FilterBuilder | None | Immediately |
| 2 | GraphQL | None | Immediately |
| 3 | Sync (Basic) | None | Immediately |
| 4 | QueryBuilder (Basic) | PR1 | PR1 merged |
| 5 | QueryBuilder (Advanced) | PR4 | PR4 merged |
| 6 | Sync (Advanced) | PR3 | PR3 merged |
| 7 | Search | PR1 | PR1 merged |
| 8 | Aggregate | PR1 | PR1 merged |
| 9 | Examples | All | All merged |

---

## PR 1: FilterBuilder

**Purpose**: Fluent API for building Data Model instance filters.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/Query/FilterBuilder.cs (~420 lines)
tests/CogniteSdk.Extensions.Tests/FilterBuilderTests.cs (~450 lines)
```

**Tests**: 32

**Key Features**:
- All filter operations: Equals, In, Range, Prefix, Exists, ContainsAny, ContainsAll, And, Or, Not, HasData
- ViewIdentifier overloads for type safety
- `Parameter()` helper for query parameters
- Correct 3-element property path format: `[space, "view/version", property]`

**Usage**:
```csharp
var filter = FilterBuilder.Create()
    .And(
        FilterBuilder.Create().HasData(view),
        FilterBuilder.Create().Equals(view, "status", "Running"),
        FilterBuilder.Create().Range(view, "temperature", gte: 20, lte: 100)
    )
    .Build();
```

---

## PR 2: GraphQL Client

**Purpose**: Execute GraphQL queries against Data Model endpoints.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/GraphQL/GraphQLRequest.cs (~32 lines)
src/CogniteSdk.Types.Extensions/DataModels/GraphQL/GraphQLResponse.cs (~116 lines)
src/CogniteSdk.Extensions/Resources/GraphQL.cs (~138 lines)
tests/CogniteSdk.Extensions.Tests/GraphQLTypesTests.cs (~169 lines)
```

**Tests**: 16

**Key Features**:
- Execute arbitrary GraphQL queries
- Variable support
- Error handling with locations and paths
- Schema introspection support

**Usage**:
```csharp
var result = await graphQL.QueryRawAsync(
    space: "mySpace",
    externalId: "MyModel",
    version: "1",
    query: @"query { listEquipment(first: 10) { items { name status } } }"
);
```

---

## PR 3: Sync API (Basic)

**Purpose**: Real-time synchronization of Data Model instances.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/Sync/SyncRequest.cs (~75 lines, basic)
src/CogniteSdk.Types.Extensions/DataModels/Sync/SyncResponse.cs (~85 lines)
src/CogniteSdk.Extensions/Resources/Sync.cs (~215 lines)
tests/CogniteSdk.Extensions.Tests/SyncTypesTests.cs (~160 lines, basic)
```

**Tests**: 12

**Key Features**:
- Cursor-based pagination
- `IAsyncEnumerable` streaming with `[EnumeratorCancellation]`
- Auto-generated hasData filter
- Configurable poll interval

**Usage**:
```csharp
// Streaming
await foreach (var batch in sync.StreamChangesAsync(view, pollIntervalMs: 5000, token: cts.Token))
{
    foreach (var node in batch.Response.Items["items"].Nodes)
        ProcessNode(node);
}
```

---

## PR 4: QueryBuilder (Basic)

**Purpose**: Fluent API for complex graph queries.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/Query/QueryRequest.cs (~130 lines, basic)
src/CogniteSdk.Types.Extensions/DataModels/Query/QueryResponse.cs (~50 lines)
src/CogniteSdk.Extensions/Resources/QueryBuilder.cs (~285 lines)
tests/CogniteSdk.Extensions.Tests/QueryBuilderTests.cs (~290 lines, basic)
```

**Tests**: 22

**Key Features**:
- `WithNodes()`, `WithEdges()`, `WithNodesFrom()` for graph traversal
- `Select()` for property selection (auto-generated if omitted)
- `EdgeDirection` enum (type-safe, not strings)
- Filter integration with FilterBuilder
- `Reset()` for reuse

**Usage**:
```csharp
var result = await query
    .WithNodes("equipment", equipmentView)
    .WithEdges("sensors", from: "equipment", direction: EdgeDirection.Outwards)
    .WithNodesFrom("sensorNodes", from: "sensors", chainTo: "destination")
    .Select("equipment", equipmentView, "name", "status")
    .ExecuteAsync();
```

---

## PR 5: QueryBuilder (Advanced)

**Purpose**: Query parameters and recursive edge traversal.

**Files** (additions to PR4 files):
```
src/CogniteSdk.Types.Extensions/DataModels/Query/QueryRequest.cs (+20 lines)
src/CogniteSdk.Extensions/Resources/QueryBuilder.cs (+50 lines)
tests/CogniteSdk.Extensions.Tests/QueryBuilderTests.cs (+150 lines)
```

**Tests**: 13 additional

**Key Features**:
- `WithParameter()` for query plan reuse
- `FilterBuilder.Parameter()` helper
- Recursive traversal: `maxDistance`, `nodeFilter`, `terminationFilter`, `limitEach`

**Usage**:
```csharp
// Parameters
var filter = FilterBuilder.Create()
    .Equals(view, "status", FilterBuilder.Parameter("statusParam"))
    .Build();

var result = await query
    .WithParameter("statusParam", "Running")
    .WithNodes("equipment", view, filter: filter)
    .ExecuteAsync();

// Recursive traversal
var result = await query
    .WithNodes("root", view)
    .WithEdges("connections", from: "root", direction: EdgeDirection.Outwards, maxDistance: 3)
    .ExecuteAsync();
```

---

## PR 6: Sync API (Advanced)

**Purpose**: Sync modes and advanced options.

**Files** (additions to PR3 files):
```
src/CogniteSdk.Types.Extensions/DataModels/Sync/SyncRequest.cs (+60 lines)
src/CogniteSdk.Extensions/Resources/Sync.cs (+30 lines)
tests/CogniteSdk.Extensions.Tests/SyncTypesTests.cs (+80 lines)
```

**Tests**: 6 additional

**Key Features**:
- `SyncMode` enum: OnePhase, TwoPhase, NoBackfill
- `backfillSort` for indexed queries
- `allowExpiredCursors` option

**Usage**:
```csharp
// TwoPhase mode with backfill sort
var result = await sync.SyncAsync(view, 
    mode: SyncMode.TwoPhase,
    backfillSort: new[] {
        new SyncBackfillSort { Property = new[] { "space", "container/1", "timestamp" }, Direction = "ascending" }
    }
);

// NoBackfill - only new changes
var result = await sync.SyncAsync(view, mode: SyncMode.NoBackfill);
```

---

## PR 7: Search API

**Purpose**: Full-text and property search.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/Search/SearchRequest.cs (~157 lines)
src/CogniteSdk.Types.Extensions/DataModels/Search/SearchResponse.cs (~75 lines)
src/CogniteSdk.Extensions/Resources/Search.cs (~203 lines)
tests/CogniteSdk.Extensions.Tests/SearchTypesTests.cs (~240 lines)
```

**Tests**: 16

**Key Features**:
- Full-text search with wildcards
- Property-scoped searching
- Filter integration
- Sort specification
- Implicit `ViewIdentifier` conversion

**Usage**:
```csharp
var results = await search.SearchAsync(new SearchInstancesRequest
{
    View = view,  // Implicit conversion from ViewIdentifier
    Query = "pump*",
    Properties = new[] { "name", "description" },
    Limit = 100
});
```

---

## PR 8: Aggregate API

**Purpose**: Analytics and summarization.

**Files**:
```
src/CogniteSdk.Types.Extensions/DataModels/Aggregate/AggregateRequest.cs (~121 lines)
src/CogniteSdk.Types.Extensions/DataModels/Aggregate/AggregateResponse.cs (~85 lines)
src/CogniteSdk.Extensions/Resources/Aggregate.cs (~340 lines)
tests/CogniteSdk.Extensions.Tests/AggregateTypesTests.cs (~300 lines)
```

**Tests**: 10

**Key Features**:
- Aggregations: count, sum, avg, min, max, histogram
- `GroupBy` support
- Filter and query integration
- Helper methods: `CountAsync()`, `AvgAsync()`, `SumAsync()`, `MinAsync()`, `MaxAsync()`

**Usage**:
```csharp
// GroupBy
var results = await aggregate.AggregateAsync(new AggregateInstancesRequest
{
    View = view,
    Aggregates = new[] { new AggregateOperation { Property = "*", Aggregate = "count" } },
    GroupBy = new[] { "status" }
});

// Helpers
var count = await aggregate.CountAsync(view);
var avgTemp = await aggregate.AvgAsync(view, "temperature");
```

---

## PR 9: Examples & Client Extensions

**Purpose**: Integration examples and convenience extensions.

**Files**:
```
src/CogniteSdk.Extensions/ClientExtensions.cs (~116 lines)
Examples/DataModeling/Program.cs (~400 lines)
Examples/DataModeling/DataModelingExamples.csproj (~20 lines)
```

**Tests**: 0 (examples only)

**Key Features**:
- Extension methods on `CogniteSdk.Client`
- Shared `HttpClient` for efficiency
- Comprehensive examples of all APIs
- Environment variable configuration (no hardcoded credentials)

**Usage**:
```csharp
// Extension methods
var graphQL = client.GraphQL(project, baseUrl, tokenProvider);
var sync = client.Sync(project, baseUrl, tokenProvider);
var query = client.QueryBuilder(project, baseUrl, tokenProvider);
var search = client.Search(project, baseUrl, tokenProvider);
var aggregate = client.Aggregate(project, baseUrl, tokenProvider);
```

---

## PR Template

Each PR will include this description format:

```markdown
## Summary
[Brief description]

## AI Assistance Disclosure
⚠️ This code was developed with AI assistance (Claude/Cursor). All code has been:
- Reviewed and validated by a human developer
- Tested with comprehensive unit tests
- Verified against CDF API documentation

## Changes
- [File list with descriptions]

## Testing
\`\`\`bash
dotnet test --filter "[TestClass]"
\`\`\`

## Checklist
- [ ] ConfigureAwait(false) on all awaits
- [ ] Input validation on public methods
- [ ] XML documentation complete
- [ ] Tests pass
```

---

*See [CURSOR_SUBMISSION_GUIDE.md](CURSOR_SUBMISSION_GUIDE.md) for AI agent instructions.*
