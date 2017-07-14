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

namespace org.secc.RoomScanner.Rest.Controllers
{
    public partial class RoomScannerController : ApiController
    {
        private const string locationEntityTypeGuid = "0D6410AD-C83C-47AC-AF3D-616D09EDF63B";

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/test" )]
        public string TEST()
        {
            return "TEST GOOD!";
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
            return new Template() { Name = location.Name, Id = location.Id, Description = "" };
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
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var roster = attendanceService.Queryable()
                .Where( a => a.LocationId == locationId && a.StartDateTime > Rock.RockDateTime.Today && a.StartDateTime < tomorrow )
                .Select( a => new AttendanceEntry()
                {   Id = a.Id,
                    PersonId = a.PersonAlias.Person.Id,
                    LastName = a.PersonAlias.Person.LastName,
                    NickName = a.PersonAlias.Person.NickName,
                    StartDateTime = a.StartDateTime,
                    EndDateTime = a.EndDateTime,
                    AttendanceGuid = a.Guid.ToString(),
                    DidAttend = a.DidAttend ?? false
                } )
                .OrderBy(ae => ae.Id)
                .ToList();
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
                .Select( ac => ac.Code ).ToList();

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
            var attendanceGuidGuid = req.AttendanceGuid.AsGuid();
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            Attendance attendeeAttendance = attendanceService.Get( attendanceGuidGuid );
            int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
            int locationEntityTypeId = EntityTypeCache.Read( locationEntityTypeGuid.AsGuid() ).Id;

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

            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.StartDateTime >= today && a.StartDateTime < tomorrow );

            if ( !req.Override )
            {
                attendances = attendances.Where( a => a.LocationId == req.LocationId );
            }

            if ( !attendances.Any() ) //There was an attendance record, but not for the selected location
            {
                return new Response( false, string.Format( "{0} is not checked-in to {1} would you like to override?", person.FullName, location.Name ), true );
            }

            foreach ( var attendance in attendances )
            {
                attendance.EndDateTime = Rock.RockDateTime.Now;
            }

            var summary = string.Format( "Exited <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );

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
                RelatedEntityId = req.LocationId,
                Verb = "Exit",
                Summary = summary,
                Caption = "Exited Location",
                RelatedData = hostInfo,
                CategoryId = 4
            };

            historyService.Add( history );
            rockContext.SaveChanges();


            var message = string.Format( "{0} has been checked-out of {1}.", person.FullName, location.Name );
            return new Response( true, message, false );
        }


        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/org.secc/roomscanner/entry" )]
        public Response Entry( [FromBody] Request req )
        {
            var attendanceGuidGuid = req.AttendanceGuid.AsGuid();
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            Attendance attendeeAttendance = attendanceService.Get( attendanceGuidGuid );
            int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
            int locationEntityTypeId = EntityTypeCache.Read( locationEntityTypeGuid.AsGuid() ).Id;

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

            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.StartDateTime >= today && a.StartDateTime < tomorrow && a.EndDateTime == null );

            if ( !attendances.Any() )
            {
                return new Response( false, string.Format( "{0} has been checked out of all locations.", person.FullName ), false );
            }

            if ( !req.Override )
            {
                attendances = attendances.Where( a => a.LocationId == req.LocationId );
            }

            if ( !attendances.Any() ) //There was an attendance record, but not for the selected location
            {
                return new Response( false, string.Format( "{0} is not checked-in to {1} would you like to override?", person.FullName, location.Name ), true );
            }

            foreach ( var attendance in attendances.ToList() )
            {
                attendance.DidAttend = true;
            }

            var summary = string.Format( "Entered <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );

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
                RelatedEntityId = req.LocationId,
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
                return new Response( true, string.Format( "{0} has been checked-in to {1}. \n\n Allergy: {2}", person.FullName, location.Name, allergyAttributeValue.Value ), false, true );
            }
            var message = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
            return new Response( true, message, false );
        }
    }

    public class AttendanceCodes
    {
        public String NickName { get; set; }
        public String LastName { get; set; }
        public List<string> Codes { get; set; }
    }

    public class AttendanceEntry
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool DidAttend { get; set; }
        public string AttendanceGuid { get; set; }
    }

    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Attendee
    {
        public Guid AttendanceGuid { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public bool DidAttend { get; set; }
        public bool CheckedOut { get; set; }
    }

    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool Overridable { get; set; }
        public bool RequireConfirmation { get; set; }

        public Response( bool success, string message, bool overridable, bool requireConfirmation = false )
        {
            this.Success = success;
            this.Message = message;
            this.Overridable = overridable;
            this.RequireConfirmation = requireConfirmation;
        }
    }

    public class Request
    {
        public string AttendanceGuid { get; set; }
        public int LocationId { get; set; }
        public bool Override { get; set; }
    }

}
