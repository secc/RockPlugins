using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using org.secc.Widgities.Model;
using Rock.Data;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.Widgities.Cache
{
    [Serializable]
    [DataContract]
    public class WidgityCache : ModelCache<WidgityCache, Widgity>
    {
        private readonly object _obj = new object();

        [DataMember]
        public int WidgityTypeId { get; private set; }

        [DataMember]
        public WidgityTypeCache WidgityType => WidgityTypeCache.Get( WidgityTypeId );

        [DataMember]
        public int EntityId { get; private set; }

        [DataMember]
        public List<WidgityItemCache> WidgityItems
        {
            get
            {
                var widgityItems = new List<WidgityItemCache>();

                if ( _widgityItemIds == null )
                {
                    lock ( _obj )
                    {
                        if ( _widgityItemIds == null )
                        {
                            using ( var rockContext = new RockContext() )
                            {
                                _widgityItemIds = new WidgityItemService( rockContext )
                                    .Queryable()
                                    .Where( wi => wi.WidgityId == Id )
                                    .OrderBy( wi => wi.Id )
                                    .Select( wi => wi.Id )
                                    .ToList();
                            }
                        }
                    }
                }

                foreach ( var id in _widgityItemIds )
                {
                    var widgityItem = WidgityItemCache.Get( id );
                    if ( widgityItem != null )
                    {
                        widgityItems.Add( widgityItem );
                    }
                }

                return widgityItems;
            }
        }

        private List<int> _widgityItemIds = null;

        [DataMember]
        public int Order { get; private set; }

        public static List<WidgityCache> GetForEntity( IEntity entity )
        {
            return GetForEntity( entity.GetType(), entity.Id );
        }

        public static List<WidgityCache> GetForEntity( Type entityType, int EntityId )
        {
            return GetForEntity( EntityTypeCache.Get( entityType ).Id, EntityId );
        }

        public static List<WidgityCache> GetForEntity( int EntityTypeId, int EntityId )
        {
            return All()
                .Where( w => w.WidgityType.EntityTypeId == EntityTypeId )
                .Where( w => w.EntityId == EntityId )
                .OrderBy( w => w.Id )
                .ToList();
        }

        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var widgity = entity as Widgity;
            if ( widgity == null )
                return;

            Id = widgity.Id;
            Guid = widgity.Guid;
            EntityId = widgity.EntityId;
            WidgityTypeId = widgity.WidgityTypeId; 
            Order = widgity.Order;
        }

        public Widgity GetEntity()
        {
            Widgity widgity = new Widgity()
            {
                Id = Id,
                Guid = Guid,
                EntityId = EntityId,
                WidgityTypeId = WidgityTypeId,
                WidgityType = WidgityType.GetEntity(),
                Order = Order,
                Attributes = Attributes,
                AttributeValues = AttributeValues
            };
            return widgity;
        }

        public override string ToString()
        {
            return "Widgity of type: " + WidgityType.Name;
        }



    }
}
