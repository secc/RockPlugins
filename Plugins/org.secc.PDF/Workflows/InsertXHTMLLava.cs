using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Inserts XTML and Lava into workflow to be merged and generated." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Insert XHTML and Lava" )]
    [CodeEditorField( "XHTML", "XHTML and Lava to be merged with merge fields." )]
    class InsertXTMLLava : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Utility.EnsureAttributes( action, rockContext );

            action.Activity.Workflow.SetAttributeValue( "XHTML", GetActionAttributeValue( action, "XHTML" ) );

            return true;
        }
    }
}
