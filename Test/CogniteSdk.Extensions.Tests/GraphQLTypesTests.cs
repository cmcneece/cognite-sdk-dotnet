// Copyright 2025 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using CogniteSdk.Types.DataModels.GraphQL;
using Xunit;

namespace CogniteSdk.Extensions.Tests;

public class GraphQLTypesTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [Fact]
    public void GraphQLRequest_SerializesCorrectly()
    {
        // Arrange
        var request = new GraphQLRequest
        {
            Query = "{ listEquipment { items { name } } }",
            Variables = new Dictionary<string, object?>
            {
                ["limit"] = 10,
                ["filter"] = null
            },
            OperationName = "ListEquipment"
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("{ listEquipment { items { name } } }", parsed.RootElement.GetProperty("query").GetString());
        Assert.Equal("ListEquipment", parsed.RootElement.GetProperty("operationName").GetString());
        Assert.True(parsed.RootElement.TryGetProperty("variables", out var vars));
        Assert.Equal(10, vars.GetProperty("limit").GetInt32());
    }

    [Fact]
    public void GraphQLRequest_OmitsNullFields()
    {
        // Arrange
        var request = new GraphQLRequest
        {
            Query = "{ test }"
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.False(parsed.RootElement.TryGetProperty("variables", out _));
        Assert.False(parsed.RootElement.TryGetProperty("operationName", out _));
    }

    [Fact]
    public void GraphQLResponse_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""data"": {
                ""listEquipment"": {
                    ""items"": [{""name"": ""Pump-001""}]
                }
            }
        }";

        // Act
        var response = JsonSerializer.Deserialize<GraphQLResponse<JsonElement>>(json, _jsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.False(response!.HasErrors);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void GraphQLResponse_DeserializesWithErrors()
    {
        // Arrange
        var json = @"{
            ""data"": null,
            ""errors"": [
                {
                    ""message"": ""Field 'invalid' not found"",
                    ""locations"": [{""line"": 1, ""column"": 5}],
                    ""path"": [""listEquipment"", ""invalid""]
                }
            ]
        }";

        // Act
        var response = JsonSerializer.Deserialize<GraphQLResponse<JsonElement>>(json, _jsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.True(response!.HasErrors);
        Assert.Single(response.Errors!);
        Assert.Equal("Field 'invalid' not found", response.Errors![0].Message);
        Assert.NotNull(response.Errors[0].Locations);
        Assert.Single(response.Errors[0].Locations!);
        Assert.Equal(1, response.Errors[0].Locations![0].Line);
        Assert.Equal(5, response.Errors[0].Locations![0].Column);
    }

    [Fact]
    public void GraphQLRawResponse_HasErrors_ReturnsFalseWhenNull()
    {
        // Arrange
        var response = new GraphQLRawResponse
        {
            Data = JsonDocument.Parse("{}").RootElement,
            Errors = null
        };

        // Act & Assert
        Assert.False(response.HasErrors);
    }

    [Fact]
    public void GraphQLRawResponse_HasErrors_ReturnsFalseWhenEmpty()
    {
        // Arrange
        var response = new GraphQLRawResponse
        {
            Data = JsonDocument.Parse("{}").RootElement,
            Errors = Array.Empty<GraphQLError>()
        };

        // Act & Assert
        Assert.False(response.HasErrors);
    }

    [Fact]
    public void GraphQLRawResponse_HasErrors_ReturnsTrueWhenHasErrors()
    {
        // Arrange
        var response = new GraphQLRawResponse
        {
            Errors = new[] { new GraphQLError { Message = "Error" } }
        };

        // Act & Assert
        Assert.True(response.HasErrors);
    }

    [Fact]
    public void GraphQLError_Path_CanContainMixedTypes()
    {
        // Arrange
        var json = @"{
            ""message"": ""Error"",
            ""path"": [""listEquipment"", ""items"", 0, ""name""]
        }";

        // Act
        var error = JsonSerializer.Deserialize<GraphQLError>(json, _jsonOptions);

        // Assert
        Assert.NotNull(error?.Path);
        Assert.Equal(4, error!.Path!.Count);
    }
}
