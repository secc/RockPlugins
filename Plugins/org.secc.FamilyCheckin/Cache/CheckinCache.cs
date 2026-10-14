using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly TimeSpan KeysRefreshInterval = TimeSpan.FromSeconds( 10 );
        private static readonly Stopwatch Uptime = Stopwatch.StartNew();

        private static readonly CheckinKeyList KeyList = new CheckinKeyList(
            new RockCacheKeyListStore( AllKey, AllRegion ),
            KeysRefreshInterval,
            () => Uptime.Elapsed );

        private static readonly CheckinWriteGuard WriteGuard = new CheckinWriteGuard();

        public void PostCached()
        {
        }

        /// <summary>
        /// Returns the cached key list, loading it if needed. The list is a read-only snapshot that is
        /// never changed after it is published, so it can be enumerated without copying it.
        /// </summary>
        public static IReadOnlyList<string> AllKeys( Func<List<string>> keyFactory )
        {
            return KeyList.GetOrLoad( QualifiedKeys( keyFactory ) );
        }

        public static T Get( string qualifiedKey, Func<T> itemFactory, Func<List<string>> keyFactory )
        {

            var item = ( T ) RockCache.Get( qualifiedKey );

            if ( item != null )
            {
                return item;
            }

            var readStamp = BeginLoad();
            try
            {
                item = itemFactory();
                if ( item != null )
                {
                    return AddLoaded( qualifiedKey, item, readStamp, keyFactory );
                }
            }
            finally
            {
                EndLoad();
            }

            //This item is gone! Make sure it's not in our key list
            KeyList.Remove( QualifiedKeys( keyFactory ), qualifiedKey );
            return item;
        }

        /// <summary>
        /// Starts a load of items from the database. Call before the query runs, pass the result to
        /// <see cref="AddLoaded"/>, and call <see cref="EndLoad"/> when the load is done.
        /// </summary>
        protected static long BeginLoad()
        {
            return WriteGuard.BeginLoad();
        }

        protected static void EndLoad()
        {
            WriteGuard.EndLoad();
        }

        /// <summary>
        /// Caches an item read by a load unless the key was updated or removed after the load began,
        /// and returns the item now in the cache: the loaded one, the newer one, or null if it was removed.
        /// </summary>
        protected static T AddLoaded( string qualifiedKey, T item, long readStamp, Func<List<string>> keyFactory )
        {
            if ( !WriteGuard.TryWriteLoaded( qualifiedKey, readStamp, () => RockCache.AddOrUpdate( qualifiedKey, item ) ) )
            {
                return ( T ) RockCache.Get( qualifiedKey );
            }

            EnsureKey( qualifiedKey, keyFactory );
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

                    EnsureKey( qualifiedKey, keyFactory );
                    //RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
                    WriteGuard.RecordWrite( qualifiedKey, () => RockCache.AddOrUpdate( qualifiedKey, item ) );
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
            WriteGuard.RecordWrite( qualifiedKey, () => RockCache.Remove( qualifiedKey ) );
            PublishCacheUpdateMessage( qualifiedKey, default( T ) );
            KeyList.Remove( QualifiedKeys( keyFactory ), qualifiedKey );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            // Flush every key we know of: the cached list plus whatever the database says exists.
            var keys = new HashSet<string>( KeyList.Current ?? new List<string>() );
            keys.UnionWith( QualifiedKeys( keyFactory )() );

            WriteGuard.RecordClear( () =>
            {
                foreach ( var key in keys )
                {
                    RockCache.Remove( key );
                }
            } );

            // Drop the key list once instead of removing keys one at a time; the next read reloads it.
            // The clear message tells the other nodes to do the same.
            KeyList.Invalidate();
            PublishCacheUpdateMessage( null, default( T ) );
        }
        public static void FlushItem( string qualifiedKey, Func<List<string>> keyFactory )
        {
            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            WriteGuard.RecordWrite( qualifiedKey, () => RockCache.Remove( qualifiedKey ) );
            KeyList.Remove( QualifiedKeys( keyFactory ), qualifiedKey );
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
        /// Applies a key change reported by another node (see <see cref="CheckinCacheConsumer"/>).
        /// </summary>
        internal static void ApplyRemoteKeyChange( string addedKey, string removedKey )
        {
            KeyList.ApplyRemoteChange( addedKey, removedKey );
        }

        /// <summary>
        /// Applies an item update reported by another node (see <see cref="CheckinCacheConsumer"/>).
        /// </summary>
        internal static void ApplyRemoteItem( string qualifiedKey, string region, T item )
        {
            WriteGuard.RecordWrite( qualifiedKey, () =>
            {
                if ( region != null )
                {
                    RockCache.AddOrUpdate( qualifiedKey, region, item );
                }
                else
                {
                    RockCache.AddOrUpdate( qualifiedKey, item );
                }
            } );
        }

        /// <summary>
        /// Applies an item removal reported by another node (see <see cref="CheckinCacheConsumer"/>).
        /// </summary>
        internal static void ApplyRemoteRemove( string qualifiedKey, string region )
        {
            WriteGuard.RecordWrite( qualifiedKey, () =>
            {
                if ( region != null )
                {
                    RockCache.Remove( qualifiedKey, region );
                }
                else
                {
                    RockCache.Remove( qualifiedKey );
                }
            } );
        }

        /// <summary>
        /// Applies a clear reported by another node (see <see cref="CheckinCacheConsumer"/>).
        /// </summary>
        internal static void ApplyRemoteClear()
        {
            WriteGuard.RecordClear( () => RockCache.ClearCachedItemsForType( typeof( T ) ) );
            KeyList.Invalidate();
        }

        private static void EnsureKey( string qualifiedKey, Func<List<string>> keyFactory )
        {
            var keys = KeyList.Current;
            if ( keys == null || !keys.Contains( qualifiedKey ) )
            {
                KeyList.Ensure( QualifiedKeys( keyFactory ), qualifiedKey );
            }
        }

        private static Func<IEnumerable<string>> QualifiedKeys( Func<List<string>> keyFactory )
        {
            return () => keyFactory().Select( k => QualifiedKey( k ) );
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

    /// <summary>
    /// Keeps a <see cref="CheckinKeyList"/> in RockCache. RockCache is in-process and stores the
    /// object by reference, so readers share the published snapshot.
    /// </summary>
    internal sealed class RockCacheKeyListStore : IKeyListStore
    {
        private readonly string _key;
        private readonly string _region;

        public RockCacheKeyListStore( string key, string region )
        {
            _key = key;
            _region = region;
        }

        public IReadOnlyList<string> Get()
        {
            return RockCache.Get( _key, _region ) as IReadOnlyList<string>;
        }

        public void Set( IReadOnlyList<string> keys )
        {
            RockCache.AddOrUpdate( _key, _region, keys );
        }

        public void Remove()
        {
            RockCache.Remove( _key, _region );
        }
    }
}
