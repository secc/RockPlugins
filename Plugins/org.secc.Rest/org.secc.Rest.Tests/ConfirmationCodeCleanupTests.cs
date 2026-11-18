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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8934: which rows the expired-code cleanup picks, and how often it may run.
    /// </summary>
    public class ConfirmationCodeCleanupTests
    {
        private static readonly DateTime Now = new DateTime( 2026, 10, 16, 12, 0, 0 );

        private static readonly string Expired = Now.AddHours( -2 ).Ticks.ToString( CultureInfo.InvariantCulture );
        private static readonly string Live = Now.AddMinutes( -5 ).Ticks.ToString( CultureInfo.InvariantCulture );

        private static KeyValuePair<int, string> Row( int userLoginId, string issued )
        {
            return new KeyValuePair<int, string>( userLoginId, issued );
        }

        [Fact]
        public void SelectExpiredLoginIds_KeepsExpiredAndUnparseableRowsOnly()
        {
            var rows = new[] { Row( 1, Expired ), Row( 2, Live ), Row( 3, "abc" ), Row( 4, null ), Row( 5, Expired ) };

            Assert.Equal( new[] { 1, 3, 4, 5 }, AccountController.SelectExpiredLoginIds( rows, Now ) );
        }

        [Fact]
        public void SelectExpiredLoginIds_RemovesDuplicatesAndKeepsInputOrder()
        {
            var rows = new[] { Row( 9, Expired ), Row( 3, Expired ), Row( 9, Expired ), Row( 7, Live ), Row( 1, "" ) };

            Assert.Equal( new[] { 9, 3, 1 }, AccountController.SelectExpiredLoginIds( rows, Now ) );
        }

        [Fact]
        public void SelectExpiredLoginIds_HandlesEmptyAndAllLiveInput()
        {
            Assert.Empty( AccountController.SelectExpiredLoginIds( new KeyValuePair<int, string>[0], Now ) );
            Assert.Empty( AccountController.SelectExpiredLoginIds( new[] { Row( 1, Live ), Row( 2, Live ) }, Now ) );
        }

        [Fact]
        public void CleanupSlot_OpensOncePerTenMinutes()
        {
            var slot = new AccountController.CleanupSlot();

            Assert.True( slot.TryClaim( Now ) );
            Assert.False( slot.TryClaim( Now ) );
            Assert.False( slot.TryClaim( Now.AddMinutes( 10 ).AddSeconds( -1 ) ) );
            Assert.True( slot.TryClaim( Now.AddMinutes( 10 ) ) );
            Assert.False( slot.TryClaim( Now.AddMinutes( 15 ) ) );
        }

        [Fact]
        public void CleanupSlot_WaitsAfterTheClockMovesBack()
        {
            var slot = new AccountController.CleanupSlot();

            Assert.True( slot.TryClaim( Now ) );
            Assert.False( slot.TryClaim( Now.AddHours( -1 ) ) );
            Assert.False( slot.TryClaim( Now.AddMinutes( 5 ) ) );
            Assert.True( slot.TryClaim( Now.AddMinutes( 10 ) ) );
        }

        [Fact]
        public void CleanupSlot_ParallelClaims_OnlyOneWins()
        {
            for ( int round = 0; round < 20; round++ )
            {
                var slot = new AccountController.CleanupSlot();
                var start = new ManualResetEventSlim( false );
                int wins = 0;

                var claims = Enumerable.Range( 0, 50 ).Select( _ => Task.Run( () =>
                {
                    start.Wait();
                    if ( slot.TryClaim( Now ) )
                    {
                        Interlocked.Increment( ref wins );
                    }
                } ) ).ToArray();

                start.Set();
                Task.WaitAll( claims );

                Assert.Equal( 1, wins );
            }
        }
    }
}
