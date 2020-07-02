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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using org.secc.FamilyCheckin.Cache;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds family members in a given family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes schedules from locations where the location is full for that schedule." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Remove Full GroupLocationSchedules" )]

    [BooleanField( "Filter Attendance Schedules", "Filter out schedules that the person has already checked into" )]
    public class RemoveFullGroupLocationScheduels : CheckInActionComponent
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
            var filterAttendanceSchedules = GetActionAttributeValue( action, "FilterAttendanceSchedules" ).AsBoolean();

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    foreach ( var person in family.People )
                    {
                        var inScheduleIds = AttendanceCache.All()
                                            .Where( a => a.PersonId == person.Person.Id
                                                      && a.AttendanceState != AttendanceState.CheckedOut )
                                            .Select( a => a.ScheduleId )
                                            .ToList();

                        foreach ( var groupType in person.GroupTypes )
                        {
                            foreach ( var group in groupType.Groups )
                            {
                                foreach ( var location in group.Locations )
                                {
                                    foreach ( var schedule in location.Schedules.ToList() )
                                    {
                                        if ( filterAttendanceSchedules && inScheduleIds.Contains( schedule.Schedule.Id ) )
                                        {
                                            location.Schedules.Remove( schedule );
                                            continue;
                                        }

                                        var occurrence = OccurrenceCache.Get( group.Group.Id, location.Location.Id, schedule.Schedule.Id );
                                        if ( occurrence == null || occurrence.IsFull )
                                        {
                                            location.Schedules.Remove( schedule );
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    errorMessages.Add( "There is not a family that is selected" );
                }

                return false;
            }

            return false;
        }
    }
}