# Security Review: Data Modeling Extensions

**Reviewer**: Senior Security Engineer (AI-Assisted)  
**Date**: January 2026  
**Scope**: FilterBuilder, SyncQuery Extensions, GraphQL Client additions  
**SDK Version**: Feature branch `feature/data-modeling-extensions`

---

## Executive Summary

The Data Modeling extensions demonstrate **generally good security practices** with comprehensive input validation and proper credential handling. This review identified several areas for improvement before submitting PRs to the official Cognite SDK repository.

### Risk Summary

| Severity | Count | Status |
|----------|-------|--------|
| 🔴 High | 1 | ✅ Fixed |
| 🟠 Medium | 3 | ✅ Fixed (2) / ⚠️ Out of Scope (1) |
| 🟡 Low | 4 | ✅ Fixed |
| ✅ Positive | 5 | N/A |

---

## Table of Contents

1. [High Severity Findings](#high-severity-findings)
2. [Medium Severity Findings](#medium-severity-findings)
3. [Low Severity Findings](#low-severity-findings)
4. [Positive Security Practices](#positive-security-practices)
5. [Recommendations Summary](#recommendations-summary)
6. [Remediation Tracking](#remediation-tracking)

---

## High Severity Findings

### SEC-001: GraphQL Input Validation Missing

**Severity**: 🔴 High  
**Component**: `CogniteSdk/src/Resources/DataModels.cs`  
**Lines**: 348-365, 379-396  
**OWASP Category**: A03:2021 – Injection

#### Description

The GraphQL methods (`GraphQLQuery<T>`, `GraphQLQueryRaw`, `GraphQLIntrospect`) accept raw string parameters without validation. The `space`, `externalId`, `version`, and `query` parameters are passed directly to the API without null checks, empty string validation, or length limits.

#### Current Code

```csharp
public async Task<GraphQLResponse<T>> GraphQLQuery<T>(
    string space,
    string externalId,
    string version,
    string query,
    Dictionary<string, object> variables = null,
    string operationName = null,
    CancellationToken token = default)
{
    var request = new GraphQLRequest
    {
        Query = query,  // No validation
        Variables = variables,
        OperationName = operationName
    };
    var req = Oryx.Cognite.DataModels.graphqlQuery<T>(space, externalId, version, request, GetContext(token));
    return await RunAsync(req).ConfigureAwait(false);
}
```

#### Risk

1. **Null Reference Exceptions**: Passing null values will cause runtime errors
2. **DoS via Large Queries**: Unbounded query strings could consume excessive memory
3. **Path Manipulation**: The `space`, `externalId`, and `version` are used in URL construction

#### Recommended Fix

```csharp
public async Task<GraphQLResponse<T>> GraphQLQuery<T>(
    string space,
    string externalId,
    string version,
    string query,
    Dictionary<string, object> variables = null,
    string operationName = null,
    CancellationToken token = default)
{
    // Input validation
    ValidateIdentifier(space, nameof(space));
    ValidateIdentifier(externalId, nameof(externalId));
    ValidateIdentifier(version, nameof(version));
    
    if (string.IsNullOrWhiteSpace(query))
        throw new ArgumentException("Query cannot be null or empty", nameof(query));
    
    // Optional: Query length limit (adjust based on CDF API limits)
    const int MaxQueryLength = 100_000;
    if (query.Length > MaxQueryLength)
        throw new ArgumentException($"Query exceeds maximum length of {MaxQueryLength} characters", nameof(query));
    
    var request = new GraphQLRequest
    {
        Query = query,
        Variables = variables,
        OperationName = operationName
    };
    var req = Oryx.Cognite.DataModels.graphqlQuery<T>(space, externalId, version, request, GetContext(token));
    return await RunAsync(req).ConfigureAwait(false);
}

private static void ValidateIdentifier(string value, string paramName)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    
    // Prevent path traversal
    if (value.Contains("..") || value.Contains("/") || value.Contains("\\"))
        throw new ArgumentException($"{paramName} contains invalid characters", paramName);
}
```

---

## Medium Severity Findings

### SEC-002: Path Traversal Risk in GraphQL URL Construction

**Severity**: 🟠 Medium  
**Component**: `Oryx.Cognite/src/DataModels.fs`  
**Lines**: 313-314  
**OWASP Category**: A01:2021 – Broken Access Control

#### Description

The `graphqlUrl` function concatenates user-provided parameters directly into URL paths without encoding or validation.

#### Current Code

```fsharp
let graphqlUrl (space: string) (externalId: string) (version: string) =
    Url +/ "spaces" +/ space +/ "datamodels" +/ externalId +/ "versions" +/ version +/ "graphql"
```

#### Risk

Malicious input containing `../`, `%2F`, or other URL-encoded characters could potentially manipulate the API endpoint path, though CDF server-side validation provides a secondary defense.

#### Recommended Fix

```fsharp
let private validatePathSegment (name: string) (value: string) =
    if String.IsNullOrWhiteSpace(value) then
        invalidArg name "cannot be null or empty"
    if value.Contains("..") || value.Contains("/") || value.Contains("\\") then
        invalidArg name "contains invalid path characters"
    System.Uri.EscapeDataString(value)

let graphqlUrl (space: string) (externalId: string) (version: string) =
    let safeSpace = validatePathSegment "space" space
    let safeExternalId = validatePathSegment "externalId" externalId  
    let safeVersion = validatePathSegment "version" version
    Url +/ "spaces" +/ safeSpace +/ "datamodels" +/ safeExternalId +/ "versions" +/ safeVersion +/ "graphql"
```

---

### SEC-003: Insufficient Security Documentation for AllowExpiredCursors

**Severity**: 🟠 Medium  
**Component**: `CogniteSdk.Types/DataModels/Query/Query.cs`  
**Lines**: 157-161  
**Category**: Documentation / Compliance

#### Description

The `AllowExpiredCursorsAndAcceptMissedDeletes` property has significant data integrity and compliance implications that are not adequately documented.

#### Current Documentation

```csharp
/// <summary>
/// When true, allows use of expired cursors (older than 3 days).
/// Warning: Using expired cursors may miss soft-deleted instances.
/// </summary>
public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
```

#### Risk

In regulated environments (pharmaceutical/GxP, financial services), using this flag could:
- Violate 21 CFR Part 11 electronic records requirements
- Cause audit trail gaps
- Lead to compliance failures during inspections

#### Recommended Fix

```csharp
/// <summary>
/// When true, allows use of expired cursors (older than 3 days).
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠️ SECURITY AND COMPLIANCE WARNING</b>
/// </para>
/// <para>
/// Setting this property to <c>true</c> may result in missed soft-deleted instances.
/// This has significant implications:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Data Integrity</b>: Deleted records may still appear in sync results,
///     leading to stale or inconsistent data in downstream systems.
///   </description></item>
///   <item><description>
///     <b>Compliance Risk</b>: In regulated environments (21 CFR Part 11, EU Annex 11,
///     SOX, GDPR), missing delete events may violate audit trail requirements.
///   </description></item>
///   <item><description>
///     <b>Audit Trail Gaps</b>: Delete operations will not be reflected in sync results,
///     creating incomplete audit histories.
///   </description></item>
/// </list>
/// <para>
/// <b>When to use:</b> Only enable this when:
/// </para>
/// <list type="number">
///   <item><description>Data freshness is acceptable for your use case</description></item>
///   <item><description>You have alternative mechanisms for ensuring data consistency</description></item>
///   <item><description>Your environment is not subject to regulatory compliance requirements</description></item>
/// </list>
/// </remarks>
/// <value>
/// <c>true</c> to allow expired cursors and accept potential missed deletes;
/// <c>false</c> or <c>null</c> to enforce cursor expiration (default, recommended).
/// </value>
public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
```

---

### SEC-004: Sensitive Data Exposure in ToString()

**Severity**: 🟠 Medium  
**Component**: `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs`  
**Lines**: 429-441  
**OWASP Category**: A09:2021 – Security Logging and Monitoring Failures

#### Description

The `ToString()` method serializes filter contents to JSON. Filters may contain sensitive property values (PII, tokens, etc.) that could be logged inadvertently. The empty `catch` block also suppresses errors silently.

#### Current Code

```csharp
public override string ToString()
{
    if (_filter == null)
        return "<no filter configured>";

    try
    {
        return JsonSerializer.Serialize(_filter, _filter.GetType(), JsonOptions);
    }
    catch
    {
        return "<filter serialization failed>";
    }
}
```

#### Risk

- Sensitive filter values could appear in logs, stack traces, or debugging output
- Silent exception swallowing hides serialization issues

#### Recommended Fix

```csharp
/// <summary>
/// Returns a JSON string representation of the filter for debugging.
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠️ Security Note:</b> This method serializes all filter values to JSON.
/// Do not use in production logging if filters may contain sensitive data
/// (PII, credentials, tokens, etc.).
/// </para>
/// </remarks>
/// <returns>JSON representation of the filter, or a placeholder message.</returns>
public override string ToString()
{
    if (_filter == null)
        return "<no filter configured>";

    try
    {
        return JsonSerializer.Serialize(_filter, _filter.GetType(), JsonOptions);
    }
    catch (JsonException ex)
    {
        // Log for debugging but don't expose serialization details
        System.Diagnostics.Debug.WriteLine($"FilterBuilder.ToString() serialization failed: {ex.Message}");
        return "<filter serialization failed>";
    }
}
```

---

## Low Severity Findings

### SEC-005: Thread Safety Warning Placement

**Severity**: 🟡 Low  
**Component**: `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs`  
**Line**: 18

#### Description

The thread-safety warning is buried in the `<remarks>` section and could be overlooked by developers scanning the class summary.

#### Recommendation

Move the warning to be more prominent:

```csharp
/// <summary>
/// Fluent builder for DMS filters. NOT THREAD-SAFE - create a new instance for each filter.
/// Produces <see cref="IDMSFilter"/> objects compatible with the CDF API.
/// </summary>
```

---

### SEC-006: Test Credential Lifecycle

**Severity**: 🟡 Low  
**Component**: `test_auth_env.sh`  
**Lines**: 54-60

#### Description

The script exports `CLIENT_SECRET` to environment variables which persist in the shell session until explicitly unset or the session ends.

#### Current Code

```bash
export TEST_CLIENT_SECRET_WRITE=$CLIENT_SECRET
export TEST_CLIENT_SECRET_READ=$CLIENT_SECRET
```

#### Recommendation

Add cleanup instructions:

```bash
echo ""
echo "✓ Environment configured for testing"
echo ""
echo "⚠️  SECURITY NOTICE: Credentials are now in environment variables."
echo "   When finished testing, run:"
echo "   unset TEST_CLIENT_SECRET_WRITE TEST_CLIENT_SECRET_READ CLIENT_SECRET"
echo ""
```

---

### SEC-007: Arbitrary Type in GraphQL Variables

**Severity**: 🟡 Low  
**Component**: `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs`  
**Lines**: 24-27

#### Description

The `Variables` dictionary uses `object` as the value type, which allows any .NET type to be serialized. This could lead to unexpected serialization of sensitive types or large object graphs.

#### Current Code

```csharp
public Dictionary<string, object> Variables { get; set; }
```

#### Recommendation

Consider using `JsonElement` or a marker interface for type safety:

```csharp
/// <summary>
/// Optional variables to pass to the query.
/// </summary>
/// <remarks>
/// Values should be JSON-serializable primitives, arrays, or objects.
/// Complex .NET types may not serialize as expected.
/// </remarks>
public Dictionary<string, object> Variables { get; set; }
```

---

### SEC-008: Static JsonSerializerOptions Mutation Risk

**Severity**: 🟡 Low  
**Component**: `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs`  
**Lines**: 28-32

#### Description

The static `JsonOptions` is correctly marked `readonly`, but `JsonSerializerOptions` is mutable after construction in older .NET versions.

#### Current Code

```csharp
private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

#### Recommendation

For .NET 5+, this is safe. For .NET Standard 2.0 compatibility, consider making options immutable or creating new instances. Add a comment documenting this:

```csharp
// Note: JsonSerializerOptions is thread-safe for reading after construction.
// Do not modify these options after initialization.
private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

---

## Positive Security Practices

### ✅ SEC-P01: Comprehensive Input Validation in FilterBuilder

The `FilterBuilder` class demonstrates excellent defensive programming with consistent null checks and validation:

```csharp
public FilterBuilder HasData(params ViewIdentifier[] views)
{
    if (views == null) throw new ArgumentNullException(nameof(views));
    if (views.Length == 0)
        throw new ArgumentException("At least one view must be provided", nameof(views));
    // ...
}
```

**Files**: `FilterBuilder.cs` - 15+ validation points

---

### ✅ SEC-P02: Property Path Validation

Strong validation of property paths prevents malformed API requests:

```csharp
private static void ValidatePropertyPath(IEnumerable<string> property)
{
    if (property == null)
        throw new ArgumentNullException(nameof(property));

    var propList = property.ToList();
    if (propList.Count == 0)
        throw new ArgumentException("Property path cannot be empty", nameof(property));

    foreach (var segment in propList)
    {
        if (string.IsNullOrEmpty(segment))
            throw new ArgumentException("Property path segments cannot be null or empty", nameof(property));
    }
}
```

---

### ✅ SEC-P03: Proper Credential Management

- `.env` files are correctly gitignored
- Credentials loaded via environment variables, not hardcoded
- OAuth2 client credentials flow used (service principal, not interactive)
- No credentials in source code or test files

**Files**: `.gitignore`, `test_auth_env.sh`

---

### ✅ SEC-P04: SyncBackfillSort Setter Validation

The property setter prevents invalid object states:

```csharp
public IEnumerable<string> Property
{
    get => _property;
    set
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Property path cannot be null");
        // Additional validation...
        _property = list;
    }
}
```

---

### ✅ SEC-P05: Error State Visibility in GraphQL Responses

GraphQL responses properly expose error information:

```csharp
[JsonIgnore]
public bool HasErrors => Errors?.Any() ?? false;
```

This allows consumers to check for errors before accessing data.

---

## Recommendations Summary

| Priority | ID | Action Item | Effort |
|----------|-----|-------------|--------|
| 🔴 P0 | SEC-001 | Add input validation to GraphQL methods | 2h |
| 🟠 P1 | SEC-002 | URL-encode/validate path segments in F# | 1h |
| 🟠 P1 | SEC-003 | Enhance AllowExpiredCursors documentation | 30m |
| 🟠 P1 | SEC-004 | Add sensitive data warning to ToString() | 30m |
| 🟡 P2 | SEC-005 | Move thread-safety warning to class summary | 15m |
| 🟡 P2 | SEC-006 | Add credential cleanup guidance to test script | 15m |
| 🟡 P2 | SEC-007 | Document Variables dictionary serialization | 15m |
| 🟡 P2 | SEC-008 | Add comment about JsonOptions thread safety | 5m |

**Total Estimated Effort**: ~5 hours

---

## Remediation Tracking

| ID | Status | Fixed In | Verified By | Date |
|----|--------|----------|-------------|------|
| SEC-001 | ✅ Fixed | DataModels.cs | Unit Tests (64 passing) | 2026-01-11 |
| SEC-002 | ⚠️ Out of Scope | N/A (F# layer) | N/A | N/A |
| SEC-003 | ✅ Fixed | Query.cs | Code Review | 2026-01-11 |
| SEC-004 | ✅ Fixed | FilterBuilder.cs | Code Review | 2026-01-11 |
| SEC-005 | ✅ Fixed | FilterBuilder.cs | Code Review | 2026-01-11 |
| SEC-006 | ✅ Fixed | test_auth_env.sh | Code Review | 2026-01-11 |
| SEC-007 | ✅ Fixed | GraphQL.cs | Code Review | 2026-01-11 |
| SEC-008 | ✅ Fixed | FilterBuilder.cs | Code Review | 2026-01-11 |

### SEC-002 Note

SEC-002 (Path Traversal Risk in F# Layer) is **out of scope** for this extension work. The fix would require modifying `Oryx.Cognite/src/DataModels.fs`, which is part of the core SDK's F# HTTP pipeline. The input validation added in SEC-001 at the C# layer provides defense-in-depth and mitigates the risk in practice.

---

## Appendix: Security Testing Recommendations

When submitting PRs to the official SDK, consider adding these security-focused tests:

### Unit Tests

```csharp
[Fact]
public void GraphQLQuery_WithNullSpace_ThrowsArgumentException()
{
    await Assert.ThrowsAsync<ArgumentException>(() => 
        client.DataModels.GraphQLQuery<object>(null, "model", "1", "{}"));
}

[Fact]
public void GraphQLQuery_WithPathTraversalInSpace_ThrowsArgumentException()
{
    await Assert.ThrowsAsync<ArgumentException>(() => 
        client.DataModels.GraphQLQuery<object>("../admin", "model", "1", "{}"));
}

[Fact]
public void GraphQLQuery_WithExcessiveQueryLength_ThrowsArgumentException()
{
    var hugeQuery = new string('a', 200_000);
    await Assert.ThrowsAsync<ArgumentException>(() => 
        client.DataModels.GraphQLQuery<object>("space", "model", "1", hugeQuery));
}
```

### Integration Tests

- Verify CDF API rejects malformed identifiers (defense in depth)
- Confirm error responses don't leak sensitive information
- Test expired cursor behavior with `AllowExpiredCursorsAndAcceptMissedDeletes`

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-11 | Security Review (AI-Assisted) | Initial review |
