using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Rock;
using Rock.Model;

namespace MigratePastoralWorkflowData
{
    class HomeboundWorkflowImport : WorkflowImport
    {

        const int DISCHARGE_ACTIVITY_ID = 85;
        const int VISIT_ACTIVITY_ID = 84;
        const int SUMMARY_ACTIVITY_ID = 83;
        const int WORKFLOW_TYPE_ID = 38;
        const int ARENA_ASSIGNMENT_TYPE_ID = 16;

        public HomeboundWorkflowImport() : base()
        {
            attributeMap = new Dictionary<int, string>() {
                { 5284, "HomeboundPerson" },
                { 5285, "StartDate"},
                { 5287, "NotifiedBy"},
                { 5288, "NotifiedOn"},
                { 5289, "Communion"},
                { 5290, "Visitor"},
                { 5291, "VisitDate"},
                { 5292, "VisitNote"},
                { 5286, "EndDate"}
            };
        }

        public void Clean()
        {
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            var workflows = workflowService.Queryable().Where( w => w.WorkflowTypeId == WORKFLOW_TYPE_ID && w.ForeignId > 0 );
            var j = 0;
            foreach ( var workflow in workflows )
            {
                foreach(WorkflowActivity activity in workflow.Activities)
                {
                    var activytAttributes = attributeValueService.Queryable( "Attribute" ).Where( av => av.EntityId == activity.Id && av.Attribute.EntityTypeId == 116 && av.Attribute.EntityTypeQualifierValue == activity.ActivityTypeId.ToString() );
                    attributeValueService.DeleteRange( activytAttributes );
                }
                var attributes = attributeValueService.Queryable( "Attribute" ).Where( av => av.EntityId == workflow.Id && av.Attribute.EntityTypeId == 113 && av.Attribute.EntityTypeQualifierValue == WORKFLOW_TYPE_ID.ToString() );
                attributeValueService.DeleteRange( attributes );
                workflowService.Delete( workflow );
                j++;
            }
            rockContext.SaveChanges();
            Console.WriteLine( "Removed " + j + " Homebound Resident Workflows." );
        }

        public void Run()
        {

            // Open a "regular" db connection for non EF stuff
            var conn = new SqlConnection( arenaContext.Connection.ConnectionString );
            conn.Open();

            var Assignments = arenaContext.asgn_assignments.AsQueryable().Where( a => a.assignment_type_id == ARENA_ASSIGNMENT_TYPE_ID );
            var i = 0;
            foreach ( asgn_assignment assignment in Assignments )
            {
                i++;
                // Give us some feedback that we are still doing stuff.
                Console.WriteLine( "Loading " + i + " of " + Assignments.Count() );

                PersonAlias requestorPersonAlias = personAliasService.Queryable().Where( pa => pa.AliasPersonId == assignment.requester_person_id ).First();


                // See if we already imported this one
                if (workflowService.Queryable().Where(w => w.ForeignId == assignment.assignment_id).Any()) {

                    Console.WriteLine( "Skipping assignment ID " + assignment.assignment_id + " since it was already imported." );
                    continue;
                }

                // Do some preliminary checking
                var fieldValues = arenaContext.asgn_assignment_field_values.Where( v => v.assignment_id == assignment.assignment_id );
                if ( fieldValues == null )
                {
                    Console.WriteLine( "Skipping assignment ID " + assignment.assignment_id + " since it has no custom field values." );
                    continue;
                }


                Workflow workflow = new Workflow();
                workflow.WorkflowTypeId = WORKFLOW_TYPE_ID;

                workflow.CreatedByPersonAliasId = workflow.InitiatorPersonAliasId = assignment.requester_person_id = requestorPersonAlias.Id;
                workflow.Name = assignment.title;
                workflow.Status = assignment.resolved_date > new DateTime( 1900, 1, 1 ) ? "Completed" : "Active";
                workflow.CreatedDateTime = workflow.ActivatedDateTime = assignment.date_created;
                workflow.LastProcessedDateTime = assignment.state_last_updated;
                if ( assignment.resolved_date > new DateTime( 1900, 1, 1 ) )
                {
                    workflow.CompletedDateTime = assignment.resolved_date;
                } 
                workflow.ForeignId = assignment.assignment_id;

                // Add it to EF
                workflowService.Add( workflow );

                workflow.LoadAttributes( rockContext );
                if ( assignment.resolved_date > new DateTime( 1900, 1, 1 ) )
                {
                    workflow.SetAttributeValue( "DischargeDate", assignment.resolved_date.ToString() );
                    // Create a completed discharged activity
                    WorkflowActivity workflowActivity = new WorkflowActivity();
                    workflowActivity.ActivityTypeId = DISCHARGE_ACTIVITY_ID;
                    workflowActivity.CompletedDateTime = assignment.resolved_date;
                    workflowActivity.ActivatedDateTime = assignment.resolved_date;
                    workflowActivity.LastProcessedDateTime = assignment.resolved_date;
                    workflowActivity.Workflow = workflow;
                    workflowActivityService.Add( workflowActivity );
                } else
                {
                    WorkflowActivity workflowActivity = WorkflowActivity.Activate( workflowActivityTypeService.Get( SUMMARY_ACTIVITY_ID ), workflow, rockContext );
                    
                    workflowActivity.ActivatedDateTime = assignment.date_created;
                    workflowActivity.LastProcessedDateTime = assignment.date_created;
                }
                // Set some attributes from data in the assignment
                workflow.SetAttributeValue( "CloseNote", assignment.resolution_text );
                workflow.SetAttributeValue( "HomeboundResidentDescription", assignment.description );
                workflow.SetAttributeValue( "Requestor", requestorPersonAlias.Guid );

                // Set more attributes from custom fields
                SetPersonAliasAttribute( workflow, fieldValues, "HomeboundPerson" );
                SetDateAttribute( workflow, fieldValues, "StartDate" );
                SetAttribute( workflow, fieldValues, "NotifiedBy" );
                SetAttribute( workflow, fieldValues, "NotifiedOn" );
                SetYesNoAttribute( workflow, fieldValues, "Communion" );
                SetDateAttribute( workflow, fieldValues, "EndDate" );

                // Now load all the visits (Let's kick it old school with some sp fun!)
                SqlCommand visitCmd = new SqlCommand( "cust_secc_sp_rept_pastoralVisit_migration", conn );
                visitCmd.Parameters.Add(new SqlParameter("@assignmentID", assignment.assignment_id));
                visitCmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader visitReader = visitCmd.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load( visitReader );
                visitReader.Close();
                if (dataTable.Rows.Count > 0)
                { 
                    // Reverse the dataTable and iterate
                    foreach (DataRow dr in dataTable.AsEnumerable().Reverse().CopyToDataTable().Rows)
                    { 
                        var visitorPersonId = dr["Visitor"].ToString().AsInteger();
                        var visitDate = dr["VisitDate"].ToString().AsDateTime();
                        var visitNote = dr["VisitNote"].ToString();

                        WorkflowActivity workflowActivity = new WorkflowActivity();
                        workflowActivity.ActivityTypeId = VISIT_ACTIVITY_ID;
                        workflowActivity.CompletedDateTime = visitDate;
                        workflowActivity.ActivatedDateTime = visitDate;
                        workflowActivity.LastProcessedDateTime = visitDate;
                    
                        workflowActivity.Workflow = workflow;
                        workflowActivityService.Add( workflowActivity );

                        // Set the attributes
                        workflowActivity.LoadAttributes();
                        workflowActivity.SetAttributeValue( "Visitor", personAliasService.Queryable().Where( p => p.AliasPersonId == visitorPersonId ).FirstOrDefault().Guid );
                        workflowActivity.SetAttributeValue( "VisitDate", visitDate );
                        workflowActivity.SetAttributeValue( "VisitNote", visitNote );
                    }
                }

                rockContext.SaveChanges();
                workflow.SaveAttributeValues();
                // Save each activities attributes too
                foreach(WorkflowActivity activity in workflow.Activities)
                {
                    activity.SaveAttributeValues();
                }
            }
            Console.WriteLine( "Loaded " + i + " Homebound Resident Workflows." );

        }
    }
}
