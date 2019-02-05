using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.Schedule
{
    [ActionCategory( "SECC > Schedule" )]
    [Description( "Gets the next start date for a schedule." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Schedule Next Start Date" )]
    [WorkflowAttribute( "Schedule Attribute", "The attribute which contains the schedule", fieldTypeClassNames: new string[] { "Rock.Field.Types.ScheduleFieldType" } )]
    [WorkflowAttribute( "DateTime Attribute", "The attribute to store the next date time.", fieldTypeClassNames: new string[] { "Rock.Field.Types.DateTimeFieldType", "Rock.Field.Types.TextFieldType" } )]

    class ScheduleNextStartDate : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var scheduleGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "ScheduleAttribute" ).AsGuid() ).AsGuid();
            ScheduleService scheduleService = new ScheduleService( rockContext );
            var schedule = scheduleService.Get( scheduleGuid );
            if ( schedule != null )
            {
                // Now store the target attribute
                var targetAttribute = AttributeCache.Get( GetActionAttributeValue( action, "DateTimeAttribute" ).AsGuid(), rockContext );
                if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                {
                    action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, schedule.GetNextStartDateTime( Rock.RockDateTime.Now ).ToString() );
                }
                else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                {
                    action.Activity.SetAttributeValue( targetAttribute.Key, schedule.GetNextStartDateTime( Rock.RockDateTime.Now ).ToString() );
                }

            }
            return true;
        }
    }
}
