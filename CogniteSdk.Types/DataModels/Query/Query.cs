// Copyright 2023 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <remarks>
    /// <para>
    /// <b>Note:</b> The <c>mode</c> parameter is not yet supported by all CDF API versions.
    /// If your cluster does not support this parameter, the API may return an error.
    /// Check CDF API documentation for your cluster's supported features.
    /// </para>
    /// </remarks>
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
        private IEnumerable<string> _property;

        /// <summary>
        /// Property path to sort by, e.g., ["space", "container/version", "property"].
        /// Cannot be null or empty.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
        /// <exception cref="ArgumentException">Thrown when value is empty or contains null/empty segments.</exception>
        public IEnumerable<string> Property
        {
            get => _property;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Property path cannot be null");

                var list = value.ToList();
                if (list.Count == 0)
                    throw new ArgumentException("Property path cannot be empty", nameof(value));

                foreach (var segment in list)
                {
                    if (string.IsNullOrEmpty(segment))
                        throw new ArgumentException("Property path segments cannot be null or empty", nameof(value));
                }

                _property = list;
            }
        }

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
        /// <remarks>
        /// <b>Note:</b> Not yet supported by all CDF clusters. See <see cref="SyncMode"/> remarks.
        /// </remarks>
        public SyncMode? Mode { get; set; }

        /// <summary>
        /// Sort specification for backfill phase when using <see cref="SyncMode.twoPhase"/>.
        /// Must match a cursorable index for optimal performance.
        /// </summary>
        /// <remarks>
        /// <b>Note:</b> Only applicable when <see cref="Mode"/> is set. See <see cref="SyncMode"/> remarks.
        /// </remarks>
        public IEnumerable<SyncBackfillSort> BackfillSort { get; set; }

        /// <summary>
        /// When true, allows use of expired cursors (older than 3 days).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>⚠️ SECURITY AND COMPLIANCE WARNING</b>
        /// </para>
        /// <para>
        /// Setting this property to <c>true</c> may result in missed soft-deleted instances.
        /// This has significant implications:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>
        ///     <b>Data Integrity</b>: Deleted records may still appear in sync results,
        ///     leading to stale or inconsistent data in downstream systems.
        ///   </description></item>
        ///   <item><description>
        ///     <b>Compliance Risk</b>: In regulated environments (21 CFR Part 11, EU Annex 11,
        ///     SOX, GDPR), missing delete events may violate audit trail requirements.
        ///   </description></item>
        ///   <item><description>
        ///     <b>Audit Trail Gaps</b>: Delete operations will not be reflected in sync results,
        ///     creating incomplete audit histories.
        ///   </description></item>
        /// </list>
        /// <para>
        /// <b>When to use:</b> Only enable this when:
        /// </para>
        /// <list type="number">
        ///   <item><description>Data freshness requirements are relaxed for your use case</description></item>
        ///   <item><description>You have alternative mechanisms for ensuring data consistency</description></item>
        ///   <item><description>Your environment is not subject to regulatory compliance requirements</description></item>
        /// </list>
        /// </remarks>
        /// <value>
        /// <c>true</c> to allow expired cursors and accept potential missed deletes;
        /// <c>false</c> or <c>null</c> to enforce cursor expiration (default, recommended).
        /// </value>
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
