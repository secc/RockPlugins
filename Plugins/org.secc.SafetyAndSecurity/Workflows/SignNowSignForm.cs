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
using System.Web;
using SignNowSDK;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Rock.Web.UI.Controls;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Handles the Sign Now redirect and response." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SignNow Sign Form" )]
    [WorkflowAttribute( "SignNow Invite Link", "The attribute to save the SignNow invite link.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute to save the SignNow document id.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Source Document", "The attribute to upload to SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType" } )]
    [WorkflowAttribute( "Signed Document", "The attribute to save the document from SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType" } )]
    [TextField( "Signed Document Name", "Then name of the document to be merged. <span class='tip tip-lava'></span>", true )]
    class SignNowSignForm : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {

            errorMessages = new List<string>();
            if ( HttpContext.Current != null )
            {
                string signedDocumentId = System.Web.HttpContext.Current.Request.QueryString["document_id"];
                if ( !string.IsNullOrWhiteSpace( signedDocumentId ) )
                {
                    return LandDocument( rockContext, action, out errorMessages );
                }
                else
                {
                    Redirect( rockContext, action, out errorMessages );
                    return false;
                }
            }
            return false;
        }


        /// <summary>
        /// Send the user redirected to sign a signnow document
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="action"></param>
        /// <param name="errorMessages"></param>
        private void Redirect( RockContext rockContext, WorkflowAction action, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SourceDocument" ).AsGuid() ).AsGuid();
            string signNowInviteLink = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid() );

            BinaryFileService binaryfileService = new BinaryFileService( rockContext );

            string signedDocumentId = HttpContext.Current.Request.QueryString["document_id"];
            if ( string.IsNullOrEmpty( signNowInviteLink ) )
            {

                BinaryFile renderedPDF = binaryfileService.Get( documentGuid );
                var mergeFields = GetMergeFields( action );
                string tempFileName = GetActionAttributeValue( action, "SignedDocumentName" ).ResolveMergeFields( mergeFields );
                // Save the file to a temporary place
                string tempFile = Path.GetTempPath() + tempFileName + ".pdf";

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
                }
                JObject result = SignNowSDK.Document.Create( token, tempFile, true );
                string documentId = result.Value<string>( "id" );
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), documentId );

                // Get the invite link
                signNowInviteLink = signNow.GetInviteLink( documentId, out errorMessages );
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid(), signNowInviteLink );

                // Delete the file when we are done:
                File.Delete( tempFile );
            }

            //Save the workflow now. If not we will loose our attribute values.
            PersistWorkflow( action, rockContext );
            var redirect = ( HttpContext.Current.Request.Url.ToString() + "?WorkflowGuid=" + action.Activity.Workflow.Guid.ToString() ).UrlEncode();

            var url = string.Format( "{0}?redirect_uri={1}", signNowInviteLink, redirect );
            HttpContext.Current.Response.Redirect( url, false );
        }

        private bool LandDocument( RockContext rockContext, WorkflowAction action, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            SignNow signNow = new SignNow();
            string snErrorMessage = "";
            string token = signNow.GetAccessToken( false, out snErrorMessage );
            if ( !string.IsNullOrEmpty( snErrorMessage ) )
            {
                errorMessages.Add( snErrorMessage );
                return false;
            }

            //Get the document and save to 
            string signedDocumentId = HttpContext.Current.Request.QueryString["document_id"];
            string tempPath = Path.GetTempPath();
            var mergeFields = GetMergeFields( action );
            string tempFileName = GetActionAttributeValue( action, "SignedDocumentName" ).ResolveMergeFields( mergeFields );
            JObject result = Document.Download( token, signedDocumentId, tempPath, tempFileName );
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SignedDocument" ).AsGuid() ).AsGuid();
            BinaryFile signedPDF = binaryFileService.Get( documentGuid );

            if ( signedPDF == null )
            {
                Guid sourceDocumentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SourceDocument" ).AsGuid() ).AsGuid();
                BinaryFile sourcePDF = binaryFileService.Get( sourceDocumentGuid );
                signedPDF = new BinaryFile();
                signedPDF.CopyPropertiesFrom( sourcePDF );
                signedPDF.Id = 0;
                signedPDF.Guid = Guid.NewGuid();
                binaryFileService.Add( signedPDF );
                signedPDF.DatabaseData = new BinaryFileData();
            }
            signedPDF.FileName = tempFileName;
            signedPDF.DatabaseData.Content = File.ReadAllBytes( tempPath + tempFileName + ".pdf" );
            rockContext.SaveChanges();

            //Save it to the workflow action
            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignedDocument" ).AsGuid(), signedPDF.Guid.ToString() );


            // Delete the file when we are done:
            File.Delete( tempPath + tempFileName );

            //Delete the unsigned copy on the network
            Guid documentIdAttributeActionGuid = GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid();
            string signNowDocumentId = action.GetWorklowAttributeValue( documentIdAttributeActionGuid );
            JObject deleteResult = Document.Delete( token, signNowDocumentId );

            // Just clear out the Invite and DocumentId links
            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid(), "" );
            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid(), "" );
            return true;
        }

        /// <summary>
        /// Immediatly persist workflow and save attributes. This is needed because 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="rockContext"></param>
        private void PersistWorkflow( WorkflowAction action, RockContext rockContext )
        {
            var workflow = action.Activity.Workflow;
            workflow.IsPersisted = true;

            //this is needed for the user entry form to be able to 
            //action.ActionType.WorkflowForm = new WorkflowActionForm();
            //action.ActionType.WorkflowForm.Actions = "";

            var service = new WorkflowService( rockContext );
            if ( workflow.Id == 0 )
            {
                service.Add( workflow );
            }

            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();
                workflow.SaveAttributeValues( rockContext );
                foreach ( var activity in workflow.Activities )
                {
                    activity.SaveAttributeValues( rockContext );
                }
            } );
            action.AddLogEntry( "Updated workflow to be persisted!" );

        }

        public bool Display(WorkflowAction action, RockContext rockContext, PlaceHolder phContent)
        {
            // This doesn't need to display anything since it's just a redirect and landing
            return false;
        }


    }
}
