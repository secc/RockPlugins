// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
                .DistinctBy( a => a.Group.Id )
                    .Select( a => new Destination
                    {
                        LocationId = a.Location.Id,
                        EntityId = a.Group.Id,
                    } )
                .ToList();

            return await BingDistanceMatrix.OrderDestinations( origin, destinations );
        }
    }
}