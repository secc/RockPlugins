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
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>A discount code applied to a cart, with its terms snapshotted at apply time.</summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_CartDiscount" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CART_DISCOUNT )]
    public class CartDiscount : Model<CartDiscount>, IRockEntity
    {
        [DataMember]
        public int CartId { get; set; }

        [DataMember]
        public int DiscountCodeId { get; set; }

        [DataMember]
        [DecimalPrecision( 18, 2 )]
        public decimal SnapshotValue { get; set; }

        [DataMember]
        public DiscountType SnapshotType { get; set; }

        [DataMember]
        public DiscountScope SnapshotScope { get; set; }

        [DataMember]
        public int? SnapshotRegistrationTemplateId { get; set; }

        [LavaVisible]
        public virtual Cart Cart { get; set; }

        [LavaVisible]
        public virtual DiscountCode DiscountCode { get; set; }
    }

    public partial class CartDiscountConfiguration : EntityTypeConfiguration<CartDiscount>
    {
        public CartDiscountConfiguration()
        {
            this.HasRequired( d => d.Cart ).WithMany( c => c.Discounts ).HasForeignKey( d => d.CartId ).WillCascadeOnDelete( true );
            this.HasRequired( d => d.DiscountCode ).WithMany().HasForeignKey( d => d.DiscountCodeId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartCartDiscount" );
        }
    }

    public class CartDiscountService : Service<CartDiscount>
    {
        public CartDiscountService( RockContext context ) : base( context ) { }
    }
}
