using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;

namespace org.secc.xAPI.Model
{
    [Table( "_org_secc_xAPI_Experience" )]
    [DataContract]
    public class Experience : Model<Experience>, IRockEntity
    {
        [DataMember]
        [Index]
        public int PersonAliasId { get; set; }

        [LavaInclude]
        public virtual PersonAlias PersonAlias { get; set; }

        [DataMember]
        [Index]
        public int VerbValueId { get; set; }

        [LavaInclude]
        public virtual DefinedValue VerbValue { get; set; }

        [DataMember]
        [Index]
        public int? xObjectId { get; set; }

        [LavaInclude]
        public ExperienceObject xObject { get; set; }

        [DataMember]
        [Index]
        public int? ResultId { get; set; }
    }

    public partial class ExperienceConfiguration : EntityTypeConfiguration<Experience>
    {
        public ExperienceConfiguration()
        {
            this.HasRequired<PersonAlias>( t => t.PersonAlias ).WithMany().HasForeignKey( x => x.PersonAliasId ).WillCascadeOnDelete( false );
            this.HasRequired<DefinedValue>( t => t.VerbValue ).WithMany().HasForeignKey( x => x.VerbValueId ).WillCascadeOnDelete( false );
            this.HasOptional<ExperienceObject>( t => t.xObject ).WithMany().HasForeignKey( x => x.xObjectId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Experiences" );
        }
    }
}
