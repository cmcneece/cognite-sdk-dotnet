// Copyright 2024 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.Json;
using CogniteSdk.DataModels;
using Xunit;

namespace Test.CSharp.Unit
{
    /// <summary>
    /// Unit tests for the SyncQuery extensions (SyncMode, SyncBackfillSort).
    /// </summary>
    public class SyncQueryTests
    {
        [Fact]
        public void SyncQuery_Mode_CanBeSetToOnePhase()
        {
            var query = new SyncQuery
            {
                Mode = SyncMode.onePhase
            };

            Assert.Equal(SyncMode.onePhase, query.Mode);
        }

        [Fact]
        public void SyncQuery_Mode_CanBeSetToTwoPhase()
        {
            var query = new SyncQuery
            {
                Mode = SyncMode.twoPhase
            };

            Assert.Equal(SyncMode.twoPhase, query.Mode);
        }

        [Fact]
        public void SyncQuery_Mode_CanBeSetToNoBackfill()
        {
            var query = new SyncQuery
            {
                Mode = SyncMode.noBackfill
            };

            Assert.Equal(SyncMode.noBackfill, query.Mode);
        }

        [Fact]
        public void SyncQuery_BackfillSort_CanBeSet()
        {
            var backfillSort = new SyncBackfillSort
            {
                Property = new[] { "mySpace", "myView/1", "timestamp" },
                Direction = SortDirection.ascending,
                NullsFirst = false
            };

            var query = new SyncQuery
            {
                Mode = SyncMode.twoPhase,
                BackfillSort = new[] { backfillSort }
            };

            Assert.NotNull(query.BackfillSort);
            Assert.Single(query.BackfillSort);
        }

        [Fact]
        public void SyncQuery_AllowExpiredCursors_CanBeSet()
        {
            var query = new SyncQuery
            {
                AllowExpiredCursorsAndAcceptMissedDeletes = true
            };

            Assert.True(query.AllowExpiredCursorsAndAcceptMissedDeletes);
        }

        [Fact]
        public void SyncQuery_InheritsFromQuery()
        {
            var query = new SyncQuery
            {
                With = new Dictionary<string, IQueryTableExpression>(),
                Select = new Dictionary<string, SelectExpression>(),
                Cursors = new Dictionary<string, string>()
            };

            Assert.NotNull(query.With);
            Assert.NotNull(query.Select);
            Assert.NotNull(query.Cursors);
        }

        [Fact]
        public void SyncBackfillSort_CanBeCreated()
        {
            var sort = new SyncBackfillSort
            {
                Property = new[] { "space", "view/1", "prop" },
                Direction = SortDirection.descending,
                NullsFirst = true
            };

            Assert.Equal(3, System.Linq.Enumerable.Count(sort.Property));
            Assert.Equal(SortDirection.descending, sort.Direction);
            Assert.True(sort.NullsFirst);
        }

        [Fact]
        public void SyncMode_SerializesToCorrectJsonValues()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Test OnePhase
            var query1 = new SyncQuery { Mode = SyncMode.onePhase };
            var json1 = JsonSerializer.Serialize(query1, options);
            Assert.Contains("onePhase", json1);

            // Test TwoPhase
            var query2 = new SyncQuery { Mode = SyncMode.twoPhase };
            var json2 = JsonSerializer.Serialize(query2, options);
            Assert.Contains("twoPhase", json2);

            // Test NoBackfill
            var query3 = new SyncQuery { Mode = SyncMode.noBackfill };
            var json3 = JsonSerializer.Serialize(query3, options);
            Assert.Contains("noBackfill", json3);
        }

        #region SyncBackfillSort Property Validation Tests

        [Fact]
        public void SyncBackfillSort_Property_NullThrowsArgumentNullException()
        {
            var sort = new SyncBackfillSort();

            Assert.Throws<ArgumentNullException>(() => sort.Property = null);
        }

        [Fact]
        public void SyncBackfillSort_Property_EmptyArrayThrowsArgumentException()
        {
            var sort = new SyncBackfillSort();

            Assert.Throws<ArgumentException>(() => sort.Property = new string[] { });
        }

        [Fact]
        public void SyncBackfillSort_Property_NullSegmentThrowsArgumentException()
        {
            var sort = new SyncBackfillSort();

            Assert.Throws<ArgumentException>(() => sort.Property = new string[] { "space", null, "prop" });
        }

        [Fact]
        public void SyncBackfillSort_Property_EmptySegmentThrowsArgumentException()
        {
            var sort = new SyncBackfillSort();

            Assert.Throws<ArgumentException>(() => sort.Property = new string[] { "space", "", "prop" });
        }

        [Fact]
        public void SyncBackfillSort_Property_ValidPathSucceeds()
        {
            var sort = new SyncBackfillSort
            {
                Property = new[] { "mySpace", "myView/1", "timestamp" }
            };

            Assert.Equal(3, System.Linq.Enumerable.Count(sort.Property));
        }

        #endregion
    }
}
