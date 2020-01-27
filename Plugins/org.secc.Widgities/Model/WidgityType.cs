using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;

namespace org.secc.Widgities.Model
{
    [Table( "_org_secc_Widgities_WidgityType" )]
    [DataContract]
    public class WidgityType : Model<WidgityType>, IRockEntity, ICategorized
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember]
        public string Icon { get; set; }

        [DataMember]
        public bool HasItems { get; set; }

        [DataMember]
        [MaxLength( 500 )]
        public string EnabledLavaCommands { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Markdown { get; set; }

        [DataMember]
        public int? CategoryId { get; set; }

        [LavaInclude]
        public virtual Category Category { get; set; }

        [LavaInclude]
        public virtual ICollection<EntityType> EntityTypes { get; set; }
    }

    public partial class WidgityTypeConfiguration : EntityTypeConfiguration<WidgityType>
    {
        public WidgityTypeConfiguration()
        {
            this.HasMany( k => k.EntityTypes ).WithMany()
                .Map( t => { t.MapLeftKey( "WidgityTypeId" ); t.MapRightKey( "EntityTypeId" ); t.ToTable( "_org_secc_Widgities_WidgityTypeEntityType" ); } );
            this.HasEntitySetName( "WidgityItemTypes" );
        }
    }
}
