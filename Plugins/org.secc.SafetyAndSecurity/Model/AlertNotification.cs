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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace org.secc.SafetyAndSecurity.Model
{
    [Table( "_org_secc_SafetyAndSecurity_AlertNotification" )]
    [DataContract]
    public partial class AlertNotification : Rock.Data.Model<AlertNotification>, IHasActiveFlag, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public int AudienceValueId { get; set; }

        [LavaVisible]
        public virtual DefinedValue AudienceValue { get; set; }

        [DataMember]
        public int AlertNotificationTypeValueId { get; set; }

        [LavaVisible]
        public virtual DefinedValue AlertNotificationTypeValue { get; set; }

        [LavaVisible]
        public virtual ICollection<AlertMessage> AlertMessages { get; set; }
    }

    public partial class AlertNotificationConfiguration : EntityTypeConfiguration<AlertNotification>
    {
        public AlertNotificationConfiguration()
        {
            this.HasRequired( a => a.AudienceValue ).WithMany().HasForeignKey( a => a.AudienceValueId ).WillCascadeOnDelete( false );
            this.HasRequired( a => a.AlertNotificationTypeValue ).WithMany().HasForeignKey( a => a.AlertNotificationTypeValueId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "AlertNotifications" );
        }
    }

}
