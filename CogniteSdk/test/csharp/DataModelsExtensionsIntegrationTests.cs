// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CogniteSdk;
using CogniteSdk.DataModels;
using CogniteSdk.Resources.DataModels;
using Xunit;

namespace Test.CSharp.Integration
{
    /// <summary>
    /// Integration tests for Data Modeling extensions: FilterBuilder and GraphQL.
    /// These tests require CDF credentials and create/delete test data.
    /// </summary>
    /// <remarks>
    /// Note: SyncMode tests are excluded because the `mode` field is not yet supported
    /// by all CDF API versions. The SyncMode enum and properties were added for forward
    /// compatibility when the API feature becomes available.
    /// </remarks>
    public class DataModelsExtensionsTest : IClassFixture<DataModelsFixture>
    {
        private readonly DataModelsFixture _fixture;

        public DataModelsExtensionsTest(DataModelsFixture fixture)
        {
            _fixture = fixture;
        }

        #region FilterBuilder Integration Tests

        [Fact]
        public async Task FilterBuilder_EqualsFilter_ReturnsMatchingNodes()
        {
            // Arrange: Create test nodes
            var nodeId = $"filtertest{Guid.NewGuid():N}".Substring(0, 30);
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
                                    { "prop", new RawPropertyValue<string>("FilterBuilderTest") },
                                    { "intProp", new RawPropertyValue<long>(999) }
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
                // Act: Use FilterBuilder to create a filter and query
                var filter = FilterBuilder.Create()
                    .Equals(_fixture.TestView, "intProp", 999L)
                    .Build();

                var query = new Query
                {
                    Select = new Dictionary<string, SelectExpression>
                    {
                        { "result", new SelectExpression
                        {
                            Sources = new[] { new SelectSource { Source = _fixture.TestView, Properties = new[] { "prop", "intProp" } } }
                        }}
                    },
                    With = new Dictionary<string, IQueryTableExpression>
                    {
                        { "result", new QueryNodeTableExpression
                        {
                            Nodes = new QueryNodes
                            {
                                Filter = filter
                            }
                        }}
                    }
                };

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert
                Assert.NotNull(result.Items);
                Assert.True(result.Items.ContainsKey("result"));
                Assert.Contains(result.Items["result"], n => n.ExternalId == nodeId);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task FilterBuilder_AndFilter_ReturnsMatchingNodes()
        {
            // Arrange
            var nodeId = $"andfilter{Guid.NewGuid():N}".Substring(0, 30);
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
                                    { "prop", new RawPropertyValue<string>("AndFilterTest") },
                                    { "intProp", new RawPropertyValue<long>(888) }
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
                // Act: Use FilterBuilder with AND
                var filter = FilterBuilder.Create()
                    .Equals(_fixture.TestView, "intProp", 888L)
                    .And(FilterBuilder.Create().Equals(_fixture.TestView, "prop", "AndFilterTest"))
                    .Build();

                var query = new Query
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

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert
                Assert.Contains(result.Items["result"], n => n.ExternalId == nodeId);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task FilterBuilder_RangeFilter_ReturnsNodesInRange()
        {
            // Arrange
            var nodeId1 = $"range1{Guid.NewGuid():N}".Substring(0, 30);
            var nodeId2 = $"range2{Guid.NewGuid():N}".Substring(0, 30);
            var req = new InstanceWriteRequest
            {
                Items = new[]
                {
                    new NodeWrite
                    {
                        ExternalId = nodeId1,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("RangeTest1") },
                                    { "intProp", new RawPropertyValue<long>(50) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    },
                    new NodeWrite
                    {
                        ExternalId = nodeId2,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("RangeTest2") },
                                    { "intProp", new RawPropertyValue<long>(150) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    }
                }
            };

            await _fixture.Write.DataModels.UpsertInstances(req);
            var ids = new[]
            {
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId1),
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId2)
            };

            try
            {
                // Act: Use FilterBuilder with Range (should match node1 but not node2)
                var filter = FilterBuilder.Create()
                    .Range(_fixture.TestView, "intProp", gte: 0, lte: 100)
                    .Build();

                var query = new Query
                {
                    Select = new Dictionary<string, SelectExpression>
                    {
                        { "result", new SelectExpression
                        {
                            Sources = new[] { new SelectSource { Source = _fixture.TestView, Properties = new[] { "prop", "intProp" } } }
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

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert: Should find node1 (intProp=50) but not node2 (intProp=150)
                var resultNodes = result.Items["result"].ToList();
                Assert.Contains(resultNodes, n => n.ExternalId == nodeId1);
                Assert.DoesNotContain(resultNodes, n => n.ExternalId == nodeId2);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task FilterBuilder_PrefixFilter_ReturnsMatchingNodes()
        {
            // Arrange
            var nodeId = $"prefix{Guid.NewGuid():N}".Substring(0, 30);
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
                                    { "prop", new RawPropertyValue<string>("PrefixTestValue") },
                                    { "intProp", new RawPropertyValue<long>(123) }
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
                // Act: Use FilterBuilder with Prefix
                var filter = FilterBuilder.Create()
                    .Prefix(_fixture.TestView, "prop", "Prefix")
                    .Build();

                var query = new Query
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

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert
                Assert.Contains(result.Items["result"], n => n.ExternalId == nodeId);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task FilterBuilder_OrFilter_ReturnsMatchingNodes()
        {
            // Arrange
            var nodeId1 = $"or1{Guid.NewGuid():N}".Substring(0, 30);
            var nodeId2 = $"or2{Guid.NewGuid():N}".Substring(0, 30);
            var req = new InstanceWriteRequest
            {
                Items = new[]
                {
                    new NodeWrite
                    {
                        ExternalId = nodeId1,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("OrTest1") },
                                    { "intProp", new RawPropertyValue<long>(100) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    },
                    new NodeWrite
                    {
                        ExternalId = nodeId2,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("OrTest2") },
                                    { "intProp", new RawPropertyValue<long>(200) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    }
                }
            };

            await _fixture.Write.DataModels.UpsertInstances(req);
            var ids = new[]
            {
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId1),
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId2)
            };

            try
            {
                // Act: Use FilterBuilder with OR (should match both nodes)
                var filter = FilterBuilder.Create()
                    .Or(
                        FilterBuilder.Create().Equals(_fixture.TestView, "intProp", 100L),
                        FilterBuilder.Create().Equals(_fixture.TestView, "intProp", 200L)
                    )
                    .Build();

                var query = new Query
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

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert: Should find both nodes
                var resultNodes = result.Items["result"].ToList();
                Assert.Contains(resultNodes, n => n.ExternalId == nodeId1);
                Assert.Contains(resultNodes, n => n.ExternalId == nodeId2);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        [Fact]
        public async Task FilterBuilder_NotFilter_ExcludesMatchingNodes()
        {
            // Arrange
            var nodeId1 = $"not1{Guid.NewGuid():N}".Substring(0, 30);
            var nodeId2 = $"not2{Guid.NewGuid():N}".Substring(0, 30);
            var req = new InstanceWriteRequest
            {
                Items = new[]
                {
                    new NodeWrite
                    {
                        ExternalId = nodeId1,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("NotTest1") },
                                    { "intProp", new RawPropertyValue<long>(300) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    },
                    new NodeWrite
                    {
                        ExternalId = nodeId2,
                        Space = _fixture.TestSpace,
                        Sources = new[]
                        {
                            new InstanceData<StandardInstanceWriteData>
                            {
                                Properties = new StandardInstanceWriteData
                                {
                                    { "prop", new RawPropertyValue<string>("NotTest2") },
                                    { "intProp", new RawPropertyValue<long>(400) }
                                },
                                Source = _fixture.TestContainer
                            }
                        }
                    }
                }
            };

            await _fixture.Write.DataModels.UpsertInstances(req);
            var ids = new[]
            {
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId1),
                new InstanceIdentifierWithType(InstanceType.node, _fixture.TestSpace, nodeId2)
            };

            try
            {
                // Act: Use FilterBuilder with NOT (exclude node with intProp=300)
                var filter = FilterBuilder.Create()
                    .And(
                        FilterBuilder.Create().Range(_fixture.TestView, "intProp", gte: 300, lte: 400),
                        FilterBuilder.Create().Not(FilterBuilder.Create().Equals(_fixture.TestView, "intProp", 300L))
                    )
                    .Build();

                var query = new Query
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

                var result = await _fixture.Write.DataModels.QueryInstances<StandardInstanceData>(query);

                // Assert: Should find node2 but not node1
                var resultNodes = result.Items["result"].ToList();
                Assert.DoesNotContain(resultNodes, n => n.ExternalId == nodeId1);
                Assert.Contains(resultNodes, n => n.ExternalId == nodeId2);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        #endregion

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
                                    { "intProp", new RawPropertyValue<long>(777) }
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
                // Act: Combine SyncQuery with FilterBuilder
                var filter = FilterBuilder.Create()
                    .Equals(_fixture.TestView, "intProp", 777L)
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
                Assert.Contains(result.Items["result"], n => n.ExternalId == nodeId);
            }
            finally
            {
                await _fixture.Write.DataModels.DeleteInstances(ids);
            }
        }

        #endregion

        #region GraphQL Integration Tests

        [Fact]
        public async Task GraphQLResource_Introspection_ReturnsSchema()
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
                // Act: Use GraphQLResource for schema introspection
                using var httpClient = new HttpClient();
                var baseUrl = Environment.GetEnvironmentVariable("TEST_HOST_WRITE") ?? "https://bluefield.cognitedata.com";
                var project = Environment.GetEnvironmentVariable("TEST_PROJECT_WRITE") ?? "colin";

                var graphql = new GraphQLResource(
                    httpClient,
                    project,
                    baseUrl,
                    (ct) => Task.FromResult(Environment.GetEnvironmentVariable("TEST_TOKEN_WRITE"))
                );

                var result = await graphql.IntrospectAsync(
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
        public async Task GraphQLResource_QuerySchemaType_ReturnsData()
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
                // Act
                using var httpClient = new HttpClient();
                var baseUrl = Environment.GetEnvironmentVariable("TEST_HOST_WRITE") ?? "https://bluefield.cognitedata.com";
                var project = Environment.GetEnvironmentVariable("TEST_PROJECT_WRITE") ?? "colin";

                var graphql = new GraphQLResource(
                    httpClient,
                    project,
                    baseUrl,
                    (ct) => Task.FromResult(Environment.GetEnvironmentVariable("TEST_TOKEN_WRITE"))
                );

                // Query the schema's query type
                var result = await graphql.QueryRawAsync(
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
        public async Task GraphQLResource_InvalidQuery_ReturnsErrors()
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
                // Act
                using var httpClient = new HttpClient();
                var baseUrl = Environment.GetEnvironmentVariable("TEST_HOST_WRITE") ?? "https://bluefield.cognitedata.com";
                var project = Environment.GetEnvironmentVariable("TEST_PROJECT_WRITE") ?? "colin";

                var graphql = new GraphQLResource(
                    httpClient,
                    project,
                    baseUrl,
                    (ct) => Task.FromResult(Environment.GetEnvironmentVariable("TEST_TOKEN_WRITE"))
                );

                // Send an intentionally invalid query
                var result = await graphql.QueryRawAsync(
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
