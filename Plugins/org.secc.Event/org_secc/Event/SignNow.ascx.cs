using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using Rock;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using System.Linq;
using org.secc.PersonMatch;
using System.Text.RegularExpressions;

namespace RockWeb.Plugins.org_secc.Event
{

    public partial class SignNow : System.Web.UI.UserControl
    {

        /// <summary>
        /// Gets or sets the digital signature component.
        /// </summary>
        /// <value>
        /// The digital signature component.
        /// </value>
        public DigitalSignatureComponent DigitalSignatureComponent { get; set; }

        /// <summary>
        /// Gets or sets the selected registration instance
        /// </summary>
        /// <value>
        /// The state of the registration instance.
        /// </value>
        public RegistrationInstance RegistrationInstanceState { get; set; }

        /// <summary>
        /// Gets or sets the index of the current registrant.
        /// </summary>
        /// <value>
        /// The index of the current registrant.
        /// </value>
        public int CurrentRegistrantIndex { get; set; }

        public RegistrationInfo RegistrationState { get; set; }

        private bool RedirectFlag { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string documentId = Request.QueryString["document_id"];
            string registrationKey = Request.QueryString["registration_key"];

            if ( !IsPostBack && !string.IsNullOrEmpty( registrationKey ) && Session[registrationKey] != null )
            {
                //Sometimes on iOS SignNow does not return a document id.
                //Why?
                //I don't know.
                //The saving grace here is that the document id is in the referer url and we can parse that to get the data!
                if ( string.IsNullOrEmpty( documentId ) )
                {
                    //Without a referer this doesn't work
                    if ( Request.UrlReferrer == null )
                    {
                        return;
                    }

                    //Hexideximal string after "document/"
                    Regex rgx = new Regex( "(?<=document\\/)[0-9a-f]*" );

                    var documentMatch = rgx.Match( Request.UrlReferrer.ToString() );
                    if ( documentMatch.Captures.Count > 0 )
                    {
                        documentId = documentMatch.Value;
                    }
                    else
                    {
                        return;
                    }
                }
                // We need to strip the extra parameters
                var qs = HttpUtility.ParseQueryString( Request.Url.Query );
                qs.Remove( "registration_key" );
                qs.Remove( "document_id" );
                // Put the URL back together again
                var newUri = string.Format( "{0}{1}{2}", Request.Url.AbsolutePath, qs.Count > 0 ? "?" : "", qs.ToString() );

                // Clear the response and build a new HTML payload with an automatic postback
                Response.Clear();

                StringBuilder sb = new StringBuilder();
                sb.Append( "<html>" );
                sb.AppendFormat( @"<body onload='document.forms[""form""].submit()'>" );
                sb.AppendFormat( "<form name='form' method='post' action='" + newUri + "'>" );

                // Loop through the viewstate params and re-instate them
                Dictionary<string, string> state = ( Dictionary<string, string> ) Session[registrationKey];
                foreach ( var stateEntry in state )
                {
                    sb.AppendFormat( "<input type=\"hidden\" name=\"" + stateEntry.Key + "\" id=\"" + stateEntry.Value + "\" value=\"" + stateEntry.Value + "\">" );
                }
                sb.AppendFormat( @"<input type=""hidden"" name=""document_id"" value=""" + documentId + @""">" );
                sb.AppendFormat( @"<input type=""hidden"" name=""registration_key"" value=""" + registrationKey + @""">" );
                sb.AppendFormat( @"<input type=""hidden"" name=""__EVENTARGUMENT"" id=""__EVENTARGUMENT"" value="""">" );
                sb.AppendFormat( @"<input type=""hidden"" name=""__VIEWSTATE"" id=""__VIEWSTATE"" value=""""> " );

                sb.Append( "</form>" );
                sb.Append( "</body>" );
                sb.Append( "</html>" );


                Response.Write( sb.ToString() );

                Response.End();
            }
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            string documentId = Request.QueryString["document_id"];
            string registrationKey = Request.QueryString["registration_key"];

            if ( IsPostBack && RegistrationState != null && !string.IsNullOrEmpty( documentId ) && !string.IsNullOrEmpty( registrationKey ) && Session[registrationKey] != null )
            {
                Session[registrationKey] = null;
            }

        }

        /// <summary>
        /// Load the SignNow control.
        /// </summary>
        /// <param name="currentRegistrantIndex">The current registrant index</param>
        /// <param name="registrationState">The registration state from RegistrationEntry</param>
        /// <param name="registrationInstanceState">The registration instance</param>
        /// <param name="digitalSignatureComponent">The digital signature component</param>
        /// <returns></returns>
        public bool Load( int currentRegistrantIndex, RegistrationInfo registrationState, RegistrationInstance registrationInstanceState )
        {
            CurrentRegistrantIndex = currentRegistrantIndex;
            RegistrationState = registrationState;
            RegistrationInstanceState = registrationInstanceState;

            var provider = DigitalSignatureContainer.GetComponent( RegistrationInstanceState.RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderEntityType.Name );
            if ( provider != null && provider.IsActive )
            {
                DigitalSignatureComponent = provider;
            }
            var registrant = RegistrationState.Registrants[CurrentRegistrantIndex];

            string firstName = RegistrationInstanceState.RegistrationTemplate.RegistrantTerm;
            if ( RegistrationState != null && RegistrationState.RegistrantCount > CurrentRegistrantIndex )
            {
                firstName = registrant.GetFirstName( RegistrationInstanceState.RegistrationTemplate );
                string lastName = registrant.GetLastName( RegistrationInstanceState.RegistrationTemplate );
                string email = registrant.GetEmail( RegistrationInstanceState.RegistrationTemplate );

                object dateOfBirthObj = registrant.GetPersonFieldValue( RegistrationInstanceState.RegistrationTemplate, RegistrationPersonFieldType.Birthdate );
                DateTime? dateOfBirth = null;
                if ( dateOfBirthObj != null && dateOfBirthObj is DateTime? )
                {
                    dateOfBirth = dateOfBirthObj as DateTime?;
                }

                // If we have a single person, then we check to see if we already have the document
                var rockContext = new Rock.Data.RockContext();
                PersonService personService = new PersonService( rockContext );
                var possiblePersons = personService.GetByMatch( firstName, lastName, dateOfBirth, email: email );
                if ( possiblePersons.Count() == 1 )
                {
                    var person = possiblePersons.First();
                    var personAliasIds = person.Aliases.Select( pa => pa.Id ).ToList();
                    var signatureDocumentService = new SignatureDocumentService( rockContext );
                    var validSignatureDocuments = signatureDocumentService.Queryable().Where( sd =>
                        personAliasIds.Contains( sd.AppliesToPersonAliasId.Value ) &&
                        sd.SignatureDocumentTemplateId == RegistrationInstanceState.RegistrationTemplate.RequiredSignatureDocumentTemplate.Id &&
                        sd.Status == SignatureDocumentStatus.Signed
                        ).OrderBy( sd => sd.CreatedDateTime );
                    if ( validSignatureDocuments.Any() )
                    {
                        registrant.SignatureDocumentId = validSignatureDocuments.First().Id;
                        registrant.SignatureDocumentKey = validSignatureDocuments.First().DocumentKey;
                        registrant.SignatureDocumentLastSent = validSignatureDocuments.First().LastInviteDate;
                        return true;
                    }
                }
            }


            nbDigitalSignature.Heading = "Signature Required";
            nbDigitalSignature.Text = string.Format(
                "This {0} requires that you sign a {1} for each registrant, please click the button below and then follow the prompts to digitally sign this document for {2}.  This will open the signing request within our digital signature provider's website.  When you have successfully signed this document, you will be returned to this page which will automatically proceed to the next step of your registration.",
                RegistrationInstanceState.RegistrationTemplate.RegistrationTerm,
                RegistrationInstanceState.RegistrationTemplate.RequiredSignatureDocumentTemplate.Name,
                firstName );

            var errors = new List<string>();
            string inviteLink = DigitalSignatureComponent.GetInviteLink( RegistrationInstanceState.RegistrationTemplate.RequiredSignatureDocumentTemplate.ProviderTemplateKey, out errors );
            if ( !string.IsNullOrWhiteSpace( inviteLink ) )
            {
                var key = Guid.NewGuid();

                string returnUrl = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() +
                    ResolveUrl( Request.RawUrl ).TrimStart( '/' );

                // We need to make sure if someone click's back that we don't end up with extra parameters
                var uri = new Uri( returnUrl );
                var qs = HttpUtility.ParseQueryString( uri.Query );
                qs.Set( "registration_key", key.ToString() );
                qs.Remove( "document_id" );
                // Put the URL back together again
                var uriBuilder = new UriBuilder( uri );
                uriBuilder.Query = qs.ToString();

                // Snap off a copy of the viewstate and set the button URL
                hfRegistrantKey.Value = key.ToString();

                hfRequiredDocumentLinkUrl.Value = string.Format( "{0}?redirect_uri={1}", inviteLink, uriBuilder.ToString().UrlEncode() );
                btnRequiredDocument.Visible = true;
            }
            else
            {
                nbDigitalSignature.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
                nbDigitalSignature.Heading = "Digital Signature Error";
                nbDigitalSignature.Text = string.Format( "An Error Occurred Trying to Get Document Link... <ul><li>{0}</li></ul>", errors.AsDelimited( "</li><li>" ) );
            }
            return false;
        }

        protected override void Render( HtmlTextWriter writer )
        {
            if ( RedirectFlag )
            {
                var fields = ScriptManager.GetCurrent( Page ).GetRegisteredHiddenFields();
                Dictionary<string, string> state = new Dictionary<string, string>();

                foreach ( var field in fields )
                {
                    state.Add( field.Name, field.InitialValue );
                }
                Session[hfRegistrantKey.Value] = state;
                HttpContext.Current.Response.Redirect( hfRequiredDocumentLinkUrl.Value );
            }
            base.Render( writer );
        }

        protected void btnRequiredDocument_Click( object sender, EventArgs e )
        {

            if ( !String.IsNullOrWhiteSpace( hfRegistrantKey.Value ) )
            {
                RedirectFlag = true;
            }
        }


    }
}