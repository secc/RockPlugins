using System;
using Newtonsoft.Json;
using Rock.Bus;
using Rock.Bus.Consumer;
using Rock.Bus.Queue;
using Rock.Logging;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Consumer for CheckinCache messages
    /// </summary>
    public class CheckinCacheConsumer : RockConsumer<CacheEventQueue, CheckinCacheMessage>
    {
        private const string AllKeysRegion = "AllItems";
        
        // Cache validation results to avoid repeated reflection overhead
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> _typeValidationCache 
            = new System.Collections.Concurrent.ConcurrentDictionary<Type, bool>();

        public override void Consume( CheckinCacheMessage message )
        {
            if ( message == null )
            {
                return;
            }

            // Ignore messages from this server
            if ( message.SenderNodeName == RockMessageBus.NodeName )
            {
                return;
            }

            try
            {
                var cacheType = Type.GetType( message.CacheTypeName );
                if ( cacheType == null )
                {
                    RockLogger.Log.Error( RockLogDomains.Bus, $"Could not resolve cache type {message.CacheTypeName} Server: {RockMessageBus.NodeName}." );
                    return;
                }

                // Validate that the cacheType satisfies the generic constraint (CheckinCache<T>)
                // Use cached validation result to avoid repeated reflection overhead
                if ( !_typeValidationCache.GetOrAdd( cacheType, IsValidCheckinCacheType ) )
                {
                    RockLogger.Log.Error( RockLogDomains.Bus, $"Cache type {message.CacheTypeName} does not satisfy CheckinCache<T> constraint. Server: {RockMessageBus.NodeName}." );
                    return;
                }

                var method = typeof( CheckinCacheConsumer ).GetMethod( nameof( ProcessCacheMessage ),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
                    .MakeGenericMethod( cacheType );

                method.Invoke( this, new object[] { message } );
            }
            catch ( Exception ex )
            {
                RockLogger.Log.Error( RockLogDomains.Bus, $"Error processing cache message. {ex.Message} Server: {RockMessageBus.NodeName}." );
            }
        }

        /// <summary>
        /// Validates that a type inherits from CheckinCache&lt;T&gt; and has a parameterless constructor
        /// </summary>
        private static bool IsValidCheckinCacheType( Type type )
        {
            if ( type == null )
            {
                return false;
            }

            // Check if type has a parameterless constructor
            var constructor = type.GetConstructor( Type.EmptyTypes );
            if ( constructor == null )
            {
                return false;
            }

            // Check if type inherits from CheckinCache<T> where T is the same type
            // (self-referencing generic constraint, e.g., MyCache : CheckinCache<MyCache>)
            var baseType = type.BaseType;
            while ( baseType != null )
            {
                if ( baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof( CheckinCache<> ) )
                {
                    var genericArg = baseType.GetGenericArguments()[0];
                    return genericArg == type;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Processes the cache message using the strongly-typed CheckinCache
        /// </summary>
        private void ProcessCacheMessage<T>( CheckinCacheMessage message ) where T : CheckinCache<T>, new()
        {
            string allKeysListCacheKey = $"{typeof( T ).Name}:All";

            // Handle key change messages (ADD/REMOVE) - use case-insensitive comparison
            if ( message.Region == AllKeysRegion && 
                 ( string.Equals( message.AdditionalData, "ADD", StringComparison.OrdinalIgnoreCase ) || 
                   string.Equals( message.AdditionalData, "REMOVE", StringComparison.OrdinalIgnoreCase ) ) )
            {
                bool isAdd = string.Equals( message.AdditionalData, "ADD", StringComparison.OrdinalIgnoreCase );
                CheckinCache<T>.ApplyKeyChange( message.Key, isAdd );

                RockLogger.Log.Debug( RockLogDomains.Bus,
                    $"Applied key change ({message.AdditionalData}) for key {message.Key}. Server: {RockMessageBus.NodeName}." );
                return;
            }

            // Handle item update messages (has serialized data)
            // Use case-insensitive comparison to avoid mismatched casing issues
            if ( !string.IsNullOrEmpty( message.AdditionalData ) && 
                 !string.Equals( message.AdditionalData, "ADD", StringComparison.OrdinalIgnoreCase ) && 
                 !string.Equals( message.AdditionalData, "REMOVE", StringComparison.OrdinalIgnoreCase ) )
            {
                try
                {
                    var item = JsonConvert.DeserializeObject<T>( message.AdditionalData );
                    if ( item != null )
                    {
                        if ( message.Region != null )
                        {
                            RockCache.AddOrUpdate( message.Key, message.Region, item );
                        }
                        else
                        {
                            RockCache.AddOrUpdate( message.Key, item );
                        }

                        RockLogger.Log.Debug( RockLogDomains.Bus,
                            $"Updated cache for key {message.Key}. Server: {RockMessageBus.NodeName}." );
                    }
                }
                catch ( Exception ex )
                {
                    RockLogger.Log.Error( RockLogDomains.Bus,
                        $"Error deserializing cache update message for key {message.Key}. {ex.Message} Server: {RockMessageBus.NodeName}." );

                    // Remove the bad item
                    if ( message.Region != null )
                    {
                        RockCache.Remove( message.Key, message.Region );
                    }
                    else
                    {
                        RockCache.Remove( message.Key );
                    }
                }
                return;
            }

            // Handle item removal (key specified but no additional data)
            if ( message.Key != null )
            {
                if ( message.Region != null )
                {
                    RockCache.Remove( message.Key, message.Region );
                }
                else
                {
                    RockCache.Remove( message.Key );
                }

                RockLogger.Log.Debug( RockLogDomains.Bus, $"Removed cache for key {message.Key}. Server: {RockMessageBus.NodeName}." );
                return;
            }

            // Handle clear all (no key specified)
            string typeName = typeof( T ).Name;
            RockCache.ClearCachedItemsForType( typeof( T ) );
            RockCache.Remove( allKeysListCacheKey, AllKeysRegion );
            CheckinCache<T>.InvalidateKeysCache();

            RockLogger.Log.Debug( RockLogDomains.Bus, $"Cleared all cache for type {typeName}. Server: {RockMessageBus.NodeName}." );
        }
    }
}
