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
    [RockDomain( "Registration Cart" )]
    [Table( "_org_secc_RegistrationCart_DiscountCode" )]
    [DataContract]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.DISCOUNT_CODE )]
    public class DiscountCode : Model<DiscountCode>, IRockEntity
    {
        /// <summary>Stored upper-case; matched case-insensitively.</summary>
        [Required]
        [MaxLength( 50 )]
        [DataMember( IsRequired = true )]
        public string Code { get; set; }

        [DataMember]
        public DiscountType DiscountType { get; set; }

        [DataMember]
        [DecimalPrecision( 18, 2 )]
        public decimal Value { get; set; }

        [DataMember]
        public DiscountScope Scope { get; set; }

        /// <summary>Only used when Scope = Offering: the Rock RegistrationTemplate this code applies to.</summary>
        [DataMember]
        public int? RegistrationTemplateId { get; set; }

        [DataMember]
        public DateTime? ValidFrom { get; set; }

        [DataMember]
        public DateTime? ValidTo { get; set; }

        [DataMember]
        public bool IsActive { get; set; } = true;

        [LavaVisible]
        public virtual RegistrationTemplate RegistrationTemplate { get; set; }

        public bool IsValidOn( DateTime date )
        {
            var day = date.Date;
            return IsActive
                && ( ValidFrom == null || day >= ValidFrom.Value.Date )
                && ( ValidTo == null || day <= ValidTo.Value.Date );
        }

        public override string ToString() => Code;
    }

    public partial class DiscountCodeConfiguration : EntityTypeConfiguration<DiscountCode>
    {
        public DiscountCodeConfiguration()
        {
            this.HasOptional( d => d.RegistrationTemplate ).WithMany().HasForeignKey( d => d.RegistrationTemplateId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "RegistrationCartDiscountCode" );
        }
    }

    public class DiscountCodeService : Service<DiscountCode>
    {
        public DiscountCodeService( RockContext context ) : base( context ) { }
    }
}
