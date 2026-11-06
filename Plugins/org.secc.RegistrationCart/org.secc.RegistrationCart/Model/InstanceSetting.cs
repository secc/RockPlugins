// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>
    /// Registration Cart overlay for a Rock RegistrationInstance: who may register (grade bands)
    /// and whether it shows in the cart. Absent row = visible, everyone eligible.
    /// </summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_InstanceSetting" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.INSTANCE_SETTING )]
    public class InstanceSetting : Model<InstanceSetting>, IRockEntity
    {
        [DataMember]
        public int RegistrationInstanceId { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [DataMember]
        public GradeBand EligibleGradeBands { get; set; } = GradeBand.Everyone;

        [DataMember]
        public int Order { get; set; }

        /// <summary>
        /// When the instance is not linked to a calendar event, staff enter the event dates
        /// here. When set, these win over the linked event's schedule.
        /// </summary>
        [DataMember]
        public DateTime? EventStartDate { get; set; }

        [DataMember]
        public DateTime? EventEndDate { get; set; }

        [LavaVisible]
        public virtual RegistrationInstance RegistrationInstance { get; set; }
    }

    public partial class InstanceSettingConfiguration : EntityTypeConfiguration<InstanceSetting>
    {
        public InstanceSettingConfiguration()
        {
            this.HasRequired( s => s.RegistrationInstance ).WithMany().HasForeignKey( s => s.RegistrationInstanceId ).WillCascadeOnDelete( true );
            this.HasEntitySetName( "RegistrationCartInstanceSetting" );
        }
    }

    public class InstanceSettingService : Service<InstanceSetting>
    {
        public InstanceSettingService( RockContext context ) : base( context ) { }
    }
}
