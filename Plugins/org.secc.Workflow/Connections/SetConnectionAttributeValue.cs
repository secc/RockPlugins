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
using Newtonsoft.Json;
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
    /// Sets the group attributes of a connection request.
    /// </summary>
    [ActionCategory( "Connections" )]
    [Description( "Sets the attributes of a connection request." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Connection Request Set Group Attribute Values" )]

    [WorkflowAttribute( "Connection Request Attribute", "The attribute that contains the connection request.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.ConnectionRequestFieldType" } )]
    [KeyValueListField( "Attribute Values", "Values to set for assigned group member attributes. <span class='tip tip-lava'></span>", false, "", "Attribute", "Value", order: 1 )]
    public class SetConnectionAttributeValue : ActionComponent
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

            var attributes = GetAttributeValue( action, "AttributeValues" ).AsDictionary();
            var lavaAttributes = new Dictionary<string, string>();

            foreach ( var attribute in attributes )
            {
                lavaAttributes[attribute.Key] = attribute.Value.ResolveMergeFields( GetMergeFields( action ) );
            }

            var jsonAttributes = JsonConvert.SerializeObject( lavaAttributes );
            request.AssignedGroupMemberAttributeValues = jsonAttributes;

            rockContext.SaveChanges();

            return true;
        }
    }
}
