using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Model;
using DotLiquid;
using Rock.Data;
using Rock.Web.Cache;
using Rock.Field;

namespace org.secc.PDF
{
    public class PDFWorkflowObject
    {
        public BinaryFile PDF { get; set; }
        public string LavaInput { get; set; }
        public Dictionary<string, object> MergeObjects { get; set; }
        public string RenderedXHTML
        {
            get
            {
                return LavaInput.ResolveMergeFields( MergeObjects );
            }
        }

        public PDFWorkflowObject()
        {
        }

        public PDFWorkflowObject( WorkflowAction action, RockContext rockContext )
        {
            //load merge objects
            MergeObjects = new Dictionary<string, object>();

            var activity = action.Activity;
            MergeObjects.Add( "Activity", activity );
            foreach ( var attribute in activity.AttributeValues )
            {
                var a = AttributeCache.Read( attribute.Value.AttributeId );
                if ( a != null && a.FieldType.Field is IEntityFieldType )
                {
                    MergeObjects.Add( attribute.Key, ( ( IEntityFieldType ) a.FieldType.Field ).GetEntity( attribute.Value.Value ) );
                }
                else
                {
                    MergeObjects.Add( attribute.Key, attribute.Value.ValueFormatted );
                }
            }

            var workflow = action.Activity.Workflow;
            MergeObjects.Add( "Workflow", workflow );
            foreach ( var attribute in workflow.AttributeValues )
            {
                var a = AttributeCache.Read( attribute.Value.AttributeId );
                if ( a != null && a.FieldType.Field is IEntityFieldType )
                {
                    MergeObjects.Add( attribute.Key, ( ( IEntityFieldType ) a.FieldType.Field ).GetEntity( attribute.Value.Value ) );
                }
                else
                {
                    MergeObjects.Add( attribute.Key, attribute.Value.ValueFormatted );
                }
            }
        }
    }
}
