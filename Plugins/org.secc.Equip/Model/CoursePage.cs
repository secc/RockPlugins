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
using Rock.Security;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Equip.Model.CoursePage}" />
    [Table( "_org_secc_Equip_CoursePage" )]
    [DataContract]
    public class CoursePage : Model<CoursePage>, IRockEntity, IOrdered, ISecured
    {
        public override string ToString()
        {
            return Name;
        }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int ChapterId { get; set; }

        [LavaInclude]
        public virtual Chapter Chapter { get; set; }

        /// <summary>Gets or sets the passing score.</summary>
        /// <value>The passing score.</value>
        [DataMember]
        public int? PassingScore { get; set; }

        /// <summary>Gets or sets the configuration.</summary>
        /// <value>The configuration.</value>
        [DataMember]
        public string Configuration { get; set; }

        /// <summary>Gets or sets the entity type identifier.</summary>
        /// <value>The entity type identifier.</value>
        [DataMember]
        [LavaIgnore]
        public int EntityTypeId { get; set; }

        /// <summary>Gets or sets the type of the entity.</summary>
        /// <value>The type of the entity.</value>
        public virtual EntityType EntityType { get; set; }

        [DataMember]
        public int Order { get; set; }

        [NotMapped]
        public override ISecured ParentAuthority
        {
            get
            {
                if ( this.Chapter != null )
                {
                    return this.Chapter;
                }
                else
                {
                    return new GlobalDefault();
                }
            }
        }
    }
    public partial class CoursePageConfiguration : EntityTypeConfiguration<CoursePage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoursePageConfiguration"/> class.
        /// </summary>
        public CoursePageConfiguration()
        {
            this.HasEntitySetName( "CoursePage" );
        }
    }
}
