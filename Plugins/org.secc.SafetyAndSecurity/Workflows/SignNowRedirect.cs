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

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Handle all the SignNow functionality in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SignNow Redirect" )]
    [WorkflowAttribute( "SignNow Invite Link", "The attribute to save the SignNow invite link.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute to save the SignNow document id.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute ( "Document", "The attribute to upload to SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BinaryFileFieldType" } ) ]
    class SignNowRedirect : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            
            Guid documentGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();
            string signNowInviteLink = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "signNowInviteLink" ).AsGuid() );
            errorMessages = new List<string>();

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );
            Person person = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() ).Person;

            string signedDocumentId = System.Web.HttpContext.Current.Request.QueryString["document_id"];
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
                JObject result = CudaSign.Document.Create( token, tempFile, true );
                string documentId = result.Value<string>( "id" );
                SetWorkflowAttributeValue( action, GetActionAttributeValue(action, "SignNowDocumentId").AsGuid(), documentId );

                // Get the invite link
                signNowInviteLink = signNow.GetInviteLink( documentId, out errorMessages );
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "SignNowInviteLink" ).AsGuid(), signNowInviteLink );

                // Delete the file when we are done:
                File.Delete( tempFile );
            }
            System.Web.HttpContext.Current.Response.Redirect( signNowInviteLink + "?redirect_uri=" + System.Web.HttpContext.Current.Request.Url.ToString().UrlEncode(), false );
            return true;
            
        }
    }
}
