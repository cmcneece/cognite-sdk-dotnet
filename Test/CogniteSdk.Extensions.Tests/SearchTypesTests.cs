// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.Resources;
using CogniteSdk.Types.DataModels.Search;
using Xunit;

namespace CogniteSdk.Extensions.Tests;

public class SearchTypesTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void SearchInstancesRequest_Serializes_Correctly()
    {
        // Arrange
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier
            {
                Type = "view",
                Space = "mySpace",
                ExternalId = "MyView",
                Version = "1"
            },
            Query = "pump*",
            Limit = 100
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"view\"", json);
        Assert.Contains("\"query\":\"pump*\"", json);
        Assert.Contains("\"limit\":100", json);
        Assert.Contains("\"mySpace\"", json);
    }

    [Fact]
    public void SearchInstancesRequest_WithFilter_Serializes()
    {
        // Arrange
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier
            {
                Space = "space",
                ExternalId = "view",
                Version = "1"
            },
            Filter = new { equals = new { property = new[] { "space", "view/1", "status" }, value = "active" } }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"filter\"", json);
        Assert.Contains("\"equals\"", json);
    }

    [Fact]
    public void SearchInstancesRequest_WithSort_Serializes()
    {
        // Arrange
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Query = "test",
            Sort = new[]
            {
                new SearchSort
                {
                    Property = new[] { "space", "view/1", "name" },
                    Direction = "ascending",
                    NullsFirst = false
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"sort\"", json);
        Assert.Contains("\"direction\":\"ascending\"", json);
    }

    [Fact]
    public void SearchInstancesRequest_WithProperties_Serializes()
    {
        // Arrange
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Query = "test",
            Properties = new[] { "name", "description" }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"properties\"", json);
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"description\"", json);
    }

    [Fact]
    public void SearchViewIdentifier_HasCorrectType()
    {
        // Arrange
        var view = new SearchViewIdentifier
        {
            Space = "mySpace",
            ExternalId = "MyView",
            Version = "1"
        };

        // Assert
        Assert.Equal("view", view.Type);
    }

    [Fact]
    public void SearchResource_Constructor_ValidatesParameters()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SearchResource(null!, "project", "url", ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new SearchResource(new HttpClient(), null!, "url", ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new SearchResource(new HttpClient(), "project", null!, ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new SearchResource(new HttpClient(), "project", "url", null!));
    }

    // Request overload validation tests
    [Fact]
    public async Task SearchAsync_WithNullRequest_Throws()
    {
        // Arrange
        var resource = new SearchResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SearchAsync((SearchInstancesRequest)null!));
    }

    [Fact]
    public async Task SearchAsync_WithNullView_Throws()
    {
        // Arrange
        var resource = new SearchResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SearchInstancesRequest { View = null!, Query = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithNoQueryOrFilter_Throws()
    {
        // Arrange
        var resource = new SearchResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" }
            // No Query or Filter
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithInvalidLimit_Throws()
    {
        // Arrange
        var resource = new SearchResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Query = "test",
            Limit = 0 // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.SearchAsync(request));
    }

    [Fact]
    public async Task SearchAsync_WithLimitOver1000_Throws()
    {
        // Arrange
        var resource = new SearchResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SearchInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Query = "test",
            Limit = 1001 // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.SearchAsync(request));
    }

    // SearchViewIdentifier constructor validation tests
    [Fact]
    public void SearchViewIdentifier_Constructor_WithNullSpace_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SearchViewIdentifier(null!, "externalId", "version"));
    }

    [Fact]
    public void SearchViewIdentifier_Constructor_WithEmptySpace_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SearchViewIdentifier("", "externalId", "version"));
    }

    [Fact]
    public void SearchViewIdentifier_Constructor_WithNullExternalId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SearchViewIdentifier("space", null!, "version"));
    }

    [Fact]
    public void SearchViewIdentifier_Constructor_WithNullVersion_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SearchViewIdentifier("space", "externalId", null!));
    }

    [Fact]
    public void SearchViewIdentifier_Constructor_WithValidParams_Succeeds()
    {
        var view = new SearchViewIdentifier("space", "externalId", "1");
        Assert.Equal("space", view.Space);
        Assert.Equal("externalId", view.ExternalId);
        Assert.Equal("1", view.Version);
    }
}
