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
using Rock.Model;
using Rock.Security;

namespace org.secc.Equip.Model
{
    [Table( "_org_secc_Equip_CourseRequirement" )]
    [DataContract]
    public class CourseRequirement : Model<CourseRequirement>, IRockEntity, ISecured
    {
        /// <summary>Gets or sets the course identifier.</summary>
        /// <value>The course identifier.</value>
        [DataMember]
        public int CourseId { get; set; }

        /// <summary>Gets or sets the course.</summary>
        /// <value>The course.</value>
        [LavaVisible]
        public virtual Course Course { get; set; }

        /// <summary>Gets or sets the group identifier.</summary>
        /// <value>The group identifier.</value>
        [DataMember]
        public int? GroupId { get; set; }

        /// <summary>Gets or sets the group.</summary>
        /// <value>The group.</value>
        [LavaVisible]
        public virtual Group Group { get; set; }

        /// <summary>Gets or sets the data view identifier.</summary>
        /// <value>The data view identifier.</value>
        [DataMember]
        public int? DataViewId { get; set; }

        /// <summary>Gets or sets the data view.</summary>
        /// <value>The data view.</value>
        [LavaVisible]
        public virtual DataView DataView { get; set; }

        /// <summary>Gets or sets the days valid.</summary>
        /// <value>The days valid.</value>
        [DataMember]
        public int? DaysValid { get; set; }

        [LavaVisible]
        public virtual ICollection<CourseRequirementStatus> CourseRequirementStatuses { get; set; }

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

    public partial class CourseRequirementConfiguration : EntityTypeConfiguration<CourseRequirement>
    {
        /// <summary>Initializes a new instance of the <see cref="CourseRequirementConfiguration"/> class.</summary>
        public CourseRequirementConfiguration()
        {
            this.HasEntitySetName( "CourseRequirement" );
        }
    }
}
