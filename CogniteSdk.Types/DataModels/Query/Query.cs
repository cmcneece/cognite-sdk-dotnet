// Copyright 2023 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CogniteSdk.DataModels
{
    /// <summary>
    /// Query accross nodes and edges of a project using a graph approach.
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Queries with filters for returned nodes and edges.
        /// </summary>
        public Dictionary<string, IQueryTableExpression> With { get; set; }
        /// <summary>
        /// Groups of values to return.
        /// </summary>
        public Dictionary<string, SelectExpression> Select { get; set; }
        /// <summary>
        /// Parameter values for the query.
        /// </summary>
        public Dictionary<string, IDMSValue> Parameters { get; set; }
        /// <summary>
        /// Cursors returned from the previous query request. These cursors match the result set expression specified in the
        /// "with" clause for the query.
        /// </summary>
        public Dictionary<string, string> Cursors { get; set; }
    }

    /// <summary>
    /// Result from a query operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryResult<T>
    {
        /// <summary>
        /// Returned items
        /// </summary>
        public Dictionary<string, IEnumerable<BaseInstance<T>>> Items { get; set; }
        /// <summary>
        /// Returned cursors.
        /// </summary>
        public Dictionary<string, string> NextCursor { get; set; }
    }

    /// <summary>
    /// Sync mode controlling the backfill phase behavior.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SyncMode
    {
        /// <summary>
        /// Don't distinguish between the backfill phase and subsequent changes.
        /// Use this mode if syncing without a filter or with a single space filter.
        /// This is the default mode.
        /// </summary>
        onePhase,

        /// <summary>
        /// Split the sync into two stages. This allows the system to take advantage
        /// of an index (default or custom cursorable) in the backfill stage.
        /// Use this when syncing with a hasData filter or other indexed filters.
        /// </summary>
        twoPhase,

        /// <summary>
        /// Skip the backfill stage and yield only instances that have changed
        /// since the sync started. Useful when you only care about new changes.
        /// </summary>
        noBackfill
    }

    /// <summary>
    /// Sort specification for backfill phase in two-phase sync.
    /// Must match a cursorable index for optimal performance.
    /// </summary>
    public class SyncBackfillSort
    {
        /// <summary>
        /// Property path to sort by, e.g., ["space", "container/version", "property"].
        /// </summary>
        public IEnumerable<string> Property { get; set; }

        /// <summary>
        /// Sort direction.
        /// </summary>
        public SortDirection Direction { get; set; }

        /// <summary>
        /// Whether nulls should come first in sort order.
        /// For ascending sorts: set to false (nulls last).
        /// For descending sorts: set to true (nulls first).
        /// </summary>
        public bool NullsFirst { get; set; }
    }

    /// <summary>
    /// Subscribe to changes for nodes and edges in a project, matching a supplied filter.
    /// </summary>
    public class SyncQuery : Query
    {
        /// <summary>
        /// Sync mode controlling the backfill phase.
        /// Default is <see cref="SyncMode.onePhase"/>.
        /// </summary>
        public SyncMode? Mode { get; set; }

        /// <summary>
        /// Sort specification for backfill phase when using <see cref="SyncMode.twoPhase"/>.
        /// Must match a cursorable index for optimal performance.
        /// </summary>
        public IEnumerable<SyncBackfillSort> BackfillSort { get; set; }

        /// <summary>
        /// When true, allows use of expired cursors (older than 3 days).
        /// Warning: Using expired cursors may miss soft-deleted instances.
        /// </summary>
        public bool? AllowExpiredCursorsAndAcceptMissedDeletes { get; set; }
    }

    /// <summary>
    /// Result from a sync operation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SyncResult<T> : QueryResult<T>
    {
    }
}
