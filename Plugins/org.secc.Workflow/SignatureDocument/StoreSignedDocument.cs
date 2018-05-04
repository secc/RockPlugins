// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;

namespace org.secc.Workflow.SignatureDocument
{
    [ActionCategory( "SECC > Signature Document" )]
    [Description( "Create a new Signature Document." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Store Signed Document" )]
    [WorkflowTextOrAttribute( "Document Key", "Attribute Value", "The document key or an attribute that contains the signature document provider's key. <span class='tip tip-lava'></span>", false, "", "", 2, "DocumentKey", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Document Name", "Attribute Value", "The document name or an attribute that contains the document's name. <span class='tip tip-lava'></span>", true, "", "", 3, "DocumentName", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Assigned To Attribute", "The person attribute that this signature is/was assigned to.", true, "", "", 4, "AssignedTo", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "Applies To Attribute", "The person attribute that this signature is/was assigned to.", true, "", "", 5, "AppliesTo", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "Signed By Attribute", "The person attribute that this signature is/was assigned to.", true, "", "", 6, "SignedBy", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "Document", "The attribute containing the document to store as a Signature Document.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType", "Rock.Field.Types.FileFieldType" } )]
    [CustomDropdownListField( "Signature Document Template", "The template of the signature document.", "select Id as Value, Name as Text from SignatureDocumentTemplate;", true, key:"DocumentTemplateId" )]
    class StoreSignedDocument : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            SignatureDocumentService signatureDocumentService = new SignatureDocumentService( rockContext );
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );
            PersonAliasService personAliasService = new PersonAliasService( rockContext );

            errorMessages = new List<string>();

            // Get all the attribute values
            var mergeFields = GetMergeFields( action );
            int documentTemplateId = GetAttributeValue( action, "DocumentTemplateId", true ).AsInteger();
            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();
            BinaryFile binaryFile = binaryfileService.Get( documentGuid );
            string documentKey = GetAttributeValue( action, "DocumentKey", true ).ResolveMergeFields( mergeFields );
            string documentName = GetAttributeValue( action, "DocumentName", true ).ResolveMergeFields( mergeFields );
            Guid assignedTo = GetAttributeValue( action, "AssignedTo", true ).AsGuid();
            Guid appliesTo = GetAttributeValue( action, "AppliesTo", true ).AsGuid();
            Guid signedBy = GetAttributeValue( action, "SignedBy", true ).AsGuid();

            if ( binaryFile != null)
            {
                // Create the signature document
                Rock.Model.SignatureDocument document = new Rock.Model.SignatureDocument();
                document.AssignedToPersonAliasId = personAliasService.Get( assignedTo ).Id;
                document.AppliesToPersonAliasId = personAliasService.Get( appliesTo ).Id;
                document.SignedByPersonAliasId = personAliasService.Get( signedBy ).Id;
                document.Name = documentName;
                document.DocumentKey = documentKey;
                document.BinaryFileId = binaryFile.Id;
                document.SignatureDocumentTemplateId = documentTemplateId;
                document.LastInviteDate = RockDateTime.Now;
                document.LastStatusDate = RockDateTime.Now;
                document.Status = SignatureDocumentStatus.Signed;

                // Add the signature document to the service
                signatureDocumentService.Add( document );
            }

            return true;
        }
    }
}
