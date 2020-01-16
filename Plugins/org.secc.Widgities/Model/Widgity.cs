using System;
using System.Collections.Generic;
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
    [Table( "_org_secc_Widgity_Widgity" )]
    [DataContract]
    public class Widgity : Model<Widgity>, IRockEntity, IOrdered
    {
        [DataMember]
        public int WidgityTypeId { get; set; }

        [LavaInclude]
        public virtual WidgityType WidgityType { get; set; }

        [DataMember]
        public int EntityId { get; set; }

        [LavaInclude]
        public virtual ICollection<WidgityItem> WidgityItems { get; set; }

        [DataMember]
        public int Order { get ; set ; }
    }

    public partial class WidgityConfiguration : EntityTypeConfiguration<Widgity>
    {
        public WidgityConfiguration()
        {
            this.HasEntitySetName( "Widgity" );
        }
    }
}
