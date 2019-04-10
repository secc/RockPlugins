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
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Removes or excludes checkin groups that require the member to be in a particular group
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "For each person in check-in who does not match the group, they get moved to a can check-in relationship rather than removed." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Sports And Fitness Group Filter" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Group Membership Group Attribute", "Select the attribute used to filter membership group.", true, false, "6f1ff463-e857-4755-b0b7-461e8c183789", order: 2 )]
    public class SportsFitnessGroupFilter : CheckInActionComponent
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
            if ( checkInState == null )
            {
                return false;
            }

            var groupIdAttributeKey = string.Empty;
            var groupIdAttributeGuid = GetAttributeValue( action, "GroupMembershipGroupAttribute" ).AsGuid();
            if ( groupIdAttributeGuid != Guid.Empty )
            {
                groupIdAttributeKey = AttributeCache.Get( groupIdAttributeGuid ).Key;
            }

            var family = checkInState.CheckIn.CurrentFamily;
            if ( family != null )
            {
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();

                foreach ( var person in family.People )
                {
                    if ( person.Person.Attributes == null )
                    {
                        person.Person.LoadAttributes( rockContext );
                    }
                    foreach ( var groupType in person.GroupTypes.ToList() )
                    {
                        foreach ( var group in groupType.Groups.ToList() )
                        {
                            var groupGuid = group.Group.GetAttributeValue( groupIdAttributeKey ).AsGuidOrNull();
                            if ( groupGuid != null )
                            {
                                if ( !groupMemberService.GetByGroupGuid( groupGuid ?? new Guid() )
                                    .Where( gm => gm.PersonId == person.Person.Id && gm.GroupMemberStatus == GroupMemberStatus.Active ).Any() )
                                {
                                    //If this is the last group set this person as not a family member
                                    //Not family members can check-in, but don't appear on the screen right away
                                    //This logic allows for volunteers who can serve but don't have S&F cards 
                                    if ( person.GroupTypes.SelectMany( gt => gt.Groups ).Count() == 1 )
                                    {
                                        person.ExcludedByFilter = true;
                                        //If this person has already checked in
                                        //Make them a family member so they can be shown in checkin
                                        if ( IsCheckedIn( person.Person, rockContext ) )
                                        {
                                            person.FamilyMember = true;
                                        }
                                    }
                                    else //otherwise remove it
                                    {
                                        groupType.Groups.Remove( group );
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsCheckedIn( Person person, RockContext rockContext )
        {
            AttendanceService attendanceService = new AttendanceService( rockContext );
            return attendanceService
                .Queryable()
                .Where( a => a.PersonAlias.PersonId == person.Id )
                .Where( a => a.StartDateTime >= Rock.RockDateTime.Today )
                .OrderByDescending( a => a.StartDateTime )
                .Any();
        }
    }
}