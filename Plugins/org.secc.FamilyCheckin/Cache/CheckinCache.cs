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

        public void PostCached()
        {
        }

        public static List<string> AllKeys( Func<List<string>> keyFactory, bool forceRefresh = false )
        {
            var keys = AllKeys();
            if ( !keys.Any() || forceRefresh )
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
                    UpdateKeys( keyFactory );
                }

                RockCache.AddOrUpdate( qualifiedKey, item );
                PublishCacheUpdateMessage( qualifiedKey, item );
            }
            else
            {
                //This item is gone! Make sure it's not in our key list
                UpdateKeys( keyFactory );
            }

            return item;
        }

        public static void AddOrUpdate( string qualifiedKey, T item, Func<List<string>> keyFactory )
        {
            if ( item == null )
            {
                return;
            }

            var keys = AllKeys();
            if ( !keys.Any() || !keys.Contains( qualifiedKey ) )
            {
                UpdateKeys( keyFactory );
            }            //RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
            RockCache.AddOrUpdate( qualifiedKey, item );
            PublishCacheUpdateMessage( qualifiedKey, item );
        }

        public static void Remove( string qualifiedKey, Func<List<string>> keyFactory )
        {            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            RockCache.Remove( qualifiedKey );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            UpdateKeys( keyFactory );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            UpdateKeys( keyFactory );

            foreach ( var key in AllKeys() )
            {
                FlushItem( key );
            }
            PublishCacheUpdateMessage( null, default( T ) );
        }
        public static void FlushItem( string qualifiedKey )
        {
            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
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

        private static List<string> AllKeys()
        {

            var keys = RockCache.Get( AllKey, AllRegion ) as List<string>;
            return keys ?? new List<string>();
        }

        private static List<string> UpdateKeys( Func<List<string>> keyFactory )
        {
            var keys = keyFactory().Select( k => QualifiedKey( k ) ).ToList();
            RockCache.AddOrUpdate( AllKey, AllRegion, keys );
            return keys;
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
