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

using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    /// <summary>
    /// ROCK-8881: exercises the pure rate-limiter policy with an in-memory
    /// store and a controllable clock - no RockCache, no HttpContext.
    /// </summary>
    public class InteractionRateLimiterTests
    {
        // Mirrors RockCacheRateLimitStore semantics (read-modify-write) without
        // a live cache. Retains references, which is fine for these tests.
        private sealed class FakeStore : IRateLimitStore
        {
            private readonly Dictionary<string, RateLimitWindow> _data =
                new Dictionary<string, RateLimitWindow>();

            public RateLimitWindow Get( string key )
            {
                return _data.TryGetValue( key, out var w ) ? w : null;
            }

            public void Set( string key, RateLimitWindow window, TimeSpan ttl )
            {
                _data[key] = window;
            }
        }

        // Simulates a poisoned entry / cache-manager fault to prove the limiter
        // fails open (records) and never lets the exception propagate.
        private sealed class ThrowingStore : IRateLimitStore
        {
            public RateLimitWindow Get( string key ) => throw new InvalidOperationException( "cache fault" );
            public void Set( string key, RateLimitWindow window, TimeSpan ttl ) => throw new InvalidOperationException( "cache fault" );
        }

        private static readonly DateTime T0 = new DateTime( 2026, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        [Fact]
        public void Allows_Up_To_Limit_Then_Drops_Within_Window()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 3, TimeSpan.FromSeconds( 60 ), () => clock );

            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );  // 1
            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );  // 2
            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );  // 3 (at limit)
            Assert.False( limiter.ShouldRecord( "1.2.3.4" ) ); // 4 (over)
            Assert.False( limiter.ShouldRecord( "1.2.3.4" ) ); // 5 (still over)
        }

        [Fact]
        public void Keys_Are_Isolated()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            // Exhaust key A.
            Assert.True( limiter.ShouldRecord( "a" ) );
            Assert.True( limiter.ShouldRecord( "a" ) );
            Assert.False( limiter.ShouldRecord( "a" ) );

            // Key B has its own independent budget.
            Assert.True( limiter.ShouldRecord( "b" ) );
            Assert.True( limiter.ShouldRecord( "b" ) );
            Assert.False( limiter.ShouldRecord( "b" ) );
        }

        [Fact]
        public void Window_Resets_After_It_Elapses()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            Assert.True( limiter.ShouldRecord( "ip" ) );
            Assert.True( limiter.ShouldRecord( "ip" ) );
            Assert.False( limiter.ShouldRecord( "ip" ) ); // over budget

            // Still within the same window at 59s - remains over budget.
            clock = T0.AddSeconds( 59 );
            Assert.False( limiter.ShouldRecord( "ip" ) );

            // At exactly the window length the window rolls over and resets.
            clock = T0.AddSeconds( 60 );
            Assert.True( limiter.ShouldRecord( "ip" ) );
            Assert.True( limiter.ShouldRecord( "ip" ) );
            Assert.False( limiter.ShouldRecord( "ip" ) );
        }

        [Fact]
        public void Limit_Of_One_Allows_Exactly_One_Per_Window()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 30 ), () => clock );

            Assert.True( limiter.ShouldRecord( "ip" ) );
            Assert.False( limiter.ShouldRecord( "ip" ) );

            clock = T0.AddSeconds( 30 );
            Assert.True( limiter.ShouldRecord( "ip" ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void Blank_Key_Always_Allows( string key )
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 60 ), () => clock );

            // Even past the notional limit, a blank key never suppresses.
            Assert.True( limiter.ShouldRecord( key ) );
            Assert.True( limiter.ShouldRecord( key ) );
            Assert.True( limiter.ShouldRecord( key ) );
        }

        [Fact]
        public void Constructor_Rejects_Invalid_Arguments()
        {
            Assert.Throws<ArgumentNullException>(
                () => new InteractionRateLimiter( null, 1, TimeSpan.FromSeconds( 1 ) ) );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new InteractionRateLimiter( new FakeStore(), 0, TimeSpan.FromSeconds( 1 ) ) );
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.Zero ) );
        }

        [Fact]
        public void Store_Exception_Fails_Open_And_Never_Propagates()
        {
            var limiter = new InteractionRateLimiter( new ThrowingStore(), 1, TimeSpan.FromSeconds( 60 ) );

            // A cache fault must never throw out of the limiter, and must resolve
            // to "record" (true) so the public data path is never 500'd and
            // analytics are never suppressed by a limiter fault. Even past the
            // notional limit it keeps returning true because the count is never
            // reached (the store throws first).
            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );
            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );
            Assert.True( limiter.ShouldRecord( "1.2.3.4" ) );
        }
    }

    /// <summary>
    /// ROCK-8881 blocker A: the policy must bound the cache keyspace by hashing
    /// the client IP into a fixed bucket set, so an attacker rotating a spoofed
    /// X-Forwarded-For cannot mint unbounded cache keys.
    /// </summary>
    public class LinkListRateLimitPolicyTests
    {
        [Theory]
        [InlineData( "1.2.3.4" )]
        [InlineData( "203.0.113.255" )]
        [InlineData( "2001:db8::dead:beef" )]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void BucketFor_Is_Always_In_Range( string ip )
        {
            var bucket = LinkListRateLimitPolicy.BucketFor( ip );
            Assert.InRange( bucket, 0, LinkListRateLimitPolicy.BucketCount - 1 );
        }

        [Fact]
        public void BucketFor_Is_Deterministic()
        {
            Assert.Equal(
                LinkListRateLimitPolicy.BucketFor( "198.51.100.7" ),
                LinkListRateLimitPolicy.BucketFor( "198.51.100.7" ) );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void Blank_Or_Null_Ips_Collapse_To_One_Shared_Bucket( string ip )
        {
            // All unresolvable IPs must land in the same bucket so a no-IP flood
            // is still capped rather than bypassing the limiter.
            Assert.Equal( LinkListRateLimitPolicy.BucketFor( "unknown" ), LinkListRateLimitPolicy.BucketFor( ip ) );
        }

        [Fact]
        public void Distinct_Spoofed_Ips_Cannot_Exceed_The_Bucket_Count()
        {
            // 100k distinct "spoofed" addresses still map into at most
            // BucketCount buckets - the property that bounds the cache keyspace.
            var seen = new System.Collections.Generic.HashSet<int>();
            for ( var i = 0; i < 100000; i++ )
            {
                seen.Add( LinkListRateLimitPolicy.BucketFor( "10.0." + ( i / 256 ) + "." + ( i % 256 ) ) );
            }

            Assert.True( seen.Count <= LinkListRateLimitPolicy.BucketCount );
        }
    }
}
