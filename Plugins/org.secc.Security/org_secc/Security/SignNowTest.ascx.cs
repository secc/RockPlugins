using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Security;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Security;
using System.Text;
using Rock.Data;
using Rock.Model;
using System.IO;
using Rock.SignNow;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace RockWeb.Plugins.org_secc.Security
{
    /// <summary>
    /// Displays currently logged in user's name along with options to Login, Logout, or manage account.
    /// </summary>
    [DisplayName( "SignNow Test" )]
    [Category( "SECC > Security" )]
    [Description( "Test for sign now." )]

    public partial class SignNowTest : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            RockContext rockContext = new RockContext();

            Guid documentGuid = "5bb17a5b-1eaa-4e41-82e1-2ee861d26658".AsGuid();
            string signNowInviteLink = "";

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            BinaryFileService binaryfileService = new BinaryFileService( rockContext );
            Person person = CurrentPerson;

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

                JObject result = SignNowSDK.Document.Create( token, tempFile, false ); //Changed from true
                string documentId = result.Value<string>( "id" );

                // Get the invite link
                var error = new List<string>();
                signNowInviteLink = signNow.GetInviteLink( documentId, out error );
                string url = "";
                string newDocumentId = "";
                using ( var client = new HttpClient( new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate } ) )
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept", Context.Request.AcceptTypes );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept-Encoding", "gzip, deflate" );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "User-Agent", Context.Request.UserAgent );
                    client.DefaultRequestHeaders.TryAddWithoutValidation( "Accept-Charset", "ISO-8859-1" );

                    client.BaseAddress = new Uri( signNowInviteLink );
                    HttpResponseMessage response = client.GetAsync( "" ).Result;
                    url = response.RequestMessage.RequestUri.AbsoluteUri;
                    MatchCollection mc = Regex.Matches( url, "document_id%253D([0-9,a-f]{40})" );
                    newDocumentId = mc[0].Groups[1].Value;
                }

                // Delete the file when we are done:
                File.Delete( tempFile );
                JObject createLinkRes = SignNowSDK.Link.Create( token, documentId );
            }
        }
    }
}