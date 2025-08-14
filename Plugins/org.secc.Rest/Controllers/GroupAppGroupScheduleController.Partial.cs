using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace org.secc.Rest.Controllers
{
    public class GroupAppGroupScheduleController : ApiControllerBase
    {
        /// <summary>
        /// Get the group's current schedule summary.
        /// </summary>
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/Groups/{groupId:int}/Schedule" )]
        public IHttpActionResult GetGroupSchedule( int groupId )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }


            using ( var rockContext = new RockContext() )
            {
                rockContext.Configuration.ProxyCreationEnabled = false;

                var groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                if ( group == null )
                {
                    return NotFound();
                }

                if ( !IsLeaderOfGroup( rockContext, currentUser.Person.Id, groupId ) )
                {
                    return StatusCode( HttpStatusCode.Forbidden );
                }

                rockContext.Entry( group ).Reference( g => g.Schedule ).Load();
                var schedule = group.Schedule;
                if ( schedule == null )
                {
                    return NotFound();
                }

                // Prefer stored iCal; synthesize from legacy weekly fields when needed
                var ical = schedule.iCalendarContent;
                if ( string.IsNullOrWhiteSpace( ical ) && schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue )
                {
                    var effectiveStart = schedule.EffectiveStartDate ?? RockDateTime.Today;
                    ical = BuildWeeklyICal(
                        schedule.WeeklyDayOfWeek.Value,
                        schedule.WeeklyTimeOfDay.Value,
                        effectiveStart,
                        schedule.EffectiveEndDate,
                        null /*duration*/ );
                }

                return Ok( new { ICalendarContent = ical ?? string.Empty } );
            }
        }

        /// <summary>
        /// Update the group's weekly schedule. If the current schedule is shared, a new schedule is created and assigned to the group.
        /// </summary>
        [HttpPut]
        [System.Web.Http.Route( "api/GroupApp/Groups/{groupId:int}/Schedule" )]
        public IHttpActionResult UpdateGroupSchedule( int groupId, GroupScheduleUpdateRequest request )
        {
            if ( request == null )
            {
                return BadRequest( "Request body is required." );
            }

            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            if ( string.IsNullOrWhiteSpace( request.ICalendarContent ) )
            {
                return BadRequest( "ICalendarContent is required." );
            }

            if ( !LooksLikeICal( request.ICalendarContent ) )
            {
                return BadRequest( "ICalendarContent must contain BEGIN:VCALENDAR, BEGIN:VEVENT, and DTSTART." );
            }

            using ( var rockContext = new RockContext() )
            {
                rockContext.Configuration.ProxyCreationEnabled = false;

                var groupService = new GroupService( rockContext );
                var scheduleService = new ScheduleService( rockContext );

                var group = groupService.Get( groupId );
                if ( group == null )
                {
                    return NotFound();
                }

                if ( !IsLeaderOfGroup( rockContext, currentUser.Person.Id, groupId ) )
                {
                    return StatusCode( HttpStatusCode.Forbidden );
                }

                // Ensure we update a schedule not shared by others
                Schedule scheduleToUpdate = null;

                if ( group.ScheduleId.HasValue )
                {
                    var currentSchedule = scheduleService.Get( group.ScheduleId.Value );
                    if ( currentSchedule != null )
                    {
                        if ( IsScheduleShared( rockContext, currentSchedule.Id, groupId ) )
                        {
                            scheduleToUpdate = new Schedule
                            {
                                Name = group.Name + " Schedule",
                                Description = currentSchedule.Description,
                                IsActive = true
                            };
                            scheduleService.Add( scheduleToUpdate );
                            rockContext.SaveChanges();

                            group.ScheduleId = scheduleToUpdate.Id;
                        }
                        else
                        {
                            scheduleToUpdate = currentSchedule;
                        }
                    }
                }

                if ( scheduleToUpdate == null )
                {
                    scheduleToUpdate = new Schedule
                    {
                        Name = group.Name + " Schedule",
                        IsActive = true
                    };
                    scheduleService.Add( scheduleToUpdate );
                    rockContext.SaveChanges();

                    group.ScheduleId = scheduleToUpdate.Id;
                }

                // Apply dates (optional metadata)
                scheduleToUpdate.EffectiveStartDate = request.EffectiveStartDate;
                scheduleToUpdate.EffectiveEndDate = request.EffectiveEndDate;
                scheduleToUpdate.IsActive = true;

                // Store iCal and clear legacy weekly properties
                scheduleToUpdate.iCalendarContent = request.ICalendarContent;
                scheduleToUpdate.WeeklyDayOfWeek = null;
                scheduleToUpdate.WeeklyTimeOfDay = null;

                rockContext.SaveChanges();

                return Ok( new
                {
                    ICalendarContent = scheduleToUpdate.iCalendarContent,
                    EffectiveStartDate = scheduleToUpdate.EffectiveStartDate,
                    EffectiveEndDate = scheduleToUpdate.EffectiveEndDate,
                    NextOccurrence = scheduleToUpdate.GetNextStartDateTime( RockDateTime.Today )
                } );
            }
        }

        private static bool LooksLikeICal( string content )
        {
            var c = ( content ?? string.Empty ).ToUpperInvariant();
            return c.Contains( "BEGIN:VCALENDAR" ) && c.Contains( "BEGIN:VEVENT" ) && c.Contains( "DTSTART" );
        }

        private static string BuildWeeklyICal( DayOfWeek dayOfWeek, TimeSpan timeOfDay, DateTime effectiveStart, DateTime? effectiveEnd = null, TimeSpan? duration = null )
        {
            // Back-compat overload (single day, no COUNT)
            return BuildWeeklyICal( new[] { dayOfWeek }, timeOfDay, effectiveStart, effectiveEnd, null, 1, duration.HasValue ? ( int? ) duration.Value.TotalMinutes : null );
        }

        private static string BuildWeeklyICal( DayOfWeek[] daysOfWeek, TimeSpan timeOfDay, DateTime effectiveStart, DateTime? effectiveEnd, int? count, int interval, int? durationMinutes )
        {
            // Align DTSTART to the first matching weekday on/after effectiveStart
            var orderedDays = daysOfWeek.Distinct().ToArray();
            var first = effectiveStart.Date;
            var minDelta = Enumerable.Range( 0, 7 )
                .Select( d => new { d, date = first.AddDays( d ) } )
                .Where( x => orderedDays.Contains( x.date.DayOfWeek ) )
                .Select( x => x.d )
                .DefaultIfEmpty( 0 )
                .First();

            var firstDate = first.AddDays( minDelta );
            var dtstart = new DateTime( firstDate.Year, firstDate.Month, firstDate.Day, timeOfDay.Hours, timeOfDay.Minutes, 0 );
            var dtStartString = dtstart.ToString( "yyyyMMdd'T'HHmmss" );

            var byday = string.Join( ",", orderedDays.Select( ToByDay ) );
            var rrule = "FREQ=WEEKLY;INTERVAL=" + Math.Max( 1, interval ) + ";BYDAY=" + byday;

            if ( count.HasValue && count.Value > 0 )
            {
                rrule += ";COUNT=" + count.Value;
            }
            else if ( effectiveEnd.HasValue )
            {
                var until = new DateTime( effectiveEnd.Value.Year, effectiveEnd.Value.Month, effectiveEnd.Value.Day, timeOfDay.Hours, timeOfDay.Minutes, 0 );
                rrule += ";UNTIL=" + until.ToString( "yyyyMMdd'T'HHmmss" );
            }

            var dur = TimeSpan.FromMinutes( durationMinutes.HasValue && durationMinutes.Value > 0 ? durationMinutes.Value : 1 );
            var durStr = ToICalDuration( dur );

            return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nDTSTART:" + dtStartString +
                   "\r\nDURATION:" + durStr +
                   "\r\nRRULE:" + rrule +
                   "\r\nEND:VEVENT\r\nEND:VCALENDAR";
        }

        private static string ToICalDuration( TimeSpan dur )
        {
            var sb = "PT";
            if ( dur.Hours > 0 )
                sb += dur.Hours + "H";
            if ( dur.Minutes > 0 )
                sb += dur.Minutes + "M";
            if ( dur.Hours == 0 && dur.Minutes == 0 )
                sb += "1M";
            return sb;
        }


        private static string ToByDay( DayOfWeek dayOfWeek )
        {
            switch ( dayOfWeek )
            {
                case DayOfWeek.Monday:
                    return "MO";
                case DayOfWeek.Tuesday:
                    return "TU";
                case DayOfWeek.Wednesday:
                    return "WE";
                case DayOfWeek.Thursday:
                    return "TH";
                case DayOfWeek.Friday:
                    return "FR";
                case DayOfWeek.Saturday:
                    return "SA";
                default:
                    return "SU";
            }
        }

        private bool IsLeaderOfGroup( RockContext rockContext, int personId, int groupId )
        {
            var gmService = new GroupMemberService( rockContext );
            return gmService.Queryable()
                .Any( gm =>
                    gm.GroupId == groupId
                    && gm.PersonId == personId
                    && gm.IsArchived == false
                    && gm.GroupMemberStatus == GroupMemberStatus.Active
                    && gm.GroupRole.IsLeader );
        }

        private bool IsScheduleShared( RockContext rockContext, int scheduleId, int thisGroupId )
        {
            // Used by another group
            var usedByOtherGroup = new GroupService( rockContext ).Queryable()
                .Any( g => g.ScheduleId == scheduleId && g.Id != thisGroupId );

            if ( usedByOtherGroup )
            {
                return true;
            }

            // Check if schedule is used by any group locations through their schedules
            // Note: This assumes there's a relationship between GroupLocation and Schedule
            // You may need to adjust this based on your specific Rock version and schema
            var groupLocationService = new GroupLocationService( rockContext );
            var usedByOtherGroupLocation = groupLocationService.Queryable()
                .Where( gl => gl.GroupId != thisGroupId )
                .SelectMany( gl => gl.Schedules ) // This may need adjustment based on actual relationship
                .Any( s => s.Id == scheduleId );

            return usedByOtherGroupLocation;
        }

        public class GroupScheduleUpdateRequest
        {
            public string ICalendarContent { get; set; }               // required: raw VCALENDAR content
            public DateTime? EffectiveStartDate { get; set; }          // optional metadata
            public DateTime? EffectiveEndDate { get; set; }            // optional metadata
        }
    }
}