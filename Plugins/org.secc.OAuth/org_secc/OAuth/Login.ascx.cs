﻿// <copyright>
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Claims;
using System.Web;
using System.Web.UI;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth login prompts user for login credentials.
    /// </summary>
    [DisplayName( "OAuth Login" )]
    [Category( "SECC > Security" )]
    [Description( "Prompts user for login credentials during the OAuth Login process." )]

    [LinkedPage( "New Account Page", "Page to navigate to when user selects 'Create New Account' (if blank will use 'NewAccountPage' page route)", false, "", "", 0 )]
    [LinkedPage( "Help Page", "Page to navigate to when user selects 'Help' option (if blank will use 'ForgotUserName' page route)", false, "", "", 1 )]
    [CodeEditorField( "Confirm Caption", "The text (HTML) to display when a user's account needs to be confirmed.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"
Thank-you for logging in, however, we need to confirm the email associated with this account belongs to you. We've sent you an email that contains a link for confirming.  Please click the link in your email to continue.
", "", 2 )]
    [LinkedPage( "Confirmation Page", "Page for user to confirm their account (if blank will use 'ConfirmAccount' page route)", false, "", "", 3 )]
    [SystemCommunicationField( "Confirm Account Template", "Confirm Account Email Template", false, Rock.SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT, "", 4 )]
    [CodeEditorField( "Locked Out Caption", "The text (HTML) to display when a user's account has been locked.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"
Sorry, your account has been locked.  Please contact our office at {{ 'Global' | Attribute:'OrganizationPhone' }} or email {{ 'Global' | Attribute:'OrganizationEmail' }} to resolve this.  Thank-you. 
", "", 5 )]
    [BooleanField( "Hide New Account Option", "Should 'New Account' option be hidden?  For site's that require user to be in a role (Internal Rock Site for example), users shouldn't be able to create their own account.", false, "", 6, "HideNewAccount" )]
    [TextField( "New Account Text", "The text to show on the New Account button.", false, "Register", "", 7, "NewAccountButtonText" )]
    [CodeEditorField( "Prompt Message", "Optional text (HTML) to display above username and password fields.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"", "", 9 )]
    public partial class Login : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            btnNewAccount.Visible = !GetAttributeValue( "HideNewAccount" ).AsBoolean();
            btnNewAccount.Text = this.GetAttributeValue( "NewAccountButtonText" ) ?? "Register";

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentUser != null )
            {
                CreateOAuthIdentity( CurrentUser );

                if ( !string.IsNullOrEmpty( PageParameter( "ReturnUrl" ) ) && IsLocalUrl( PageParameter( "ReturnUrl" ) ) )
                {
                    Response.Redirect( PageParameter( "ReturnUrl" ) );
                }
                else
                {

                }
            }
            var authentication = HttpContext.Current.GetOwinContext().Authentication;



            if ( !Page.IsPostBack )
            {
                lPromptMessage.Text = GetAttributeValue( "PromptMessage" );
                tbUserName.Focus();
            }


            pnlMessage.Visible = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// NOTE: This is the btnLogin for Internal Auth
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnLogin_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                var rockContext = new RockContext();
                var userLoginService = new UserLoginService( rockContext );
                var userLogin = userLoginService.GetByUserName( tbUserName.Text );
                if ( userLogin != null && userLogin.EntityType != null )
                {
                    var component = AuthenticationContainer.GetComponent( userLogin.EntityType.Name );
                    if ( component != null && component.IsActive && !component.RequiresRemoteAuthentication )
                    {
                        if ( component.Authenticate( userLogin, tbPassword.Text ) )
                        {
                            if ( ( userLogin.IsConfirmed ?? true ) && !( userLogin.IsLockedOut ?? false ) )
                            {

                                var authentication = HttpContext.Current.GetOwinContext().Authentication;

                                UserLoginService.UpdateLastLogin( tbUserName.Text );

                                Rock.Security.Authorization.SetAuthCookie( tbUserName.Text, cbRememberMe.Checked, false );

                                CreateOAuthIdentity( userLogin );

                                if ( !string.IsNullOrEmpty( PageParameter( "ReturnUrl" ) ) && IsLocalUrl( PageParameter( "ReturnUrl" ) ) )
                                {
                                    Response.Redirect( PageParameter( "ReturnUrl" ) );
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                var globalMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                                if ( userLogin.IsLockedOut ?? false )
                                {
                                    lLockedOutCaption.Text = GetAttributeValue( "LockedOutCaption" ).ResolveMergeFields( globalMergeFields );

                                    pnlLogin.Visible = false;
                                    pnlLockedOut.Visible = true;
                                }
                                else
                                {
                                    SendConfirmation( userLogin );

                                    lConfirmCaption.Text = GetAttributeValue( "ConfirmCaption" ).ResolveMergeFields( globalMergeFields );

                                    pnlLogin.Visible = false;
                                    pnlConfirmation.Visible = true;
                                }
                            }

                            return;
                        }
                    }
                }
            }

            string helpUrl = string.Empty;

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
                helpUrl = LinkedPageUrl( "HelpPage" );
            }
            else
            {
                helpUrl = ResolveRockUrl( "~/ForgotUserName" );
            }

            DisplayError( string.Format( "Sorry, we couldn't find an account matching that username/password. Can we help you <a href='{0}'>recover your account information</a>?", helpUrl ) );
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnNewAccount_Click( object sender, EventArgs e )
        {
            string returnUrl = Server.UrlEncode( Request.QueryString["returnurl"] );

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "NewAccountPage" ) ) )
            {
                var parms = new Dictionary<string, string>();

                if ( !string.IsNullOrWhiteSpace( returnUrl ) )
                {
                    parms.Add( "returnurl", returnUrl );
                }

                NavigateToLinkedPage( "NewAccountPage", parms );
            }
            else
            {
                string url = "~/NewAccount";

                if ( !string.IsNullOrWhiteSpace( returnUrl ) )
                {
                    url += "?returnurl=" + returnUrl;
                }

                Response.Redirect( url, false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnHelp control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnHelp_Click( object sender, EventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
                NavigateToLinkedPage( "HelpPage" );
            }
            else
            {
                Response.Redirect( "~/ForgotUserName", false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays the error.
        /// </summary>
        /// <param name="message">The message.</param>
        private void DisplayError( string message )
        {
            pnlMessage.Controls.Clear();
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }


        /// <summary>
        /// Sends the confirmation.
        /// </summary>
        /// <param name="userLogin">The user login.</param>
        private void SendConfirmation( UserLogin userLogin )
        {
            string url = LinkedPageUrl( "ConfirmationPage" );
            if ( string.IsNullOrWhiteSpace( url ) )
            {
                url = ResolveRockUrl( "~/ConfirmAccount" );
            }

            var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( null, CurrentPerson );
            mergeObjects.Add( "ConfirmAccountUrl", RootPath + url.TrimStart( new char[] { '/' } ) );

            var personDictionary = userLogin.Person.ToLiquid() as Dictionary<string, object>;
            mergeObjects.Add( "Person", personDictionary );
            mergeObjects.Add( "User", userLogin );

            var recipients = new List<RockMessageRecipient>();
            recipients.Add( new RockEmailMessageRecipient( userLogin.Person, mergeObjects ) );

            var emailMessage = new RockEmailMessage( GetAttributeValue( "ConfirmAccountTemplate" ).AsGuid() );
            emailMessage.SetRecipients( recipients );
            emailMessage.CreateCommunicationRecord = false;
            emailMessage.Send();
        }

        private void CreateOAuthIdentity( UserLogin userLogin )
        {


            var authentication = HttpContext.Current.GetOwinContext().Authentication;

            List<Claim> claims = new List<Claim>
                                {
                                    new Claim(ClaimsIdentity.DefaultNameClaimType, userLogin.UserName),
                                    new Claim("First_Name", userLogin.Person.FirstName),
                                    new Claim("Nick_Name", userLogin.Person.NickName),
                                    new Claim("Last_Name", userLogin.Person.LastName),
                                    //new Claim("Gender", person.Gender),
                                    //new Claim("Birthdate", person.BirthDate == null ? null : ((DateTime)person.BirthDate).ToShortDateString()),
                                    //new Claim("PersonId", person.PersonID.ToString()),  
                                    //new Claim("Primary_Email", person.Emails.FirstOrDefault().Address)

                                };

            authentication.SignIn( new AuthenticationProperties { IsPersistent = cbRememberMe.Checked },
                new ClaimsIdentity( claims.ToArray(), DefaultAuthenticationTypes.ApplicationCookie ) );


        }
        private bool IsLocalUrl( string url )
        {
            if ( string.IsNullOrEmpty( url ) )
            {
                return false;
            }

            Uri absoluteUri;
            if ( Uri.TryCreate( url, UriKind.Absolute, out absoluteUri ) )
            {
                return String.Equals( this.Request.Url.Host, absoluteUri.Host,
                            StringComparison.OrdinalIgnoreCase );
            }
            else
            {
                bool isLocal = !url.StartsWith( "http:", StringComparison.OrdinalIgnoreCase )
                    && !url.StartsWith( "https:", StringComparison.OrdinalIgnoreCase )
                    && Uri.IsWellFormedUriString( url, UriKind.Relative );
                return isLocal;
            }
        }
        #endregion
    }
}
