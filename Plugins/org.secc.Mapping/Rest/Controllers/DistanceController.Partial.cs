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
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using org.secc.Mapping.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;
using Rock.Web.Cache;

namespace org.secc.Mapping.Rest.Controllers
{
    public partial class DistanceController : ApiControllerBase
    {

        [Authenticate]
        [HttpGet]
        [System.Web.Http.Route( "api/mapping/distance/{definedValueId}/{address}" )]
        public async Task<object> GetDistance( int definedValueId, string address )
        {
            var mappable = DefinedValueCache.Get( definedValueId );
            if ( mappable == null || mappable.DefinedType.Guid != Constants.UniversalDefinedTypeGuid.AsGuid() )
            {
                return BadRequest( "Non valid distance entity" );
            }

            var entityKey = mappable.GetAttributeValue( "EntityType" );

            switch ( entityKey )
            {
                case "Campus":
                    return await CampusUtilities.OrderCampusesByDistance( address );
                case "ParentGroup":
                    return await GetChildGroupDistances( mappable.GetAttributeValue( "EntityId" ).AsInteger(), address );
                case "GroupType":
                    return await GetGroupTypeDistances( mappable.GetAttributeValue( "EntityId" ).AsInteger(), address );
                case "Attribute":
                    return await GetAttributeDistances( mappable.GetAttributeValue( "EntityId" ).AsInteger(), address );
                default:
                    break;
            }


            return BadRequest( "Unmappable Entity" );
        }

        private async Task<object> GetAttributeDistances( int attributeId, string address )
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            LocationService locationService = new LocationService( rockContext );

            var attributeValues = attributeValueService.Queryable()
                .Where( av => av.AttributeId == attributeId && !string.IsNullOrEmpty( av.Value ) )
                .ToList();

            var locationGuids = attributeValues
                .Select( a => a.Value.AsGuidOrNull() )
                .Where( g => g != null )
                .ToList();

            var locations = locationService.Queryable()
                .Where( l => locationGuids.Contains( l.Guid ) )
                .ToList();

            var destinations = attributeValues
                .Select( av => new Destination
                {
                    EntityId = av.Id,
                    LocationId = locations.Where( l => l.Guid == av.Value.AsGuid() ).Select( l => l.Id ).FirstOrDefault()
                } )
                .Where( d => d.LocationId.HasValue && d.EntityId.HasValue )
                .ToList();
            var output = await BingDistanceMatrix.OrderDestinations( address, destinations );
            return output.ToDictionary( d => d.EntityId.ToString(), d => d.TravelDistance.ToString() );
        }

        private async Task<object> GetGroupTypeDistances( int groupTypeId, string address )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );

            var groups = groupService.Queryable().Where( g => g.IsActive && !g.IsArchived && g.IsPublic && g.GroupTypeId == groupTypeId );
            var output = await GroupUtilities.GetGroupsDestinations( address, groups.AsQueryable<Group>(), rockContext );
            return output.ToDictionary( d => d.EntityId.ToString(), d => d.TravelDistance.ToString() );
        }

        private async Task<object> GetChildGroupDistances( int parentGroupId, string address )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );

            var groups = groupService.Queryable().Where( g => g.IsActive && !g.IsArchived && g.IsPublic && g.ParentGroupId == parentGroupId );
            var output = await GroupUtilities.GetGroupsDestinations( address, groups, rockContext );
            return output.ToDictionary( d => d.EntityId.ToString(), d => d.TravelDistance.ToString() );
        }
    }
}
