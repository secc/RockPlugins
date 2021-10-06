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
        public EntityTypeCache EntityType => EntityTypeCache.Get( EntityTypeId );

        [DataMember]
        public int EntityTypeId { get; set; }

        [DataMember]
        public Guid EntityGuid { get; private set; }

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
                                    .OrderBy( wi => wi.Order )
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
            return GetForEntity( entity.GetType(), entity.Guid );
        }

        public static List<WidgityCache> GetForEntity( Type entityType, Guid entityGuid )
        {
            return GetForEntity( EntityTypeCache.Get( entityType ).Id, entityGuid );
        }

        public static List<WidgityCache> GetForEntity( int EntityTypeId, Guid entityGuid )
        {
            return All()
                .Where( w => w.EntityTypeId == EntityTypeId )
                .Where( w => w.EntityGuid == entityGuid )
                .OrderBy( w => w.Order )
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
            EntityGuid = widgity.EntityGuid;
            EntityTypeId = widgity.EntityTypeId;
            WidgityTypeId = widgity.WidgityTypeId;
            Order = widgity.Order;
        }

        public Widgity GetEntity()
        {
            Widgity widgity = new Widgity()
            {
                Id = Id,
                Guid = Guid,
                EntityGuid = EntityGuid,
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
