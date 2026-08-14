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
using System.Linq;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.Mapping.Utilities
{
    public static class GroupUtilities
    {
        /// <summary>
        /// Bounds how many destinations a single request can send to Azure Maps, capping billed
        /// matrix size / cost. Applied HERE rather than on the caller's group query on purpose:
        /// only groups that survive the GroupLocation + postal-coded Location joins below are ever
        /// billed, so capping the raw group query would spend the budget on candidates that yield
        /// no destination at all (e.g. group types with no mapped locations) and silently drop
        /// mappable ones. Capping post-join bounds exactly the quantity Azure charges for.
        /// </summary>
        public const int MaxMatrixDestinations = 1000;

        public static async Task<List<Destination>> GetGroupsDestinations( string origin, IQueryable<Group> groups, RockContext rockContext, List<int> locationTypeIds = null )
        {
            var groupLocationQueryable = new GroupLocationService( rockContext ).Queryable();
            if ( locationTypeIds != null && locationTypeIds.Any() )
            {
                groupLocationQueryable = groupLocationQueryable.Where( gl => locationTypeIds.Contains( gl.GroupLocationTypeValueId ?? 0 ) );
            }

            var locationQueryable = new LocationService( rockContext ).Queryable().Where( l => l.PostalCode != null && l.PostalCode != "" );

            var destinations = groups
                .Join( groupLocationQueryable,
                g => g.Id,
                gl => gl.GroupId,
                ( g, gl ) => new
                {
                    Group = g,
                    GroupLocation = gl
                } )
                .Join( locationQueryable,
                    a => a.GroupLocation.LocationId,
                    l => l.Id,
                    ( a, l ) => new
                    {
                        Group = a.Group,
                        GroupLocation = a.GroupLocation,
                        Location = l
                    } )
                // One row per group, same intent as the DistinctBy this replaces, but written in plain
                // LINQ so the chain is guaranteed to stay an IQueryable and the cap below executes as
                // SQL rather than after materializing the join. That guarantee matters now that this
                // method is the only cap: the callers used to pre-cap their own group query, so a
                // client-side dedupe still read a bounded number of rows. Min( LocationId ) also picks
                // each group's location deterministically, where DistinctBy took an arbitrary row.
                .GroupBy( a => a.Group.Id )
                    .Select( grp => new Destination
                    {
                        LocationId = grp.Min( a => ( int? ) a.Location.Id ),
                        EntityId = ( int? ) grp.Key,
                    } )
                // Order before Take so the capped subset is stable across requests -- an unordered
                // Take can hand back a different set of groups each call, making results appear to
                // shuffle. Group id is arbitrary but deterministic; distance ordering happens in
                // OrderDestinations once Azure returns.
                .OrderBy( d => d.EntityId )
                .Take( MaxMatrixDestinations )
                .ToList();

            return await AzureDistanceMatrix.OrderDestinations( origin, destinations );
        }
    }
}