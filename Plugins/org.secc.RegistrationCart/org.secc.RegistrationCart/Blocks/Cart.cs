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
using System.ComponentModel;
using System.Linq;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart" )]
    [Category( "Registration Cart" )]
    [Description( "Shows the current person's cart with pricing and discount codes." )]
    [IconCssClass( "fa fa-shopping-cart" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [LinkedPage( "Checkout Page", Key = NavAttributeKey.CheckoutPage, IsRequired = false, Order = 0 )]
    [LinkedPage( "Catalog Page", Key = NavAttributeKey.CatalogPage, IsRequired = false, Order = 1 )]
    [LinkedPage( "Offering Detail Page", Key = NavAttributeKey.OfferingDetailPage, IsRequired = false, Order = 2,
        Description = "Where a line's Edit link goes (the offering page in edit mode)." )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_CART )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CART )]
    public class Cart : RegistrationCartBlockBase
    {
        public override string ObsidianFileUrl => ObsidianBlockUrl( "cart" );

        protected override string LineEditUrl( Model.CartLineItem line ) => OfferingPageEditUrl( line );

        public override object GetObsidianBlockInitialization()
        {
            var manager = GetCartManager();
            var cart = manager?.GetOpenCart();
            return new CartInitializationBox
            {
                Navigation = BuildNavigation( cart ),
                Cart = ToBag( manager?.Price( cart ) ?? global::org.secc.RegistrationCart.Domain.Pricing.PricedCart.Empty, cart, Problems( cart ) )
            };
        }

        /// <summary>What checkout would refuse, shown on the lines now so they can be fixed first.</summary>
        private List<Services.CheckoutProblem> Problems( Model.Cart cart ) =>
            cart?.LineItems.Any() == true ? new Services.CheckoutProcessor( RockContext ).Validate( cart ) : new List<Services.CheckoutProblem>();

        [BlockAction]
        public BlockActionResult RemoveLineItem( int lineItemId )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in." );
            }

            manager.RemoveLineItem( lineItemId );
            return CartResult( manager, null );
        }

        [BlockAction]
        public BlockActionResult ApplyDiscount( string code )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in." );
            }

            var error = manager.ApplyDiscount( code );
            return CartResult( manager, error );
        }

        [BlockAction]
        public BlockActionResult RemoveDiscount( int cartDiscountId )
        {
            var manager = GetCartManager();
            if ( manager == null )
            {
                return ActionUnauthorized( "Please sign in." );
            }

            manager.RemoveDiscount( cartDiscountId );
            return CartResult( manager, null );
        }

        private BlockActionResult CartResult( Services.CartManager manager, string error )
        {
            var cart = manager.GetOpenCart();
            return ActionOk( new DiscountResultBag
            {
                ErrorMessage = error,
                Cart = ToBag( manager.Price( cart ), cart, Problems( cart ) )
            } );
        }
    }
}
