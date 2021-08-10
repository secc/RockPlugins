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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNet.SignalR;
using org.secc.Communication.Twilio;
using Rock;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Communication
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Sync Twilio History" )]
    [Category( "SECC > Communication" )]
    [Description( "Block to sync Twilio SMS history to our server." )]
    public partial class SyncTwilioHistory : RockBlock
    {

        #region Fields

        /// <summary>
        /// This holds the reference to the RockMessageHub SignalR Hub context.
        /// </summary>
        private IHubContext _hubContext = GlobalHost.ConnectionManager.GetHubContext<RockMessageHub>();

        /// <summary>
        /// Gets the signal r notification key.
        /// </summary>
        /// <value>
        /// The signal r notification key.
        /// </value>
        public string SignalRNotificationKey
        {
            get
            {
                return string.Format( "SyncTwilioHistory:{0}_SessionId:{1}", this.BlockId, Session.SessionID );
            }
        }

        #endregion Fields

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", fingerprint: false );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        #endregion

        #region Events

        Dictionary<string, string> results = new Dictionary<string, string>() { { "Complete", "" } };

        /// <summary>
        /// Starts the refund process.
        /// </summary>
        private void Sync()
        {
            long totalMilliseconds = 0;
            long syncTotal = 0;
            var startDate = drpDateRange.DateRange.Start;
            var endDate = drpDateRange.DateRange.End;

            var importTask = new Task( () =>
            {
                // wait a little so the browser can render and start listening to events
                System.Threading.Thread.Sleep( 1000 );
                _hubContext.Clients.All.showButtons( this.SignalRNotificationKey, false );

                Stopwatch stopwatch = Stopwatch.StartNew();



                if ( startDate != null || endDate != null && startDate.Value < endDate.Value )
                {
                    var syncDate = startDate.Value;

                    TwilioDownloader twilioDownloader = new TwilioDownloader();



                    while ( syncDate <= endDate )
                    {
                        OnProgress( "Syncing for " + syncDate.ToString( "M/d/yyyy" ) );
                        var syncCount = twilioDownloader.SyncItems( syncDate );
                        results["Complete"] += string.Format( "Synced {0} messages from {1}{2}", syncCount, syncDate.ToString( "M/d/yyyy" ), Environment.NewLine );
                        syncDate = syncDate.AddDays( 1 );
                        syncTotal += syncCount;
                    }
                }

                stopwatch.Stop();

                totalMilliseconds = stopwatch.ElapsedMilliseconds;

                _hubContext.Clients.All.showButtons( this.SignalRNotificationKey, true );
            } );

            importTask.ContinueWith( ( t ) =>
            {
                if ( t.IsFaulted )
                {
                    foreach ( var exception in t.Exception.InnerExceptions )
                    {
                        LogException( exception );
                    }

                    OnProgress( "ERROR: " + t.Exception.Message );
                }
                else
                {
                    var time = TimeSpan.FromMilliseconds( totalMilliseconds );

                    OnProgress( string.Format( "Synced {0} messages between {1} and {2}. [{3}]",
                            syncTotal,
                            startDate.Value.ToString( "M/d/yyyy" ),
                            endDate.Value.ToString( "M/d/yyyy" ),
                            time.Humanize() ) );
                }

            } );

            importTask.Start();
        }

        /// <summary>
        /// Handles the ProgressChanged event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
        private void OnProgress( object e )
        {

            string progressMessage = string.Empty;
            DescriptionList progressResults = new DescriptionList();
            if ( e is string )
            {
                progressMessage = e.ToString();
            }

            foreach ( var result in results )
            {
                progressResults.Add( result.Key, result.Value );
            }

            WriteProgressMessage( progressMessage, progressResults.Html );
        }



        /// <summary>
        /// Writes the progress message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteProgressMessage( string message, string results )
        {
            _hubContext.Clients.All.receiveNotification( this.SignalRNotificationKey, message, results.ConvertCrLfToHtmlBr() );
        }

        #endregion


        protected void btnSync_Click( object sender, EventArgs e )
        {
            btnSync.Visible = false;
            drpDateRange.ReadOnly = true;
            Sync();
        }
    }
}