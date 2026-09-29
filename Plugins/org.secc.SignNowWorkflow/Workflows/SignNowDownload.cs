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
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.SignNow;
using Rock.Web.Cache;
using Rock.Workflow;

// The Rock.SignNow provider and bundled SignNowSDK are obsolete in Rock 16 with no
// direct replacement API. Suppressed pending retirement of the legacy SignNow
// e-signature workflow.
#pragma warning disable 612, 618

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

            string signNowDocumentId = action.GetWorkflowAttributeValue( GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid() );
            if ( string.IsNullOrEmpty( signNowDocumentId ) )
            {
                errorMessages.Add( "A sign now document is required to complete this action" );
                return false;
            }

            Guid documentGuid = action.GetWorkflowAttributeValue( GetActionAttributeValue( action, "Document" ).AsGuid() ).AsGuid();


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
            // The SDK returns null when the request fails, and a non-object JSON body or a non-JSON body
            // (e.g. a gateway error page) on SignNow errors, so don't assume a JObject.
            object documentResult;
            try
            {
                documentResult = SignNowSDK.Document.Get( token, signNowDocumentId );
            }
            catch ( JsonException ex )
            {
                errorMessages.Add( "SignNow Document Error: " + ex.Message );
                return false;
            }

            JObject document = documentResult as JObject;
            var signatures = document?["signatures"] as JArray;
            if ( signatures == null )
            {
                errorMessages.Add( "SignNow Document Error: " + DescribeResponse( documentResult ) );
                return false;
            }

            if ( signatures.Count > 0 )
            {
                string fileName = ( string ) document["document_name"];
                if ( string.IsNullOrWhiteSpace( fileName ) )
                {
                    fileName = "SignedDocument";
                }
                if ( !fileName.EndsWith( ".pdf", StringComparison.OrdinalIgnoreCase ) )
                {
                    fileName += ".pdf";
                }

                // ROCK-9041: Download into a directory unique to this call so concurrent runs can't read each
                // other's PDF, and remove it before any database work so cleanup can't fail after the save.
                // Previously every run shared %TEMP%\{document_name}.pdf, a FileStream on it was handed to the
                // storage provider and never disposed, and the delete targeted a path without ".pdf", so signed
                // PDFs piled up in the temp directory.
                byte[] signedPdfBytes;
                string tempDirectory = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() );
                try
                {
                    Directory.CreateDirectory( tempDirectory );

                    // The SDK saves to Path.GetDirectoryName( SaveFilePath ), so the trailing separator is required.
                    object downloadResult = SignNowSDK.Document.Download( token, signNowDocumentId, tempDirectory + Path.DirectorySeparatorChar, "signed" );
                    string downloadedFilePath = ( downloadResult as JObject )?.Value<string>( "file" );
                    if ( string.IsNullOrWhiteSpace( downloadedFilePath ) || !File.Exists( downloadedFilePath ) )
                    {
                        errorMessages.Add( "SignNow Download Error: " + DescribeResponse( downloadResult ) );
                        return false;
                    }

                    signedPdfBytes = File.ReadAllBytes( downloadedFilePath );

                    // The SDK fetches the file with a second request and saves its body without a status check, so
                    // a SignNow error on that request lands here as JSON rather than a PDF.
                    if ( !IsPdf( signedPdfBytes ) )
                    {
                        string body = Encoding.UTF8.GetString( signedPdfBytes, 0, Math.Min( signedPdfBytes.Length, 500 ) );
                        errorMessages.Add( "SignNow Download Error: the downloaded file is not a PDF. " + body );
                        return false;
                    }
                }
                // ArgumentNullException: the SDK's second request failed and it tried to save a null body.
                catch ( Exception ex ) when ( ex is IOException || ex is UnauthorizedAccessException || ex is JsonException || ex is ArgumentNullException )
                {
                    errorMessages.Add( "SignNow Download Error: " + ex.Message );
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

                // Put it into the workflow attribute
                BinaryFile signedPDF = binaryfileService.Get( documentGuid );
                if ( signedPDF == null )
                {
                    var destinationAttribute = AttributeCache.Get( GetActionAttributeValue( action, "Document" ).AsGuid(), rockContext );
                    if ( destinationAttribute == null )
                    {
                        errorMessages.Add( "The Document attribute for the SignNow Download action could not be found." );
                        return false;
                    }

                    // BinaryFile's save hook only stores content for new files that have a file type, so fall back
                    // to the default type when the attribute doesn't name one.
                    var binaryFileTypeService = new BinaryFileTypeService( rockContext );
                    BinaryFileType binaryFileType = null;
                    if ( destinationAttribute.QualifierValues.TryGetValue( "binaryFileType", out var binaryFileTypeQualifier ) )
                    {
                        var binaryFileTypeGuid = binaryFileTypeQualifier.Value.AsGuidOrNull();
                        if ( binaryFileTypeGuid.HasValue )
                        {
                            binaryFileType = binaryFileTypeService.Get( binaryFileTypeGuid.Value );
                        }
                    }
                    binaryFileType = binaryFileType ?? binaryFileTypeService.Get( Rock.SystemGuid.BinaryFiletype.DEFAULT.AsGuid() );
                    if ( binaryFileType == null )
                    {
                        errorMessages.Add( "No file type is available to store the signed SignNow document." );
                        return false;
                    }

                    signedPDF = new BinaryFile();
                    // TODO: This probably shouldn't be hardcoded
                    signedPDF.MimeType = "application/pdf";
                    signedPDF.FileName = fileName;
                    signedPDF.IsTemporary = false;
                    signedPDF.BinaryFileTypeId = binaryFileType.Id;
                    signedPDF.ContentStream = new MemoryStream( signedPdfBytes );
                    binaryfileService.Add( signedPDF );

                    rockContext.SaveChanges();

                    // Now store the attribute
                    if ( destinationAttribute.EntityTypeId == new Workflow().TypeId )
                    {
                        action.Activity.Workflow.SetAttributeValue( destinationAttribute.Key, signedPDF.Guid.ToString() );
                    }
                    else if ( destinationAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                    {
                        action.Activity.SetAttributeValue( destinationAttribute.Key, signedPDF.Guid.ToString() );
                    }
                }
                else
                {
                    // The existing file may have been uploaded as another type (e.g. DOCX); it now holds a PDF.
                    signedPDF.MimeType = "application/pdf";
                    signedPDF.FileName = fileName;
                    signedPDF.ContentStream = new MemoryStream( signedPdfBytes );

                    rockContext.SaveChanges();
                }

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

        private static string DescribeResponse( object response )
        {
            return response == null ? "No response from SignNow." : response.ToString();
        }

        private static bool IsPdf( byte[] content )
        {
            return content != null && content.Length >= 5 && Encoding.ASCII.GetString( content, 0, 5 ) == "%PDF-";
        }
    }
}
#pragma warning restore 612, 618
