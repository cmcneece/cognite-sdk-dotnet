// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.DataModels;
using CogniteSdk.Resources;
using CogniteSdk.Types.DataModels.Query;
using Xunit;

namespace CogniteSdk.Extensions.Tests;

public class QueryBuilderTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static QueryBuilderResource CreateBuilder()
    {
        var httpClient = new HttpClient();
        return new QueryBuilderResource(
            httpClient,
            "test-project",
            "https://api.cognitedata.com",
            ct => Task.FromResult("test-token")
        );
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBuilderResource(null!, "project", "url", ct => Task.FromResult("token")));
    }

    [Fact]
    public void Constructor_WithNullProject_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBuilderResource(new HttpClient(), null!, "url", ct => Task.FromResult("token")));
    }

    [Fact]
    public void Constructor_WithNullBaseUrl_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBuilderResource(new HttpClient(), "project", null!, ct => Task.FromResult("token")));
    }

    [Fact]
    public void Constructor_WithNullTokenProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new QueryBuilderResource(new HttpClient(), "project", "url", null!));
    }

    [Fact]
    public void WithNodes_CreatesCorrectExpression()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view);
        var request = builder.Build();

        // Assert
        Assert.True(request.With.ContainsKey("equipment"));
        Assert.NotNull(request.With["equipment"].Nodes);
        Assert.Null(request.With["equipment"].Edges);
    }

    [Fact]
    public void WithNodes_AddsHasDataFilter()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view);
        var request = builder.Build();

        // Assert - check the filter contains hasData
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        Assert.Contains("hasData", json);
        Assert.Contains("mySpace", json);
        Assert.Contains("Equipment", json);
    }

    [Fact]
    public void WithNodes_WithFilter_CombinesFilters()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        var filter = FilterBuilder.Create().Equals(view, "status", "Running").Build();

        // Act
        builder.WithNodes("equipment", view, filter: filter);
        var request = builder.Build();

        // Assert - should have an AND combining hasData and equals
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        Assert.Contains("and", json);
        Assert.Contains("hasData", json);
        Assert.Contains("equals", json);
    }

    [Fact]
    public void WithEdges_CreatesCorrectExpression()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("sensors", from: "equipment");
        var request = builder.Build();

        // Assert
        Assert.True(request.With.ContainsKey("sensors"));
        Assert.NotNull(request.With["sensors"].Edges);
        Assert.Equal("equipment", request.With["sensors"].Edges!.From);
        Assert.Equal("outwards", request.With["sensors"].Edges!.Direction);
    }

    [Fact]
    public void WithEdges_WithInwardsDirection_SetsCorrectDirection()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("parentEdges", from: "equipment", direction: EdgeDirection.Inwards);
        var request = builder.Build();

        // Assert
        Assert.Equal("inwards", request.With["parentEdges"].Edges!.Direction);
    }

    [Fact]
    public void WithNodesFrom_CreatesCorrectTraversal()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("sensorEdges", from: "equipment")
               .WithNodesFrom("sensorNodes", from: "sensorEdges", chainTo: "destination");
        var request = builder.Build();

        // Assert
        Assert.True(request.With.ContainsKey("sensorNodes"));
        Assert.NotNull(request.With["sensorNodes"].Nodes);
        Assert.Equal("sensorEdges", request.With["sensorNodes"].Nodes!.From);
        Assert.Equal("destination", request.With["sensorNodes"].Nodes!.ChainTo);
    }

    [Fact]
    public void Select_AddsSourceSelection()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .Select("equipment", view, "name", "status");
        var request = builder.Build();

        // Assert
        Assert.True(request.Select.ContainsKey("equipment"));
        Assert.Single(request.Select["equipment"].Sources);
        Assert.Equal(2, request.Select["equipment"].Sources[0].Properties.Count);
    }

    [Fact]
    public void Build_AutoGeneratesSelectForUnselectedResultSets()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act - only add WithNodes, no explicit Select
        builder.WithNodes("equipment", view);
        var request = builder.Build();

        // Assert - should auto-generate select with empty properties (all)
        Assert.True(request.Select.ContainsKey("equipment"));
        Assert.Empty(request.Select["equipment"].Sources[0].Properties);
    }

    [Fact]
    public void Build_IncludesCursors()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        var cursors = new Dictionary<string, string?> { ["equipment"] = "cursor123" };

        // Act
        builder.WithNodes("equipment", view)
               .WithCursors(cursors);
        var request = builder.Build();

        // Assert
        Assert.NotNull(request.Cursors);
        Assert.Equal("cursor123", request.Cursors["equipment"]);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        builder.WithNodes("equipment", view)
               .WithCursors(new Dictionary<string, string?> { ["equipment"] = "cursor" });

        // Act
        builder.Reset();

        // Assert - Build should now throw because no result sets
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithNoResultSets_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    // Input validation tests
    [Fact]
    public void WithNodes_WithNullName_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("space", "view", "1");

        Assert.Throws<ArgumentException>(() => builder.WithNodes(null!, view));
    }

    [Fact]
    public void WithNodes_WithEmptyName_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("space", "view", "1");

        Assert.Throws<ArgumentException>(() => builder.WithNodes("", view));
    }

    [Fact]
    public void WithNodes_WithNullView_ThrowsArgumentNullException()
    {
        var builder = CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithNodes("test", null!));
    }

    [Fact]
    public void WithNodes_WithZeroLimit_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("space", "view", "1");

        Assert.Throws<ArgumentException>(() => builder.WithNodes("test", view, limit: 0));
    }

    [Fact]
    public void WithEdges_WithNullFrom_ThrowsArgumentException()
    {
        var builder = CreateBuilder();

        Assert.Throws<ArgumentException>(() => builder.WithEdges("edges", from: null!));
    }

    [Fact]
    public void WithNodesFrom_WithInvalidChainTo_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("space", "view", "1");
        builder.WithNodes("nodes", view);

        Assert.Throws<ArgumentException>(() => builder.WithNodesFrom("next", from: "nodes", chainTo: "invalid"));
    }

    [Fact]
    public void Select_WithNullResultSetName_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("space", "view", "1");

        Assert.Throws<ArgumentException>(() => builder.Select(null!, view));
    }

    [Fact]
    public void Select_WithNullView_ThrowsArgumentNullException()
    {
        var builder = CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Select("test", null!));
    }

    // Parameter tests
    [Fact]
    public void WithParameter_AddsSingleParameter()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithParameter("status", "Running");
        var request = builder.Build();

        // Assert
        Assert.NotNull(request.Parameters);
        Assert.True(request.Parameters.ContainsKey("status"));
        Assert.Equal("Running", request.Parameters["status"]);
    }

    [Fact]
    public void WithParameters_AddsMultipleParameters()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        var parameters = new Dictionary<string, object?>
        {
            ["status"] = "Running",
            ["minTemp"] = 20,
            ["maxTemp"] = 100
        };

        // Act
        builder.WithNodes("equipment", view)
               .WithParameters(parameters);
        var request = builder.Build();

        // Assert
        Assert.NotNull(request.Parameters);
        Assert.Equal(3, request.Parameters.Count);
        Assert.Equal("Running", request.Parameters["status"]);
        Assert.Equal(20, request.Parameters["minTemp"]);
    }

    [Fact]
    public void WithParameter_WithNullName_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithParameter(null!, "value"));
    }

    [Fact]
    public void WithParameter_WithEmptyName_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentException>(() => builder.WithParameter("", "value"));
    }

    [Fact]
    public void WithParameters_WithNullDictionary_ThrowsArgumentNullException()
    {
        var builder = CreateBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithParameters(null!));
    }

    [Fact]
    public void Reset_ClearsParameters()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        builder.WithNodes("equipment", view)
               .WithParameter("status", "Running");

        // Act
        builder.Reset().WithNodes("equipment", view);
        var request = builder.Build();

        // Assert - parameters should be null after reset
        Assert.Null(request.Parameters);
    }

    // Edge traversal tests
    [Fact]
    public void WithEdges_WithMaxDistance_SetsCorrectValue()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("connections", from: "equipment", maxDistance: 3);
        var request = builder.Build();

        // Assert
        Assert.Equal(3, request.With["connections"].Edges!.MaxDistance);
    }

    [Fact]
    public void WithEdges_WithNodeFilter_SetsCorrectFilter()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        var nodeFilter = new { exists = new { property = new[] { "space", "view/1", "prop" } } };

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("connections", from: "equipment", nodeFilter: nodeFilter);
        var request = builder.Build();

        // Assert
        Assert.NotNull(request.With["connections"].Edges!.NodeFilter);
    }

    [Fact]
    public void WithEdges_WithTerminationFilter_SetsCorrectFilter()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        var terminationFilter = new { equals = new { property = new[] { "space", "view/1", "isLeaf" }, value = true } };

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("connections", from: "equipment", terminationFilter: terminationFilter);
        var request = builder.Build();

        // Assert
        Assert.NotNull(request.With["connections"].Edges!.TerminationFilter);
    }

    [Fact]
    public void WithEdges_WithLimitEach_RequiresMaxDistanceOne()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act & Assert - should throw when maxDistance != 1
        builder.WithNodes("equipment", view);
        Assert.Throws<ArgumentException>(() => 
            builder.WithEdges("connections", from: "equipment", maxDistance: 2, limitEach: 10));
    }

    [Fact]
    public void WithEdges_WithLimitEachAndMaxDistanceOne_Succeeds()
    {
        // Arrange
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");

        // Act
        builder.WithNodes("equipment", view)
               .WithEdges("connections", from: "equipment", maxDistance: 1, limitEach: 10);
        var request = builder.Build();

        // Assert
        Assert.Equal(10, request.With["connections"].Edges!.LimitEach);
    }

    [Fact]
    public void WithEdges_WithZeroMaxDistance_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        builder.WithNodes("equipment", view);

        Assert.Throws<ArgumentException>(() => 
            builder.WithEdges("edges", from: "equipment", maxDistance: 0));
    }

    [Fact]
    public void WithEdges_WithZeroLimitEach_ThrowsArgumentException()
    {
        var builder = CreateBuilder();
        var view = new ViewIdentifier("mySpace", "Equipment", "1");
        builder.WithNodes("equipment", view);

        Assert.Throws<ArgumentException>(() => 
            builder.WithEdges("edges", from: "equipment", maxDistance: 1, limitEach: 0));
    }
}
