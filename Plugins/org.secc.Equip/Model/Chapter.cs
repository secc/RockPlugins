using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Lava;
using Rock.Security;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Equip.Model.Chapter}" />
    [Table( "_org_secc_Equip_Chapter" )]
    [DataContract]
    public class Chapter : Model<Chapter>, IRockEntity, IOrdered, ISecured
    {
        /// <summary>Converts to string.</summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        [DataMember]
        public string Name { get; set; }

        /// <summary>Gets or sets the description.</summary>
        /// <value>The description.</value>
        [DataMember]
        public string Description { get; set; }

        /// <summary>Gets or sets the course pages.</summary>
        /// <value>The course pages.</value>
        [LavaVisible]
        public virtual ICollection<CoursePage> CoursePages { get; set; }

        [DataMember]
        public int CourseId { get; set; }

        [LavaVisible]
        public virtual Course Course { get; set; }

        [DataMember]
        public int Order { get; set; }

        [NotMapped]
        public override ISecured ParentAuthority
        {
            get
            {
                if ( this.Course != null )
                {
                    return this.Course;
                }
                else
                {
                    return new GlobalDefault();
                }
            }
        }

    }

    /// <summary></summary>
    /// <seealso cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{org.secc.Equip.Model.Chapter}" />
    public partial class ChapterConfiguration : EntityTypeConfiguration<Chapter>
    {
        /// <summary>Initializes a new instance of the <see cref="ChapterConfiguration"/> class.</summary>
        public ChapterConfiguration()
        {
            this.HasEntitySetName( "Chapter" );
        }
    }
}
