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
using Rock.Model;
using System.Web.Routing;
using System.Linq;
using Rock;

namespace RockWeb.Plugins.org_secc.CMS
{
    [DisplayName( "Register Routes" )]
    [Category("SECC > CMS")]
    [Description( "Updates and registers routes if those routes have changed." )]
    public partial class RegisterRoutes : Rock.Web.UI.RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            RouteCollection routes = RouteTable.Routes;

            PageRouteService pageRouteService = new PageRouteService( new Rock.Data.RockContext() );
            var pageRoutes = pageRouteService.Queryable().ToList();


            // Check to see if we have any missing routes.  If so, simply run reregister.
            foreach ( var pageRoute in pageRoutes )
            {
                var route = routes.OfType<Route>().Where( a => a.Url == pageRoute.Route && a.PageIds().Contains( pageRoute.PageId ) ).FirstOrDefault();

                if ( route == null )
                {
                    nbNotification.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                    nbNotification.Text = "Routes were out-of-date.  Running reregister routes.";
                    var routesToDelete = routes.OfType<Route>().Where( r => r.RouteHandler is Rock.Web.RockRouteHandler ).ToList();
                    foreach ( Route oldRoute in routesToDelete )
                    {
                        routes.Remove( oldRoute );
                    }
                    Rock.Web.RockRouteHandler.ReregisterRoutes();
                    break;
                }
            }
        }
        #endregion
        
    }
}