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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Bus;
using Rock.Bus.Queue;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.WebFarm;

namespace RockWeb.Plugins.org_secc.SystemsMonitor
{
    [DisplayName( "Server Health" )]
    [Category( "SECC > Systems Monitor" )]
    [Description( "Displays the health status of this Rock web-farm node, including heartbeat, message bus, and cache queue checks." )]

    [DefinedTypeField(
        "Health Check URLs Defined Type",
        Description = "The Defined Type that contains the health check URLs for each web farm node. Each Defined Value's Value should be the URL and its Description should be the machine name.",
        IsRequired = false,
        DefaultValue = "7ac04f77-c889-493b-9f0a-fa92c1508067",
        Key = AttributeKey.HealthCheckDefinedType,
        Order = 0 )]

    public partial class ServerHealth : RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string HealthCheckDefinedType = "HealthCheckDefinedType";
        }

        #endregion

        #region Constants

        /// <summary>
        /// Shared HttpClient with a short timeout so the health check doesn't hang.
        /// </summary>
        private static readonly HttpClient HealthClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds( 3 )
        };

        #endregion

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

        /// <summary>
        /// Handles the Click event of the btnRefresh control.
        /// </summary>
        protected void btnRefresh_Click( object sender, EventArgs e )
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
            pnlDetails.Visible = false;

            string currentMachineName = Environment.MachineName;
            lMachineName.Text = currentMachineName;

            // ---------------------------------------------------------
            // DEPLOYMENT MAINTENANCE MODE CHECK
            // ---------------------------------------------------------
            string maintenanceFile = Server.MapPath( "~/_maintenance.flag" );

            if ( File.Exists( maintenanceFile ) )
            {
                ShowResult( false, "Maintenance Mode: Server is draining connections for deployment." );
                return;
            }

            // ---------------------------------------------------------
            // WEB FARM CHECK — only run when Web Farm is enabled
            // ---------------------------------------------------------
            bool webFarmEnabled = RockWebFarm.IsEnabled();
            lWebFarmEnabled.Text = webFarmEnabled
                ? "<span class='label label-success'>Enabled</span>"
                : "<span class='label label-default'>Disabled</span>";

            if ( !webFarmEnabled )
            {
                pnlDetails.Visible = true;
                lLastSeen.Text = "N/A";
                lMessageBus.Text = "N/A";
                lCacheQueueRate.Text = "N/A";
                ShowResult( true, "Healthy (Web Farm is not enabled; farm-specific checks were skipped)" );
                return;
            }

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var webFarmService = new WebFarmNodeService( rockContext );

                    var currentNode = webFarmService.Queryable()
                        .Where( n => n.NodeName == currentMachineName && n.IsActive )
                        .OrderByDescending( n => n.LastSeenDateTime )
                        .FirstOrDefault();

                    if ( currentNode == null )
                    {
                        currentNode = webFarmService.Queryable()
                            .Where( n => n.NodeName.StartsWith( currentMachineName ) && n.IsActive )
                            .OrderByDescending( n => n.LastSeenDateTime )
                            .FirstOrDefault();
                    }

                    if ( currentNode == null )
                    {
                        ShowResult( false, string.Format( "Critical: Server '{0}' is not registered in WebFarmNode table.", currentMachineName ) );
                        return;
                    }

                    // Populate detail fields
                    pnlDetails.Visible = true;
                    lLastSeen.Text = currentNode.LastSeenDateTime.ToString( "g" );

                    // 2. Check for "Zombie" State (Stale Heartbeat)
                    double minutesSinceLastSeen = RockDateTime.Now.Subtract( currentNode.LastSeenDateTime ).TotalMinutes;

                    if ( minutesSinceLastSeen > 5 )
                    {
                        ShowResult( false, string.Format( "Error: Node heartbeat is stale. Last seen {0} minutes ago.", Math.Round( minutesSinceLastSeen, 1 ) ) );
                        return;
                    }

                    // 3. Ensure the bus is active
                    bool busReady = RockMessageBus.IsReady();
                    lMessageBus.Text = busReady ? "<span class='label label-success'>Ready</span>" : "<span class='label label-danger'>Not Ready</span>";

                    if ( !busReady )
                    {
                        ShowResult( false, "Error: Message bus is not ready." );
                        return;
                    }

                    // 4. Check cache queue consumption
                    var cacheQueue = RockQueue.GetQueueTypes()
                        .Select( qt => RockQueue.Get( qt ) )
                        .FirstOrDefault( q => q != null && q.Name.Equals( "rock-cache-queue", StringComparison.OrdinalIgnoreCase ) );

                    if ( cacheQueue == null )
                    {
                        ShowResult( false, "Error: Cache queue 'rock-cache-queue' not found." );
                        return;
                    }

                    int? ratePerMinute = cacheQueue.StatLog.MessagesConsumedLastMinute;

                    if ( !ratePerMinute.HasValue )
                    {
                        lCacheQueueRate.Text = "N/A (not yet available)";
                        ShowResult( true, "Healthy (cache queue stat data not yet available)" );
                        return;
                    }

                    lCacheQueueRate.Text = ratePerMinute.Value.ToString();

                    if ( ratePerMinute.Value > 0 )
                    {
                        ShowResult( true, string.Format( "Healthy (rate/min: {0})", ratePerMinute.Value ) );
                        return;
                    }

                    // 5. Rate is 0 — dynamically resolve other servers from DefinedType
                    var otherServerUrls = GetOtherServerHealthUrls( currentMachineName );

                    bool anyOtherServerHealthy = false;

                    foreach ( var url in otherServerUrls )
                    {
                        try
                        {
                            using ( var response = HealthClient.GetAsync( url ).GetAwaiter().GetResult() )
                            {
                                if ( ( int ) response.StatusCode == 200 )
                                {
                                    anyOtherServerHealthy = true;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Timeout, connection refused, DNS failure, etc.
                        }
                    }

                    if ( !anyOtherServerHealthy )
                    {
                        ShowResult( true, "Healthy (last server standing, rate/min: 0, no other servers responded healthy)" );
                        return;
                    }

                    ShowResult( false, "Error: Cache queue consumption is zero. Consumed 0 message(s) in the last minute. At least 1 other server is healthy." );
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
                ShowResult( false, "An unexpected error occurred while checking server health." );
            }
        }

        /// <summary>
        /// Reads all DefinedValues from the configured "Health Check URLs" DefinedType,
        /// skips the entry whose Description matches the current server's machine name,
        /// and returns the remaining health check URLs.
        /// </summary>
        private List<string> GetOtherServerHealthUrls( string currentMachineName )
        {
            var urls = new List<string>();

            var definedTypeGuid = GetAttributeValue( AttributeKey.HealthCheckDefinedType ).AsGuidOrNull();

            if ( !definedTypeGuid.HasValue )
            {
                return urls;
            }

            var definedType = DefinedTypeCache.Get( definedTypeGuid.Value );

            if ( definedType == null )
            {
                return urls;
            }

            foreach ( var dv in definedType.DefinedValues.Where( v => v.IsActive ) )
            {
                if ( dv.Description.IsNotNullOrWhiteSpace()
                     && dv.Description.Trim().Equals( currentMachineName, StringComparison.OrdinalIgnoreCase ) )
                {
                    continue;
                }

                if ( dv.Value.IsNotNullOrWhiteSpace() )
                {
                    urls.Add( dv.Value );
                }
            }

            return urls;
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