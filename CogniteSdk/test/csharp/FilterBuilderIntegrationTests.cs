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
    /// Integration tests for FilterBuilder fluent API.
    /// These tests require CDF credentials and create/delete test data.
    /// </summary>
    public class FilterBuilderIntegrationTests : IClassFixture<DataModelsFixture>
    {
        private readonly DataModelsFixture _fixture;

        public FilterBuilderIntegrationTests(DataModelsFixture fixture)
        {
            _fixture = fixture;
        }

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
    }
}
