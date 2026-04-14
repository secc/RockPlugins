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

        // Warm-up: suppress publishing until this node is fully online.
        // Anchored to the time this type was first loaded (i.e. app start),
        // NOT to the first publish attempt, so the grace period cannot
        // fire long after the startup burst has already passed.
        // Override via web.config appSettings: <add key="CheckinCacheWarmUpSeconds" value="30" />
        private static readonly DateTime _typeLoadedUtc = DateTime.UtcNow;
        private static readonly TimeSpan WarmUpGracePeriod = TimeSpan.FromSeconds(
            int.TryParse( System.Configuration.ConfigurationManager.AppSettings["CheckinCacheWarmUpSeconds"], out int configuredSeconds )
                ? configuredSeconds
                : 30 );

        public void PostCached()
        {
        }

        /// <summary>
        /// Returns true once Rock is started AND the grace period (measured
        /// from app start) has elapsed. During warm-up the node consumes
        /// messages but does not publish.
        /// </summary>
        private static bool IsReadyToPublish
        {
            get
            {
                if ( !RockMessageBus.IsRockStarted )
                {
                    return false;
                }

                // Grace period is always relative to when the app loaded this type.
                // If the app has been running longer than the grace period,
                // this is true immediately — no delayed suppression.
                return ( DateTime.UtcNow - _typeLoadedUtc ) >= WarmUpGracePeriod;
            }
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
            RockCache.Remove( qualifiedKey );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            UpdateKeys( keyFactory, removeKey: qualifiedKey );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            // Get the definitive list of keys from the DB to ensure complete
            // local cleanup, even if the cached AllKeys list was evicted or stale.
            var keysToRemove = keyFactory().Select( k => QualifiedKey( k ) ).ToList();

            // Also include any locally cached keys that might not be in DB
            // (e.g. recently added but not yet persisted).
            foreach ( var cachedKey in AllKeys() )
            {
                if ( !keysToRemove.Contains( cachedKey ) )
                {
                    keysToRemove.Add( cachedKey );
                }
            }

            // Remove each cached item locally without publishing per-item messages
            foreach ( var key in keysToRemove )
            {
                RockCache.Remove( key );
            }

            // Clear the AllKeys list
            RockCache.Remove( AllKey, AllRegion );

            // Publish a single clear-all message instead of N+1
            PublishCacheUpdateMessage( null, default( T ) );
        }

        public static void FlushItem( string qualifiedKey, Func<List<string>> keyFactory )
        {
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

                    // Copy-on-write: clone the list so threads currently
                    // enumerating the original reference are not affected.
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
}
