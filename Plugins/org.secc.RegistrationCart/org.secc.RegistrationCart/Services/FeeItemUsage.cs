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
using System.Data.Entity;
using System.Linq;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Model;
using Rock.Data;
using Rock.Model;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Rock's fee item Maximum Usage: how many of a limited item an instance has left. Rock
    /// counts the quantity on every registrant fee for the instance; the cart adds the add-ons
    /// of other carts mid-checkout (open carts hold nothing, as with seats).
    /// </summary>
    public class FeeItemUsage
    {
        private readonly RockContext _rockContext;

        public FeeItemUsage( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>
        /// Per instance id, per limited fee item id: how many are left for <paramref name="cartId"/>,
        /// not counting that cart's own lines. Items with no limit are left out.
        /// </summary>
        public Dictionary<int, Dictionary<int, int>> Remaining( IEnumerable<RegistrationInstance> instances, int? cartId )
        {
            var instanceList = instances.Where( i => i != null ).GroupBy( i => i.Id ).Select( g => g.First() ).ToList();
            var limited = instanceList.ToDictionary( i => i.Id, i => i.RegistrationTemplate.Fees
                .SelectMany( f => f.FeeItems )
                .Where( fi => fi.MaximumUsageCount.HasValue )
                .ToList() );
            var instanceIds = instanceList.Select( i => i.Id ).ToList();
            var itemIds = limited.Values.SelectMany( items => items ).Select( i => i.Id ).Distinct().ToList();
            if ( !itemIds.Any() )
            {
                return instanceIds.ToDictionary( id => id, id => new Dictionary<int, int>() );
            }

            var otherCartId = cartId ?? 0;

            // Cart add-ons first, registrant fees second (as seats): a checkout committing in
            // between is counted twice rather than not at all.
            var held = new CartLineItemAddOnService( _rockContext ).Queryable().AsNoTracking()
                .Where( a => itemIds.Contains( a.RegistrationTemplateFeeItemId )
                    && instanceIds.Contains( a.CartLineItem.RegistrationInstanceId )
                    && a.CartLineItem.CartId != otherCartId
                    && a.CartLineItem.Cart.Status == CartStatus.Processing )
                .GroupBy( a => new { a.CartLineItem.RegistrationInstanceId, a.RegistrationTemplateFeeItemId } )
                .Select( g => new { g.Key.RegistrationInstanceId, g.Key.RegistrationTemplateFeeItemId, Count = g.Count() } )
                .ToList();
            var used = new RegistrationRegistrantFeeService( _rockContext ).Queryable().AsNoTracking()
                .Where( f => f.RegistrationTemplateFeeItemId.HasValue
                    && itemIds.Contains( f.RegistrationTemplateFeeItemId.Value )
                    && instanceIds.Contains( f.RegistrationRegistrant.Registration.RegistrationInstanceId ) )
                .GroupBy( f => new { f.RegistrationRegistrant.Registration.RegistrationInstanceId, FeeItemId = f.RegistrationTemplateFeeItemId.Value } )
                .Select( g => new { g.Key.RegistrationInstanceId, g.Key.FeeItemId, Quantity = g.Sum( f => f.Quantity ) } )
                .ToList();

            return limited.ToDictionary( kvp => kvp.Key, kvp => kvp.Value.ToDictionary( item => item.Id, item => CheckoutRules.FeeItemRemaining(
                item.MaximumUsageCount,
                used.Where( u => u.RegistrationInstanceId == kvp.Key && u.FeeItemId == item.Id ).Sum( u => u.Quantity ),
                held.Where( h => h.RegistrationInstanceId == kvp.Key && h.RegistrationTemplateFeeItemId == item.Id ).Sum( h => h.Count ) ).Value ) );
        }
    }
}
