# Security Review: Data Modeling Extensions

**Reviewer**: Senior Security Engineer (AI-Assisted)  
**Date**: January 2026  
**Scope**: FilterBuilder, SyncQuery Extensions, GraphQL Client additions  
**SDK Version**: Feature branch `feature/data-modeling-extensions`

---

## Executive Summary

The Data Modeling extensions demonstrate **good security practices** with comprehensive input validation and proper credential handling. This document records the security review findings, remediation recommendations, and implemented fixes.

### Remediation Status

| Severity   | Count | Status                           |
| ---------- | ----- | -------------------------------- |
| 🔴 High     | 1     | ✅ Fixed                          |
| 🟠 Medium   | 3     | ✅ Fixed (2) / ⚠️ Out of Scope (1) |
| 🟡 Low      | 4     | ✅ Fixed                          |
| ✅ Positive | 5     | N/A                              |

---

## Table of Contents

1. [High Severity Findings](#high-severity-findings)
2. [Medium Severity Findings](#medium-severity-findings)
3. [Low Severity Findings](#low-severity-findings)
4. [Positive Security Practices](#positive-security-practices)
5. [Remediation Tracking](#remediation-tracking)

---

## High Severity Findings

### SEC-001: GraphQL Input Validation Missing

| Field              | Value                                    |
| ------------------ | ---------------------------------------- |
| **Severity**       | 🔴 High                                   |
| **Status**         | ✅ Fixed                                  |
| **Component**      | `CogniteSdk/src/Resources/DataModels.cs` |
| **OWASP Category** | A03:2021 – Injection                     |

#### Original Problem

The GraphQL methods (`GraphQLQuery<T>`, `GraphQLQueryRaw`, `GraphQLIntrospect`) accepted raw string parameters without validation. Parameters were passed directly to the API without null checks, empty string validation, or length limits.

```csharp
// BEFORE: No validation
public async Task<GraphQLResponse<T>> GraphQLQuery<T>(
    string space,
    string externalId,
    string version,
    string query,
    ...)
{
    var request = new GraphQLRequest { Query = query };  // No validation
    var req = Oryx.Cognite.DataModels.graphqlQuery<T>(space, externalId, version, request, GetContext(token));
    return await RunAsync(req).ConfigureAwait(false);
}
```

**Risks identified:**
1. Null Reference Exceptions from null values
2. DoS via unbounded query strings consuming excessive memory
3. Path traversal via `space`, `externalId`, `version` parameters used in URL construction

#### Recommendation

Add comprehensive input validation including:
- Null/empty checks for all identifier parameters
- Path traversal character blocking (`..`, `/`, `\`, `%`)
- Maximum query length limit
- Proper exception types with descriptive messages

#### Implementation

Two private validation methods were added with comprehensive checks:

```csharp
// AFTER: Full validation implemented (Lines 501-523)
private const int MaxGraphQLQueryLength = 100_000;

private static void ValidateGraphQLIdentifier(string value, string paramName)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException($"{paramName} cannot be null or empty", paramName);

    // Prevent path traversal attacks
    if (value.Contains("..") || value.Contains("/") || value.Contains("\\") || value.Contains("%"))
        throw new ArgumentException($"{paramName} contains invalid characters that could be used for path traversal", paramName);
}

private static void ValidateGraphQLQuery(string query)
{
    if (string.IsNullOrWhiteSpace(query))
        throw new ArgumentException("Query cannot be null or empty", nameof(query));

    if (query.Length > MaxGraphQLQueryLength)
        throw new ArgumentException($"Query exceeds maximum length of {MaxGraphQLQueryLength} characters", nameof(query));
}
```

All three GraphQL methods now call validation before processing:

```csharp
public async Task<GraphQLResponse<T>> GraphQLQuery<T>(...)
{
    ValidateGraphQLIdentifier(space, nameof(space));
    ValidateGraphQLIdentifier(externalId, nameof(externalId));
    ValidateGraphQLIdentifier(version, nameof(version));
    ValidateGraphQLQuery(query);
    // ... rest of implementation
}
```

**Verification**: 64 unit tests passing, including security-focused tests for invalid inputs.

---

## Medium Severity Findings

### SEC-002: Path Traversal Risk in GraphQL URL Construction

| Field              | Value                            |
| ------------------ | -------------------------------- |
| **Severity**       | 🟠 Medium                         |
| **Status**         | ⚠️ Out of Scope                   |
| **Component**      | `Oryx.Cognite/src/DataModels.fs` |
| **OWASP Category** | A01:2021 – Broken Access Control |

#### Original Problem

The F# `graphqlUrl` function concatenated user-provided parameters directly into URL paths without encoding or validation.

```fsharp
let graphqlUrl (space: string) (externalId: string) (version: string) =
    Url +/ "spaces" +/ space +/ "datamodels" +/ externalId +/ "versions" +/ version +/ "graphql"
```

#### Recommendation

Add URL-encoding and path traversal validation at the F# layer:

```fsharp
let private validatePathSegment (name: string) (value: string) =
    if String.IsNullOrWhiteSpace(value) then
        invalidArg name "cannot be null or empty"
    if value.Contains("..") || value.Contains("/") || value.Contains("\\") then
        invalidArg name "contains invalid path characters"
    System.Uri.EscapeDataString(value)
```

#### Implementation

**Status: Out of Scope**

This fix requires modifying `Oryx.Cognite/src/DataModels.fs`, which is part of the core SDK's F# HTTP pipeline and outside the scope of the Data Modeling extensions.

**Mitigation**: The input validation added at the C# layer (SEC-001) provides defense-in-depth. All identifiers are validated before reaching the F# layer, effectively preventing path traversal attacks from the public API surface.

---

### SEC-003: Insufficient Security Documentation for AllowExpiredCursors

| Field         | Value                                        |
| ------------- | -------------------------------------------- |
| **Severity**  | 🟠 Medium                                     |
| **Status**    | ✅ Fixed                                      |
| **Component** | `CogniteSdk.Types/DataModels/Query/Query.cs` |
| **Category**  | Documentation / Compliance                   |

#### Original Problem

The `AllowExpiredCursorsAndAcceptMissedDeletes` property had minimal documentation that did not adequately warn about data integrity and compliance implications.

```csharp
// BEFORE: Minimal documentation
/// <summary>
/// When true, allows use of expired cursors (older than 3 days).
/// Warning: Using expired cursors may miss soft-deleted instances.
/// </summary>
public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
```

**Risks identified:**
- Potential violation of 21 CFR Part 11 electronic records requirements
- Audit trail gaps in regulated environments
- Compliance failures during inspections

#### Recommendation

Enhance documentation with comprehensive security and compliance warnings including specific regulatory references and guidance on when (not) to use this flag.

#### Implementation

The property now includes detailed XML documentation (Lines 157-194 in Query.cs):

```csharp
// AFTER: Comprehensive security documentation
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
public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
```

---

### SEC-004: Sensitive Data Exposure in ToString()

| Field              | Value                                                |
| ------------------ | ---------------------------------------------------- |
| **Severity**       | 🟠 Medium                                             |
| **Status**         | ✅ Fixed                                              |
| **Component**      | `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` |
| **OWASP Category** | A09:2021 – Security Logging and Monitoring Failures  |

#### Original Problem

The `ToString()` method serialized filter contents to JSON with silent exception handling. Filters may contain sensitive property values (PII, tokens, etc.) that could be logged inadvertently.

```csharp
// BEFORE: Silent catch, no security warning
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

**Risks identified:**
- Sensitive filter values appearing in logs or debugging output
- Silent exception swallowing hiding serialization issues

#### Recommendation

Add XML documentation warning about sensitive data and implement proper exception logging with `JsonException` specificity.

#### Implementation

The method now includes security documentation and proper exception handling (Lines 429-455 in FilterBuilder.cs):

```csharp
// AFTER: Security warning and proper exception handling
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
        // Log for debugging but don't expose serialization details in production
        System.Diagnostics.Debug.WriteLine($"FilterBuilder.ToString() serialization failed: {ex.Message}");
        return "<filter serialization failed>";
    }
}
```

---

## Low Severity Findings

### SEC-005: Thread Safety Warning Placement

| Field         | Value                                                |
| ------------- | ---------------------------------------------------- |
| **Severity**  | 🟡 Low                                                |
| **Status**    | ✅ Fixed                                              |
| **Component** | `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` |

#### Original Problem

The thread-safety warning was buried in the `<remarks>` section of the class documentation and could be overlooked.

```csharp
// BEFORE: Warning in remarks only
/// <summary>
/// Fluent builder for DMS filters.
/// Produces <see cref="IDMSFilter"/> objects compatible with the CDF API.
/// </summary>
/// <remarks>
/// <para>This builder is not thread-safe. Create a new instance for each filter.</para>
/// </remarks>
```

#### Recommendation

Move the thread-safety warning to the class summary for visibility.

#### Implementation

The warning is now prominently displayed in the summary (Lines 11-19 in FilterBuilder.cs):

```csharp
// AFTER: Warning in summary AND detailed in remarks
/// <summary>
/// Fluent builder for DMS filters. NOT THREAD-SAFE - create a new instance for each filter.
/// Produces <see cref="IDMSFilter"/> objects compatible with the CDF API.
/// </summary>
/// <remarks>
/// <para>Property paths use the format: [space, "viewExternalId/version", property]</para>
/// <para><b>Thread Safety:</b> This builder is NOT thread-safe. Each thread or concurrent 
/// operation must create its own FilterBuilder instance. Do not share instances across threads.</para>
/// </remarks>
```

---

### SEC-006: Test Credential Lifecycle

| Field         | Value              |
| ------------- | ------------------ |
| **Severity**  | 🟡 Low              |
| **Status**    | ✅ Fixed            |
| **Component** | `test_auth_env.sh` |

#### Original Problem

The script exported credentials to environment variables without providing cleanup instructions, leaving credentials in shell history and environment until session end.

```bash
# BEFORE: No cleanup guidance
export TEST_CLIENT_SECRET_WRITE=$CLIENT_SECRET
export TEST_CLIENT_SECRET_READ=$CLIENT_SECRET

echo "Environment configured:"
echo "  Project: $CDF_PROJECT"
```

#### Recommendation

Add security notice and cleanup instructions to help developers clear credentials after testing.

#### Implementation

The script now displays clear security notice and cleanup commands (Lines 62-73 in test_auth_env.sh):

```bash
# AFTER: Security notice with cleanup commands
echo ""
echo "✓ Environment configured for testing"
echo "  Project: $CDF_PROJECT"
echo "  Cluster: $CDF_CLUSTER"
echo "  Host: $TEST_HOST_WRITE"
echo ""
echo "⚠️  SECURITY NOTICE: Credentials are now in environment variables."
echo "   When finished testing, run the following to clear credentials:"
echo ""
echo "   unset TEST_CLIENT_SECRET_WRITE TEST_CLIENT_SECRET_READ CLIENT_SECRET"
echo "   unset TEST_TOKEN_WRITE TEST_TOKEN_READ"
echo ""
```

---

### SEC-007: Arbitrary Type in GraphQL Variables

| Field         | Value                                            |
| ------------- | ------------------------------------------------ |
| **Severity**  | 🟡 Low                                            |
| **Status**    | ✅ Fixed                                          |
| **Component** | `CogniteSdk.Types/DataModels/GraphQL/GraphQL.cs` |

#### Original Problem

The `Variables` dictionary used `object` as the value type with no documentation about serialization behavior or security considerations.

```csharp
// BEFORE: No documentation
public Dictionary<string, object> Variables { get; set; }
```

#### Recommendation

Add documentation about expected value types and security considerations for sensitive data.

#### Implementation

Comprehensive documentation was added (Lines 22-37 in GraphQL.cs):

```csharp
// AFTER: Documented with security note
/// <summary>
/// Optional variables to pass to the query.
/// </summary>
/// <remarks>
/// <para>
/// Values should be JSON-serializable primitives (string, number, boolean), 
/// arrays, or simple objects. Complex .NET types may not serialize as expected.
/// </para>
/// <para>
/// <b>Security Note:</b> Avoid passing sensitive data (credentials, tokens, PII) 
/// as variables if logging is enabled, as these may appear in request logs.
/// </para>
/// </remarks>
[JsonPropertyName("variables")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public Dictionary<string, object> Variables { get; set; }
```

---

### SEC-008: Static JsonSerializerOptions Mutation Risk

| Field         | Value                                                |
| ------------- | ---------------------------------------------------- |
| **Severity**  | 🟡 Low                                                |
| **Status**    | ✅ Fixed                                              |
| **Component** | `CogniteSdk.Types/DataModels/Query/FilterBuilder.cs` |

#### Original Problem

The static `JsonOptions` field was marked `readonly` but lacked documentation about thread-safety behavior, which varies by .NET version.

```csharp
// BEFORE: No comment about thread safety
private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

#### Recommendation

Add documentation clarifying thread-safety behavior and warning against modification.

#### Implementation

Comment added explaining thread-safety (Lines 29-35 in FilterBuilder.cs):

```csharp
// AFTER: Thread-safety documented
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

The following security-positive patterns were identified during the review:

### ✅ SEC-P01: Comprehensive Input Validation in FilterBuilder

The `FilterBuilder` class demonstrates excellent defensive programming with 15+ validation points using consistent null checks and argument validation.

### ✅ SEC-P02: Property Path Validation

Strong validation of property paths prevents malformed API requests via the `ValidatePropertyPath` method.

### ✅ SEC-P03: Proper Credential Management

- `.env` files are correctly gitignored
- Credentials loaded via environment variables, not hardcoded
- OAuth2 client credentials flow used (service principal, not interactive)
- No credentials in source code or test files

### ✅ SEC-P04: SyncBackfillSort Setter Validation

The property setter prevents invalid object states through validation in the setter.

### ✅ SEC-P05: Error State Visibility in GraphQL Responses

GraphQL responses properly expose error information via `HasErrors` property, allowing consumers to check for errors before accessing data.

---

## Remediation Tracking

| ID      | Status         | Fixed In         | Verified By             | Date       |
| ------- | -------------- | ---------------- | ----------------------- | ---------- |
| SEC-001 | ✅ Fixed        | DataModels.cs    | Unit Tests (64 passing) | 2026-01-11 |
| SEC-002 | ⚠️ Out of Scope | N/A (F# layer)   | N/A                     | N/A        |
| SEC-003 | ✅ Fixed        | Query.cs         | Code Review             | 2026-01-11 |
| SEC-004 | ✅ Fixed        | FilterBuilder.cs | Code Review             | 2026-01-11 |
| SEC-005 | ✅ Fixed        | FilterBuilder.cs | Code Review             | 2026-01-11 |
| SEC-006 | ✅ Fixed        | test_auth_env.sh | Code Review             | 2026-01-11 |
| SEC-007 | ✅ Fixed        | GraphQL.cs       | Code Review             | 2026-01-11 |
| SEC-008 | ✅ Fixed        | FilterBuilder.cs | Code Review             | 2026-01-11 |

### SEC-002 Resolution Note

SEC-002 (Path Traversal Risk in F# Layer) is **out of scope** for this extension work. The fix would require modifying `Oryx.Cognite/src/DataModels.fs`, which is part of the core SDK's F# HTTP pipeline. The input validation added in SEC-001 at the C# layer provides defense-in-depth and mitigates the risk in practice—all parameters are validated before they reach the F# layer.

---

## Document History

| Version | Date       | Author                        | Changes                                                                    |
| ------- | ---------- | ----------------------------- | -------------------------------------------------------------------------- |
| 1.0     | 2026-01-11 | Security Review (AI-Assisted) | Initial review                                                             |
| 1.1     | 2026-01-11 | Security Review (AI-Assisted) | Validated fixes, reformatted to show problem→recommendation→implementation |
