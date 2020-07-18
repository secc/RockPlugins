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
    [DisplayName( "Bulk Refund" )]
    [Category( "SECC > Finance" )]
    [Description( "Block to issue bulk refunds." )]
    public partial class BulkRefund : RockBlock
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

                RockContext rockContext = new RockContext();

                SystemCommunicationService systemCommunicationService = new SystemCommunicationService( rockContext );

                if ( pnlRegistration.Visible && registrationTemplateIds.Count > 0 )
                {
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

                    // Load the registration instance and then iterate through all registrations.
                    var registrations = registrationInstanceService.Queryable().Where( ri => registrationInstanceIds.Contains( ri.Id )  ).SelectMany( ri => ri.Registrations );
                    int j = 1;
                    foreach ( Registration registration in registrations )
                    {
                        bool issuedRefund = false;
                        OnProgress( "Processing registration refund " + j + " of " + registrations.Count() );
                        foreach ( var payment in registration.GetPayments( rockContext ) )
                        {
                                issuedRefund = issueRefund( payment.Transaction, "Registration", registration.FirstName + " " + registration.LastName );                            
                        }
                        j++;

                        // Send an email if applicable
                        if ( issuedRefund && !string.IsNullOrWhiteSpace( registration.ConfirmationEmail ) && ddlSystemCommunication.SelectedValueAsInt().HasValue && ddlSystemCommunication.SelectedValueAsInt() > 0 )
                        {
                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
                            mergeFields.Add( "Registration", registration );

                            SystemCommunication systemCommunication = systemCommunicationService.Get( ddlSystemCommunication.SelectedValueAsInt().Value );
                            
                            var emailMessage = new RockEmailMessage( systemCommunication );
                            emailMessage.AdditionalMergeFields = mergeFields;
                            
                            emailMessage.AddRecipient( RockEmailMessageRecipient.CreateAnonymous( registration.ConfirmationEmail, mergeFields ) );
                            emailMessage.CreateCommunicationRecord = true;
                            emailMessage.Send();
                        }
                    }
                }

                if ( pnlTransactionCodes.Visible && tbTransactionCodes.Text.Length > 0 )
                {
                    var codes = tbTransactionCodes.Text.SplitDelimitedValues();
                    FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
                    var transactions = financialTransactionService.Queryable().Where( ft => codes.Contains( ft.TransactionCode ) );
                    int j = 0;
                    foreach(var transaction in transactions)
                    {
                        OnProgress( "Processing transaction refund " + j + " of " + transactions.Count() );
                        var issuedRefund = issueRefund( transaction, "Transaction", transaction.AuthorizedPersonAlias != null ? transaction.AuthorizedPersonAlias.Person.FullName : "Unknown" );

                        // Send an email if applicable
                        if ( issuedRefund && transaction.AuthorizedPersonAlias != null && !string.IsNullOrWhiteSpace( transaction.AuthorizedPersonAlias.Person.Email ) && ddlSystemCommunication.SelectedValueAsInt().HasValue && ddlSystemCommunication.SelectedValueAsInt() > 0 )
                        {
                            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
                            mergeFields.Add( "Transaction", transaction );

                            SystemCommunication systemCommunication = systemCommunicationService.Get( ddlSystemCommunication.SelectedValueAsInt().Value );

                            var emailMessage = new RockEmailMessage( systemCommunication );
                            emailMessage.AdditionalMergeFields = mergeFields;
                            emailMessage.FromEmail = ebEmail.Text;
                            emailMessage.AddRecipient( new RockEmailMessageRecipient(transaction.AuthorizedPersonAlias.Person, mergeFields ));
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
        /// Issue a refund for the transaction
        /// </summary>
        /// <param name="transaction">The transaction to refund.</param>
        /// <param name="label">The label for the log entry (Registration or Transaction).</param>
        /// <param name="value">The value for the log entry for this line item.</param>
        /// <returns></returns>
        private bool issueRefund( FinancialTransaction transaction, string label, string value)
        {
            decimal refundAmount = transaction.TotalAmount + transaction.Refunds.Sum( r => r.FinancialTransaction.TotalAmount );

            // If refunds totalling the amount of the payments have not already been issued
            if ( transaction.TotalAmount > 0 && refundAmount > 0 )
            {
                string errorMessage;

                using ( var refundRockContext = new RockContext() )
                {
                    var financialTransactionService = new FinancialTransactionService( refundRockContext );
                    var refundTransaction = financialTransactionService.ProcessRefund( transaction, refundAmount, dvpRefundReason.SelectedDefinedValueId, tbRefundSummary.Text, true, string.Empty, out errorMessage );

                    if ( refundTransaction != null )
                    {
                        refundRockContext.SaveChanges();
                    }

                    if ( !string.IsNullOrWhiteSpace( errorMessage ) )
                    {
                        results["Fail"] += string.Format( "Failed refund for {0} {1}: {2}",
                            label,
                            value,
                            errorMessage ) + Environment.NewLine;
                    }
                    else
                    {
                        results["Success"] += string.Format( "Successfully issued {0} refund for {1} {2} payment {3} ({4}) - Refund Transaction Id: {5}, Amount: {6}",

                            refundAmount < transaction.TotalAmount ? "partial" : "full",
                            label,
                            value,
                            transaction.TransactionCode,
                            transaction.TotalAmount,
                            refundTransaction.TransactionCode,
                            refundTransaction.TotalAmount.FormatAsCurrency() ) + Environment.NewLine;
                        return true;

                    }

                }
            }
            else if ( transaction.Refunds.Count > 0 )
            {
                results["Success"] += string.Format( "Refund already issued for {0} {1} payment {2} ({3})",
                            label,
                            value,
                            transaction.TransactionCode,
                            transaction.TotalAmount ) + Environment.NewLine;
            }
            return false;
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

            if ( !ddlSystemCommunication.SelectedValueAsInt().HasValue )
            {
                SystemCommunicationService systemCommunicationService = new SystemCommunicationService( rockContext );
                var systemCommunications = systemCommunicationService.Queryable().Where(c => c.IsActive == true ).Select( e => new { Title = e.Category.Name + " - " + e.Title, e.Id } ).OrderBy( e => e.Title ).ToList();
                systemCommunications.Insert( 0, new { Title = "", Id = 0 } );
                ddlSystemCommunication.DataSource = systemCommunications;
                ddlSystemCommunication.DataValueField = "Id";
                ddlSystemCommunication.DataTextField = "Title";
                ddlSystemCommunication.DataBind();
            }

            List<int> registrationTemplateIds = rtpRegistrationTemplate.ItemIds.AsIntegerList();
            registrationTemplateIds.RemoveAll( i => i.Equals( 0 ) );
            int itemCount = 0;
            decimal totalPayments = 0;

            if ( liRegistration.Visible == true &&  registrationTemplateIds.Count > 0 )
            {
                RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
                var templates = registrationTemplateService.GetByIds( rtpRegistrationTemplate.ItemIds.AsIntegerList() );
                var instances = templates.SelectMany( t => t.Instances );
                if ( ddlRegistrationInstance.SelectedValueAsId().HasValue && ddlRegistrationInstance.SelectedValueAsId() > 0 )
                {
                    var instanceId = ddlRegistrationInstance.SelectedValueAsId();
                    instances = instances.Where( i => i.Id == instanceId );
                }
                itemCount = instances.SelectMany( i => i.Registrations ).Count();
                totalPayments = instances.SelectMany( i => i.Registrations ).ToList().SelectMany( r => r.Payments ).Sum( p => p.Transaction.TotalAmount );

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

            if ( liTransactionCodes.Visible == true && tbTransactionCodes.Text.Length > 0 )
            {
                var codes = tbTransactionCodes.Text.SplitDelimitedValues();
                FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
                var transactions = financialTransactionService.Queryable().Where( ft => codes.Contains( ft.TransactionCode ) );
                totalPayments = transactions.SelectMany( t => t.TransactionDetails ).Sum( td => td.Amount );
                itemCount = transactions.Count();

            }
            lAlert.Text = itemCount + ( pnlRegistration.Visible?" Registrations - ": " Transactions - " ) + totalPayments.FormatAsCurrency() + " Total";
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

        protected void btnRefundType_Click( object sender, EventArgs e )
        {
            liTransactionCodes.RemoveCssClass( "active" );
            pnlTransactionCodes.Visible = false;
            liRegistration.RemoveCssClass( "active" );
            pnlRegistration.Visible = false;
            var argument = ( ( LinkButton ) sender).CommandArgument;
            switch ( argument )
            {
                case "Codes":
                    liTransactionCodes.AddCssClass( "active" );
                    pnlTransactionCodes.Visible = true;
                    ebEmail.Required = true;
                    break;
                case "Registration":
                default:
                    liRegistration.AddCssClass( "active" );
                    pnlRegistration.Visible = true;
                    ebEmail.Required = false;
                    break;
            }

            BindData();

        }
    }
}