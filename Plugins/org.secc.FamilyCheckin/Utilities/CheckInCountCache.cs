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
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Utilities
{
    public static class CheckInCountCache
    {
        private static Object LOCK = new Object();

        private const string cacheKey = "CheckInCountCache";
        public static List<GroupLocationScheduleCount> GetByLocation( int locationId )
        {
            var glsc = ( List<GroupLocationScheduleCount> ) RockCache.Get( cacheKey );
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
                var lglsc = ( List<GroupLocationScheduleCount> ) RockCache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }
                GroupLocationScheduleCount glsc = lglsc.FirstOrDefault( g =>
                                                                    g.LocationId == ( attendance.Occurrence.LocationId ?? 0 )
                                                                    && g.ScheduleId == ( attendance.Occurrence.ScheduleId ?? 0 )
                                                                    && g.GroupId == ( attendance.Occurrence.GroupId ?? 0 ) );
                if ( glsc == null )
                {
                    glsc = new GroupLocationScheduleCount()
                    {
                        LocationId = attendance.Occurrence.LocationId ?? 0,
                        ScheduleId = attendance.Occurrence.ScheduleId ?? 0,
                        GroupId = attendance.Occurrence.GroupId ?? 0,
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
                RockCache.AddOrUpdate( cacheKey, null, lglsc, RockDateTime.Now.AddMinutes( 10 ), Constants.CACHE_TAG );
            }
        }

        public static void RemoveAttendance( Attendance attendance )
        {
            lock ( LOCK )
            {
                var lglsc = ( List<GroupLocationScheduleCount> ) RockCache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }

                var personId = attendance.PersonAlias.PersonId;

                var items = lglsc.Where( g =>
                                g.LocationId == attendance.Occurrence.LocationId
                                && g.ScheduleId == attendance.Occurrence.ScheduleId
                                && g.GroupId == attendance.Occurrence.GroupId )
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
                    RockCache.AddOrUpdate( cacheKey, null, lglsc, RockDateTime.Now.AddMinutes( 10 ), Constants.CACHE_TAG );
                }
            }
        }

        public static void UpdateAttendance( Attendance attendance )
        {
            lock ( LOCK )
            {
                var lglsc = ( List<GroupLocationScheduleCount> ) RockCache.Get( cacheKey );
                if ( lglsc == null )
                {
                    lglsc = UpdateCache();
                }

                var personId = attendance.PersonAlias.PersonId;

                var items = lglsc.Where( g =>
                                g.LocationId == attendance.Occurrence.LocationId
                                && g.ScheduleId == attendance.Occurrence.ScheduleId
                                && g.GroupId == attendance.Occurrence.GroupId )
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
                RockCache.AddOrUpdate( cacheKey, null, lglsc, RockDateTime.Now.AddMinutes( 10 ) );
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
                .GroupBy( a => new { a.Occurrence.GroupId, a.Occurrence.LocationId, a.Occurrence.ScheduleId } )
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

            RockCache.AddOrUpdate( cacheKey, null, output, RockDateTime.Now.AddMinutes( 10 ), Constants.CACHE_TAG );
            return output;
        }
    }
}
