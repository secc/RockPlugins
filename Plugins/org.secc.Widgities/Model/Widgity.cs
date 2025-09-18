using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace org.secc.Widgities.Model
{
    [Table( "_org_secc_Widgities_Widgity" )]
    [DataContract]
    public class Widgity : Model<Widgity>, IRockEntity, IOrdered
    {
        [DataMember]
        public int WidgityTypeId { get; set; }

        [LavaVisible]
        public virtual WidgityType WidgityType { get; set; }

        [DataMember]
        public int EntityTypeId { get; set; }

        [LavaVisible]
        public virtual EntityType EntityType { get; set; }

        [DataMember]
        public Guid EntityGuid { get; set; }

        [LavaVisible]
        public virtual ICollection<WidgityItem> WidgityItems { get; set; }

        [DataMember]
        public int Order { get; set; }
    }

    public partial class WidgityConfiguration : EntityTypeConfiguration<Widgity>
    {
        public WidgityConfiguration()
        {
            this.HasEntitySetName( "Widgity" );
        }
    }
}
