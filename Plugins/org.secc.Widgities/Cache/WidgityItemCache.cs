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
    public class WidgityItemCache : ModelCache<WidgityItemCache, WidgityItem>
    {
        [DataMember]
        public int WidgityId { get; private set; }

        [DataMember]
        public WidgityCache Widgity => WidgityCache.Get( WidgityId );

        [DataMember]
        public int WidgityTypeId { get; private set; }

        [DataMember]
        public WidgityTypeCache WidgityType => WidgityTypeCache.Get( WidgityTypeId );

        [DataMember]
        public int Order { get; private set; }

        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var widgityItem = entity as WidgityItem;
            if ( widgityItem == null )
                return;

            Id = widgityItem.Id;
            Guid = widgityItem.Guid;
            WidgityId = widgityItem.WidgityId;
            WidgityTypeId = widgityItem.WidgityTypeId;
            Order = widgityItem.Order;
        }

        public WidgityItem GetEntity()
        {
            WidgityItem widgityItem = new WidgityItem()
            {
                Id = Id,
                Guid = Guid,
                WidgityTypeId = WidgityTypeId,
                WidgityType = WidgityType.GetEntity(),
                Order = Order,
                Attributes = Attributes,
                AttributeValues = AttributeValues
            };
            return widgityItem;
        }


        public override string ToString()
        {
            return "Widgity Item of type: " + WidgityType.Name;
        }



    }
}
