using System;
using Newtonsoft.Json;
using Rock.Bus;
using Rock.Bus.Consumer;
using Rock.Bus.Queue;
using Rock.Logging;

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

            // The sending node already updated its own items and key list before publishing.
            if ( RockMessageBus.IsFromSelf( message ) )
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
            // Key list changes go through CheckinCache<T> so they take its lock and publish a new snapshot.
            // Removing the list instead would race with a local update that is copying the old list.

            // If we have additional data, this is an update
            if ( !string.IsNullOrEmpty( message.AdditionalData ) )
            {
                try
                {
                    var item = JsonConvert.DeserializeObject<T>( message.AdditionalData );
                    if ( item != null )
                    {
                        // Recorded through CheckinCache<T> so a load that read before this update cannot overwrite it.
                        CheckinCache<T>.ApplyRemoteItem( message.Key, message.Region, item );
                        CheckinCache<T>.ApplyRemoteKeyChange( message.Key, null );

                        RockLogger.Log.Debug( RockLogDomains.Bus,
                            $"Updated cache for key {message.Key}. Server: {RockMessageBus.NodeName}. AdditionalData: {message.AdditionalData}" );
                    }
                }
                catch ( Exception ex )
                {
                    RockLogger.Log.Error( RockLogDomains.Bus,
                        $"Error deserializing cache update message for key {message.Key}. {ex.Message} Server: {RockMessageBus.NodeName}." );

                    CheckinCache<T>.ApplyRemoteRemove( message.Key, message.Region );
                    // The item still exists on the sending node; the next Get reloads it locally.
                    CheckinCache<T>.ApplyRemoteKeyChange( message.Key, null );
                }
            }
            else if ( message.Key != null )
            {
                // This is a remove for a specific key
                CheckinCache<T>.ApplyRemoteRemove( message.Key, message.Region );
                CheckinCache<T>.ApplyRemoteKeyChange( null, message.Key );

                RockLogger.Log.Debug( RockLogDomains.Bus, $"Removed cache for key {message.Key}" );
            }
            else
            {
                // This is a clear all for this cache type
                string typeName = typeof( T ).Name;
                CheckinCache<T>.ApplyRemoteClear();
                RockLogger.Log.Debug( RockLogDomains.Bus, $"Cleared all cache for type {typeName}" );
            }
        }

    }
}
