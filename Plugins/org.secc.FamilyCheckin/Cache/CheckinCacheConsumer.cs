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
        public override void Consume( CheckinCacheMessage message )
        {
            if ( message == null )
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

                // Get the generic method to invoke the appropriate RockCacheManager
                var method = typeof( CheckinCacheConsumer ).GetMethod( nameof( ProcessCacheMessage ),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
                    .MakeGenericMethod( cacheType );

                // Invoke the method with this message
                method.Invoke( this, new object[] { message } );
            }
            catch ( Exception ex )
            {
                RockLogger.Log.Error( RockLogDomains.Bus, $"Error processing cache message. {ex.Message} Server: {RockMessageBus.NodeName}." );
            }
        }

        /// <summary>
        /// Processes the cache message using the strongly-typed RockCacheManager
        /// </summary>
        private void ProcessCacheMessage<T>( CheckinCacheMessage message )
        {
            // Constants for the AllKeys list, matching CheckinCache<T>
            string allKeysListCacheKey = $"{typeof( T ).Name}:All";
            string allKeysListCacheRegion = "AllItems"; // This is the 'AllRegion' constant from CheckinCache<T>

            // If we have additional data, this is an update
            if ( !string.IsNullOrEmpty( message.AdditionalData ) )
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

                        // Invalidate the AllKeys list as an item was added/updated
                        RockCache.Remove( allKeysListCacheKey, allKeysListCacheRegion );

                        RockLogger.Log.Debug( RockLogDomains.Bus,
                            $"Updated cache for key {message.Key}. Server: {RockMessageBus.NodeName}. AdditionalData: {message.AdditionalData}" );
                    }
                }
                catch ( Exception ex )
                {
                    RockLogger.Log.Error( RockLogDomains.Bus,
                        $"Error deserializing cache update message for key {message.Key}. {ex.Message} Server: {RockMessageBus.NodeName}." );

                    if ( message.Region != null )
                    {
                        RockCache.Remove( message.Key, message.Region );
                    }
                    else
                    {
                        RockCache.Remove( message.Key );
                    }
                    // invalidate AllKeys on error
                    RockCache.Remove( allKeysListCacheKey, allKeysListCacheRegion );
                }
            }
            else if ( message.Key != null )
            {
                // This is a remove for a specific key
                if ( message.Region != null )
                {
                    RockCache.Remove( message.Key, message.Region );
                }
                else
                {
                    RockCache.Remove( message.Key );
                }
                // Invalidate the AllKeys list as an item was removed
                RockCache.Remove( allKeysListCacheKey, allKeysListCacheRegion );

                RockLogger.Log.Debug( RockLogDomains.Bus, $"Removed cache for key {message.Key}" );
            }
            else
            {
                // This is a clear all for this cache type
                string typeName = typeof( T ).Name;
                RockCache.ClearCachedItemsForType( typeof( T ) );
                // Clear/invalidate the AllKeys list specifically
                RockCache.Remove( allKeysListCacheKey, allKeysListCacheRegion );
                RockLogger.Log.Debug( RockLogDomains.Bus, $"Cleared all cache for type {typeName}" );
            }
        }

    }
}
