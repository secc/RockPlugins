using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

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
                        Location = a.Location,
                        Entity = a.Group
                    } )
                .ToList();

            return await BingDistanceMatrix.OrderDestinations( origin, destinations );
        }
    }
}