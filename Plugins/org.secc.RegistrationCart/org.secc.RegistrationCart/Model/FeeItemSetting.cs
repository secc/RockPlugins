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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>Registration Cart overlay for a Rock fee item: grade-band eligibility. Absent row = everyone.</summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_FeeItemSetting" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.FEE_ITEM_SETTING )]
    public class FeeItemSetting : Model<FeeItemSetting>, IRockEntity
    {
        [DataMember]
        public int RegistrationTemplateFeeItemId { get; set; }

        [DataMember]
        public GradeBand EligibleGradeBands { get; set; } = GradeBand.Everyone;

        [LavaVisible]
        public virtual RegistrationTemplateFeeItem RegistrationTemplateFeeItem { get; set; }
    }

    public partial class FeeItemSettingConfiguration : EntityTypeConfiguration<FeeItemSetting>
    {
        public FeeItemSettingConfiguration()
        {
            this.HasRequired( s => s.RegistrationTemplateFeeItem ).WithMany().HasForeignKey( s => s.RegistrationTemplateFeeItemId ).WillCascadeOnDelete( true );
            this.HasEntitySetName( "RegistrationCartFeeItemSetting" );
        }
    }

    public class FeeItemSettingService : Service<FeeItemSetting>
    {
        public FeeItemSettingService( RockContext context ) : base( context ) { }
    }
}
