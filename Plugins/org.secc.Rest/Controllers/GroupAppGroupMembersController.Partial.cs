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
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Rest.Controllers;
//using Rock.SystemGuid;
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
                    Phone = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid())?.ToString(),
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

            if ( message == null || message.FromAddress.IsNullOrWhiteSpace() || message.Subject.IsNullOrWhiteSpace() || message.Body.IsNullOrWhiteSpace() )
            {
                return BadRequest( "Invalid request. Please provide a valid 'FromAddress', 'Subject', and 'Body' in the message." );
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

            CreateCommunication( message.Subject, message.Body, groupMembers, currentUser.Person );

            return Ok();
        }

        public class MessageModel
        {
            public string FromAddress { get; set; }
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

        /// <summary>
        /// Adds group members to the provided group.
        /// <param name="groupId">The group ID</param>        
        /// </summary>
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/GroupMembers/{groupId}/Add" )]
        public IHttpActionResult AddGroupMembers( int groupId, [FromBody] GroupAppAddGroupMember personToAdd )
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

            if ( personToAdd == null || ( personToAdd.FirstName.IsNullOrWhiteSpace() || personToAdd.LastName.IsNullOrWhiteSpace() ) || ( personToAdd.DateOfBirth == null && string.IsNullOrWhiteSpace( personToAdd.Email ) && personToAdd.MobileNumber.IsNullOrWhiteSpace() ) )
            {
                return BadRequest( "Invalid request. Please provide a valid First Name and Last Name and/or a valid Date of Birth, Email, or Mobile Number." );
            }

            Person person = null;

            var phoneNumber = personToAdd.MobileNumber.IsNullOrWhiteSpace() ? null : personToAdd.MobileNumber.AsNumeric();

            // look to see if there are any people in the database matching the info provided: name + either DOB, email, or mobile number
            var personQuery = new PersonService( _context ).Queryable();
            personQuery = personQuery.Where( p => p.LastName == personToAdd.LastName && (p.FirstName == personToAdd.FirstName || p.NickName == personToAdd.FirstName) );
            if ( personToAdd.DateOfBirth != null )
            {
                personQuery = personQuery.Where( p => p.BirthDate == personToAdd.DateOfBirth );
            }
            if ( !string.IsNullOrWhiteSpace( personToAdd.Email ) )
            {
                personQuery = personQuery.Where( p => p.Email == personToAdd.Email );
            }
            if ( !string.IsNullOrWhiteSpace( phoneNumber ) )
            {
                personQuery = personQuery.Where( p => p.PhoneNumbers.Any( n => n.Number == phoneNumber ) );
            }

            person = personQuery.FirstOrDefault();

            if ( person == null )
            {
                // if no person was found, create a new person
                person = new Person
                {
                    FirstName = personToAdd.FirstName,
                    LastName = personToAdd.LastName,                    
                    Email = personToAdd.Email,
                    IsEmailActive = true,
                    EmailPreference = EmailPreference.EmailAllowed,
                    ReviewReasonNote = "Added via GroupApp",
                    RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id,
                    ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() ).Id,
                    RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id,
                    Gender = Gender.Unknown,
                    CreatedByPersonAliasId = currentUser.Person.PrimaryAliasId,
                };
                person.UpdatePhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id,
                        PhoneNumber.DefaultCountryCode(), phoneNumber, true, false, _context );
                person.SetBirthDate( personToAdd.DateOfBirth );

                // Save the person
                var personService = new PersonService( _context );
                PersonService.SaveNewPerson( person, _context, group.CampusId, false );
                _context.SaveChanges();
            }

            // if person already in the group return bad request
            if ( _groupMemberService.GetByGroupId( groupId ).Any( gm => gm.PersonId == person.Id ) )
            {
                return BadRequest( "Person is already a member of the group." );
            }

            // add person to the group
            var groupMember = new GroupMember
            {
                GroupId = groupId,
                PersonId = person.Id,
                GroupRoleId = group.GroupType.DefaultGroupRoleId ?? group.GroupType.Roles.FirstOrDefault().Id,
                GroupMemberStatus = GroupMemberStatus.Active,
                DateTimeAdded = RockDateTime.Now,
                CreatedByPersonAliasId = currentUser.Person.PrimaryAliasId
            };

            var groupMemberService = new GroupMemberService( _context );
            var member = groupMemberService.AddOrRestoreGroupMember( group, person.Id, groupMember.GroupRoleId );
            _context.SaveChanges();

            // Add Table Number
            // check if the group has a group member attribute for table number

            var currentGroupMember = groupMemberService.GetByPersonId( ( int ) currentUser.PersonId ).AsQueryable().AsNoTracking()
                        .Where( gm => gm.GroupId == group.Id ).FirstOrDefault();
            currentGroupMember.LoadAttributes();
            var currentGroupMemberTableNumber = currentGroupMember.GetAttributeValue( "TableNumber" );
            if ( currentGroupMemberTableNumber != null )
            {
                member.LoadAttributes();
                member.SetAttributeValue( "TableNumber", currentGroupMemberTableNumber );
                member.SaveAttributeValue( "TableNumber" );
            }

            return Ok();
        }
    }

    public class GroupAppAddGroupMember
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
    }

}