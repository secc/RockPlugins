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
using System.Net.Http;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace org.secc.SignNowWorkflow
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Handle all the SignNow functionality in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SignNow Create" )]
    [WorkflowAttribute( "SignNow Invite Link", "The attribute to save the SignNow invite link.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute to save the SignNow document id.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Document", "The attribute to upload to SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType" } )]
    [TextField( "Redirect Uri", "Webpage to redirect to after the document has been signed", false )]
    class SignNowCreate : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            string url = "";

            // If this request isn't coming from a browser it can't be completed).
            if ( System.Web.HttpContext.Current == null )
            {
                return false;
            }

            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();
            string signNowInviteLink = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "signNowInviteLink" ).AsGuid() );

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );
            Person person = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() ).Person;

            if ( string.IsNullOrEmpty( signNowInviteLink ) )
            {

                BinaryFile renderedPDF = binaryfileService.Get( documentGuid );

                // Save the file to a temporary place
                string tempFile = Path.GetTempPath() + "VolunteerApplication_" + person.FirstName + person.LastName + ".pdf";

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
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), documentId );

                // Get the invite link
                signNowInviteLink = signNow.GetInviteLink( documentId, out errorMessages );

                var redirectUri = GetAttributeValue( action, "RedirectUri" );
                if ( !string.IsNullOrWhiteSpace( redirectUri ) )
                {
                    signNowInviteLink += "redirect_uri=" + redirectUri;
                }

                string newDocumentId = "";
                using ( var client = new HttpClient( new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate } ) )
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept", HttpContext.Current.Request.AcceptTypes );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept-Encoding", "gzip, deflate" );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "User-Agent", HttpContext.Current.Request.UserAgent );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept-Charset", "ISO-8859-1" );

                    client.BaseAddress = new Uri( signNowInviteLink );
                    HttpResponseMessage response = client.GetAsync( "" ).Result;
                    url = response.RequestMessage.RequestUri.AbsoluteUri;
                    MatchCollection mc = Regex.Matches( url, "document_id%253D([0-9,a-f]{40})" );
                    newDocumentId = mc[0].Groups[1].Value;
                }

                SignNowSDK.Document.Delete( token, documentId ); //Delete the original document

                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid(), url );
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), newDocumentId );

                // Delete the file when we are done:
                File.Delete( tempFile );
            }
            return true;
        }
    }
}
