# Cursor AI Agent - PR Submission Guide

This document contains step-by-step instructions for Cursor to submit PRs to the official Cognite .NET SDK.

---

## Prerequisites

Before starting PR submission:

1. ✅ Internal review complete
2. ✅ Community discussion positive response
3. ✅ Fork of `cognitedata/cognite-sdk-dotnet` created at `cmcneece/cognite-sdk-dotnet`
4. ✅ All 127 tests passing locally

---

## Step 1: Clone the Official SDK Fork

```bash
# Clone the fork (if not already done)
git clone https://github.com/cmcneece/cognite-sdk-dotnet.git
cd cognite-sdk-dotnet

# Add upstream remote
git remote add upstream https://github.com/cognitedata/cognite-sdk-dotnet.git

# Ensure main is up to date
git fetch upstream
git checkout main
git merge upstream/main
git push origin main
```

---

## Step 2: File Mapping

Copy files from this repository to the official SDK structure:

| Source (this repo) | Destination (official SDK) |
|--------------------|---------------------------|
| `src/CogniteSdk.Types.Extensions/DataModels/` | `CogniteSdk/Types/DataModels/` |
| `src/CogniteSdk.Extensions/Resources/` | `CogniteSdk/Resources/DataModeling/` |
| `tests/CogniteSdk.Extensions.Tests/` | `CogniteSdk.Tests/` |

---

## PR Submission Instructions

### PR 1: FilterBuilder

```bash
# Create branch
git checkout main
git pull upstream main
git checkout -b feature/filterbuilder

# Copy files
cp <extensions-repo>/src/CogniteSdk.Types.Extensions/DataModels/Query/FilterBuilder.cs \
   CogniteSdk/Types/DataModels/Query/

cp <extensions-repo>/tests/CogniteSdk.Extensions.Tests/FilterBuilderTests.cs \
   CogniteSdk.Tests/

# Verify tests pass
dotnet test --filter "FilterBuilder"

# Commit
git add -A
git commit -m "feat(datamodels): add FilterBuilder for type-safe filter construction

- Add fluent API for building Data Model instance filters
- Support all filter operations: Equals, In, Range, Prefix, Exists, etc.
- Add ViewIdentifier overloads for type safety
- Add Parameter() helper for query parameters
- Correct 3-element property path format

Tests: 32 new tests"

# Push and create PR
git push origin feature/filterbuilder
```

**PR Title**: `feat(datamodels): add FilterBuilder for type-safe filter construction`

**PR Body**:
```markdown
## Summary

Adds a fluent FilterBuilder utility for constructing Data Model instance filters with type safety.

## AI Assistance Disclosure

⚠️ This code was developed with the assistance of AI tools (Claude/Cursor). All code has been:
- Reviewed and validated by a human developer
- Tested with 32 unit tests
- Verified against CDF API documentation

## Changes

- `CogniteSdk/Types/DataModels/Query/FilterBuilder.cs` - FilterBuilder implementation
- `CogniteSdk.Tests/FilterBuilderTests.cs` - Unit tests

## Features

- All filter operations: Equals, In, Range, Prefix, Exists, ContainsAny, ContainsAll, And, Or, Not, HasData
- ViewIdentifier overloads for type safety
- Parameter() helper for parameterized queries
- Correct property path format: [space, "view/version", property]

## Testing

```bash
dotnet test --filter "FilterBuilder"
```

## Checklist

- [x] ConfigureAwait(false) on all awaits (N/A - no async)
- [x] Input validation on public methods
- [x] XML documentation complete
- [x] Tests pass (32 tests)
```

---

### PR 2: GraphQL

```bash
git checkout main && git pull upstream main
git checkout -b feature/graphql

# Copy files (adjust paths as needed)
# ... GraphQL files ...

git commit -m "feat(datamodels): add GraphQL client for Data Model queries"
git push origin feature/graphql
```

**PR Title**: `feat(datamodels): add GraphQL client for Data Model queries`

---

### PR 3: Sync (Basic)

```bash
git checkout main && git pull upstream main
git checkout -b feature/sync-basic

# Copy files
# ... Sync files (basic only, not advanced) ...

git commit -m "feat(datamodels): add Sync API with IAsyncEnumerable streaming"
git push origin feature/sync-basic
```

**PR Title**: `feat(datamodels): add Sync API with IAsyncEnumerable streaming`

---

### PR 4: QueryBuilder (Basic)

**Depends on**: PR1 merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/querybuilder-basic

# Copy files
# ... QueryBuilder files (basic only) ...

git commit -m "feat(datamodels): add QueryBuilder for fluent graph queries"
git push origin feature/querybuilder-basic
```

**PR Title**: `feat(datamodels): add QueryBuilder for fluent graph queries`

---

### PR 5: QueryBuilder (Advanced)

**Depends on**: PR4 merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/querybuilder-advanced

# Add to existing files
# ... Parameter support, recursive traversal ...

git commit -m "feat(datamodels): add query parameters and recursive traversal to QueryBuilder"
git push origin feature/querybuilder-advanced
```

**PR Title**: `feat(datamodels): add query parameters and recursive traversal to QueryBuilder`

---

### PR 6: Sync (Advanced)

**Depends on**: PR3 merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/sync-advanced

# Add to existing files
# ... SyncMode, backfillSort, allowExpiredCursors ...

git commit -m "feat(datamodels): add sync modes and backfill options to Sync API"
git push origin feature/sync-advanced
```

**PR Title**: `feat(datamodels): add sync modes and backfill options to Sync API`

---

### PR 7: Search

**Depends on**: PR1 merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/search

# Copy files
# ... Search files ...

git commit -m "feat(datamodels): add Search API for full-text and property search"
git push origin feature/search
```

**PR Title**: `feat(datamodels): add Search API for full-text and property search`

---

### PR 8: Aggregate

**Depends on**: PR1 merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/aggregate

# Copy files
# ... Aggregate files ...

git commit -m "feat(datamodels): add Aggregate API for analytics and summarization"
git push origin feature/aggregate
```

**PR Title**: `feat(datamodels): add Aggregate API for analytics and summarization`

---

### PR 9: Examples

**Depends on**: All above merged

```bash
git checkout main && git pull upstream main
git checkout -b feature/datamodeling-examples

# Copy files
# ... ClientExtensions and Examples ...

git commit -m "feat(datamodels): add client extensions and usage examples"
git push origin feature/datamodeling-examples
```

**PR Title**: `feat(datamodels): add client extensions and usage examples`

---

## Handling Review Feedback

When maintainers request changes:

1. Make changes locally on the feature branch
2. Run tests: `dotnet test --filter "[FeatureName]"`
3. Commit with descriptive message: `fix: address review feedback - [description]`
4. Push: `git push origin feature/[name]`
5. Comment on PR that changes are ready for re-review

---

## Merge Order Summary

```
Week 1: PR1, PR2, PR3 (parallel, no dependencies)
Week 2: PR4 (after PR1), PR7, PR8 (after PR1)
Week 3: PR5 (after PR4), PR6 (after PR3)
Week 4: PR9 (after all)
```

---

## Verification Before Each PR

```bash
# Run all tests
dotnet test

# Check for ConfigureAwait
grep -r "await " --include="*.cs" | grep -v "ConfigureAwait" | grep -v "test" | grep -v "Test"

# Should return empty if all awaits have ConfigureAwait(false)
```

---

*This guide is for AI agent use. See [PULL_REQUESTS.md](PULL_REQUESTS.md) for human-readable descriptions.*
