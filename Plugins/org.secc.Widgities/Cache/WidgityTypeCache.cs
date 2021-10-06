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
    public class WidgityTypeCache : ModelCache<WidgityTypeCache, WidgityType>
    {
        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public bool IsSystem { get; private set; }

        [DataMember]
        public string Icon { get; private set; }

        [DataMember]
        public bool HasItems { get; private set; }

        [DataMember]
        public string EnabledLavaCommands { get; private set; }

        [DataMember]
        public string Description { get; private set; }

        [DataMember]
        public string Markdown { get; private set; }

        [DataMember]
        public int? CategoryId { get; private set; }

        [DataMember]
        public CategoryCache Category => CategoryId.HasValue ? CategoryCache.Get( CategoryId.Value ) : null;

        [DataMember]
        public List<int> EntityTypeIds { get; private set; }

        [DataMember]
        public List<EntityTypeCache> EntityTypes
        {
            get
            {
                var entityTypes = new List<EntityTypeCache>();
                foreach ( var id in EntityTypeIds )
                {
                    entityTypes.Add( EntityTypeCache.Get( id ) );
                }
                return entityTypes;
            }
        }

        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            var widgityType = entity as WidgityType;
            if ( widgityType == null )
                return;

            Id = widgityType.Id;
            Name = widgityType.Name;
            Guid = widgityType.Guid;
            Description = widgityType.Description;
            EnabledLavaCommands = widgityType.EnabledLavaCommands;
            EntityTypeIds = widgityType.EntityTypes.Select( e => e.Id ).ToList();
            Markdown = widgityType.Markdown;
            HasItems = widgityType.HasItems;
            CategoryId = widgityType.CategoryId;
            IsSystem = widgityType.IsSystem;
            Icon = widgityType.Icon;
        }

        internal WidgityType GetEntity()
        {
            WidgityType widgityType = new WidgityType
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Guid = Guid,
                EnabledLavaCommands = EnabledLavaCommands,
                EntityTypes = EntityTypes.Select( e => e.GetEntityType() ) as ICollection<Rock.Model.EntityType>,
                Markdown = Markdown,
                HasItems = HasItems,
                CategoryId = CategoryId,
                IsSystem = IsSystem,
                Icon = Icon,
                Attributes = Attributes,
                AttributeValues = AttributeValues
            };

            return widgityType;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
