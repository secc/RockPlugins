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
        public static AttributeCache volAttribute = AttributeCache.Read( new Guid( "F5DAD320-B77D-4282-98C9-35414FB0A6DC" ) );

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
            return lglsc
                .Where( glsc => VolunteerGroupIds.Contains( glsc.GroupId ) || glsc.GroupId == 0 )
                .SelectMany( glsc => glsc.InRoomPersonIds )
                .Distinct()
                .Count();
        }

        private bool AreChildrenCheckedIn( int locationId )
        {
            var lglsc = CheckInCountCache.GetByLocation( locationId );
            var count = lglsc.Where( glsc => !VolunteerGroupIds.Contains( glsc.GroupId ) && glsc.GroupId != 0 )
                .Select( glsc => glsc.InRoomPersonIds.Count() )
                .Sum();
            return count >= 1;
        }


        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/test" )]
        public string TEST()
        {
            try
            {
                return "TEST GOOD!";
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return "Error Testing System.";
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/pin/{pinCode}" )]
        public Response Pin( string pinCode )
        {
            try
            {

                if ( ValidationHelper.TestPin( new RockContext(), pinCode ) != null )
                {
                    return new Response( true, "PIN is authorized", false );
                }
                return new Response( false, "PIN is not authorized", false );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/templates" )]
        public List<Template> Templates()
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<Template>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/areas/{templateId}" )]
        public List<Template> Areas( int templateId )
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<Template>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/groups/{groupTypeId}" )]
        public List<Template> Groups( int groupTypeId )
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<Template>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/locations/{groupId}" )]
        public List<Template> Locations( int groupId )
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<Template>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/locationbyguid/{guid}" )]
        public Template LocationByGuid( string guid )
        {
            try
            {
                RockContext rockContext = new RockContext();
                LocationService locationService = new LocationService( rockContext );
                var location = locationService.Get( guid.AsGuid() );
                var campus = CampusCache.Read( location.CampusId ?? 0 );
                return new Template() { Name = location.Name, Id = location.Id, Description = campus?.Name ?? "" };
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Template();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/attendees/{locationId}" )]
        public List<Attendee> Attendees( int locationId )
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<Attendee>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/getroster/{locationId}" )]
        public List<AttendanceEntry> GetRoster( int locationId )
        {
            try
            {
                var tomorrow = Rock.RockDateTime.Today.AddDays( 1 );
                RockContext rockContext = new RockContext();
                Location location = new LocationService( rockContext ).Get( locationId );
                if ( location == null )
                {
                    return new List<AttendanceEntry>();
                }
                var isSubroom = ValidationHelper.IsSubRoom( location );
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
                    IsVolunteer = VolunteerGroupIds.Contains( a.GroupId ?? 0 ) || a.GroupId == null
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new List<AttendanceEntry>();
            }
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/GetAttendanceCode/{attendanceGuid}" )]
        public AttendanceCodes GetAttendanceCode( string attendanceGuid )
        {
            try
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
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new AttendanceCodes() { Codes = new List<string>() };
            }
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/exit" )]
        public Response Exit( [FromBody] Request req )
        {
            try
            {
                RockContext rockContext = new RockContext();

                Attendance attendeeAttendance = ValidationHelper.GetAttendeeAttendance( req, rockContext );

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
                bool isSubroom = ValidationHelper.IsSubRoom( location );
                if ( isSubroom )
                {
                    req.LocationId = location.ParentLocationId ?? 0;
                }

                var attendances = ValidationHelper.GetAttendancesForAttendee( rockContext, attendeeAttendance );
                attendances = attendances.Where( a => a.LocationId == req.LocationId );


                //If person is a volunteer, children are checked in, and would result in less than 2 volunteers
                //Then don't allow for check-out
                if ( ( attendances.Where( a => VolunteerGroupIds.Contains( a.GroupId ?? 0 ) ).Any()
                    || attendances.Where( a => a.GroupId == 0 || a.GroupId == null ).Any() )
                    && AreChildrenCheckedIn( req.LocationId )
                    && NumberOfVolunteersCheckedIn( req.LocationId ) <= 2 )
                {
                    return new Response( false, "Cannot checkout volunteer with children still in class. Two volunteers are required at all times.", false );
                }

                foreach ( var attendance in attendances )
                {
                    var stayedFifteenMinutes = ( Rock.RockDateTime.Now - attendance.StartDateTime ) > new TimeSpan( 0, 15, 0 );
                    attendance.DidAttend = stayedFifteenMinutes;
                    attendance.EndDateTime = Rock.RockDateTime.Now;
                    CheckInCountCache.RemoveAttendance( attendance );
                    var personId = attendeeAttendance.PersonAlias.PersonId;
                    InMemoryPersonStatus.RemoveFromWorship( personId );
                    InMemoryPersonStatus.RemoveFromWithParent( personId );
                }

                //Add history of exit
                DataHelper.AddExitHistory( rockContext, location, attendeeAttendance, isSubroom );

                rockContext.SaveChanges();

                var message = string.Format( "{0} has been checked-out of {1}.", person.FullName, location.Name );
                return new Response( true, message, false, personId: person.Id );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }


        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/entry" )]
        public Response Entry( [FromBody] Request req )
        {
            try
            {
                RockContext rockContext = new RockContext();
                AttendanceService attendanceService = new AttendanceService( rockContext );
                Attendance attendeeAttendance = ValidationHelper.GetAttendeeAttendance( req, rockContext );

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
                bool isSubroom = ValidationHelper.IsSubRoom( location );
                if ( isSubroom )
                {
                    req.LocationId = location.ParentLocationId ?? 0;
                }

                var attendances = ValidationHelper.GetAttendancesForAttendee( rockContext, attendeeAttendance );

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

                //Need to move this person to a different location
                if ( !attendancesToModify.Any() && req.Override )
                {
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
                        var availableAttendances = attendances.Where( a => acceptableServiceIds.Contains( a.ScheduleId ?? 0 ) );

                        if ( availableAttendances.Any() )
                        {
                            attendances = availableAttendances;
                        }
                        else
                        {
                            //If there are no attendances that match this schedule,
                            //Take one preferably if it isn't attended yet.
                            attendances = attendances.OrderBy( a => a.DidAttend ).Take( 1 );
                        }
                    }

                    if ( !attendances.Any() )
                    {
                        return new Response( false, "There are no attendances which can be moved to this location", false );
                    }

                    if ( ValidationHelper.LocationsFull( attendances.ToList(), req.LocationId, VolunteerGroupIds, rockContext ) )
                    {
                        return new Response( false, "Could not move location. Location is full.", false );
                    }

                    foreach ( var attendance in attendances )
                    {
                        DataHelper.CloneAttendance( attendance, isSubroom, location, attendanceService, req );
                    }

                    DataHelper.CloseActiveAttendances( rockContext, attendeeAttendance, location, isSubroom );
                    //Set person history showing that the person was moved on scan in
                    DataHelper.AddMoveHistory( rockContext, location, attendeeAttendance, isSubroom );
                    rockContext.SaveChanges();
                    return DataHelper.GetEntryResponse( rockContext, person, location );
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

                DataHelper.CloseActiveAttendances( rockContext, attendeeAttendance, location, isSubroom );
                DataHelper.AddEntranceHistory( rockContext, location, attendeeAttendance, isSubroom );
                rockContext.SaveChanges();

                return DataHelper.GetEntryResponse( rockContext, person, location );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/movetowithparent" )]
        public Response MoveToWithParent( [FromBody] MultiRequest req )
        {
            try
            {
                var personIds = req.PersonIds
                    .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsInteger() ).ToList();
                RockContext rockContext = new RockContext();
                PersonService personService = new PersonService( rockContext );
                var people = personService.GetByIds( personIds );

                foreach ( var person in people )
                {
                    DataHelper.AddWithParentHistory( rockContext, person );
                }
                rockContext.SaveChanges();
                return new Response( true, "Success", false );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/movetoworship" )]
        public Response MoveToWorship( [FromBody] MultiRequest req )
        {
            try
            {
                var personIds = req.PersonIds
                    .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsInteger() ).ToList();
                RockContext rockContext = new RockContext();
                PersonService personService = new PersonService( rockContext );
                var people = personService.GetByIds( personIds );

                foreach ( var person in people )
                {

                    DataHelper.AddMoveTwoWorshipHistory( rockContext, person );
                }
                rockContext.SaveChanges();
                return new Response( true, "Success", false );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }


        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/returntoroom" )]
        public Response ReturnToRoom( [FromBody] MultiRequest req )
        {
            try
            {
                var personIds = req.PersonIds
                    .Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsInteger() ).ToList();
                RockContext rockContext = new RockContext();
                PersonService personService = new PersonService( rockContext );
                var people = personService.GetByIds( personIds );

                foreach ( var person in people )
                {
                    DataHelper.AddReturnToRoomHistory( rockContext, person );
                }
                rockContext.SaveChanges();
                return new Response( true, "Success", false );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }

        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/insert" )]
        public Response Insert( [FromBody] Request req )
        {
            try
            {
                RockContext rockContext = new RockContext();
                AttendanceService attendanceService = new AttendanceService( rockContext );
                LocationService locationService = new LocationService( rockContext );

                var location = locationService.Get( req.LocationId );
                if ( location == null )
                {
                    return new Response( false, "Could not find location", false );
                }

                var isSubroom = ValidationHelper.IsSubRoom( location );

                var person = ValidationHelper.TestPin( rockContext, req.PIN );

                if ( person == null )
                {
                    return new Response( false, "Person not authorized", false );
                }
                var newAttendance = new Attendance
                {
                    PersonAlias = person.PrimaryAlias,
                    LocationId = location.Id,
                    StartDateTime = Rock.RockDateTime.Now,
                    DidAttend = true,
                };
                if ( isSubroom )
                {
                    newAttendance.LocationId = location.ParentLocationId;
                    newAttendance.ForeignId = location.Id;
                }
                attendanceService.Add( newAttendance );
                DataHelper.CloseActiveAttendances( rockContext, newAttendance, location, isSubroom );
                DataHelper.AddEntranceHistory( rockContext, location, newAttendance, isSubroom );
                CheckInCountCache.AddAttendance( newAttendance );
                rockContext.SaveChanges();

                return DataHelper.GetEntryResponse( rockContext, person, location );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e, System.Web.HttpContext.Current );
                return new Response( false, "An error occured", false );
            }
        }
    }
}