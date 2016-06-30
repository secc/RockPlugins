using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Model;
using DotLiquid;
using Rock.Data;

namespace org.secc.PDF
{
    public class PDFWorkflowObject
    {
        public BinaryFile PDF { get; set; }
        public string LavaInput { get; set; }
        public Dictionary<string, object> MergeObjects { get; set; }
        public string RenderedXHTML {
            get
            {
                return LavaInput.ResolveMergeFields( MergeObjects );
            }
        }

        //The reason this is deprecated is to allow PDFs to be emailed or passed through without needing merged or generated first.
        [Obsolete("This property is deprecated. Please reference PDFWorkflowObject.PDF instead.", false)]
        public BinaryFile RenderedPDF {
            get
            {
                return PDF;
            }
        }

        public PDFWorkflowObject()
        {
        }

        public PDFWorkflowObject(WorkflowAction action, RockContext rockContext)
        {
            Utility.EnsureAttributes( action, rockContext );

            //load merge objects
            MergeObjects = new Dictionary<string, object>();

            int personId = action.Activity.Workflow.GetAttributeValue( "PersonId" ).AsInteger();
            if ( personId != 0 )
            {
                MergeObjects.Add( "Person", new PersonService( rockContext ).Get( personId ) );
            }

            int groupMemberId = action.Activity.Workflow.GetAttributeValue( "GroupMemberId" ).AsInteger();
            if ( groupMemberId != 0 )
            {
                var groupMember = new GroupMemberService( rockContext ).Get( groupMemberId );
                MergeObjects.Add( "GroupMember", groupMember );
                MergeObjects.Add( "Group", groupMember.Group );
            }

            int registrationRegistrantId = action.Activity.Workflow.GetAttributeValue( "RegistrationRegistrantId" ).AsInteger();
            if ( registrationRegistrantId != 0 )
            {
                var registrationRegistrant = new RegistrationRegistrantService( rockContext ).Get( registrationRegistrantId );
                MergeObjects.Add( "RegistrationRegistrant", registrationRegistrant );
                MergeObjects.Add( "Registration", registrationRegistrant.Registration );
                MergeObjects.Add( "RegistrationInstance", registrationRegistrant.Registration.RegistrationInstance );
            }

            var workflow = action.Activity.Workflow;
            workflow.LoadAttributes();
            MergeObjects.Add( "Workflow", workflow );

            //load pdf binary file
            Guid? pdfGuid = action.Activity.Workflow.GetAttributeValue( "PDFGuid" ).AsGuidOrNull();
            PDF = new BinaryFileService( rockContext ).Get( pdfGuid ?? new Guid() );
            MergeObjects.Add( "PDF", PDF );

            LavaInput = action.Activity.Workflow.GetAttributeValue( "XHTML" );
        }
    }
}
