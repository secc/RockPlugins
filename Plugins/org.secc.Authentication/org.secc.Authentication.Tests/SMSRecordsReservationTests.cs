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

// SMSRecords is process-wide static state, so the tests in this assembly run one at a time.
[assembly: CollectionBehavior( DisableTestParallelization = true )]

namespace org.secc.Authentication.Tests
{
    public class SMSRecordsReservationTests
    {
        [Fact]
        public void ReleasedItems_CanBeReservedAgain()
        {
            var ip = "ip-" + Guid.NewGuid().ToString( "N" );
            var phone = "phone-" + Guid.NewGuid().ToString( "N" );

            Assert.True( SMSRecords.ReserveItems( ip, phone ) );
            Assert.False( SMSRecords.ReserveItems( ip, "other-" + phone ) );
            Assert.False( SMSRecords.ReserveItems( "other-" + ip, phone ) );

            SMSRecords.ReleaseItems( ip, phone );

            Assert.True( SMSRecords.ReserveItems( ip, phone ) );
            SMSRecords.ReleaseItems( ip, phone );
        }

        /// <summary>
        /// Each thread reserves and releases only its own ip/phone pair, so every reservation
        /// must succeed and nothing may be left reserved, however the threads interleave.
        /// </summary>
        [Fact]
        public void ConcurrentReserveAndRelease_NeverLoseOrKeepAnItem()
        {
            const int threadCount = 8;
            const int rounds = 20000;
            var prefix = Guid.NewGuid().ToString( "N" );
            var failures = 0;
            var start = new Barrier( threadCount );

            var threads = Enumerable.Range( 0, threadCount ).Select( t => new Thread( () =>
            {
                var ip = prefix + "-ip-" + t;
                var phone = prefix + "-phone-" + t;
                start.SignalAndWait();
                for ( var i = 0; i < rounds; i++ )
                {
                    try
                    {
                        if ( !SMSRecords.ReserveItems( ip, phone ) )
                        {
                            Interlocked.Increment( ref failures );
                        }
                        SMSRecords.ReleaseItems( ip, phone );
                    }
                    catch ( Exception )
                    {
                        Interlocked.Increment( ref failures );
                    }
                }
            } ) ).ToList();

            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            Assert.Equal( 0, failures );
            for ( var t = 0; t < threadCount; t++ )
            {
                Assert.True( SMSRecords.ReserveItems( prefix + "-ip-" + t, prefix + "-phone-" + t ) );
                SMSRecords.ReleaseItems( prefix + "-ip-" + t, prefix + "-phone-" + t );
            }
        }
    }
}
