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
using System.Collections.Generic;

using org.secc.LinkList.ViewModels;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-7164: pure helpers for the analytics time series (BCL only, so
    /// they are unit-testable in isolation).
    /// </summary>
    public static class AnalyticsSeries
    {
        /// <summary>
        /// Expands sparse per-day counts into a dense, ascending series with a
        /// zero-count point for every day from <paramref name="startDate"/>
        /// through <paramref name="endDate"/> (dates compared by .Date; both
        /// endpoints inclusive). Charting libraries then render gaps as real
        /// zero days instead of connecting across them. Null counts are
        /// treated as empty; an inverted range yields an empty list.
        /// </summary>
        public static List<AnalyticsPointBag> FillDailySeries( IDictionary<DateTime, int> counts, DateTime startDate, DateTime endDate )
        {
            var result = new List<AnalyticsPointBag>();
            var day = startDate.Date;
            var last = endDate.Date;

            while ( day <= last )
            {
                var count = 0;
                if ( counts != null && counts.TryGetValue( day, out var c ) )
                {
                    count = c;
                }
                result.Add( new AnalyticsPointBag { Date = day, Count = count } );
                day = day.AddDays( 1 );
            }

            return result;
        }

        /// <summary>Clamps a requested range to the supported set (30, 90, 365); anything else falls back to 30.</summary>
        public static int ClampDays( int days )
        {
            return days == 90 || days == 365 ? days : 30;
        }
    }
}
