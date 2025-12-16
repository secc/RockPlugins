using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        private static readonly object _keysLock = new object();

        // Background refresh settings
        private static readonly TimeSpan KeysRefreshInterval = TimeSpan.FromSeconds( 10 );
        private static DateTime _lastKeysRefresh = DateTime.MinValue;
        private static int _isRefreshing = 0; // 0 = false, 1 = true (for Interlocked operations)

        public void PostCached()
        {
        }

        public static List<string> AllKeys( Func<List<string>> keyFactory, bool forceRefresh = false )
        {
            var keys = GetCachedKeys();
            var now = DateTime.UtcNow;
            
            DateTime lastKeysRefresh;
            lock ( _keysLock )
            {
                lastKeysRefresh = _lastKeysRefresh;
            }
            
            var needsRefresh = forceRefresh
                || !keys.Any()
                || ( now - lastKeysRefresh ) > KeysRefreshInterval;

            if ( needsRefresh )
            {
                // If cache is empty, we must refresh synchronously
                if ( !keys.Any() )
                {
                    keys = RefreshKeysFromDatabase( keyFactory );
                }
                else
                {
                    // Cache has data - trigger background refresh and return stale data
                    // Note: During the background refresh window, the returned data may be slightly
                    // stale but this is intentional for performance. Local key changes via
                    // AddKeyToCache/RemoveKeyFromCache will update the cached list immediately.
                    TriggerBackgroundRefresh( keyFactory );
                }
            }

            return keys;
        }

        /// <summary>
        /// Triggers a background refresh of the keys list if one isn't already in progress.
        /// Uses Interlocked.CompareExchange for thread-safe flag checking.
        /// Note: Background tasks may be abruptly terminated during application shutdown,
        /// but the _isRefreshing flag will be reset on next application start.
        /// </summary>
        private static void TriggerBackgroundRefresh( Func<List<string>> keyFactory )
        {
            // Atomically set _isRefreshing to 1 (true) only if it was 0 (false)
            // CompareExchange returns the original value - if it was already 1, return early
            // as another thread is already handling the refresh
            if ( Interlocked.CompareExchange( ref _isRefreshing, 1, 0 ) == 1 )
            {
                return;
            }

            ThreadPool.QueueUserWorkItem( _ =>
            {
                try
                {
                    RefreshKeysFromDatabase( keyFactory );
                }
                catch ( Exception ex )
                {
                    Rock.Model.ExceptionLogService.LogException(
                        new Exception( $"Background cache refresh failed for {typeof( T ).Name}", ex ) );
                }
                finally
                {
                    _isRefreshing = 0;
                }
            } );
        }

        /// <summary>
        /// Refreshes the keys list from the database and updates the cache.
        /// </summary>
        private static List<string> RefreshKeysFromDatabase( Func<List<string>> keyFactory )
        {
            lock ( _keysLock )
            {
                var keys = keyFactory().Select( k => QualifiedKey( k ) ).ToList();
                RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                _lastKeysRefresh = DateTime.UtcNow;
                return keys;
            }
        }

        /// <summary>
        /// Gets the currently cached keys without triggering a refresh.
        /// Returns a defensive copy to prevent concurrent modification issues.
        /// </summary>
        private static List<string> GetCachedKeys()
        {
            var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
            return keys != null ? keys.ToList() : new List<string>();
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
                AddKeyToCache( qualifiedKey );
                RockCache.AddOrUpdate( qualifiedKey, item );
            }
            // Note: When itemFactory returns null, we do not remove the key from cache
            // as this might be due to a transient condition (e.g., database connection issue)
            // rather than the item actually being deleted from the database.

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
                    AddKeyToCache( qualifiedKey );
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
            RemoveKeyFromCache( qualifiedKey );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            List<string> keysSnapshot;
            lock ( _keysLock )
            {
                keysSnapshot = GetCachedKeys().ToList();
                // Clear the keys list immediately while holding the lock
                RockCache.AddOrUpdate( AllKey, AllRegion, new List<string>() );
                _lastKeysRefresh = DateTime.MinValue;
            }

            // Now remove individual items outside the lock (this may take time)
            foreach ( var key in keysSnapshot )
            {
                FlushItem( key );
            }

            PublishCacheUpdateMessage( null, default( T ) );
        }

        public static void FlushItem( string qualifiedKey )
        {
            RockCache.Remove( qualifiedKey );
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

        /// <summary>
        /// Adds a key to the cached keys list and notifies other servers.
        /// </summary>
        private static void AddKeyToCache( string qualifiedKey )
        {
            lock ( _keysLock )
            {
                var keys = GetCachedKeys();
                if ( !keys.Contains( qualifiedKey ) )
                {
                    keys.Add( qualifiedKey );
                    RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                    PublishKeyChangeMessage( qualifiedKey, isAdd: true );
                }
            }
        }

        /// <summary>
        /// Removes a key from the cached keys list and notifies other servers.
        /// </summary>
        private static void RemoveKeyFromCache( string qualifiedKey )
        {
            lock ( _keysLock )
            {
                var keys = GetCachedKeys();
                if ( keys.Contains( qualifiedKey ) )
                {
                    keys.Remove( qualifiedKey );
                    RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                    PublishKeyChangeMessage( qualifiedKey, isAdd: false );
                }
            }
        }

        /// <summary>
        /// Applies a key change received from another server via message bus.
        /// </summary>
        public static void ApplyKeyChange( string qualifiedKey, bool isAdd )
        {
            lock ( _keysLock )
            {
                var keys = GetCachedKeys();
                if ( isAdd && !keys.Contains( qualifiedKey ) )
                {
                    keys.Add( qualifiedKey );
                    RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                }
                else if ( !isAdd && keys.Contains( qualifiedKey ) )
                {
                    keys.Remove( qualifiedKey );
                    RockCache.AddOrUpdate( AllKey, AllRegion, keys );
                }
            }
        }

        /// <summary>
        /// Forces a refresh of the keys list from the database on next access.
        /// </summary>
        public static void InvalidateKeysCache()
        {
            lock ( _keysLock )
            {
                _lastKeysRefresh = DateTime.MinValue;
            }
        }

        private static void PublishKeyChangeMessage( string qualifiedKey, bool isAdd )
        {
            var message = new CheckinCacheMessage
            {
                Key = qualifiedKey,
                Region = AllRegion,
                CacheTypeName = typeof( T ).AssemblyQualifiedName,
                SenderNodeName = RockMessageBus.NodeName,
                AdditionalData = isAdd ? "ADD" : "REMOVE"
            };

            _ = RockMessageBus.PublishAsync<CacheEventQueue, CheckinCacheMessage>( message );
            RockLogger.Log.Debug( RockLogDomains.Bus, $"Published Key Change message. {message.ToDebugString()}." );
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
