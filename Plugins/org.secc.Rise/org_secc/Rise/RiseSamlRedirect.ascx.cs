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
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Xml;
using Microsoft.AspNet.SignalR;
using org.secc.Rise;
using org.secc.Rise.Utilities;
using org.secc.Security.SAML2;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Rise
{
    [DisplayName( "Rise SAML Redirect" )]
    [Category( "SECC > Rise" )]
    [Description( "Posts the user back to Rise with SAML login" )]

    [TextField( "SAML Request Url",
        Description = "The request url to put in the SAML Assertion",
        Key = AttributeKey.RequestUrl,
        DefaultValue = "",
        Order = 0 )]

    [TextField( "Audience",
        Description = "The request url to put in the SAML Assertion",
        Key = AttributeKey.Audience,
        DefaultValue = "",
        Order = 1 )]

    [TextField( "Post Url",
        Description = "The url to post the SAML response to.",
        Key = AttributeKey.PostUrl,
        DefaultValue = "",
        Order = 2 )]

    [TextField( "Issuer",
        Description = "The assertion issuer, usually a url",
        Key = AttributeKey.Issuer,
        DefaultValue = "https://southeastchristian.org/account/login/ssologin",
        Order = 3 )]

    [FileField( Rock.SystemGuid.BinaryFiletype.DEFAULT,
        "Certificate",
        "Certificate in pfx format to encryt the data with.",
        key: AttributeKey.CertificateFile,
        Order = 4 )]

    [TextField( "Certificate Password",
        Description = "The password for the certificate",
        Key = AttributeKey.CertificatePassword,
        Order = 5 )]


    [IntegerField( "Days Valid",
        Description = "The number of days the assertion is valid.",
        Key = AttributeKey.DaysValid,
        DefaultValue = "30",
        Order = 6 )]

    [KeyValueListField( "Attribute Statements",
        Description = "A set of attributes to include in the assertion.",
        Key = AttributeKey.AttributeStatements,
        KeyPrompt = "Attribute Key",
        ValuePrompt = "Lava",
        DefaultValue = "FirstName^{{ CurrentPerson.FirstName }}|LastName^{{ CurrentPerson.LastName }}|Email^{{ CurrentPerson.Email }}",
        Order = 7 )]

    [BooleanField( "Show Debug",
        Description = "Should administrators be shown a debug menu before being forwarded to Rise?",
        Key = AttributeKey.ShowDebug,
        DefaultValue = "True",
        Order = 8 )]
    public partial class RiseSamlRedirect : Rock.Web.UI.RockBlock
    {

        internal static class AttributeKey
        {
            internal const string RequestUrl = "RequestUrl";
            internal const string PostUrl = "PostUrl";
            internal const string CertificateFile = "CertificateFile";
            internal const string CertificatePassword = "CertificatePassword";
            internal const string DaysValid = "DaysValid";
            internal const string ShowDebug = "ShowDebug";
            internal const string Issuer = "Issuer";
            internal const string AttributeStatements = "AttributeStatements";
            internal const string Audience = "Audience";
        }

        internal static class PageParameterKeys
        {
            internal const string Continue = "Continue";
            internal const string BackgroundReqest = "BackgroundRequest";
        }

        /// <summary>
        /// This holds the reference to the RockMessageHub SignalR Hub context.
        /// </summary>
        private IHubContext _hubContext = GlobalHost.ConnectionManager.GetHubContext<RockMessageHub>();

        /// <summary>
        /// Gets the signal r notification key.
        /// </summary>
        /// <value>
        /// The signal r notification key.
        /// </value>
        public string SignalRNotificationKey
        {
            get
            {
                return string.Format( "RiseSAMLRedirect_BlockId:{0}_SessionId:{1}", this.BlockId, Session.SessionID );
            }
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
            if ( Context.Request.Form.Count > 0
                && Context.Request.Form["SAMLRequest"] != null
                && Context.Request.Form["Relay"] == null )
            {
                RelayRedirect();
                return;
            }
            // Store the Relay State and the SAML Request in the Session
            if ( Context.Request.Form.Count > 0
                && Context.Request.Form["SAMLRequest"] != null
                && Context.Request.Form["RelayState"] != null
                && Context.Request.Form["Relay"] != null )
            {
                Session.Add( string.Format( "RiseSAMLRedirect_BlockId:{0}_SAMLRequest", this.BlockId ), Context.Request.Form["SAMLRequest"] );
                Session.Add( string.Format( "RiseSAMLRedirect_BlockId:{0}_RelayState", this.BlockId ), Context.Request.Form["RelayState"] );
            }

            if ( CurrentPerson == null )
            {
                var site = RockPage.Layout.Site;
                if ( site.LoginPageId.HasValue )
                {
                    site.RedirectToLoginPage( true );
                }
                else
                {
                    FormsAuthentication.RedirectToLoginPage();
                }
                return;
            }

            if ( !Page.IsPostBack )
            {

                if ( CurrentPerson.Email.IsNullOrWhiteSpace() )
                {
                    //The user does not have an email address and we cannot continue
                    return;
                }

                CurrentPerson.LoadAttributes();

                RedirectToResponse();
            }
        }


        private void RedirectToResponse()
        {
            var response = GetResponse();

            if ( UserCanAdministrate
                && !PageParameter( PageParameterKeys.Continue ).AsBoolean()
                && !PageParameter( PageParameterKeys.BackgroundReqest ).AsBoolean()
                && GetAttributeValue( AttributeKey.ShowDebug ).AsBoolean() == true )
            {
                DisplayData( response );
            }
            else
            {
                Redirect( response );
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

            var attributeStatements = new Dictionary<string, string>();

            var statements = GetAttributeValue( AttributeKey.AttributeStatements ).ToKeyValuePairList();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );

            foreach ( var statement in statements )
            {
                var value = statement.Value.ToString().ResolveMergeFields( mergeFields, CurrentPerson );
                attributeStatements[statement.Key] = value;
            }


            var response = SAML20Assertion.CreateSAML20Response(
                GetAttributeValue( AttributeKey.Issuer ),
                60 * 24 * GetAttributeValue( AttributeKey.DaysValid ).AsInteger(),
                GetAttributeValue( AttributeKey.Audience ),
                CurrentPerson.Email,
                GetAttributeValue( AttributeKey.RequestUrl ),
                attributeStatements,
                signingCert
                );

            return response;
        }

        private void DisplayData( string response )
        {
            pnlDebug.Visible = true;
            byte[] data = System.Convert.FromBase64String( response );
            var xml = System.Text.ASCIIEncoding.ASCII.GetString( data );

            if ( xml.IsNotNullOrWhiteSpace() )
            {
                xml = FormatXML( xml );
            }

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
                // Handle the exception . . . Or don't ¯\_(ツ)_/¯
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
            if ( Session[string.Format( "RiseSAMLRedirect_BlockId:{0}_RelayState", this.BlockId )] != null )
            {
                sb.AppendFormat( "<input type='hidden' name='RelayState' value='{0}'>", Session[string.Format( "RiseSAMLRedirect_BlockId:{0}_RelayState", this.BlockId )] );
            }
            sb.Append( "</form>" );
            sb.Append( "</body>" );
            sb.Append( "</html>" );
            Response.Write( sb.ToString() );
            Response.End();
        }


        private void RelayRedirect()
        {
            Response.Clear();
            var sb = new System.Text.StringBuilder();
            sb.Append( "<html>" );
            sb.Append( "<body onload='document.forms[0].submit()'>" );
            sb.Append( "<form method='post'>" );
            sb.Append( "<input type='hidden' name='Relay' value='True'>" );
            foreach ( string key in Context.Request.Form.Keys )
            {
                sb.AppendFormat( "<input type='hidden' name='{0}' value='{1}'>", key, Context.Request.Form[key] );
            }
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