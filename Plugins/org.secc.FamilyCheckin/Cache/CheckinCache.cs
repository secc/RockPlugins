using System;
using System.Collections.Generic;
using System.Linq;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    public class CheckinCache<T>
    {
        private const string AllRegion = "AllItems";
        protected static readonly string AllString = "All";

        private static readonly string KeyPrefix = $"{typeof( T ).Name}";

        private static string AllKey => $"{typeof( T ).Name}:{AllString}";



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
            var item = RockCacheManager<T>.Instance.Get( qualifiedKey );

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

                RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
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
            }

            RockCacheManager<T>.Instance.Cache.AddOrUpdate( qualifiedKey, item, v => item );
        }

        public static void Remove( string qualifiedKey, Func<List<string>> keyFactory )
        {
            RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
            UpdateKeys( keyFactory );
        }

        public static void Clear( Func<List<string>> keyFactory )
        {
            UpdateKeys( keyFactory );

            foreach ( var key in AllKeys() )
            {
                FlushItem( key );
            }
        }

        public static void FlushItem( string qualifiedKey )
        {
            RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey );
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
            var keys = RockCacheManager<List<string>>.Instance.Cache.Get( AllKey, AllRegion );
            return keys ?? new List<string>();
        }

        private static List<string> UpdateKeys( Func<List<string>> keyFactory )
        {
            var keys = keyFactory().Select( k => QualifiedKey( k ) ).ToList();
            RockCacheManager<List<string>>.Instance.Cache.AddOrUpdate( AllKey, AllRegion, keys, v => keys );
            return keys;
        }
    }
}
