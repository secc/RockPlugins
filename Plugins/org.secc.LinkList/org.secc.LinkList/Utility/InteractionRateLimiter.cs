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
using System.Diagnostics;
using System.Threading;

using Rock.Web.Cache;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-8881: fixed-window, per-key rate limiter for the anonymous
    /// LinkList analytics endpoints. It governs interaction <b>writes</b>
    /// only, with accept-but-drop semantics: an over-budget event is silently
    /// skipped (the View bag is still returned, the click beacon still
    /// answers 200) - a shared-NAT visitor is NEVER blocked from the data
    /// path. The limiter only decides whether the analytics row is recorded.
    ///
    /// The counting/decision logic is pure and storage-agnostic (see
    /// <see cref="IRateLimitStore"/>), so it is unit-testable without an
    /// HttpContext or a live Rock cache. Production wires it to
    /// <see cref="RockCacheRateLimitStore"/>.
    ///
    /// <b>Fail-open, guaranteed here.</b> The entire store interaction is
    /// wrapped in a try/catch that returns <c>true</c> (record) on ANY
    /// exception. This is deliberately centralized in <see cref="ShouldRecord"/>
    /// rather than in the store or the call sites, so the "never fails closed"
    /// invariant holds for every current and future caller even if the backing
    /// cache throws (poisoned entry, cache-manager fault). The public
    /// <c>Get</c> data path - which was cache-independent before ROCK-8881 -
    /// therefore cannot be 500'd by a limiter/cache fault.
    ///
    /// <b>Concurrency.</b> On the target platform RockCache (1.16.x) is an
    /// in-process, by-reference cache (see <see cref="RockCacheRateLimitStore"/>),
    /// so the stored <see cref="RateLimitWindow"/> is a shared object. The
    /// per-hit increment uses <see cref="Interlocked"/> to avoid lost updates.
    /// The window-reset check itself is not transactional, so under a burst two
    /// threads may both open a fresh window - a slight undercount that only ever
    /// errs toward recording (fail-open), which is acceptable for best-effort
    /// analytics limiting.
    /// </summary>
    public class InteractionRateLimiter
    {
        private readonly IRateLimitStore _store;
        private readonly int _limit;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _utcNow;

        /// <param name="store">Backing store for the per-key windows.</param>
        /// <param name="limit">Max recorded events allowed per key per window (>= 1).</param>
        /// <param name="window">Length of the fixed window (> 0).</param>
        /// <param name="utcNow">Clock, injectable for tests. Defaults to <see cref="DateTime.UtcNow"/>.</param>
        public InteractionRateLimiter( IRateLimitStore store, int limit, TimeSpan window, Func<DateTime> utcNow = null )
        {
            if ( store == null )
            {
                throw new ArgumentNullException( nameof( store ) );
            }
            if ( limit < 1 )
            {
                throw new ArgumentOutOfRangeException( nameof( limit ), "limit must be at least 1." );
            }
            if ( window <= TimeSpan.Zero )
            {
                throw new ArgumentOutOfRangeException( nameof( window ), "window must be positive." );
            }

            _store = store;
            _limit = limit;
            _window = window;
            _utcNow = utcNow ?? ( () => DateTime.UtcNow );
        }

        /// <summary>
        /// Records one event against <paramref name="key"/> and returns whether
        /// it is within budget. <c>true</c> = under the limit, the caller SHOULD
        /// record the interaction; <c>false</c> = over the limit, the caller
        /// should drop it (accept-but-drop). A null/blank key always returns
        /// <c>true</c> so an unresolvable key never suppresses analytics, and
        /// ANY store exception also returns <c>true</c> (fail-open).
        /// </summary>
        public bool ShouldRecord( string key )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
            {
                return true;
            }

            try
            {
                var now = _utcNow();
                var window = _store.Get( key );

                // Start a fresh window on first sight or once the current one
                // has fully elapsed. The stored WindowStartUtc - not the cache
                // TTL - is the source of truth for the window boundary, so the
                // reset is deterministic even if the cache retains the entry
                // longer.
                if ( window == null || ( now - window.WindowStartUtc ) >= _window )
                {
                    _store.Set( key, new RateLimitWindow { WindowStartUtc = now, Count = 1 }, _window );
                    return true;
                }

                // Shared in-memory reference (RockCache 1.16 is by-reference);
                // the atomic increment is visible to concurrent/subsequent
                // requests without re-Set'ing the entry.
                var count = Interlocked.Increment( ref window.Count );
                if ( count > _limit )
                {
                    // Observability: lets ops tell "limiter engaging" (expected
                    // under load) apart from "feature broken". Client response
                    // is unchanged (silent drop). Trace has no listeners by
                    // default, so this is a cheap no-op in production unless one
                    // is attached.
                    Trace.WriteLine( $"org.secc.LinkList: rate limit engaged for '{key}' ({count} > {_limit} per {_window.TotalSeconds:0}s window)." );
                    return false;
                }

                return true;
            }
            catch ( Exception ex )
            {
                // Fail OPEN: a limiter/cache fault must never take down the
                // public data path or drop analytics as a side effect.
                Trace.WriteLine( $"org.secc.LinkList: rate limiter error for '{key}', failing open (recording): {ex.Message}" );
                return true;
            }
        }
    }

    /// <summary>
    /// One per-key fixed-window counter. Plain mutable fields (not properties):
    /// <see cref="Count"/> is incremented atomically via
    /// <see cref="Interlocked.Increment(ref int)"/>, which requires a field
    /// reference.
    /// </summary>
    public class RateLimitWindow
    {
        /// <summary>UTC instant the current window opened.</summary>
        public DateTime WindowStartUtc;

        /// <summary>Events counted in the current window (including dropped ones).</summary>
        public int Count;
    }

    /// <summary>
    /// Storage abstraction for <see cref="InteractionRateLimiter"/>. Kept free
    /// of any Rock dependency so the limiter's logic can be unit-tested with a
    /// trivial in-memory fake (including one that throws, to prove fail-open).
    /// </summary>
    public interface IRateLimitStore
    {
        /// <summary>Current window for <paramref name="key"/>, or null if none.</summary>
        RateLimitWindow Get( string key );

        /// <summary>Persist <paramref name="window"/> for <paramref name="key"/> with a time-to-live.</summary>
        void Set( string key, RateLimitWindow window, TimeSpan ttl );
    }

    /// <summary>
    /// Production <see cref="IRateLimitStore"/> backed by Rock's shared cache.
    ///
    /// RockCache in 1.16.x is <b>in-process and by-reference</b>: serialization
    /// is hardcoded off and the distributed (Redis) backing was removed, so
    /// <see cref="RockCache.Get(string, string)"/> returns the live shared
    /// object and mutating it is immediately visible to later reads. That is
    /// why the limiter can increment the returned window in place.
    ///
    /// This is a thin, honest adapter - it does NOT swallow exceptions; the
    /// fail-open guard lives in <see cref="InteractionRateLimiter.ShouldRecord"/>.
    ///
    /// <b>Keyspace must stay bounded (ROCK-8881 blocker).</b>
    /// <see cref="RockCache.AddOrUpdate(string, string, object, TimeSpan)"/>
    /// registers every distinct key in a process-global reference set that TTL
    /// eviction never prunes (only explicit Remove/ClearAll do). Callers must
    /// therefore hand this store a key drawn from a small, fixed set - see
    /// <see cref="LinkListRateLimitPolicy"/>, which hashes the client IP into a
    /// bounded bucket space so an attacker cannot mint unbounded keys by
    /// rotating a spoofed address.
    /// </summary>
    public class RockCacheRateLimitStore : IRateLimitStore
    {
        /// <summary>Cache region isolating LinkList rate-limit keys.</summary>
        public const string Region = "org.secc.LinkList.RateLimit";

        public RateLimitWindow Get( string key )
        {
            return RockCache.Get( key, Region ) as RateLimitWindow;
        }

        public void Set( string key, RateLimitWindow window, TimeSpan ttl )
        {
            RockCache.AddOrUpdate( key, Region, window, ttl );
        }
    }

    /// <summary>
    /// Concrete rate-limit policy for the LinkList anonymous endpoints. The
    /// limits are plugin constants (not SystemSettings) - tune here and
    /// redeploy. Views and clicks use independent buckets so a page's views
    /// never crowd out its clicks.
    ///
    /// <b>Bounded keyspace.</b> The client IP is hashed into a fixed number of
    /// buckets (<see cref="BucketCount"/>) and the bucket index - never the raw
    /// IP - is the cache key suffix. This caps the total distinct cache keys at
    /// <c>BucketCount x 2 prefixes</c>, which is required because RockCache
    /// never prunes its key-reference set (see
    /// <see cref="RockCacheRateLimitStore"/>). It also keeps client IPs (PII)
    /// out of admin-visible cache keys. Distinct IPs that collide into the same
    /// bucket share a budget; with a large bucket count and the generous limits
    /// below that is rare and, being analytics-only accept-but-drop, harmless.
    ///
    /// <b>Residual risk - XFF spoofing (ROCK-8881 Major C, accepted by human).</b>
    /// The client IP comes from <c>WebRequestHelper.GetClientIpAddress</c>,
    /// which trusts the first <c>X-Forwarded-For</c> token with no trusted-proxy
    /// validation. An attacker who rotates a fresh spoofed XFF per request
    /// spreads traffic across buckets and BYPASSES this limiter. It therefore
    /// blunts naive single-IP floods and crawlers, NOT a deliberate distributed
    /// spoofing script. A trusted-proxy allowlist is intentionally out of scope
    /// for this ticket; the bounded keyspace above ensures the bypass attempt
    /// still cannot exhaust memory.
    /// </summary>
    public static class LinkListRateLimitPolicy
    {
        /// <summary>Fixed window length, in seconds.</summary>
        public const int WindowSeconds = 60;

        /// <summary>Max recorded View writes per bucket per window.</summary>
        public const int ViewWritesPerWindow = 60;

        /// <summary>Max recorded Click writes per bucket per window.</summary>
        public const int ClickWritesPerWindow = 120;

        /// <summary>
        /// Number of IP buckets. Bounds the cache keyspace to
        /// <c>BucketCount x 2</c> keys total. A power of two, large enough that
        /// collisions between concurrently-active IPs are rare.
        /// </summary>
        public const int BucketCount = 8192;

        private const string ViewKeyPrefix = "linklist:view:";
        private const string ClickKeyPrefix = "linklist:click:";

        private static readonly IRateLimitStore Store = new RockCacheRateLimitStore();

        private static readonly TimeSpan Window = TimeSpan.FromSeconds( WindowSeconds );

        /// <summary>Governs View-interaction writes from <c>Get</c>.</summary>
        public static readonly InteractionRateLimiter ViewLimiter =
            new InteractionRateLimiter( Store, ViewWritesPerWindow, Window );

        /// <summary>Governs Click-interaction writes from <c>PostClick</c>.</summary>
        public static readonly InteractionRateLimiter ClickLimiter =
            new InteractionRateLimiter( Store, ClickWritesPerWindow, Window );

        /// <summary>True if a View for this IP is within budget and should be recorded.</summary>
        public static bool ShouldRecordView( string ipAddress )
        {
            return ViewLimiter.ShouldRecord( ViewKeyPrefix + BucketFor( ipAddress ) );
        }

        /// <summary>True if a Click for this IP is within budget and should be recorded.</summary>
        public static bool ShouldRecordClick( string ipAddress )
        {
            return ClickLimiter.ShouldRecord( ClickKeyPrefix + BucketFor( ipAddress ) );
        }

        /// <summary>
        /// Hashes the (normalized) client IP into <see cref="BucketCount"/>
        /// buckets. <see cref="string.GetHashCode()"/> is stable within a
        /// process on net472 - the only scope that matters for a per-process
        /// in-memory cache, so its lack of cross-process stability is
        /// irrelevant here. The mask forces a non-negative value before the
        /// modulo. An unresolvable IP collapses to a single shared bucket so a
        /// flood with no usable address is still capped rather than bypassing
        /// the limiter entirely.
        /// </summary>
        public static int BucketFor( string ipAddress )
        {
            var normalized = string.IsNullOrWhiteSpace( ipAddress ) ? "unknown" : ipAddress.Trim();
            return ( normalized.GetHashCode() & 0x7FFFFFFF ) % BucketCount;
        }
    }
}
