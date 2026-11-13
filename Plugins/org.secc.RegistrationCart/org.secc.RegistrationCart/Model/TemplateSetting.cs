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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>
    /// Registration Cart overlay for a Rock RegistrationTemplate: the things Rock doesn't model
    /// (catalog visibility, image, ordering). Absent row = visible, no image, order 0.
    /// </summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_TemplateSetting" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.TEMPLATE_SETTING )]
    public class TemplateSetting : Model<TemplateSetting>, IRockEntity
    {
        [DataMember]
        public int RegistrationTemplateId { get; set; }

        [DataMember]
        public bool IsVisible { get; set; } = true;

        [MaxLength( 500 )]
        [DataMember]
        public string ImageUrl { get; set; }

        [MaxLength( 2000 )]
        [DataMember]
        public string CatalogDescription { get; set; }

        [DataMember]
        public int Order { get; set; }

        [LavaVisible]
        public virtual RegistrationTemplate RegistrationTemplate { get; set; }
    }

    public partial class TemplateSettingConfiguration : EntityTypeConfiguration<TemplateSetting>
    {
        public TemplateSettingConfiguration()
        {
            this.HasRequired( s => s.RegistrationTemplate ).WithMany().HasForeignKey( s => s.RegistrationTemplateId ).WillCascadeOnDelete( true );
            this.HasEntitySetName( "RegistrationCartTemplateSetting" );
        }
    }

    public class TemplateSettingService : Service<TemplateSetting>
    {
        public TemplateSettingService( RockContext context ) : base( context ) { }
    }
}
