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
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Loads the groups available for each location.
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Loads the groups available for each selected (or optionally all) location(s). Note: This is almost identical to the default load groups workflow action except that it loads groups even when they are not active." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Load All Groups" )]
    [BooleanField( "Load All", "By default groups are only loaded for the selected person, group type, and location.  Select this option to load groups for all the loaded people and group types." )]
    public class LoadAllGroups : CheckInActionComponent
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
        public override bool Execute( RockContext rockContext, Rock.Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                bool loadAll = GetAttributeValue( action, "LoadAll" ).AsBoolean();

                foreach ( var family in checkInState.CheckIn.GetFamilies( true ) )
                {
                    foreach ( var person in family.GetPeople( !loadAll ) )
                    {
                        foreach ( var groupType in person.GetGroupTypes( !loadAll ) )
                        {
                            var kioskGroupType = checkInState.Kiosk.ActiveGroupTypes( checkInState.ConfiguredGroupTypes )
                                .Where( g => g.GroupType.Id == groupType.GroupType.Id )
                                .FirstOrDefault();

                            if ( kioskGroupType != null )
                            {
                                foreach ( var kioskGroup in kioskGroupType.KioskGroups )
                                {
                                    bool validGroup = true;
                                    if ( groupType.GroupType.AttendanceRule == AttendanceRule.AlreadyBelongs )
                                    {
                                        validGroup = new GroupMemberService( rockContext ).Queryable()
                                            .Any( m =>
                                                m.GroupId == kioskGroup.Group.Id &&
                                                m.GroupMemberStatus == GroupMemberStatus.Active &&
                                                m.PersonId == person.Person.Id );
                                    }

                                    if ( validGroup && !groupType.Groups.Any( g => g.Group.Id == kioskGroup.Group.Id ) )
                                    {
                                        var checkInGroup = new CheckInGroup();
                                        checkInGroup.Group = kioskGroup.Group.Clone( false );
                                        checkInGroup.Group.CopyAttributesFrom( kioskGroup.Group );
                                        groupType.Groups.Add( checkInGroup );
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
    }
}