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

namespace org.secc.Equip.Model
{
    [Table( "_org_secc_Equip_CourseRequirementStatus" )]
    [DataContract]
    public class CourseRequirementStatus : Model<CourseRequirementStatus>, IRockEntity
    {
        /// <summary>Gets or sets the course identifier.</summary>
        /// <value>The course identifier.</value>
        [DataMember]
        public int CourseRequirementId { get; set; }

        /// <summary>Gets or sets the course.</summary>
        /// <value>The course.</value>
        [LavaInclude]
        public virtual CourseRequirement CourseRequirement { get; set; }

        /// <summary>Gets or sets the person alias identifier.</summary>
        /// <value>The person alias identifier.</value>
        [DataMember]
        public int PersonAliasId { get; set; }

        /// <summary>Gets or sets the person alias.</summary>
        /// <value>The person alias.</value>
        [LavaInclude]
        public PersonAlias PersonAlias { get; set; }

        /// <summary>Gets or sets a value indicating whether this instance is complete.</summary>
        /// <value>
        ///   <c>true</c> if this instance is complete; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool IsComplete { get; set; }

        /// <summary>Gets or sets the valid until.</summary>
        /// <value>The valid until.</value>
        [DataMember]
        public DateTime? ValidUntil { get; set; }

        [LavaInclude]
        public CourseRequirementState State
        {
            get
            {
                if ( IsComplete == false )
                {
                    return CourseRequirementState.Incomplete;
                }
                if ( IsComplete &&
                     ( !ValidUntil.HasValue || ValidUntil >= Rock.RockDateTime.Today ) )
                {
                    return CourseRequirementState.Complete;
                }
                return CourseRequirementState.Expired;
            }

        }
    }

    public enum CourseRequirementState
    {
        Incomplete = 0,
        Complete = 1,
        Expired = 2
    }

    public partial class CourseRequirementStatusConfiguration : EntityTypeConfiguration<CourseRequirementStatus>
    {
        /// <summary>Initializes a new instance of the <see cref="CourseRequirementStatusConfiguration"/> class.</summary>
        public CourseRequirementStatusConfiguration()
        {
            this.HasEntitySetName( "CourseRequirementStatus" );
        }
    }
}
