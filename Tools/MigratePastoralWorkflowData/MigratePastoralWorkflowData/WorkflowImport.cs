using System.Collections.Generic;
using System.Linq;
using Rock.Data;
using Rock.Model;
using Rock;

namespace MigratePastoralWorkflowData
{

    abstract class WorkflowImport
    {
        protected Dictionary<int, string> attributeMap = new Dictionary<int, string>();
        protected RockContext rockContext = null;
        protected ArenaDataContext arenaContext = null;
        protected WorkflowService workflowService = null;
        protected WorkflowActivityService workflowActivityService = null;
        protected WorkflowActivityTypeService workflowActivityTypeService = null;
        protected WorkflowActionService workflowActionService = null;
        protected DefinedValueService definedValueService = null;
        protected PersonAliasService personAliasService = null;

        protected WorkflowImport()
        {
            rockContext = new RockContext();
            arenaContext = new ArenaDataContext();
            workflowService = new WorkflowService( rockContext );
            workflowActivityService = new WorkflowActivityService( rockContext );
            workflowActivityTypeService = new WorkflowActivityTypeService( rockContext );
            workflowActionService = new WorkflowActionService( rockContext );
            definedValueService = new DefinedValueService( rockContext );
            personAliasService = new PersonAliasService( rockContext );
        }


        protected void SetDefinedValueAttribute( Workflow workflow, IQueryable<asgn_assignment_field_value> fieldValues, string key )
        {
            var FID = fieldValues.Where( v => v.custom_field_id == attributeMap.Where( am => am.Value == key ).First().Key ).Select( v => v.selected_value ).FirstOrDefault().AsIntegerOrNull();
            if ( FID.HasValue )
            {
                workflow.SetAttributeValue( key, definedValueService.Queryable().Where( dv => dv.ForeignId == FID ).FirstOrDefault().Guid );
            }
        }

        protected void SetPersonAliasAttribute(Workflow workflow, IQueryable<asgn_assignment_field_value> fieldValues, string key )
        {
            var aliasId = fieldValues.Where( v => v.custom_field_id == attributeMap.Where( am => am.Value == key ).First().Key ).Select( v => v.selected_value ).FirstOrDefault().AsIntegerOrNull();
            if ( aliasId.HasValue )
            {
                workflow.SetAttributeValue( key, personAliasService.Queryable().Where( p => p.AliasPersonId == aliasId ).FirstOrDefault().Guid );
            }
        }

        protected void SetDateAttribute( Workflow workflow, IQueryable<asgn_assignment_field_value> fieldValues, string key )
        {
            System.DateTime? date = fieldValues.Where( v => v.custom_field_id == attributeMap.Where( am => am.Value == key ).First().Key ).Select( v => v.selected_value ).FirstOrDefault().AsDateTime();
            if (date.HasValue)
            {
                workflow.SetAttributeValue( key, date.ToString() );
            }
        }
        protected void SetYesNoAttribute( Workflow workflow, IQueryable<asgn_assignment_field_value> fieldValues, string key )
        {
            workflow.SetAttributeValue( key, fieldValues.Where( v => v.custom_field_id == attributeMap.Where( am => am.Value == key ).First().Key ).Select( v => v.selected_value ).FirstOrDefault().AsBoolean()?"Yes":"No" );
        }

        protected void SetAttribute( Workflow workflow, IQueryable<asgn_assignment_field_value> fieldValues, string key )
        {
            workflow.SetAttributeValue( key, fieldValues.Where( v => v.custom_field_id == attributeMap.Where( am => am.Value == key ).First().Key ).Select( v => v.selected_value ).FirstOrDefault() );
        }
    }
}
