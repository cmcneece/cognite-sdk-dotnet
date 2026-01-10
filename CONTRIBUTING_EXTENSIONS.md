# Contributing to Cognite .NET SDK Extensions

**Status**: 🟡 Internal Review  
**Author**: Colin McNeece (@cmcneece)

---

## Overview

This repository contains proposed Data Modeling extensions for the official [Cognite .NET SDK](https://github.com/cognitedata/cognite-sdk-dotnet).

### What's Included

| Feature | Description | Lines | Tests |
|---------|-------------|-------|-------|
| FilterBuilder | Fluent API for DMS filters | ~420 | 32 |
| GraphQL | Query Data Models via GraphQL | ~285 | 16 |
| Sync (Basic) | Real-time sync with IAsyncEnumerable | ~380 | 12 |
| Sync (Advanced) | Sync modes, backfillSort | ~170 | 6 |
| QueryBuilder (Basic) | Fluent graph queries | ~350 | 22 |
| QueryBuilder (Advanced) | Parameters, recursive traversal | ~200 | 13 |
| Search | Full-text and property search | ~380 | 16 |
| Aggregate | Count, sum, avg, histogram, groupBy | ~460 | 10 |
| **Total** | | **~2,625** | **127** |

---

## Contribution Phases

### Phase 1: Internal Review (Current)

**Goal**: Get feedback from colleagues before any external submission.

- Share this private repo with team members
- Gather feedback on approach and code quality
- Address concerns before proceeding

**Duration**: 1-2 weeks

### Phase 2: Community Discussion

**Goal**: Gauge maintainer interest before submitting code.

Open a GitHub Discussion on the official repo:
- Describe the proposed additions
- Link to this repository
- Ask for feedback on approach

**Only proceed to Phase 3 if maintainers are receptive.**

### Phase 3: PR Submission

**Goal**: Submit code in small, reviewable chunks.

See [docs/extensions/PULL_REQUESTS.md](docs/extensions/PULL_REQUESTS.md) for the 9 PR breakdown.

---

## AI Assistance Disclosure

This code was developed with AI assistance (Claude/Cursor). All PRs will include:

> ⚠️ **AI Assistance Disclosure**
> 
> This code was developed with the assistance of AI tools. All code has been:
> - Reviewed and validated by a human developer
> - Tested with 127 unit tests
> - Verified against CDF API documentation

---

## How to Provide Feedback

If you have access to this repository:

1. **Open an Issue** - For bugs, concerns, or suggestions
2. **Comment on Code** - Use GitHub's code review features
3. **Direct Message** - Contact Colin McNeece directly

---

## Repository Structure

```
├── CogniteSdk.Extensions/               # Resource implementations
├── CogniteSdk.Types.Extensions/         # Type definitions
├── Test/
│   └── CogniteSdk.Extensions.Tests/     # 127 unit tests
├── Examples/
│   └── DataModeling/                    # Usage examples
├── docs/
│   └── extensions/
│       ├── PULL_REQUESTS.md             # All 9 PRs described
│       ├── CURSOR_SUBMISSION_GUIDE.md   # AI agent instructions
│       └── FEATURE_PARITY_ANALYSIS.md   # Python SDK comparison
├── CONTRIBUTING_EXTENSIONS.md           # This file
└── cognite-sdk-dotnet.sln               # Solution file (includes extensions)
```

---

## Running Tests

```bash
cd cognite-sdk-dotnet
git checkout feature/data-modeling-extensions
dotnet test Test/CogniteSdk.Extensions.Tests/CogniteSdk.Extensions.Tests.csproj
# Expected: 127 tests pass
```

---

## Code Quality Verification

All code has been verified:

- [x] `ConfigureAwait(false)` on every await
- [x] Null checks on all constructor parameters
- [x] Input validation on all public methods
- [x] XML documentation on all public APIs
- [x] Request overloads validate same as helper methods
- [x] No hardcoded credentials
- [x] Shared HttpClient (no per-call instantiation)

---

*Last updated: January 2026*
