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
    [Table( "_org_secc_Widgities_WidgityItem" )]
    [DataContract]
    public class WidgityItem : Model<WidgityItem>, IRockEntity, IOrdered
    {
        [DataMember]
        public int WidgityId { get; set; }

        [LavaInclude]
        public virtual Widgity Widgity { get; set; }

        [DataMember]
        public int WidgityTypeId { get; set; }

        [LavaInclude]
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
