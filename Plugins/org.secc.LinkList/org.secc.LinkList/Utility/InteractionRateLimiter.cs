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
using System.Threading;

using Rock.Logging;
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
    /// <b>What this does and does not shed.</b> <c>PostClick</c> can bail
    /// before it opens a RockContext, because an over-budget click returns the
    /// same empty 200 either way - so a flood costs nothing beyond the parse.
    /// <c>Get</c> cannot: the bag has to be built to be returned, so its
    /// database work runs at full rate regardless. The limiter is therefore
    /// NOT a load shed for <c>Get</c>; protecting that endpoint needs output
    /// caching or a WAF rule and is tracked separately.
    ///
    /// The counting/decision logic is pure and storage-agnostic (see
    /// <see cref="IRateLimitStore"/>), so it is unit-testable without an
    /// HttpContext or a live Rock cache. Production wires it to
    /// <see cref="RockCacheRateLimitStore"/>.
    ///
    /// <b>Fail-open.</b> Every store interaction is wrapped in a try/catch
    /// that returns "record" on ANY exception, and <see cref="LinkListRateLimitPolicy"/>
    /// repeats the guard around its own members so a fault in the policy's own
    /// construction cannot escape either. The public <c>Get</c> data path -
    /// which was cache-independent before ROCK-8881 - therefore cannot be
    /// 500'd by a limiter/cache fault.
    ///
    /// <b>Concurrency.</b> The per-hit increment uses <see cref="Interlocked"/>
    /// and the mutated window is always written back through
    /// <see cref="IRateLimitStore.Set"/>, so correctness does not depend on the
    /// store returning a live shared instance. Two known races, both benign for
    /// best-effort analytics but neither is "always errs toward recording":
    /// at a window boundary two threads may both open a fresh window, and the
    /// loser's increment lands on an orphaned window that may already be over
    /// budget - dropping one event while the live window has full budget. A
    /// backward clock step (NTP, VM time sync) is handled explicitly: negative
    /// elapsed time resets the window rather than wedging an exhausted bucket
    /// closed for the length of the step.
    /// </summary>
    public class InteractionRateLimiter
    {
        private readonly IRateLimitStore _store;
        private readonly int _limit;
        private readonly TimeSpan _window;
        private readonly Func<DateTime> _utcNow;
        private readonly Action<string> _log;

        /// <summary>Ticks of the last emitted fault line; throttles the catch path.</summary>
        private long _lastFaultLogTicks;

        /// <param name="store">Backing store for the per-key windows.</param>
        /// <param name="limit">Max recorded events allowed per key per window (>= 1).</param>
        /// <param name="window">Length of the fixed window (> 0).</param>
        /// <param name="utcNow">Clock, injectable for tests. Defaults to <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="log">
        /// Optional sink for the sampled drop/fault lines. Null (the default)
        /// means nothing is logged and nothing is allocated to decide that -
        /// which is what the unit tests rely on to stay free of Rock.
        /// </param>
        public InteractionRateLimiter( IRateLimitStore store, int limit, TimeSpan window, Func<DateTime> utcNow = null, Action<string> log = null )
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
            _log = log;
        }

        /// <summary>
        /// Charges one event against <paramref name="key"/> and returns whether
        /// it is within budget. <c>true</c> = under the limit, the caller SHOULD
        /// record the interaction; <c>false</c> = over the limit, the caller
        /// should drop it (accept-but-drop).
        ///
        /// This MUTATES shared state - the budget is spent at the moment of the
        /// call, not at the moment of the write. Call it exactly once per event,
        /// at the point the event is actually recorded. To test the budget
        /// without spending it, use <see cref="IsOverBudget"/>.
        ///
        /// A null/blank key always returns <c>true</c> so an unresolvable key
        /// never suppresses analytics, and ANY store exception also returns
        /// <c>true</c> (fail-open).
        /// </summary>
        public bool TryConsume( string key )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
            {
                return true;
            }

            try
            {
                var now = _utcNow();
                var window = _store.Get( key );

                // Start a fresh window on first sight, once the current one has
                // fully elapsed, or if the clock has stepped backward past its
                // start. The stored WindowStartUtc - not the cache TTL - is the
                // source of truth for the boundary, so the reset is
                // deterministic even if the cache retains the entry longer.
                if ( window == null || HasElapsed( now, window ) )
                {
                    _store.Set( key, new RateLimitWindow { WindowStartUtc = now, Count = 1 }, _window );
                    return true;
                }

                var count = Interlocked.Increment( ref window.Count );

                // Write the mutation back so correctness never depends on Get()
                // having returned the live shared instance. Redundant but cheap
                // on today's in-process by-reference RockCache; load-bearing if
                // a future Rock version reintroduces a serializing or
                // distributed handle. Without it, a copy-returning store makes
                // this a silent no-op at any limit >= 2 - and the production
                // limits are 60 and 120.
                _store.Set( key, window, _window );

                if ( count > _limit )
                {
                    // Observability: lets ops tell "limiter engaging" (expected
                    // under load, and the signal that a proxy has collapsed
                    // every visitor into one bucket) apart from "feature
                    // broken". Sampled to one line per key per window - the
                    // drop path is the hot path under exactly the flood this
                    // exists to blunt, so it must not do per-request I/O.
                    if ( _log != null && Interlocked.CompareExchange( ref window.DropLogged, 1, 0 ) == 0 )
                    {
                        SafeLog( $"rate limit engaged for '{key}' ({count} > {_limit} per {_window.TotalSeconds:0}s window)." );
                    }

                    return false;
                }

                return true;
            }
            catch ( Exception ex )
            {
                // Fail OPEN: a limiter/cache fault must never take down the
                // public data path or drop analytics as a side effect.
                LogFault( ex );
                return true;
            }
        }

        /// <summary>
        /// Non-consuming peek: <c>true</c> when the next <see cref="TryConsume"/>
        /// for <paramref name="key"/> would be refused. Charges nothing, so it is
        /// safe to call early - <c>PostClick</c> uses it to skip the database
        /// work for an already-exhausted bucket without spending budget on
        /// requests that never reach the write.
        ///
        /// Fails open in the same direction as <see cref="TryConsume"/>: a blank
        /// key, an absent/elapsed window, or any store exception all report
        /// <c>false</c> (not over budget).
        /// </summary>
        public bool IsOverBudget( string key )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
            {
                return false;
            }

            try
            {
                var window = _store.Get( key );
                if ( window == null || HasElapsed( _utcNow(), window ) )
                {
                    return false;
                }

                // >= because TryConsume increments first and refuses on
                // count > limit, so a window sitting at the limit is already
                // spent.
                return Volatile.Read( ref window.Count ) >= _limit;
            }
            catch ( Exception ex )
            {
                LogFault( ex );
                return false;
            }
        }

        /// <summary>
        /// True when <paramref name="window"/> should be replaced: either it ran
        /// its full length, or the clock moved behind its start. Without the
        /// negative case an exhausted bucket stays refused for the whole
        /// duration of a backward clock step, which is a sustained fail-CLOSED
        /// - the one outcome this class promises never to produce.
        /// </summary>
        private bool HasElapsed( DateTime now, RateLimitWindow window )
        {
            var elapsed = now - window.WindowStartUtc;
            return elapsed >= _window || elapsed < TimeSpan.Zero;
        }

        /// <summary>
        /// Emits at most one fault line per window. Runs from a catch block, so
        /// it swallows its own failures - a logging fault here would propagate
        /// straight out of the fail-open guard and 500 the request it was
        /// protecting. Reads the wall clock directly rather than the injected
        /// one, which may be what threw.
        /// </summary>
        private void LogFault( Exception ex )
        {
            if ( _log == null )
            {
                return;
            }

            var nowTicks = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read( ref _lastFaultLogTicks );
            if ( nowTicks - last < _window.Ticks )
            {
                return;
            }

            if ( Interlocked.CompareExchange( ref _lastFaultLogTicks, nowTicks, last ) != last )
            {
                return;
            }

            SafeLog( $"rate limiter error, failing open (recording): {ex.Message}" );
        }

        private void SafeLog( string message )
        {
            try
            {
                _log( message );
            }
            catch ( Exception )
            {
                // Deliberately empty: see LogFault.
            }
        }
    }

    /// <summary>
    /// One per-key fixed-window counter. Plain mutable fields (not properties):
    /// <see cref="Count"/> and <see cref="DropLogged"/> are updated atomically
    /// via <see cref="Interlocked"/>, which requires a field reference.
    /// </summary>
    public class RateLimitWindow
    {
        /// <summary>UTC instant the current window opened.</summary>
        public DateTime WindowStartUtc;

        /// <summary>Events counted in the current window (including dropped ones).</summary>
        public int Count;

        /// <summary>
        /// 0 until a drop has been logged for this window, 1 after. Samples the
        /// drop line to once per key per window.
        /// </summary>
        public int DropLogged;
    }

    /// <summary>
    /// Storage abstraction for <see cref="InteractionRateLimiter"/>. Kept free
    /// of any Rock dependency so the limiter's logic can be unit-tested with a
    /// trivial in-memory fake (including one that throws, to prove fail-open,
    /// and one that returns copies, to prove the limiter does not secretly
    /// depend on by-reference storage).
    /// </summary>
    public interface IRateLimitStore
    {
        /// <summary>
        /// Current window for <paramref name="key"/>, or null if none.
        ///
        /// May return either the live shared instance or a copy - the limiter
        /// writes every mutation back through <see cref="Set"/> and does not
        /// depend on which. Implementations must be safe to call concurrently
        /// from request threads.
        /// </summary>
        RateLimitWindow Get( string key );

        /// <summary>
        /// Persist <paramref name="window"/> for <paramref name="key"/> with a
        /// time-to-live. The value must be visible to subsequent
        /// <see cref="Get"/> calls for the same key. Must be safe to call
        /// concurrently.
        /// </summary>
        void Set( string key, RateLimitWindow window, TimeSpan ttl );
    }

    /// <summary>
    /// Production <see cref="IRateLimitStore"/> backed by Rock's shared cache.
    ///
    /// RockCache in 1.16.x is in-process and by-reference (serialization is
    /// hardcoded off and the distributed backing was removed), but the limiter
    /// does not rely on that - it writes every mutation back explicitly, so a
    /// future Rock version that reintroduces a serializing handle changes
    /// nothing here.
    ///
    /// This is a thin, honest adapter - it does NOT swallow exceptions; the
    /// fail-open guard lives in <see cref="InteractionRateLimiter"/> and
    /// <see cref="LinkListRateLimitPolicy"/>.
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
    /// bucket share a budget; being analytics-only accept-but-drop, that is
    /// harmless.
    ///
    /// <b>Salted bucket assignment.</b> The hash is salted with a value
    /// generated once per process. <see cref="string.GetHashCode()"/> is
    /// unrandomized on net472 unless <c>UseRandomizedStringHashAlgorithm</c> is
    /// enabled, which Rock's web.config does not do - so without the salt an
    /// attacker could compute a chosen victim's bucket offline and saturate it
    /// with a trickle of requests, silently suppressing that victim's analytics
    /// while every response stayed 200. The salt makes bucket assignment
    /// unpredictable outside the process. It changes on app-pool recycle, which
    /// is immaterial for a 60-second window.
    ///
    /// <b>Residual risk - XFF spoofing (ROCK-8881 Major C, accepted by human).</b>
    /// The client IP is derived from <c>X-Forwarded-For</c> with no
    /// trusted-proxy validation (see <see cref="ClientIpResolver"/>). An
    /// attacker who rotates a fresh spoofed address per request spreads traffic
    /// across buckets and BYPASSES this limiter. It therefore blunts naive
    /// single-IP floods and crawlers, NOT a deliberate distributed spoofing
    /// script. A trusted-proxy allowlist is intentionally out of scope for this
    /// ticket; the bounded keyspace above ensures the bypass attempt still
    /// cannot exhaust memory, and <see cref="ClientIpResolver"/> ensures the
    /// spoofed value cannot reach the analytics tables unparsed.
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

        /// <summary>
        /// Per-process hash salt. See the class remarks on salted bucket
        /// assignment.
        /// </summary>
        private static readonly string BucketSalt = Guid.NewGuid().ToString( "N" );

        /// <summary>
        /// Lazy, not an eager field initializer, so a construction fault throws
        /// at <c>.Value</c> - inside the fail-open guards below - rather than in
        /// this type's initializer. A throwing type initializer is cached by the
        /// CLR and rethrown on every subsequent touch of the type for the life
        /// of the AppDomain, which would permanently 500 the anonymous data
        /// path the limiter is supposed to leave alone.
        /// </summary>
        private static readonly Lazy<InteractionRateLimiter> ViewLimiter =
            new Lazy<InteractionRateLimiter>( () => CreateLimiter( ViewWritesPerWindow ) );

        private static readonly Lazy<InteractionRateLimiter> ClickLimiter =
            new Lazy<InteractionRateLimiter>( () => CreateLimiter( ClickWritesPerWindow ) );

        /// <summary>Ticks of the last emitted policy-fault line.</summary>
        private static long _lastFaultLogTicks;

        /// <summary>True if a View for this IP is within budget and should be recorded.</summary>
        public static bool TryConsumeView( string ipAddress )
        {
            try
            {
                return ViewLimiter.Value.TryConsume( ViewKeyFor( ipAddress ) );
            }
            catch ( Exception ex )
            {
                LogFault( ex );
                return true;
            }
        }

        /// <summary>True if a Click for this IP is within budget and should be recorded.</summary>
        public static bool TryConsumeClick( string ipAddress )
        {
            try
            {
                return ClickLimiter.Value.TryConsume( ClickKeyFor( ipAddress ) );
            }
            catch ( Exception ex )
            {
                LogFault( ex );
                return true;
            }
        }

        /// <summary>
        /// Non-consuming peek used by <c>PostClick</c> to skip its database work
        /// for an exhausted bucket. Charges nothing - <c>RecordClick</c> is what
        /// spends the budget.
        /// </summary>
        public static bool IsClickOverBudget( string ipAddress )
        {
            try
            {
                return ClickLimiter.Value.IsOverBudget( ClickKeyFor( ipAddress ) );
            }
            catch ( Exception ex )
            {
                LogFault( ex );
                return false;
            }
        }

        /// <summary>
        /// The View cache key for this IP. Public so tests can assert the
        /// property that actually bounds the keyspace: the raw IP never appears
        /// in the key.
        /// </summary>
        public static string ViewKeyFor( string ipAddress )
        {
            return ViewKeyPrefix + BucketFor( ipAddress );
        }

        /// <summary>The Click cache key for this IP. See <see cref="ViewKeyFor"/>.</summary>
        public static string ClickKeyFor( string ipAddress )
        {
            return ClickKeyPrefix + BucketFor( ipAddress );
        }

        /// <summary>
        /// Hashes the salted, normalized client IP into <see cref="BucketCount"/>
        /// buckets. The mask forces a non-negative value before the modulo. An
        /// unresolvable IP collapses to a single shared bucket so a flood with
        /// no usable address is still capped rather than bypassing the limiter
        /// entirely - if that bucket is the one engaging under normal traffic,
        /// the proxy is not setting X-Forwarded-For and every visitor has
        /// collapsed into it.
        /// </summary>
        public static int BucketFor( string ipAddress )
        {
            var normalized = string.IsNullOrWhiteSpace( ipAddress ) ? "unknown" : ipAddress.Trim();
            return ( ( BucketSalt + normalized ).GetHashCode() & 0x7FFFFFFF ) % BucketCount;
        }

        private static InteractionRateLimiter CreateLimiter( int limit )
        {
            return new InteractionRateLimiter(
                new RockCacheRateLimitStore(),
                limit,
                TimeSpan.FromSeconds( WindowSeconds ),
                log: Log );
        }

        /// <summary>
        /// Production log sink. RockLogger defaults to <c>Off</c> with an empty
        /// domain list, so this is a genuine no-op until an admin enables it -
        /// unlike <c>Trace.WriteLine</c>, which always has DefaultTraceListener
        /// attached and serializes on a machine-global mutex.
        /// </summary>
        private static void Log( string message )
        {
            try
            {
                RockLogger.Log.Warning( RockLogDomains.Other, "org.secc.LinkList: {Message}", message );
            }
            catch ( Exception )
            {
                // Logging must never be the thing that breaks the data path.
            }
        }

        /// <summary>
        /// Emits at most one policy-fault line per window. A construction fault
        /// is a permanent condition, so an unsampled line here would fire on
        /// every request for the life of the AppDomain.
        /// </summary>
        private static void LogFault( Exception ex )
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read( ref _lastFaultLogTicks );
            if ( nowTicks - last < TimeSpan.TicksPerSecond * WindowSeconds )
            {
                return;
            }

            if ( Interlocked.CompareExchange( ref _lastFaultLogTicks, nowTicks, last ) != last )
            {
                return;
            }

            Log( $"rate limit policy fault, failing open: {ex.Message}" );
        }
    }
}
