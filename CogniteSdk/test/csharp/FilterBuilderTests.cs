// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using CogniteSdk.DataModels;
using Xunit;

namespace Test.CSharp.Unit
{
    /// <summary>
    /// Unit tests for the FilterBuilder class.
    /// </summary>
    public class FilterBuilderTests
    {
        private readonly ViewIdentifier _testView = new ViewIdentifier
        {
            Space = "test-space",
            ExternalId = "test-view",
            Version = "1"
        };

        [Fact]
        public void Create_ReturnsNewInstance()
        {
            var builder = FilterBuilder.Create();
            Assert.NotNull(builder);
        }

        [Fact]
        public void Build_WithNoFilter_ThrowsInvalidOperationException()
        {
            var builder = FilterBuilder.Create();
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void BuildOrNull_WithNoFilter_ReturnsNull()
        {
            var builder = FilterBuilder.Create();
            Assert.Null(builder.BuildOrNull());
        }

        [Fact]
        public void HasData_WithValidView_CreatesHasDataFilter()
        {
            var filter = FilterBuilder.Create()
                .HasData(_testView)
                .Build();

            Assert.IsType<HasDataFilter>(filter);
            var hasDataFilter = (HasDataFilter)filter;
            Assert.Single(hasDataFilter.Models);
            var model = hasDataFilter.Models.First() as ViewIdentifier;
            Assert.NotNull(model);
            Assert.Equal("test-space", model.Space);
            Assert.Equal("test-view", model.ExternalId);
            Assert.Equal("1", model.Version);
        }

        [Fact]
        public void HasData_WithMultipleViews_CreatesHasDataFilterWithAllViews()
        {
            var view2 = new ViewIdentifier { Space = "space2", ExternalId = "view2", Version = "2" };
            var filter = FilterBuilder.Create()
                .HasData(_testView, view2)
                .Build();

            var hasDataFilter = (HasDataFilter)filter;
            Assert.Equal(2, hasDataFilter.Models.Count());
        }

        [Fact]
        public void HasData_WithNullViews_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                FilterBuilder.Create().HasData((ViewIdentifier[])null));
        }

        [Fact]
        public void HasData_WithEmptyViews_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                FilterBuilder.Create().HasData(new ViewIdentifier[0]));
        }

        [Fact]
        public void Equals_WithStringValue_CreatesEqualsFilter()
        {
            var filter = FilterBuilder.Create()
                .Equals(_testView, "status", "active")
                .Build();

            Assert.IsType<EqualsFilter>(filter);
            var equalsFilter = (EqualsFilter)filter;
            Assert.Equal(3, equalsFilter.Property.Count());
            Assert.IsType<RawPropertyValue<string>>(equalsFilter.Value);
            Assert.Equal("active", ((RawPropertyValue<string>)equalsFilter.Value).Value);
        }

        [Fact]
        public void Equals_WithDoubleValue_CreatesEqualsFilter()
        {
            var filter = FilterBuilder.Create()
                .Equals(_testView, "temperature", 25.5)
                .Build();

            var equalsFilter = (EqualsFilter)filter;
            Assert.IsType<RawPropertyValue<double>>(equalsFilter.Value);
            Assert.Equal(25.5, ((RawPropertyValue<double>)equalsFilter.Value).Value);
        }

        [Fact]
        public void Equals_WithBoolValue_CreatesEqualsFilter()
        {
            var filter = FilterBuilder.Create()
                .Equals(_testView, "active", true)
                .Build();

            var equalsFilter = (EqualsFilter)filter;
            Assert.IsType<RawPropertyValue<bool>>(equalsFilter.Value);
            Assert.True(((RawPropertyValue<bool>)equalsFilter.Value).Value);
        }

        [Fact]
        public void In_WithStringValues_CreatesInFilter()
        {
            var filter = FilterBuilder.Create()
                .In(_testView, "status", "active", "pending", "complete")
                .Build();

            Assert.IsType<InFilter>(filter);
            var inFilter = (InFilter)filter;
            Assert.Equal(3, inFilter.Values.Count());
        }

        [Fact]
        public void Range_WithBounds_CreatesRangeFilter()
        {
            var filter = FilterBuilder.Create()
                .Range(_testView, "temperature", gte: 10.0, lte: 30.0)
                .Build();

            Assert.IsType<RangeFilter>(filter);
            var rangeFilter = (RangeFilter)filter;
            Assert.NotNull(rangeFilter.GreaterThanEqual);
            Assert.NotNull(rangeFilter.LessThanEqual);
            Assert.Null(rangeFilter.GreaterThan);
            Assert.Null(rangeFilter.LessThan);
        }

        [Fact]
        public void Range_WithNoBounds_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                FilterBuilder.Create().Range(_testView, "temperature"));
        }

        [Fact]
        public void Prefix_CreatesFilter()
        {
            var filter = FilterBuilder.Create()
                .Prefix(_testView, "name", "pump-")
                .Build();

            Assert.IsType<PrefixFilter>(filter);
        }

        [Fact]
        public void Exists_CreatesFilter()
        {
            var filter = FilterBuilder.Create()
                .Exists(_testView, "description")
                .Build();

            Assert.IsType<ExistsFilter>(filter);
        }

        [Fact]
        public void ContainsAny_CreatesFilter()
        {
            var filter = FilterBuilder.Create()
                .ContainsAny(_testView, "tags", "tag1", "tag2")
                .Build();

            Assert.IsType<ContainsAnyFilter>(filter);
        }

        [Fact]
        public void ContainsAll_CreatesFilter()
        {
            var filter = FilterBuilder.Create()
                .ContainsAll(_testView, "tags", "required1", "required2")
                .Build();

            Assert.IsType<ContainsAllFilter>(filter);
        }

        [Fact]
        public void And_WithTwoFilters_CreatesAndFilter()
        {
            var filter1 = FilterBuilder.Create().Equals(_testView, "status", "active");
            var filter2 = FilterBuilder.Create().Equals(_testView, "type", "pump");

            var filter = FilterBuilder.Create()
                .And(filter1, filter2)
                .Build();

            Assert.IsType<AndFilter>(filter);
            var andFilter = (AndFilter)filter;
            Assert.Equal(2, andFilter.And.Count());
        }

        [Fact]
        public void And_ChainedWithSingleFilter_CreatesAndFilter()
        {
            var filter = FilterBuilder.Create()
                .Equals(_testView, "status", "active")
                .And(FilterBuilder.Create().Equals(_testView, "type", "pump"))
                .Build();

            Assert.IsType<AndFilter>(filter);
        }

        [Fact]
        public void Or_WithTwoFilters_CreatesOrFilter()
        {
            var filter1 = FilterBuilder.Create().Equals(_testView, "status", "active");
            var filter2 = FilterBuilder.Create().Equals(_testView, "status", "pending");

            var filter = FilterBuilder.Create()
                .Or(filter1, filter2)
                .Build();

            Assert.IsType<OrFilter>(filter);
        }

        [Fact]
        public void Not_CreatesNotFilter()
        {
            var innerFilter = FilterBuilder.Create().Equals(_testView, "status", "deleted");
            var filter = FilterBuilder.Create()
                .Not(innerFilter)
                .Build();

            Assert.IsType<NotFilter>(filter);
        }

        [Fact]
        public void MatchAll_CreatesMatchAllFilter()
        {
            var filter = FilterBuilder.Create()
                .MatchAll()
                .Build();

            Assert.IsType<MatchAllFilter>(filter);
        }

        [Fact]
        public void Parameter_CreatesParameterizedValue()
        {
            var paramValue = FilterBuilder.Parameter("myParam");

            Assert.IsType<ParameterizedPropertyValue>(paramValue);
            Assert.Equal("myParam", ((ParameterizedPropertyValue)paramValue).Parameter);
        }

        [Fact]
        public void Parameter_WithEmptyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => FilterBuilder.Parameter(""));
        }

        [Fact]
        public void ToString_WithNoFilter_ReturnsMessage()
        {
            var builder = FilterBuilder.Create();
            Assert.Equal("<no filter configured>", builder.ToString());
        }

        [Fact]
        public void ToString_WithFilter_ReturnsJsonString()
        {
            var builder = FilterBuilder.Create().MatchAll();
            var result = builder.ToString();
            Assert.NotNull(result);
            Assert.NotEqual("<no filter configured>", result);
        }
    }
}
