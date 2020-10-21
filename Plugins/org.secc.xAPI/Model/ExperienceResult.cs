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
    [Table( "_org_secc_xAPI_ExperienceResult" )]
    [DataContract]
    public class ExperienceResult: Model<ExperienceResult>, IRockEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is complete.
        /// This is equivalent to the xAPI Result Completion property
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        [Index]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether was successful.
        /// This is equivalent to the xAPI Result Success property
        /// </summary>
        /// <value>
        ///   <c>true</c> if was successful; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        [Index]
        public bool WasSuccess { get; set; }
    }

    public partial class ExperienceResultConfiguration : EntityTypeConfiguration<ExperienceResult>
    {
        public ExperienceResultConfiguration()
        {
            this.HasEntitySetName( "ExperienceResults" );
        }
    }
}
