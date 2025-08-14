using System.Linq;
using System.Net;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace org.secc.Rest.Controllers
{
    public class GroupAppGroupLocationController : ApiControllerBase
    {
        /// <summary>
        /// Get the group's current schedule location.
        /// </summary>
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/Groups/{groupId:int}/Location" )]
        public IHttpActionResult GetGroupLocation( int groupId )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            using ( var rockContext = new RockContext() )
            {
                rockContext.Configuration.ProxyCreationEnabled = false;

                if ( !IsMemberOfGroup( rockContext, currentUser.Person.Id, groupId ) )
                {
                    return StatusCode( HttpStatusCode.Forbidden );
                }

                var group = new GroupService( rockContext ).Get( groupId );
                if ( group == null )
                {
                    return NotFound();
                }

                // Load group locations
                var glService = new GroupLocationService( rockContext );
                var meetingTypeId = GetMeetingLocationTypeId( rockContext );
                var homeTypeId = GetHomeLocationTypeId( rockContext );

                var gl = glService.Queryable( "Location" )
                    .Where( x => x.GroupId == groupId )
                    .OrderByDescending( x => x.GroupLocationTypeValueId == meetingTypeId )
                    .ThenByDescending( x => x.GroupLocationTypeValueId == homeTypeId )
                    .ThenBy( x => x.Id )
                    .FirstOrDefault();

                if ( gl == null || gl.Location == null )
                {
                    return NotFound();
                }

                var loc = gl.Location;

                return Ok( new GroupLocationDto
                {
                    GroupId = group.Id,
                    GroupLocationId = gl.Id,
                    LocationId = loc.Id,
                    Street1 = loc.Street1,
                    Street2 = loc.Street2,
                    City = loc.City,
                    State = loc.State,
                    PostalCode = loc.PostalCode,
                    Country = loc.Country,
                    FormattedAddress = loc.FormattedAddress,
                    Latitude = loc.GeoPoint != null ? ( double? ) loc.GeoPoint.Latitude : null,
                    Longitude = loc.GeoPoint != null ? ( double? ) loc.GeoPoint.Longitude : null,
                    GroupLocationTypeValueId = gl.GroupLocationTypeValueId
                } );
            }
        }

        /// <summary>
        /// Update the group's location
        /// </summary>
        [HttpPut]
        [System.Web.Http.Route( "api/GroupApp/Groups/{groupId:int}/Location" )]
        public IHttpActionResult UpsertGroupLocation( int groupId, [FromBody] UpdateLocationRequest request )
        {
            if ( request == null )
            {
                return BadRequest( "Request body is required." );
            }

            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            // Minimal validation
            if ( string.IsNullOrWhiteSpace( request.Street1 )
                || string.IsNullOrWhiteSpace( request.City )
                || string.IsNullOrWhiteSpace( request.State )
                || string.IsNullOrWhiteSpace( request.PostalCode ) )
            {
                return BadRequest( "Street1, City, State, and PostalCode are required." );
            }

            using ( var rockContext = new RockContext() )
            {
                rockContext.Configuration.ProxyCreationEnabled = false;

                var groupService = new GroupService( rockContext );
                var locationService = new LocationService( rockContext );
                var glService = new GroupLocationService( rockContext );

                var group = groupService.Get( groupId );
                if ( group == null )
                {
                    return NotFound();
                }

                if ( !IsLeaderOfGroup( rockContext, currentUser.Person.Id, groupId ) )
                {
                    return StatusCode( HttpStatusCode.Forbidden );
                }

                // Remove ALL existing group locations (single location app requirement)
                var existingGroupLocations = glService.Queryable()
                    .Where( gl => gl.GroupId == groupId )
                    .ToList();

                foreach ( var gl in existingGroupLocations )
                {
                    glService.Delete( gl );
                }

                // Create a new Location similar to how GroupDetail.ascx.cs handles it
                var newLocation = new Location
                {
                    Street1 = request.Street1.Trim(),
                    Street2 = string.IsNullOrWhiteSpace( request.Street2 ) ? null : request.Street2.Trim(),
                    City = request.City.Trim(),
                    State = request.State.Trim(),
                    PostalCode = request.PostalCode.Trim(),
                    Country = string.IsNullOrWhiteSpace( request.Country ) ? null : request.Country.Trim(),
                    IsActive = true
                };

                // Let Rock handle automatic address standardization and geocoding
                // This is the key difference - Rock will automatically set GeoPoint and FormattedAddress
                // when you call Verify or when the location is saved (via PreSaveChanges)
                locationService.Verify( newLocation, true );

                // Add the location to the context
                locationService.Add( newLocation );
                rockContext.SaveChanges();

                // Associate as the only GroupLocation, prefer Meeting Location; fall back to Home
                var typeId = request.GroupLocationTypeValueId
                             ?? GetMeetingLocationTypeId( rockContext )
                             ?? GetHomeLocationTypeId( rockContext );

                var groupLocation = new GroupLocation
                {
                    GroupId = group.Id,
                    LocationId = newLocation.Id,
                    GroupLocationTypeValueId = typeId,
                    IsMappedLocation = true // This is important for showing on maps
                };

                glService.Add( groupLocation );
                rockContext.SaveChanges();

                return Ok( new GroupLocationDto
                {
                    GroupId = group.Id,
                    GroupLocationId = groupLocation.Id,
                    LocationId = newLocation.Id,
                    Street1 = newLocation.Street1,
                    Street2 = newLocation.Street2,
                    City = newLocation.City,
                    State = newLocation.State,
                    PostalCode = newLocation.PostalCode,
                    Country = newLocation.Country,
                    FormattedAddress = newLocation.FormattedAddress, // This will be populated by Rock's verification
                    Latitude = newLocation.GeoPoint?.Latitude,
                    Longitude = newLocation.GeoPoint?.Longitude,
                    GroupLocationTypeValueId = groupLocation.GroupLocationTypeValueId
                } );
            }
        }

        private static bool IsMemberOfGroup( RockContext rockContext, int personId, int groupId )
        {
            var gmService = new GroupMemberService( rockContext );
            return gmService.Queryable()
                .Any( gm =>
                    gm.GroupId == groupId
                    && gm.PersonId == personId
                    && gm.IsArchived == false
                    && gm.GroupMemberStatus == GroupMemberStatus.Active );
        }

        private static bool IsLeaderOfGroup( RockContext rockContext, int personId, int groupId )
        {
            var gmService = new GroupMemberService( rockContext );
            return gmService.Queryable()
                .Any( gm =>
                    gm.GroupId == groupId
                    && gm.PersonId == personId
                    && gm.IsArchived == false
                    && gm.GroupMemberStatus == GroupMemberStatus.Active
                    && gm.GroupRole.IsLeader );
        }

        private static int? GetMeetingLocationTypeId( RockContext rockContext )
        {
            var dvs = new DefinedValueService( rockContext );
            var dv = dvs.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION.AsGuid() );
            return dv?.Id;
        }

        private static int? GetHomeLocationTypeId( RockContext rockContext )
        {
            var dvs = new DefinedValueService( rockContext );
            var dv = dvs.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() );
            return dv?.Id;
        }

        public class UpdateLocationRequest
        {
            public string Street1 { get; set; }
            public string Street2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public int? GroupLocationTypeValueId { get; set; } // Optional override (e.g., Meeting/Home)
        }

        public class GroupLocationDto
        {
            public int GroupId { get; set; }
            public int GroupLocationId { get; set; }
            public int LocationId { get; set; }
            public string Street1 { get; set; }
            public string Street2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string PostalCode { get; set; }
            public string Country { get; set; }
            public string FormattedAddress { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public int? GroupLocationTypeValueId { get; set; }
        }
    }
}
