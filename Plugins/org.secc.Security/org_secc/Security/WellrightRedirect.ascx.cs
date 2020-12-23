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
using Rock;
using Rock.Attribute;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.IO;
using org.secc.Security.SAML2;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Security
{
    /// <summary>
    /// Displays currently logged in user's name along with options to Login, Logout, or manage account.
    /// </summary>
    [DisplayName( "Wellright Redirect" )]
    [Category( "SECC > Security" )]
    [Description( "Posts the user back to Wellright with SAML login" )]

    [TextField( "SAML Request Url",
        Description = "The request url to put in the SAML Assertion",
        Key = AttributeKey.RequestUrl,
        DefaultValue = "https://ssotest.qa.wellright.com/account/login/ssologin/Account/Login/SSOLogin" )]

    [TextField( "Post Url",
        Description = "The url to post the SAML response to.",
        Key = AttributeKey.PostUrl,
        DefaultValue = "https://southeastchristianchurch.wellright.com/account/login/ssologin" )]

    [FileField( Rock.SystemGuid.BinaryFiletype.DEFAULT,
        "Certificate",
        "Certificate in pfx format to encryt the data with.",
        key: AttributeKey.CertificateFile )]

    [TextField( "Certificate Password",
        Description = "The password for the certificate",
        Key = AttributeKey.CertificatePassword )]


    [IntegerField( "Days Valid",
        Description = "The number of days the assertion is valid.",
        Key = AttributeKey.DaysValid,
        DefaultValue = "30" )]

    [BooleanField( "Show Debug",
        Description = "Should administrators be shown a debug menu before being forwarded to WellRight?",
        Key = AttributeKey.ShowDebug,
        DefaultValue = "False" )]

    public partial class WellrightRedirect : Rock.Web.UI.RockBlock
    {

        internal static class AttributeKey
        {
            internal const string RequestUrl = "RequestUrl";
            internal const string PostUrl = "PostUrl";
            internal const string CertificateFile = "CertificateFile";
            internal const string CertificatePassword = "CertificatePassword";
            internal const string DaysValid = "DaysValid";
            internal const string ShowDebug = "ShowDebug";
        }

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                var response = GetResponse();

                if ( UserCanAdministrate
                    && !PageParameter( "Continue" ).AsBoolean()
                    && GetAttributeValue( AttributeKey.ShowDebug ).AsBoolean() == true )
                {
                    DisplayData( response );
                }
                else
                {
                    Redirect( response );
                }
            }
        }

        private string GetResponse()
        {
            if ( CurrentPerson == null )
            {
                return "";
            }

            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( GetAttributeValue( AttributeKey.CertificateFile ).AsGuid() );
            if ( binaryFile == null )
            {
                return "";
            }
            var bindaryFileData = binaryFile.DatabaseData;
            var binaryData = bindaryFileData.Content;

            X509Certificate2 signingCert = new X509Certificate2( binaryData, GetAttributeValue( AttributeKey.CertificatePassword ), X509KeyStorageFlags.Exportable );

            var attributeStatements = new Dictionary<string, string> {
                {"FirstName", CurrentPerson.FirstName },
                { "LastName", CurrentPerson.LastName },
                { "DateOfBirth", CurrentPerson.BirthDate.Value.ToString( "o" )},
                { "Gender", CurrentPerson.Gender.ToString() },
                { "Email", CurrentPerson.Email },
            };

            var response = SAML20Assertion.CreateSAML20Response(
                "Southeast Christian Church",
                60 * 24 * GetAttributeValue( AttributeKey.DaysValid ).AsInteger(),
                "WellRight",
                CurrentPerson.Email,
                GetAttributeValue( AttributeKey.RequestUrl ),
                attributeStatements,
                signingCert
                );

            return response;
        }

        private void DisplayData( string response )
        {
            byte[] data = System.Convert.FromBase64String( response );
            var xml = System.Text.ASCIIEncoding.ASCII.GetString( data );

            xml = FormatXML( xml );


            ltDebug.Text = "If you had not been an administrator you would have had the following code posted to " + GetAttributeValue( AttributeKey.PostUrl ) + " <pre>" + xml.EncodeHtml() + "</pre>";
        }

        private string FormatXML( string xml )
        {
            string result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter( mStream, Encoding.Unicode );
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml( xml );

                writer.Formatting = Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo( writer );
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader( mStream );

                // Extract the text from the StreamReader.
                string formattedXml = sReader.ReadToEnd();

                result = formattedXml;
            }
            catch ( XmlException )
            {
                // Handle the exception
            }

            mStream.Close();
            writer.Close();

            return result;
        }

        private void Redirect( string response )
        {
            Response.Clear();
            var sb = new System.Text.StringBuilder();
            sb.Append( "<html>" );
            sb.AppendFormat( "<body onload='document.forms[0].submit()'>" );
            sb.AppendFormat( "<form action='{0}' method='post'>", GetAttributeValue( AttributeKey.PostUrl ) );
            sb.AppendFormat( "<input type='hidden' name='SAMLResponse' value='{0}'>", response );
            sb.Append( "</form>" );
            sb.Append( "</body>" );
            sb.Append( "</html>" );
            Response.Write( sb.ToString() );
            Response.End();
        }

        #endregion


        protected void btnPost_Click( object sender, EventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string> { { "Continue", "true" } } );
        }
    }
}