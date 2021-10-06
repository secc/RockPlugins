using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Equip.Model.CoursePageRecord}" />
    [Table( "_org_secc_Equip_CoursePageRecord" )]
    [DataContract]
    public class CoursePageRecord : Model<CoursePageRecord>, IRockEntity
    {
        /// <summary>Gets or sets the chapter record identifier.</summary>
        /// <value>The chapter record identifier.</value>
        [DataMember]
        public int ChapterRecordId { get; set; }

        /// <summary>Gets or sets the chapter record.</summary>
        /// <value>The chapter record.</value>
        [LavaInclude]
        public virtual ChapterRecord ChapterRecord { get; set; }

        /// <summary>Gets or sets the course page identifier.</summary>
        /// <value>The course page identifier.</value>
        [DataMember]
        public int CoursePageId { get; set; }

        /// <summary>Gets or sets the course page.</summary>
        /// <value>The course page.</value>
        [LavaInclude]
        public virtual CoursePage CoursePage { get; set; }

        /// <summary>Gets or sets the score.</summary>
        /// <value>The score.</value>
        [DataMember]
        public int Score { get; set; }

        /// <summary>Gets or sets a value indicating whether this <see cref="CoursePageRecord"/> is passed.</summary>
        /// <value>
        ///   <c>true</c> if passed; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool Passed { get; set; }

        /// <summary>Gets or sets the completion details.</summary>
        /// <value>The completion details.</value>
        [DataMember]
        public string CompletionDetails { get; set; }

        /// <summary>Gets or sets the completion date time.</summary>
        /// <value>The completion date time.</value>
        [DataMember]
        public DateTime? CompletionDateTime { get; set; }
    }

    /// <summary></summary>
    /// <seealso cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{org.secc.Equip.Model.CoursePageRecord}" />
    public partial class CoursePageRecordConfiguration : EntityTypeConfiguration<CoursePageRecord>
    {

        /// <summary>Initializes a new instance of the <see cref="CoursePageRecordConfiguration"/> class.</summary>
        public CoursePageRecordConfiguration()
        {
            this.HasEntitySetName( "CoursePageRecord" );
        }
    }
}
