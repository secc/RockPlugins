// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
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
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    var attendanceService = new AttendanceService( rockContext ).Queryable();
                    foreach ( var person in family.People )
                    {
                        foreach ( var groupType in person.GroupTypes )
                        {
                            foreach ( var group in groupType.Groups )
                            {
                                foreach ( var location in group.Locations )
                                {
                                    foreach ( var schedule in location.Schedules.ToList() )
                                    {
                                        if ( (person.Person.Age ?? 0) > 12 )
                                        {
                                            var threshold = Math.Max( location.Location.FirmRoomThreshold ?? 0, location.Location.SoftRoomThreshold ?? 0);
                                            if ( attendanceService.Where( a =>
                                                 a.DidAttend == true
                                                 && a.EndDateTime == null
                                                 && a.ScheduleId == schedule.Schedule.Id
                                                 && a.LocationId == location.Location.Id
                                                 && a.CreatedDateTime >= Rock.RockDateTime.Today
                                                ).Count() >= threshold )
                                            {
                                                location.Schedules.Remove( schedule );
                                            }
                                        }
                                        else
                                        {
                                            var threshold = location.Location.SoftRoomThreshold ?? 0;
                                            if ( attendanceService.Where( a =>
                                                 a.DidAttend == true
                                                 && a.EndDateTime == null
                                                 && a.ScheduleId == schedule.Schedule.Id
                                                 && a.LocationId == location.Location.Id
                                                 && a.CreatedDateTime >= Rock.RockDateTime.Today
                                                ).Count() >= threshold )
                                            {
                                                location.Schedules.Remove( schedule );
                                            }
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