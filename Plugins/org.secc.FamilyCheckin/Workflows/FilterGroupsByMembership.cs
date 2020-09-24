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
using DotLiquid.Util;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Removes or excludes checkin groups that require the member to be in a particular group
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes or excludes checkin groups that require the member to be in a particular group" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Filter Groups By Membership" )]
    [CustomDropdownListField( "Check Requirements", "How should group member reqirements be checked?", "0^Don\'t Check,1^Check Required Only,2^ Check Required and Warning", true, "0", order: 4 )]
    [BooleanField( "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", true, order: 5 )]
    public class FilterGroupsByMembership : CheckInActionComponent
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
                errorMessages.Add( $"Attempted to run {this.GetType().GetFriendlyTypeName()} in check-in, but the check-in state was null." );
                return false;
            }

            var groupAttributeKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_MEMBERSHIP_GROUP ).Key;
            var checkRequirementsKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_CHECK_REQUIREMENTS ).Key;
            var memberRoleAttributeKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_MEMBER_ROLE ).Key;

            bool allowInactive = false;

            if ( checkInState.CheckInType != null )
            {
                allowInactive = !checkInState.CheckInType.PreventInactivePeople;
            }

            var family = checkInState.CheckIn.CurrentFamily;
            if ( family != null )
            {
                GroupService groupService = new GroupService( rockContext );
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( rockContext );


                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();

                foreach ( var person in family.People )
                {
                    if ( person.Person.Attributes == null )
                    {
                        person.Person.LoadAttributes( rockContext );
                    }
                    foreach ( var groupType in person.GroupTypes.ToList() )
                    {
                        var groupsToCheck = groupType.Groups
                            .Where( g => g.Group.AttributeValues.ContainsKey( groupAttributeKey ) )
                            .Select( g => g.Group.AttributeValues[groupAttributeKey].Value )
                            .Where( s => s.IsNotNullOrWhiteSpace() )
                            .Select( s => s.AsGuid() );

                        if ( !groupsToCheck.Any() )
                        {
                            //There are no groups to search here. Don't remove any and move on
                            continue;
                        }

                        var groupMemberQry = groupMemberService.Queryable().AsNoTracking()
                            .Where( gm => gm.PersonId == person.Person.Id );
                        if ( !allowInactive )
                        {
                            groupMemberQry = groupMemberQry.Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active );
                        }

                        //Check all the groups at once. This turns many little requests into one medium request.
                        var qry = groupService.Queryable().AsNoTracking()
                            .Where( g => groupsToCheck.Contains( g.Guid ) )
                            .Join( groupMemberQry,
                            g => g.Id,
                            gm => gm.GroupId,
                            ( g, gm ) => new
                            {
                                g.Guid,
                                gm.GroupRole.IsLeader
                            } );

                        var validGroups = qry.ToList();

                        foreach ( var group in groupType.Groups.ToList() )
                        {
                            var groupGuid = group.Group.GetAttributeValue( groupAttributeKey ).AsGuid();
                            if ( groupGuid == Guid.Empty )
                            {
                                //There is no group to check... do not remove check-in group
                                continue;
                            }
                            var cannotCheckin = false;

                            var role = group.Group.GetAttributeValue( memberRoleAttributeKey );

                            switch ( role )
                            {

                                case "0": // Any
                                    if ( !validGroups.Where( g => g.Guid == groupGuid ).Any() )
                                    {
                                        cannotCheckin = true;
                                    }
                                    break;
                                case "1": //Leaders Only
                                    if ( !validGroups.Where( g => g.Guid == groupGuid && g.IsLeader ).Any() )
                                    {
                                        cannotCheckin = true;
                                    }
                                    break;
                                case "2": //Non Leaders
                                    if ( !validGroups.Where( g => g.Guid == groupGuid && !g.IsLeader ).Any() )
                                    {
                                        cannotCheckin = true;
                                    }
                                    break;
                            }

                            if ( !cannotCheckin )
                            {
                                //Check the group requirements to see if this person passes
                                var requirement = group.Group.GetAttributeValue( checkRequirementsKey );

                                switch ( requirement )
                                {
                                    case "0": //No requirement
                                        break;
                                    case "1": //Required Only
                                        cannotCheckin = !groupMemberService.GetByGroupGuid( groupGuid )
                                            .Where( gm => gm.PersonId == person.Person.Id && ( gm.GroupMemberStatus == GroupMemberStatus.Active || allowInactive ) )
                                            .Where( gm => !gm.GroupMemberRequirements.Where( r => r.RequirementFailDateTime != null )
                                            .Any() )
                                            .Any();
                                        break;
                                    case "2": //Required And Warning
                                        cannotCheckin = !groupMemberService.GetByGroupGuid( groupGuid )
                                            .Where( gm => gm.PersonId == person.Person.Id && ( gm.GroupMemberStatus == GroupMemberStatus.Active || allowInactive ) )
                                            .Where( gm => !gm.GroupMemberRequirements.Where( r => r.RequirementFailDateTime != null || r.RequirementWarningDateTime != null )
                                            .Any() )
                                            .Any();
                                        break;
                                }
                            }

                            if ( cannotCheckin )
                            {
                                if ( remove )
                                {
                                    groupType.Groups.Remove( group );
                                }
                                else
                                {
                                    group.ExcludedByFilter = true;
                                }
                            }
                        }

                    }
                }
            }

            return true;
        }
    }
}