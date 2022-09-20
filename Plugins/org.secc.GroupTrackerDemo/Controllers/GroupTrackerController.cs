using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using org.secc.GroupTrackerDemo.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;


namespace org.secc.GroupTrackerDemo.Controllers
{
    public partial class GroupTrackerController : ApiControllerBase
    {

        [HttpGet]
        [System.Web.Http.Route( "api/GroupTracker/GroupSummaryByLeader/{personAliasId}" )]
        public IHttpActionResult GetGroupSummaryByLeader( int personAliasId, string includedGroupTypeIds = null )
        {
            var rockContext = new RockContext();
            var groupTypeIdsList = new List<int>();
            if ( includedGroupTypeIds.IsNotNullOrWhiteSpace() )
            {
                groupTypeIdsList.AddRange( includedGroupTypeIds.Split( ",".ToCharArray() ).Select( t => t.AsInteger() ).Where( t => t > 0 ).ToList() );
            }

            var person = new PersonAliasService( rockContext ).GetPerson( personAliasId );

            var groupQry = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                .Where( m => m.PersonId == person.Id )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Where( m => !m.IsArchived )
                .Where( m => m.GroupRole.IsLeader )
                .Where( m => m.Group.IsActive && !m.Group.IsArchived );

            if ( groupTypeIdsList.Count > 0 )
            {
                groupQry = groupQry.Where( m => groupTypeIdsList.Contains( m.Group.GroupTypeId ) );
            }

            var groups = groupQry.GroupBy( g => g.Group )
                .Select( g => new GroupSummary
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    GroupTypeId = g.Key.GroupTypeId,
                    GroupTypeName = g.Key.GroupType.Name,
                    Guid = g.Key.Guid,
                    IsActive = g.Key.IsActive && !g.Key.IsArchived
                } ).ToList();

            return Json( groups );
        }

        [HttpGet]
        [System.Web.Http.Route( "api/GroupTracker/GroupOccurrenceAttendance/{groupGuid}" )]
        public IHttpActionResult GetGroupOccurrenceAttendance( string groupGuid )
        {
            var rockContext = new RockContext();
            var guid = groupGuid.AsGuid();
            var today = RockDateTime.Today;

            var mobilePhoneDVID = Rock.Web.Cache.DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id;

            var mobilePhoneQry = new PhoneNumberService( rockContext ).Queryable().AsNoTracking()
                .Where( m => !m.IsUnlisted )
                .Where( m => m.NumberTypeValueId == mobilePhoneDVID );

            var attendance = new AttendanceService( rockContext ).Queryable().AsNoTracking()
                .Where( a => a.Occurrence.OccurrenceDate == today );

            var group = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                .Where( gm => gm.Group.Guid == guid )
                .Where( gm => gm.Group.IsActive && !gm.Group.IsArchived )
                .Where( gm => !gm.GroupRole.IsLeader )
                .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                .GroupJoin( attendance, gm => new { gm.PersonId, gm.GroupId }, a => new { a.PersonAlias.PersonId, GroupId = a.Occurrence.GroupId ?? -1 },
                    ( gm, a ) => new { GroupMember = gm, Attendance = a.FirstOrDefault() } )
                .GroupJoin( mobilePhoneQry, gm => gm.GroupMember.PersonId, mp => mp.PersonId,
                    ( gm, mp ) => new
                    {
                        Group = gm.GroupMember.Group,
                        GroupMemberId = gm.GroupMember.Id,
                        PersonId = gm.GroupMember.PersonId,
                        NickName = gm.GroupMember.Person.NickName,
                        LastName = gm.GroupMember.Person.LastName,
                        BirthDate = gm.GroupMember.Person.BirthDate,
                        PhotoId = gm.GroupMember.Person.PhotoId,
                        MobilePhone = mp.FirstOrDefault() != null ? mp.Select( mp1 => mp1.NumberFormatted ).FirstOrDefault() : null,
                        OccurrenceId = gm.Attendance != null ? gm.Attendance.OccurrenceId : ( int? ) null,
                        ScheduleId = gm.Attendance != null ? gm.Attendance.Occurrence.ScheduleId : null,
                        ScheduleName = gm.Attendance != null ? gm.Attendance.Occurrence.Schedule.Name : null,
                        LocationId = gm.Attendance != null ? gm.Attendance.Occurrence.LocationId : null,
                        LocationName = gm.Attendance != null ? gm.Attendance.Occurrence.Location.Name : null,
                        OccurrenceDate = gm.Attendance == null ? ( System.DateTime? ) null : gm.Attendance.Occurrence.OccurrenceDate,
                        AttendanceId = gm.Attendance != null ? gm.Attendance.Id : ( int? ) null,
                        StartDateTime = gm.Attendance != null ? ( System.DateTime? ) gm.Attendance.StartDateTime : null,
                        EndDateTime = gm.Attendance != null ? ( System.DateTime? ) gm.Attendance.EndDateTime : null,
                        DidAttend = gm.Attendance != null ? gm.Attendance.DidAttend : null,
                        PresentDateTime = gm.Attendance != null ? ( System.DateTime? ) gm.Attendance.PresentDateTime : null
                    }
                )
                .GroupBy( gm => gm.Group )
                .Select( g => new GroupWithOccurrence
                {
                    GroupId = g.Key.Id,
                    GroupName = g.Key.Name,
                    GroupTypeId = g.Key.GroupTypeId,
                    GroupTypeName = g.Key.GroupType.Name,
                    OccurrenceId = g.Select( g1 => g1.OccurrenceId ).FirstOrDefault(),
                    ScheduleId = g.Select( g1 => g1.ScheduleId ).FirstOrDefault(),
                    ScheduleName = g.Select( g1 => g1.ScheduleName ).FirstOrDefault(),
                    LocationId = g.Select( g1 => g1.LocationId ).FirstOrDefault(),
                    LocationName = g.Select( g1 => g1.LocationName ).FirstOrDefault(),
                    OccurrenceDate = g.Select( g1 => g1.OccurrenceDate ).FirstOrDefault(),
                    GroupMember = g.Select( gm => new GroupMemberWithAttendance
                    {
                        GroupMemberId = gm.GroupMemberId,
                        PersonId = gm.PersonId,
                        NickName = gm.NickName,
                        LastName = gm.LastName,
                        BirthDate = gm.BirthDate,
                        MobilePhone = gm.MobilePhone,
                        PhotoId = gm.PhotoId,
                        AttendanceId = gm.AttendanceId,
                        StartDateTime = gm.StartDateTime,
                        EndDateTime = gm.EndDateTime,
                        DidAttend = gm.DidAttend,
                        PresentDateTime = gm.PresentDateTime
                    } ).ToList()
                } ).FirstOrDefault();

            if ( group == null )
            {
                return NotFound();
            }


            return Json( group );

        }


        [HttpPost]
        [System.Web.Http.Route( "api/GroupTracker/UpdateAttendedStatus/{attendanceId}/{isPresent}" )]
        public IHttpActionResult UpdateAttendedStatus(int attendanceId, bool isPresent)
        {
            var rockContext = new RockContext();
            var attendanceService = new AttendanceService( rockContext );

            var attendance = attendanceService.Get( attendanceId );

            if(attendance == null)
            {
                return NotFound();
            }
            
            if(isPresent)
            {
                attendance.DidAttend = true;
                
                if(!attendance.PresentDateTime.HasValue)
                {
                    attendance.PresentDateTime = RockDateTime.Now;
                }
            }
            else
            {
                attendance.DidAttend = false;
                attendance.PresentDateTime = null;
                attendance.PresentByPersonAliasId = null;
            }

            rockContext.SaveChanges();

            return StatusCode( System.Net.HttpStatusCode.Accepted );
        }
    }
}
