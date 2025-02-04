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
using Rock.Tasks;
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

            bool isGroupMember = _groupMemberService.Queryable()
                .Any( gm => gm.GroupId == group.Id
                    && gm.PersonId == currentUser.Person.Id
                    && gm.IsArchived == false
                    && gm.GroupMemberStatus == GroupMemberStatus.Active );

            if ( isGroupMember || group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
            {


                var groupMemberServiceHelper = new GroupMemberServiceHelper( _context );
                var groupMembers = groupMemberServiceHelper.GetGroupMembers( group, currentUser.Person );
                var currentUserGroupMember = groupMembers.FirstOrDefault( gm => gm.PersonId == currentUser.Person.Id );
                var isCurrentUserLeader = currentUserGroupMember?.GroupRole.IsLeader ?? false;

                var groupMemberList = new List<GroupAppGroupMember>();

                var tableBasedGroupTypeIds = _definedValueService
                 .GetByDefinedTypeGuid( new Guid( "90526a36-fda6-4c90-997c-636b82b793d8" ) )
                 .ToList();

                var parsedGroupTypeIds = new List<int>();
                foreach ( var dv in tableBasedGroupTypeIds )
                {
                    if ( int.TryParse( dv.Value, out int groupTypeId ) )
                    {
                        parsedGroupTypeIds.Add( groupTypeId );
                    }
                }

                var isTableBasedGroup = parsedGroupTypeIds.Contains( group.GroupTypeId );

                if ( !isCurrentUserLeader && isTableBasedGroup )
                {
                    var tableNumberAttribute = currentUser.Person.GetAttributeValue( "TableNumber" );
                    if ( string.IsNullOrEmpty( tableNumberAttribute ) )
                    {
                        return Ok( "You are not assigned to a table" );
                    }

                    groupMembers = groupMembers.Where( gm => gm.Person.GetAttributeValue( "TableNumber" ) == tableNumberAttribute ).ToList();
                }

                var homeLocationTypeId = _definedValueService.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;

                foreach ( var groupMember in groupMembers )
                {
                    var person = _personService.Get( groupMember.PersonId );
                    var familyGroup = person.GetFamily();

                    var groupAppGroupMember = new GroupAppGroupMember
                    {
                        Id = groupMember.Id,
                        Name = person.FullName,
                        GroupRole = groupMember.GroupRole.IsLeader ? "Leader" : "Member",
                        Status = isCurrentUserLeader ? groupMember.GroupMemberStatus.ToString() : null,
                        Address = isCurrentUserLeader ? familyGroup.GroupLocations
                            .FirstOrDefault( gl => gl.GroupLocationTypeValueId == homeLocationTypeId )
                            ?.Location.GetFullStreetAddress() : null,
                        Email = isCurrentUserLeader ? person.Email : null,
                        Phone = isCurrentUserLeader ? person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() )?.ToString() : null,
                        PhotoURL = person.PhotoUrl,
                        IsLeader = groupMember.GroupRole.IsLeader
                    };

                    groupMemberList.Add( groupAppGroupMember );
                }

                return Ok( groupMemberList );
            }
            else
            {
                return StatusCode( HttpStatusCode.Forbidden );
            }
        }

        /// <summary>
        /// Creates a communication for all group members or one specific group member (indicated by groupMemberId).
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="groupMemberId"></param>
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/GroupMembers/{groupId}/Communicate" )]
        public IHttpActionResult Communicate( int groupId, [FromBody] MessageModel message )
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

            if ( message == null || message.Subject.IsNullOrWhiteSpace() || message.Body.IsNullOrWhiteSpace() )
            {
                return BadRequest( "Invalid request. Please provide a valid 'Subject' and 'Body' in the message." );
            }

            if ( message.GroupMemberId != 0 )
            {
                var groupMember = _groupMemberService.Get( message.GroupMemberId );
                if ( groupMember == null )
                {
                    return NotFound();
                }
            }

            if ( message.SendToParents )
            {
                group.LoadAttributes();
                var groupContentItems = new List<GroupContentItem>();
                bool? emailParentsEnabled = null;
                emailParentsEnabled = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
                if ( emailParentsEnabled == false )
                {
                    return BadRequest( "Invalid request. \"Allow Email Parents\" is not configured for this group." );
                }
            }

            var groupMemberServiceHelper = new GroupMemberServiceHelper( _context );
            var recipients = groupMemberServiceHelper.GetRecipients( groupId, message.GroupMemberId, message.SendToParents );

            CreateCommunication( message.Subject, message.Body, recipients, currentUser.Person, group );

            return Ok( recipients.AsQueryable().Select( r => r.FullName ) );
        }

        public class MessageModel
        {
            public string Subject { get; set; }
            public string Body { get; set; }
            public int GroupMemberId { get; set; }
            public bool SendToParents { get; set; } = false;
        }

        public void CreateCommunication( string subject, string body, List<Person> recipients, Person currentPerson, Group group )
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
                communication.FromEmail = "noreply@secc.org";
                communication.Subject = "SE Groups: " + subject;
                communication.Message = "{{ 'Global' | Attribute:'EmailHeader' }}" + $"<h2>Message from {currentPerson.FullName} via {group.Name} group:</h2><br><br>" + body.Replace( "\n", "<br>" ).Replace( "\r", "<br>" ) + "{{'Global' | Attribute:'EmailFooter'}}";
                communication.ReviewedDateTime = RockDateTime.Now;
                communication.FutureSendDateTime = RockDateTime.Now;
                communication.ReviewerPersonAliasId = currentPerson.PrimaryAliasId;
                communication.Status = CommunicationStatus.Approved;

                foreach ( var r in recipients )
                {
                    var recipient = new CommunicationRecipient
                    {
                        PersonAliasId = r.PrimaryAliasId,
                        CreatedByPersonAliasId = currentPerson.PrimaryAliasId,
                        Communication = communication,
                        MediumEntityTypeId = EntityTypeCache.Get( new Guid( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_EMAIL ) ).Id
                    };

                    communication.Recipients.Add( recipient );
                }

                _context.SaveChanges();
                var transaction = new ProcessSendCommunication.Message();
                transaction.CommunicationId = communication.Id;
                transaction.Send();
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
            personQuery = personQuery.Where( p => p.LastName == personToAdd.LastName && ( p.FirstName == personToAdd.FirstName || p.NickName == personToAdd.FirstName ) );
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
                    ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT.AsGuid() ).Id,
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

            var groupMemberService = new GroupMemberService( _context );

            // is person already in the group?            
            var groupMember = _groupMemberService.GetByGroupId( groupId ).Where( gm => gm.PersonId == person.Id && gm.IsArchived == false ).FirstOrDefault();

            // create a new group member
            if ( groupMember == null )
            {
                groupMember = new GroupMember
                {
                    GroupId = groupId,
                    PersonId = person.Id,
                    GroupRoleId = group.GroupType.DefaultGroupRoleId ?? group.GroupType.Roles.FirstOrDefault().Id,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    DateTimeAdded = RockDateTime.Now,
                    CreatedByPersonAliasId = currentUser.Person.PrimaryAliasId
                };

                groupMember = groupMemberService.AddOrRestoreGroupMember( group, person.Id, groupMember.GroupRoleId );
                _context.SaveChanges();
            }



            // Add Table Number
            // check if the group has a group member attribute for table number

            var currentGroupMember = groupMemberService.GetByPersonId( ( int ) currentUser.PersonId ).AsQueryable().AsNoTracking()
                        .Where( gm => gm.GroupId == group.Id ).FirstOrDefault();
            currentGroupMember.LoadAttributes();
            var currentGroupMemberTableNumber = currentGroupMember.GetAttributeValue( "TableNumber" );
            if ( currentGroupMemberTableNumber != null )
            {
                groupMember.LoadAttributes();
                groupMember.SetAttributeValue( "TableNumber", currentGroupMemberTableNumber );
                groupMember.SaveAttributeValues();
            }

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Removes a group member from the provided group.
        /// <param name="groupId">The group ID</param>
        /// <param name="groupMemberId">The group member ID</param>"
        /// 
        [HttpDelete]
        [System.Web.Http.Route( "api/GroupApp/GroupMembers/{groupId}/Remove/{groupMemberId}" )]
        public IHttpActionResult RemoveGroupMember( int groupId, int groupMemberId )
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

            if ( groupMemberId > 0 )
            {
                var _rockContext = new RockContext();
                var groupMemberService = new GroupMemberService( _rockContext );
                var groupMember = groupMemberService.Get( groupMemberId );
                if ( groupMember == null )
                {
                    return NotFound();
                }
                groupMemberService.Delete( groupMember );
                _rockContext.SaveChanges();
                return Ok();
            }
            else
            {
                return BadRequest( "Invalid request. Please provide a valid group member ID." );
            }

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

    public class GroupMemberServiceHelper
    {
        private readonly RockContext _rockContext;

        public GroupMemberServiceHelper( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        public List<Person> GetRecipients( int groupId, int? groupMemberId, bool sendToParents )
        {
            var groupMemberService = new GroupMemberService( _rockContext );
            var recipients = new List<Person>();

            if ( groupMemberId != 0 )
            {
                var groupMember = groupMemberService.Get( ( int ) groupMemberId );

                if ( sendToParents )
                {
                    var adults = GetParents( groupMember.Person );
                    recipients.AddRange( adults );
                }
                else
                {
                    recipients.Add( groupMember.Person );
                }
            }
            else
            {
                var groupService = new GroupService( _rockContext );
                // Get the group members of the specified groupId
                var groupMembers = GetGroupMembers( groupService.Get( groupId ) );
                if ( sendToParents )
                {

                    // Iterate through each group member
                    foreach ( var groupMember in groupMembers )
                    {
                        // Get the parents of the current group member
                        var groupMemberParents = GetParents( groupMember.Person );

                        // Add the parents to the list
                        recipients.AddRange( groupMemberParents );
                    }
                }
                else
                {
                    foreach ( var groupMember in groupMembers )
                    {
                        recipients.Add( groupMember.Person );
                    }
                }
            }

            return recipients;
        }

        public List<Person> GetParents( Person person )
        {
            var parents = new List<Person>();
            var families = person.GetFamilies().ToList();
            var adultGuid = new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT );
            foreach ( var family in families )
            {
                var familyRoleGuid = family.Members.Where( gm => gm.PersonId == person.Id ).FirstOrDefault().GroupRole.Guid;
                if ( familyRoleGuid != adultGuid )
                {
                    parents.AddRange( family.Members.Where( m => m.GroupRole.Guid == adultGuid ).Select( m => m.Person ).ToList() );
                }
            }
            return parents;
        }

        public List<GroupMember> GetGroupMembers( Group group, Person currentPerson = null )
        {
            var groupMemberService = new GroupMemberService( _rockContext );
            var groupMembers = new List<GroupMember>();

            if ( currentPerson != null )
            {
                var currentGroupMember = groupMemberService.GetByPersonId( currentPerson.Id ).AsQueryable().AsNoTracking()
                    .Where( groupmember => groupmember.GroupId == group.Id ).FirstOrDefault();

                if ( currentGroupMember != null )
                {
                    currentGroupMember.LoadAttributes();
                    var currentGroupMemberTableNumber = currentGroupMember.GetAttributeValue( "TableNumber" );

                    if ( currentGroupMemberTableNumber != null )
                    {
                        var tableNumberAttributeIds = new AttributeService( _rockContext )
                        .Queryable()
                        .Where( a => a.Key == "TableNumber" )
                        .Select( a => a.Id )
                        .ToList();

                        groupMembers = groupMemberService.GetByGroupId( group.Id )
                            .Join( new AttributeValueService( _rockContext ).Queryable(),
                                    gm => gm.Id,
                                    av => av.EntityId,
                                    ( gm, av ) => new { GroupMember = gm, AttributeValue = av } )
                            .Where( x => tableNumberAttributeIds.Contains( x.AttributeValue.AttributeId ) && x.AttributeValue.Value == currentGroupMemberTableNumber && x.GroupMember.IsArchived == false )
                            .Select( x => x.GroupMember )
                            .OrderByDescending( gm => gm.GroupRole.IsLeader )
                            .ThenBy( gm => gm.Person.LastName )
                            .ThenBy( gm => gm.Person.NickName )
                            .ToList();

                        return groupMembers;
                    }
                }
            }

            groupMembers = groupMemberService.GetByGroupId( group.Id )
                .Where( gm => gm.IsArchived == false )
                .OrderByDescending( gm => gm.GroupRole.IsLeader )
                .ThenBy( gm => gm.Person.LastName )
                .ThenBy( gm => gm.Person.NickName )
                .ToList();

            return groupMembers;
        }
    }
}