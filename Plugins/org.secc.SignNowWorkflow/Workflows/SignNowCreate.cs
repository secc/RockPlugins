// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using org.secc.DevLib.Extensions;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.SignNow;
using Rock.Workflow;

// The Rock.SignNow provider and bundled SignNowSDK are obsolete in Rock 16 with no
// direct replacement API. Suppressed pending retirement of the legacy SignNow
// e-signature workflow.
#pragma warning disable 612, 618

namespace org.secc.SignNowWorkflow
{
    [ActionCategory( "SECC > Sign Now" )]
    [Description( "Handle all the SignNow functionality in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SignNow Create" )]
    [WorkflowAttribute( "SignNow Invite Link", "The attribute to save the SignNow invite link.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute to save the SignNow document id.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Document", "The attribute containing the document to upload to SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType", "Rock.Field.Types.FileFieldType" } )]
    [TextField( "Redirect Uri", "Webpage to redirect to after the document has been signed. <span class='tip tip-lava'></span>", false )]
    [TextField( "Signer Role", "Role the signature is assigned to.", true, "Applicant" )]
    class SignNowCreate : ActionComponent
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
            if ( renderedPDF == null )
            {
                errorMessages.Add( "Rendered PDF binary file was not found." );
                return false;
            }

            SignNow signNow = new SignNow();
            string snErrorMessage = "";
            String token = signNow.GetAccessToken( false, out snErrorMessage );
            if ( !string.IsNullOrEmpty( snErrorMessage ) )
            {
                errorMessages.Add( snErrorMessage );
                return false;
            }

            // Save the file to a temporary place for the upload, and remove it however the upload ends so the
            // unsigned document doesn't pile up in the temp directory on retries (ROCK-9041).
            string documentId;
            string tempDirectory = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
            try
            {
                Directory.CreateDirectory( tempDirectory );
                string tempFile = tempDirectory + Path.DirectorySeparatorChar + renderedPDF.FileName;

                // ROCK-9041: Read through a fresh provider stream; see BinaryFileExtensions.ReadContentBytes.
                File.WriteAllBytes( tempFile, renderedPDF.ReadContentBytes( "Rendered PDF" ) );

                // The SDK returns null when the request fails and non-object JSON on some SignNow errors.
                object result = SignNowSDK.Document.Create( token, tempFile, true );
                documentId = ( result as JObject )?.Value<string>( "id" );
                if ( string.IsNullOrWhiteSpace( documentId ) )
                {
                    errorMessages.Add( "SignNow Document Creation Error: " + DescribeResponse( result ) );
                    return false;
                }
            }
            // InvalidOperationException: ReadContentBytes found no content. IOException / UnauthorizedAccessException:
            // the temp file could not be written (e.g. a FileName with invalid path characters).
            catch ( Exception ex ) when ( ex is JsonException || ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException )
            {
                errorMessages.Add( "SignNow Document Creation Error: " + ex.Message );
                return false;
            }
            finally
            {
                try
                {
                    if ( Directory.Exists( tempDirectory ) )
                    {
                        Directory.Delete( tempDirectory, true );
                    }
                }
                catch ( Exception ex )
                {
                    action.AddLogEntry( $"Could not delete SignNow temp directory {tempDirectory}: {ex.Message}", true );
                }
            }

            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), documentId );

            var signerEmail = "guest_signer_" + Guid.NewGuid().ToString() + "@no.reply";
            var signerPassword = Guid.NewGuid().ToString();

            // The document now exists in SignNow and its id is saved above, so a failure below leaves that
            // document behind and a retry uploads a new one. Report the failure rather than throwing an NRE so
            // the workflow log says what went wrong. The SDK returns null when a request fails.
            string userAccessToken;
            try
            {
                object user = SignNowSDK.User.Create( signerEmail, signerPassword );
                if ( ( user as JObject )?.Value<string>( "id" ) == null )
                {
                    errorMessages.Add( "SignNow Signer Creation Error: " + DescribeResponse( user ) );
                    return false;
                }

                object oauthResult = SignNowSDK.OAuth2.RequestToken( signerEmail, signerPassword );
                userAccessToken = ( oauthResult as JObject )?.Value<string>( "access_token" );
                if ( string.IsNullOrWhiteSpace( userAccessToken ) )
                {
                    errorMessages.Add( "SignNow Signer Token Error: " + DescribeResponse( oauthResult ) );
                    return false;
                }

                dynamic dataobject = new
                {
                    to = new[]  {
                            new {
                                email = signerEmail,
                                role = GetAttributeValue(action,"SignerRole"),
                                role_id = "",
                                order = 1
                            }
                        },
                    from = "SignNow@secc.org"
                };

                // Create the invite; SignNow answers {"status":"success"} and an error object otherwise.
                object invite = SignNowSDK.Document.Invite( token, documentId, dataobject, DisableEmail: true );
                if ( ( invite as JObject )?.Value<string>( "status" ) != "success" )
                {
                    errorMessages.Add( "SignNow Invite Error: " + DescribeResponse( invite ) );
                    return false;
                }
            }
            catch ( JsonException ex )
            {
                errorMessages.Add( "SignNow Invite Error: " + ex.Message );
                return false;
            }

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

        private static string DescribeResponse( object response )
        {
            return response == null ? "No response from SignNow." : response.ToString();
        }
    }
}
#pragma warning restore 612, 618
