// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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

                // For table-based groups this is the caller's own table; a caller with no
                // table (not a member, or TableNumber blank) sees nothing.
                var scope = groupMemberServiceHelper.GetScopedGroupMembers( group, currentUser.Person );
                if ( !scope.SenderHasScope )
                {
                    return Ok( GroupMemberServiceHelper.NotAssignedToTableMessage );
                }

                var groupMembers = scope.Members;
                var currentUserGroupMember = groupMembers.FirstOrDefault( gm => gm.PersonId == currentUser.Person.Id );
                var isCurrentUserLeader = currentUserGroupMember?.GroupRole.IsLeader ?? false;

                var groupMemberList = new List<GroupAppGroupMember>();

                var homeLocationTypeId = _definedValueService.GetByGuid( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() ).Id;

                foreach ( var groupMember in groupMembers )
                {
                    var person = _personService.Get( groupMember.PersonId );
                    var familyGroup = person.GetFamily();
                    // AgeClassification folds in birthdate, family role, and IsLockedAsChild,
                    // so it also catches minors with no birthdate on record. Businesses are
                    // skipped by Rock's classifier (stay Unknown), hence == Child, not != Adult.
                    var isMinor = person.AgeClassification == AgeClassification.Child;

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
                        IsLeader = groupMember.GroupRole.IsLeader,
                        IsCurrentUser = groupMember.PersonId == currentUser.Person.Id,
                        // Age-derived info is leader-gated like the other PII fields.
                        IsMinor = isCurrentUserLeader && isMinor
                    };

                    // Check if the person is a minor (under 18) and get parent information
                    // Only include parent information if the current user is a group leader
                    if ( isCurrentUserLeader && isMinor )
                    {
                        var parents = groupMemberServiceHelper.GetParents( person );
                        foreach ( var parent in parents )
                        {
                            var parentInfo = new GroupAppParent
                            {
                                Name = parent.FullName,
                                Phone = parent.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() )?.ToString(),
                                Email = parent.Email
                            };
                            groupAppGroupMember.Parents.Add( parentInfo );
                        }
                    }

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

            if ( message.SendToParents )
            {
                group.LoadAttributes();
                if ( !group.GetAttributeValue( "AllowEmailParents" ).AsBoolean() )
                {
                    return BadRequest( "Invalid request. \"Allow Email Parents\" is not configured for this group." );
                }
            }

            // Scope recipients to the caller's view of the group. For table-based
            // groups the whole study is one Rock group and "table" is a group member
            // attribute; a caller with no table cannot email anyone (ROCK-9151).
            var groupMemberServiceHelper = new GroupMemberServiceHelper( _context );
            var scope = groupMemberServiceHelper.GetScopedGroupMembers( group, currentUser.Person );
            if ( !scope.SenderHasScope )
            {
                return BadRequest( GroupMemberServiceHelper.NotAssignedToTableMessage );
            }

            string ccEmails = null;
            List<GroupMember> targets;

            if ( message.GroupMemberId != 0 )
            {
                // The target must be within the caller's scope (their group, and for
                // table-based groups their table); otherwise a caller could target members
                // (and parents) they cannot see. Same 404 as an id from another group.
                var groupMember = scope.Members.FirstOrDefault( gm => gm.Id == message.GroupMemberId );
                if ( groupMember == null )
                {
                    return NotFound();
                }

                if ( groupMember.GroupMemberStatus != GroupMemberStatus.Active )
                {
                    return BadRequest( $"{groupMember.Person.FullName} is not an active member of this group." );
                }

                // Policy: individual communications may not be sent to a minor unless
                // another adult is included. CC the minor's parents/guardians, or reject
                // the send if no parent with an email address is on record.
                if ( !message.SendToParents )
                {
                    var person = groupMember.Person;
                    // == Child (not != Adult): catches no-DOB minors classified by family
                    // role, while businesses (classified Unknown) are not treated as minors.
                    var isMinor = person.AgeClassification == AgeClassification.Child;
                    if ( isMinor )
                    {
                        var parentEmails = groupMemberServiceHelper.GetParents( person )
                            .Where( p => p.Email.IsNotNullOrWhiteSpace() )
                            .Select( p => p.Email )
                            .Distinct( StringComparer.OrdinalIgnoreCase )
                            .ToList();

                        if ( !parentEmails.Any() )
                        {
                            return BadRequest( "Individual communications cannot be sent to a minor without a parent or guardian email on record. Please update the family record." );
                        }

                        ccEmails = string.Join( ",", parentEmails );
                    }
                }

                targets = new List<GroupMember> { groupMember };
            }
            else
            {
                // Only active members receive group communications (the roster still lists
                // inactive/pending members so leaders can see them).
                targets = scope.Members
                    .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                    .ToList();
            }

            var recipients = groupMemberServiceHelper.GetRecipients( targets, message.SendToParents );

            if ( !recipients.Any() )
            {
                return BadRequest( "No recipients found for this message." );
            }

            CreateCommunication( message.Subject, message.Body, recipients, currentUser.Person, group, ccEmails );

            return Ok( recipients.AsQueryable().Select( r => r.FullName ) );
        }

        public class MessageModel
        {
            public string Subject { get; set; }
            public string Body { get; set; }
            public int GroupMemberId { get; set; }
            public bool SendToParents { get; set; } = false;
        }

        public void CreateCommunication( string subject, string body, List<Person> recipients, Person currentPerson, Group group, string ccEmails = null )
        {
            var communication = UpdateCommunication( _context );
            if ( communication != null )
            {
                communication.CommunicationType = CommunicationType.Email;
                communication.CCEmails = ccEmails;
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
                var transaction = new Rock.Tasks.ProcessSendCommunication.Message
                {
                    CommunicationId = communication.Id
                };
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
                    ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_VISITOR.AsGuid() ).Id,
                    RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id,
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

            // Set graduation year based on grade offset if provided and valid
            if ( !string.IsNullOrWhiteSpace( personToAdd.GradeOffset ) && personToAdd.GradeOffset != "-1" )
            {
                if ( int.TryParse( personToAdd.GradeOffset, out int gradeOffsetValue ) && gradeOffsetValue >= 0 && gradeOffsetValue <= 12 )
                {
                    var graduationYear = Person.GraduationYearFromGradeOffset( gradeOffsetValue );
                    if ( graduationYear.HasValue )
                    {
                        person.GraduationYear = graduationYear.Value;
                        _context.SaveChanges();
                    }
                }
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

            // Table-based groups: the new member joins the caller's table. A caller with no
            // table leaves TableNumber unset rather than writing a blank value.
            var callerTableNumber = new GroupMemberServiceHelper( _context ).GetScopedGroupMembers( group, currentUser.Person ).TableNumber;
            if ( callerTableNumber != null )
            {
                groupMember.LoadAttributes();
                groupMember.SetAttributeValue( "TableNumber", callerTableNumber );
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
        public string GradeOffset { get; set; }
    }

    /// <summary>
    /// The members of a group as seen by one person. For table-based groups this is the
    /// person's own table; for every other group it is the whole (non-archived) membership.
    /// </summary>
    public class ScopedGroupMembers
    {
        /// <summary>The group's type is in the "Table-based" defined type.</summary>
        public bool IsTableBased { get; set; }

        /// <summary>The viewer's TableNumber, trimmed; null when they have none (or the group is not table-based).</summary>
        public string TableNumber { get; set; }

        /// <summary>False only for a table-based group whose viewer has no table; such a viewer sees and can email nobody.</summary>
        public bool SenderHasScope => !IsTableBased || TableNumber != null;

        /// <summary>Non-archived members in scope, leaders first then by name. Not filtered by status.</summary>
        public List<GroupMember> Members { get; set; } = new List<GroupMember>();
    }

    public class GroupMemberServiceHelper
    {
        public const string NotAssignedToTableMessage = "You are not assigned to a table";

        private const string TableNumberAttributeKey = "TableNumber";
        private static readonly Guid TableBasedGroupTypesDefinedTypeGuid = new Guid( "90526a36-fda6-4c90-997c-636b82b793d8" );

        private readonly RockContext _rockContext;

        public GroupMemberServiceHelper( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>
        /// True when the group's type is listed in the "Table-based" group types defined type.
        /// </summary>
        public bool IsTableBasedGroup( Group group )
        {
            return new DefinedValueService( _rockContext )
                .GetByDefinedTypeGuid( TableBasedGroupTypesDefinedTypeGuid )
                .Select( dv => dv.Value )
                .ToList()
                .Select( v => v.AsIntegerOrNull() )
                .Any( id => id == group.GroupTypeId );
        }

        /// <summary>
        /// Gets the members of <paramref name="group"/> that <paramref name="currentPerson"/> may
        /// see and communicate with. For a table-based group that is the current person's own
        /// table, matched case-sensitively (surrounding whitespace ignored) on the TableNumber group
        /// member attribute; a current person with no table gets an empty scope.
        /// </summary>
        public ScopedGroupMembers GetScopedGroupMembers( Group group, Person currentPerson )
        {
            if ( group == null )
            {
                throw new ArgumentNullException( nameof( group ) );
            }
            if ( currentPerson == null )
            {
                throw new ArgumentNullException( nameof( currentPerson ) );
            }

            var groupMemberService = new GroupMemberService( _rockContext );
            var scope = new ScopedGroupMembers { IsTableBased = IsTableBasedGroup( group ) };

            IQueryable<GroupMember> members = groupMemberService.GetByGroupId( group.Id )
                .Include( gm => gm.Person )
                .Include( gm => gm.GroupRole )
                .Where( gm => gm.IsArchived == false );

            if ( !scope.IsTableBased )
            {
                scope.Members = OrderForRoster( members.ToList() );
                return scope;
            }

            // A person can hold more than one role in the group; prefer the leader row, then
            // the oldest, so the same table is chosen on every request.
            var currentGroupMember = members
                .Where( gm => gm.PersonId == currentPerson.Id )
                .OrderByDescending( gm => gm.GroupRole.IsLeader )
                .ThenBy( gm => gm.Id )
                .FirstOrDefault();

            if ( currentGroupMember == null )
            {
                return scope;
            }

            currentGroupMember.LoadAttributes();
            var tableNumber = currentGroupMember.GetAttributeValue( TableNumberAttributeKey );
            if ( tableNumber.IsNullOrWhiteSpace() )
            {
                // Unset attributes come back as the attribute's default value ("" for a text
                // attribute), so blank means "no table", not "table named ''".
                return scope;
            }

            scope.TableNumber = tableNumber.Trim();

            // Only the TableNumber attribute(s) that actually apply to this group's members,
            // not every attribute in the system that happens to share the key.
            var tableNumberAttributeIds = currentGroupMember.Attributes.Values
                .Where( a => a.Key == TableNumberAttributeKey )
                .Select( a => a.Id )
                .ToList();

            // SQL narrows the candidates (its collation ignores case and trailing spaces),
            // then the exact comparison decides, so the scope is never wider than the roster.
            // Person and GroupRole ride along because Include() does not survive the join.
            var senderTable = scope.TableNumber;
            var candidates = members
                .Join( new AttributeValueService( _rockContext ).Queryable(),
                        gm => gm.Id,
                        av => av.EntityId,
                        ( gm, av ) => new { GroupMember = gm, gm.Person, gm.GroupRole, av.AttributeId, av.Value } )
                .Where( x => tableNumberAttributeIds.Contains( x.AttributeId ) && x.Value == senderTable )
                .ToList();

            scope.Members = OrderForRoster( candidates
                .Where( x => string.Equals( x.Value?.Trim(), senderTable, StringComparison.Ordinal ) )
                .Select( x => x.GroupMember )
                .DistinctBy( gm => gm.Id ) );
            return scope;
        }

        private static List<GroupMember> OrderForRoster( IEnumerable<GroupMember> members )
        {
            return members
                .OrderByDescending( gm => gm.GroupRole.IsLeader )
                .ThenBy( gm => gm.Person.LastName )
                .ThenBy( gm => gm.Person.NickName )
                .ToList();
        }

        /// <summary>
        /// Builds the recipient list for a group communication to <paramref name="targets"/>,
        /// which the caller has already resolved and validated (scope, group, status).
        /// </summary>
        /// <param name="targets">The group members being communicated with.</param>
        /// <param name="sendToParents">
        /// Send to parents instead: members who are an adult in their family receive the
        /// message themselves, children's parents/guardians receive it for them (the same rule
        /// as the Group Manager website).
        /// </param>
        public List<Person> GetRecipients( IEnumerable<GroupMember> targets, bool sendToParents )
        {
            var recipients = new List<Person>();

            foreach ( var groupMember in targets )
            {
                var person = groupMember.Person;
                if ( !sendToParents )
                {
                    recipients.Add( person );
                    continue;
                }

                var family = GetFamilyRoleInfo( person );
                if ( family.IsAdult )
                {
                    recipients.Add( person );
                }
                else
                {
                    recipients.AddRange( family.Parents );
                }
            }

            // A person can be in the group under more than one role, and siblings share
            // parents; send each person one copy.
            return recipients.DistinctBy( p => p.Id ).ToList();
        }

        /// <summary>
        /// The adults in each family where <paramref name="person"/> is not an adult.
        /// Empty for a person who is an adult in every family they belong to.
        /// </summary>
        public List<Person> GetParents( Person person )
        {
            return GetFamilyRoleInfo( person ).Parents;
        }

        private class FamilyRoleInfo
        {
            /// <summary>True when the person holds the Adult role in at least one family.</summary>
            public bool IsAdult { get; set; }
            public List<Person> Parents { get; set; } = new List<Person>();
        }

        private static FamilyRoleInfo GetFamilyRoleInfo( Person person )
        {
            var info = new FamilyRoleInfo();
            var adultGuid = Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid();

            foreach ( var family in person.GetFamilies().ToList() )
            {
                var ownRole = family.Members.FirstOrDefault( gm => gm.PersonId == person.Id )?.GroupRole;
                if ( ownRole != null && ownRole.Guid == adultGuid )
                {
                    info.IsAdult = true;
                }
                else
                {
                    info.Parents.AddRange( family.Members
                        .Where( m => m.GroupRole.Guid == adultGuid )
                        .Select( m => m.Person ) );
                }
            }

            return info;
        }
    }
}
