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
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowControl
{
    /// <summary>
    /// Activates a new activity for a given activity type
    /// </summary>
    [ActionCategory( "SECC > Workflow Control" )]
    [Description( "ReActivates an activity instance and all of its actions." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "ReActivate Activity" )]

    [WorkflowTextOrAttribute( "Activity Id", "Activity Id", "The activity to Re-Activate.  <span class='tip tip-lava'></span>", true, "", "", 0, "ActivityId" )]
    public class ReActivateActivity : ActionComponent
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


            var ActivityToActivate = GetAttributeValue( action, "ActivityId", true ).ResolveMergeFields( GetMergeFields( action ) ).AsIntegerOrNull();
            if ( !ActivityToActivate.HasValue )
            {
                action.AddLogEntry( "Invalid Activity Property", true );
                return false;
            }

            WorkflowActivity activity = action.Activity.Workflow.Activities.Where( a => a.Id == ActivityToActivate ).FirstOrDefault();

            activity.CompletedDateTime = null;
            action.AddLogEntry( "Marked activity incomplete" );

            foreach ( var a in activity.Actions )
            {
                a.CompletedDateTime = null;
            }

            action.AddLogEntry( string.Format( "Activated all actions for '{0}' activity.", action.ActionTypeCache.Name ) );





            return true;
        }

    }
}