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
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Counts what takes each camp's seats, for <see cref="CheckoutRules.SpotsAvailable"/>. The
    /// catalog, adding to the cart and checkout all count here, so a camp is full in the same
    /// way everywhere (and the way Rock's own registration sees it).
    /// </summary>
    public class SeatCounter
    {
        private readonly RockContext _rockContext;

        public SeatCounter( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>Seats left per instance for <paramref name="cartId"/> (null = no limit), not counting that cart's own lines.</summary>
        public Dictionary<int, int?> SpotsAvailable( IEnumerable<RegistrationInstance> instances, int? cartId ) =>
            Count( instances, cartId ).ToDictionary( kvp => kvp.Key, kvp => CheckoutRules.SpotsAvailable( kvp.Value ) );

        /// <summary>What takes each instance's seats, besides the lines of <paramref name="cartId"/>.</summary>
        public Dictionary<int, SeatCounts> Count( IEnumerable<RegistrationInstance> instances, int? cartId )
        {
            var instanceList = instances.Where( i => i != null ).GroupBy( i => i.Id ).Select( g => g.First() ).ToList();
            var ids = instanceList.Select( i => i.Id ).ToList();
            if ( !ids.Any() )
            {
                return new Dictionary<int, SeatCounts>();
            }

            var otherCartId = cartId ?? 0;
            var now = RockDateTime.Now;

            // Lines first, registrants second: a checkout that commits in between is counted
            // twice (refusing a seat that was free) rather than not at all (overbooking).
            var processing = new CartLineItemService( _rockContext ).Queryable().AsNoTracking()
                .Where( l => ids.Contains( l.RegistrationInstanceId ) && l.CartId != otherCartId && l.Cart.Status == CartStatus.Processing )
                .GroupBy( l => l.RegistrationInstanceId )
                .Select( g => new { InstanceId = g.Key, Count = g.Count() } )
                .ToDictionary( x => x.InstanceId, x => x.Count );
            var registrants = new RegistrationRegistrantService( _rockContext ).Queryable().AsNoTracking()
                .Where( r => ids.Contains( r.Registration.RegistrationInstanceId ) && !r.Registration.IsTemporary )
                .GroupBy( r => r.Registration.RegistrationInstanceId )
                .Select( g => new { InstanceId = g.Key, Count = g.Count() } )
                .ToDictionary( x => x.InstanceId, x => x.Count );
            var sessions = new RegistrationSessionService( _rockContext ).Queryable().AsNoTracking()
                .Where( s => ids.Contains( s.RegistrationInstanceId ) && s.ExpirationDateTime > now )
                .GroupBy( s => s.RegistrationInstanceId )
                .Select( g => new { InstanceId = g.Key, Count = g.Sum( s => s.RegistrationCount ) } )
                .ToDictionary( x => x.InstanceId, x => x.Count );

            return instanceList.ToDictionary( i => i.Id, i => new SeatCounts(
                i.MaxAttendees,
                registrants.TryGetValue( i.Id, out var registered ) ? registered : 0,
                sessions.TryGetValue( i.Id, out var inSession ) ? inSession : 0,
                i.TimeoutIsEnabled,
                processing.TryGetValue( i.Id, out var lines ) ? lines : 0 ) );
        }
    }
}
