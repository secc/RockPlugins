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
using System.ComponentModel;
using System.Linq;
using org.secc.RegistrationCart.Model;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Model;
using Rock.ViewModels.Utility;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Discount Codes" )]
    [Category( "Registration Cart > Administration" )]
    [Description( "Administer discount codes." )]
    [IconCssClass( "fa fa-tags" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_ADMIN_DISCOUNT_CODES )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.ADMIN_DISCOUNT_CODES )]
    public class AdminDiscountCodes : AdminBlockBase
    {
        public override string ObsidianFileUrl => ObsidianBlockUrl( "adminDiscountCodes" );

        public override object GetObsidianBlockInitialization()
        {
            return new DiscountCodeListInitializationBox
            {
                CanEdit = CanEdit,
                Codes = LoadCodes(),
                Offerings = new RegistrationTemplateService( RockContext ).Queryable()
                    .Where( t => t.IsActive )
                    .OrderBy( t => t.Name )
                    .Select( t => new { t.Id, t.Name } )
                    .ToList()
                    .Select( t => new ListItemBag { Value = t.Id.ToString(), Text = t.Name } )
                    .ToList()
            };
        }

        [BlockAction]
        public BlockActionResult Save( DiscountCodeBag bag )
        {
            var denied = RequireEdit();
            if ( denied != null ) return denied;

            var code = ( bag?.Code ?? "" ).Trim().ToUpperInvariant();
            if ( code.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A code is required." );
            }

            if ( bag.Value <= 0m )
            {
                return ActionBadRequest( "The value must be greater than zero." );
            }

            if ( ( DiscountType ) bag.DiscountType == DiscountType.Percent && bag.Value > 100m )
            {
                return ActionBadRequest( "A percentage discount can't exceed 100." );
            }

            var service = new DiscountCodeService( RockContext );
            var duplicate = service.Queryable().Any( d => d.Code.ToUpper() == code && d.Id != bag.Id );
            if ( duplicate )
            {
                return ActionBadRequest( $"The code {code} already exists." );
            }

            var discount = bag.Id > 0 ? service.Get( bag.Id ) : null;
            if ( discount == null )
            {
                discount = new DiscountCode();
                service.Add( discount );
            }

            discount.Code = code.Truncate( 50 );
            discount.DiscountType = ( DiscountType ) bag.DiscountType;
            discount.Value = bag.Value;
            discount.Scope = ( DiscountScope ) bag.Scope;
            discount.RegistrationTemplateId = discount.Scope == DiscountScope.Offering ? ParseInt( bag.OfferingId ) : null;
            discount.ValidFrom = ParseDate( bag.ValidFrom );
            discount.ValidTo = ParseDate( bag.ValidTo );
            discount.IsActive = bag.IsActive;

            if ( discount.Scope == DiscountScope.Offering && discount.RegistrationTemplateId == null )
            {
                return ActionBadRequest( "Choose the offering this code applies to." );
            }

            RockContext.SaveChanges();
            return ActionOk( new SaveDiscountCodeResultBag { Codes = LoadCodes() } );
        }

        [BlockAction]
        public BlockActionResult Delete( int id )
        {
            var denied = RequireEdit();
            if ( denied != null ) return denied;

            var service = new DiscountCodeService( RockContext );
            var discount = service.Get( id );
            if ( discount == null )
            {
                return ActionNotFound( "Code not found." );
            }

            if ( new CartDiscountService( RockContext ).Queryable().Any( cd => cd.DiscountCodeId == id ) )
            {
                return ActionBadRequest( "This code has been applied to carts, so it can't be deleted. Mark it inactive instead." );
            }

            service.Delete( discount );
            RockContext.SaveChanges();
            return ActionOk( new SaveDiscountCodeResultBag { Codes = LoadCodes() } );
        }

        private List<DiscountCodeBag> LoadCodes()
        {
            var used = new CartDiscountService( RockContext ).Queryable()
                .GroupBy( cd => cd.DiscountCodeId )
                .Select( g => new { Id = g.Key, Count = g.Count() } )
                .ToDictionary( x => x.Id, x => x.Count );

            return new DiscountCodeService( RockContext ).Queryable()
                .OrderBy( d => d.Code )
                .ToList()
                .Select( d => new DiscountCodeBag
                {
                    Id = d.Id,
                    Code = d.Code,
                    DiscountType = ( int ) d.DiscountType,
                    Value = d.Value,
                    Scope = ( int ) d.Scope,
                    OfferingId = d.RegistrationTemplateId?.ToString() ?? "",
                    OfferingTitle = d.RegistrationTemplate?.Name,
                    ValidFrom = DateString( d.ValidFrom ),
                    ValidTo = DateString( d.ValidTo ),
                    IsActive = d.IsActive,
                    TimesUsed = used.TryGetValue( d.Id, out var c ) ? c : 0
                } )
                .ToList();
        }
    }
}
