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
using Microsoft.AspNet.SignalR;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
using System.Linq;
using System.Web.UI.WebControls;
using Rock.Web.Cache;
using Rock.Communication;

namespace RockWeb.Plugins.org_secc.Finance
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Bulk Registration Refund" )]
    [Category( "SECC > Finance" )]
    [Description( "Block to issue bulk refunds for registrations." )]
    public partial class BulkRegistrationRefund : RockBlock
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
                return string.Format( "BulkRegistrationRefund_BlockId:{0}_SessionId:{1}", this.BlockId, Session.SessionID );
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
            BindData();
        }

        #endregion

        #region Events

        Dictionary<string, string> results = new Dictionary<string, string>() { { "Success", "" }, { "Fail", "" } };

        /// <summary>
        /// Starts the refund process.
        /// </summary>
        private void StartRefunds()
        {
            long totalMilliseconds = 0;


            var importTask = new Task( () =>
            {
                // wait a little so the browser can render and start listening to events
                System.Threading.Thread.Sleep( 1000 );
                _hubContext.Clients.All.showButtons( this.SignalRNotificationKey, false );

                Stopwatch stopwatch = Stopwatch.StartNew();

                List<int> registrationTemplateIds = rtpRegistrationTemplate.ItemIds.AsIntegerList();
                registrationTemplateIds.RemoveAll( i => i.Equals( 0 ) );

                if ( registrationTemplateIds.Count > 0 )
                {
                    RockContext rockContext = new RockContext();
                    List<int> registrationInstanceIds = new List<int>();
                    if ( ddlRegistrationInstance.SelectedValueAsId().HasValue && ddlRegistrationInstance.SelectedValueAsId() > 0)
                    {
                        registrationInstanceIds.Add( ddlRegistrationInstance.SelectedValueAsId().Value );
                    }
                    else
                    {

                        RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
                        var templates = registrationTemplateService.GetByIds( rtpRegistrationTemplate.ItemIds.AsIntegerList() );
                        int registrationCount = templates.SelectMany( t => t.Instances ).SelectMany( i => i.Registrations ).Count();

                        registrationInstanceIds.AddRange( templates.SelectMany( t => t.Instances ).OrderBy( i => i.Name ).Select( i => i.Id ) );
                    }

                    RegistrationInstanceService registrationInstanceService = new RegistrationInstanceService( rockContext );
                    SystemEmailService systemEmailService = new SystemEmailService( rockContext );

                    // Load the registration instance and then iterate through all registrations.
                    var registrations = registrationInstanceService.Queryable().Where( ri => registrationInstanceIds.Contains( ri.Id )  ).SelectMany( ri => ri.Registrations );
                    int j = 1;
                    foreach ( Registration registration in registrations )
                    {
                        bool issuedRefund = false;
                        OnProgress( "Processing registration refund " + j + " of " + registrations.Count() );
                        foreach ( var payment in registration.GetPayments( rockContext ) )
                        {
                            decimal refundAmount = payment.Amount + payment.Transaction.Refunds.Sum( r => r.FinancialTransaction.TotalAmount );

                            // If refunds totalling the amount of the payments have not already been issued
                            if ( payment.Amount > 0 && refundAmount > 0 )
                            {
                                string errorMessage;

                                using ( var refundRockContext = new RockContext() )
                                {
                                    var financialTransactionService = new FinancialTransactionService( refundRockContext );
                                    var refundTransaction = financialTransactionService.ProcessRefund( payment.Transaction, refundAmount, dvpRefundReason.SelectedDefinedValueId, tbRefundSummary.Text, true, string.Empty, out errorMessage );

                                    if ( refundTransaction != null )
                                    {
                                        refundRockContext.SaveChanges();
                                    }

                                    if ( !string.IsNullOrWhiteSpace( errorMessage ) )
                                    {
                                        results["Fail"] += string.Format( "Failed refund for registration {0}: {1}",
                                            registration.FirstName + " " + registration.LastName,
                                            errorMessage) + Environment.NewLine ;
                                    }
                                    else
                                    {
                                        results["Success"] += string.Format("Successfully issued {0} refund for registration {1} payment {2} ({3}) - Refund Transaction Id: {4}, Amount: {5}",

                                            refundAmount < payment.Amount?"Partial":"Full",
                                            registration.FirstName + " " + registration.LastName,
                                            payment.Transaction.TransactionCode,
                                            payment.Transaction.TotalAmount,
                                            refundTransaction.TransactionCode,
                                            refundTransaction.TotalAmount.FormatAsCurrency() ) + Environment.NewLine;
                                        issuedRefund = true;

                                    }

                                }
                                System.Threading.Thread.Sleep( 2500 );
                            }
                            else if ( payment.Transaction.Refunds.Count > 0 )
                            {
                                results[ "Success"] += string.Format( "Refund already issued for registration {0} payment {1} ({2})",
                                            registration.FirstName + " " + registration.LastName,
                                            payment.Transaction.TransactionCode,
                                            payment.Transaction.TotalAmount ) + Environment.NewLine;
                            }
                        }
                        j++;

                        // Send an email if applicable
                        if ( issuedRefund && !string.IsNullOrWhiteSpace( registration.ConfirmationEmail ) && ddlSystemEmail.SelectedValueAsInt().HasValue && ddlSystemEmail.SelectedValueAsInt() > 0 )
                        {
                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
                            mergeFields.Add( "Registration", registration );

                            SystemEmail systemEmail = systemEmailService.Get( ddlSystemEmail.SelectedValueAsInt().Value );
                            
                            var emailMessage = new RockEmailMessage( systemEmail );
                            emailMessage.AddRecipient( new RecipientData( registration.ConfirmationEmail, mergeFields ) );
                            emailMessage.CreateCommunicationRecord = true;
                            emailMessage.Send();
                        }
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
                    OnProgress( string.Format( "{0} Complete: [{1}ms]", "All refunds have been issued.", totalMilliseconds ) );
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

        private void BindData()
        {

            RockContext rockContext = new RockContext();
            dvpRefundReason.DefinedTypeId = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_REFUND_REASON.AsGuid(), rockContext ).Id;

            if ( !ddlSystemEmail.SelectedValueAsInt().HasValue )
            {
                SystemEmailService systemEmailService = new SystemEmailService( rockContext );
                var systemEmails = systemEmailService.Queryable().Select( e => new { Title = e.Category.Name + " - " + e.Title, e.Id } ).OrderBy( e => e.Title ).ToList();
                systemEmails.Insert( 0, new { Title = "", Id = 0 } );
                ddlSystemEmail.DataSource = systemEmails;
                ddlSystemEmail.DataValueField = "Id";
                ddlSystemEmail.DataTextField = "Title";
                ddlSystemEmail.DataBind();
            }

            List<int> registrationTemplateIds = rtpRegistrationTemplate.ItemIds.AsIntegerList();
            registrationTemplateIds.RemoveAll( i => i.Equals( 0 ) );

            if ( registrationTemplateIds.Count > 0 )
            {
                RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
                var templates = registrationTemplateService.GetByIds( rtpRegistrationTemplate.ItemIds.AsIntegerList() );
                var instances = templates.SelectMany( t => t.Instances );
                if ( ddlRegistrationInstance.SelectedValueAsId().HasValue && ddlRegistrationInstance.SelectedValueAsId() > 0 )
                {
                    var instanceId = ddlRegistrationInstance.SelectedValueAsId();
                    instances = instances.Where( i => i.Id == instanceId );
                }
                int registrationCount = instances.SelectMany( i => i.Registrations ).Count();
                var totalPayments = instances.SelectMany( i => i.Registrations ).ToList().SelectMany( r => r.Payments ).Sum( p => p.Transaction.TotalAmount );
                lAlert.Text = registrationCount + " Registrations - " + totalPayments.FormatAsCurrency() + " Total";

                if ( ! ddlRegistrationInstance.SelectedValueAsInt().HasValue )
                { 
                    var instanceList = templates.SelectMany( t => t.Instances ).OrderBy( i => i.Name ).Select( i => new { i.Id, i.Name } ).ToList();
                    instanceList.Insert( 0, new { Id = 0, Name="" } );
                    ddlRegistrationInstance.DataSource = instanceList;
                    ddlRegistrationInstance.DataValueField = "Id";
                    ddlRegistrationInstance.DataTextField = "Name";
                    ddlRegistrationInstance.DataBind();
                }
            }
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



        protected void btnProcessRefunds_Click( object sender, EventArgs e )
        {
            btnProcessRefunds.Visible = false;
            StartRefunds();
        }

        protected void rtpRegistrationTemplate_SelectItem( object sender, EventArgs e )
        {
            if ( rtpRegistrationTemplate.ItemIds.AsIntegerList().Count > 0 )
            {
                BindData();
            }
        }

        protected void ddlRegistrationInstance_SelectionChanged( object sender, EventArgs e )
        {
            BindData();
        }
    }
}