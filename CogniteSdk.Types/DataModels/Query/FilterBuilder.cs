// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CogniteSdk.DataModels
{
    /// <summary>
    /// Fluent builder for DMS filters.
    /// Produces <see cref="IDMSFilter"/> objects compatible with the CDF API.
    /// </summary>
    /// <remarks>
    /// <para>Property paths use the format: [space, "viewExternalId/version", property]</para>
    /// <para>This builder is not thread-safe. Create a new instance for each filter.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = FilterBuilder.Create()
    ///     .Equals(view, "status", "Running")
    ///     .Build();
    /// </code>
    /// </example>
    public class FilterBuilder
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private IDMSFilter _filter;

        /// <summary>
        /// Creates a new filter builder.
        /// </summary>
        public static FilterBuilder Create()
        {
            return new FilterBuilder();
        }

        /// <summary>
        /// Creates a hasData filter for views.
        /// </summary>
        /// <param name="views">The views to filter by.</param>
        /// <exception cref="ArgumentNullException">Thrown when views is null.</exception>
        /// <exception cref="ArgumentException">Thrown when views is empty.</exception>
        public FilterBuilder HasData(params ViewIdentifier[] views)
        {
            if (views == null) throw new ArgumentNullException(nameof(views));
            if (views.Length == 0)
                throw new ArgumentException("At least one view must be provided", nameof(views));

            _filter = new HasDataFilter
            {
                Models = views.Select(v => (SourceIdentifier)new ViewIdentifier
                {
                    Type = PropertySourceType.view,
                    Space = v.Space,
                    ExternalId = v.ExternalId,
                    Version = v.Version
                }).ToList()
            };
            return this;
        }

        /// <summary>
        /// Creates a hasData filter for a single view.
        /// </summary>
        /// <param name="space">The space external ID.</param>
        /// <param name="externalId">The view external ID.</param>
        /// <param name="version">The view version.</param>
        public FilterBuilder HasData(string space, string externalId, string version)
        {
            ValidateStringParameter(space, nameof(space));
            ValidateStringParameter(externalId, nameof(externalId));
            ValidateStringParameter(version, nameof(version));

            return HasData(new ViewIdentifier { Space = space, ExternalId = externalId, Version = version });
        }

        /// <summary>
        /// Creates an equals filter.
        /// </summary>
        /// <param name="property">Property path as [space, "view/version", property].</param>
        /// <param name="value">The value to compare.</param>
        public FilterBuilder Equals(IEnumerable<string> property, IDMSValue value)
        {
            ValidatePropertyPath(property);
            if (value == null) throw new ArgumentNullException(nameof(value));

            _filter = new EqualsFilter { Property = property, Value = value };
            return this;
        }

        /// <summary>
        /// Creates an equals filter using a ViewIdentifier.
        /// </summary>
        /// <param name="view">The view identifier.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="value">The value to compare.</param>
        public FilterBuilder Equals(ViewIdentifier view, string propertyName, IDMSValue value)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return Equals(BuildPropertyPath(view, propertyName), value);
        }

        /// <summary>
        /// Creates an equals filter with a string value.
        /// </summary>
        public FilterBuilder Equals(ViewIdentifier view, string propertyName, string value)
        {
            return Equals(view, propertyName, new RawPropertyValue<string>(value));
        }

        /// <summary>
        /// Creates an equals filter with a numeric value.
        /// </summary>
        public FilterBuilder Equals(ViewIdentifier view, string propertyName, double value)
        {
            return Equals(view, propertyName, new RawPropertyValue<double>(value));
        }

        /// <summary>
        /// Creates an equals filter with an integer value.
        /// </summary>
        public FilterBuilder Equals(ViewIdentifier view, string propertyName, long value)
        {
            return Equals(view, propertyName, new RawPropertyValue<double>(value));
        }

        /// <summary>
        /// Creates an equals filter with a boolean value.
        /// </summary>
        public FilterBuilder Equals(ViewIdentifier view, string propertyName, bool value)
        {
            return Equals(view, propertyName, new RawPropertyValue<bool>(value));
        }

        /// <summary>
        /// Creates an "in" filter.
        /// </summary>
        /// <param name="property">Property path as [space, "view/version", property].</param>
        /// <param name="values">The values to match.</param>
        public FilterBuilder In(IEnumerable<string> property, IEnumerable<IDMSValue> values)
        {
            ValidatePropertyPath(property);
            if (values == null) throw new ArgumentNullException(nameof(values));
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                throw new ArgumentException("At least one value must be provided", nameof(values));

            _filter = new InFilter { Property = property, Values = valuesList };
            return this;
        }

        /// <summary>
        /// Creates an "in" filter using a ViewIdentifier with string values.
        /// </summary>
        public FilterBuilder In(ViewIdentifier view, string propertyName, params string[] values)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return In(BuildPropertyPath(view, propertyName), values.Select(v => (IDMSValue)new RawPropertyValue<string>(v)));
        }

        /// <summary>
        /// Creates a range filter.
        /// </summary>
        /// <param name="property">Property path as [space, "view/version", property].</param>
        /// <param name="gte">Greater than or equal to.</param>
        /// <param name="gt">Greater than.</param>
        /// <param name="lte">Less than or equal to.</param>
        /// <param name="lt">Less than.</param>
        public FilterBuilder Range(IEnumerable<string> property, IDMSValue gte = null, IDMSValue gt = null, IDMSValue lte = null, IDMSValue lt = null)
        {
            ValidatePropertyPath(property);
            if (gte == null && gt == null && lte == null && lt == null)
                throw new ArgumentException("At least one range bound must be specified");

            _filter = new RangeFilter
            {
                Property = property,
                GreaterThanEqual = gte,
                GreaterThan = gt,
                LessThanEqual = lte,
                LessThan = lt
            };
            return this;
        }

        /// <summary>
        /// Creates a range filter using a ViewIdentifier with numeric bounds.
        /// </summary>
        public FilterBuilder Range(ViewIdentifier view, string propertyName, double? gte = null, double? gt = null, double? lte = null, double? lt = null)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return Range(
                BuildPropertyPath(view, propertyName),
                gte.HasValue ? new RawPropertyValue<double>(gte.Value) : null,
                gt.HasValue ? new RawPropertyValue<double>(gt.Value) : null,
                lte.HasValue ? new RawPropertyValue<double>(lte.Value) : null,
                lt.HasValue ? new RawPropertyValue<double>(lt.Value) : null
            );
        }

        /// <summary>
        /// Creates a prefix filter.
        /// </summary>
        public FilterBuilder Prefix(IEnumerable<string> property, string value)
        {
            ValidatePropertyPath(property);
            ValidateStringParameter(value, nameof(value));

            _filter = new PrefixFilter { Property = property, Value = new RawPropertyValue<string>(value) };
            return this;
        }

        /// <summary>
        /// Creates a prefix filter using a ViewIdentifier.
        /// </summary>
        public FilterBuilder Prefix(ViewIdentifier view, string propertyName, string value)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return Prefix(BuildPropertyPath(view, propertyName), value);
        }

        /// <summary>
        /// Creates an exists filter.
        /// </summary>
        public FilterBuilder Exists(IEnumerable<string> property)
        {
            ValidatePropertyPath(property);

            _filter = new ExistsFilter { Property = property };
            return this;
        }

        /// <summary>
        /// Creates an exists filter using a ViewIdentifier.
        /// </summary>
        public FilterBuilder Exists(ViewIdentifier view, string propertyName)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return Exists(BuildPropertyPath(view, propertyName));
        }

        /// <summary>
        /// Creates a containsAny filter.
        /// </summary>
        public FilterBuilder ContainsAny(IEnumerable<string> property, IEnumerable<IDMSValue> values)
        {
            ValidatePropertyPath(property);
            if (values == null) throw new ArgumentNullException(nameof(values));
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                throw new ArgumentException("At least one value must be provided", nameof(values));

            _filter = new ContainsAnyFilter { Property = property, Values = valuesList };
            return this;
        }

        /// <summary>
        /// Creates a containsAny filter using a ViewIdentifier with string values.
        /// </summary>
        public FilterBuilder ContainsAny(ViewIdentifier view, string propertyName, params string[] values)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return ContainsAny(BuildPropertyPath(view, propertyName), values.Select(v => (IDMSValue)new RawPropertyValue<string>(v)));
        }

        /// <summary>
        /// Creates a containsAll filter.
        /// </summary>
        public FilterBuilder ContainsAll(IEnumerable<string> property, IEnumerable<IDMSValue> values)
        {
            ValidatePropertyPath(property);
            if (values == null) throw new ArgumentNullException(nameof(values));
            var valuesList = values.ToList();
            if (valuesList.Count == 0)
                throw new ArgumentException("At least one value must be provided", nameof(values));

            _filter = new ContainsAllFilter { Property = property, Values = valuesList };
            return this;
        }

        /// <summary>
        /// Creates a containsAll filter using a ViewIdentifier with string values.
        /// </summary>
        public FilterBuilder ContainsAll(ViewIdentifier view, string propertyName, params string[] values)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            ValidateStringParameter(propertyName, nameof(propertyName));

            return ContainsAll(BuildPropertyPath(view, propertyName), values.Select(v => (IDMSValue)new RawPropertyValue<string>(v)));
        }

        /// <summary>
        /// Creates an AND filter combining multiple filters.
        /// </summary>
        /// <param name="filters">The filters to combine.</param>
        public FilterBuilder And(params FilterBuilder[] filters)
        {
            if (filters == null) throw new ArgumentNullException(nameof(filters));
            if (filters.Length < 2)
                throw new ArgumentException("At least two filters must be provided for AND", nameof(filters));

            _filter = new AndFilter { And = filters.Select(f => f.Build()).ToList() };
            return this;
        }

        /// <summary>
        /// Chains this filter with another using AND.
        /// </summary>
        /// <param name="other">The filter to combine with.</param>
        public FilterBuilder And(FilterBuilder other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            if (_filter == null)
            {
                _filter = other._filter;
            }
            else
            {
                _filter = new AndFilter { And = new List<IDMSFilter> { _filter, other.Build() } };
            }
            return this;
        }

        /// <summary>
        /// Creates an OR filter combining multiple filters.
        /// </summary>
        /// <param name="filters">The filters to combine.</param>
        public FilterBuilder Or(params FilterBuilder[] filters)
        {
            if (filters == null) throw new ArgumentNullException(nameof(filters));
            if (filters.Length < 2)
                throw new ArgumentException("At least two filters must be provided for OR", nameof(filters));

            _filter = new OrFilter { Or = filters.Select(f => f.Build()).ToList() };
            return this;
        }

        /// <summary>
        /// Creates a NOT filter.
        /// </summary>
        /// <param name="filter">The filter to negate.</param>
        public FilterBuilder Not(FilterBuilder filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            _filter = new NotFilter { Not = filter.Build() };
            return this;
        }

        /// <summary>
        /// Creates a nested filter for direct relations.
        /// </summary>
        public FilterBuilder Nested(IEnumerable<string> scope, FilterBuilder filter)
        {
            ValidatePropertyPath(scope);
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            _filter = new NestedFilter { Scope = scope, Filter = filter.Build() };
            return this;
        }

        /// <summary>
        /// Creates a matchAll filter that matches all instances.
        /// </summary>
        public FilterBuilder MatchAll()
        {
            _filter = new MatchAllFilter();
            return this;
        }

        /// <summary>
        /// Creates a parameter reference for use in parameterized queries.
        /// </summary>
        /// <remarks>
        /// Parameter references are substituted at query time with values provided in Query.Parameters.
        /// Parameterized queries enable query plan reuse across queries with different parameter values.
        /// </remarks>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns>A parameter reference for use as a filter value.</returns>
        public static IDMSValue Parameter(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty", nameof(parameterName));

            return new ParameterizedPropertyValue { Parameter = parameterName };
        }

        /// <summary>
        /// Builds the filter.
        /// </summary>
        /// <returns>The constructed <see cref="IDMSFilter"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no filter has been configured.</exception>
        public IDMSFilter Build()
        {
            return _filter ?? throw new InvalidOperationException("No filter has been configured");
        }

        /// <summary>
        /// Builds the filter or returns null if no filter has been configured.
        /// </summary>
        public IDMSFilter BuildOrNull()
        {
            return _filter;
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
                return JsonSerializer.Serialize(_filter, _filter.GetType(), JsonOptions);
            }
            catch
            {
                return "<filter serialization failed>";
            }
        }

        private static IEnumerable<string> BuildPropertyPath(ViewIdentifier view, string propertyName)
        {
            return new[] { view.Space, $"{view.ExternalId}/{view.Version}", propertyName };
        }

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

        private static void ValidateStringParameter(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{paramName} cannot be null or empty", paramName);
        }
    }
}
