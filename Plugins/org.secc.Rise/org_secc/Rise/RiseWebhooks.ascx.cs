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
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using org.secc.Rise;
using Rock;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Rise
{

    [DisplayName( "Rise Webhooks" )]
    [Category( "SECC > Rise" )]
    [Description( "Block for displaying and editing webhooks." )]
    public partial class RiseWebhooks : RockBlock
    {

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            gList.Actions.ShowAdd = true;
            gList.Actions.AddClick += Actions_AddClick;
        }

        private void Actions_AddClick( object sender, EventArgs e )
        {
            mdAddWebhook.Show();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindDropDowns();
                BindGrid();
                PrepopulateUrl();
            }
        }



        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            //Async code does not work in the webforms main thread.
            var task = Task.Run( () =>
            {
                RiseClient riseClient = new RiseClient();
                var webhooks = riseClient.GetWebhooks().ToList();
                gList.DataSource = webhooks;
            } );
            task.Wait( 1000 * 10 );

            gList.DataBind();
        }

        private void BindDropDowns()
        {
            var eventTypes = org.secc.Rise.Utilities.Constants.WEBHOOK_EVENTS
                .ToDictionary( e => e, e => e.Humanize() );
            ddlEventType.DataSource = eventTypes;
            ddlEventType.DataBind();
        }

        #endregion

        protected void btnDelete_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var id = ( string ) e.RowKeyValue;
            var task = Task.Run( () =>
            {
                RiseClient riseClient = new RiseClient();
                riseClient.DeleteWebhook( id );
            } );
            task.Wait( 1000 * 10 );

            BindGrid();
        }

        protected void mdAddWebhook_SaveClick( object sender, EventArgs e )
        {
            var task = Task.Run( () =>
            {
                RiseClient riseClient = new RiseClient();
                riseClient.CreateWebhook( tbUrl.Text, new List<string> { ddlEventType.SelectedValue } );
            } );
            task.Wait( 1000 * 10 );
            mdAddWebhook.Hide();
            BindGrid();
        }

        protected void ddlEventType_SelectedIndexChanged( object sender, EventArgs e )
        {
            PrepopulateUrl();
        }

        private void PrepopulateUrl()
        {
            var eventType = ddlEventType.SelectedValue.Split( '.' ); // <-- looks like a face
            var externalUrl = GlobalAttributesCache.Value( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
            tbUrl.Text = string.Format( "{0}api/lms/rise/{1}/{2}", externalUrl, eventType[0], eventType[1] );
        }
    }
}