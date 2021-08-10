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
using System.ComponentModel;
using System.Web;
using Rock;
using Rock.Attribute;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth logout.
    /// </summary>
    [DisplayName( "OAuth Logout" )]
    [Category( "SECC > Security" )]
    [Description( "Logs a user out of Rock/OAuth." )]
    [BooleanField( "Enabled", "Enabled or disabled.  This is helpful for editing the page!", true )]
    public partial class Logout : Rock.Web.UI.RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( GetAttributeValue( "Enabled" ).AsBoolean() )
            {
                var authentication = HttpContext.Current.GetOwinContext().Authentication;
                authentication.SignOut( "OAuth" );
                if ( CurrentUser != null )
                {
                    Response.Redirect( Request.RawUrl + ( Request.RawUrl.Contains( "?" ) ? "&" : "?" ) + "logout=true" );
                }
            }

        }

    }
}
