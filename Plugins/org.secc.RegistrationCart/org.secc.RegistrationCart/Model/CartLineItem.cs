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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace org.secc.RegistrationCart.Model
{
    /// <summary>One registrant registered for one Rock registration instance. Prices are snapshotted at add time.</summary>
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_CartLineItem" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.CART_LINE_ITEM )]
    public class CartLineItem : Model<CartLineItem>, IRockEntity
    {
        [DataMember]
        public int CartId { get; set; }

        [DataMember]
        public int CartRegistrantId { get; set; }

        [DataMember]
        public int RegistrationTemplateId { get; set; }

        [DataMember]
        public int RegistrationInstanceId { get; set; }

        [DataMember]
        [DecimalPrecision( 18, 2 )]
        public decimal SnapshotBasePrice { get; set; }

        /// <summary>
        /// The camp is full and its template allows a wait list: nothing is due now and checkout
        /// creates a wait-listed registrant. Re-assigned at checkout in cart order.
        /// </summary>
        [DataMember]
        public bool IsWaitList { get; set; }

        /// <summary>
        /// The registrant's answers to the template's forms, keyed by form field Guid (see
        /// Services/RegistrationForms). Written to the person, registrant and group member at checkout.
        /// </summary>
        [DataMember]
        public string FieldValuesJson { get; set; }

        /// <summary>The signed inline signature document, as Rock's electronic signature returns it.</summary>
        [DataMember]
        public string SignatureData { get; set; }

        /// <summary>The Rock Registration created for this line at checkout.</summary>
        [DataMember]
        public int? RegistrationId { get; set; }

        [LavaVisible]
        public virtual Cart Cart { get; set; }

        [LavaVisible]
        public virtual CartRegistrant CartRegistrant { get; set; }

        [LavaVisible]
        public virtual RegistrationTemplate RegistrationTemplate { get; set; }

        [LavaVisible]
        public virtual RegistrationInstance RegistrationInstance { get; set; }

        [LavaVisible]
        public virtual Registration Registration { get; set; }

        [LavaVisible]
        public virtual ICollection<CartLineItemAddOn> AddOns { get; set; } = new List<CartLineItemAddOn>();
    }

    public partial class CartLineItemConfiguration : EntityTypeConfiguration<CartLineItem>
    {
        public CartLineItemConfiguration()
        {
            this.HasRequired( l => l.Cart ).WithMany( c => c.LineItems ).HasForeignKey( l => l.CartId ).WillCascadeOnDelete( true );
            // No cascade from registrant: SQL Server forbids two cascade paths into the same table.
            this.HasRequired( l => l.CartRegistrant ).WithMany().HasForeignKey( l => l.CartRegistrantId ).WillCascadeOnDelete( false );
            this.HasRequired( l => l.RegistrationTemplate ).WithMany().HasForeignKey( l => l.RegistrationTemplateId ).WillCascadeOnDelete( false );
            this.HasRequired( l => l.RegistrationInstance ).WithMany().HasForeignKey( l => l.RegistrationInstanceId ).WillCascadeOnDelete( false );
            this.HasOptional( l => l.Registration ).WithMany().HasForeignKey( l => l.RegistrationId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartCartLineItem" );
        }
    }

    public class CartLineItemService : Service<CartLineItem>
    {
        public CartLineItemService( RockContext context ) : base( context ) { }
    }
}
