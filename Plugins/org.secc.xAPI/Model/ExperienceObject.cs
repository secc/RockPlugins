using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;

namespace org.secc.xAPI.Model
{
    [Table( "_org_secc_xAPI_ExperienceObject" )]
    [DataContract]
    public class ExperienceObject : Model<ExperienceObject>, IRockEntity
    {
        [DataMember]
        [Index]
        public int EntityTypeId { get; set; }

        [LavaInclude]
        public virtual EntityType EntityType { get; set; }

        [DataMember]
        [MaxLength(200)]
        [Index]
        public string ObjectId { get; set; }
    }

    public partial class ExperienceObjectConfiguration : EntityTypeConfiguration<ExperienceObject>
    {
        public ExperienceObjectConfiguration()
        {
            this.HasRequired<EntityType>( t => t.EntityType ).WithMany().HasForeignKey( x => x.EntityTypeId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "ExperienceObjects" );
        }
    }
}
