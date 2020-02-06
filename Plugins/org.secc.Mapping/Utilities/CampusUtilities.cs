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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Mapping.Utilities
{
    public static class CampusUtilities
    {
        public static async Task<CampusCache> GetClosestCampus( string origin )
        {
            return ( await OrderCampusesByDistance( origin ) ).FirstOrDefault();
        }

        public static async Task<List<CampusCache>> OrderCampusesByDistance( string origin )
        {
            var campusDestinations = CampusCache.All()
                .Where( c => c.Location != null && !string.IsNullOrEmpty( c.Location.Street1 ) )
                .Select( c => new Destination
                {
                    Address = string.Format( "{0} {1} {2}, {3} {4}", c.Location.Street1, c.Location.Street2, c.Location.City, c.Location.State, c.Location.PostalCode ),
                    Entity = c
                } )
                .ToList();

            var distances = await BingDistanceMatrix.OrderDestinations( origin, campusDestinations );
            return distances.Select( d => d.Entity as CampusCache ).ToList();
        }
    }
}
