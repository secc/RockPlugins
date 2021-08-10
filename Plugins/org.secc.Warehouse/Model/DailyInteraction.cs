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

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace org.secc.Warehouse.Model
{
    [Table("_org_secc_Warehouse_DailyInteraction")]
    [DataContract]
    public class DailyInteraction : Model<DailyInteraction>, IRockEntity
    {
        [DataMember]
        [Index]
        public DateTime Date { get; set; }

        [DataMember]
        public int Visits { get; set; }

        [DataMember]
        [Index]
        public int PageId { get; set; }

        [DataMember]
        public int StaffVisitors { get; set; }

        [DataMember]
        public int MemberVisitors { get; set; }

        [DataMember]
        public int AttendeeVisitors { get; set; }

        [DataMember]
        public int ProspectVisitors { get; set; }

        [DataMember]
        public int AnonymousVisitors { get; set; }
    }

    public partial class DailyInteractionConfiguration : EntityTypeConfiguration<DailyInteraction>
    {
        public DailyInteractionConfiguration()
        {
            this.HasEntitySetName( "DailyInteraction" );
        }
    }
}
