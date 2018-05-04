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
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using System.IO;
using Rock.SignNow;
using Newtonsoft.Json.Linq;
using Rock.Attribute;
using Rock.Web.Cache;
using System.Linq;

namespace org.secc.SignNowWorkflow
{
    [ActionCategory( "SECC > Sign Now" )]
    [ExportMetadata( "ComponentName", "SignNow Download" )]
    [Description( "Checks to see if the document has been signed and downloads it if so." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute which contains the document to check.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Document", "The attribute to store the signed document from SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType", "Rock.Field.Types.FileFieldType" } )]
    [WorkflowAttribute( "PDF Signed", "Indicator that we have a signed document from SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    class SignNowDownload : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Check to see if the action's activity does not yet have the the 'InviteLink' attribute.
            string signNowDocumentId = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid() );
            if ( string.IsNullOrEmpty( signNowDocumentId ) )
            {
                errorMessages.Add( "A sign now document is required to complete this action" );
                return false;
            }

            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();


            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );


            SignNow signNow = new SignNow();
            string snErrorMessage = "";
            string token = signNow.GetAccessToken( false, out snErrorMessage );
            if ( !string.IsNullOrEmpty( snErrorMessage ) )
            {
                errorMessages.Add( snErrorMessage );
                return false;
            }

            //Check if document is signed.
            JObject document = SignNowSDK.Document.Get( token, signNowDocumentId );
            if ( ( ( JArray ) document["signatures"] ).Count() > 0 )
            {
                // Download the file
                string tempPath = Path.GetTempPath();
                string tempFileName = (String)document["document_name"];
                var result = SignNowSDK.Document.Download( token, signNowDocumentId, tempPath, tempFileName );

                // Put it into the workflow attribute
                BinaryFile signedPDF = binaryfileService.Get( documentGuid );
                if ( signedPDF == null )
                {
                    signedPDF = new BinaryFile();
                    // TODO: This probably shouldn't be hardcoded
                    signedPDF.MimeType = "application/pdf";
                    signedPDF.FileName = tempFileName;
                    signedPDF.IsTemporary = false;
                    binaryfileService.Add(signedPDF);

                    // Update the file type if necessary
                    Guid binaryFileTypeGuid = Guid.Empty;

                    var destinationAttribute = AttributeCache.Read(GetActionAttributeValue(action, "Document").AsGuid(), rockContext);
                    var binaryFileTypeQualifier = destinationAttribute.QualifierValues["binaryFileType"];
                    if (!String.IsNullOrWhiteSpace(binaryFileTypeQualifier.Value))
                    {
                        if (binaryFileTypeQualifier.Value != null)
                        {
                            binaryFileTypeGuid = binaryFileTypeQualifier.Value.AsGuid();

                            signedPDF.BinaryFileTypeId = new BinaryFileTypeService(rockContext).Get(binaryFileTypeGuid).Id;
                        }
                    }
                    signedPDF.DatabaseData = new BinaryFileData();
                    signedPDF.DatabaseData.Content = File.ReadAllBytes( tempPath + tempFileName + ".pdf" );

                    rockContext.SaveChanges();

                    // Now store the attribute
                    if (destinationAttribute.EntityTypeId == new Workflow().TypeId)
                    {
                        action.Activity.Workflow.SetAttributeValue(destinationAttribute.Key, signedPDF.Guid.ToString());
                    }
                    else if (destinationAttribute.EntityTypeId == new WorkflowActivity().TypeId)
                    {
                        action.Activity.SetAttributeValue(destinationAttribute.Key, signedPDF.Guid.ToString());
                    }
                }
                else
                {
                    signedPDF.FileName = tempFileName;
                    signedPDF.DatabaseData.Content = File.ReadAllBytes(tempPath + tempFileName + ".pdf");
                }

                // Delete the file when we are done:
                File.Delete( tempPath + tempFileName );

                // We have a signed copy
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "PDFSigned" ).AsGuid(), "True" );

            }
            else
            {
                // Not signed yet.
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "PDFSigned" ).AsGuid(), "False" );
            }
            return true;
        }
    }
}
