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

namespace org.secc.xAPI.Model
{
    [Table( "_org_secc_xAPI_ExperienceQualifier" )]
    [DataContract]
    public class ExperienceQualifier: Model<ExperienceQualifier>, IRockEntity
    {
        [DataMember]
        [Index]
        public int ParentId { get; set; }

        [DataMember]
        [Index]
        public int EntityTypeId { get; set; }

        [LavaInclude]
        public EntityType EntityType { get; set; }

        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public string Value { get; set; }
    }

    public partial class ExperienceQualifierConfiguration : EntityTypeConfiguration<ExperienceQualifier>
    {
        public ExperienceQualifierConfiguration()
        {
            this.HasRequired<EntityType>( t => t.EntityType ).WithMany().HasForeignKey( x => x.EntityTypeId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Objects" );
        }
    }
}
