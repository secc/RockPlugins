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
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>
    /// A family member placed in the cart. Name and grade band are snapshotted from the
    /// Rock Person when added so pricing/eligibility stay stable while the cart is open.
    /// </summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_CartRegistrant" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CART_REGISTRANT )]
    public class CartRegistrant : Model<CartRegistrant>, IRockEntity
    {
        [DataMember]
        public int CartId { get; set; }

        [DataMember]
        public int PersonAliasId { get; set; }

        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string FirstName { get; set; }

        [Required]
        [MaxLength( 100 )]
        [DataMember( IsRequired = true )]
        public string LastName { get; set; }

        [DataMember]
        public GradeBand GradeBand { get; set; }

        [LavaVisible]
        public virtual Cart Cart { get; set; }

        [LavaVisible]
        public virtual PersonAlias PersonAlias { get; set; }

        [NotMapped]
        public string DisplayName => ( FirstName + " " + LastName ).Trim();
    }

    public partial class CartRegistrantConfiguration : EntityTypeConfiguration<CartRegistrant>
    {
        public CartRegistrantConfiguration()
        {
            this.HasRequired( r => r.Cart ).WithMany( c => c.Registrants ).HasForeignKey( r => r.CartId ).WillCascadeOnDelete( true );
            this.HasRequired( r => r.PersonAlias ).WithMany().HasForeignKey( r => r.PersonAliasId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartCartRegistrant" );
        }
    }

    public class CartRegistrantService : Service<CartRegistrant>
    {
        public CartRegistrantService( RockContext context ) : base( context ) { }
    }
}
