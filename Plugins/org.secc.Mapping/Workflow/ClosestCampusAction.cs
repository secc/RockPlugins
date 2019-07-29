using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using org.secc.Mapping.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Mapping.Workflow
{
    [ExportMetadata( "ComponentName", "Closest Campus" )]
    [ActionCategory( "SECC > Mapping" )]
    [Description( "Finds closest campus to an address." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowTextOrAttribute( "Origin Address", "Origin Address Attribute", "Address to search from as the origin location. <span class='tip tip-lava'></span>", true, "", "", 0, "Origin", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Campus", "Ouptput of the campus which is closest to the origin location.", true, order: 1, fieldTypeClassNames: new string[] { "Rock.Field.Types.CampusFieldType" } )]

    class ClosestCampusAction : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var originAttributeValue = GetActionAttributeValue( action, "Origin" );

            var origin = "";
            //first check to see if is attribute
            var cacheTagsGuid = originAttributeValue.AsGuidOrNull();
            if ( cacheTagsGuid != null )
            {
                origin = action.GetWorklowAttributeValue( cacheTagsGuid.Value );
            }
            else
            {
                var mergeFields = GetMergeFields( action );
                origin = originAttributeValue.ResolveMergeFields( mergeFields );
            }

            try
            {

                var campusTask = Task.Run( async () => await CampusUtilities.GetClosestCampus( origin ) );
                campusTask.Wait();

                var campusAttributeValue = GetActionAttributeValue( action, "Campus" ).AsGuid();
                SetWorkflowAttributeValue( action, campusAttributeValue, campusTask.Result.Guid.ToString() );
            }
            catch ( Exception e )
            {
                action.AddLogEntry( "Exception while trying to load closest campus: " + e.Message, true );
            }


            return true;
        }
    }
}
