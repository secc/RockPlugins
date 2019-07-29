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
