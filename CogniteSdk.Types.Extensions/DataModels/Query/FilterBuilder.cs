// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using CogniteSdk.DataModels;
using System.Text.Json;

namespace CogniteSdk.Types.DataModels.Query;

/// <summary>
/// Fluent builder for DMS filters.
/// Produces filter objects compatible with the CDF API JSON structure.
/// </summary>
/// <remarks>
/// Property paths use the format: [space, "viewExternalId/version", property]
/// This builder is not thread-safe. Create a new instance for each filter.
/// </remarks>
public class FilterBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private object? _filter;

    /// <summary>
    /// Creates a new filter builder.
    /// </summary>
    public static FilterBuilder Create() => new();

    /// <summary>
    /// Creates a hasData filter for a view.
    /// </summary>
    /// <param name="space">The space external ID.</param>
    /// <param name="externalId">The view external ID.</param>
    /// <param name="version">The view version.</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is null or empty.</exception>
    public FilterBuilder HasData(string space, string externalId, string version)
    {
        ValidateStringParameter(space, nameof(space));
        ValidateStringParameter(externalId, nameof(externalId));
        ValidateStringParameter(version, nameof(version));

        _filter = new
        {
            hasData = new[]
            {
                new { type = "view", space, externalId, version }
            }
        };
        return this;
    }

    /// <summary>
    /// Creates a hasData filter for multiple views.
    /// </summary>
    /// <param name="views">The views to filter by.</param>
    /// <exception cref="ArgumentNullException">Thrown when views is null.</exception>
    /// <exception cref="ArgumentException">Thrown when views is empty.</exception>
    public FilterBuilder HasData(params ViewIdentifier[] views)
    {
        ArgumentNullException.ThrowIfNull(views);
        if (views.Length == 0)
            throw new ArgumentException("At least one view must be provided", nameof(views));

        _filter = new
        {
            hasData = views.Select(v => new { type = "view", space = v.Space, externalId = v.ExternalId, version = v.Version }).ToArray()
        };
        return this;
    }

    /// <summary>
    /// Creates an equals filter.
    /// </summary>
    /// <param name="property">Property path as [space, "view/version", property].</param>
    /// <param name="value">The value to compare.</param>
    /// <exception cref="ArgumentException">Thrown when property is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public FilterBuilder Equals(string[] property, object value)
    {
        ValidatePropertyPath(property);
        ArgumentNullException.ThrowIfNull(value);

        _filter = new { equals = new { property, value } };
        return this;
    }

    /// <summary>
    /// Creates an equals filter with simplified property path.
    /// </summary>
    /// <param name="space">The space external ID.</param>
    /// <param name="viewExternalId">The view external ID.</param>
    /// <param name="version">The view version.</param>
    /// <param name="property">The property name.</param>
    /// <param name="value">The value to compare.</param>
    public FilterBuilder Equals(string space, string viewExternalId, string version, string property, object value)
    {
        ValidateStringParameter(space, nameof(space));
        ValidateStringParameter(viewExternalId, nameof(viewExternalId));
        ValidateStringParameter(version, nameof(version));
        ValidateStringParameter(property, nameof(property));
        ArgumentNullException.ThrowIfNull(value);

        return Equals(new[] { space, $"{viewExternalId}/{version}", property }, value);
    }

    /// <summary>
    /// Creates an equals filter using a ViewIdentifier.
    /// </summary>
    /// <param name="view">The view identifier.</param>
    /// <param name="property">The property name.</param>
    /// <param name="value">The value to compare.</param>
    /// <exception cref="ArgumentNullException">Thrown when view is null.</exception>
    public FilterBuilder Equals(ViewIdentifier view, string property, object value)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));
        ArgumentNullException.ThrowIfNull(value);

        return Equals(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, value);
    }

    /// <summary>
    /// Creates an "in" filter.
    /// </summary>
    /// <param name="property">Property path as [space, "view/version", property].</param>
    /// <param name="values">The values to match.</param>
    public FilterBuilder In(string[] property, params object[] values)
    {
        ValidatePropertyPath(property);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("At least one value must be provided", nameof(values));

        _filter = new { @in = new { property, values } };
        return this;
    }

    /// <summary>
    /// Creates an "in" filter using a ViewIdentifier.
    /// </summary>
    /// <param name="view">The view identifier.</param>
    /// <param name="property">The property name.</param>
    /// <param name="values">The values to match.</param>
    public FilterBuilder In(ViewIdentifier view, string property, params object[] values)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));

        return In(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, values);
    }

    /// <summary>
    /// Creates a range filter.
    /// </summary>
    /// <param name="property">Property path as [space, "view/version", property].</param>
    /// <param name="gte">Greater than or equal to.</param>
    /// <param name="gt">Greater than.</param>
    /// <param name="lte">Less than or equal to.</param>
    /// <param name="lt">Less than.</param>
    public FilterBuilder Range(string[] property, object? gte = null, object? gt = null, object? lte = null, object? lt = null)
    {
        ValidatePropertyPath(property);
        if (gte == null && gt == null && lte == null && lt == null)
            throw new ArgumentException("At least one range bound must be specified");

        var rangeObj = new Dictionary<string, object> { ["property"] = property };
        if (gte != null) rangeObj["gte"] = gte;
        if (gt != null) rangeObj["gt"] = gt;
        if (lte != null) rangeObj["lte"] = lte;
        if (lt != null) rangeObj["lt"] = lt;
        _filter = new { range = rangeObj };
        return this;
    }

    /// <summary>
    /// Creates a range filter using a ViewIdentifier.
    /// </summary>
    public FilterBuilder Range(ViewIdentifier view, string property, object? gte = null, object? gt = null, object? lte = null, object? lt = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));

        return Range(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, gte, gt, lte, lt);
    }

    /// <summary>
    /// Creates a prefix filter.
    /// </summary>
    public FilterBuilder Prefix(string[] property, string value)
    {
        ValidatePropertyPath(property);
        ValidateStringParameter(value, nameof(value));

        _filter = new { prefix = new { property, value } };
        return this;
    }

    /// <summary>
    /// Creates a prefix filter using a ViewIdentifier.
    /// </summary>
    public FilterBuilder Prefix(ViewIdentifier view, string property, string value)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));
        ValidateStringParameter(value, nameof(value));

        return Prefix(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, value);
    }

    /// <summary>
    /// Creates an exists filter.
    /// </summary>
    public FilterBuilder Exists(string[] property)
    {
        ValidatePropertyPath(property);

        _filter = new { exists = new { property } };
        return this;
    }

    /// <summary>
    /// Creates an exists filter using a ViewIdentifier.
    /// </summary>
    public FilterBuilder Exists(ViewIdentifier view, string property)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));

        return Exists(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property });
    }

    /// <summary>
    /// Creates a containsAny filter.
    /// </summary>
    public FilterBuilder ContainsAny(string[] property, params object[] values)
    {
        ValidatePropertyPath(property);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("At least one value must be provided", nameof(values));

        _filter = new { containsAny = new { property, values } };
        return this;
    }

    /// <summary>
    /// Creates a containsAny filter using a ViewIdentifier.
    /// </summary>
    public FilterBuilder ContainsAny(ViewIdentifier view, string property, params object[] values)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));

        return ContainsAny(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, values);
    }

    /// <summary>
    /// Creates a containsAll filter.
    /// </summary>
    public FilterBuilder ContainsAll(string[] property, params object[] values)
    {
        ValidatePropertyPath(property);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
            throw new ArgumentException("At least one value must be provided", nameof(values));

        _filter = new { containsAll = new { property, values } };
        return this;
    }

    /// <summary>
    /// Creates a containsAll filter using a ViewIdentifier.
    /// </summary>
    public FilterBuilder ContainsAll(ViewIdentifier view, string property, params object[] values)
    {
        ArgumentNullException.ThrowIfNull(view);
        ValidateStringParameter(property, nameof(property));

        return ContainsAll(new[] { view.Space, $"{view.ExternalId}/{view.Version}", property }, values);
    }

    /// <summary>
    /// Creates an AND filter combining multiple filters.
    /// </summary>
    /// <param name="filters">The filters to combine.</param>
    /// <exception cref="ArgumentException">Thrown when fewer than 2 filters are provided.</exception>
    public FilterBuilder And(params FilterBuilder[] filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        if (filters.Length < 2)
            throw new ArgumentException("At least two filters must be provided for AND", nameof(filters));

        _filter = new { and = filters.Select(f => f.Build()).ToArray() };
        return this;
    }

    /// <summary>
    /// Chains this filter with another using AND.
    /// </summary>
    /// <remarks>
    /// If this builder has no filter configured, the other filter is used directly.
    /// Otherwise, both filters are combined with AND.
    /// </remarks>
    /// <param name="other">The filter to combine with.</param>
    /// <exception cref="ArgumentNullException">Thrown when other is null.</exception>
    public FilterBuilder And(FilterBuilder other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (_filter == null)
        {
            _filter = other._filter;
        }
        else
        {
            _filter = new { and = new[] { _filter, other.Build() } };
        }
        return this;
    }

    /// <summary>
    /// Creates an OR filter combining multiple filters.
    /// </summary>
    /// <param name="filters">The filters to combine.</param>
    /// <exception cref="ArgumentException">Thrown when fewer than 2 filters are provided.</exception>
    public FilterBuilder Or(params FilterBuilder[] filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        if (filters.Length < 2)
            throw new ArgumentException("At least two filters must be provided for OR", nameof(filters));

        _filter = new { or = filters.Select(f => f.Build()).ToArray() };
        return this;
    }

    /// <summary>
    /// Creates a NOT filter.
    /// </summary>
    /// <param name="filter">The filter to negate.</param>
    public FilterBuilder Not(FilterBuilder filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _filter = new { not = filter.Build() };
        return this;
    }

    /// <summary>
    /// Creates a nested filter.
    /// </summary>
    public FilterBuilder Nested(string[] scope, FilterBuilder filter)
    {
        ValidatePropertyPath(scope);
        ArgumentNullException.ThrowIfNull(filter);

        _filter = new { nested = new { scope, filter = filter.Build() } };
        return this;
    }

    /// <summary>
    /// Builds the filter object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no filter has been configured.</exception>
    public object Build()
    {
        return _filter ?? throw new InvalidOperationException("No filter has been configured");
    }

    /// <summary>
    /// Builds the filter or returns null if no filter has been configured.
    /// </summary>
    /// <remarks>
    /// Useful for optional filter scenarios where a null filter means "no filter".
    /// </remarks>
    public object? BuildOrNull() => _filter;

    /// <summary>
    /// Creates a parameter reference for use in parameterized queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parameter references are substituted at query time with values provided
    /// via <c>QueryBuilderResource.WithParameter()</c>.
    /// </para>
    /// <para>
    /// Parameterized queries enable query plan reuse across queries with different
    /// parameter values, improving performance for read-heavy workloads.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = FilterBuilder.Create()
    ///     .Equals(view, "status", FilterBuilder.Parameter("statusParam"))
    ///     .Build();
    ///     
    /// var result = await queryBuilder
    ///     .WithParameter("statusParam", "Running")
    ///     .WithNodes("equipment", view, filter)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <param name="parameterName">Name of the parameter (must match the name used in WithParameter).</param>
    /// <returns>A parameter reference object for use as a filter value.</returns>
    /// <exception cref="ArgumentException">Thrown when parameterName is null or empty.</exception>
    public static object Parameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));

        return new { parameter = parameterName };
    }

    /// <summary>
    /// Returns a JSON string representation of the filter for debugging.
    /// </summary>
    public override string ToString()
    {
        if (_filter == null)
            return "<no filter configured>";

        try
        {
            return JsonSerializer.Serialize(_filter, JsonOptions);
        }
        catch
        {
            return "<filter serialization failed>";
        }
    }

    private static void ValidatePropertyPath(string[] property)
    {
        if (property is null || property.Length == 0)
            throw new ArgumentException("Property path cannot be null or empty", nameof(property));

        foreach (var segment in property)
        {
            if (string.IsNullOrEmpty(segment))
                throw new ArgumentException("Property path segments cannot be null or empty", nameof(property));
        }
    }

    private static void ValidateStringParameter(string value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
    }
}
