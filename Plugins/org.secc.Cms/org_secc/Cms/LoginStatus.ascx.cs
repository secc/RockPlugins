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
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.Security;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.CMS
{
    /// <summary>
    /// Displays currently logged in user's name along with options to log in, log out, or manage account.
    /// </summary>
    [DisplayName( "Login Status" )]
    [Category( "SECC > Security" )]
    [Description( "Displays the currently logged in user's name along with options to log in, log out, or manage account." )]

    [LinkedPage( "My Account Page", "Page for user to manage their account (if blank will use 'MyAccount' page route)", false )]
    [LinkedPage( "My Profile Page", "Page for user to view their person profile (if blank option will not be displayed)", false )]
    [LinkedPage( "My Settings Page", "Page for user to view their settings (if blank option will not be displayed)", false )]
    [LinkedPage( "My Dashboard Page", "Page for user to view their dashboard (if blank option will not be displayed)", false )]
    [KeyValueListField( "Logged In Page List", "List of pages to show in the dropdown when the user is logged in. The link field takes Lava with the CurrentPerson merge fields. Place the text 'divider' in the title field to add a divider.", false, "", "Title", "Link" )]

    [BooleanField( "Enable Notifications", "Display a notification of new workflows or connections." )]

    [LinkedPage( "Logout Page", "Page for logging out of Rock", required: false )]

    public partial class LoginStatus : Rock.Web.UI.RockBlock
    {
        private const string LOG_OUT = "Log Out";
        private const string LOG_IN = "Log In";
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            var myAccountUrl = LinkedPageUrl( "MyAccountPage" );

            if ( !string.IsNullOrWhiteSpace( myAccountUrl ) )
            {
                hlMyAccount.NavigateUrl = myAccountUrl;
            }
            else
            {
                phMyAccount.Visible = false;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var currentPerson = CurrentPerson;
            if ( currentPerson != null )
            {
                //Reset our clock when we look at our dashboard.
                if ( PageCache.Guid == GetAttributeValue( "MyDashboardPage" ).AsGuid() )
                {
                    SetUserPreference( "LastViewedDashboard", Rock.RockDateTime.Now.ToString() );
                }

                phHello.Visible = true;
                lHello.Text = string.Format( "<span>Hello {0}</span>", currentPerson.NickName );

                var currentUser = CurrentUser;
                if ( currentUser == null )
                {
                    phMyAccount.Visible = false;
                    phMySettings.Visible = false;
                }

                var queryParams = new Dictionary<string, string>();
                queryParams.Add( "PersonId", currentPerson.Id.ToString() );

                var myProfileUrl = LinkedPageUrl( "MyProfilePage", queryParams );
                if ( !string.IsNullOrWhiteSpace( myProfileUrl ) )
                {
                    hlMyProfile.NavigateUrl = myProfileUrl;
                }
                else
                {
                    phMyProfile.Visible = false;
                }

                var mySettingsUrl = LinkedPageUrl( "MySettingsPage", null );
                if ( !string.IsNullOrWhiteSpace( mySettingsUrl ) )
                {
                    hlMySettings.NavigateUrl = mySettingsUrl;
                }
                else
                {
                    phMySettings.Visible = false;
                }

                if ( GetAttributeValue( "EnableNotifications" ).AsBoolean() )
                {
                    int notificationCount = GetNotificationCount();
                    if ( notificationCount > 0 )
                    {
                        var notificationText = string.Format( " <div class='notification'>{0}</div>", notificationCount );
                        lNotifications.Text = notificationText;
                        hlMyDashboard.Text += notificationText;
                    }
                }

                var myDashboardUrl = LinkedPageUrl( "MyDashboardPage", null );
                if ( !string.IsNullOrWhiteSpace( myDashboardUrl ) )
                {
                    hlMyDashboard.NavigateUrl = myDashboardUrl;
                }
                else
                {
                    phMyDashboard.Visible = false;
                }

                lbLoginLogout.Text = LOG_OUT;

                divProfilePhoto.Attributes.Add( "style", String.Format( "background-image: url('{0}');", Rock.Model.Person.GetPersonPhotoUrl( currentPerson, 200, 200 ) ) );

                var navPagesString = GetAttributeValue( "LoggedInPageList" );

                if ( !string.IsNullOrWhiteSpace( navPagesString ) )
                {
                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "CurrentPerson", CurrentPerson );

                    navPagesString = navPagesString.TrimEnd( '|' );
                    var navPages = navPagesString.Split( '|' )
                                    .Select( s => s.Split( '^' ) )
                                    .Select( p => new { Title = p[0], Link = p[1] } );

                    StringBuilder sbPageMarkup = new StringBuilder();
                    foreach ( var page in navPages )
                    {
                        if ( page.Title.Trim() == "divider" )
                        {
                            sbPageMarkup.Append( "<li class='divider'></li>" );
                        }
                        else
                        {
                            sbPageMarkup.Append( string.Format( "<li><a href='{0}'>{1}</a>", Page.ResolveUrl( page.Link.ResolveMergeFields( mergeFields ) ), page.Title ) );
                        }
                    }

                    lDropdownItems.Text = sbPageMarkup.ToString();
                }

            }
            else
            {
                phHello.Visible = false;
                phMyAccount.Visible = false;
                phMyProfile.Visible = false;
                phMySettings.Visible = false;
                lbLoginLogout.Text = LOG_IN;

                liDropdown.Visible = false;
                liLogin.Visible = true;
            }

            hfActionType.Value = lbLoginLogout.Text;
        }

        private int GetNotificationCount()
        {
            var lastChecked = GetUserPreference( "LastViewedDashboard" ).AsDateTime();
            if ( !lastChecked.HasValue )
            {
                lastChecked = RockDateTime.Now.AddMonths( -3 );
            }
            RockContext rockContext = new RockContext();

            //Any workflow assigned to the person where we have active forms.
            var workflowCount = new WorkflowActionService( rockContext ).GetActiveForms( CurrentPerson ).Where( a => a.CreatedDateTime > lastChecked.Value ).DistinctBy( a => a.Activity.WorkflowId ).Count();

            //Connections - If a new connecion was made, a connection was just transfered, or a future followup just came up
            DateTime midnightToday = RockDateTime.Today.AddDays( 1 );
            var connectionRequests = new ConnectionRequestService( rockContext ).Queryable()
                .Where( r => r.ConnectorPersonAlias != null && r.ConnectorPersonAlias.PersonId == CurrentPersonId )
                .Where( r => ( r.ConnectionState == ConnectionState.Active && ( r.CreatedDateTime > lastChecked.Value || r.ConnectionRequestActivities.Where( a => a.CreatedDateTime > lastChecked.Value && a.ConnectionActivityType.Name == "Transferred" ).Any() ) ) ||
                             ( r.ConnectionState == ConnectionState.FutureFollowUp && r.FollowupDate.HasValue && r.FollowupDate.Value < midnightToday ) && r.FollowupDate.Value > lastChecked.Value )
                             .Count();

            return workflowCount + connectionRequests;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbLoginLogout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbLoginLogout_Click( object sender, EventArgs e )
        {
            string action = hfActionType.Value;
            if ( action == LOG_IN )
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
            }
            else
            {
                var logoutPage = GetAttributeValue( "LogoutPage" );
                if ( logoutPage.IsNotNullOrWhiteSpace() )
                {
                    NavigateToLinkedPage( "LogoutPage" );
                    return;
                }


                if ( CurrentUser != null )
                {
                    var transaction = new Rock.Tasks.UpdateUserLastActivity.Message
                    {
                        UserId = CurrentUser.Id,
                        LastActivityDate = RockDateTime.Now,
                        IsOnline = false
                    };
                    transaction.SendIfNeeded();
                }

                Authorization.SignOut();

                // After logging out check to see if an anonymous user is allowed to view the current page.  If so
                // redirect back to the current page, otherwise redirect to the site's default page
                var currentPage = Rock.Web.Cache.PageCache.Get( RockPage.PageId );
                if ( currentPage != null && currentPage.IsAuthorized( Authorization.VIEW, null ) )
                {
                    string url = CurrentPageReference.BuildUrl( true );
                    Response.Redirect( url );
                    Context.ApplicationInstance.CompleteRequest();
                }
                else
                {
                    RockPage.Layout.Site.RedirectToDefaultPage();
                }
            }
        }

        #endregion
    }
}
