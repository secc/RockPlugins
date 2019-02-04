using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;

namespace org.secc.Workflow.Schedule
{
    [ActionCategory( "SECC > Utilities" )]
    [Description( "Pre-calculates the next start date time for the schedule." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Pre-Calculate Schedule Next Start Date" )]
    [WorkflowAttribute( "Schedule", "The attribute which contains the schedule", fieldTypeClassNames: new string[] { "Rock.Field.Types.ScheduleFieldType" } )]
    [TextField( "Attribute Key", "Attribute key to store the date in.", defaultValue: "NextStartDate" )]

    class PreCalculateNextStartDate : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var scheduleGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Schedule" ).AsGuid() ).AsGuid();
            ScheduleService scheduleService = new ScheduleService( rockContext );
            var schedule = scheduleService.Get( scheduleGuid );
            if ( schedule != null )
            {
                schedule.LoadAttributes();
                schedule.SetAttributeValue( GetActionAttributeValue( action, "AttributeKey" ), schedule.GetNextStartDateTime( Rock.RockDateTime.Now ).ToString() );
                schedule.SaveAttributeValues();
            }
            return true;
        }
    }
}
