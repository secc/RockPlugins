using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rock.Bus;
using Rock.Bus.Queue;
using Rock.Logging;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    public class CheckinCache<T> : IItemCache
    {
        private const string AllRegion = "AllItems";
        protected static readonly string AllString = "All";

        private static readonly string KeyPrefix = $"{typeof( T ).Name}";

        private static string AllKey => $"{typeof( T ).Name}:{AllString}";

        // Throttle key refreshes to avoid excessive DB calls
        private static readonly object KeysUpdateLock = new object();
        private static DateTime _lastKeysRefreshUtc = DateTime.MinValue;
        private static readonly TimeSpan KeysRefreshInterval = TimeSpan.FromSeconds( 10 );

        // The subclass supplies its keyFactory as a delegate on every public call.
        // Remember the most recent one so internal/remote paths that have no keyFactory
        // of their own (ClearLocal, called from the bus consumer) can still rebuild the
        // key set from the DB when the cached AllKeys list is missing.
        private static Func<List<string>> _lastKeyFactory;

        private static void RememberKeyFactory( Func<List<string>> keyFactory )
        {
            if ( keyFactory != null )
            {
                _lastKeyFactory = keyFactory;
            }
        }

        // Warm-up: for the first N seconds after THIS NODE's bus comes online,
        // suppress publishing so a cold, recycled node repopulating its cache
        // does not flood the bus and force every other node to churn.
        //
        // The window is anchored to when the bus is first observed ready
        // (see CheckinCacheWarmUp), NOT to type load. Type load happens early
        // in app start, well before Rock/the bus finish initializing — with a
        // type-load anchor the grace period expired during the (often >30s)
        // cold start, before the node could publish anything, so it never
        // actually suppressed the startup burst.
        // Override via web.config appSettings: <add key="CheckinCacheWarmUpSeconds" value="30" />
        private static readonly TimeSpan WarmUpGracePeriod = TimeSpan.FromSeconds(
            Math.Max( 0,
                int.TryParse( System.Configuration.ConfigurationManager.AppSettings["CheckinCacheWarmUpSeconds"], out int configuredSeconds )
                    ? configuredSeconds
                    : 30 ) );

        public void PostCached()
        {
        }

        /// <summary>
        /// Returns true once Rock is started AND the grace period (measured from
        /// when the bus was first observed ready) has elapsed. During warm-up the
        /// node still consumes messages but does not publish.
        /// </summary>
        private static bool IsReadyToPublish
        {
            get
            {
                if ( !RockMessageBus.IsRockStarted )
                {
                    return false;
                }

                // Anchor the grace window to the first moment the bus is ready
                // (process-wide, set once). Measuring from here — not type load —
                // means the window covers the node's first N seconds of being
                // online, which is exactly when a cold node repopulating its
                // cache would otherwise flood the bus.
                var now = DateTime.UtcNow;
                var busReadyUtc = CheckinCacheWarmUp.MarkBusReady( now );
                return ( now - busReadyUtc ) >= WarmUpGracePeriod;
            }
        }

        public static List<string> AllKeys( Func<List<string>> keyFactory )
        {
            RememberKeyFactory( keyFactory );
            var keys = AllKeys();
            if ( !keys.Any() )
            {
                keys = UpdateKeys( keyFactory );
            }
            return keys;
        }

        public static T Get( string qualifiedKey, Func<T> itemFactory, Func<List<string>> keyFactory )
        {
            RememberKeyFactory( keyFactory );

            var item = ( T ) RockCache.Get( qualifiedKey );

            if ( item != null )
            {
                return item;
            }

            item = itemFactory();
            if ( item != null )
            {
                var keys = AllKeys();
                if ( !keys.Any() || !keys.Contains( qualifiedKey ) )
                {
                    UpdateKeys( keyFactory, ensureKey: qualifiedKey );
                }

                RockCache.AddOrUpdate( qualifiedKey, item );
            }
            else
            {
                //This item is gone! Make sure it's not in our key list
                UpdateKeys( keyFactory, removeKey: qualifiedKey );
            }

            return item;
        }

        public static void AddOrUpdate( string qualifiedKey, T item, Func<List<string>> keyFactory )
        {
            if ( item == null )
            {
                return;
            }

            RememberKeyFactory( keyFactory );

            try
            {
                var keys = AllKeys();
                if ( !keys.Any() || !keys.Contains( qualifiedKey ) )
                {
                    UpdateKeys( keyFactory, ensureKey: qualifiedKey );
                }
                RockCache.AddOrUpdate( qualifiedKey, item );
                PublishCacheUpdateMessage( qualifiedKey, item );
            }
            catch ( Exception ex )
            {
                // Log but don't retry synchronously — retrying with Thread.Sleep
                // blocks the request thread while holding the ASP.NET session lock,
                // which causes session queue exhaustion under load.
                Rock.Model.ExceptionLogService.LogException(
                    new Exception( $"Failed to update cache for key {qualifiedKey}: {ex.Message}", ex ) );

                // Best-effort: at minimum get it into local cache
                try
                {
                    RockCache.AddOrUpdate( qualifiedKey, item );
                }
                catch
                {
                    // Swallow — the item will be fetched from DB on next access
                }
            }
        }

        public static void Remove( string qualifiedKey, Func<List<string>> keyFactory )
        {
            RememberKeyFactory( keyFactory );
            try
            {
                RockCache.Remove( qualifiedKey );
                UpdateKeys( keyFactory, removeKey: qualifiedKey );
                PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException(
                    new Exception( $"Failed to remove cache for key {qualifiedKey}: {ex.Message}", ex ) );

                // Best-effort: at minimum evict from local cache
                try
                {
                    RockCache.Remove( qualifiedKey );
                }
                catch
                {
                    // Swallow — the item will be re-evaluated from DB on next access
                }
            }
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            RememberKeyFactory( keyFactory );
            try
            {
                // Hold KeysUpdateLock for the whole clear so a concurrent UpdateKeys
                // cannot rebuild the list from the DB and undo the local clear.
                lock ( KeysUpdateLock )
                {
                    // Get the definitive list of keys from the DB to ensure complete
                    // local cleanup, even if the cached AllKeys list was evicted or stale.
                    var keysToRemove = new HashSet<string>( keyFactory().Select( k => QualifiedKey( k ) ) );

                    // Also include any locally cached keys that might not be in DB
                    // (e.g. recently added but not yet persisted).
                    foreach ( var cachedKey in AllKeys() )
                    {
                        keysToRemove.Add( cachedKey );
                    }

                    // Remove each cached item locally without publishing per-item messages
                    foreach ( var key in keysToRemove )
                    {
                        RockCache.Remove( key );
                    }

                    // Clear the AllKeys list and force the next read to rebuild from DB.
                    RockCache.Remove( AllKey, AllRegion );
                    _lastKeysRefreshUtc = DateTime.MinValue;
                }

                // Publish a single clear-all message instead of N+1
                PublishCacheUpdateMessage( null, default( T ) );
            }
            catch ( Exception ex )
            {
                // Match the other mutators: log and degrade rather than throwing into
                // the caller (e.g. a check-in request). A failed keyFactory()/RockCache
                // call leaves the clear partially applied, which the next refresh heals.
                Rock.Model.ExceptionLogService.LogException(
                    new Exception( $"Failed to clear cache for {typeof( T ).Name}: {ex.Message}", ex ) );
            }
        }

        public static void FlushItem( string qualifiedKey, Func<List<string>> keyFactory )
        {
            RememberKeyFactory( keyFactory );
            try
            {
                RockCache.Remove( qualifiedKey );
                UpdateKeys( keyFactory, removeKey: qualifiedKey );
                PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            }
            catch ( Exception ex )
            {
                Rock.Model.ExceptionLogService.LogException(
                    new Exception( $"Failed to flush cache for key {qualifiedKey}: {ex.Message}", ex ) );

                // Best-effort: at minimum evict from local cache
                try
                {
                    RockCache.Remove( qualifiedKey );
                }
                catch
                {
                    // Swallow — the item will be re-evaluated from DB on next access
                }
            }
        }

        internal protected static string QualifiedKey( int id )
        {
            return QualifiedKey( id.ToString() );
        }

        internal protected static string QualifiedKey( string key )
        {
            return $"{KeyPrefix}:{key}";
        }

        internal protected static string KeyFromQualifiedKey( string qualifiedKey )
        {
            var parts = qualifiedKey.Split( ':' );
            if ( parts.Length > 1 )
            {
                return parts[1];
            }
            return "";
        }

        private static List<string> AllKeys()
        {

            var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
            return keys ?? new List<string>();
        }

        /// <summary>
        /// Adds a key to the AllKeys list in response to a remote (bus) update.
        /// Takes <see cref="KeysUpdateLock"/> so it cannot race the local
        /// <see cref="UpdateKeys"/> read-modify-write and clobber concurrent edits.
        /// If the local list is missing, it is rebuilt from the DB (via the last known
        /// keyFactory) with this key included, so the list does not stay null — leaving
        /// it null would also blind <see cref="ClearLocal"/>. If no keyFactory is known
        /// yet, fall back to invalidation so the next read rebuilds it.
        /// </summary>
        internal static void RegisterRemoteKey( string qualifiedKey )
        {
            if ( string.IsNullOrEmpty( qualifiedKey ) )
            {
                return;
            }

            lock ( KeysUpdateLock )
            {
                var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
                if ( keys == null )
                {
                    var keyFactory = _lastKeyFactory;
                    if ( keyFactory != null )
                    {
                        try
                        {
                            var rebuilt = keyFactory().Select( k => QualifiedKey( k ) ).ToList();
                            if ( !rebuilt.Contains( qualifiedKey ) )
                            {
                                rebuilt.Add( qualifiedKey );
                            }
                            RockCache.AddOrUpdate( AllKey, AllRegion, rebuilt );
                            _lastKeysRefreshUtc = DateTime.UtcNow;
                            return;
                        }
                        catch ( Exception ex )
                        {
                            Rock.Model.ExceptionLogService.LogException(
                                new Exception( $"RegisterRemoteKey rebuild failed for {typeof( T ).Name}: {ex.Message}", ex ) );
                        }
                    }

                    // No keyFactory known (or rebuild failed) — invalidate so the next
                    // AllKeys(keyFactory) rebuilds from DB.
                    RockCache.Remove( AllKey, AllRegion );
                    return;
                }

                if ( !keys.Contains( qualifiedKey ) )
                {
                    // Copy-on-write so any reader still enumerating the old reference is unaffected.
                    var updatedKeys = new List<string>( keys ) { qualifiedKey };
                    RockCache.AddOrUpdate( AllKey, AllRegion, updatedKeys );
                }
            }
        }

        /// <summary>
        /// Removes a key from the AllKeys list in response to a remote (bus) update.
        /// Takes <see cref="KeysUpdateLock"/> to avoid racing local edits.
        /// </summary>
        internal static void UnregisterRemoteKey( string qualifiedKey )
        {
            if ( string.IsNullOrEmpty( qualifiedKey ) )
            {
                return;
            }

            lock ( KeysUpdateLock )
            {
                var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
                if ( keys != null && keys.Contains( qualifiedKey ) )
                {
                    var updatedKeys = new List<string>( keys );
                    updatedKeys.Remove( qualifiedKey );
                    RockCache.AddOrUpdate( AllKey, AllRegion, updatedKeys );
                }
            }
        }

        /// <summary>
        /// Invalidates the AllKeys list in response to a remote (bus) clear-all,
        /// under <see cref="KeysUpdateLock"/>. The next read rebuilds it from the DB.
        /// </summary>
        internal static void InvalidateRemoteKeys()
        {
            lock ( KeysUpdateLock )
            {
                RockCache.Remove( AllKey, AllRegion );
            }
        }

        /// <summary>
        /// Evicts this node's locally cached items for this type in response to a
        /// remote (bus) clear-all, then invalidates the AllKeys list.
        /// Items are stored via RockCache.AddOrUpdate(key, item) — i.e. in
        /// RockCacheManager&lt;object&gt; — so RockCache.ClearCachedItemsForType(T)
        /// (which only clears RockCacheManager&lt;T&gt;) does NOT remove them; we must
        /// remove each key explicitly, mirroring the publisher's local Clear().
        /// </summary>
        internal static void ClearLocal()
        {
            lock ( KeysUpdateLock )
            {
                var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
                if ( keys == null )
                {
                    // The AllKeys list is missing (evicted, or not yet built on a cold node
                    // that has only been fed remote updates). Without it we have no way to
                    // find the per-key item entries, so a clear-all would silently leave them
                    // stale. Fall back to the DB key set via the last keyFactory a local
                    // caller supplied. Best-effort: this evicts DB-current keys; a cached item
                    // whose DB row was already deleted cannot be located without the list.
                    var keyFactory = _lastKeyFactory;
                    if ( keyFactory != null )
                    {
                        try
                        {
                            keys = keyFactory().Select( k => QualifiedKey( k ) ).ToList();
                        }
                        catch ( Exception ex )
                        {
                            Rock.Model.ExceptionLogService.LogException(
                                new Exception( $"ClearLocal key rebuild failed for {typeof( T ).Name}: {ex.Message}", ex ) );
                        }
                    }
                }

                if ( keys != null )
                {
                    foreach ( var key in keys )
                    {
                        RockCache.Remove( key );
                    }
                }

                RockCache.Remove( AllKey, AllRegion );
                _lastKeysRefreshUtc = DateTime.MinValue;
            }
        }

        private static List<string> UpdateKeys( Func<List<string>> keyFactory, string ensureKey = null, string removeKey = null )
        {
            lock ( KeysUpdateLock )
            {
                var now = DateTime.UtcNow;
                var currentKeys = AllKeys();

                // If within throttle window and we have existing keys
                if ( currentKeys.Any() && ( now - _lastKeysRefreshUtc ) < KeysRefreshInterval )
                {
                    bool modified = false;

                    // Copy-on-write: clone the list so any reader still enumerating
                    // the original reference is not affected. NOTE: this protects
                    // readers only — concurrent writers are serialized by KeysUpdateLock,
                    // which all key mutators (local and remote) must hold.
                    var updatedKeys = new List<string>( currentKeys );

                    // Ensure the specified key is present
                    if ( !string.IsNullOrEmpty( ensureKey ) && !updatedKeys.Contains( ensureKey ) )
                    {
                        updatedKeys.Add( ensureKey );
                        modified = true;
                    }

                    // Remove the specified key
                    if ( !string.IsNullOrEmpty( removeKey ) && updatedKeys.Contains( removeKey ) )
                    {
                        updatedKeys.Remove( removeKey );
                        modified = true;
                    }

                    if ( modified )
                    {
                        RockCache.AddOrUpdate( AllKey, AllRegion, updatedKeys );
                    }

                    return updatedKeys;
                }

                var keys = keyFactory().Select( k => QualifiedKey( k ) ).ToList();

                // Apply ensureKey/removeKey operations to refreshed keys                
                if ( !string.IsNullOrEmpty( ensureKey ) && !keys.Contains( ensureKey ) )
                {
                    keys.Add( ensureKey );
                }

                if ( !string.IsNullOrEmpty( removeKey ) && keys.Contains( removeKey ) )
                {
                    keys.Remove( removeKey );
                }

                RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                _lastKeysRefreshUtc = now;
                return keys;
            }
        }

        private static void PublishCacheUpdateMessage( string key, T item )
        {
            // During warm-up, only consume — don't publish.
            // This prevents a recycling node from flooding the bus and
            // causing all other nodes to invalidate their caches.
            if ( !IsReadyToPublish )
            {
                RockLogger.Log.Debug( RockLogDomains.Bus,
                    $"Suppressed cache publish during warm-up for {typeof( T ).Name} key={key ?? "(clear all)"}. Server: {RockMessageBus.NodeName}." );
                return;
            }

            var message = new CheckinCacheMessage
            {
                Key = key,
                Region = null,
                CacheTypeName = typeof( T ).AssemblyQualifiedName,
                SenderNodeName = RockMessageBus.NodeName,
                AdditionalData = item != null ? JsonConvert.SerializeObject( item ) : null
            };

            _ = RockMessageBus.PublishAsync<CacheEventQueue, CheckinCacheMessage>( message );
            RockLogger.Log.Debug( RockLogDomains.Bus, $"Published Cache Update message. {message.ToDebugString()}." );
        }

        /// <summary>
        /// Serializes the cache object to JSON format for compatibility with IItemCache interface
        /// </summary>
        /// <returns>JSON representation of the cache object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject( this );
        }
    }

    /// <summary>
    /// Process-wide warm-up state shared across all <see cref="CheckinCache{T}"/>
    /// closed generic types. The bus-ready instant is recorded once for the whole
    /// app domain so the warm-up window is anchored to node startup — not re-armed
    /// hours later the first time some rarely-used cache type happens to publish.
    /// </summary>
    internal static class CheckinCacheWarmUp
    {
        // 0 = bus not yet observed ready. Stored as UTC ticks for a lock-free
        // compare-and-set.
        private static long _busReadyTicks = 0;

        /// <summary>
        /// Records <paramref name="nowUtc"/> as the bus-ready instant the first
        /// time it is called, and returns the recorded instant on every call.
        /// Because check-in caches are exercised immediately under check-in load,
        /// the first call lands right as the node comes online.
        /// </summary>
        internal static DateTime MarkBusReady( DateTime nowUtc )
        {
            var existing = System.Threading.Interlocked.CompareExchange( ref _busReadyTicks, nowUtc.Ticks, 0 );
            return existing == 0
                ? nowUtc
                : new DateTime( existing, DateTimeKind.Utc );
        }
    }
}
