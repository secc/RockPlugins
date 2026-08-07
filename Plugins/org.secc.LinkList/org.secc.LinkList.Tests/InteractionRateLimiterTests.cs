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
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        // Mirrors RockCacheRateLimitStore: read-modify-write, by reference, and
        // safe to call concurrently (the interface requires it, and the
        // concurrency test below depends on it).
        private sealed class FakeStore : IRateLimitStore
        {
            private readonly ConcurrentDictionary<string, RateLimitWindow> _data =
                new ConcurrentDictionary<string, RateLimitWindow>();

            public RateLimitWindow Get( string key )
            {
                return _data.TryGetValue( key, out var w ) ? w : null;
            }

            public void Set( string key, RateLimitWindow window, TimeSpan ttl )
            {
                _data[key] = window;
            }
        }

        // Returns a COPY from Get, so a mutation the limiter makes to the
        // returned object is invisible unless the limiter writes it back. This
        // is the shape a serializing or distributed cache handle would have.
        // RockCache is by-reference today, but RockCache.IsCacheSerialized is
        // marked [Obsolete] with a note that Rock wants to keep items
        // serializable - so the platform has reserved the right to change it,
        // and the limiter must not silently depend on liveness.
        private sealed class CloningStore : IRateLimitStore
        {
            private readonly Dictionary<string, RateLimitWindow> _data =
                new Dictionary<string, RateLimitWindow>();

            public RateLimitWindow Get( string key )
            {
                return _data.TryGetValue( key, out var w ) ? Clone( w ) : null;
            }

            public void Set( string key, RateLimitWindow window, TimeSpan ttl )
            {
                _data[key] = Clone( window );
            }

            private static RateLimitWindow Clone( RateLimitWindow w )
            {
                return new RateLimitWindow
                {
                    WindowStartUtc = w.WindowStartUtc,
                    Count = w.Count,
                    DropLogged = w.DropLogged
                };
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

            Assert.True( limiter.TryConsume( "1.2.3.4" ) );  // 1
            Assert.True( limiter.TryConsume( "1.2.3.4" ) );  // 2
            Assert.True( limiter.TryConsume( "1.2.3.4" ) );  // 3 (at limit)
            Assert.False( limiter.TryConsume( "1.2.3.4" ) ); // 4 (over)
            Assert.False( limiter.TryConsume( "1.2.3.4" ) ); // 5 (still over)
        }

        [Fact]
        public void Keys_Are_Isolated()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            // Exhaust key A.
            Assert.True( limiter.TryConsume( "a" ) );
            Assert.True( limiter.TryConsume( "a" ) );
            Assert.False( limiter.TryConsume( "a" ) );

            // Key B has its own independent budget.
            Assert.True( limiter.TryConsume( "b" ) );
            Assert.True( limiter.TryConsume( "b" ) );
            Assert.False( limiter.TryConsume( "b" ) );
        }

        [Fact]
        public void Window_Resets_After_It_Elapses()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) ); // over budget

            // Still within the same window at 59s - remains over budget.
            clock = T0.AddSeconds( 59 );
            Assert.False( limiter.TryConsume( "ip" ) );

            // At exactly the window length the window rolls over and resets.
            clock = T0.AddSeconds( 60 );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );
        }

        [Fact]
        public void Backward_Clock_Step_Resets_Instead_Of_Wedging_Closed()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );

            // An NTP correction or VM time sync steps the host clock backward.
            // Without treating negative elapsed time as a reset, this bucket
            // stays refused for the entire length of the step - a sustained
            // fail-CLOSED, which is the one outcome the limiter promises never
            // to produce.
            clock = T0.AddMinutes( -30 );
            Assert.True( limiter.TryConsume( "ip" ) );
        }

        [Fact]
        public void Limit_Of_One_Allows_Exactly_One_Per_Window()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 30 ), () => clock );

            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );

            clock = T0.AddSeconds( 30 );
            Assert.True( limiter.TryConsume( "ip" ) );
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
            Assert.True( limiter.TryConsume( key ) );
            Assert.True( limiter.TryConsume( key ) );
            Assert.True( limiter.TryConsume( key ) );
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
            Assert.True( limiter.TryConsume( "1.2.3.4" ) );
            Assert.True( limiter.TryConsume( "1.2.3.4" ) );
            Assert.True( limiter.TryConsume( "1.2.3.4" ) );
        }

        [Fact]
        public void Bounds_Correctly_With_A_Copy_Returning_Store()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new CloningStore(), 3, TimeSpan.FromSeconds( 60 ), () => clock );

            // The limiter must not depend on Get() handing back the live shared
            // instance. If it only mutated the returned object without writing
            // it back, the increment would die with the copy and the count
            // would never pass the limit - making the limiter a totally silent
            // no-op at any limit >= 2. Production runs at 60 and 120, so the
            // bug would only have been invisible in a limit-of-one test.
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );

            clock = T0.AddSeconds( 60 );
            Assert.True( limiter.TryConsume( "ip" ) );
        }

        [Fact]
        public void Concurrent_Callers_Get_Exactly_The_Limit()
        {
            const int limit = 50;
            const int threadCount = 8;
            const int callsPerThread = 40;

            var limiter = new InteractionRateLimiter( new FakeStore(), limit, TimeSpan.FromMinutes( 5 ), () => T0 );

            // Prime the window single-threaded. The window-open check is
            // deliberately not transactional, so racing on first sight can open
            // two windows - a documented, benign undercount that would make an
            // exact assertion flaky for the wrong reason. What is being pinned
            // here is the increment path.
            var allowed = 1;
            Assert.True( limiter.TryConsume( "shared" ) );

            var barrier = new Barrier( threadCount );
            var threads = Enumerable.Range( 0, threadCount ).Select( _ => new Thread( () =>
            {
                barrier.SignalAndWait();
                for ( var i = 0; i < callsPerThread; i++ )
                {
                    if ( limiter.TryConsume( "shared" ) )
                    {
                        Interlocked.Increment( ref allowed );
                    }
                }
            } ) ).ToList();

            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            // Exactly the limit, no more and no fewer. Swapping
            // Interlocked.Increment back to Count++ or turning Count into a
            // property loses updates and lets this overshoot.
            Assert.Equal( limit, allowed );
        }

        [Fact]
        public void IsOverBudget_Does_Not_Consume()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 3, TimeSpan.FromSeconds( 60 ), () => clock );

            for ( var i = 0; i < 10; i++ )
            {
                Assert.False( limiter.IsOverBudget( "ip" ) );
            }

            // The peeks spent nothing - the full budget is still available.
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );
        }

        [Fact]
        public void IsOverBudget_Reports_True_Only_Once_Exhausted()
        {
            var clock = T0;
            var limiter = new InteractionRateLimiter( new FakeStore(), 2, TimeSpan.FromSeconds( 60 ), () => clock );

            Assert.False( limiter.IsOverBudget( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.IsOverBudget( "ip" ) );
            Assert.True( limiter.TryConsume( "ip" ) );

            // At the limit the budget is spent, so the next consume would be
            // refused and the peek must say so.
            Assert.True( limiter.IsOverBudget( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );

            clock = T0.AddSeconds( 60 );
            Assert.False( limiter.IsOverBudget( "ip" ) );
        }

        [Fact]
        public void IsOverBudget_Fails_Open()
        {
            Assert.False( new InteractionRateLimiter( new ThrowingStore(), 1, TimeSpan.FromSeconds( 60 ) ).IsOverBudget( "ip" ) );
            Assert.False( new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 60 ) ).IsOverBudget( "   " ) );
        }

        [Fact]
        public void Drop_Is_Logged_Once_Per_Window()
        {
            var clock = T0;
            var lines = new List<string>();
            var limiter = new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 60 ), () => clock, lines.Add );

            Assert.True( limiter.TryConsume( "ip" ) );
            for ( var i = 0; i < 25; i++ )
            {
                Assert.False( limiter.TryConsume( "ip" ) );
            }

            // The drop path is the hot path under exactly the flood the limiter
            // exists to blunt, so it must not log per request.
            Assert.Single( lines );

            clock = T0.AddSeconds( 60 );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );
            Assert.Equal( 2, lines.Count );
        }

        [Fact]
        public void Fault_Log_Is_Sampled()
        {
            var lines = new List<string>();
            var limiter = new InteractionRateLimiter( new ThrowingStore(), 1, TimeSpan.FromSeconds( 60 ), null, lines.Add );

            for ( var i = 0; i < 25; i++ )
            {
                Assert.True( limiter.TryConsume( "ip" ) );
            }

            // A faulting cache faults on every request; an unsampled line there
            // would be a write amplifier on an unauthenticated endpoint.
            Assert.Single( lines );
        }

        [Fact]
        public void A_Throwing_Log_Sink_Never_Escapes()
        {
            var clock = T0;
            Action<string> boom = _ => throw new InvalidOperationException( "logger fault" );

            var limiter = new InteractionRateLimiter( new FakeStore(), 1, TimeSpan.FromSeconds( 60 ), () => clock, boom );
            Assert.True( limiter.TryConsume( "ip" ) );
            Assert.False( limiter.TryConsume( "ip" ) );

            // Same on the fault path, where the sink runs from inside the
            // fail-open catch and a throw would propagate straight out.
            var faulting = new InteractionRateLimiter( new ThrowingStore(), 1, TimeSpan.FromSeconds( 60 ), () => clock, boom );
            Assert.True( faulting.TryConsume( "ip" ) );
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
        public void Keys_Never_Contain_The_Raw_Client_Ip()
        {
            const string ip = "203.0.113.9";

            var viewKey = LinkListRateLimitPolicy.ViewKeyFor( ip );
            var clickKey = LinkListRateLimitPolicy.ClickKeyFor( ip );

            // This is the property that actually bounds the RockCache keyspace,
            // and it is what the old <= BucketCount assertion failed to check:
            // the key suffix is a bucket index, never the address. Reverting to
            // prefix + ipAddress would reopen the unbounded-keyspace blocker
            // while leaving every count-based test green.
            Assert.DoesNotContain( ip, viewKey );
            Assert.DoesNotContain( ip, clickKey );

            // Views and clicks must not share a budget.
            Assert.NotEqual( viewKey, clickKey );

            var suffix = viewKey.Substring( viewKey.LastIndexOf( ':' ) + 1 );
            Assert.True( int.TryParse( suffix, out var bucket ) );
            Assert.InRange( bucket, 0, LinkListRateLimitPolicy.BucketCount - 1 );
        }

        [Fact]
        public void BucketFor_Spreads_Across_The_Bucket_Space()
        {
            var seen = new HashSet<int>();
            for ( var i = 0; i < 100000; i++ )
            {
                seen.Add( LinkListRateLimitPolicy.BucketFor( "10.0." + ( i / 256 ) + "." + ( i % 256 ) ) );
            }

            // The previous assertion here (seen.Count <= BucketCount) was
            // guaranteed by the modulo and passed even for a constant hash.
            // 100k addresses over 8192 buckets should reach essentially all of
            // them; anything less means the hash is not distributing and
            // unrelated visitors are sharing budgets.
            Assert.InRange( seen.Count, LinkListRateLimitPolicy.BucketCount * 9 / 10, LinkListRateLimitPolicy.BucketCount );
        }

        [Fact]
        public void BucketFor_Is_Salted_So_A_Victims_Bucket_Cannot_Be_Computed_Offline()
        {
            // String.GetHashCode is unrandomized on net472 unless
            // UseRandomizedStringHashAlgorithm is enabled, which Rock's
            // web.config does not do. Unsalted, an attacker computes a chosen
            // victim's bucket with nothing but the IP, then saturates it with a
            // trickle of requests and silently blanks that victim's analytics
            // while every response stays 200.
            var ips = new[] { "1.2.3.4", "203.0.113.255", "198.51.100.7", "2001:db8::dead:beef", "10.20.30.40" };

            Assert.Contains( ips, ip =>
                LinkListRateLimitPolicy.BucketFor( ip )
                    != ( ( ip.GetHashCode() & 0x7FFFFFFF ) % LinkListRateLimitPolicy.BucketCount ) );
        }
    }
}
