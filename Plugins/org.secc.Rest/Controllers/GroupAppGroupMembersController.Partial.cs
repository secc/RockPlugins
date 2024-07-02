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
using System.Linq;
using System.Net;
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;

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
                .OrderByDescending( gm => gm.GroupRole.IsLeader )
                .ThenBy( gm => gm.Person.LastName )
                .ThenBy( gm => gm.Person.NickName )
                .ToList();

            foreach ( var groupMember in groupMembers )
            {
                var person = _personService.Get( groupMember.PersonId );
                var familyGroup = person.GetFamily();

                var groupAppGroupMember = new GroupAppGroupMember
                {
                    Id = groupMember.Id,
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

        /// <summary>
        /// Creates a communication for all group members or one specific group member (indicated by groupMemberId).
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="groupMemberId"></param>
        /// 
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/GroupMembers/{groupId}/Communicate" )]
        public IHttpActionResult Communicate( int groupId, [FromBody] MessageModel message, int? groupMemberId = null )
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

            if ( !group.IsAuthorized( Rock.Security.Authorization.EDIT, currentUser.Person ) || !group.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, currentUser.Person ) )
            {
                return StatusCode( HttpStatusCode.Forbidden );
            }

            if ( groupMemberId != null )
            {
                var groupMember = _groupMemberService.Get( groupMemberId.Value );
                if ( groupMember == null )
                {
                    return NotFound();
                }
            }
            var groupMembers = _groupMemberService.GetByGroupId( groupId ).ToList();

            if ( groupMemberId.HasValue )
            {
                groupMembers = groupMembers.Where( gm => gm.Id == groupMemberId ).ToList();
            }

            CreateCommunication( message.Subject, message.Body, groupMembers, currentUser.Person);

            return Ok();
        }

        public class MessageModel
        {
            public string Subject { get; set; }
            public string Body { get; set; }
        }

        public void CreateCommunication( string subject, string body, List<GroupMember> groupMembers, Person currentPerson )
        {
            var communication = UpdateCommunication( _context );
            if ( communication != null )
            {
                communication.CommunicationType = CommunicationType.Email;
                communication.IsBulkCommunication = false;
                communication.FutureSendDateTime = null;
                communication.CreatedByPersonAliasId = currentPerson.PrimaryAliasId;
                communication.ReplyToEmail = currentPerson.Email;
                communication.SenderPersonAliasId = currentPerson.PrimaryAliasId;
                communication.FromName = currentPerson.FullName;
                communication.FromEmail = currentPerson.Email;
                communication.Subject = subject;
                communication.Message = body;
                communication.ReviewedDateTime = RockDateTime.Now;
                communication.FutureSendDateTime = RockDateTime.Now;
                communication.ReviewerPersonAliasId = currentPerson.PrimaryAliasId;
                communication.Status = CommunicationStatus.Approved;

                foreach ( var groupMember in groupMembers )
                {
                    var recipient = new CommunicationRecipient
                    {                        
                        PersonAliasId = groupMember.Person.PrimaryAliasId,
                        CreatedByPersonAliasId = currentPerson.PrimaryAliasId,
                        Communication = communication,
                        MediumEntityTypeId = EntityTypeCache.Get( new Guid( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_EMAIL ) ).Id
                    };

                    communication.Recipients.Add( recipient );
                }

                _context.SaveChanges();
                var transaction = new Rock.Transactions.SendCommunicationTransaction();
                transaction.CommunicationId = communication.Id;
                transaction.PersonAlias = currentPerson.PrimaryAlias;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
            }
            return;
        }

        private Communication UpdateCommunication( RockContext context )
        {
            var communicationService = new CommunicationService( context );
            var recipientService = new CommunicationRecipientService( context );

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            communication = new Rock.Model.Communication();
            communicationService.Add( communication );

            qryRecipients = communication.GetRecipientsQry( context );

            communication.IsBulkCommunication = false;

            communication.FutureSendDateTime = null;

            return communication;
        }




    }

}