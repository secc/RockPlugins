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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using System;
using org.secc.Rest.Models;

namespace org.secc.Rest.Controllers
{
    public partial class GroupAppGroupListController : ApiControllerBase
    {
        /// <summary>
        /// Returns the groups for which the current user is a group member with a role including "leader" from the group types defined in the "Group App Group Type
        /// </summary>
        /// <returns>List<Group></returns> 
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/GroupList/" )]
        public IHttpActionResult GetGroupList()
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            var groupServiceHelper = new GroupServiceHelper( rockContext );
            var groupTypeIds = groupServiceHelper.GetGroupTypeIdsFromDefinedType( "Group App Group Types" );
            var groupsAsLeader = groupServiceHelper.GetGroupsAsLeader( currentUser.Person.Id, groupTypeIds );

            return Ok( groupsAsLeader );

        }
    }

    public class GroupServiceHelper
    {
        private readonly RockContext _rockContext;

        public GroupServiceHelper( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        public List<int> GetGroupTypeIdsFromDefinedType( string definedTypeName )
        {
            var definedTypeService = new DefinedTypeService( _rockContext );
            var groupTypeDefinedType = definedTypeService
                .Queryable()
                .FirstOrDefault( dt => dt.Name == definedTypeName );

            if ( groupTypeDefinedType == null )
                return new List<int>();

            var definedValueService = new DefinedValueService( _rockContext );
            var groupTypeDefinedValues = definedValueService
                .Queryable()
                .Where( dv => dv.DefinedTypeId == groupTypeDefinedType.Id )
                .ToList();

            return groupTypeDefinedValues.Select( dv => int.Parse( dv.Value ) ).ToList();
        }

        public List<GroupAppGroup> GetGroupsAsLeader( int currentPersonId, List<int> groupTypeIds )
        {
            return new GroupMemberService( _rockContext )
                .Queryable( "Group, GroupRole" )
                .Where( gm => gm.PersonId == currentPersonId &&
                             gm.GroupRole.IsLeader &&
                             groupTypeIds.Contains( gm.Group.GroupTypeId ) )
                .Select( gm => new GroupAppGroup
                {
                    Id = gm.Group.Id,
                    Name = gm.Group.Name,
                    IsActive = gm.Group.IsActive,
                    IsArchived = gm.Group.IsArchived
                } )
                .Distinct()
                .ToList();
        }
    }
}
