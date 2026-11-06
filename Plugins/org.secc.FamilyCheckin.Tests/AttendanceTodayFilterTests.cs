// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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

using org.secc.FamilyCheckin.Cache;

using Rock.Model;

using Xunit;

namespace org.secc.FamilyCheckin.Tests
{
    /// <summary>
    /// ROCK-9442: which attendance the check-in cache holds. The query filter (key list, bulk load,
    /// Verify, IsFull recount) and the in-memory check (per-item load) must agree.
    /// </summary>
    public class AttendanceTodayFilterTests
    {
        private static readonly DateTime Today = new DateTime( 2026, 11, 5 );

        public static IEnumerable<object[]> Cases()
        {
            // created, start, expected
            yield return new object[] { Today.AddHours( 17 ), Today.AddHours( 17 ), true };              // kiosk check-in
            yield return new object[] { Today.AddHours( 8 ), Today, true };                              // dated at midnight today
            yield return new object[] { Today.AddHours( 8 ), Today.AddDays( 1 ).AddTicks( -1 ), true };  // last tick of today
            yield return new object[] { Today.AddHours( 8 ), Today.AddDays( -4 ).AddHours( 18 ), false }; // last week's group, entered today
            yield return new object[] { Today.AddHours( 8 ), Today.AddTicks( -1 ), false };              // dated yesterday
            yield return new object[] { Today.AddHours( 8 ), Today.AddDays( 1 ), false };                // dated tomorrow
            yield return new object[] { Today.AddHours( 8 ), Today.AddDays( 7 ), false };                // next week's, entered today
            yield return new object[] { Today.AddTicks( -1 ), Today.AddHours( 9 ), false };              // created yesterday for today
            yield return new object[] { null, Today.AddHours( 9 ), false };                              // no created date
        }

        [Theory]
        [MemberData( nameof( Cases ) )]
        public void IsToday_MatchesExpected( DateTime? created, DateTime start, bool expected )
        {
            var attendance = new Attendance { CreatedDateTime = created, StartDateTime = start };

            Assert.Equal( expected, AttendanceCache.IsToday( attendance, Today ) );
        }

        [Theory]
        [MemberData( nameof( Cases ) )]
        public void TodayFilter_AgreesWithIsToday( DateTime? created, DateTime start, bool expected )
        {
            var attendance = new Attendance { CreatedDateTime = created, StartDateTime = start };
            var filter = AttendanceCache.TodayFilter( Today ).Compile();

            Assert.Equal( expected, filter( attendance ) );
            Assert.Equal( AttendanceCache.IsToday( attendance, Today ), filter( attendance ) );
        }
    }
}
