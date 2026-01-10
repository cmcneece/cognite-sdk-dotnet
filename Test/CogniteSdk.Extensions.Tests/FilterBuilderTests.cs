// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Types.DataModels.Query;
using Xunit;

namespace CogniteSdk.Extensions.Tests;

public class FilterBuilderTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void HasData_SingleView_CreatesCorrectFilter()
    {
        // Arrange & Act
        var filter = FilterBuilder.Create()
            .HasData("mySpace", "MyView", "1")
            .Build();

        // Assert - serialize and check JSON structure
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        
        Assert.True(doc.RootElement.TryGetProperty("hasData", out var hasData));
        Assert.Single(hasData.EnumerateArray());
        var view = hasData[0];
        Assert.Equal("mySpace", view.GetProperty("space").GetString());
        Assert.Equal("MyView", view.GetProperty("externalId").GetString());
        Assert.Equal("1", view.GetProperty("version").GetString());
    }

    [Fact]
    public void HasData_MultipleViews_CreatesCorrectFilter()
    {
        // Arrange
        var views = new[]
        {
            new ViewIdentifier("space1", "View1", "1"),
            new ViewIdentifier("space2", "View2", "2")
        };

        // Act
        var filter = FilterBuilder.Create()
            .HasData(views)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("hasData", out var hasData));
        Assert.Equal(2, hasData.GetArrayLength());
    }

    [Fact]
    public void Equals_WithRawPropertyPath_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format: [space, "view/version", property]
        var filter = FilterBuilder.Create()
            .Equals(new[] { "mySpace", "MyView/1", "status" }, "active")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("equals", out var equals));
        Assert.Equal("active", equals.GetProperty("value").GetString());
        
        var property = equals.GetProperty("property");
        Assert.Equal(3, property.GetArrayLength());
        Assert.Equal("mySpace", property[0].GetString());
        Assert.Equal("MyView/1", property[1].GetString());
        Assert.Equal("status", property[2].GetString());
    }

    [Fact]
    public void Equals_WithSimplifiedPath_CreatesCorrectPropertyFormat()
    {
        // Arrange & Act
        var filter = FilterBuilder.Create()
            .Equals("mySpace", "MyView", "1", "status", "active")
            .Build();

        // Assert - property path should be [space, "view/version", property]
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        var equals = doc.RootElement.GetProperty("equals");
        var property = equals.GetProperty("property");
        
        Assert.Equal(3, property.GetArrayLength()); // [space, view/version, property]
        Assert.Equal("mySpace", property[0].GetString());
        Assert.Equal("MyView/1", property[1].GetString()); // view/version combined!
        Assert.Equal("status", property[2].GetString());
    }

    [Fact]
    public void Equals_WithViewIdentifier_CreatesCorrectPropertyFormat()
    {
        // Arrange
        var view = new ViewIdentifier("mySpace", "MyView", "v1");

        // Act
        var filter = FilterBuilder.Create()
            .Equals(view, "status", "active")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        var equals = doc.RootElement.GetProperty("equals");
        var property = equals.GetProperty("property");
        
        Assert.Equal(3, property.GetArrayLength());
        Assert.Equal("mySpace", property[0].GetString());
        Assert.Equal("MyView/v1", property[1].GetString());
        Assert.Equal("status", property[2].GetString());
    }

    [Fact]
    public void In_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .In(new[] { "space", "view/1", "type" }, "A", "B", "C")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("in", out var inFilter));
        Assert.Equal(3, inFilter.GetProperty("values").GetArrayLength());
    }

    [Fact]
    public void In_WithViewIdentifier_CreatesCorrectFilter()
    {
        // Arrange
        var view = new ViewIdentifier("space", "view", "1");

        // Act
        var filter = FilterBuilder.Create()
            .In(view, "type", "A", "B", "C")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        var inFilter = doc.RootElement.GetProperty("in");
        var property = inFilter.GetProperty("property");
        
        Assert.Equal(3, property.GetArrayLength());
        Assert.Equal("view/1", property[1].GetString());
    }

    [Fact]
    public void Range_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .Range(new[] { "space", "view/1", "value" }, gte: 10, lt: 100)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("range", out var range));
        Assert.Equal(10, range.GetProperty("gte").GetInt32());
        Assert.Equal(100, range.GetProperty("lt").GetInt32());
    }

    [Fact]
    public void And_CombinesFilters()
    {
        // Arrange
        var hasData = FilterBuilder.Create().HasData("space", "view", "1");
        var equals = FilterBuilder.Create().Equals("space", "view", "1", "status", "active");

        // Act
        var filter = FilterBuilder.Create()
            .And(hasData, equals)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("and", out var and));
        Assert.Equal(2, and.GetArrayLength());
    }

    [Fact]
    public void And_ChainsFilters()
    {
        // Arrange
        var builder = FilterBuilder.Create()
            .HasData("space", "view", "1");
        var other = FilterBuilder.Create().Equals("space", "view", "1", "status", "active");

        // Act
        var filter = builder.And(other).Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        Assert.Contains("and", json);
    }

    [Fact]
    public void Or_CombinesFilters()
    {
        // Arrange
        var eq1 = FilterBuilder.Create().Equals("space", "view", "1", "type", "A");
        var eq2 = FilterBuilder.Create().Equals("space", "view", "1", "type", "B");

        // Act
        var filter = FilterBuilder.Create()
            .Or(eq1, eq2)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("or", out var or));
        Assert.Equal(2, or.GetArrayLength());
    }

    [Fact]
    public void Not_NegatesFilter()
    {
        // Arrange
        var equals = FilterBuilder.Create().Equals("space", "view", "1", "archived", true);

        // Act
        var filter = FilterBuilder.Create()
            .Not(equals)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        Assert.Contains("not", json);
    }

    [Fact]
    public void Prefix_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .Prefix(new[] { "space", "view/1", "name" }, "Equipment-")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("prefix", out var prefix));
        Assert.Equal("Equipment-", prefix.GetProperty("value").GetString());
    }

    [Fact]
    public void Exists_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .Exists(new[] { "space", "view/1", "serialNumber" })
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        Assert.Contains("exists", json);
    }

    [Fact]
    public void ContainsAny_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .ContainsAny(new[] { "space", "view/1", "tags" }, "critical", "priority")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("containsAny", out var contains));
        Assert.Equal(2, contains.GetProperty("values").GetArrayLength());
    }

    [Fact]
    public void ContainsAll_CreatesCorrectFilter()
    {
        // Arrange & Act - use correct 3-element format
        var filter = FilterBuilder.Create()
            .ContainsAll(new[] { "space", "view/1", "tags" }, "verified", "approved")
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        Assert.Contains("containsAll", json);
    }

    [Fact]
    public void Nested_CreatesCorrectFilter()
    {
        // Arrange
        var innerFilter = FilterBuilder.Create().Equals("space", "view", "1", "name", "Sensor1");

        // Act - use correct 3-element format for scope
        var filter = FilterBuilder.Create()
            .Nested(new[] { "space", "view/1", "equipment" }, innerFilter)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("nested", out var nested));
        Assert.True(nested.TryGetProperty("scope", out _));
        Assert.True(nested.TryGetProperty("filter", out _));
    }

    [Fact]
    public void BuildOrNull_ReturnsNullWhenEmpty()
    {
        // Arrange
        var builder = FilterBuilder.Create();

        // Act
        var filter = builder.BuildOrNull();

        // Assert
        Assert.Null(filter);
    }

    [Fact]
    public void Build_ThrowsWhenEmpty()
    {
        // Arrange
        var builder = FilterBuilder.Create();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void ToString_ReturnsJsonForDebugging()
    {
        // Arrange
        var filter = FilterBuilder.Create()
            .Equals("space", "view", "1", "status", "active");

        // Act
        var result = filter.ToString();

        // Assert
        Assert.Contains("equals", result);
        Assert.Contains("status", result);
    }

    [Fact]
    public void ToString_ReturnsMessageWhenEmpty()
    {
        // Arrange
        var builder = FilterBuilder.Create();

        // Act
        var result = builder.ToString();

        // Assert
        Assert.Equal("<no filter configured>", result);
    }

    // Input validation tests
    [Fact]
    public void Equals_WithNullProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FilterBuilder.Create().Equals(null!, "value"));
    }

    [Fact]
    public void Equals_WithEmptyProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FilterBuilder.Create().Equals(Array.Empty<string>(), "value"));
    }

    [Fact]
    public void Equals_WithNullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilterBuilder.Create().Equals(new[] { "space", "view/1", "prop" }, null!));
    }

    [Fact]
    public void Equals_WithNullView_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FilterBuilder.Create().Equals((ViewIdentifier)null!, "prop", "value"));
    }

    [Fact]
    public void HasData_WithEmptyViews_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FilterBuilder.Create().HasData(Array.Empty<ViewIdentifier>()));
    }

    [Fact]
    public void And_ParamsOverload_WithSingleFilter_ThrowsArgumentException()
    {
        var single = FilterBuilder.Create().HasData("s", "v", "1");
        // Call the params overload with a single-element array
        Assert.Throws<ArgumentException>(() =>
            FilterBuilder.Create().And(new[] { single }));
    }

    [Fact]
    public void Range_WithNoBounds_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            FilterBuilder.Create().Range(new[] { "space", "view/1", "value" }));
    }

    // Parameter tests
    [Fact]
    public void Parameter_CreatesCorrectReference()
    {
        // Act
        var paramRef = FilterBuilder.Parameter("myParam");

        // Assert
        var json = JsonSerializer.Serialize(paramRef, _jsonOptions);
        var doc = JsonDocument.Parse(json);
        
        Assert.True(doc.RootElement.TryGetProperty("parameter", out var param));
        Assert.Equal("myParam", param.GetString());
    }

    [Fact]
    public void Parameter_WithNullName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FilterBuilder.Parameter(null!));
    }

    [Fact]
    public void Parameter_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => FilterBuilder.Parameter(""));
    }

    [Fact]
    public void Equals_WithParameterValue_SerializesCorrectly()
    {
        // Arrange
        var view = new ViewIdentifier("mySpace", "MyView", "1");
        var paramRef = FilterBuilder.Parameter("statusParam");

        // Act
        var filter = FilterBuilder.Create()
            .Equals(view, "status", paramRef)
            .Build();

        // Assert
        var json = JsonSerializer.Serialize(filter, _jsonOptions);
        Assert.Contains("\"parameter\":\"statusParam\"", json);
    }
}
