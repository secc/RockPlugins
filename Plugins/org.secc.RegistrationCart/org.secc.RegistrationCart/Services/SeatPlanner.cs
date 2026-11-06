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
    /// <summary>Where one cart line goes.</summary>
    public enum SeatPlace
    {
        Seat,
        WaitList,
        /// <summary>No seat left and the template has no wait list.</summary>
        Refused
    }

    /// <summary>
    /// Places a cart's campers in the seats each camp has left (<see cref="SeatCounter"/>), in
    /// cart order: the order they were added, a line not saved yet last. Campers who don't fit
    /// go on the wait list when the template has one, else they are refused. Adding to the cart,
    /// the checkout page and checkout itself all place lines here.
    /// </summary>
    public class SeatPlanner
    {
        /// <summary>Result of <see cref="Place(Cart)"/>.</summary>
        public class SeatCheck
        {
            /// <summary>Campers that don't fit a camp with no wait list. Checkout can't go on.</summary>
            public List<CheckoutProblem> Problems { get; } = new List<CheckoutProblem>();

            /// <summary>Lines moved onto or off the wait list (already set on the cart, not saved). The total changed.</summary>
            public List<string> Changes { get; } = new List<string>();
        }

        private readonly RockContext _rockContext;

        public SeatPlanner( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>
        /// Where each of <paramref name="lines"/> (one cart's) goes, without changing them. A line
        /// whose instance no longer exists is left out.
        /// </summary>
        public Dictionary<CartLineItem, SeatPlace> Plan( int cartId, IEnumerable<CartLineItem> lines )
        {
            var byInstance = lines
                .GroupBy( l => l.RegistrationInstanceId )
                .ToDictionary( g => g.Key, g => g.OrderBy( l => l.Id == 0 ? int.MaxValue : l.Id ).ToList() );
            var instanceIds = byInstance.Keys.ToList();
            var instances = new RegistrationInstanceService( _rockContext ).Queryable()
                .Include( i => i.RegistrationTemplate )
                .Where( i => instanceIds.Contains( i.Id ) )
                .ToList();
            var spots = new SeatCounter( _rockContext ).SpotsAvailable( instances, cartId );

            var plan = new Dictionary<CartLineItem, SeatPlace>();
            foreach ( var instance in instances )
            {
                var instanceLines = byInstance[instance.Id];
                var seats = CheckoutRules.AssignSeats( spots[instance.Id], instanceLines.Count, instance.RegistrationTemplate.WaitListEnabled );
                for ( var i = 0; i < instanceLines.Count; i++ )
                {
                    plan[instanceLines[i]] = i < seats.Seated ? SeatPlace.Seat
                        : i < seats.Seated + seats.WaitListed ? SeatPlace.WaitList
                        : SeatPlace.Refused;
                }
            }

            return plan;
        }

        /// <summary>
        /// Places all of <paramref name="cart"/>'s lines. A camp that can't take every camper
        /// for it (no wait list) is a problem and its lines are left as they were. A camper whose
        /// place changed (the camp filled up, or a spot opened) has
        /// <see cref="CartLineItem.IsWaitList"/> updated on the cart, unsaved, and is listed in
        /// <see cref="SeatCheck.Changes"/>: the total changed, so the shopper must confirm it.
        /// </summary>
        public SeatCheck Place( Cart cart )
        {
            var check = new SeatCheck();
            if ( cart == null || cart.LineItems.Count == 0 )
            {
                return check;
            }

            var plan = Plan( cart.Id, cart.LineItems );
            foreach ( var instanceLines in cart.LineItems.Where( plan.ContainsKey ).GroupBy( l => l.RegistrationInstanceId ) )
            {
                var lines = instanceLines.ToList();
                var name = lines[0].RegistrationInstance?.Name;
                var refused = lines.Count( l => plan[l] == SeatPlace.Refused );
                if ( refused > 0 )
                {
                    var seated = lines.Count - refused;
                    check.Problems.Add( new CheckoutProblem( seated == 0
                        ? $"\"{name}\" is full."
                        : $"\"{name}\" has only {seated} spot{( seated == 1 ? "" : "s" )} left, but your cart has {lines.Count}.",
                        lines.Select( l => l.Id ) ) );
                    continue;
                }

                foreach ( var line in lines )
                {
                    var waitList = plan[line] == SeatPlace.WaitList;
                    if ( line.IsWaitList == waitList )
                    {
                        continue;
                    }

                    line.IsWaitList = waitList;
                    var camper = line.CartRegistrant?.DisplayName ?? "A camper";
                    check.Changes.Add( waitList
                        ? $"\"{name}\" filled up, so {camper} is now on the wait list (nothing is due for them now)."
                        : $"A spot opened in \"{name}\", so {camper} is no longer on the wait list." );
                }
            }

            return check;
        }
    }
}
