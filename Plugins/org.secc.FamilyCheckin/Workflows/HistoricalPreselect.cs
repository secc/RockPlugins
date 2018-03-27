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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Workflow;
using Rock.Data;
using Rock.Model;
using Rock.Workflow.Action.CheckIn;
using Rock.Attribute;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds family members in a given family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Preselects groups and rooms based upon historical data." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Historical Preselect" )]
    [IntegerField( "Week History", "Number of weeks history to look back.", true, 4 )]
    public class HistoricalPreselect : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var sundayList = GetSundayList( action );
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                AttendanceService attendanceService = new AttendanceService( rockContext );
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    foreach ( var person in family.People )
                    {
                        var acceptableGroupIds = person.GroupTypes.SelectMany( _gt => _gt.Groups ).Where( _g => !_g.ExcludedByFilter ).Select( _g => _g.Group.Id ).Distinct().ToList();
                        var acceptableLocations = person.GroupTypes.SelectMany( _gt => _gt.Groups ).SelectMany( _g => _g.Locations ).Where( _l => !_l.ExcludedByFilter ).Select( _s => _s.Location.Id ).Distinct().ToList();
                        var acceptableScheduleIds = person.GroupTypes.SelectMany( _gt => _gt.Groups ).SelectMany( _g => _g.Locations ).SelectMany( _l => _l.Schedules ).Where( _s => !_s.ExcludedByFilter && _s.Schedule.IsCheckInActive ).Select( _s => _s.Schedule.Id ).Distinct().ToList();

                        var personAlias = personAliasService.Queryable()
                            .Where( pa => pa.PersonId == person.Person.Id )
                            .Select( pa => pa.Id ).ToList();

                        var attendance = attendanceService.Queryable().AsNoTracking()
                            .Where( a =>
                                personAlias.Contains( a.PersonAliasId ?? 0 )
                                && a.DidAttend == true
                                && sundayList.Contains( a.SundayDate )
                                && a.DeviceId != null
                                && a.Location.IsActive
                                && acceptableGroupIds.Contains( a.GroupId ?? 0 )
                                && acceptableLocations.Contains( a.LocationId ?? 0 )
                                && acceptableScheduleIds.Contains( a.ScheduleId ?? 0 )
                            )
                        .GroupBy( a => new
                        {
                            ScheduleId = a.ScheduleId,
                            GroupId = a.GroupId,
                            LocationId = a.LocationId,
                            GroupTypeId = a.Group.GroupTypeId
                        } )
                        .OrderByDescending( an => an.Count() )
                        .DistinctBy( an => an.Key.ScheduleId )
                        .ToList();

                        foreach ( var item in attendance )
                        {
                            var gt = person.GroupTypes.Where( _gt => _gt.GroupType.Id == item.Key.GroupTypeId ).FirstOrDefault();
                            if ( gt != null )
                            {
                                var g = gt.Groups.Where( _g => _g.Group.Id == item.Key.GroupId ).FirstOrDefault();
                                if ( g != null )
                                {
                                    var l = g.Locations.Where( _l => _l.Location.Id == item.Key.LocationId ).FirstOrDefault();
                                    if ( l != null )
                                    {
                                        var s = l.Schedules.Where( _s => _s.Schedule.Id == item.Key.ScheduleId ).FirstOrDefault();
                                        if ( s != null )
                                        {
                                            gt.Selected = true;
                                            g.Selected = true;
                                            l.Selected = true;
                                            s.Selected = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private List<DateTime> GetSundayList( WorkflowAction action )
        {
            List<DateTime> sundayList = new List<DateTime>();
            var depth = GetActionAttributeValue( action, "WeekHistory" ).AsInteger();

            //Get last sunday
            DateTime lastSunday = DateTime.Now.Date.AddDays( -1 );
            while ( lastSunday.DayOfWeek != DayOfWeek.Sunday )
            {
                lastSunday = lastSunday.AddDays( -1 );
            }

            //add history of sundays to list
            for ( int i = 0; i < depth; i++ )
            {
                sundayList.Add( lastSunday );
                lastSunday = lastSunday.AddDays( -7 );
            }
            return sundayList;
        }
    }
}