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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Rock.Security.ExternalAuthentication;
using Xunit;
using static Rock.Security.ExternalAuthentication.SMSRecords;

namespace org.secc.Authentication.Tests
{
    public class SMSRecordsIssuanceTests
    {
        private const int WindowMinutes = 15;

        private static string NewKey( string kind )
        {
            return kind + "-" + Guid.NewGuid().ToString( "N" );
        }

        [Fact]
        public void Allowed_ReservesIpAndNumberUntilReleased()
        {
            var person = NewKey( "SMS" );
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );

            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, ip, 3, WindowMinutes ) );
            Assert.Equal( IssuanceResult.Reserved, TryBeginIssuance( NewKey( "SMS" ), phone, NewKey( "ip" ), 3, WindowMinutes ) );
            Assert.Equal( IssuanceResult.Reserved, TryBeginIssuance( NewKey( "SMS" ), NewKey( "phone" ), ip, 3, WindowMinutes ) );

            ReleaseItems( ip, phone );

            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, ip, 3, WindowMinutes ) );
            ReleaseItems( ip, phone );
        }

        [Fact]
        public void Budget_IsCountedForBothPersonAndNumber()
        {
            var person = NewKey( "SMS" );
            var phone = NewKey( "phone" );

            for ( var i = 0; i < 3; i++ )
            {
                var ip = NewKey( "ip" );
                Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, ip, 3, WindowMinutes ) );
                ReleaseItems( ip, phone );
            }

            Assert.Equal( IssuanceResult.BudgetExceeded, TryBeginIssuance( person, NewKey( "phone" ), NewKey( "ip" ), 3, WindowMinutes ) );
            Assert.Equal( IssuanceResult.BudgetExceeded, TryBeginIssuance( NewKey( "SMS" ), phone, NewKey( "ip" ), 3, WindowMinutes ) );
        }

        [Fact]
        public void ReservedRefusal_DoesNotUseBudget()
        {
            var person = NewKey( "SMS" );
            var phone = NewKey( "phone" );
            var firstIp = NewKey( "ip" );

            // First of two allowed requests; the number stays reserved until released.
            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, firstIp, 2, WindowMinutes ) );

            for ( var i = 0; i < 5; i++ )
            {
                Assert.Equal( IssuanceResult.Reserved, TryBeginIssuance( person, phone, NewKey( "ip" ), 2, WindowMinutes ) );
            }

            ReleaseItems( firstIp, phone );

            var secondIp = NewKey( "ip" );
            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, secondIp, 2, WindowMinutes ) );
            ReleaseItems( secondIp, phone );

            Assert.Equal( IssuanceResult.BudgetExceeded, TryBeginIssuance( person, phone, NewKey( "ip" ), 2, WindowMinutes ) );
        }

        [Fact]
        public void BudgetRefusal_DoesNotReserve()
        {
            var person = NewKey( "SMS" );
            var phone = NewKey( "phone" );
            var firstIp = NewKey( "ip" );

            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( person, phone, firstIp, 1, WindowMinutes ) );
            ReleaseItems( firstIp, phone );

            var ip = NewKey( "ip" );
            Assert.Equal( IssuanceResult.BudgetExceeded, TryBeginIssuance( person, phone, ip, 1, WindowMinutes ) );

            // The refused request reserved neither its IP nor the number.
            var otherPhone = NewKey( "phone" );
            Assert.Equal( IssuanceResult.Allowed, TryBeginIssuance( NewKey( "SMS" ), otherPhone, ip, 1, WindowMinutes ) );
            ReleaseItems( ip, otherPhone );
        }

        [Fact]
        public void ConcurrentRequests_NeverExceedBudget()
        {
            const int threadCount = 32;
            const int maxRequests = 3;
            var person = NewKey( "SMS" );
            var allowed = new ConcurrentBag<Tuple<string, string>>();
            var start = new Barrier( threadCount );

            var threads = Enumerable.Range( 0, threadCount ).Select( t => new Thread( () =>
            {
                var phone = NewKey( "phone" );
                var ip = NewKey( "ip" );
                start.SignalAndWait();
                if ( TryBeginIssuance( person, phone, ip, maxRequests, WindowMinutes ) == IssuanceResult.Allowed )
                {
                    allowed.Add( Tuple.Create( ip, phone ) );
                }
            } ) ).ToList();

            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            foreach ( var item in allowed )
            {
                ReleaseItems( item.Item1, item.Item2 );
            }

            Assert.Equal( maxRequests, allowed.Count );
        }

        [Fact]
        public void TryBeginIssuance_LetsOneOfTwoRequestsForAPersonThrough_WhenTheBudgetHasRoomForOne()
        {
            var person = NewKey( "SMS" );
            var firstPhone = NewKey( "phone" );
            var firstIp = NewKey( "ip" );
            var secondPhone = NewKey( "phone" );
            var secondIp = NewKey( "ip" );
            var results = new IssuanceResult[2];
            var firstParked = new ManualResetEventSlim();
            var secondDone = new ManualResetEventSlim();

            var first = new Thread( () => results[0] = TryBeginIssuance( person, firstPhone, firstIp, 1, WindowMinutes ) );
            var second = new Thread( () =>
            {
                results[1] = TryBeginIssuance( person, secondPhone, secondIp, 1, WindowMinutes );
                secondDone.Set();
            } );
            var firstThreadId = first.ManagedThreadId;

            // The first request pauses for up to 1 second after its checks, before its issuance is recorded, and the
            // second one starts during that pause. The second can't check the budget until the first has recorded.
            BeforeRecordIssuance = () =>
            {
                if ( Thread.CurrentThread.ManagedThreadId == firstThreadId )
                {
                    firstParked.Set();
                    secondDone.Wait( TimeSpan.FromSeconds( 1 ) );
                }
            };
            try
            {
                first.Start();
                Assert.True( firstParked.Wait( TimeSpan.FromSeconds( 10 ) ) );
                second.Start();
                first.Join();
                second.Join();
            }
            finally
            {
                BeforeRecordIssuance = null;
                ReleaseItems( firstIp, firstPhone );
                ReleaseItems( secondIp, secondPhone );
            }

            Assert.Equal( IssuanceResult.Allowed, results[0] );
            Assert.Equal( IssuanceResult.BudgetExceeded, results[1] );
        }

        [Fact]
        public void IssueIfAllowed_ReplacesTheCode_WhenAllowed()
        {
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );
            var replaced = 0;

            Assert.Equal( IssuanceResult.Allowed, IssueIfAllowed( NewKey( "SMS" ), phone, ip, 3, WindowMinutes, () => replaced++ ) );
            Assert.Equal( 1, replaced );
            ReleaseItems( ip, phone );
        }

        [Fact]
        public void IssueIfAllowed_LeavesTheCode_WhenReserved()
        {
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );
            var replaced = 0;

            Assert.Equal( IssuanceResult.Allowed, IssueIfAllowed( NewKey( "SMS" ), phone, ip, 3, WindowMinutes, () => { } ) );

            // Still sending to this number: refused, and the code is left alone.
            Assert.Equal( IssuanceResult.Reserved, IssueIfAllowed( NewKey( "SMS" ), phone, NewKey( "ip" ), 3, WindowMinutes, () => replaced++ ) );
            Assert.Equal( 0, replaced );
            ReleaseItems( ip, phone );
        }

        [Fact]
        public void IssueIfAllowed_LeavesTheCode_WhenOverBudget()
        {
            var person = NewKey( "SMS" );
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );
            var replaced = 0;

            Assert.Equal( IssuanceResult.Allowed, IssueIfAllowed( person, phone, ip, 1, WindowMinutes, () => { } ) );
            ReleaseItems( ip, phone );

            Assert.Equal( IssuanceResult.BudgetExceeded, IssueIfAllowed( person, phone, NewKey( "ip" ), 1, WindowMinutes, () => replaced++ ) );
            Assert.Equal( 0, replaced );
        }

        [Fact]
        public void IssueIfAllowed_ReleasesTheReservation_WhenReplacingTheCodeFails()
        {
            var phone = NewKey( "phone" );
            var ip = NewKey( "ip" );

            Assert.Throws<InvalidOperationException>( () =>
                IssueIfAllowed( NewKey( "SMS" ), phone, ip, 3, WindowMinutes, () => { throw new InvalidOperationException(); } ) );

            Assert.Equal( IssuanceResult.Allowed, IssueIfAllowed( NewKey( "SMS" ), phone, ip, 3, WindowMinutes, () => { } ) );
            ReleaseItems( ip, phone );
        }
    }
}
