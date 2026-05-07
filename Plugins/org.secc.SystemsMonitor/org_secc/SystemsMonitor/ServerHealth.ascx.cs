// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System;
using System.ComponentModel;
using System.IO;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.SystemsMonitor
{
    [DisplayName( "Server Health" )]
    [Category( "SECC > Systems Monitor" )]
    [Description( "Displays the health status of this Rock web-farm node." )]

    public partial class ServerHealth : RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                RunHealthCheck();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event.
        /// </summary>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            RunHealthCheck();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs all health checks and updates the UI.
        /// </summary>
        private void RunHealthCheck()
        {
            nbMessage.Visible = false;
            pnlDetails.Visible = true;

            lMachineName.Text = Environment.MachineName;

            string maintenanceFile = Server.MapPath( "~/_maintenance.flag" );

            if ( File.Exists( maintenanceFile ) )
            {
                ShowResult( false, "Maintenance Mode: Server is draining connections for deployment." );
                return;
            }

            ShowResult( true, "Healthy" );
        }

        /// <summary>
        /// Displays the health check result in the UI and sets the HTTP status code
        /// so that external probes (e.g. Azure Application Gateway) see 200 or 503.
        /// </summary>
        /// <param name="isHealthy">If <c>true</c>, show a success state; otherwise show a failure state.</param>
        /// <param name="message">The message to display.</param>
        private void ShowResult( bool isHealthy, string message )
        {
            // Set the HTTP status code for health probes.
            Context.Response.StatusCode = isHealthy ? 200 : 503;
            Context.Response.TrySkipIisCustomErrors = true;

            if ( isHealthy )
            {
                lStatusBadge.Text = "<span class='label label-success'>Healthy</span>";
                nbMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
            }
            else
            {
                lStatusBadge.Text = "<span class='label label-danger'>Unhealthy</span>";
                nbMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Danger;
            }

            nbMessage.Text = message;
            nbMessage.Visible = true;
        }

        #endregion
    }
}