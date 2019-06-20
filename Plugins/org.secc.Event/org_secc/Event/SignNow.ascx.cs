using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Event
{

    public partial class SignNow : System.Web.UI.UserControl
    {
        
        public string RegistrationKey { get; set; }

        /// <summary>
        /// Gets or sets the index of the current registrant.
        /// </summary>
        /// <value>
        /// The index of the current registrant.
        /// </value>
        public int CurrentRegistrantIndex { get; set; }
        public RegistrationInfo RegistrationState { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string documentId = Request.QueryString["document_id"];
            string registrationKey = Request.QueryString["registration_key"];

            if (!IsPostBack && !string.IsNullOrEmpty( documentId ) && !string.IsNullOrEmpty( registrationKey ) && Session[registrationKey] != null)
            {
                // We need to strip the extra parameters
                var qs = HttpUtility.ParseQueryString( Request.Url.Query );
                qs.Remove( "registration_key" );
                qs.Remove( "document_id" );
                // Put the URL back together again
                var newUri = string.Format( "{0}{1}{2}", Request.Url.AbsolutePath, qs.Count>0?"?":"", qs.ToString() );

                // Clear the response and build a new HTML payload with an automatic postback
                Response.Clear();
                
                StringBuilder sb = new StringBuilder();
                sb.Append( "<html>" );
                sb.AppendFormat( @"<body onload='document.forms[""form""].submit()'>" );
                sb.AppendFormat( "<form name='form' method='post' action='" + newUri + "'>" );

                // Loop through the viewstate params and re-instate them
                Dictionary<string, string> state = (Dictionary<string, string>)Session[registrationKey];
                foreach (var stateEntry in state)
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

            if( IsPostBack && RegistrationState != null && !string.IsNullOrEmpty( documentId ) && !string.IsNullOrEmpty( registrationKey ) && Session[registrationKey] != null)
            {
                //RegistrationState.Registrants[CurrentRegistrantIndex].SignatureDocumentKey = documentId;
                //RegistrationState.Registrants[CurrentRegistrantIndex].SignatureDocumentLastSent = RockDateTime.Now;

                Session[registrationKey] = null;
            }
        }

        protected override void Render( HtmlTextWriter writer )
        {
            if (!String.IsNullOrWhiteSpace( RegistrationKey ))
            {
                var fields = ScriptManager.GetCurrent( Page ).GetRegisteredHiddenFields();
                Dictionary<string, string> state = new Dictionary<string, string>();

                foreach (var field in fields)
                {
                    state.Add( field.Name, field.InitialValue );
                }
                Session[RegistrationKey] = state;
            }
        }

    }
}