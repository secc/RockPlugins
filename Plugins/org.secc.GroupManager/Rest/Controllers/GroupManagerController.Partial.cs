using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.GroupManager.Rest.Controllers
{
    public partial class GroupManagerController : ApiControllerBase
    {
        // Bounds how many groups a single anonymous request can send to Azure Maps, capping billed matrix size / cost.
        private const int MaxMatrixDestinations = 1000;

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/groupmanager/homegroups/{groupTypeId}/{zipcode}" )]
        public async Task<Dictionary<string, string>> HomeGroupsByDistance( int groupTypeId, string zipcode )
        {
            if ( zipcode.Length != 5 || zipcode.AsInteger() == 0 )
            {
                throw new ArgumentException( "Postal code must be 5 integers" );
            }

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var groups = groupService.GetByGroupTypeId( groupTypeId ).Where( g => g.IsActive && g.IsPublic && !g.IsArchived ).Take( MaxMatrixDestinations );

            var meetingTypeIds = new List<int> {
                DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_MEETING_LOCATION.AsGuid() ).Id,
                DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id
            };
            var destinations = await Mapping.Utilities.GroupUtilities.GetGroupsDestinations( zipcode, groups, rockContext, meetingTypeIds );

            return destinations.ToDictionary( d => d.EntityId.ToString(), d => d.TravelDistance.ToString() );
        }
    }
}
