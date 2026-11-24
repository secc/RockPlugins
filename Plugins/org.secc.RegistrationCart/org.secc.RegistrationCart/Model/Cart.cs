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
using System.Collections.Generic;
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
    /// One shopping cart per logged-in person. The cart row IS the idempotency record:
    /// checkout moves it Open → Processing before charging, so only one request can charge
    /// it, and once Status is CheckedOut a retry cannot charge again.
    /// </summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_Cart" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CART )]
    public class Cart : Model<Cart>, IRockEntity
    {
        [DataMember]
        public int OwnerPersonAliasId { get; set; }

        [DataMember]
        public CartStatus Status { get; set; } = CartStatus.Open;

        [DataMember]
        public DateTime? CheckedOutDateTime { get; set; }

        /// <summary>
        /// The Rock payment, saved as soon as the gateway has charged the card (before the
        /// registrations), so a charge always has its record in Rock.
        /// </summary>
        [DataMember]
        public int? FinancialTransactionId { get; set; }

        /// <summary>Last checkout failure, kept for support/audit. Cleared on success.</summary>
        [MaxLength( 2000 )]
        [DataMember]
        public string LastCheckoutError { get; set; }

        /// <summary>
        /// When the claimed cart's charge was sent to the gateway. A cart still Processing without
        /// it never reached the gateway, so nothing was charged and it can be reopened.
        /// </summary>
        [DataMember]
        public DateTime? PaymentSentDateTime { get; set; }

        /// <summary>When staff closed this unconfirmed checkout (settings page), having checked the gateway.</summary>
        [DataMember]
        public DateTime? ResolvedDateTime { get; set; }

        [DataMember]
        public int? ResolvedByPersonAliasId { get; set; }

        /// <summary>What staff found and did when they closed it.</summary>
        [MaxLength( 1000 )]
        [DataMember]
        public string ResolutionNote { get; set; }

        /// <summary>When the maintenance job last warned about this unconfirmed checkout.</summary>
        [DataMember]
        public DateTime? PendingReportedDateTime { get; set; }

        [LavaVisible]
        public virtual PersonAlias OwnerPersonAlias { get; set; }

        [LavaVisible]
        public virtual PersonAlias ResolvedByPersonAlias { get; set; }

        [LavaVisible]
        public virtual FinancialTransaction FinancialTransaction { get; set; }

        [LavaVisible]
        public virtual ICollection<CartRegistrant> Registrants { get; set; } = new List<CartRegistrant>();

        [LavaVisible]
        public virtual ICollection<CartLineItem> LineItems { get; set; } = new List<CartLineItem>();
    }

    public partial class CartConfiguration : EntityTypeConfiguration<Cart>
    {
        public CartConfiguration()
        {
            this.HasRequired( c => c.OwnerPersonAlias ).WithMany().HasForeignKey( c => c.OwnerPersonAliasId ).WillCascadeOnDelete( false );
            this.HasOptional( c => c.FinancialTransaction ).WithMany().HasForeignKey( c => c.FinancialTransactionId ).WillCascadeOnDelete( false );
            this.HasOptional( c => c.ResolvedByPersonAlias ).WithMany().HasForeignKey( c => c.ResolvedByPersonAliasId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartCart" );
        }
    }

    public class CartService : Service<Cart>
    {
        public CartService( RockContext context ) : base( context ) { }
    }
}
