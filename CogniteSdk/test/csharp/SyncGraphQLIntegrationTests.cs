// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CogniteSdk;
using CogniteSdk.DataModels;
using Xunit;

namespace Test.CSharp.Integration
{
    /// <summary>
    /// Integration tests for SyncQuery extensions and GraphQL resource.
    /// These tests require CDF credentials and create/delete test data.
    /// </summary>
    /// <remarks>
    /// Note: SyncMode tests are excluded because the `mode` field is not yet supported
    /// by all CDF API versions. The SyncMode enum and properties were added for forward
    /// compatibility when the API feature becomes available.
    /// </remarks>
    public class SyncGraphQLIntegrationTests : IClassFixture<DataModelsFixture>
    {
        private readonly DataModelsFixture _fixture;

        public SyncGraphQLIntegrationTests(DataModelsFixture fixture)
        {
            _fixture = fixture;
        }

        #region SyncQuery Integration Tests (Basic - without SyncMode)

        [Fact]
        public async Task SyncQuery_BasicSync_ReturnsResults()
        {
            // Arrange
            var nodeId = $"sync{Guid.NewGuid():N}".Substring(0, 30);
            var req = new InstanceWriteRequest
            {
                Items = new[]
                {
                    new NodeWrite
                    {
                        ExternalId = nodeId,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("SyncBasicTest") },
                                    { "intProp", new RawPropertyValue<long>(555) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    }
                }
            };

            await _fixture.Write.DataModels.UpsertInstances(req);
            var ids = new[] { new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId) };

            try
            {
                // Act: Use basic SyncQuery (without Mode - testing base functionality)
                var syncQuery = new SyncQuery
                {
                    // Note: Mode property is not set - testing backwards compatibility
                    Select = new Dictionary<string, SelectExpression>
                    {
                        { "result", new SelectExpression
                        {
                            Sources = new[] { new SelectSource { Source = _fixture.TestView, Properties = new[] { "prop" } } }
                        }}
                    },
                    With = new Dictionary<string, IQueryTableExpression>
                    {
                        { "result", new QueryNodeTableExpression
                        {
                            Nodes = new QueryNodes
                            {
                                Filter = new EqualsFilter
                                {
                                    Property = new[] { _fixture.TestSpace, _fixture.TestView.ExternalId + "/1", "intProp" },
                                    Value = new RawPropertyValue<long>(555)
                                }
                            }
                        }}
                    }
                };

                var result = await _fixture.Write.DataModels.SyncInstances<StandardInstanceData>(syncQuery);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Items);
                Assert.NotNull(result.NextCursor);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task SyncQuery_WithFilterBuilder_WorksCorrectly()
        {
            // Arrange
            var nodeId = $"syncfb{Guid.NewGuid():N}".Substring(0, 30);
            var req = new InstanceWriteRequest
            {
                Items = new[]
                {
                    new NodeWrite
                    {
                        ExternalId = nodeId,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("SyncFilterBuilderTest") },
                                    { "intProp", new RawPropertyValue<long>(666) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    }
                }
            };

            await _fixture.Write.DataModels.UpsertInstances(req);
            var ids = new[] { new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId) };

            try
            {
                // Act: Use SyncQuery with FilterBuilder
                var filter = FilterBuilder.Create()
                    .Equals(_fixture.TestView, "intProp", 666L)
                    .Build();

                var syncQuery = new SyncQuery
                {
                    Select = new Dictionary<string, SelectExpression>
                    {
                        { "result", new SelectExpression
                        {
                            Sources = new[] { new SelectSource { Source = _fixture.TestView, Properties = new[] { "prop" } } }
                        }}
                    },
                    With = new Dictionary<string, IQueryTableExpression>
                    {
                        { "result", new QueryNodeTableExpression
                        {
                            Nodes = new QueryNodes { Filter = filter }
                        }}
                    }
                };

                var result = await _fixture.Write.DataModels.SyncInstances<StandardInstanceData>(syncQuery);

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Items);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        #endregion

        #region GraphQL Integration Tests

        [Fact]
        public async Task GraphQL_Introspection_ReturnsSchema()
        {
            // Arrange: Create a data model for GraphQL testing
            var modelExtId = $"gqlmodel{_fixture.Prefix}";
            var model = new DataModelCreate
            {
                Space = _fixture.TestSpace,
                ExternalId = modelExtId,
                Version = "1",
                Views = new[] { (IViewCreateOrReference)_fixture.TestView }
            };

            await _fixture.Write.DataModels.UpsertDataModels(new[] { model });
            var modelId = new FDMExternalId(modelExtId, _fixture.TestSpace, "1");

            try
            {
                // Act: Use integrated GraphQL introspection
                var result = await _fixture.Write.DataModels.GraphQLIntrospect(
                    _fixture.TestSpace,
                    modelExtId,
                    "1"
                );

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result.Data);
                Assert.False(result.HasErrors, result.Errors?.FirstOrDefault()?.Message ?? "No error");
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteDataModels(new[] { modelId });
            }
        }

        [Fact]
        public async Task GraphQL_QuerySchemaType_ReturnsData()
        {
            // Arrange
            var modelExtId = $"gqlquery{_fixture.Prefix}";
            var model = new DataModelCreate
            {
                Space = _fixture.TestSpace,
                ExternalId = modelExtId,
                Version = "1",
                Views = new[] { (IViewCreateOrReference)_fixture.TestView }
            };

            await _fixture.Write.DataModels.UpsertDataModels(new[] { model });
            var modelId = new FDMExternalId(modelExtId, _fixture.TestSpace, "1");

            try
            {
                // Act: Use integrated GraphQL query
                var result = await _fixture.Write.DataModels.GraphQLQueryRaw(
                    _fixture.TestSpace,
                    modelExtId,
                    "1",
                    @"query { __schema { queryType { name } } }"
                );

                // Assert
                Assert.NotNull(result);
                Assert.False(result.HasErrors, result.Errors?.FirstOrDefault()?.Message ?? "No error");
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteDataModels(new[] { modelId });
            }
        }

        [Fact]
        public async Task GraphQL_InvalidQuery_ReturnsErrors()
        {
            // Arrange
            var modelExtId = $"gqlerror{_fixture.Prefix}";
            var model = new DataModelCreate
            {
                Space = _fixture.TestSpace,
                ExternalId = modelExtId,
                Version = "1",
                Views = new[] { (IViewCreateOrReference)_fixture.TestView }
            };

            await _fixture.Write.DataModels.UpsertDataModels(new[] { model });
            var modelId = new FDMExternalId(modelExtId, _fixture.TestSpace, "1");

            try
            {
                // Act: Use integrated GraphQL query with invalid query
                var result = await _fixture.Write.DataModels.GraphQLQueryRaw(
                    _fixture.TestSpace,
                    modelExtId,
                    "1",
                    "{ nonExistentField }"
                );

                // Assert: Should have errors
                Assert.True(result.HasErrors);
                Assert.NotEmpty(result.Errors);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteDataModels(new[] { modelId });
            }
        }

        #endregion
    }
}
