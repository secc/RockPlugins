using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.SessionState;
using Rock.Rest.Filters;
using Rock.Data;
using Rock.Model;
using System.Data.Entity;
using Rock;
using Rock.Web.Cache;

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
                .Select( t => new Template() { Id = t.Id, Name = t.Name } )
                    .OrderBy( t => t.Name )

                .ToList()
                .Where( t => t.Name.ToLower().Contains( "kids" ) )
                .ToList();

            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/areas/{id}" )]
        public List<Template> Areas( int id )
        {
            RockContext rockContext = new RockContext();
            GroupTypeService groupTypeService = new GroupTypeService( rockContext );
            List<Template> templates = groupTypeService
                .Queryable().AsNoTracking()
                .Where( t =>
                 t.ParentGroupTypes.Select( p => p.Id ).Contains( id ) )
                .Select( t => new Template() { Id = t.Id, Name = t.Name } )
                .OrderBy( t => t.Name )
                .ToList();
            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/groups/{id}" )]
        public List<Template> Groups( int id )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            List<Template> templates = groupService
                .Queryable().AsNoTracking()
                .Where( t => t.GroupTypeId == id )
                .Select( t => new Template() { Id = t.Id, Name = t.Name } )
                .OrderBy( t => t.Name )
                .ToList();
            return templates;
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/locations/{id}" )]
        public List<Template> Locations( int id )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            List<Template> templates = groupService
                .Queryable().AsNoTracking()
                .Where( t => t.Id == id )
                .SelectMany( g => g.GroupLocations )
                .Select( gl => gl.Location )
                .Select( t => new Template() { Id = t.Id, Name = t.Name } )
                .OrderBy( t => t.Name )
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
            return new Template() { Name = location.Name, Id = location.Id };
        }

        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/attendees/{id}" )]
        public List<Attendee> Attendees( int id )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            List<Attendee> attendees = attendanceService
                .Queryable().AsNoTracking()
                .Where( a => a.LocationId == id && a.StartDateTime >= Rock.RockDateTime.Today )
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
        [System.Web.Http.Route( "api/org.secc/roomscanner/exit/{guidString}/{locationId}" )]
        public Response Exit( string guidString, int locationId )
        {
            var attendanceGuid = guidString.AsGuid();
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            Attendance attendeeAttendance = attendanceService.Get( attendanceGuid );
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
            var location = new LocationService( rockContext ).Get( locationId );

            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.LocationId == locationId );

            foreach ( var attendance in attendances.ToList() )
            {
                attendance.DidAttend = true;
            }

            var summary = string.Format( "Exited <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = attendeeAttendance.PersonAlias.PersonId,
                RelatedEntityTypeId = locationEntityTypeId,
                RelatedEntityId = locationId,
                Verb = "Exit",
                Summary = summary,
                Caption = "Exited Location",
                RelatedData = System.Net.Dns.GetHostEntry( Rock.Web.UI.RockPage.GetClientIpAddress() ).HostName,
                CategoryId = 4
            };

            historyService.Add( history );
            rockContext.SaveChanges();

            var message = string.Format( "{0} has been checked-out of {1}.", person.FullName, location.Name );
            return new Response( true, message, false );
        }


        [Authenticate, Secured]
        [HttpGet]
        [System.Web.Http.Route( "api/org.secc/roomscanner/entry/{guidString}/{locationId}" )]
        public Response Entry( string guidString, int locationId )
        {
            var attendanceGuid = guidString.AsGuid();
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            Attendance attendeeAttendance = attendanceService.Get( attendanceGuid );
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
            var location = new LocationService( rockContext ).Get( locationId );

            var attendances = attendanceService.Queryable()
                .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId && a.LocationId == locationId );

            foreach ( var attendance in attendances.ToList() )
            {
                attendance.EndDateTime = Rock.RockDateTime.Now;
            }

            var summary = string.Format( "Entered <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = attendeeAttendance.PersonAlias.PersonId,
                RelatedEntityTypeId = locationEntityTypeId,
                RelatedEntityId = locationId,
                Verb = "Entry",
                Summary = summary,
                Caption = "Entered Location",
                RelatedData = System.Net.Dns.GetHostEntry( Rock.Web.UI.RockPage.GetClientIpAddress() ).HostName,
                CategoryId = 4
            };

            historyService.Add( history );
            rockContext.SaveChanges();

            var message = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
            return new Response( true, message, false );
        }
    }


    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; }
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

        public Response( bool success, string message, bool overridable )
        {
            this.Success = success;
            this.Message = message;
            this.Overridable = overridable;
        }
    }
}
