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
using System.Linq;
using System.Threading;
using Rock.Security.ExternalAuthentication;
using Xunit;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// SMSRecords.TryRegisterCodeEntry's window, its clock and its bound on tracked addresses. Each test starts and
    /// ends with no windows tracked.
    /// </summary>
    public class SMSRecordsCodeEntryTests : IDisposable
    {
        private static readonly DateTime BoundStart = new DateTime( 2000, 1, 1 );

        public SMSRecordsCodeEntryTests()
        {
            SMSRecords.ClearCodeEntryWindows();
        }

        public void Dispose()
        {
            SMSRecords.ClearCodeEntryWindows();
        }

        private static string NewKey()
        {
            return "entry-" + Guid.NewGuid().ToString( "N" ) + "-";
        }

        /// <summary>
        /// Gives the addresses key + 0 to key + (count - 1) one entry each, a millisecond apart from BoundStart, except
        /// that the addresses in <paramref name="full"/> use their whole budget.
        /// </summary>
        private static void Track( string key, int count, params int[] full )
        {
            bool logRefusal;
            for ( var i = 0; i < count; i++ )
            {
                var entries = full.Contains( i ) ? 30 : 1;
                for ( var entry = 1; entry <= entries; entry++ )
                {
                    Assert.True( SMSRecords.TryRegisterCodeEntry( key + i, BoundStart.AddMilliseconds( i ), out logRefusal ) );
                }
            }
        }

        [Fact]
        public void Window_RunsFifteenMinutesFromItsFirstEntry_AndARefusalDoesNotExtendIt()
        {
            var key = NewKey();
            var start = new DateTime( 2001, 1, 1, 12, 0, 0 );
            bool logRefusal;

            for ( var entry = 1; entry <= 30; entry++ )
            {
                Assert.True( SMSRecords.TryRegisterCodeEntry( key, start, out logRefusal ) );
                Assert.False( logRefusal );
            }

            Assert.False( SMSRecords.TryRegisterCodeEntry( key, start.AddMinutes( 14 ).AddSeconds( 59 ), out logRefusal ) );
            Assert.True( logRefusal );
            Assert.False( SMSRecords.TryRegisterCodeEntry( key, start.AddMinutes( 15 ), out logRefusal ) );
            Assert.False( logRefusal );

            // The next window has its own thirty entries and its own logged refusal.
            var next = start.AddMinutes( 15 ).AddTicks( 1 );
            for ( var entry = 1; entry <= 30; entry++ )
            {
                Assert.True( SMSRecords.TryRegisterCodeEntry( key, next, out logRefusal ) );
                Assert.False( logRefusal );
            }

            Assert.False( SMSRecords.TryRegisterCodeEntry( key, next, out logRefusal ) );
            Assert.True( logRefusal );
        }

        [Fact]
        public void Window_StartsAtItsFirstCountedEntry_NotItsLatest()
        {
            var key = NewKey();
            var start = new DateTime( 2001, 1, 1, 12, 0, 0 );
            bool logRefusal;

            Assert.True( SMSRecords.TryRegisterCodeEntry( key, start, out logRefusal ) );
            for ( var entry = 2; entry <= 30; entry++ )
            {
                Assert.True( SMSRecords.TryRegisterCodeEntry( key, start.AddMinutes( 10 ), out logRefusal ) );
            }

            Assert.False( SMSRecords.TryRegisterCodeEntry( key, start.AddMinutes( 15 ), out logRefusal ) );
            Assert.True( SMSRecords.TryRegisterCodeEntry( key, start.AddMinutes( 15 ).AddTicks( 1 ), out logRefusal ) );
        }

        [Fact]
        public void EntriesWithoutAGivenTime_AreTimedInUtc()
        {
            var now = DateTime.UtcNow;
            var ended = NewKey();
            var open = NewKey();
            bool logRefusal;

            for ( var entry = 1; entry <= 30; entry++ )
            {
                Assert.True( SMSRecords.TryRegisterCodeEntry( ended, now.AddMinutes( -16 ), out logRefusal ) );
                Assert.True( SMSRecords.TryRegisterCodeEntry( open, now.AddMinutes( -14 ), out logRefusal ) );
            }

            // Windows that started 16 and 14 minutes ago in UTC. On a host whose time zone isn't UTC, timing
            // these entries in local time would keep the first window open or end the second.
            Assert.True( SMSRecords.TryRegisterCodeEntry( ended, out logRefusal ) );
            Assert.False( SMSRecords.TryRegisterCodeEntry( open, out logRefusal ) );

            // The renewed window's start is stored as a UTC time, whatever the host's time zone.
            Assert.Equal( DateTimeKind.Utc, SMSRecords.GetCodeEntryWindowStart( ended ).Value.Kind );
        }

        [Fact]
        public void ConcurrentEntries_AreCountedInOneStep()
        {
            const int threadCount = 48;
            var now = new DateTime( 2002, 1, 1 );

            for ( var round = 1; round <= 20; round++ )
            {
                var key = NewKey();
                var accepted = 0;
                var start = new Barrier( threadCount );

                var threads = Enumerable.Range( 0, threadCount ).Select( t => new Thread( () =>
                {
                    start.SignalAndWait();
                    bool logRefusal;
                    if ( SMSRecords.TryRegisterCodeEntry( key, now, out logRefusal ) )
                    {
                        Interlocked.Increment( ref accepted );
                    }
                } ) ).ToList();

                threads.ForEach( t => t.Start() );
                threads.ForEach( t => t.Join() );

                Assert.Equal( SMSRecords.MaxCodeEntriesPerClient, accepted );
            }
        }

        [Fact]
        public void TwentyThousandAddresses_AreAllTracked()
        {
            var key = NewKey();
            bool logRefusal;

            Track( key, 20000, 0 );

            // Nothing was dropped, so the oldest address's full window still refuses.
            Assert.False( SMSRecords.TryRegisterCodeEntry( key + 0, BoundStart.AddMinutes( 1 ), out logRefusal ) );
        }

        [Fact]
        public void TrackedAddresses_AreBounded_ByDroppingTheOldestTenth()
        {
            var key = NewKey();
            bool logRefusal;

            // Three of the 20,000 addresses use their whole budget: the oldest, the last of the oldest tenth and the
            // first after it.
            Track( key, 20000, 0, 1999, 2000 );

            // The 20,001st address drops the oldest 2,000 windows, so their counts start again; the next is still counted.
            Assert.True( SMSRecords.TryRegisterCodeEntry( key + 20000, BoundStart.AddMinutes( 1 ), out logRefusal ) );
            Assert.True( SMSRecords.TryRegisterCodeEntry( key + 0, BoundStart.AddMinutes( 1 ), out logRefusal ) );
            Assert.True( SMSRecords.TryRegisterCodeEntry( key + 1999, BoundStart.AddMinutes( 1 ), out logRefusal ) );
            Assert.False( SMSRecords.TryRegisterCodeEntry( key + 2000, BoundStart.AddMinutes( 1 ), out logRefusal ) );
        }

        [Fact]
        public void RenewingAnExpiredWindow_WithTwentyThousandAddresses_DropsNothing()
        {
            var key = NewKey();
            var stale = NewKey();
            bool logRefusal;

            // 20,000 addresses: one whose window started twenty minutes before the others, then 19,999 more, the
            // oldest of which uses its whole budget.
            Assert.True( SMSRecords.TryRegisterCodeEntry( stale, BoundStart.AddMinutes( -20 ), out logRefusal ) );
            Track( key, 19999, 0 );

            // Renewing the expired window doesn't add an address, so the full window still refuses.
            Assert.True( SMSRecords.TryRegisterCodeEntry( stale, BoundStart.AddMinutes( 1 ), out logRefusal ) );
            Assert.Equal( BoundStart.AddMinutes( 1 ), SMSRecords.GetCodeEntryWindowStart( stale ) );
            Assert.False( SMSRecords.TryRegisterCodeEntry( key + 0, BoundStart.AddMinutes( 1 ), out logRefusal ) );
        }
    }
}
