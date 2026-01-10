// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.Types.DataModels.Sync;
using Xunit;

// Alias to avoid ambiguity with SDK types
using SelectExpression = CogniteSdk.Types.DataModels.Sync.SelectExpression;
using SourceSelection = CogniteSdk.Types.DataModels.Sync.SourceSelection;

namespace CogniteSdk.Extensions.Tests;

public class SyncTypesTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void SyncInstancesRequest_Serializes_Correctly()
    {
        // Arrange
        var hasDataFilter = new
        {
            hasData = new[]
            {
                new { type = "view", space = "mySpace", externalId = "MyView", version = "1" }
            }
        };

        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery { Filter = hasDataFilter },
                    Limit = 100
                }
            },
            Select = new Dictionary<string, SelectExpression>
            {
                ["items"] = new SelectExpression
                {
                    Sources = new[]
                    {
                        new SourceSelection
                        {
                            Source = new { type = "view", space = "mySpace", externalId = "MyView", version = "1" },
                            Properties = new[] { "name", "description" }
                        }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"with\"", json);
        Assert.Contains("\"items\"", json);
        Assert.Contains("\"nodes\"", json);
        Assert.Contains("\"filter\"", json);
        Assert.Contains("\"hasData\"", json);
        Assert.Contains("\"select\"", json);
        Assert.Contains("\"sources\"", json);
        Assert.Contains("\"mySpace\"", json);
    }

    [Fact]
    public void SyncInstancesRequest_WithCursors_Serializes_Correctly()
    {
        // Arrange
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "space", externalId = "view", version = "1" } } }
                    }
                }
            },
            Select = new Dictionary<string, SelectExpression>(),
            Cursors = new Dictionary<string, string?>
            {
                ["items"] = "cursor123"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"cursors\"", json);
        Assert.Contains("\"cursor123\"", json);
    }

    [Fact]
    public void SyncInstancesResponse_ParsesCorrectly()
    {
        // Arrange - simulated API response
        var responseJson = @"{
            ""items"": {
                ""nodes"": [
                    { ""space"": ""mySpace"", ""externalId"": ""node1"" },
                    { ""space"": ""mySpace"", ""externalId"": ""node2"" }
                ]
            },
            ""nextCursor"": {
                ""nodes"": ""nextCursor123""
            }
        }";

        // Act
        var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("items", out var items));
        Assert.True(items.TryGetProperty("nodes", out var nodes));
        Assert.Equal(2, nodes.GetArrayLength());
        Assert.True(root.TryGetProperty("nextCursor", out _));
    }

    [Fact]
    public void SyncNodesQuery_Filter_CanBeComplexObject()
    {
        // Arrange
        var complexFilter = new
        {
            and = new object[]
            {
                new { hasData = new[] { new { type = "view", space = "space", externalId = "view", version = "1" } } },
                new { equals = new { property = new[] { "space", "view", "1", "status" }, value = "active" } }
            }
        };

        var query = new SyncNodesQuery { Filter = complexFilter };

        // Act
        var json = JsonSerializer.Serialize(query, _jsonOptions);

        // Assert
        Assert.Contains("\"and\"", json);
        Assert.Contains("\"hasData\"", json);
        Assert.Contains("\"equals\"", json);
        Assert.Contains("\"status\"", json);
    }

    [Fact]
    public void SyncResultSetExpression_WithLimit_Serializes()
    {
        // Arrange
        var expr = new SyncResultSetExpression
        {
            Nodes = new SyncNodesQuery
            {
                Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
            },
            Limit = 500
        };

        // Act
        var json = JsonSerializer.Serialize(expr, _jsonOptions);

        // Assert
        Assert.Contains("\"limit\":500", json);
    }

    [Fact]
    public void SelectExpression_WithMultipleSources_Serializes()
    {
        // Arrange
        var select = new SelectExpression
        {
            Sources = new[]
            {
                new SourceSelection
                {
                    Source = new { type = "view", space = "space1", externalId = "view1", version = "1" },
                    Properties = new[] { "name" }
                },
                new SourceSelection
                {
                    Source = new { type = "view", space = "space2", externalId = "view2", version = "1" },
                    Properties = new[] { "description" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(select, _jsonOptions);
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("sources", out var sources));
        Assert.Equal(2, sources.GetArrayLength());
    }

    [Fact]
    public void SourceSelection_WithEmptyProperties_SelectsAll()
    {
        // Arrange
        var selection = new SourceSelection
        {
            Source = new { type = "view", space = "space", externalId = "view", version = "1" },
            Properties = Array.Empty<string>()
        };

        // Act
        var json = JsonSerializer.Serialize(selection, _jsonOptions);
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("properties", out var props));
        Assert.Equal(0, props.GetArrayLength());
    }

    // Sync Mode tests
    [Fact]
    public void SyncInstancesRequest_WithTwoPhaseMode_Serializes()
    {
        // Arrange
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            Mode = "twoPhase"
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"mode\":\"twoPhase\"", json);
    }

    [Fact]
    public void SyncInstancesRequest_WithNoBackfillMode_Serializes()
    {
        // Arrange
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            Mode = "noBackfill"
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"mode\":\"noBackfill\"", json);
    }

    [Fact]
    public void SyncInstancesRequest_WithBackfillSort_Serializes()
    {
        // Arrange
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            Mode = "twoPhase",
            BackfillSort = new[]
            {
                new SyncBackfillSort
                {
                    Property = new[] { "space", "container/1", "timestamp" },
                    Direction = "ascending",
                    NullsFirst = false
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"backfillSort\"", json);
        Assert.Contains("\"direction\":\"ascending\"", json);
        Assert.Contains("\"nullsFirst\":false", json);
    }

    [Fact]
    public void SyncInstancesRequest_WithAllowExpiredCursors_Serializes()
    {
        // Arrange
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            AllowExpiredCursors = true
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"allowExpiredCursorsAndAcceptMissedDeletes\":true", json);
    }

    [Fact]
    public void SyncBackfillSort_Serializes_Correctly()
    {
        // Arrange
        var sort = new SyncBackfillSort
        {
            Property = new[] { "space", "container/1", "createdTime" },
            Direction = "descending",
            NullsFirst = true
        };

        // Act
        var json = JsonSerializer.Serialize(sort, _jsonOptions);
        var doc = JsonDocument.Parse(json);

        // Assert
        Assert.True(doc.RootElement.TryGetProperty("property", out var prop));
        Assert.Equal(3, prop.GetArrayLength());
        Assert.Equal("descending", doc.RootElement.GetProperty("direction").GetString());
        Assert.True(doc.RootElement.GetProperty("nullsFirst").GetBoolean());
    }

    [Fact]
    public void SyncMode_Enum_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)SyncMode.OnePhase);
        Assert.Equal(1, (int)SyncMode.TwoPhase);
        Assert.Equal(2, (int)SyncMode.NoBackfill);
    }

    // Request overload validation tests
    [Fact]
    public async Task SyncAsync_WithNullRequest_Throws()
    {
        // Arrange
        var resource = new CogniteSdk.Resources.SyncResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SyncAsync((SyncInstancesRequest)null!));
    }

    [Fact]
    public async Task SyncAsync_WithNullBackfillSortProperty_Throws()
    {
        // Arrange
        var resource = new CogniteSdk.Resources.SyncResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            BackfillSort = new[]
            {
                new SyncBackfillSort { Property = null! } // Invalid - null property
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.SyncAsync(request));
    }

    [Fact]
    public async Task SyncAsync_WithEmptyBackfillSortProperty_Throws()
    {
        // Arrange
        var resource = new CogniteSdk.Resources.SyncResource(new HttpClient(), "project", "url", ct => Task.FromResult("token"));
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            BackfillSort = new[]
            {
                new SyncBackfillSort { Property = Array.Empty<string>() } // Invalid - empty property
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => resource.SyncAsync(request));
    }

    [Fact]
    public async Task SyncAsync_WithValidBackfillSort_DoesNotThrow()
    {
        // Arrange - just checking validation passes (actual HTTP call will fail)
        var resource = new CogniteSdk.Resources.SyncResource(new HttpClient(), "project", "https://api.cognitedata.com", ct => Task.FromResult("token"));
        var request = new SyncInstancesRequest
        {
            With = new Dictionary<string, SyncResultSetExpression>
            {
                ["items"] = new SyncResultSetExpression
                {
                    Nodes = new SyncNodesQuery
                    {
                        Filter = new { hasData = new[] { new { type = "view", space = "s", externalId = "v", version = "1" } } }
                    }
                }
            },
            BackfillSort = new[]
            {
                new SyncBackfillSort 
                { 
                    Property = new[] { "space", "container/1", "timestamp" },
                    Direction = "ascending"
                }
            }
        };

        // Act - validation should pass, but HTTP call will fail (that's OK)
        try
        {
            await resource.SyncAsync(request);
        }
        catch (HttpRequestException)
        {
            // Expected - we just want to verify validation passed
        }
    }
}
