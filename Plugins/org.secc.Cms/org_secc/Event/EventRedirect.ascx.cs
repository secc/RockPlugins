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
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Store;
using System.Text;
using Rock.Security;
using System.Text.RegularExpressions;

namespace RockWeb.Plugins.org_secc.Event
{
    /// <summary>
    /// Renders a particular calendar using Lava.
    /// </summary>
    [DisplayName( "Event Redirect" )]
    [Category( "SECC > Event" )]
    [Description( "Interprets slugs and forwards as needed" )]
    [TextField( "Base Route", "Route for the redirect to start from.", true, "upcomingevents" )]


    public partial class EventRedirect : Rock.Web.UI.RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            var baseRoute = GetAttributeValue( "BaseRoute" ).Trim( '/' );
            var path = Request.Url.PathAndQuery;
            var slug = path.Replace( "/" + baseRoute + "/", "" );
            var campus = Regex.Match( slug, "(?<=events\\-)([a-zA-Z0-9\\-]*)" ).Value;
            var ministry = Regex.Match( slug, "([a-zA-Z0-9\\-]*)(?=\\-events)" ).Value;

            var redirect = "";
            if ( !string.IsNullOrWhiteSpace( campus ) && !string.IsNullOrWhiteSpace( ministry ) )
            {
                redirect = string.Format( "/{0}/{1}/{2}", baseRoute, campus, ministry );
            }
            else if ( !string.IsNullOrWhiteSpace( campus ) )
            {
                redirect = string.Format( "/{0}/{1}/{2}", baseRoute, campus, "all" );
            }
            else if ( !string.IsNullOrWhiteSpace( ministry ) )
            {
                redirect = string.Format( "/{0}/{1}/{2}", baseRoute, "southeast", ministry );
            }
            else
            {
                return;
            }
            if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
            {
                nbRedirect.Text = string.Format( "If you did not have Administrate permissions on this block, you would have been redirected to here: <a href='{0}'>{0}</a>.", redirect );
            }
            else
            {
                Response.Redirect( redirect );
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }
    }
}
