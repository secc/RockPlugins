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

            // Skip messages this node published — the local cache was already
            // updated before the message was sent, so processing it again is
            // redundant and doubles the work.
            if ( RockMessageBus.IsFromSelf( message ) )
            {
                RockLogger.Log.Debug( RockLogDomains.Bus,
                    $"Skipping self-sent CheckinCache message for key {message.Key}. Server: {RockMessageBus.NodeName}." );
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
                // method.Invoke wraps any exception from ProcessCacheMessage in a
                // TargetInvocationException whose own Message is the useless generic
                // "Exception has been thrown by the target of an invocation." Unwrap it
                // so the real failure (and its stack) is what gets logged.
                var actual = ( ex as System.Reflection.TargetInvocationException )?.InnerException ?? ex;
                RockLogger.Log.Error( RockLogDomains.Bus, actual, $"Error processing cache message. {actual.Message} Server: {RockMessageBus.NodeName}." );
            }
        }

        /// <summary>
        /// Processes the cache message using the strongly-typed RockCacheManager
        /// </summary>
        private void ProcessCacheMessage<T>( CheckinCacheMessage message )
        {
            // If we have additional data, this is an update — apply the value directly
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

                        // Maintain the AllKeys list under the shared KeysUpdateLock so we
                        // cannot lose updates racing the local UpdateKeys read-modify-write.
                        CheckinCache<T>.RegisterRemoteKey( message.Key );

                        RockLogger.Log.Debug( RockLogDomains.Bus,
                            $"Updated cache for key {message.Key}. Server: {RockMessageBus.NodeName}." );
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
                    // Only invalidate AllKeys on deserialization error
                    CheckinCache<T>.InvalidateRemoteKeys();
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

                // Drop the key from AllKeys under the shared lock.
                CheckinCache<T>.UnregisterRemoteKey( message.Key );

                RockLogger.Log.Debug( RockLogDomains.Bus, $"Removed cache for key {message.Key}. Server: {RockMessageBus.NodeName}." );
            }
            else
            {
                // This is a clear all for this cache type.
                // ClearLocal removes the per-key items (stored in RockCacheManager<object>)
                // AND invalidates the AllKeys list. RockCache.ClearCachedItemsForType only
                // clears RockCacheManager<T>, which does not hold these items, so it would
                // leave every cached item stale on this node.
                string typeName = typeof( T ).Name;
                CheckinCache<T>.ClearLocal();
                RockLogger.Log.Debug( RockLogDomains.Bus, $"Cleared all cache for type {typeName}. Server: {RockMessageBus.NodeName}." );
            }
        }

    }
}
