// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.Resources;
using CogniteSdk.Types.DataModels.Aggregate;
using CogniteSdk.Types.DataModels.Search;
using Xunit;

namespace CogniteSdk.Extensions.Tests;

public class AggregateTypesTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void AggregateInstancesRequest_Serializes_Correctly()
    {
        // Arrange
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier
            {
                Space = "mySpace",
                ExternalId = "MyView",
                Version = "1"
            },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "temperature", Aggregate = "avg" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"view\"", json);
        Assert.Contains("\"aggregates\"", json);
        Assert.Contains("\"temperature\"", json);
        Assert.Contains("\"avg\"", json);
    }

    [Fact]
    public void AggregateInstancesRequest_WithGroupBy_Serializes()
    {
        // Arrange
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "value", Aggregate = "sum" }
            },
            GroupBy = new[] { "category", "status" }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"groupBy\"", json);
        Assert.Contains("\"category\"", json);
        Assert.Contains("\"status\"", json);
    }

    [Fact]
    public void AggregateInstancesRequest_WithFilter_Serializes()
    {
        // Arrange
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "*", Aggregate = "count" }
            },
            Filter = new { range = new { property = new[] { "s", "v/1", "temp" }, gte = 20, lte = 30 } }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"filter\"", json);
        Assert.Contains("\"range\"", json);
    }

    [Fact]
    public void AggregateInstancesRequest_WithHistogram_Serializes()
    {
        // Arrange
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "price", Aggregate = "histogram", Interval = 100.0 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"histogram\"", json);
        Assert.Contains("\"interval\":100", json);
    }

    [Fact]
    public void AggregateInstancesRequest_WithQuery_Serializes()
    {
        // Arrange
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "*", Aggregate = "count" }
            },
            Query = "pump*",
            Properties = new[] { "name" }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"query\":\"pump*\"", json);
        Assert.Contains("\"properties\"", json);
    }

    [Fact]
    public void AggregateOperation_WithAllAggregateTypes()
    {
        // Arrange & Act & Assert - verify all types can be created
        var ops = new[]
        {
            new AggregateOperation { Property = "p", Aggregate = "count" },
            new AggregateOperation { Property = "p", Aggregate = "sum" },
            new AggregateOperation { Property = "p", Aggregate = "avg" },
            new AggregateOperation { Property = "p", Aggregate = "min" },
            new AggregateOperation { Property = "p", Aggregate = "max" },
            new AggregateOperation { Property = "p", Aggregate = "histogram", Interval = 10 }
        };

        Assert.Equal(6, ops.Length);
    }

    [Fact]
    public void AggregateType_Enum_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)AggregateType.Count);
        Assert.Equal(1, (int)AggregateType.Sum);
        Assert.Equal(2, (int)AggregateType.Avg);
        Assert.Equal(3, (int)AggregateType.Min);
        Assert.Equal(4, (int)AggregateType.Max);
        Assert.Equal(5, (int)AggregateType.Histogram);
    }

    [Fact]
    public void AggregateResource_Constructor_ValidatesParameters()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AggregateResource(null!, "project", "url", ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new AggregateResource(new HttpClient(), null!, "url", ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new AggregateResource(new HttpClient(), "project", null!, ct => Task.FromResult("token")));
        Assert.Throws<ArgumentNullException>(() =>
            new AggregateResource(new HttpClient(), "project", "url", null!));
    }

    [Fact]
    public void AggregateValue_CanHoldAllTypes()
    {
        // Arrange
        var value = new AggregateValue
        {
            Count = 100,
            Sum = 1500.5,
            Avg = 15.005,
            Min = 0.1,
            Max = 99.9,
            Histogram = new[]
            {
                new HistogramBucket { Start = 0, Count = 10 },
                new HistogramBucket { Start = 10, Count = 20 }
            }
        };

        // Assert
        Assert.Equal(100, value.Count);
        Assert.Equal(1500.5, value.Sum);
        Assert.Equal(15.005, value.Avg);
        Assert.Equal(0.1, value.Min);
        Assert.Equal(99.9, value.Max);
        Assert.NotNull(value.Histogram);
        Assert.Equal(2, value.Histogram.Count);
    }

    [Fact]
    public void HistogramBucket_HasCorrectProperties()
    {
        // Arrange
        var bucket = new HistogramBucket { Start = 100.5, Count = 42 };

        // Assert
        Assert.Equal(100.5, bucket.Start);
        Assert.Equal(42, bucket.Count);
    }

    // Request overload validation tests
    [Fact]
    public async Task AggregateAsync_WithNullRequest_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.AggregateAsync(null!));
    }

    [Fact]
    public async Task AggregateAsync_WithNullView_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = null!,
            Aggregates = new[] { new AggregateOperation { Property = "*", Aggregate = "count" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithEmptyAggregates_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = Array.Empty<AggregateOperation>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithTooManyAggregates_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[]
            {
                new AggregateOperation { Property = "p1", Aggregate = "count" },
                new AggregateOperation { Property = "p2", Aggregate = "count" },
                new AggregateOperation { Property = "p3", Aggregate = "count" },
                new AggregateOperation { Property = "p4", Aggregate = "count" },
                new AggregateOperation { Property = "p5", Aggregate = "count" },
                new AggregateOperation { Property = "p6", Aggregate = "count" } // 6th - exceeds max of 5
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithInvalidLimit_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[] { new AggregateOperation { Property = "*", Aggregate = "count" } },
            Limit = 0 // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithNullAggregateProperty_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[] { new AggregateOperation { Property = null!, Aggregate = "count" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithEmptyAggregateProperty_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[] { new AggregateOperation { Property = "", Aggregate = "count" } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }

    [Fact]
    public async Task AggregateAsync_WithNullAggregateType_Throws()
    {
        // Arrange
        var resource = new AggregateResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new AggregateInstancesRequest
        {
            View = new SearchViewIdentifier { Space = "s", ExternalId = "v", Version = "1" },
            Aggregates = new[] { new AggregateOperation { Property = "*", Aggregate = null! } }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.AggregateAsync(request));
    }
}
