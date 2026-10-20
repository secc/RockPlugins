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
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Financial;
using Rock.Model;
using Rock.ViewModels.Controls;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Checkout" )]
    [Category( "Registration Cart" )]
    [Description( "Takes payment for the cart and creates Rock registrations and the financial transaction." )]
    [IconCssClass( "fa fa-credit-card" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [FinancialGatewayField( "Financial Gateway", Key = AttributeKey.FinancialGateway, IsRequired = false, Order = 0,
        Description = "The gateway (e.g. My Well) used to charge the card. Gateways that support Rock's hosted payment control are used directly; otherwise the tokenizer settings below are used." )]
    [TextField( "Batch Name Prefix", Key = AttributeKey.BatchNamePrefix, IsRequired = false, DefaultValue = "Registration Cart", Order = 1,
        Description = "Prefix for the financial batch new payments are placed in." )]
    [TextField( "Tokenizer Script URL", Key = AttributeKey.TokenizerScriptUrl, IsRequired = false, DefaultValue = "https://sandbox.gotnpgateway.com/tokenizer/tokenizer.js", Category = "Tokenizer Fallback", Order = 2 )]
    [TextField( "Tokenizer Gateway URL", Key = AttributeKey.TokenizerGatewayUrl, IsRequired = false, DefaultValue = "https://sandbox.gotnpgateway.com", Category = "Tokenizer Fallback", Order = 3 )]
    [TextField( "Tokenizer Public Key", Key = AttributeKey.TokenizerPublicKey, IsRequired = false, Category = "Tokenizer Fallback", Order = 4,
        Description = "My Well / gotnpgateway PUBLIC api key. Only needed when the gateway does not provide a hosted payment control." )]
    [LinkedPage( "Cart Page", Key = NavAttributeKey.CartPage, IsRequired = false, Order = 5 )]
    [LinkedPage( "Catalog Page", Key = NavAttributeKey.CatalogPage, IsRequired = false, Order = 6 )]
    [LinkedPage( "Offering Detail Page", Key = NavAttributeKey.OfferingDetailPage, IsRequired = false, Order = 7,
        Description = "Where a line's Edit link goes (the offering page in edit mode), e.g. to answer questions a camper owes after coming off a wait list." )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_CHECKOUT )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CHECKOUT )]
    public class Checkout : RegistrationCartBlockBase
    {
        private static class AttributeKey
        {
            public const string FinancialGateway = "FinancialGateway";
            public const string BatchNamePrefix = "BatchNamePrefix";
            public const string TokenizerScriptUrl = "TokenizerScriptUrl";
            public const string TokenizerGatewayUrl = "TokenizerGatewayUrl";
            public const string TokenizerPublicKey = "TokenizerPublicKey";
        }

        public override string ObsidianFileUrl => ObsidianBlockUrl( "checkout" );

        protected override string LineEditUrl( Model.CartLineItem line ) => OfferingPageEditUrl( line );

        public override object GetObsidianBlockInitialization()
        {
            var manager = GetCartManager();
            var cart = manager?.GetOpenCart();
            var person = GetCurrentPerson();

            // Place campers in seats or on the wait list first, so the total shown is the one
            // checkout will expect, and say so when that moved someone. Then check the cart for
            // those places: a camper who just got a seat owes the questions the wait list skips.
            // ProcessCheckout does both again under the seat lock.
            var problems = new List<CheckoutProblem>();
            var notices = new List<string>();
            if ( cart != null && cart.LineItems.Any() )
            {
                var seats = new SeatPlanner( RockContext ).Place( cart );
                if ( seats.Changes.Any() )
                {
                    RockContext.SaveChanges();
                    notices.AddRange( seats.Changes );
                }

                problems.AddRange( seats.Problems );
                problems.AddRange( new CheckoutProcessor( RockContext ).Validate( cart ) );
            }

            var box = new CheckoutInitializationBox
            {
                Navigation = BuildNavigation( cart ),
                Cart = ToBag( manager?.Price( cart ) ?? global::org.secc.RegistrationCart.Domain.Pricing.PricedCart.Empty, cart, problems ),
                PayerName = person?.FullName,
                PayerEmail = person?.Email,
                Problems = problems.Select( p => p.Message ).Distinct().ToList(),
                Notices = notices
            };

            var gateway = GetFinancialGateway();
            if ( gateway != null )
            {
                box.IsGatewayConfigured = true;
                if ( gateway.GetGatewayComponent() is IObsidianHostedGatewayComponent hosted )
                {
                    // Rock-internal API (RK1000), but it is how Rock's own Obsidian
                    // RegistrationEntry block renders the hosted payment control; there is no
                    // public equivalent.
#pragma warning disable RK1000
                    box.GatewayControl = new GatewayControlBag
                    {
                        FileUrl = hosted.GetObsidianControlFileUrl( gateway ),
                        Settings = hosted.GetObsidianControlSettings( gateway, null )
                    };
#pragma warning restore RK1000
                }
                else
                {
                    var publicKey = GetAttributeValue( AttributeKey.TokenizerPublicKey );
                    if ( publicKey.IsNotNullOrWhiteSpace() )
                    {
                        box.Tokenizer = new TokenizerSettingsBag
                        {
                            ScriptUrl = GetAttributeValue( AttributeKey.TokenizerScriptUrl ),
                            GatewayUrl = GetAttributeValue( AttributeKey.TokenizerGatewayUrl ),
                            PublicKey = publicKey
                        };
                    }
                    else
                    {
                        box.Problems.Add( "The payment gateway does not provide a hosted payment form and no tokenizer public key is configured on this block." );
                    }
                }
            }
            else if ( box.Cart.Total > 0m )
            {
                box.Problems.Add( "No payment gateway is configured on this block." );
            }

            return box;
        }

        [BlockAction]
        public BlockActionResult ProcessCheckout( CheckoutRequestBag request )
        {
            var manager = GetCartManager();
            var person = GetCurrentPerson();
            if ( manager == null || person == null )
            {
                return ActionUnauthorized( "Please sign in." );
            }

            // Only the id: the processor reads the cart's contents after it has claimed it.
            var cartId = manager.GetOpenCartId();
            if ( cartId == null )
            {
                return ActionBadRequest( "Your cart is empty." );
            }

            if ( request == null )
            {
                return ActionBadRequest( "Please reload the page and try again." );
            }

            var processor = new CheckoutProcessor( RockContext )
            {
                AppRoot = RequestContext.RootUrlPath.EnsureTrailingForwardslash(),
                ThemeRoot = RequestContext.RootUrlPath + RequestContext.ResolveRockUrl( "~~/" )
            };
            var result = processor.Process( cartId.Value, request.ExpectedTotal, person, GetFinancialGateway(), request.GatewayToken, GetAttributeValue( AttributeKey.BatchNamePrefix ) );

            var bag = new CheckoutResultBag
            {
                Success = result.Success,
                AlreadyProcessed = result.AlreadyProcessed,
                IsDeclined = result.IsDeclined,
                CartChanged = result.CartChanged,
                ErrorMessage = result.ErrorMessage,
                TransactionId = result.TransactionId,
                Total = result.Total,
                ItemDescriptions = result.ItemDescriptions
            };

            if ( result.CartChanged || result.Problems.Any() )
            {
                // Nothing was charged: send the cart as it is now, with what stops it, so the page
                // shows what would be charged and what to fix.
                var cart = manager.GetOpenCart();
                bag.Cart = ToBag( manager.Price( cart ), cart, result.Problems );
                bag.Problems = result.Problems.Select( p => p.Message ).Distinct().ToList();
            }

            return ActionOk( bag );
        }

        private FinancialGateway GetFinancialGateway()
        {
            var guid = GetAttributeValue( AttributeKey.FinancialGateway ).AsGuidOrNull();
            if ( guid == null )
            {
                return null;
            }

            var gateway = new FinancialGatewayService( RockContext ).Get( guid.Value );
            if ( gateway == null || !gateway.IsActive )
            {
                return null;
            }

            gateway.LoadAttributes( RockContext );
            return gateway;
        }
    }
}
