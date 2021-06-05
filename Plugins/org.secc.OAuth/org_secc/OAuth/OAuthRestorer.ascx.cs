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
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using Newtonsoft.Json;
using org.secc.OAuth;
using org.secc.OAuth.Data;
using org.secc.OAuth.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "OAuth Session Restorer" )]
    [Category( "SECC > OAuth" )]
    [Description( "Restores the OAuth session. Used to enable chaining of authentication" )]

    [LinkedPage( "OAuth Authorization Page",
        Description = "The OAuth authorization page",
        Key = AttributeKeys.AuthorizePage,
        Order = 0 )]

    public partial class OAuthRestorer : RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string AuthorizePage = "AuthorizePage";
        }

        internal static class CookieKeys
        {
            internal const string OAuthQueryString = "OAuthQueryString";
        }

        #region Base Control Methods

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var authCookie = Request.Cookies[CookieKeys.OAuthQueryString];

            if ( authCookie != null )
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>( authCookie.Value );
                NavigateToLinkedPage( AttributeKeys.AuthorizePage, parameters );
            }
        }

        #endregion

    }
}