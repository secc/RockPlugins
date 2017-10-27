using System;
using System.Linq;
using System.Web.Http;
using System.Collections.Generic;
using Rock.Rest.Filters;
using Rock.Data;
using Rock.Model;
using System.Data.Entity;
using Rock;
using Rock.Web.Cache;
using System.Runtime.Caching;
using System.Text;
using org.secc.RoomScanner.Utilities;
using org.secc.RoomScanner.Models;
using org.secc.FamilyCheckin.Utilities;

namespace org.secc.RoomScanner.Rest.Controllers
{
    public partial class RoomScannerController : ApiController
    {
        private static Dictionary<string, string> settings = GlobalAttributesCache.Value( "RoomScannerSettings" ).AsDictionary();
        private int allowedGroupId = settings["AllowedGroupId"].AsInteger();
        private int subroomLocationTypeId = settings["SubroomLocationType"].AsInteger();

        private const string locationEntityTypeGuid = "0D6410AD-C83C-47AC-AF3D-616D09EDF63B";
        private int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
        private int locationEntityTypeId = EntityTypeCache.Read( locationEntityTypeGuid.AsGuid() ).Id;

        private List<int> VolunteerGroupIds
        {
            get
            {
                ObjectCache cache = RockMemoryCache.Default;
                var ids = ( List<int> ) cache.Get( "org_secc_familycheckin_volunteer_ids" );
                if ( ids != null )
                {
                    return ids;
                }

                AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
                var volAttributeId = AttributeCache.Read( Constants.VOLUNTEER_ATTRIBUTE_GUID.AsGuid() )?.Id;
                ids = attributeValueService.Queryable().Where( av => av.AttributeId == volAttributeId && av.Value == "True" ).Select( av => av.EntityId ?? 0 ).ToList();
                cache.Set( "org_secc_familycheckin_volunteer_ids", ids, new CacheItemPolicy() { AbsoluteExpiration = Rock.RockDateTime.Now.AddHours( 12 ) } );
                return ids;
            }
        }

        private int NumberOfVolunteersCheckedIn( int locationId )
        {
            var lglsc = CheckInCountCache.GetByLocation( locationId );
            return lglsc.Where( glsc => VolunteerGroupIds.Contains( glsc.GroupId ) ).Select( glsc => glsc.InRoomPersonIds.Count() ).Sum();
        }

        private bool AreChildrenCheckedIn( int locationId )
        {
            var lglsc = CheckInCountCache.GetByLocation( locationId );
            var count = lglsc.Where( glsc => !VolunteerGroupIds.Contains( glsc.GroupId ) ).Select( glsc => glsc.InRoomPersonIds.Count() ).Sum();
            return count >= 1;
        }


        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/test" )]
        public string TEST()
        {
            return "TEST GOOD!";
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/pin/{pinCode}" )]
        public Response Pin( string pinCode )
        {
            if ( TestPin( pinCode ) != null )
            {
                return new Response( true, "PIN is authorized", false );
            }
            return new Response( false, "PIN is not authorized", false );
        }

        private Person TestPin( string pin )
        {
            RockContext rockContext = new RockContext();
            UserLoginService userLoginService = new UserLoginService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var user = userLoginService.GetByUserName( pin );
            if ( user != null )
            {
                var personId = user.PersonId ?? 0;
                var groupMember = groupMemberService.GetByGroupIdAndPersonId( allowedGroupId, personId );
                if ( groupMember != null )
                {
                    return user.Person;
                }
            }
            return null;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/templates" )]
        public List<Template> Templates()
        {
            RockContext rockContext = new RockContext();
            GroupTypeService groupTypeService = new GroupTypeService( rockContext );
            Guid templateTypeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();
            List<Template> templates = groupTypeService
                .Queryable().AsNoTracking()
                .Where( t =>
                    t.GroupTypePurposeValue != null &&
                    t.GroupTypePurposeValue.Guid == templateTypeGuid )
                .Select( t => new Template() { Id = t.Id, Name = t.Name, Description = t.Description ?? "" } )
                    .OrderBy( t => t.Name )

                .ToList()
                .Where( t => t.Name.ToLower().Contains( "kids" ) )
                .ToList();

            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/areas/{templateId}" )]
        public List<Template> Areas( int templateId )
        {
            RockContext rockContext = new RockContext();
            GroupTypeService groupTypeService = new GroupTypeService( rockContext );
            List<Template> templates = groupTypeService
                .Queryable().AsNoTracking()
                .Where( t =>
                 t.ParentGroupTypes.Select( p => p.Id ).Contains( templateId ) )
                .OrderBy( t => t.Order )
                .Select( t => new Template() { Id = t.Id, Name = t.Name, Description = t.Description ?? "" } )
                .ToList();
            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/groups/{groupTypeId}" )]
        public List<Template> Groups( int groupTypeId )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            List<Template> templates = groupService
                .Queryable().AsNoTracking()
                .Where( g => g.GroupTypeId == groupTypeId && g.IsActive )
                .OrderBy( g => g.Order )
                .Select( g => new Template() { Id = g.Id, Name = g.Name, Description = g.Description ?? "" } )
                .ToList();
            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/locations/{groupId}" )]
        public List<Template> Locations( int groupId )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            List<Template> templates = groupService
                .Queryable().AsNoTracking()
                .Where( t => t.Id == groupId )
                .SelectMany( g => g.GroupLocations )
                .Where( gl => gl.Schedules.Any() )
                .OrderBy( gl => gl.Order )
                .Select( gl => gl.Location )
                .Select( t => new Template() { Id = t.Id, Name = t.Name, Description = "" } )
                .ToList();
            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/locationbyguid/{guid}" )]
        public Template LocationByGuid( string guid )
        {
            RockContext rockContext = new RockContext();
            LocationService locationService = new LocationService( rockContext );
            var location = locationService.Get( guid.AsGuid() );
            var campus = CampusCache.Read( location.CampusId ?? 0 );
            return new Template() { Name = location.Name, Id = location.Id, Description = campus?.Name ?? "" };
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/attendees/{locationId}" )]
        public List<Attendee> Attendees( int locationId )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            List<Attendee> attendees = attendanceService
                .Queryable().AsNoTracking()
                .Where( a => a.LocationId == locationId && a.StartDateTime >= Rock.RockDateTime.Today )
                .Join( personAliasService.Queryable(),
                    a => a.PersonAliasId,
                    pa => pa.Id,
                    ( a, pa ) => new Attendee()
                    {
                        AttendanceGuid = a.Guid,
                        DidAttend = a.DidAttend ?? false,
                        CheckedOut = a.EndDateTime != null,
                        PersonId = pa.PersonId,
                        PersonName = pa.Person.FullName
                    } )
                .OrderBy( a => a.PersonName )
                .ToList();
            return attendees;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/getroster/{locationId}" )]
        public List<AttendanceEntry> GetRoster( int locationId )
        {
            var tomorrow = Rock.RockDateTime.Today.AddDays( 1 );
            RockContext rockContext = new RockContext();
            Location location = new LocationService( rockContext ).Get( locationId );
            if ( location == null )
            {
                return new List<AttendanceEntry>();
            }
            bool isSubroom = location.LocationTypeValueId == subroomLocationTypeId;
            if ( isSubroom )
            {
                locationId = location.ParentLocationId ?? 0;
            }
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var qry = attendanceService.Queryable()
                .Where( a => a.LocationId == locationId && a.StartDateTime > Rock.RockDateTime.Today && a.StartDateTime < tomorrow );
            if ( isSubroom )
            {
                qry = qry.Where( a => ( a.DidAttend == true && a.ForeignId == location.Id ) || a.DidAttend != true );
            }
            var roster = qry.Select( a => new AttendanceEntry()
            {
                Id = a.Id,
                PersonId = a.PersonAlias.Person.Id,
                LastName = a.PersonAlias.Person.LastName,
                NickName = a.PersonAlias.Person.NickName,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                AttendanceGuid = a.Guid.ToString(),
                DidAttend = a.DidAttend ?? false,
                IsVolunteer = VolunteerGroupIds.Contains( a.GroupId ?? 0 )
            } )
                .OrderBy( ae => ae.Id )
                .ToList();
            foreach ( var entry in roster )
            {
                entry.InWorship = InMemoryPersonStatus.IsInWorship( entry.PersonId );
                entry.WithParent = InMemoryPersonStatus.IsWithParent( entry.PersonId );
            }
            return roster;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/GetAttendanceCode/{attendanceGuid}" )]
        public AttendanceCodes GetAttendanceCode( string attendanceGuid )
        {
            Guid guid = attendanceGuid.AsGuid();
            RockContext rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );
            var primaryAttendance = attendanceService.Get( guid );

            if ( primaryAttendance == null )
            {
                return null;
            }

            var personAliasService = new PersonAliasService( rockContext );
            var person = primaryAttendance.PersonAlias.Person;
            var personAliasIds = personAliasService.Queryable()
                .Where( pa => pa.PersonId == primaryAttendance.PersonAlias.PersonId )
                .Select( pa => pa.Id );

            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            var codes = attendanceService.Queryable()
                .Where( a => personAliasIds.Contains( a.PersonAliasId ?? 0 ) && a.StartDateTime >= today && a.StartDateTime < tomorrow )
                .Select( a => a.AttendanceCode )
                .Select( ac => ac.Code )
                .DistinctBy( c => c )
                .ToList();

            return new AttendanceCodes()
            {
                NickName = person.NickName,
                LastName = person.LastName,
                Codes = codes
            };
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/exit" )]
        public Response Exit( [FromBody] Request req )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );

            Attendance attendeeAttendance = null;
            var attendanceGuidGuid = req.AttendanceGuid.AsGuidOrNull();
            if ( attendanceGuidGuid != null )
            {
                attendeeAttendance = attendanceService.Get( attendanceGuidGuid ?? new Guid() );
            }
            else
            {
                UserLoginService userLoginService = new UserLoginService( rockContext );
                var user = userLoginService.GetByUserName( req.AttendanceGuid );
                if ( user != null )
                {
                    attendeeAttendance = attendanceService.Queryable().Where( a => a.PersonAlias.PersonId == user.PersonId
                                                        && Rock.RockDateTime.Today == attendeeAttendance.StartDateTime.Date )
                        .FirstOrDefault();
                }
            }
            if ( attendeeAttendance == null )
            {
                return new Response( false, "Attendance record not found.", false );
            }
            if ( Rock.RockDateTime.Today != attendeeAttendance.StartDateTime.Date )
            {
                return new Response( false, "Attendance record not from today.", false );
            }

            var person = attendeeAttendance.PersonAlias.Person;
            var location = new LocationService( rockContext ).Get( req.LocationId );
            bool isSubroom = location.LocationTypeValueId == subroomLocationTypeId;
            if ( isSubroom )
            {
                req.LocationId = location.ParentLocationId ?? 0;
            }
            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.StartDateTime >= today && a.StartDateTime < tomorrow );

            //If person is a volunteer, children are checked in, and would result in less than 2 volunteers
            //Then don't allow for check-out
            if ( attendances.Where( a => VolunteerGroupIds.Contains( a.GroupId ?? 0 ) ).Any()
                && AreChildrenCheckedIn( req.LocationId )
                && NumberOfVolunteersCheckedIn( req.LocationId ) <= 2 )
            {
                return new Response( false, "Cannot checkout volunteer with children still in class. Two volunteers are required at all times.", false );
            }

            if ( !req.Override )
            {
                attendances = attendances.Where( a => a.LocationId == req.LocationId );
            }

            if ( !attendances.Any() ) //There was an attendance record, but not for the selected location
            {
                return new Response( false, string.Format( "{0} is not checked-in to {1} would you like to override?", person.FullName, location.Name ), false );
            }

            foreach ( var attendance in attendances )
            {
                attendance.EndDateTime = Rock.RockDateTime.Now;
                CheckInCountCache.RemoveAttendance( attendance );
            }

            var summary = string.Format( "Exited <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );
            if ( isSubroom )
            {
                summary += string.Format( " (a subroom of <span class=\"field-name\">{0}</span>)", location.ParentLocation.Name );
            }

            var hostInfo = "Unknown Host";
            try
            {

                hostInfo = Rock.Web.UI.RockPage.GetClientIpAddress();
                var host = System.Net.Dns.GetHostEntry( hostInfo );
                if ( host != null )
                {
                    hostInfo = host.HostName;
                }
            }
            catch { }

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = attendeeAttendance.PersonAlias.PersonId,
                RelatedEntityTypeId = locationEntityTypeId,
                RelatedEntityId = location.Id,
                Verb = "Exit",
                Summary = summary,
                Caption = "Exited Location",
                RelatedData = hostInfo,
                CategoryId = 4
            };

            historyService.Add( history );
            InMemoryPersonStatus.RemoveFromWorship( attendeeAttendance.PersonAlias.PersonId );
            rockContext.SaveChanges();

            var message = string.Format( "{0} has been checked-out of {1}.", person.FullName, location.Name );
            return new Response( true, message, false, personId: person.Id );
        }


        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/entry" )]
        public Response Entry( [FromBody] Request req )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            Attendance attendeeAttendance = null;
            var attendanceGuidGuid = req.AttendanceGuid.AsGuidOrNull();
            if ( attendanceGuidGuid != null )
            {
                attendeeAttendance = attendanceService.Get( attendanceGuidGuid ?? new Guid() );
            }
            else
            {
                UserLoginService userLoginService = new UserLoginService( rockContext );
                var user = userLoginService.GetByUserName( req.AttendanceGuid );
                if ( user != null )
                {
                    attendeeAttendance = attendanceService.Queryable().Where( a => a.PersonAlias.PersonId == user.PersonId
                                                        && Rock.RockDateTime.Today == attendeeAttendance.StartDateTime.Date )
                        .FirstOrDefault();
                }
            }

            if ( attendeeAttendance == null )
            {
                return new Response( false, "Attendance record not found.", false );
            }
            if ( Rock.RockDateTime.Today != attendeeAttendance.StartDateTime.Date )
            {
                return new Response( false, "Attendance record not from today.", false );
            }

            var person = attendeeAttendance.PersonAlias.Person;
            var location = new LocationService( rockContext ).Get( req.LocationId );
            bool isSubroom = location.LocationTypeValueId == subroomLocationTypeId;
            if ( isSubroom )
            {
                req.LocationId = location.ParentLocationId ?? 0;
            }
            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.StartDateTime >= today && a.StartDateTime < tomorrow && a.EndDateTime == null );

            if ( !attendances.Any() )
            {
                return new Response( false, string.Format( "{0} has been checked out of all locations.", person.FullName ), false );
            }

            //If no volunteers are checked in and not checking-in a volunteer
            if ( NumberOfVolunteersCheckedIn( req.LocationId ) < 2
                && !attendances.Where( a => VolunteerGroupIds.Contains( a.GroupId ?? 0 ) ).Any() )
            {
                return new Response(
                    false,
                    "Cannot check-in child before 2 volunteers are checked-in.",
                    false );
            }

            var attendancesToModify = attendances.Where( a => a.LocationId == req.LocationId ).ToList();

            //There was an attendance record, but not for the selected location
            if ( !attendancesToModify.Any() && !req.Override )
            {
                var currentAttendances = new StringBuilder();
                foreach ( var attendance in attendances )
                {
                    currentAttendances.Append( string.Format( "\n{0} @ {1} ", attendance.Location.Name, attendance.Schedule.Name ) );
                }

                return new Response( false, string.Format( "{0} is not checked-in to {1}. \n\n{2} is currently checked in to: {3} \n\nWould you like to override?",
                    person.FullName,
                    location.Name,
                    person.Gender == Gender.Female ? "She" : "He",
                    currentAttendances.ToString()
                    ), true );
            }

            //We will need to know the host from here on
            var hostInfo = "Unknown Host";
            try
            {

                hostInfo = Rock.Web.UI.RockPage.GetClientIpAddress();
                var host = System.Net.Dns.GetHostEntry( hostInfo );
                if ( host != null )
                {
                    hostInfo = host.HostName;
                }
            }
            catch { }

            //Need to move this person to a different location
            if ( !attendancesToModify.Any() && req.Override )
            {
                var authorizedPerson = TestPin( req.PIN );
                if ( authorizedPerson == null )
                {
                    return new Response( false, "PIN not authorized", false );
                }

                var volAttribute = AttributeCache.Read( new Guid( "F5DAD320-B77D-4282-98C9-35414FB0A6DC" ) );
                AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
                var childGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "False" ).Select( av => av.EntityId.Value ).ToList();

                if ( childGroupIds.Contains( attendeeAttendance.GroupId ?? 0 ) )
                {
                    //This section tests for attendances that can be moved to this location.
                    //It tests for people 
                    GroupLocationService groupLocationService = new GroupLocationService( rockContext );
                    var acceptableServiceIds = groupLocationService.Queryable()
                        .Where( gl => gl.LocationId == req.LocationId && childGroupIds.Contains( gl.GroupId ) )
                        .SelectMany( gl => gl.Schedules )
                        .Select( s => s.Id ).ToList();
                    attendances = attendances.Where( a => acceptableServiceIds.Contains( a.ScheduleId ?? 0 ) );
                }

                if ( !attendances.Any() )
                {
                    return new Response( false, "There are no attendances which can be moved to this location", false );
                }

                if ( LocationsFull( attendances.ToList(), req.LocationId, rockContext ) )
                {
                    return new Response( false, "Could not move location. Location is full.", false );
                }

                foreach ( var attendance in attendances )
                {
                    Attendance newAttendance = ( Attendance ) attendance.Clone();
                    newAttendance.Id = 0;
                    newAttendance.Guid = new Guid();
                    newAttendance.StartDateTime = Rock.RockDateTime.Now;
                    newAttendance.EndDateTime = null;
                    newAttendance.DidAttend = true;
                    newAttendance.Device = null;
                    newAttendance.SearchTypeValue = null;
                    newAttendance.LocationId = req.LocationId;
                    newAttendance.AttendanceCode = null;
                    newAttendance.AttendanceCodeId = attendance.AttendanceCodeId;
                    if ( isSubroom )
                    {
                        newAttendance.ForeignId = location.Id;
                    }
                    attendanceService.Add( newAttendance );
                    attendance.DidAttend = false;
                    attendance.EndDateTime = Rock.RockDateTime.Now;
                    CheckInCountCache.AddAttendance( newAttendance );
                    CheckInCountCache.RemoveAttendance( attendance );
                }

                var moveSummary = string.Format( "Moved to and Entered <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span> under the authority of {2}", location.Name, Rock.RockDateTime.Now, authorizedPerson.FullName );
                if ( isSubroom )
                {
                    moveSummary += string.Format( " (a subroom of <span class=\"field-name\">{0}</span>)", location.ParentLocation.Name );
                }

                History moveHistory = new History()
                {
                    EntityTypeId = personEntityTypeId,
                    EntityId = attendeeAttendance.PersonAlias.PersonId,
                    RelatedEntityTypeId = locationEntityTypeId,
                    RelatedEntityId = location.Id,
                    Verb = "Moved",
                    Summary = moveSummary,
                    Caption = "Moved To Location",
                    RelatedData = hostInfo,
                    CategoryId = 4
                };

                historyService.Add( moveHistory );
                rockContext.SaveChanges();
                int allergyAttributeId2 = AttributeCache.Read( Rock.SystemGuid.Attribute.PERSON_ALLERGY.AsGuid() ).Id;
                var allergyAttributeValue2 = new AttributeValueService( rockContext )
                    .Queryable()
                    .FirstOrDefault( av => av.AttributeId == allergyAttributeId2 && av.EntityId == person.Id );
                if ( allergyAttributeValue2 != null
                    && !string.IsNullOrWhiteSpace( allergyAttributeValue2.Value ) )
                {
                    return new Response( true,
                        string.Format( "{0} has been checked-in to {1}. \n\n Allergy: {2}", person.FullName, location.Name, allergyAttributeValue2.Value ),
                        false,
                        true,
                        person.Id
                        );
                }
                var message2 = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
                return new Response( true, message2, false, personId: person.Id );
            }

            foreach ( var attendance in attendancesToModify.ToList() )
            {
                attendance.DidAttend = true;
                attendance.StartDateTime = Rock.RockDateTime.Now;
                if ( isSubroom )
                {
                    attendance.ForeignId = location.Id;
                }
                CheckInCountCache.UpdateAttendance( attendance );
            }

            var summary = string.Format( "Entered <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );
            if ( isSubroom )
            {
                summary += string.Format( " (a subroom of <span class=\"field-name\">{0}</span>)", location.ParentLocation.Name );
            }

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = attendeeAttendance.PersonAlias.PersonId,
                RelatedEntityTypeId = locationEntityTypeId,
                RelatedEntityId = location.Id,
                Verb = "Entry",
                Summary = summary,
                Caption = "Entered Location",
                RelatedData = hostInfo,
                CategoryId = 4
            };

            historyService.Add( history );
            rockContext.SaveChanges();


            int allergyAttributeId = AttributeCache.Read( Rock.SystemGuid.Attribute.PERSON_ALLERGY.AsGuid() ).Id;
            var allergyAttributeValue = new AttributeValueService( rockContext )
                .Queryable()
                .FirstOrDefault( av => av.AttributeId == allergyAttributeId && av.EntityId == person.Id );
            if ( allergyAttributeValue != null && !string.IsNullOrWhiteSpace( allergyAttributeValue.Value ) )
            {
                return new Response( true,
                    string.Format( "{0} has been checked-in to {1}. \n\n Allergy: {2}", person.FullName, location.Name, allergyAttributeValue.Value ),
                    false,
                    true,
                    person.Id );
            }
            var message = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
            return new Response( true, message, false, personId: person.Id );
        }

        private bool LocationsFull( List<Attendance> attendancesToMove, int locationId, RockContext rockContext )
        {
            LocationService locationService = new LocationService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var location = locationService.Get( locationId );

            if ( location == null )
            {
                return true;
            }

            foreach ( var attendance in attendancesToMove )
            {
                var count = attendanceService.Queryable()
                    .Where( a => a.LocationId == locationId
                         && a.ScheduleId == attendance.ScheduleId
                         && a.EndDateTime == null
                         && a.StartDateTime >= Rock.RockDateTime.Today
                        ).Count();
                var threshold = location.FirmRoomThreshold ?? 0;
                if ( !attendance.Group.GetAttributeValue( "IsVolunteer" ).AsBoolean() )
                {
                    threshold = Math.Min( location.SoftRoomThreshold ?? 0, threshold );
                }
                if ( count >= threshold )
                {
                    return true;
                }
            }
            return false;
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/movetowithparent" )]
        public Response MoveToWithParent( [FromBody] MultiRequest req )
        {
            var personIds = req.PersonIds
                .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.AsInteger() ).ToList();
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            var people = personService.GetByIds( personIds );
            int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;

            var hostInfo = "Unknown Host";
            try
            {
                hostInfo = Rock.Web.UI.RockPage.GetClientIpAddress();
                var host = System.Net.Dns.GetHostEntry( hostInfo );
                if ( host != null )
                {
                    hostInfo = host.HostName;
                }
            }
            catch { }

            foreach ( var person in people )
            {
                InMemoryPersonStatus.AddToWithParent( person.Id );
                var summary = string.Format( "Moved to be with Parent at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );

                History history = new History()
                {
                    EntityTypeId = personEntityTypeId,
                    EntityId = person.Id,
                    Verb = "Moved",
                    Summary = summary,
                    Caption = "Moved be with Parent",
                    RelatedData = hostInfo,
                    CategoryId = 4
                };
                historyService.Add( history );
            }
            rockContext.SaveChanges();
            return new Response( true, "Success", false );
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/movetoworship" )]
        public Response MoveToWorship( [FromBody] MultiRequest req )
        {
            var personIds = req.PersonIds
                .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.AsInteger() ).ToList();
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            var people = personService.GetByIds( personIds );
            int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;

            var hostInfo = "Unknown Host";
            try
            {
                hostInfo = Rock.Web.UI.RockPage.GetClientIpAddress();
                var host = System.Net.Dns.GetHostEntry( hostInfo );
                if ( host != null )
                {
                    hostInfo = host.HostName;
                }
            }
            catch { }

            foreach ( var person in people )
            {
                InMemoryPersonStatus.AddToWorship( person.Id );
                var summary = string.Format( "Moved to Worship at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );

                History history = new History()
                {
                    EntityTypeId = personEntityTypeId,
                    EntityId = person.Id,
                    Verb = "Moved",
                    Summary = summary,
                    Caption = "Moved To Worship",
                    RelatedData = hostInfo,
                    CategoryId = 4
                };
                historyService.Add( history );
            }
            rockContext.SaveChanges();
            return new Response( true, "Success", false );
        }


        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/returntoroom" )]
        public Response ReturnToRoom( [FromBody] MultiRequest req )
        {
            var personIds = req.PersonIds
                .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.AsInteger() ).ToList();
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            var people = personService.GetByIds( personIds );
            int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;

            var hostInfo = "Unknown Host";
            try
            {

                hostInfo = Rock.Web.UI.RockPage.GetClientIpAddress();
                var host = System.Net.Dns.GetHostEntry( hostInfo );
                if ( host != null )
                {
                    hostInfo = host.HostName;
                }
            }
            catch { }

            var summary = "";
            var caption = "";

            foreach ( var person in people )
            {
                if ( InMemoryPersonStatus.IsInWorship( person.Id ) && InMemoryPersonStatus.IsWithParent( person.Id ) )
                {
                    InMemoryPersonStatus.RemoveFromWorship( person.Id );
                    InMemoryPersonStatus.RemoveFromWithParent( person.Id );
                    summary = string.Format( "Returned from Worship and Parent at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );
                    caption = "Returned from Worship and Parent";
                }
                else if ( InMemoryPersonStatus.IsInWorship( person.Id ) )
                {
                    InMemoryPersonStatus.RemoveFromWorship( person.Id );
                    summary = string.Format( "Returned from Worship at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );
                    caption = "Returned from Worship";
                }
                else if ( InMemoryPersonStatus.IsWithParent( person.Id ) )
                {
                    InMemoryPersonStatus.RemoveFromWithParent( person.Id );
                    summary = string.Format( "Returned from Parent at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );
                    caption = "Returned from Parent";
                }
                if ( !string.IsNullOrWhiteSpace( caption ) )
                {
                    History history = new History()
                    {
                        EntityTypeId = personEntityTypeId,
                        EntityId = person.Id,
                        Verb = "Returned",
                        Summary = summary,
                        Caption = "Returned from Worship",
                        RelatedData = hostInfo,
                        CategoryId = 4
                    };
                    historyService.Add( history );
                }
            }
            rockContext.SaveChanges();
            return new Response( true, "Success", false );
        }
    }
}