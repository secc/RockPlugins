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
using System.Data.Entity;
using System.Linq;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    /// <summary>
    /// Job to close workflows
    /// </summary>
    [WorkflowTypeField( "Workflow Types", "The type of workflows to close.", true, true, order: 0 )]
    [TextField( "Close Status", "The status to set the workflow to when closed.", true, "Completed", order: 1 )]
    [IntegerField( "Expiration Age", "The age in minutes that a workflow needs to be in order to close them.", false, order: 2 )]
    [BooleanField( "Expiration Calc Last Used", "If this is set to True the Expiration Age will be caluculated from the last time an entry form was updated.  Otherwise it will use the create date", false, order: 3 )]
    [DisallowConcurrentExecution]
    public class CompleteWorkflows : IJob
    {
        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public CompleteWorkflows()
        {
        }

        /// <summary>
        /// Job that will close workflows.
        /// 
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var workflowTypeGuids = dataMap.GetString( "WorkflowTypes" ).Split( ',' ).Select( Guid.Parse ).ToList();
            int? expirationAge = dataMap.GetString( "ExpirationAge" ).AsIntegerOrNull();
            bool lastUsed = dataMap.GetString( "ExpirationCalcLastUsed" ).AsBoolean();
            string closeStatus = dataMap.GetString( "CloseStatus" );

            var rockContext = new RockContext();
            var workflowService = new WorkflowService( rockContext );
            var workflowActionService = new WorkflowActionService( rockContext );


            var qry = workflowService.Queryable().AsNoTracking()
                        .Where( w => workflowTypeGuids.Contains( w.WorkflowType.Guid )
                                     && w.ActivatedDateTime.HasValue
                                     && !w.CompletedDateTime.HasValue );



            if ( expirationAge.HasValue )
            {
                var expirationDate = RockDateTime.Now.AddMinutes( 0 - expirationAge.Value );

                if ( !lastUsed )
                {
                    qry = qry.Where( w => w.CreatedDateTime <= expirationDate );
                }
                else
                {
                    var formEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Workflow.Action.UserEntryForm ) );
                    qry = qry.Where( w =>
                        w.Activities
                        .Where( a => a.CompletedDateTime == null )
                        .SelectMany( a => a.Actions )
                        .Where( ac => ac.CompletedDateTime == null && ac.CreatedDateTime <= expirationDate && ac.ActionType.EntityTypeId == formEntityTypeId )
                        .Any() );
                }



            }

            // Get a list of workflows to expire so we can open a new context in the loop
            var workflowIds = qry.Select( w => w.Id ).ToList();

            foreach ( var workflowId in workflowIds )
            {
                rockContext = new RockContext();
                workflowService = new WorkflowService( rockContext );

                var workflow = workflowService.Get( workflowId );

                if ( workflow == null )
                {
                    continue;
                }

                workflow.MarkComplete();
                workflow.Status = closeStatus;

                rockContext.SaveChanges();
            }

            context.Result = string.Format( "{0} workflows were closed", workflowIds.Count );
        }

    }
}
