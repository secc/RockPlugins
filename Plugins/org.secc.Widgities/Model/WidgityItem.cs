using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;

namespace org.secc.Widgities.Model
{
    [Table( "_org_secc_Widgities_WidgityItem" )]
    [DataContract]
    public class WidgityItem : Model<WidgityItem>, IRockEntity, IOrdered
    {
        [DataMember]
        public int WidgityId { get; set; }

        [LavaVisible]
        public virtual Widgity Widgity { get; set; }

        [DataMember]
        public int WidgityTypeId { get; set; }

        [LavaVisible]
        public WidgityType WidgityType { get; set; }

        [DataMember]
        public int Order { get; set; }
    }

    public partial class WidgityItemConfiguration : EntityTypeConfiguration<WidgityItem>
    {
        public WidgityItemConfiguration()
        {
            this.HasEntitySetName( "WidgityItems" );
        }
    }
}
