// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using org.secc.Finance.Rest.Controllers;

namespace RockWeb
{
    /// <summary>
    /// Handles retrieving file data from storage
    /// </summary>
    public class GetStatement : IHttpAsyncHandler
    {
        internal class AttributeKeys
        {
            internal const string GivingId = "GivingId";
            internal const string PersonIds = "PersonIds";
        }

        /// <summary>
        /// Called to initialize an asynchronous call to the HTTP handler. 
        /// </summary>
        /// <param name="context">An HttpContext that provides references to intrinsic server objects used to service HTTP requests.</param>
        /// <param name="cb">The AsyncCallback to call when the asynchronous method call is complete.</param>
        /// <param name="extraData">Any state data needed to process the request.</param>
        /// <returns>An IAsyncResult that contains information about the status of the process.</returns>
        public IAsyncResult BeginProcessRequest( HttpContext context, AsyncCallback cb, object extraData )
        {
            int fileId = context.Request.QueryString["id"].AsInteger();
            Guid fileGuid = context.Request.QueryString["guid"].AsGuid();

            if ( fileGuid != Guid.Empty )
            {
                return BeginGetFile( cb, context, fileGuid );
            }
            else if ( fileId != 0 )
            {
                return BeginGetFile( cb, context, fileId );
            }
            else
            {
                return new SendErrorDelegate( SendError ).BeginInvoke( context, 400, "File id key must be a guid or an int.", cb, extraData );
            }
        }

        /// <summary>
        /// Provides an end method for an asynchronous process. 
        /// </summary>
        /// <param name="result">An IAsyncResult that contains information about the status of the process.</param>
        public void EndProcessRequest( IAsyncResult result )
        {
            // restore the context from the asyncResult.AsyncState 
            HttpContext context = ( HttpContext ) result.AsyncState;

            if ( context != null )
            {

                context.Response.Clear();

                var rockContext = CreateRockContext();

                BinaryFile binaryFile = EndGetFile( result, context, rockContext );

                // Only contribution statement files are served here, and each one is checked
                // against the current person whether or not its file type requires view security.
                var statementFileTypeId = GetFileTypeIdByGuid( FinancialStatementsController.CONTRIBUTION_STATEMENT_FILETYPE_GUID.AsGuid() );
                if ( binaryFile != null && statementFileTypeId.HasValue && binaryFile.BinaryFileTypeId == statementFileTypeId.Value )
                {
                    var currentUser = GetCurrentUser( rockContext );
                    Person currentPerson = currentUser != null ? currentUser.Person : null;
                    binaryFile.BinaryFileType = binaryFile.BinaryFileType ?? GetBinaryFileType( binaryFile.BinaryFileTypeId.Value, rockContext );
                    if ( currentUser == null )
                    {
                        SendError( context, 403, "Not authorized to view file." );
                        return;
                    }
                    if ( !IsAuthorizedToView( binaryFile, currentPerson ) )
                    {
                        LoadAttributes( binaryFile );
                        var previousPersonIds = currentPerson.Aliases.Select( pa => pa.AliasPersonId ).ToArray();
                        var binaryFilePersonIds = binaryFile.GetAttributeValue( AttributeKeys.PersonIds ).SplitDelimitedValues().Select( pi => pi.AsIntegerOrNull() );
                        if ( !( binaryFile.GetAttributeValue( AttributeKeys.GivingId ) == currentPerson.GivingId || previousPersonIds.Intersect( binaryFilePersonIds ).Any() ) )
                        {
                            SendError( context, 403, "Not authorized to view file." );
                            return;
                        }
                    }

                    SendBinaryFile( context, binaryFile );
                    return;
                }

                SendError( context, 404, "File could not be found." );
            }
        }

        /// <summary>
        /// Creates the RockContext used to read the file, its type and the current user.
        /// </summary>
        protected virtual RockContext CreateRockContext()
        {
            return new RockContext();
        }

        /// <summary>
        /// Starts reading the binary file with the given Guid.
        /// </summary>
        protected virtual IAsyncResult BeginGetFile( AsyncCallback cb, HttpContext context, Guid fileGuid )
        {
            return new BinaryFileService( new RockContext() ).BeginGet( cb, context, fileGuid );
        }

        /// <summary>
        /// Starts reading the binary file with the given Id.
        /// </summary>
        protected virtual IAsyncResult BeginGetFile( AsyncCallback cb, HttpContext context, int fileId )
        {
            return new BinaryFileService( new RockContext() ).BeginGet( cb, context, fileId );
        }

        /// <summary>
        /// Finishes reading the binary file, or returns null if it wasn't found.
        /// </summary>
        protected virtual BinaryFile EndGetFile( IAsyncResult result, HttpContext context, RockContext rockContext )
        {
            return new BinaryFileService( rockContext ).EndGet( result, context );
        }

        /// <summary>
        /// Gets the Id of the binary file type with the given Guid, or null if it doesn't exist.
        /// </summary>
        protected virtual int? GetFileTypeIdByGuid( Guid fileTypeGuid )
        {
            return BinaryFileTypeCache.GetId( fileTypeGuid );
        }

        /// <summary>
        /// Gets the binary file type with the given Id.
        /// </summary>
        protected virtual BinaryFileType GetBinaryFileType( int binaryFileTypeId, RockContext rockContext )
        {
            return new BinaryFileTypeService( rockContext ).Get( binaryFileTypeId );
        }

        /// <summary>
        /// Gets the current user's login, or null if no one is logged in.
        /// </summary>
        protected virtual UserLogin GetCurrentUser( RockContext rockContext )
        {
            return new UserLoginService( rockContext ).GetByUserName( UserLogin.GetCurrentUserName() );
        }

        /// <summary>
        /// Determines whether the person has View access to the binary file.
        /// </summary>
        protected virtual bool IsAuthorizedToView( BinaryFile binaryFile, Person person )
        {
            return binaryFile.IsAuthorized( Authorization.VIEW, person );
        }

        /// <summary>
        /// Loads the binary file's attribute values.
        /// </summary>
        protected virtual void LoadAttributes( BinaryFile binaryFile )
        {
            binaryFile.LoadAttributes();
        }

        /// <summary>
        /// Sends the binary file's content.
        /// </summary>
        protected virtual void SendBinaryFile( HttpContext context, BinaryFile binaryFile )
        {
            SendFile( context, binaryFile.ContentStream, binaryFile.MimeType, binaryFile.FileName, binaryFile.Guid.ToString( "N" ) );
        }

        /// <summary>
        /// Sends the file.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="eTag">The e tag.</param>
        private void SendFile( HttpContext context, Stream fileContents, string mimeType, string fileName, string eTag )
        {
            int startIndex = 0;
            int fileLength = ( int ) fileContents.Length;
            int responseLength = fileLength;

            // resumable logic from http://stackoverflow.com/a/6475414/1755417
            if ( context.Request.Headers["Range"] != null && ( context.Request.Headers["If-Range"] == null ) )
            {
                var match = Regex.Match( context.Request.Headers["Range"], @"bytes=(\d*)-(\d*)" );
                startIndex = match.Groups[1].Value.AsInteger();
                responseLength = ( match.Groups[2].Value.AsIntegerOrNull() + 1 ?? fileLength ) - startIndex;
                context.Response.StatusCode = ( int ) System.Net.HttpStatusCode.PartialContent;
                context.Response.Headers["Content-Range"] = "bytes " + startIndex + "-" + ( startIndex + responseLength - 1 ) + "/" + fileLength;
            }

            context.Response.Clear();
            context.Response.Buffer = false;
            context.Response.Headers["Accept-Ranges"] = "bytes";

            bool sendAsAttachment = context.Request.QueryString["attachment"].AsBooleanOrNull() ?? false;

            context.Response.AddHeader( "content-disposition", string.Format( "{1};filename={0}", fileName.MakeValidFileName(), sendAsAttachment ? "attachment" : "inline" ) );
            context.Response.AddHeader( "content-length", responseLength.ToString() );
            context.Response.Cache.SetCacheability( HttpCacheability.Public ); // required for etag output

            context.Response.Cache.SetETag( eTag ); // required for IE9 resumable downloads
            context.Response.ContentType = mimeType;
            byte[] buffer = new byte[4096];

            if ( context.Response.IsClientConnected )
            {
                using ( var fileStream = fileContents )
                {
                    if ( fileStream.CanSeek )
                    {
                        fileStream.Seek( startIndex, SeekOrigin.Begin );
                    }
                    while ( true )
                    {
                        var bytesRead = fileStream.Read( buffer, 0, buffer.Length );
                        if ( bytesRead == 0 )
                        {
                            break;
                        }

                        if ( !context.Response.IsClientConnected )
                        {
                            // quit sending if the client isn't connected
                            break;
                        }

                        try
                        {
                            context.Response.OutputStream.Write( buffer, 0, bytesRead );
                        }
                        catch ( HttpException ex )
                        {
                            if ( !context.Response.IsClientConnected )
                            {
                                // if client disconnected during the .write, ignore
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }
            }

            context.ApplicationInstance.CompleteRequest();
        }


        private delegate void SendErrorDelegate( HttpContext context, int code, string message );

        /// <summary>
        /// Sends an error code response and completes the request.
        /// </summary>
        /// <param name="context">THe HttpContext for this request.</param>
        /// <param name="code">The response code to send.</param>
        /// <param name="message">The response message to send.</param>
        protected virtual void SendError( HttpContext context, int code, string message )
        {
            context.Response.Clear();
            context.Response.StatusCode = code;
            context.Response.StatusDescription = message;
            context.ApplicationInstance.CompleteRequest();
        }

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler" /> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext" /> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ProcessRequest( HttpContext context )
        {
            throw new NotImplementedException( "The method or operation is not implemented. This is an asynchronous file handler." );
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.</returns>
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}