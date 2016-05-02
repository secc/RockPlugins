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
    [Description( "Inserts PDF form file into workflow to be filled out." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Insert PDF Form File" )]
    [BinaryFileField(Rock.SystemGuid.BinaryFiletype.MERGE_TEMPLATE, "PDF", "PDF form file to fill out.", true)]
    class InsertPDFFormFile : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Utility.EnsureAttributes( action, rockContext );

            var pdfBinary = GetActionAttributeValue( action, "PDF" );

            action.Activity.Workflow.SetAttributeValue( "PDFGuid", pdfBinary );

            return true;
        }
    }
}
