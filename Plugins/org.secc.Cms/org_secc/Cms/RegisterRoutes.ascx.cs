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
using System.Data.Entity;
using Rock.Lava.Shortcodes;
using DotLiquid;
using Rock.Web.Cache;
using AngleSharp.Css.Values;
using System.Data.Entity.Core.Objects;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

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

                    ReRegisterRoutes();
                    break;
                }
            }

            // Check to see if we have any missing shortcodes
            var outOfDate = RockDateTime.Now.AddMinutes( -30 );
            var sc = new LavaShortcodeService( new Rock.Data.RockContext() ).Queryable().Where( l => l.CreatedDateTime > outOfDate || l.ModifiedDateTime > outOfDate ).ToList();
            if ( sc.Count > 0 )
            {
                nbNotification.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                nbNotification.Text = "Shortcodes were out-of-date.  Running register shortcodes. " + sc.Count;
                 foreach ( var code in sc )
                {
                    // register shortcode
                    if ( code.TagType == TagType.Block )
                    {
                        Template.RegisterShortcode<DynamicShortcodeBlock>( code.TagName );
                    }
                    else
                    {
                        Template.RegisterShortcode<DynamicShortcodeInline>( code.TagName );
                    }
                }

                LavaShortcodeCache.Clear();
            }
            
        }
        protected void ReRegisterRoutes()
        {
            RouteCollection routes = RouteTable.Routes;
            var routesToDelete = routes.OfType<Route>().Where( r => r.RouteHandler is Rock.Web.RockRouteHandler ).ToList();
            foreach ( Route oldRoute in routesToDelete )
            {
                routes.Remove( oldRoute );
            }


            PageRouteService pageRouteService = new PageRouteService( new Rock.Data.RockContext() );

            var routesToInsert = new RouteCollection();

            // Add ignore rule for asp.net ScriptManager files. 
            routesToInsert.Ignore( "{resource}.axd/{*pathInfo}" );

            //Add page routes, order is very important here as IIS takes the first match
            IOrderedEnumerable<PageRoute> pageRoutes = pageRouteService.Queryable().AsNoTracking().ToList().OrderBy( r => r.Route, StringComparer.OrdinalIgnoreCase );

            foreach ( var pageRoute in pageRoutes )
            {
                routesToInsert.AddPageRoute( pageRoute.Route, new Rock.Web.PageAndRouteId { PageId = pageRoute.PageId, RouteId = pageRoute.Id } );
            }

            // Add a default page route
            routesToInsert.Add( new Route( "page/{PageId}", new Rock.Web.RockRouteHandler() ) );

            // Add a default route for when no parameters are passed
            routesToInsert.Add( new Route( "", new Rock.Web.RockRouteHandler() ) );

            // Add a default route for shortlinks
            routesToInsert.Add( new Route( "{shortlink}", new Rock.Web.RockRouteHandler() ) );

            // Insert the list of routes to the beginning of the Routes so that PageRoutes, etc are before OdataRoutes. Even when Re-Registering routes
            // Since we are inserting at 0, reverse the list to they end up in the original order
            foreach ( var pageRoute in routesToInsert.Reverse() )
            {
                routes.Insert( 0, pageRoute );
            }
        }

        #endregion

    }
}