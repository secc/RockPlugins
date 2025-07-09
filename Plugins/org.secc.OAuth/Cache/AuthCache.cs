using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rock.Bus;
using Rock.Bus.Queue;
using Rock.Logging;
using Rock.Web.Cache;

namespace org.secc.OAuth.Cache
{
    [DataContract]
    [Serializable]
    public class AuthCache : IItemCache
    {
        private const string AllRegion = "AllItems";
        protected static readonly string AllString = "All";

        private static readonly string KeyPrefix = $"{typeof(AuthCache).Name}";

        private static string AllKey => $"{KeyPrefix}:{AllString}";

        

        [DataMember]
        public string Token { get; set; }

        [DataMember]
        public string Ticket { get; set; }

        public void PostCached()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Ticket;
        }

        //public static List<string> AllKeys(Func<List<string>> keyFactory, bool forceRefresh = false)
        //{
        //    var keys = AllKeys();
        //    if (!keys.Any() || forceRefresh)
        //    {
        //        keys = UpdateKeys(keyFactory);
        //    }
        //    return keys;
        //}

        public static AuthCache Get(string qualifiedKey)
        {
            var item = (AuthCache)RockCache.Get(qualifiedKey);
            if (item != null)
            {
                return item;
            }
            else
            {
                var keys = AllKeys();
                keys.Remove(qualifiedKey);
            }

            return item;
        }

        public static void AddOrUpdate(string qualifiedKey, AuthCache item )
        {
            if (item == null)
            {
                return;
            }

            var keys = AllKeys();
            if (!keys.Any() || !keys.Contains(qualifiedKey))
            {
                keys.Add(qualifiedKey);
                UpdateKeys(keys);
            }            //RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
            RockCache.AddOrUpdate(qualifiedKey, item);
            PublishCacheUpdateMessage(qualifiedKey, item);
        }

        public static void Remove(string qualifiedKey)
        {            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            RockCache.Remove(qualifiedKey);
            PublishCacheUpdateMessage(qualifiedKey, default(AuthCache));
            var keys = AllKeys();
            keys.Remove(qualifiedKey);
            UpdateKeys(keys);
        }

        public static void Clear()
        {
            //UpdateKeys(keyFactory);

            foreach (var key in AllKeys())
            {
                FlushItem(key);
            }
            UpdateKeys(new List<string>());
            PublishCacheUpdateMessage(null, default(AuthCache));
        }
        public static void FlushItem(string qualifiedKey)
        {
            //RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            RockCache.Remove(qualifiedKey);
            PublishCacheUpdateMessage(qualifiedKey, default(AuthCache));
        }

        internal protected static string QualifiedKey(int id)
        {
            return QualifiedKey(id.ToString());
        }

        internal protected static string QualifiedKey(string key)
        {
            return $"{KeyPrefix}:{key}";
        }

        internal protected static string KeyFromQualifiedKey(string qualifiedKey)
        {
            var parts = qualifiedKey.Split(':');
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return "";
        }

        private static List<string> AllKeys()
        {
            var keys = RockCache.Get(AllKey, AllRegion) as List<string>;
            return keys ?? new List<string>();
        }

        private static List<string> UpdateKeys(List<string> keys)
        {
            RockCache.AddOrUpdate(AllKey, AllRegion, keys);
            return keys;
        }

        private static void PublishCacheUpdateMessage(string qualifiedKey, AuthCache item)
        {
            var message = new AuthCacheMessage
            {
                CacheTypeName = typeof(AuthCache).AssemblyQualifiedName,
                Key = qualifiedKey,
                Region = AllRegion,
                AdditionalData = JsonConvert.SerializeObject(item),
                SenderNodeName = RockMessageBus.NodeName
            };
            _ = RockMessageBus.PublishAsync<CacheEventQueue, AuthCacheMessage>(message);
            RockLogger.Log.Debug(RockLogDomains.Bus, $"Published Cache Update message. {message.ToDebugString()}.");
        }



        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
