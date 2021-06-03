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
using System;
using System.ComponentModel;
using System.Web;
using Microsoft.AspNet.Identity;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Security
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "Auth Logout" )]
    [Category( "SECC > OAuth" )]
    [Description( "Provides a chained way to logout of this system and the auth provider" )]

    [TextField( "Auth Provider Logout URL",
        Description = "The url to the auth provider to tell it to logout",
        IsRequired = false,
        Key =AttributeKeys.ProviderUrl)]

    [TextField( "Return Url",
        Description = "The url to send the user once the logout chain is complete",
        IsRequired = false,
         Key = AttributeKeys.ReturnUrl )]



    public partial class AuthLogout : RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string ProviderUrl = "ProviderUrl";
            internal const string ReturnUrl = "ReturnUrl";
        }

        internal static class PageParameterKeys
        {
            internal const string ReturnUrl = "returnurl";
        }

        #region Base Control Methods

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            string returnUrl = PageParameter( PageParameterKeys.ReturnUrl );
            returnUrl = Server.UrlDecode( returnUrl );

            if ( returnUrl.IsNullOrWhiteSpace() )
            {
                returnUrl = GetAttributeValue( AttributeKeys.ReturnUrl );
            }

            if ( returnUrl.IsNullOrWhiteSpace() )
            {
                returnUrl = "/";
            }



            Authorization.SignOut();
            var authentication = HttpContext.Current.GetOwinContext().Authentication;
            authentication.SignOut( "OAuth" );
            authentication.SignOut( DefaultAuthenticationTypes.ApplicationCookie );

            var nextServer = GetAttributeValue( AttributeKeys.ProviderUrl );

            if ( nextServer.IsNotNullOrWhiteSpace() )
            {
                nextServer += "?returnurl=" + Server.UrlEncode( returnUrl );
            }
            else
            {
                nextServer = returnUrl;
            }

            Response.Redirect( nextServer, true );
        }

        #endregion
    }
}