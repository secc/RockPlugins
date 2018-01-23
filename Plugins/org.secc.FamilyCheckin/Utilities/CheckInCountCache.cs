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
                                                                    g.LocationId == ( attendance.LocationId ?? 0 )
                                                                    && g.ScheduleId == ( attendance.ScheduleId ?? 0 )
                                                                    && g.GroupId == ( attendance.GroupId ?? 0 ) );
                if ( glsc == null )
                {
                    glsc = new GroupLocationScheduleCount()
                    {
                        LocationId = attendance.LocationId ?? 0,
                        ScheduleId = attendance.ScheduleId ?? 0,
                        GroupId = attendance.GroupId ?? 0,
                        PersonIds = new List<int>(),
                        InRoomPersonIds = new List<int>()

                    };
                    lglsc.Add( glsc );
                }


                if ( !glsc.PersonIds.Contains( attendance.PersonAlias?.PersonId ?? 0 ) )
                {
                    glsc.PersonIds.Add( attendance.PersonAlias.PersonId );
                }

                if ( attendance.DidAttend == true && !glsc.InRoomPersonIds.Contains( attendance.PersonAlias?.PersonId ?? 0 ) )
                {
                    glsc.InRoomPersonIds.Add( attendance.PersonAlias.PersonId );
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

                var personId = attendance.PersonAlias.PersonId;

                var items = lglsc.Where( g =>
                                g.LocationId == attendance.LocationId
                                && g.ScheduleId == attendance.ScheduleId
                                && g.GroupId == attendance.GroupId )
                    .ToList();

                foreach ( var glsc in items )
                {
                    while ( glsc.PersonIds.Contains( personId ) )
                    {
                        glsc.PersonIds.Remove( personId );
                    }
                    while ( glsc.InRoomPersonIds.Contains( personId ) )
                    {
                        glsc.InRoomPersonIds.Remove( personId );
                    }
                    var cachePolicy = new CacheItemPolicy();
                    cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                    cache.Set( cacheKey, lglsc, cachePolicy );
                }
            }
        }

        public static void UpdateAttendance( Attendance attendance )
        {
            lock ( LOCK )
            {
                ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
                var lglsc = ( List<GroupLocationScheduleCount> ) cache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }

                var personId = attendance.PersonAlias.PersonId;

                var items = lglsc.Where( g =>
                                g.LocationId == attendance.LocationId
                                && g.ScheduleId == attendance.ScheduleId
                                && g.GroupId == attendance.GroupId )
                    .ToList();

                foreach ( var glsc in items )
                {
                    if ( attendance.EndDateTime != null )
                    {
                        while ( glsc.PersonIds.Contains( personId ) )
                        {
                            glsc.PersonIds.Remove( personId );
                        }
                        while ( glsc.InRoomPersonIds.Contains( personId ) )
                        {
                            glsc.InRoomPersonIds.Remove( personId );
                        }
                    }
                    else
                    {
                        if ( !glsc.PersonIds.Contains( personId ) )
                        {
                            glsc.PersonIds.Add( personId );
                        }
                        if ( !glsc.InRoomPersonIds.Contains( personId ) && attendance.DidAttend == true )
                        {
                            glsc.InRoomPersonIds.Add( personId );
                        }
                    }
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
                            a.StartDateTime >= today
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
                    PersonIds = attendance.Select( a => a.PersonAlias?.PersonId ?? 0 ).ToList(),
                    InRoomPersonIds = attendance.Where( a => a.DidAttend == true ).Select( a => a.PersonAlias?.PersonId ?? 0 ).ToList()
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
