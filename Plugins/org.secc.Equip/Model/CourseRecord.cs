using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Equip.Model.CourseRecord}" />
    [Table( "_org_secc_Equip_CourseRecord" )]
    [DataContract]
    public class CourseRecord : Model<CourseRecord>, IRockEntity
    {
        /// <summary>Gets or sets the person alias identifier.</summary>
        /// <value>The person alias identifier.</value>
        [DataMember]
        public int PersonAliasId { get; set; }

        /// <summary>Gets or sets the person alias.</summary>
        /// <value>The person alias.</value>
        [LavaVisible]
        public virtual PersonAlias PersonAlias { get; set; }

        /// <summary>Gets or sets the course identifier.</summary>
        /// <value>The course identifier.</value>
        [DataMember]
        public int CourseId { get; set; }

        /// <summary>Gets or sets the course.</summary>
        /// <value>The course.</value>
        [LavaVisible]
        public virtual Course Course { get; set; }

        /// <summary>Gets or sets the completion date time.</summary>
        /// <value>The completion date time.</value>
        [DataMember]
        public DateTime? CompletionDateTime { get; set; }

        /// <summary>Gets or sets a value indicating whether this <see cref="CourseRecord"/> is passed.</summary>
        /// <value>
        ///   <c>true</c> if passed; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool Passed { get; set; }

        /// <summary>Gets or sets the chapter records.</summary>
        /// <value>The chapter records.</value>
        [LavaVisible]
        public virtual ICollection<ChapterRecord> ChapterRecords { get; set; } = new Collection<ChapterRecord>();
    }

    public partial class CourseRecordConfiguration : EntityTypeConfiguration<CourseRecord>
    {
        /// <summary>Initializes a new instance of the <see cref="CourseRecordConfiguration"/> class.</summary>
        public CourseRecordConfiguration()
        {
            this.HasEntitySetName( "CourseRecord" );
        }
    }
}
