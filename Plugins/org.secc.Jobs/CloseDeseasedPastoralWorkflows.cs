using System.Linq;
using System.Data;
using System.Data.Entity;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Collections.Generic;

namespace org.secc.Jobs
{
    [WorkflowTypeField( "Hospital Admission Workflow", "", false, true, "", "Workflows" )]
    [WorkflowTypeField( "Nursing Home Resident Workflow", "", false, true, "", "Workflows" )]
    [WorkflowTypeField( "Homebound Person Workflow", "", false, true, "", "Workflows" )]

    [DisallowConcurrentExecution]
    public class CloseDeseasedPastoralWorkflows : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var hospitalWorkflow = dataMap.GetString( "HospitalAdmissionWorkflow" ).AsGuidOrNull();
            var nursingHomeAdmissionWorkflow = dataMap.GetString( "NursingHomeResidentWorkflow" ).AsGuidOrNull();
            var homeBoundPersonWorkflow = dataMap.GetString( "HomeboundPersonWorkflow" ).AsGuidOrNull();

            var workflowService = new WorkflowService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );
            var personAliasService = new PersonAliasService( rockContext );
            var personService = new PersonService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );

            var workflowTypesIdAsStrings = new WorkflowTypeService( rockContext ).Queryable()
                    .Where( wt =>
                         wt.Guid == hospitalWorkflow
                         || wt.Guid == nursingHomeAdmissionWorkflow
                         || wt.Guid == homeBoundPersonWorkflow
                        )
                    .ToList()
                    .Select( wf => wf.Id.ToString() )
                    .ToList();

            var attributeIds = attributeService.Queryable()
                    .Where( a => a.EntityTypeQualifierColumn == "WorkflowTypeId" && workflowTypesIdAsStrings.Contains( a.EntityTypeQualifierValue ) )
                    .Select( a => a.Id ).ToList();

            var wfTmpqry = workflowService.Queryable().AsNoTracking()
                 .Where( w => (
                    w.WorkflowType.Guid == hospitalWorkflow
                    || w.WorkflowType.Guid == nursingHomeAdmissionWorkflow
                    || w.WorkflowType.Guid == homeBoundPersonWorkflow
                 ) && ( w.Status == "Active" ) );

            var tqry = wfTmpqry.Join( attributeValueService.Queryable(),
                obj => obj.Id,
                av => av.EntityId.Value,
                ( obj, av ) => new { Workflow = obj, AttributeValue = av } )
                .Where( a => attributeIds.Contains( a.AttributeValue.AttributeId ) )
                .GroupBy( obj => obj.Workflow )
                .Select( obj => new { Workflow = obj.Key, AttributeValues = obj.Select( a => a.AttributeValue ) } );
            var qry = tqry.ToList();

            var newQry = qry.Select( w => new
            {
                Workflow = w.Workflow,
                Person = GetPerson( personAliasService, w.AttributeValues )
            } );

            var workflowsToClose = newQry
                .Where( cd => cd.Person.IsDeceased && cd.Workflow.CompletedDateTime == null )
                .Select(cd => cd.Workflow)
                .ToList();
            
            foreach (var workflow in workflowsToClose)
            {
                workflow.MarkComplete();
            }

            rockContext.SaveChanges();

            context.Result = string.Format( "Closed {0} workflows.", workflowsToClose.Count() );
        }

        private Person GetPerson( PersonAliasService personAliasService, IEnumerable<AttributeValue> AttributeValues )
        {
            AttributeValue personAliasAV = AttributeValues.AsQueryable().Where( av => av.AttributeKey == "PersonToVisit" || av.AttributeKey == "HomeboundPerson" ).FirstOrDefault();
            if ( personAliasAV != null )
            {
                PersonAlias pa = personAliasService.Get( personAliasAV.Value.AsGuid() );
                if ( pa != null )
                {
                    return pa.Person;
                }
            }
            return new Person();
        }
    }
}
