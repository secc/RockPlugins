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
using System.Linq;

using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class AnalyticsSeriesTests
    {
        private static readonly DateTime Start = new DateTime( 2026, 7, 1 );

        [Fact]
        public void Fills_Gaps_With_Zero_Days()
        {
            var counts = new Dictionary<DateTime, int>
            {
                [Start] = 5,
                [Start.AddDays( 3 )] = 2
            };

            var series = AnalyticsSeries.FillDailySeries( counts, Start, Start.AddDays( 4 ) );

            Assert.Equal( 5, series.Count );
            Assert.Equal( new[] { 5, 0, 0, 2, 0 }, series.Select( p => p.Count ).ToArray() );
            Assert.Equal( Start, series[0].Date );
            Assert.Equal( Start.AddDays( 4 ), series[4].Date );
        }

        [Fact]
        public void Both_Endpoints_Are_Inclusive()
        {
            var series = AnalyticsSeries.FillDailySeries( null, Start, Start.AddDays( 1 ) );
            Assert.Equal( 2, series.Count );
        }

        [Fact]
        public void Single_Day_Range_Yields_One_Point()
        {
            var series = AnalyticsSeries.FillDailySeries( new Dictionary<DateTime, int> { [Start] = 7 }, Start, Start );
            Assert.Single( series );
            Assert.Equal( 7, series[0].Count );
        }

        [Fact]
        public void Null_Counts_Yield_All_Zeroes()
        {
            var series = AnalyticsSeries.FillDailySeries( null, Start, Start.AddDays( 2 ) );
            Assert.All( series, p => Assert.Equal( 0, p.Count ) );
        }

        [Fact]
        public void Inverted_Range_Yields_Empty_List()
        {
            var series = AnalyticsSeries.FillDailySeries( null, Start, Start.AddDays( -1 ) );
            Assert.Empty( series );
        }

        [Fact]
        public void Time_Components_Are_Truncated()
        {
            // A count keyed on midnight matches even when the range endpoints
            // carry a time-of-day.
            var counts = new Dictionary<DateTime, int> { [Start] = 3 };
            var series = AnalyticsSeries.FillDailySeries( counts, Start.AddHours( 9 ), Start.AddHours( 17 ) );
            Assert.Single( series );
            Assert.Equal( 3, series[0].Count );
        }

        [Theory]
        [InlineData( 30, 30 )]
        [InlineData( 90, 90 )]
        [InlineData( 365, 365 )]
        [InlineData( 0, 30 )]
        [InlineData( -5, 30 )]
        [InlineData( 60, 30 )]
        [InlineData( 9999, 30 )]
        public void ClampDays_Falls_Back_To_30( int input, int expected )
        {
            Assert.Equal( expected, AnalyticsSeries.ClampDays( input ) );
        }
    }
}
