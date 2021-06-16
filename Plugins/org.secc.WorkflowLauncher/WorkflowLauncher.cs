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
using System.Data;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Collections.Generic;
using System;
using System.Web;

namespace org.secc.Jobs
{
    [CodeEditorField( "SQL Query", "The SQL to execute.  Each row will launch a workflow and columns will be automatically mapped to workflow attributes.", Rock.Web.UI.Controls.CodeEditorMode.Sql, required: false, category:"Source", order:1 )]
    [DataViewField( "Data View", "A Dataview to use for launching workflows with the selected entity.", false, category: "Source", order: 2)]
    [IntegerField( "Command Timeout", "Maximum amount of time (in seconds) to wait for the SQL Query or DataView to complete.", false, 60 * 60, "Source", 2, "CommandTimeout" )]
    [WorkflowTypeField( "Workflow", "The workflow to launch for each row.", false, true, category:"Target" )]

    [DisallowConcurrentExecution]
    public class WorkflowLauncher : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            string query = dataMap.GetString( "SQLQuery" );

            Guid? dataViewGuid = dataMap.GetString( "DataView" ).AsGuidOrNull();
            Guid? workflowTypeGuid = dataMap.GetString( "Workflow" ).AsGuidOrNull();

            // run a SQL query to do something
            int? commandTimeout = dataMap.GetString( "CommandTimeout" ).AsIntegerOrNull();
            int failed = 0;
            int successful = 0;

            List<string> errors = new List<string>();
            try
            {
                if ( workflowTypeGuid.HasValue )
                {
                    var workflowType = Rock.Web.Cache.WorkflowTypeCache.Get( workflowTypeGuid.Value );
                    if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            // Handle the SQL Query
                            if ( !query.IsNullOrWhiteSpace() )
                            {
                                DataSet rows = DbService.GetDataSet( query, System.Data.CommandType.Text, null, commandTimeout );

                                // Fire off a workflow for each row in the query
                                foreach ( DataRow row in rows.Tables[0].Rows )
                                {
                                    var workflow = Workflow.Activate( workflowType, workflowType.Name );
                                    workflow.LoadAttributes( rockContext );

                                    // Iterate over each column setting attribute values in the workflow
                                    foreach ( DataColumn column in rows.Tables[0].Columns )
                                    {
                                        if ( workflow.Attributes.ContainsKey( column.ColumnName ) )
                                        {
                                            workflow.SetAttributeValue( column.ColumnName, Convert.ToString( row[column] ) );
                                        }
                                    }

                                    // Now process the workflow
                                    var processed = new WorkflowService( rockContext ).Process( workflow, out errors );
                                    if ( processed )
                                    {
                                        successful++;
                                    }
                                    else
                                    {
                                        failed++;
                                    }

                                }

                            }

                            // If a dataview is specified, then we'll fire off workflows for that dataview
                            if ( dataViewGuid != null )
                            {
                                var dataView = new DataViewService( rockContext ).Get( dataViewGuid.Value );
                                
                                var entityList = dataView.GetQuery( null, commandTimeout, out errors );
                                foreach(IEntity entity in entityList)
                                {
                                    var workflow = Workflow.Activate( workflowType, workflowType.Name );

                                    var processed = new WorkflowService( rockContext ).Process( workflow, entity, out errors );
                                    if ( processed )
                                    {
                                        successful++;
                                    }
                                    else
                                    {
                                        failed++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch ( System.Exception ex )
            {
                HttpContext context2 = HttpContext.Current;
                ExceptionLogService.LogException( ex, context2 );
                throw;
            }

            context.Result = string.Format( "Workflow Launch Results:<br />Success:  {0}<br />Failed: {1}{2}", successful, failed, errors.Count>0? "<br />Errors: " + string.Join("<br />", errors ):"" );
        }
    }
}
