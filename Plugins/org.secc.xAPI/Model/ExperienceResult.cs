// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace org.secc.xAPI.Model
{
    [Table( "_org_secc_xAPI_ExperienceResult" )]
    [DataContract]
    public class ExperienceResult : QualifiableModel<ExperienceResult>, IRockEntity
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
