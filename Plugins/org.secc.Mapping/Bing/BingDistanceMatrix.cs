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
using BingMapsRESTToolkit;
using org.secc.Mapping.Model;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Mapping
{
    public static class BingDistanceMatrix
    {
        public static async Task<List<org.secc.Mapping.Destination>> OrderDestinations( string origin, List<Destination> destinations )
        {

            var destinationWaypoints = new List<SimpleWaypoint>();

            //Get stored distances from database
            RockContext rockContext = new RockContext();
            LocationDistanceStoreService locationDistanceStoreService = new LocationDistanceStoreService( rockContext );
            locationDistanceStoreService.LoadDurations( origin, destinations );

            //Get new distances from Bing
            foreach ( var destination in destinations.Where( d => !d.IsCalculated && d.Address.IsNotNullOrWhiteSpace() ) )
            {
                destinationWaypoints.Add( new SimpleWaypoint( destination.Address ) );
            }
            if ( destinationWaypoints.Any() )
            {
                var distanceMatrix = new DistanceMatrixRequest();
                distanceMatrix.BingMapsKey = GlobalAttributesCache.Get().GetValue( "BingMapsKey" );
                distanceMatrix.DistanceUnits = DistanceUnitType.Miles;
                distanceMatrix.TravelMode = TravelModeType.Driving;
                distanceMatrix.TimeUnits = TimeUnitType.Minute;
                distanceMatrix.Origins = new List<SimpleWaypoint> { new SimpleWaypoint( origin ) };
                distanceMatrix.Destinations = destinationWaypoints;
                var response = await distanceMatrix.Execute();

                var data = response.ResourceSets[0].Resources[0];
                var distances = ( BingMapsRESTToolkit.DistanceMatrix ) data;
                var results = distances.Results.OrderBy( r => r.TravelDuration );
                foreach ( var result in results )
                {
                    var destination = destinations.Where( d => d.Address == distances.Destinations[result.DestinationIndex].Address ).FirstOrDefault();
                    if ( destination != null && !result.HasError && !( result.TravelDistance < 0 ) && !( result.TravelDuration < 0 ) )
                    {
                        destination.TravelDuration = result.TravelDuration;
                        destination.TravelDistance = result.TravelDistance;
                        destination.IsCalculated = true;
                        locationDistanceStoreService.AddOrUpdate( origin, destination, "Bing" );
                    }
                }
                rockContext.SaveChanges();
            }

            return destinations
                .Where( d => d.IsCalculated == true )
                .OrderBy( d => d.TravelDuration )
                .ToList();
        }

    }
}
