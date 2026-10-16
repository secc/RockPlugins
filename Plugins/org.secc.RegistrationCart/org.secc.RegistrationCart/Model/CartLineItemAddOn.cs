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
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_CartLineItemAddOn" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CART_LINE_ITEM_ADD_ON )]
    public class CartLineItemAddOn : Model<CartLineItemAddOn>, IRockEntity
    {
        [DataMember]
        public int CartLineItemId { get; set; }

        [DataMember]
        public int RegistrationTemplateFeeItemId { get; set; }

        /// <summary>Fee item name at add time (fee items can be renamed later).</summary>
        [MaxLength( 200 )]
        [DataMember]
        public string SnapshotName { get; set; }

        [DataMember]
        [DecimalPrecision( 18, 2 )]
        public decimal SnapshotPrice { get; set; }

        [LavaVisible]
        public virtual CartLineItem CartLineItem { get; set; }

        [LavaVisible]
        public virtual RegistrationTemplateFeeItem RegistrationTemplateFeeItem { get; set; }
    }

    public partial class CartLineItemAddOnConfiguration : EntityTypeConfiguration<CartLineItemAddOn>
    {
        public CartLineItemAddOnConfiguration()
        {
            this.HasRequired( a => a.CartLineItem ).WithMany( l => l.AddOns ).HasForeignKey( a => a.CartLineItemId ).WillCascadeOnDelete( true );
            this.HasRequired( a => a.RegistrationTemplateFeeItem ).WithMany().HasForeignKey( a => a.RegistrationTemplateFeeItemId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartCartLineItemAddOn" );
        }
    }

    public class CartLineItemAddOnService : Service<CartLineItemAddOn>
    {
        public CartLineItemAddOnService( RockContext context ) : base( context ) { }
    }
}
