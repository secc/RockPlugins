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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.FamilyCheckin.Cache;
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
        private static int personEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
        private const string locationEntityTypeGuid = "0D6410AD-C83C-47AC-AF3D-616D09EDF63B";
        private static int locationEntityTypeId = EntityTypeCache.Get( locationEntityTypeGuid.AsGuid() ).Id;
        private static int allergyAttributeId = AttributeCache.Get( Rock.SystemGuid.Attribute.PERSON_ALLERGY.AsGuid() ).Id;

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
            AttendanceCache.RemoveWithParent( attendeeAttendance.PersonAlias.PersonId );
        }


        public static void AddMoveHistory( RockContext rockContext, Location location, Attendance attendeeAttendance, bool isSubroom )
        {
            HistoryService historyService = new HistoryService( rockContext );
            var moveSummary = string.Format( "Moved to and Entered <span class=\"field-name\">{0}</span> at <span class=\"field-name\">{1}</span>", location.Name, Rock.RockDateTime.Now );
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
                Verb = "Entry",
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
            AttendanceCache.SetWithParent( person.Id );
        }


        public static void AddReturnToRoomHistory( RockContext rockContext, Person person )
        {
            HistoryService historyService = new HistoryService( rockContext );
            if ( AttendanceCache.IsWithParent( person.Id ) )
            {
                AttendanceCache.RemoveWithParent( person.Id );
                var summary = string.Format( "Returned from Parent at <span class=\"field-name\">{0}</span>", Rock.RockDateTime.Now );

                History history = new History()
                {
                    EntityTypeId = personEntityTypeId,
                    EntityId = person.Id,
                    Verb = "Returned",
                    Summary = summary,
                    Caption = "Returned from Parent",
                    RelatedData = GetHostInfo(),
                    CategoryId = 4
                };
                historyService.Add( history );
                //HistoryService.SaveChanges( rockContext, typeof( Person ), Rock.SystemGuid.Category.HISTORY_PERSON, person.Id, memberChanges, newFamily.Name, typeof( Group ), newFamily.Id, false, null, SOURCE_OF_CHANGE );

            }
        }

        public static void CloneAttendance( Attendance attendance, bool isSubroom, Location location, AttendanceService notUsed, Request req )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            Attendance newAttendance;
            if ( !isSubroom )
            {
                newAttendance = attendanceService.AddOrUpdate( attendance.PersonAliasId, attendance.StartDateTime.Date, attendance.Occurrence.GroupId,
                                                        location.Id, attendance.Occurrence.ScheduleId, location.CampusId,
                                                        null, null, null, null, null, null );
            }
            else
            {
                newAttendance = attendanceService.AddOrUpdate( attendance.PersonAliasId, attendance.StartDateTime.Date, attendance.Occurrence.GroupId,
                                                        location.ParentLocationId, attendance.Occurrence.ScheduleId, location.CampusId,
                                                        null, null, null, null, null, null );
            }
            newAttendance.StartDateTime = Rock.RockDateTime.Now;
            newAttendance.EndDateTime = null;
            newAttendance.DidAttend = true;
            newAttendance.AttendanceCodeId = attendance.AttendanceCodeId;
            if ( isSubroom )
            {
                newAttendance.ForeignId = location.Id;
            }
            else
            {
                newAttendance.ForeignId = null;
            }
            attendanceService.Add( newAttendance );
            var stayedFifteenMinutes = ( Rock.RockDateTime.Now - attendance.StartDateTime ) > new TimeSpan( 0, 15, 0 );
            attendance.DidAttend = stayedFifteenMinutes;
            attendance.EndDateTime = Rock.RockDateTime.Now;

            rockContext.SaveChanges();

            AttendanceCache.RemoveWithParent( attendance.PersonAlias.PersonId );
            AttendanceCache.AddOrUpdate( newAttendance );
            AttendanceCache.AddOrUpdate( attendance );
        }

        public static Response GetEntryResponse( RockContext rockContext, Person person, Location location )
        {
            var birthdateText = string.Empty;
            if ( ( person.DaysToBirthday < 4 || person.DaysToBirthday > 363 ) && person.DaysToBirthday != int.MaxValue )
            {
                var birthdate = new DateTime( RockDateTime.Now.Year, person.BirthDate.Value.Month, person.BirthDate.Value.Day );
                if ( birthdate == RockDateTime.Today )
                {
                    birthdateText = string.Format( "{0}'s Birthday is Today! {1}", person.NickName, GetAge( person ) );
                }
                else
                {
                    var elapsedString = birthdate.ToElapsedString( false, false );
                    birthdateText = string.Format( "{0}'s Birthday {1} {2}! {3}", person.NickName, elapsedString.Contains( "Ago" ) ? "was" : "is", elapsedString, GetAge( person ) );
                }
            }
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
                    person.Id,
                    birthdateText
                    );
            }
            var message2 = string.Format( "{0} has been checked-in to {1}.", person.FullName, location.Name );
            return new Response( true, message2, false, personId: person.Id, birthdayText: birthdateText );
        }

        private static string GetAge( Person person )
        {
            if ( person.Age > 18 )
            {
                return "";
            }
            return string.Format( "{0} years old.", RockDateTime.Today.Year - person.BirthYear );
        }

        public static void CloseActiveAttendances( RockContext rockContext, Attendance attendeeAttendance, Location location, bool isSubroom )
        {
            var activeAttendances = ValidationHelper.GetActiveAttendances( rockContext, attendeeAttendance, location );
            bool didRemove = false;
            foreach ( var activeAttendance in activeAttendances )
            {
                didRemove = true;
                var stayedFifteenMinutes = ( Rock.RockDateTime.Now - activeAttendance.StartDateTime ) > new TimeSpan( 0, 15, 0 );
                activeAttendance.DidAttend = stayedFifteenMinutes;
                activeAttendance.EndDateTime = Rock.RockDateTime.Now;
                AddExitHistory( rockContext, attendeeAttendance.Occurrence.Location, attendeeAttendance, isSubroom );
                AttendanceCache.AddOrUpdate( activeAttendance );
            }
            if ( didRemove )
            {
                var personId = attendeeAttendance.PersonAlias.PersonId;
                AttendanceCache.RemoveWithParent( personId );
            }
        }
    }
}
