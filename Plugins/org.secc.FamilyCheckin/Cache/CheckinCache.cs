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
        // Whether the key list this node last wrote to the cache was empty. Tracks every write, not just
        // full rebuilds, so it always describes what the cache should currently hold. See UpdateKeys.
        private static bool _lastWrittenKeysWereEmpty;
        private static readonly TimeSpan KeysRefreshInterval = TimeSpan.FromSeconds( 10 );

        /// <summary>
        /// Called when this node's key list is dropped because of a change published by another node
        /// (see <see cref="CheckinCacheConsumer"/>). The remote change means this node no longer knows
        /// what the list contains, so forget that it was last written empty: the next read of the missing
        /// entry is then treated as an eviction and rebuilds, instead of being trusted and throttled.
        /// </summary>
        internal static void InvalidateKeysState()
        {
            lock ( KeysUpdateLock )
            {
                _lastWrittenKeysWereEmpty = false;
            }
        }

        public void PostCached()
        {
        }

        public static List<string> AllKeys( Func<List<string>> keyFactory )
        {
            var keys = AllKeys();
            if ( !keys.Any() )
            {
                keys = UpdateKeys( keyFactory );
            }
            return keys;
        }

        public static T Get( string qualifiedKey, Func<T> itemFactory, Func<List<string>> keyFactory )
        {

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
                // PublishCacheUpdateMessage( qualifiedKey, item );
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

            int retryCount = 3;
            int retryDelayMs = 100;
            Exception lastException = null;

            while ( retryCount > 0 )
            {
                try
                {

                    var keys = AllKeys();
                    if ( !keys.Any() || !keys.Contains( qualifiedKey ) )
                    {
                        UpdateKeys( keyFactory, ensureKey: qualifiedKey );
                    }            //RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
                    RockCache.AddOrUpdate( qualifiedKey, item );
                    PublishCacheUpdateMessage( qualifiedKey, item );
                    return;
                }
                catch ( Exception ex )
                {
                    lastException = ex;
                    retryCount--;

                    if ( retryCount > 0 )
                    {
                        Rock.Model.ExceptionLogService.LogException(
                            new Exception( $"Retrying cache update for {qualifiedKey}: {ex.Message}", ex ) );
                        System.Threading.Thread.Sleep( retryDelayMs );
                        retryDelayMs *= 2; // Exponential backoff
                    }
                }
            }

            // If we get here, all retries failed
            if ( lastException != null )
            {
                Rock.Model.ExceptionLogService.LogException(
                    new Exception( $"Failed to update cache after multiple attempts for key {qualifiedKey}", lastException ) );
            }
        }

        public static void Remove( string qualifiedKey, Func<List<string>> keyFactory )
        {
            RockCache.Remove( qualifiedKey );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            UpdateKeys( keyFactory, removeKey: qualifiedKey );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            // Force the rebuild: a throttled refresh here would flush against a stale -- or empty --
            // key list and silently leave cached items behind.
            UpdateKeys( keyFactory, forceRefresh: true );

            // Create a copy of the keys to avoid collection modification during enumeration
            foreach ( var key in AllKeys().ToList() )
            {
                FlushItem( key, keyFactory );
            }
            PublishCacheUpdateMessage( null, default( T ) );
        }
        public static void FlushItem( string qualifiedKey, Func<List<string>> keyFactory )
        {
            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            RockCache.Remove( qualifiedKey );
            UpdateKeys( keyFactory, removeKey: qualifiedKey );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
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

        private static List<string> UpdateKeys( Func<List<string>> keyFactory, string ensureKey = null, string removeKey = null, bool forceRefresh = false )
        {
            lock ( KeysUpdateLock )
            {
                var now = DateTime.UtcNow;
                var currentKeys = AllKeys();

                // AllKeys() returns an empty list for two very different situations and cannot tell them
                // apart: keyFactory() legitimately having nothing to return (overnight, before the day's
                // first check-in), and the RockCache entry having been evicted. The throttle timestamp is
                // a process-local field, so it outlives the cache entry it guards -- which means an
                // evicted entry would otherwise be served as "empty" for the rest of the throttle window,
                // handing every caller zero keys and blanking the check-in monitor until the window rolls.
                //
                // Tracking whether the list this node last wrote was empty separates the two: an empty
                // cache read that contradicts a non-empty last write means the entry went away, so rebuild
                // now. A genuinely empty list stays throttled, which is what keeps keyFactory() -- a
                // full scan of Attendance in the AttendanceCache case -- from running on every call.
                //
                // The flag has to describe what was written, not what keyFactory() returned: an empty
                // rebuild followed by a throttled ensureKey leaves a non-empty list in the cache, and if
                // that entry is then evicted the read must rebuild, not be trusted as "still empty".
                var cacheEntryLooksEvicted = !currentKeys.Any() && !_lastWrittenKeysWereEmpty;

                if ( !forceRefresh
                    && !cacheEntryLooksEvicted
                    && ( now - _lastKeysRefreshUtc ) < KeysRefreshInterval )
                {
                    bool modified = false;

                    // Ensure the specified key is present
                    if ( !string.IsNullOrEmpty( ensureKey ) && !currentKeys.Contains( ensureKey ) )
                    {
                        currentKeys.Add( ensureKey );
                        modified = true;
                    }

                    // Remove the specified key
                    if ( !string.IsNullOrEmpty( removeKey ) && currentKeys.Contains( removeKey ) )
                    {
                        currentKeys.Remove( removeKey );
                        modified = true;
                    }

                    if ( modified )
                    {
                        RockCache.AddOrUpdate( AllKey, AllRegion, currentKeys );
                        _lastWrittenKeysWereEmpty = !currentKeys.Any();
                    }

                    return currentKeys;
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

                // Record what was written, after ensure/remove, so the next empty cache read can be
                // classified as "still nothing" or "entry went away".
                _lastWrittenKeysWereEmpty = !keys.Any();
                _lastKeysRefreshUtc = now;
                return keys;
            }
        }

        private static void PublishCacheUpdateMessage( string key, T item )
        {
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
}
