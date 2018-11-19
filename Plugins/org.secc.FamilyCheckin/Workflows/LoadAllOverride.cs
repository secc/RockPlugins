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
// <copyright>
// Copyright by the Spark Development Network
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
using System.Linq;
using Rock;
using Rock.Workflow;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Workflow.Action.CheckIn;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Loads the group types allowed for each person in a family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Loads all kiosk data for all people in selected family" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Load All Override" )]
    public class LoadGroupTypesOverride : CheckInActionComponent
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
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var groupService = new GroupService( rockContext );

                foreach ( var family in checkInState.CheckIn.Families.Where( f => f.Selected ).ToList() )
                {
                    foreach ( var person in family.People )
                    {
                        var checkinGroupTypes = new List<CheckInGroupType>();

                        foreach ( var id in checkInState.ConfiguredGroupTypes )
                        {
                            var cgt = new CheckInGroupType();
                            cgt.GroupType = GroupTypeCache.Get( id );
                            checkinGroupTypes.Add( cgt );
                            var groups = groupService.Queryable().Where( g => g.GroupTypeId == id );
                            List<CheckInGroup> checkinGroups = new List<CheckInGroup>();
                            foreach ( var group in groups.Where(g => g.IsActive) )
                            {
                                var cg = new CheckInGroup();
                                cg.Group = group;
                                group.LoadAttributes();
                                checkinGroups.Add( cg );
                                var groupLocations = group.GroupLocations;
                                List<CheckInLocation> checkinLocations = new List<CheckInLocation>();
                                foreach(var groupLocation in groupLocations )
                                {
                                    var cl = new CheckInLocation();
                                    cl.Location = groupLocation.Location;
                                    cl.CampusId = cl.Location.CampusId;
                                    checkinLocations.Add( cl );
                                    var schedules = new List<CheckInSchedule>();
                                    foreach(var schedule in groupLocation.Schedules )
                                    {
                                        var cs = new CheckInSchedule();
                                        cs.Schedule = schedule;
                                        schedules.Add( cs );
                                    }
                                    cl.Schedules = schedules;
                                }
                                cg.Locations = checkinLocations;
                            }
                            cgt.Groups = checkinGroups;
                        }
                        person.GroupTypes = checkinGroupTypes;
                    }
                }
                return true;
            }
            return false;
        }
    }
}