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
using Rock.Model;

namespace org.secc.SafetyAndSecurity.Model
{
    [Table( "_org_secc_SafetyAndSecurity_AlertMessage" )]
    [DataContract]
    public partial class AlertMessage : Rock.Data.Model<AlertMessage>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        public int AlertNotificationId { get; set; }

        [LavaInclude]
        public AlertNotification AlertNotification { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public int CommunicationId { get; set; }

        [LavaInclude]
        public virtual Communication Communication { get; set; }
    }

    public partial class AlertMessageConfiguration : EntityTypeConfiguration<AlertMessage>
    {
        public AlertMessageConfiguration()
        {
            this.HasRequired<Communication>( m => m.Communication ).WithMany().HasForeignKey( m => m.CommunicationId ).WillCascadeOnDelete( false );
            this.HasRequired<AlertNotification>( m => m.AlertNotification ).WithMany().HasForeignKey( m => m.AlertNotificationId ).WillCascadeOnDelete( true );
            this.HasEntitySetName( "AlertMessages" );
        }
    }

}
