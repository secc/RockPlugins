// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using Rock;
using Rock.Web.Cache;

namespace org.secc.Mapping.Utilities
{
    /// <summary>
    /// SOFT, PER-NODE, IN-MEMORY rate brake on the number of billed Azure Maps route-matrix
    /// computes the plugin will issue per minute. Every distance path funnels through
    /// <see cref="AzureDistanceMatrix.OrderDestinations" />, so the brake is checked there and
    /// covers the Mapping, GroupManager and Campus callers in one place.
    ///
    /// The counter is a plain static field guarded by a tiny in-process <c>lock</c> -- there is
    /// NO database, NO SQL, and NO I/O on the check path, so it can never 500 the request or
    /// block on the network. The lock is held only for a read-reset-increment of a couple of
    /// integers (microseconds); it is nothing like a DB lock and carries no deadlock risk.
    ///
    /// HONEST CAVEAT: because the counter lives in process memory, this is a PER-NODE brake.
    /// On the multi-node SECC web farm the effective aggregate ceiling is roughly
    /// (budget x node-count), and the count resets whenever an app pool recycles. It caps the
    /// burn RATE per node, not total spend. A hard / global spend cap belongs at the edge or on
    /// the Azure Maps account itself and is out of scope here.
    ///
    /// C# 6 only (kept deliberately simple: no out-var declarations, tuples, or pattern matching).
    /// </summary>
    public static class ComputeBrake
    {
        /// <summary>
        /// Global Attribute key holding the per-minute, per-node compute budget.
        /// </summary>
        public const string BudgetAttributeKey = "MappingComputeBudgetPerMinute";

        /// <summary>
        /// Safe fallback budget used when the Global Attribute is unset or non-positive.
        /// </summary>
        public const int DefaultBudgetPerMinute = 500;

        // In-memory state, guarded by syncRoot. currentBucket is "now" truncated to the minute;
        // currentCount is the computes billed so far in that bucket; lastAlertedBucket is the most
        // recent minute for which we emitted an ops alert (so we alert at most once per minute).
        private static readonly object syncRoot = new object();
        private static DateTime currentBucket = DateTime.MinValue;
        private static int currentCount = 0;
        private static DateTime lastAlertedBucket = DateTime.MinValue;

        /// <summary>
        /// Resolves the per-minute compute budget from the Global Attribute, falling back to
        /// <see cref="DefaultBudgetPerMinute" /> when unset or non-positive.
        /// </summary>
        public static int GetBudgetPerMinute()
        {
            var configured = GlobalAttributesCache.Get().GetValue( BudgetAttributeKey ).AsIntegerOrNull() ?? DefaultBudgetPerMinute;
            return configured > 0 ? configured : DefaultBudgetPerMinute;
        }

        /// <summary>
        /// Checks whether billing <paramref name="computeCount" /> more computes would keep the
        /// current minute at or under <paramref name="budgetPerMinute" />. Rolls the counter over
        /// to a fresh minute bucket when the clock has advanced, and only increments the running
        /// count when the request is allowed (denied floods do not inflate the counter).
        ///
        /// Never throws and never performs I/O -- the whole read-reset-increment happens under a
        /// short in-memory lock. The caller is responsible for doing any logging OUTSIDE this call.
        /// </summary>
        /// <param name="computeCount">Number of uncached destinations about to be billed to Azure.</param>
        /// <param name="budgetPerMinute">Resolved per-minute ceiling (see <see cref="GetBudgetPerMinute" />).</param>
        /// <param name="shouldAlert">
        /// On a denial, set true only the FIRST time the brake trips in the current minute bucket
        /// (so callers alert at most once per minute regardless of flood volume); false otherwise.
        /// </param>
        /// <returns>True if allowed (caller may bill Azure); false if it would exceed budget.</returns>
        public static bool TryConsume( int computeCount, int budgetPerMinute, out bool shouldAlert )
        {
            shouldAlert = false;

            // Nothing to bill -- always allowed, record nothing.
            if ( computeCount <= 0 )
            {
                return true;
            }

            // Minute bucket = "now" truncated to the minute, using Rock's clock (consistent with
            // the rest of the plugin; do NOT mix in DateTime.Now).
            var now = Rock.RockDateTime.Now;
            var minuteBucket = new DateTime( now.Year, now.Month, now.Day, now.Hour, now.Minute, 0 );

            lock ( syncRoot )
            {
                // New minute -> reset the running count.
                if ( minuteBucket != currentBucket )
                {
                    currentBucket = minuteBucket;
                    currentCount = 0;
                }

                // Deny only if this minute already has activity AND adding this batch would exceed
                // the budget. The first request of a minute (currentCount == 0) always proceeds --
                // even if it alone exceeds the budget -- so a single legitimate oversized request
                // (cold cache) warms the cache instead of stalling forever. Single-request SIZE is
                // already bounded by .Take + Azure's ~100-cell limit; this brake governs FREQUENCY.
                if ( currentCount > 0 && currentCount + computeCount > budgetPerMinute )
                {
                    // Denied. Flag the alert only once per bucket so a flood does not spam the log.
                    if ( lastAlertedBucket != minuteBucket )
                    {
                        lastAlertedBucket = minuteBucket;
                        shouldAlert = true;
                    }

                    return false;
                }

                currentCount += computeCount;
                return true;
            }
        }
    }
}
