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
using Rock.Data;
using org.secc.Mapping.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using Rock.Web.Cache;

namespace org.secc.Mapping.Model
{
    public class LocationDistanceStoreService : MappingService<LocationDistanceStore>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishGroup"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public LocationDistanceStoreService( RockContext context ) : base( context ) { }

        /// <summary>
        /// Inserts travel time from an origin to a set of destination objects.
        /// </summary>
        /// <param name="origin">The origin address</param>
        /// <param name="destinations">List of destination objects</param>
        public void LoadDurations( string origin, List<Destination> destinations )
        {

            foreach ( var destination in destinations )
            {
                var destinationCache = RockCache.Get( origin + destination.Address, "org.secc.Mapping.LocationDistance" ) as Destination;
                if ( destinationCache != null )
                {
                    destination.TravelDistance = destinationCache.TravelDistance;
                    destination.TravelDuration = destinationCache.TravelDuration;
                    destination.IsCalculated = true;
                }
            }
            var searchList = destinations.Where( d => d.IsCalculated == false ).Select( d => d.Address );

            var locationDistances = this.Queryable().Where( d => d.Origin == origin && searchList.Contains( d.Destination ) );
            foreach ( var locationDistance in locationDistances )
            {
                var destinationItems = destinations.Where( d => d.Address == locationDistance.Destination );
                foreach ( var destinationItem in destinationItems )
                {
                    destinationItem.TravelDuration = locationDistance.TravelDuration;
                    destinationItem.TravelDistance = locationDistance.TravelDistance;
                    destinationItem.IsCalculated = true;
                    RockCache.AddOrUpdate( origin + destinationItem.Address, "org.secc.Mapping.LocationDistance", destinationItem );
                }
            }
        }

        public void AddOrUpdate( string origin, Destination destination, string calculatedBy )
        {
            var distances = this.Queryable().Where( d => d.Origin == origin && d.Destination == destination.Address ).ToList();
            if ( distances.Any() )
            {
                foreach ( var distance in distances )
                {
                    distance.TravelDuration = destination.TravelDuration;
                    distance.TravelDistance = destination.TravelDistance;
                    distance.CalculatedBy = calculatedBy;
                }
            }
            else
            {
                Add( new LocationDistanceStore
                {
                    Origin = origin,
                    Destination = destination.Address,
                    TravelDistance = destination.TravelDistance,
                    TravelDuration = destination.TravelDuration,
                    CalculatedBy = calculatedBy
                } );
            }
        }

    }
}
