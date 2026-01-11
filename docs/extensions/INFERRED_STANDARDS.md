# Cognite .NET SDK - Inferred Coding Standards

> **Note**: This document captures coding standards, design patterns, and best practices inferred from analyzing the existing Cognite .NET SDK codebase. These are not official Cognite guidelines but represent patterns consistently observed throughout the SDK.

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Naming Conventions](#naming-conventions)
3. [Type Design Patterns](#type-design-patterns)
4. [API Design Patterns](#api-design-patterns)
5. [Async Programming](#async-programming)
6. [Input Validation](#input-validation)
7. [JSON Serialization](#json-serialization)
8. [Error Handling](#error-handling)
9. [Documentation Standards](#documentation-standards)
10. [Testing Standards](#testing-standards)
11. [Resource Management](#resource-management)

---

## Design Philosophy

### Mission-Critical Software Mindset

The Cognite SDK powers industrial applications where reliability is paramount. Code should be written with the assumption that it will run in:

- 24/7 production environments
- High-throughput data pipelines
- Safety-critical industrial systems

### Key Principles

1. **Fail Fast**: Invalid inputs should throw immediately, not during API calls
2. **Predictable Behavior**: No surprises - methods do exactly what their names suggest
3. **Defensive Programming**: Validate at boundaries, trust nothing from external sources
4. **Minimal Footprint**: Don't add features, abstractions, or patterns unless strictly necessary

---

## Naming Conventions

### General Rules

| Element           | Convention   | Example                               |
| ----------------- | ------------ | ------------------------------------- |
| Public properties | PascalCase   | `ExternalId`, `CreatedTime`           |
| Private fields    | `_camelCase` | `_filter`, `_httpClient`              |
| Parameters        | camelCase    | `externalId`, `cancellationToken`     |
| Constants         | PascalCase   | `DefaultLimit`, `MaxBatchSize`        |
| Interfaces        | `I` prefix   | `IDMSFilter`, `IQueryTableExpression` |

### Async Methods

All async methods MUST have the `Async` suffix:

```csharp
// ✅ Correct
public Task<T> QueryAsync<T>(...)
public Task<IEnumerable<T>> ListAsync<T>(...)

// ❌ Incorrect
public Task<T> Query<T>(...)
public Task<IEnumerable<T>> List<T>(...)
```

### Type Suffixes

Types follow a consistent suffix pattern based on their purpose:

| Suffix     | Purpose                      | Example                           |
| ---------- | ---------------------------- | --------------------------------- |
| `Create`   | Types for creating resources | `AssetCreate`, `NodeCreate`       |
| `Update`   | Types for updating resources | `AssetUpdate`, `NodeUpdate`       |
| `Filter`   | Types for filtering queries  | `AssetFilter`, `DMSFilter`        |
| `Query`    | Types for complex queries    | `Query`, `SyncQuery`              |
| `Request`  | Full API request payloads    | `GraphQLRequest`                  |
| `Response` | API response wrappers        | `GraphQLResponse<T>`              |
| `Result`   | Operation results            | `QueryResult<T>`, `SyncResult<T>` |

---

## Type Design Patterns

### Builder Pattern

Use fluent builders for complex object construction:

```csharp
public class FilterBuilder
{
    private IDMSFilter _filter;
    
    // Factory method - preferred entry point
    public static FilterBuilder Create() => new();
    
    // Fluent methods return 'this'
    public FilterBuilder Equals(string[] property, IDMSValue value)
    {
        _filter = new DMSEqualsFilter { Property = property, Value = value };
        return this;
    }
    
    public FilterBuilder And(params FilterBuilder[] filters)
    {
        _filter = new DMSAndFilter 
        { 
            And = filters.Select(f => f.Build()).ToList() 
        };
        return this;
    }
    
    // Terminal method produces the result
    public IDMSFilter Build() => _filter ?? throw new InvalidOperationException("No filter set");
    
    // Nullable variant for optional filters
    public IDMSFilter BuildOrNull() => _filter;
}
```

**Usage:**

```csharp
var filter = FilterBuilder.Create()
    .And(
        FilterBuilder.Create().Equals(prop1, value1),
        FilterBuilder.Create().Range(prop2, gte: min, lte: max)
    )
    .Build();
```

### Resource Pattern

API resources are organized by domain and accessed via the client:

```csharp
// Resource class encapsulates all operations for a domain
public class DataModelsResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    
    // Operations are async methods
    public Task<QueryResult<T>> QueryAsync<T>(...) { }
    public Task<SyncResult<T>> SyncAsync<T>(...) { }
}

// Client provides resource accessors
public class Client
{
    public DataModelsResource DataModels { get; }
    public AssetsResource Assets { get; }
}
```

### Identity Pattern

Resources use a consistent identity model:

```csharp
// Instance identity - space + externalId
public class InstanceIdentifier
{
    public string Space { get; set; }
    public string ExternalId { get; set; }
}

// View identity - space + externalId + version
public class ViewIdentifier
{
    public string Space { get; set; }
    public string ExternalId { get; set; }
    public string Version { get; set; }
    
    // Implicit conversion from tuple for convenience
    public static implicit operator ViewIdentifier((string space, string externalId, string version) tuple)
        => new() { Space = tuple.space, ExternalId = tuple.externalId, Version = tuple.version };
}
```

### Generic Wrapper Types

API responses use consistent wrapper types:

```csharp
// For paginated results with cursor
public class ItemsWithCursor<T>
{
    public IEnumerable<T> Items { get; set; }
    public string NextCursor { get; set; }
}

// For results without pagination
public class ItemsWithoutCursor<T>
{
    public IEnumerable<T> Items { get; set; }
}
```

---

## API Design Patterns

### Method Signatures

Standard patterns for API methods:

```csharp
// Query methods - return typed results
public async Task<QueryResult<T>> QueryAsync<T>(
    Query query,
    CancellationToken cancellationToken = default)

// List methods - return collections
public async Task<ItemsWithCursor<T>> ListAsync<T>(
    int? limit = null,
    string cursor = null,
    CancellationToken cancellationToken = default)

// Create methods - accept items, return created items
public async Task<IEnumerable<T>> CreateAsync<T>(
    IEnumerable<T> items,
    CancellationToken cancellationToken = default)

// Delete methods - accept identifiers
public async Task DeleteAsync(
    IEnumerable<InstanceIdentifier> ids,
    CancellationToken cancellationToken = default)
```

### Overloads

Provide convenience overloads but keep the core implementation in one place:

```csharp
// Core implementation
public async Task<QueryResult<T>> QueryAsync<T>(
    Query query,
    JsonSerializerOptions options,
    CancellationToken cancellationToken = default)
{
    // Implementation here
}

// Convenience overload - delegates to core
public Task<QueryResult<T>> QueryAsync<T>(
    Query query,
    CancellationToken cancellationToken = default)
    => QueryAsync<T>(query, null, cancellationToken);
```

### Property Paths

Data Modeling API uses property paths as string arrays:

```csharp
// 3-element path: [space, view/version, property]
var propertyPath = new[] { "mySpace", "MyView/1", "name" };

// In FilterBuilder - use params for convenience
public FilterBuilder Equals(string[] property, IDMSValue value)

// Helper for view-based paths
public FilterBuilder Equals(ViewIdentifier view, string property, IDMSValue value)
    => Equals(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, value);
```

---

## Async Programming

### ConfigureAwait(false)

**CRITICAL**: All `await` calls in library code MUST use `ConfigureAwait(false)`:

```csharp
// ✅ Correct - prevents deadlocks in sync-over-async scenarios
public async Task<T> QueryAsync<T>(Query query, CancellationToken ct)
{
    var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return JsonSerializer.Deserialize<T>(content);
}

// ❌ WRONG - can deadlock in WPF/WinForms/ASP.NET contexts
public async Task<T> QueryAsync<T>(Query query, CancellationToken ct)
{
    var response = await _httpClient.SendAsync(request, ct);  // Missing ConfigureAwait!
    var content = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<T>(content);
}
```

### CancellationToken

Always accept and propagate `CancellationToken`:

```csharp
public async Task<T> QueryAsync<T>(
    Query query,
    CancellationToken cancellationToken = default)  // Default value for convenience
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var response = await _httpClient.SendAsync(request, cancellationToken)
        .ConfigureAwait(false);
    
    // Pass through to all async operations
    var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
        .ConfigureAwait(false);
}
```

### Task-Based Async Pattern (TAP)

- Return `Task<T>` for operations that produce a value
- Return `Task` for operations that don't produce a value
- Never return `void` from async methods (except event handlers)
- Never use `.Result` or `.Wait()` in library code

---

## Input Validation

### Fail Fast Principle

Validate inputs at public API boundaries before any work is done:

```csharp
public async Task<T> QueryAsync<T>(Query query, CancellationToken ct)
{
    // Validate at entry point
    ArgumentNullException.ThrowIfNull(query);
    
    if (query.With == null || query.With.Count == 0)
        throw new ArgumentException("Query must have at least one 'with' clause", nameof(query));
    
    // Only proceed after validation passes
    return await ExecuteQueryAsync<T>(query, ct).ConfigureAwait(false);
}
```

### Constructor Validation

Validate required dependencies in constructors:

```csharp
public class GraphQLResource
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _baseUrl;
    
    public GraphQLResource(HttpClient httpClient, string project, string baseUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _project = !string.IsNullOrWhiteSpace(project) 
            ? project 
            : throw new ArgumentException("Project cannot be null or empty", nameof(project));
        _baseUrl = !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl.TrimEnd('/')
            : throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));
    }
}
```

### Property Validation

For properties that have constraints, use backing fields with validation:

```csharp
public class SyncBackfillSort
{
    private IEnumerable<string> _property;
    
    public IEnumerable<string> Property
    {
        get => _property;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Property path cannot be null");
            
            var list = value.ToList();
            if (list.Count == 0)
                throw new ArgumentException("Property path cannot be empty", nameof(value));
            
            foreach (var segment in list)
            {
                if (string.IsNullOrEmpty(segment))
                    throw new ArgumentException("Property path segments cannot be null or empty", nameof(value));
            }
            
            _property = list;
        }
    }
}
```

---

## JSON Serialization

### System.Text.Json

The SDK uses `System.Text.Json` (not Newtonsoft.Json):

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
```

### Property Naming

Use `JsonPropertyName` for API-specific names:

```csharp
public class GraphQLRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; }
    
    [JsonPropertyName("variables")]
    public Dictionary<string, object> Variables { get; set; }
    
    [JsonPropertyName("operationName")]
    public string OperationName { get; set; }
}
```

### Null Handling

Exclude null properties from serialization:

```csharp
// On individual properties
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string Cursor { get; set; }

// Or via options
var options = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

### Enum Serialization

Use `JsonStringEnumConverter` for enums that serialize as strings:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncMode
{
    onePhase,   // Serializes as "onePhase"
    twoPhase,   // Serializes as "twoPhase"
    noBackfill  // Serializes as "noBackfill"
}
```

### Polymorphic Types

For interface types that need polymorphic serialization:

```csharp
// Define discriminator
[JsonDerivedType(typeof(DMSEqualsFilter), "equals")]
[JsonDerivedType(typeof(DMSAndFilter), "and")]
[JsonDerivedType(typeof(DMSOrFilter), "or")]
public interface IDMSFilter { }

// Or use custom converters
[JsonConverter(typeof(DMSFilterConverter))]
public interface IDMSFilter { }
```

---

## Error Handling

### ResponseException

API errors are wrapped in `ResponseException`:

```csharp
public class ResponseException : Exception
{
    public int Code { get; }
    public string Message { get; }
    public string RequestId { get; }
    public IEnumerable<CogniteError> Errors { get; }
}

// Usage
try
{
    await client.DataModels.QueryAsync<T>(query);
}
catch (ResponseException ex) when (ex.Code == 400)
{
    // Handle validation error
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
    }
}
catch (ResponseException ex) when (ex.Code == 404)
{
    // Handle not found
}
```

### Don't Swallow Exceptions

Never catch exceptions without handling or rethrowing:

```csharp
// ❌ WRONG - swallows exception
try { await DoSomething(); }
catch { }

// ❌ WRONG - loses stack trace
try { await DoSomething(); }
catch (Exception ex) { throw ex; }

// ✅ Correct - preserves stack trace
try { await DoSomething(); }
catch (Exception ex) 
{ 
    _logger.LogError(ex, "Operation failed");
    throw;  // Rethrow preserves stack trace
}
```

---

## Documentation Standards

### XML Documentation

All public members MUST have XML documentation:

```csharp
/// <summary>
/// Executes a GraphQL query against the CDF Data Modeling API.
/// </summary>
/// <typeparam name="T">The type to deserialize the data field into.</typeparam>
/// <param name="request">The GraphQL request containing query, variables, and operation name.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>A GraphQL response containing the typed data and any errors.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
/// <exception cref="HttpRequestException">Thrown when the HTTP request fails.</exception>
/// <example>
/// <code>
/// var response = await client.GraphQL.QueryAsync&lt;MyType&gt;(
///     new GraphQLRequest { Query = "{ nodes { items { name } } }" }
/// );
/// </code>
/// </example>
public async Task<GraphQLResponse<T>> QueryAsync<T>(
    GraphQLRequest request,
    CancellationToken cancellationToken = default)
```

### Required Tags

| Tag           | When to Use                             |
| ------------- | --------------------------------------- |
| `<summary>`   | Always - describes what the member does |
| `<param>`     | For each parameter                      |
| `<typeparam>` | For each type parameter                 |
| `<returns>`   | For methods that return a value         |
| `<exception>` | For each exception that can be thrown   |
| `<example>`   | For complex APIs - shows usage          |
| `<remarks>`   | For additional context or caveats       |

---

## Testing Standards

### Test Organization

```
CogniteSdk/test/csharp/
├── Unit/
│   ├── FilterBuilderTests.cs      # Unit tests - no external dependencies
│   ├── SyncQueryTests.cs
│   └── ...
├── Integration/
│   ├── FilterBuilderIntegrationTests.cs  # Requires CDF connection
│   ├── SyncGraphQLIntegrationTests.cs
│   └── ...
└── CogniteSdk.Test.CSharp.csproj
```

### Unit Test Pattern

```csharp
public class FilterBuilderTests
{
    [Fact]
    public void Equals_WithValidInput_CreatesEqualsFilter()
    {
        // Arrange
        var property = new[] { "space", "view/1", "name" };
        var value = new RawPropertyValue<string>("test");
        
        // Act
        var filter = FilterBuilder.Create()
            .Equals(property, value)
            .Build();
        
        // Assert
        var equalsFilter = Assert.IsType<DMSEqualsFilter>(filter);
        Assert.Equal(property, equalsFilter.Property);
    }
    
    [Fact]
    public void Property_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var sort = new SyncBackfillSort();
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sort.Property = null);
    }
}
```

### Integration Test Pattern

```csharp
public class FilterBuilderIntegrationTests : IAsyncLifetime
{
    private CogniteClient _readClient;
    private CogniteClient _writeClient;
    
    public async Task InitializeAsync()
    {
        // Two-client pattern for isolation
        _readClient = CreateClient("READ_CLIENT_CONFIG");
        _writeClient = CreateClient("WRITE_CLIENT_CONFIG");
        
        // Setup test data
        await SetupTestData();
    }
    
    public async Task DisposeAsync()
    {
        // Cleanup test data
        await CleanupTestData();
    }
    
    [Fact]
    public async Task Query_WithEqualsFilter_ReturnsMatchingNodes()
    {
        // Arrange
        var filter = FilterBuilder.Create()
            .Equals(_testView, "name", new RawPropertyValue<string>("TestNode"))
            .Build();
        
        // Act
        var result = await _readClient.DataModels.QueryAsync<Dictionary<string, object>>(
            new Query { /* ... */ });
        
        // Assert
        Assert.NotEmpty(result.Items);
    }
}
```

### Environment-Based Configuration

Integration tests use environment variables:

```csharp
// Required environment variables
// CDF_PROJECT - Target CDF project
// CDF_CLUSTER - CDF cluster (e.g., "westeurope-1")
// CDF_CLIENT_ID - OAuth client ID
// CDF_CLIENT_SECRET - OAuth client secret
// CDF_TENANT_ID - Azure AD tenant ID

private static CogniteClient CreateClient()
{
    var project = Environment.GetEnvironmentVariable("CDF_PROJECT")
        ?? throw new InvalidOperationException("CDF_PROJECT not set");
    // ...
}
```

---

## Resource Management

### HttpClient Lifecycle

**CRITICAL**: Never create `HttpClient` per request - reuse instances:

```csharp
// ❌ WRONG - socket exhaustion
public async Task<T> QueryAsync<T>(Query query)
{
    using var client = new HttpClient();  // Creates new socket each call!
    return await client.PostAsync(...);
}

// ✅ Correct - shared instance
public class MyResource
{
    private static readonly HttpClient SharedClient = new();
    
    public async Task<T> QueryAsync<T>(Query query)
    {
        return await SharedClient.PostAsync(...);
    }
}

// ✅ Better - injected dependency
public class MyResource
{
    private readonly HttpClient _httpClient;
    
    public MyResource(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }
}
```

### IDisposable

Implement `IDisposable` correctly when managing resources:

```csharp
public class MyResource : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private bool _disposed;
    
    public MyResource(HttpClient httpClient = null)
    {
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        
        _disposed = true;
    }
}
```

---

## Code Style Summary

### Do's ✅

- Use `ConfigureAwait(false)` on all awaits in library code
- Validate inputs at public API boundaries
- Use XML documentation on all public members
- Use consistent type suffixes (Create, Update, Filter, etc.)
- Use fluent builders for complex objects
- Reuse `HttpClient` instances
- Use `CancellationToken` throughout

### Don'ts ❌

- Don't swallow exceptions
- Don't use `.Result` or `.Wait()` on tasks
- Don't create `HttpClient` per request
- Don't forget `ConfigureAwait(false)`
- Don't add unnecessary abstractions
- Don't mix naming conventions
- Don't skip input validation

---

## References

- [Cognite .NET SDK Repository](https://github.com/cognitedata/cognite-sdk-dotnet)
- [Cognite API Documentation](https://docs.cognite.com/api)
- [Microsoft Async Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/async)
- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json)

---

*Document generated from code analysis - January 2026*
