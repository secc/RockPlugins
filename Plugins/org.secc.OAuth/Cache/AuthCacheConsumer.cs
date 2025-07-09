using Newtonsoft.Json;
using Rock.Bus;
using Rock.Bus.Consumer;
using Rock.Bus.Queue;
using Rock.Logging;
using Rock.Web.Cache;
using System;

namespace org.secc.OAuth.Cache
{
    public class AuthCacheConsumer : RockConsumer<CacheEventQueue, AuthCacheMessage>
    {
        public override void Consume(AuthCacheMessage message)
        {
            if (message == null)
            {
                return;
            }
            try
            {
                var cacheType = Type.GetType(message.CacheTypeName);
                if (cacheType == null)
                {
                    RockLogger.Log.Error(RockLogDomains.Bus, $"Could not resolve cache type {message.CacheTypeName} Server: {RockMessageBus.NodeName}.");
                    return;
                }

                ProcessCacheMessage(message);
            }
            catch (Exception ex)
            {
                RockLogger.Log.Error(RockLogDomains.Bus, $"Error processing cache message. {ex.Message} Server: {RockMessageBus.NodeName}.");
            }
        }

        private void ProcessCacheMessage(AuthCacheMessage message)
        {
            string allKeysListCacheKey = $"{ typeof(AuthCache).Name}:All";
            string allKeysListCacheRegion = "AllItems";

            if (!string.IsNullOrEmpty(message.AdditionalData))
            {
                try
                {
                    var item = JsonConvert.DeserializeObject<AuthCache>(message.AdditionalData);

                    if(item != null)
                    {
                        if(message.Region != null)
                        {
                            RockCache.AddOrUpdate(message.Key, message.Region, item);
                        }
                        else
                        {
                            RockCache.AddOrUpdate(message.Key, item);
                        }
                        RockCache.Remove(allKeysListCacheKey, allKeysListCacheRegion);

                        RockLogger.Log.Debug(RockLogDomains.Bus,
                            $"Updated cache for key {message.Key}. Server: {RockMessageBus.NodeName}. AdditionalData: {message.AdditionalData}");
                    }
                }
                catch(Exception ex)
                {
                    RockLogger.Log.Error(RockLogDomains.Bus,
                        $"Error deserializing cache update message for key {message.Key}. {ex.Message} Server: {RockMessageBus.NodeName}.");

                    if (message.Region != null)
                    {
                        RockCache.Remove(message.Key, message.Region);
                    }
                    else
                    {
                        RockCache.Remove(message.Key);
                    }
                    // invalidate AllKeys on error
                    RockCache.Remove(allKeysListCacheKey, allKeysListCacheRegion);
                }
            }
            else if (message.Key != null)
            {
                // This is a remove for a specific key
                if (message.Region != null)
                {
                    RockCache.Remove(message.Key, message.Region);
                }
                else
                {
                    RockCache.Remove(message.Key);
                }
                // Invalidate the AllKeys list as an item was removed
                RockCache.Remove(allKeysListCacheKey, allKeysListCacheRegion);

                RockLogger.Log.Debug(RockLogDomains.Bus, $"Removed cache for key {message.Key}");
            }
            else
            {
                // This is a clear all for this cache type
                string typeName = typeof(AuthCache).Name;
                RockCache.ClearCachedItemsForType(typeof(AuthCache));
                // Clear/invalidate the AllKeys list specifically
                RockCache.Remove(allKeysListCacheKey, allKeysListCacheRegion);
                RockLogger.Log.Debug(RockLogDomains.Bus, $"Cleared all cache for type {typeName}");
            }

        }
    }

}
