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
        /// Processes the cache message using the strongly-typed CheckinCache
        /// </summary>
        private void ProcessCacheMessage<T>( CheckinCacheMessage message ) where T : CheckinCache<T>, new()
        {
            string allKeysListCacheKey = $"{typeof( T ).Name}:All";

            // Handle key change messages (ADD/REMOVE)
            if ( message.Region == AllKeysRegion && ( message.AdditionalData == "ADD" || message.AdditionalData == "REMOVE" ) )
            {
                bool isAdd = message.AdditionalData == "ADD";
                CheckinCache<T>.ApplyKeyChange( message.Key, isAdd );

                RockLogger.Log.Debug( RockLogDomains.Bus,
                    $"Applied key change ({message.AdditionalData}) for key {message.Key}. Server: {RockMessageBus.NodeName}." );
                return;
            }

            // Handle item update messages (has serialized data)
            if ( !string.IsNullOrEmpty( message.AdditionalData ) && message.AdditionalData != "ADD" && message.AdditionalData != "REMOVE" )
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
