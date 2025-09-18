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

namespace org.secc.Equip.Model
{
    /// <summary></summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Equip.Model.ChapterRecord}" />
    [Table( "_org_secc_Equip_ChapterRecord" )]
    [DataContract]
    public class ChapterRecord : Model<ChapterRecord>, IRockEntity
    {
        /// <summary>Gets or sets the course record identifier.</summary>
        /// <value>The course record identifier.</value>
        [DataMember]
        public int CourseRecordId { get; set; }

        /// <summary>Gets or sets the course record.</summary>
        /// <value>The course record.</value>
        [DataMember]
        public virtual CourseRecord CourseRecord { get; set; }

        /// <summary>Gets or sets the completion date time.</summary>
        /// <value>The completion date time.</value>
        [DataMember]
        public DateTime? CompletionDateTime { get; set; }

        /// <summary>Gets or sets the chapter identifier.</summary>
        /// <value>The chapter identifier.</value>
        [DataMember]
        public int ChapterId { get; set; }

        /// <summary>Gets or sets the chapter.</summary>
        /// <value>The chapter.</value>
        [LavaVisible]
        public virtual Chapter Chapter { get; set; }


        /// <summary>Gets or sets a value indicating whether this <see cref="ChapterRecord"/> is passed.</summary>
        /// <value>
        ///   <c>true</c> if passed; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool Passed { get; set; }

        /// <summary>Gets or sets the course page records.</summary>
        /// <value>The course page records.</value>
        [LavaVisible]
        public virtual ICollection<CoursePageRecord> CoursePageRecords { get; set; } = new Collection<CoursePageRecord>();
    }

    /// <summary></summary>
    /// <seealso cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{org.secc.Equip.Model.ChapterRecord}" />
    public partial class ChapterRecordConfiguration : EntityTypeConfiguration<ChapterRecord>
    {
        /// <summary>Initializes a new instance of the <see cref="ChapterRecordConfiguration"/> class.</summary>
        public ChapterRecordConfiguration()
        {
            this.HasEntitySetName( "ChapterRecord" );
        }
    }
}
