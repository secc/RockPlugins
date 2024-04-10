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
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.SignNow;
using Rock.Workflow;

namespace org.secc.SignNowWorkflow
{
    [ActionCategory( "SECC > Sign Now" )]
    [Description( "Handle all the SignNow functionality in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SignNow Create Two Signers" )]
    [WorkflowAttribute( "SignNow Invite Link", "The attribute to save the SignNow invite link.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute to save the SignNow document id.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Document", "The attribute containing the document to upload to SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType", "Rock.Field.Types.FileFieldType" } )]
    [TextField( "Redirect Uri", "Webpage to redirect to after the document has been signed. <span class='tip tip-lava'></span>", false )]
    [TextField( "Signer Role 1", "Role the first signature is assigned to.", true, "Applicant" )]
    [TextField( "Signer Role 2", "Role the second signature is assigned to.", true, "Applicant" )]
    class SignNowCreateTwoSigners : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // If this request isn't coming from a browser it can't be completed).
            if ( System.Web.HttpContext.Current == null )
            {
                return false;
            }

            Guid documentGuid = action.GetWorkflowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );

            BinaryFile renderedPDF = binaryfileService.Get( documentGuid );

            // Save the file to a temporary place
            string tempDirectory = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
            Directory.CreateDirectory( tempDirectory );
            string tempFile = tempDirectory + Path.DirectorySeparatorChar + renderedPDF.FileName;

            // Open a FileStream to write to the file:
            using ( Stream fileStream = File.OpenWrite( tempFile ) )
            {
                renderedPDF.ContentStream.CopyTo( fileStream );
            }

            SignNow signNow = new SignNow();
            string snErrorMessage = "";
            String token = signNow.GetAccessToken( false, out snErrorMessage );
            if ( !string.IsNullOrEmpty( snErrorMessage ) )
            {
                errorMessages.Add( snErrorMessage );
                return false;
            }
            JObject result = SignNowSDK.Document.Create( token, tempFile, true );
            string documentId = result.Value<string>( "id" );
            if ( string.IsNullOrWhiteSpace( documentId ) )
            {
                errorMessages.Add( "SignNow Document Creation Error: " + result.ToString() );
                return false;
            }
            // Clean up the temporary directory
            Directory.Delete( tempDirectory, true );

            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), documentId );

            var signerEmail1 = "guest_signer_" + Guid.NewGuid().ToString() + "@no.reply";
            var signerEmail2 = "witness_signer_" + Guid.NewGuid().ToString() + "@no.reply";
            var signerPassword = Guid.NewGuid().ToString();

            var user = SignNowSDK.User.Create( signerEmail1, signerPassword );

            JObject OAuthRes = SignNowSDK.OAuth2.RequestToken( signerEmail1, signerPassword );
            var userAccessToken = OAuthRes.Value<string>( "access_token" );

            dynamic dataobject = new
            {
                to = new[]  {
                        new {
                            email = signerEmail1,
                            role = GetAttributeValue(action,"SignerRole1"),
                            role_id = "",
                            order = 1
                        },
                        new {
                            email = signerEmail2,
                            role = GetAttributeValue(action,"SignerRole2"),
                            role_id = "",
                            order = 2
                        }
                    },
                from = "SignNow@secc.org"
            };


            // Get the invite link
            var generated = SignNowSDK.Document.Invite( token, documentId, dataobject, DisableEmail: true );

            var signNowInviteLink = string.Format(
                "https://signnow.com/dispatch?route=fieldinvite&document_id={0}&access_token={1}&mobileweb=mobileweb_only",
                documentId,
                userAccessToken );
            var mergeFields = GetMergeFields( action );
            var redirectUri = GetAttributeValue( action, "RedirectUri" ).ResolveMergeFields( mergeFields );

            if ( !string.IsNullOrWhiteSpace( redirectUri ) )
            {
                redirectUri += string.Format( "{0}document_id={1}", redirectUri.Contains( "?" ) ? "&" : "?", documentId );
                signNowInviteLink += "&redirect_uri=" + HttpUtility.UrlEncode( redirectUri );
            }

            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid(), signNowInviteLink );
            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), documentId );
            return true;
        }
    }
}

