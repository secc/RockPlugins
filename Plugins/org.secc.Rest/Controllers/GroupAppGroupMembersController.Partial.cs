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

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Filters;

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
            var group = _groupService.Get( groupId );
            var currentUser = UserLoginService.GetCurrentUser();

            if ( currentUser == null )
            {
                return ( IHttpActionResult ) ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );
            }

            if ( !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
            {
                throw new HttpResponseException( HttpStatusCode.Unauthorized );
            }

            var groupMemberList = new List<GroupAppGroupMember>();
                        
            var homeLocationTypeId = _definedValueService.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;

            var groupMembers = _groupMemberService.Queryable()
                .Where( gm => gm.GroupId == groupId )
                .ToList();

            foreach ( var groupMember in groupMembers )
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