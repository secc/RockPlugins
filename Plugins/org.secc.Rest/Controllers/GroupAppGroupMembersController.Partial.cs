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
using System.Net;
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace org.secc.Rest.Controllers
{
    public partial class GroupAppGroupMembersController : ApiControllerBase
    {
        private readonly DefinedValueService _definedValueService;
        private readonly GroupService _groupService;
        private readonly GroupMemberService _groupMemberService;
        private readonly PersonService _personService;
        private readonly RockContext _context;

        public GroupAppGroupMembersController()
        {
            _context = new RockContext();
            _definedValueService = new DefinedValueService( _context );
            _groupService = new GroupService( _context );
            _groupMemberService = new GroupMemberService( _context );
            _personService = new PersonService( _context );
        }

        /// <summary>
        /// Gets the members of the provided group.
        /// </summary>
        /// <param name="groupId">The group ID</param>
        /// <returns></returns> 
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/GetGroupMembers/{groupId}" )]
        public IHttpActionResult GetGroupMembers( int groupId )
        {
            var currentUser = UserLoginService.GetCurrentUser();

            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            var group = _groupService.Get( groupId );
            if ( group == null )
            {
                return NotFound();
            }

            if ( !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
            {
                return StatusCode( HttpStatusCode.Forbidden );
            }

            var groupMemberList = new List<GroupAppGroupMember>();

            var homeLocationTypeId = _definedValueService.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;

            var groupMembers = _groupMemberService.GetByGroupId( groupId )
                .ToList();

            var orderedGroupMembers = groupMembers
                .OrderByDescending( gm => gm.GroupRole.IsLeader )
                .ThenBy( gm => gm.Person.LastName )
                .ThenBy( gm => gm.Person.NickName )
                .ToList();

            foreach ( var groupMember in orderedGroupMembers )
            {
                var person = _personService.Get( groupMember.PersonId );
                var familyGroup = person.GetFamily();

                var groupAppGroupMember = new GroupAppGroupMember
                {
                    Id = person.Id,
                    Name = person.FullName,
                    GroupRole = groupMember.GroupRole.Name,
                    Status = groupMember.GroupMemberStatus.ToString(),
                    Address = familyGroup.GroupLocations
                        .FirstOrDefault( gl => gl.GroupLocationTypeValueId == homeLocationTypeId )
                        ?.Location.GetFullStreetAddress(),
                    Email = person.Email,
                    Phone = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).ToString(),
                    PhotoId = person.PhotoId ?? 0
                };

                groupMemberList.Add( groupAppGroupMember );
            }

            return Ok( groupMemberList );
        }
    }
}