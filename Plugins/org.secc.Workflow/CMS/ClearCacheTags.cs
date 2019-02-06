using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.CMS
{
    [ExportMetadata( "ComponentName", "Clear Cache Tags" )]
    [ActionCategory( "SECC > CMS" )]
    [Description( "Clears all cached items with selected tag(s)." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowTextOrAttribute( "Cache Tags", "Cache Tags Attribute", "The a comma separated list of cache tags. <span class='tip tip-lava'></span>", true, "", "", 3, "CacheTags", new string[] { "Rock.Field.Types.DateTimeFieldType", "Rock.Field.Types.TextFieldType" } )]

    class ClearCacheTags : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var cacheTagsAttributeValue = GetActionAttributeValue( action, "CacheTags" );

            var cacheTags = "";
            //first check to see if is attribute
            var cacheTagsGuid = cacheTagsAttributeValue.AsGuidOrNull();
            if ( cacheTagsGuid != null )
            {
                cacheTags = action.GetWorklowAttributeValue( cacheTagsGuid.Value );
            }
            else
            {
                //the value is the cachetags
                cacheTags = cacheTagsAttributeValue;
            }
            RockCache.RemoveForTags( cacheTags );
            action.AddLogEntry( "Cleared cache tags: " + cacheTags );

            return true;
        }
    }
}
