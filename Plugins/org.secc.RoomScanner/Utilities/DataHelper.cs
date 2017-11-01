using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.FamilyCheckin.Utilities;
using org.secc.RoomScanner.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RoomScanner.Utilities
{
    public static class DataHelper
    {
        private static int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
        private const string locationEntityTypeGuid = "0D6410AD-C83C-47AC-AF3D-616D09EDF63B";
        private static int locationEntityTypeId = EntityTypeCache.Read( locationEntityTypeGuid.AsGuid() ).Id;
        private static int allergyAttributeId = AttributeCache.Read( Rock.SystemGuid.Attribute.PERSON_ALLERGY.AsGuid() ).Id;

        public static string GetHostInfo()
        {
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
            return hostInfo;
        }



        public static void AddExitHistory( RockContext rockContext, Location location, Attendance attendeeAttendance, bool isSubroom )
        {
            HistoryService historyService = new HistoryService( rockContext );
            var summary = string.Format( "Exited <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );
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
                Verb = "Exit",
                Summary = summary,
                Caption = "Exited Location",
                RelatedData = GetHostInfo(),
                CategoryId = 4
            };

            historyService.Add( history );
            InMemoryPersonStatus.RemoveFromWorship( attendeeAttendance.PersonAlias.PersonId );
        }


        public static void AddMoveHistory( RockContext rockContext, Location location, Attendance attendeeAttendance, Person authorizedPerson, bool isSubroom )
        {
            HistoryService historyService = new HistoryService( rockContext );
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
                RelatedData = GetHostInfo(),
                CategoryId = 4
            };

            historyService.Add( moveHistory );
        }

        public static void AddEntranceHistory( RockContext rockContext, Location location, Attendance attendeeAttendance, bool isSubroom )
        {
            HistoryService historyService = new HistoryService( rockContext );
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
                RelatedData = GetHostInfo(),
                CategoryId = 4
            };

            historyService.Add( history );
        }

        public static void AddWithParentHistory( RockContext rockContext, Person person )
        {
            HistoryService historyService = new HistoryService( rockContext );
            var summary = string.Format( "Moved to be with Parent at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = person.Id,
                Verb = "Moved",
                Summary = summary,
                Caption = "Moved be with Parent",
                RelatedData = GetHostInfo(),
                CategoryId = 4
            };
            historyService.Add( history );
            InMemoryPersonStatus.AddToWithParent( person.Id );
        }

        public static void AddMoveTwoWorshipHistory( RockContext rockContext, Person person )
        {
            HistoryService historyService = new HistoryService( rockContext );
            var summary = string.Format( "Moved to Worship at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );

            History history = new History()
            {
                EntityTypeId = personEntityTypeId,
                EntityId = person.Id,
                Verb = "Moved",
                Summary = summary,
                Caption = "Moved To Worship",
                RelatedData = GetHostInfo(),
                CategoryId = 4
            };
            historyService.Add( history );
            InMemoryPersonStatus.AddToWorship( person.Id );
        }

        public static void AddReturnToRoomHistory( RockContext rockContext, Person person )
        {
            var summary = "";
            var caption = "";
            HistoryService historyService = new HistoryService( rockContext );
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
                    RelatedData = GetHostInfo(),
                    CategoryId = 4
                };
                historyService.Add( history );
            }
        }

        public static void CloneAttendance( Attendance attendance, bool isSubroom, Location location, AttendanceService attendanceService, Request req )
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

        public static Response GetEntryResponse( RockContext rockContext, Person person, Location location )
        {

            var allergyAttributeValue = new AttributeValueService( rockContext )
                .Queryable()
                .FirstOrDefault( av => av.AttributeId == allergyAttributeId && av.EntityId == person.Id );
            if ( allergyAttributeValue != null
                && !string.IsNullOrWhiteSpace( allergyAttributeValue.Value ) )
            {
                return new Response( true,
                    string.Format( "{0} has been checked-in to {1}. \n\n Allergy: {2}", person.FullName, location.Name, allergyAttributeValue.Value ),
                    false,
                    true,
                    person.Id
                    );
            }
            var message2 = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
            return new Response( true, message2, false, personId: person.Id );
        }

        public static void CloseActiveAttendances( RockContext rockContext, Attendance attendeeAttendance, Location location, bool isSubroom )
        {
            var activeAttendances = ValidationHelper.GetActiveAttendances( rockContext, attendeeAttendance, location );
            foreach ( var activeAttendance in activeAttendances )
            {
                activeAttendance.EndDateTime = Rock.RockDateTime.Now;
                AddExitHistory( rockContext, attendeeAttendance.Location, attendeeAttendance, isSubroom );
            }
        }
    }
}
