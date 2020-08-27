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

namespace org.secc.SystemsMonitor.Model
{
    [Table( "_org_secc_SystemsMonitor_SystemTestHistory" )]
    [DataContract]
    public class SystemTestHistory : Model<SystemTestHistory>, IRockEntity
    {
        [DataMember]
        public int SystemTestId { get; set; }

        [LavaInclude]
        public virtual SystemTest SystemTest { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public bool Passed { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    public partial class SytemTestHistoryConfiguration : EntityTypeConfiguration<SystemTestHistory>
    {
        public SytemTestHistoryConfiguration()
        {
            this.HasRequired<SystemTest>( t => t.SystemTest ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "SystemTestHistory" );
        }
    }
}
