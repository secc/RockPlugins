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
using System.Linq;
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Blocks;
using Rock.Model;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Pricing;

namespace org.secc.RegistrationCart.Blocks
{
    /// <summary>Shared plumbing for the public Registration Cart blocks.</summary>
    public abstract class RegistrationCartBlockBase : RockBlockType
    {
        /// <summary>Attribute keys shared by the public blocks (each block declares the ones it uses).</summary>
        protected static class NavAttributeKey
        {
            public const string CatalogPage = "CatalogPage";
            public const string CartPage = "CartPage";
            public const string CheckoutPage = "CheckoutPage";
            public const string OfferingDetailPage = "OfferingDetailPage";
        }

        protected const string OfferingKeyParameter = "OfferingKey";

        /// <summary>The offering page edits this cart line instead of adding a new one.</summary>
        protected const string LineIdParameter = "LineId";

        /// <summary>Where a cart line's Edit link goes; none unless the block links the offering page.</summary>
        protected virtual string LineEditUrl( Model.CartLineItem line ) => null;

        /// <summary>The block's linked offering page, in edit mode for <paramref name="line"/>.</summary>
        protected string OfferingPageEditUrl( Model.CartLineItem line ) =>
            this.GetLinkedPageUrl( NavAttributeKey.OfferingDetailPage, new Dictionary<string, string>
            {
                [OfferingKeyParameter] = line.RegistrationTemplate?.IdKey ?? line.RegistrationTemplateId.ToString(),
                [LineIdParameter] = line.Id.ToString()
            } );

        protected string ObsidianBlockUrl( string fileName ) => "~/Plugins/org_secc/RegistrationCart/blocks/" + fileName + ".obs";

        protected bool AllowIntegerIds => !( PageCache?.Layout?.Site?.DisablePredictableIds ?? false );

        /// <summary>A CartManager for the signed-in person, or null when anonymous.</summary>
        protected CartManager GetCartManager()
        {
            var person = GetCurrentPerson();
            return person == null ? null : new CartManager( RockContext, person );
        }

        protected NavigationBag BuildNavigation( Model.Cart cart )
        {
            string loginUrl = "";
            try
            {
                loginUrl = PageCache?.Layout?.Site?.GetLoginUrlWithReturnUrl() ?? "";
            }
            catch
            {
                // No HttpContext (e.g. tests) — leave empty.
            }

            var pending = GetCartManager()?.GetPendingCheckouts().FirstOrDefault();
            return new NavigationBag
            {
                IsSignedIn = GetCurrentPerson() != null,
                LoginUrl = loginUrl,
                CatalogUrl = this.GetLinkedPageUrl( NavAttributeKey.CatalogPage ),
                CartUrl = this.GetLinkedPageUrl( NavAttributeKey.CartPage ),
                CheckoutUrl = this.GetLinkedPageUrl( NavAttributeKey.CheckoutPage ),
                CartItemCount = cart?.LineItems.Count ?? 0,
                PendingCheckoutNotice = pending == null ? null
                    : $"We couldn't confirm your checkout from {( pending.ModifiedDateTime ?? pending.CreatedDateTime )?.ToString( "MMM d" ) ?? "earlier"}. Your card may have been charged, so please contact the office before paying again."
            };
        }

        /// <param name="problems">What stops the cart checking out; each shows on the lines it concerns.</param>
        protected PricedCartBag ToBag( PricedCart priced, Model.Cart cart, IEnumerable<CheckoutProblem> problems = null )
        {
            var problemsByLine = ( problems ?? Enumerable.Empty<CheckoutProblem>() )
                .SelectMany( p => p.LineItemIds.Select( id => new { id, p.Message } ) )
                .GroupBy( x => x.id )
                .ToDictionary( g => g.Key, g => string.Join( " ", g.Select( x => x.Message ).Distinct() ) );
            var dateTexts = cart == null
                ? new Dictionary<int, string>()
                : new CatalogService( RockContext ).GetEventDateTexts( cart.LineItems.Select( l => l.RegistrationInstanceId ) );
            var instanceByLine = cart?.LineItems.ToDictionary( l => l.Id, l => l.RegistrationInstanceId ) ?? new Dictionary<int, int>();
            var lineById = cart?.LineItems.ToDictionary( l => l.Id ) ?? new Dictionary<int, Model.CartLineItem>();

            var bag = new PricedCartBag
            {
                Subtotal = priced.Subtotal,
                DiscountTotal = priced.DiscountTotal,
                Total = priced.Total,
                ItemCount = priced.Lines.Count
            };

            foreach ( var line in priced.Lines )
            {
                bag.Lines.Add( new PricedLineBag
                {
                    LineItemId = line.LineItemId,
                    RegistrantId = line.RegistrantId,
                    RegistrantName = line.RegistrantName,
                    OfferingTitle = line.OfferingTitle,
                    VariantName = line.VariantName,
                    DateText = instanceByLine.TryGetValue( line.LineItemId, out var instanceId ) && dateTexts.TryGetValue( instanceId, out var dt ) ? dt : "",
                    BasePrice = line.BasePrice,
                    LineTotal = line.LineTotal,
                    IsWaitList = line.IsWaitList,
                    EditUrl = lineById.TryGetValue( line.LineItemId, out var cartLine ) ? LineEditUrl( cartLine ) : null,
                    Problem = problemsByLine.GetValueOrNull( line.LineItemId ),
                    AddOns = line.AddOns.Select( a => new PricedAddOnBag { Name = a.Name, Price = a.Price } ).ToList()
                } );
            }

            foreach ( var d in priced.Discounts )
            {
                var cartDiscount = cart?.Discounts.FirstOrDefault( cd => cd.DiscountCode?.Code == d.Code );
                bag.Discounts.Add( new AppliedDiscountBag { CartDiscountId = cartDiscount?.Id ?? 0, Code = d.Code, Amount = d.Amount } );
            }

            return bag;
        }

        protected static string DateRangeText( DateTime? start, DateTime? end ) => CatalogService.DateRangeText( start, end );
    }
}
