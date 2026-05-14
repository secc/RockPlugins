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

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

// Namespace must remain Rock.Workflow.Action so EntityType.Name in the database
// matches and existing workflow references (170+) resolve without modification.
// Originally authored by Mark Lee (Mar 2018), restored from secc/Rock@26f714c979.
namespace Rock.Workflow.Action
{
    /// <summary>
    /// Sets the group of a connection request.
    /// </summary>
    [ActionCategory( "Connections" )]
    [Description( "Sets the group of a connection request." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Connection Request Set Group" )]

    [WorkflowAttribute( "Connection Request Attribute", "The attribute that contains the connection request.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.ConnectionRequestFieldType" } )]
    [WorkflowAttribute( "Group And Role Attribute", "The attribute that contains the group and role to set.", false, "", "", 1, null,
        new string[] { "Rock.Field.Types.GroupAndRoleFieldType" } )]
    [CustomDropdownListField( "Group Status", "The group status to put the person into.", "0^Inactive,1^Active,3^Pending", false, true, "1", "", 2 )]
    public class SetConnectionRequestGroup : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Get the connection request
            ConnectionRequest request = null;
            Guid connectionRequestGuid = action.GetWorklowAttributeValue( GetAttributeValue( action, "ConnectionRequestAttribute" ).AsGuid() ).AsGuid();
            request = new ConnectionRequestService( rockContext ).Get( connectionRequestGuid );
            if ( request == null )
            {
                errorMessages.Add( "Invalid Connection Request Attribute or Value!" );
                return false;
            }

            var groupAndRole = GetAttributeValue( action, "GroupAndRoleAttribute", true );
            string[] parts = ( groupAndRole ?? string.Empty ).Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
            if ( parts.Length > 1 )
            {
                var groupGuid = parts[1].AsGuidOrNull();
                if ( groupGuid != null )
                {
                    GroupService groupService = new GroupService( rockContext );
                    var group = groupService.Get( groupGuid ?? Guid.NewGuid() );
                    if ( group != null )
                    {
                        request.AssignedGroupId = group.Id;
                    }
                }
            }

            if ( parts.Length > 2 )
            {
                var groupTypeRoleGuid = parts[2].AsGuidOrNull();
                if ( groupTypeRoleGuid != null )
                {
                    GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( rockContext );
                    var groupTypeRole = groupTypeRoleService.Get( groupTypeRoleGuid ?? Guid.NewGuid() );
                    if ( groupTypeRole != null )
                    {
                        request.AssignedGroupMemberRoleId = groupTypeRole.Id;
                    }
                }
            }

            rockContext.SaveChanges();

            return true;
        }
    }
}
