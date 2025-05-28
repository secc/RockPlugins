using System;
using Rock.Bus.Consumer;
using Rock.Bus.Message;
using Rock.Bus.Queue;
using Rock.Logging;
using Newtonsoft.Json;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    /// <summary>
    /// Consumer for CheckinCache messages
    /// </summary>
    public class CheckinCacheConsumer : RockConsumer<CacheEventQueue, CheckinCacheMessage>
    {
        /// <summary>
        /// Consumes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void Consume(CheckinCacheMessage message)
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
                    RockLogger.Log.Error(RockLogDomains.Bus, $"Could not resolve cache type {message.CacheTypeName}");
                    return;
                }

                // If we have additional data, this is an update
                if (!string.IsNullOrEmpty(message.AdditionalData))
                {
                    try
                    {
                        var item = JsonConvert.DeserializeObject(message.AdditionalData, cacheType);
                        if (item != null)
                        {
                            RockCache.AddOrUpdate(message.Key, message.Region, item);
                            RockLogger.Log.Debug(RockLogDomains.Bus, $"Updated cache for key {message.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        RockLogger.Log.Error(RockLogDomains.Bus, $"Error deserializing cache update message for key {message.Key}. {ex.Message}");
                        RockCache.Remove(message.Key, message.Region);
                    }
                }
                else if (message.Key != null)
                {
                    // This is a remove for a specific key
                    RockCache.Remove(message.Key, message.Region);
                    RockLogger.Log.Debug(RockLogDomains.Bus, $"Removed cache for key {message.Key}");
                }
                else
                {
                    // This is a clear all for this cache type
                    var keys = RockCache.Get($"{cacheType.Name}:All", message.Region) as System.Collections.Generic.List<string>;
                    if (keys != null)
                    {
                        foreach (var key in keys)
                        {
                            RockCache.Remove(key, message.Region);
                        }
                    }
                    RockCache.Remove($"{cacheType.Name}:All", message.Region);
                    RockLogger.Log.Debug(RockLogDomains.Bus, $"Cleared all cache for type {cacheType.Name}");
                }
            }
            catch (Exception ex)
            {
                RockLogger.Log.Error(RockLogDomains.Bus, $"Error processing cache message. {ex.Message}");
            }
        }
    }
}
