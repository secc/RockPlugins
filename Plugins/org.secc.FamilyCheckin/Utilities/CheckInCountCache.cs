using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;
using Rock.Model;

namespace org.secc.FamilyCheckin.Utilities
{
    public static class CheckInCountCache
    {
        private static Object LOCK = new Object();

        private const string cacheKey = "CheckInCountCache";
        public static List<GroupLocationScheduleCount> GetByLocation( int locationId )
        {
            ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
            var glsc = ( List<GroupLocationScheduleCount> ) cache.Get( cacheKey );
            if ( glsc == null )
            {
                glsc = UpdateCache();
            }
            return glsc.Where( a => a.LocationId == locationId ).ToList();
        }

        public static void AddAttendance( Attendance attendance )
        {
            lock ( LOCK )
            {

                ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
                var lglsc = ( List<GroupLocationScheduleCount> ) cache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }
                GroupLocationScheduleCount glsc = lglsc.FirstOrDefault( g =>
                                                                    g.LocationId == attendance.LocationId
                                                                    && g.ScheduleId == attendance.ScheduleId
                                                                    && g.GroupId == attendance.GroupId );
                if ( glsc == null )
                {
                    glsc = new GroupLocationScheduleCount()
                    {
                        LocationId = attendance.LocationId ?? 0,
                        ScheduleId = attendance.ScheduleId ?? 0,
                        GroupId = attendance.GroupId ?? 0,
                        PersonIds = new List<int>()
                    };
                }

                if ( !glsc.PersonIds.Contains( attendance.PersonAlias?.PersonId ?? 0 ) )
                {
                    glsc.PersonIds.Add( attendance.PersonAlias.PersonId );
                }
                var cachePolicy = new CacheItemPolicy();
                cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                cache.Set( cacheKey, lglsc, cachePolicy );
            }
        }

        public static void RemoveAttendance( Attendance attendance )
        {
            lock ( LOCK )
            {
                ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
                var lglsc = ( List<GroupLocationScheduleCount> ) cache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }
                GroupLocationScheduleCount glsc = lglsc.FirstOrDefault( g =>
                                                                    g.LocationId == attendance.LocationId
                                                                    && g.ScheduleId == attendance.ScheduleId
                                                                    && g.GroupId == attendance.GroupId );
                if ( glsc == null )
                {
                    glsc = new GroupLocationScheduleCount()
                    {
                        LocationId = attendance.LocationId ?? 0,
                        ScheduleId = attendance.ScheduleId ?? 0,
                        GroupId = attendance.GroupId ?? 0,
                        PersonIds = new List<int>()
                    };
                }

                if ( glsc.PersonIds.Contains( attendance.PersonAlias?.PersonId ?? 0 ) )
                {
                    glsc.PersonIds.Remove( attendance.PersonAlias.PersonId );
                }
                var cachePolicy = new CacheItemPolicy();
                cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                cache.Set( cacheKey, lglsc, cachePolicy );
            }
        }

        public static void Flush()
        {
            lock ( LOCK )
            {
                UpdateCache();
            }
        }

        private static List<GroupLocationScheduleCount> UpdateCache()
        {
            var output = new List<GroupLocationScheduleCount>();

            var today = Rock.RockDateTime.Today;
            var tomorrow = Rock.RockDateTime.Today.AddDays( 1 );
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable()
                .Where( a =>
                            a.GroupId != null
                            && a.LocationId != null
                            && a.ScheduleId != null
                            && a.StartDateTime >= today
                            && a.StartDateTime < tomorrow
                            && a.EndDateTime == null )
                .GroupBy( a => new { a.GroupId, a.LocationId, a.ScheduleId } )
                .ToList();
            foreach ( var attendance in attendances )
            {
                var glsc = new GroupLocationScheduleCount()
                {
                    GroupId = attendance.Key.GroupId ?? 0,
                    LocationId = attendance.Key.LocationId ?? 0,
                    ScheduleId = attendance.Key.ScheduleId ?? 0,
                    PersonIds = attendance.Select( a => a.PersonAlias?.PersonId ?? 0 ).ToList()
                };
                output.Add( glsc );
            }

            var cachePolicy = new CacheItemPolicy();
            cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
            ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
            cache.Set( cacheKey, output, cachePolicy );
            return output;
        }
    }
}
