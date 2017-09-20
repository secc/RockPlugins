// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.ComponentModel;
using Rock.Attribute;
using Rock.Model;
using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Rock.Data;
using Rock.Financial;
using Rock;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace org.secc.PayPalExpress
{
    #region Block Attributes

    /// <summary>
    /// Add a new one-time or scheduled transaction
    /// </summary>
    [DisplayName("Transaction Entry with PayPal Express")]
    [Category("SECC > Finance")]
    [Description("Creates a new financial transaction or scheduled transaction including PayPal Express.")]
    [FinancialGatewayField("PayPal Express Gateway", "The PayPal Express gateway.", false, "", "", 1, "PayPalExpressGateway")]
    #endregion

    public partial class TransactionEntry : RockWeb.Blocks.Finance.TransactionEntry
    {
        RockWeb.Blocks.Finance.TransactionEntry RockTransactionEntry = null;
        private FinancialGateway _payPalExpressGateway;
        private GatewayComponent _payPalExpressGatewayComponent = null;
        private Boolean showPayPalExpressTab = false;
        LinkButton PayPalExpressNextButton = new LinkButton();
        HtmlGenericControl liPayPal = new HtmlGenericControl("li");
        HtmlGenericControl redirectDiv = new HtmlGenericControl("div");
        private Person _targetPerson = null;
        PaymentInfo paymentInfo = null;
        List<AccountItem> TokenSelectedAccounts = null;

    protected override void OnInit(EventArgs e)
        {
            using (var rockContext = new RockContext())
            {
                _payPalExpressGateway = GetGateway(rockContext, "PayPalExpressGateway");
                _payPalExpressGatewayComponent = GetGatewayComponent(rockContext, _payPalExpressGateway);
                SetTargetPerson(rockContext);
            }
            if (PageParameter("token") != String.Empty && PageParameter("PayerID") != string.Empty)
            {
                String errorMessage = String.Empty;
                PaymentInfo paymentInfo = GetPaymentInfo();
                if (errorMessage != string.Empty)
                {
                    ShowMessage(NotificationBoxType.Danger, "PayPal Error", errorMessage);
                } else { 
                    tdNameConfirm.Description = paymentInfo.FullName;
                    tdPhoneConfirm.Description = paymentInfo.Phone;
                    tdEmailConfirm.Description = paymentInfo.Email;
                    tdAddressConfirm.Description = string.Format("{0} {1}, {2} {3}", paymentInfo.Street1, paymentInfo.City, paymentInfo.State, paymentInfo.PostalCode);
                
                    tdTotalConfirm.Description = paymentInfo.Amount.ToString("C");
                
                    tdPaymentMethodConfirm.Description = paymentInfo.CurrencyTypeValue.Description;

                    tdAccountNumberConfirm.Description = paymentInfo.MaskedNumber;
                    tdAccountNumberConfirm.Visible = !string.IsNullOrWhiteSpace(paymentInfo.MaskedNumber);
                    tdWhenConfirm.Description = "Today";

                    rptAccountListConfirmation.DataSource = GetSelectedAccounts().Where(a => a.Amount != 0);
                    rptAccountListConfirmation.DataBind();
                }
                pnlConfirmation.Visible = true;
                pnlDupWarning.Visible = false;

            } else { 
                RockTransactionEntry = (RockWeb.Blocks.Finance.TransactionEntry)LoadControl("~/Blocks/Finance/TransactionEntry.ascx");
                RockTransactionEntry.Page = RockPage;
                RockTransactionEntry.SetBlock(PageCache, BlockCache);
                Controls.Add(RockTransactionEntry);
                RegisterScript();
            }

        }
        protected override void OnLoad(EventArgs e)
        {

            if (Page.IsPostBack)
            {
                if (RockTransactionEntry != null)
                { 
                    rptAccountList = (Repeater)RockTransactionEntry.FindControl("rptAccountList");
                    // Save amounts from controls to the viewstate list
                    foreach (RepeaterItem item in rptAccountList.Items)
                    {
                        var accountAmount = item.FindControl("txtAccountAmount") as RockTextBox;
                        if (accountAmount != null)
                        {
                            if (GetSelectedAccounts().Count > item.ItemIndex)
                            {
                                decimal amount = decimal.MinValue;
                                if (decimal.TryParse(accountAmount.Text, out amount))
                                {
                                    GetSelectedAccounts()[item.ItemIndex].Amount = amount;
                                }
                            }
                        }
                    }
                }
            } else
            {
                GetAccounts();

                lPanelTitle2.Text = GetAttributeValue("PanelTitle");
                lConfirmationTitle.Text = GetAttributeValue("ConfirmationTitle");
                lSuccessTitle.Text = GetAttributeValue("SuccessTitle");

                // Resolve the text field merge fields
                var configValues = new Dictionary<string, object>();
                lConfirmationHeader.Text = GetAttributeValue("ConfirmationHeader").ResolveMergeFields(configValues);
                lConfirmationFooter.Text = GetAttributeValue("ConfirmationFooter").ResolveMergeFields(configValues);
                lSuccessHeader.Text = GetAttributeValue("SuccessHeader").ResolveMergeFields(configValues);

            }
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (RockTransactionEntry != null) { 
                // Add the additional next button action
                ((LinkButton)RockTransactionEntry.FindControl("btnPaymentInfoNext")).Click += btnPaymentInfoNext_Click;
                ((ButtonDropDownList)RockTransactionEntry.FindControl("btnAddAccount")).SelectionChanged += btnAddAccount_SelectionChanged;

                dtpStartDate = ((DatePicker)RockTransactionEntry.FindControl("dtpStartDate"));
                dtpStartDate.AutoPostBack = true;
            }
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // The rest of this is only when we aren't in confirmation mode
            if (RockTransactionEntry == null)
            {
                return;
            }

            PayPalExpressNextButton.Text = "Next";
            PayPalExpressNextButton.CssClass = "btn btn-primary pull-right";
            PayPalExpressNextButton.Click += btnPaymentInfoNext_Click;
            bool payPalExpressEnabled = _payPalExpressGatewayComponent != null;
            if (payPalExpressEnabled)
            {
                PaymentSchedule schedule = GetSchedule();
                // Disable the other gateway if the frequency is unsupported
                if (schedule != null && !_payPalExpressGatewayComponent.SupportedPaymentSchedules.Contains(schedule.TransactionFrequencyValue))
                {
                    payPalExpressEnabled = false;
                }

                bool allowScheduled = GetAttributeValue( "AllowScheduled" ).AsBoolean();

                if (allowScheduled && ( !dtpStartDate.SelectedDate.HasValue || dtpStartDate.SelectedDate.Value.Date > RockDateTime.Today.AddDays(1)))
                {
                    payPalExpressEnabled = false;
                }
            }
            if (hfMyPaymentTab.Value == "divPayPalPaymentInfo")
            {
                ShowTab();
            }
            if (payPalExpressEnabled)
            {
                RockTransactionEntry.FindControl("phPills").Visible = true;
                liPayPal.ID = "liPayPalExpress";

                LinkButton payPalExpressLink = new LinkButton();
                payPalExpressLink.ID = "lbPayPalExpress";
                payPalExpressLink.Attributes.Add("title", "PayPal - The safer, easier way to pay online!");
                payPalExpressLink.Attributes.Add("style", "padding: 7px");
                payPalExpressLink.Attributes.Add("href", "#divPayPalPaymentInfo");
                payPalExpressLink.Attributes.Add("data-toggle", "pill");
                payPalExpressLink.Text = "<img src=\"/assets/PayPalExpress/PP_logo_h_100x26.png\" alt=\"PayPal\" />";
                liPayPal.Controls.Add(payPalExpressLink);

                redirectDiv.ID = "divPayPalPaymentInfo";
                redirectDiv.AddCssClass("tab-pane");
                redirectDiv.InnerText = "You will be redirected to PayPal to complete this transaction when you click \"Next\" below.";

                // These controls needs to be added early to ensure the postbacks fire

                // Add the pill
                var pillControls = RockTransactionEntry.FindControl("phPills").Controls;
                pillControls.AddAt(pillControls.Count - 1, liPayPal);
                
                // Add the tab content div
                var divControls = RockTransactionEntry.FindControl("divNewPayment").Controls;
                LiteralControl tabContent = (LiteralControl)divControls[divControls.Count - 3];
                StringBuilder generatedHtml = new StringBuilder();
                using (var htmlStringWriter = new StringWriter(generatedHtml))
                {
                    using (var htmlTextWriter = new HtmlTextWriter(htmlStringWriter))
                    {
                        redirectDiv.RenderControl(htmlTextWriter);
                        tabContent.Text += generatedHtml.ToString();
                    }
                }
            }
        }

        private List<AccountItem> GetSelectedAccounts()
        {

            if (PageParameter("token") != String.Empty && PageParameter("PayerID") != string.Empty)
            {
                // Load this once per token request
                if (TokenSelectedAccounts != null)
                {
                    return TokenSelectedAccounts;
                }
                SelectedAccounts = new List<AccountItem>();
                String errorMessage = string.Empty;
                var selectedAccounts = ((org.secc.PayPalExpress.Gateway)_payPalExpressGatewayComponent).GetSelectedAccounts(_payPalExpressGateway, PageParameter("token"), out errorMessage);
                if (errorMessage != string.Empty)
                {
                    ShowMessage(NotificationBoxType.Danger, "Payment Processing Error", errorMessage);
                    return SelectedAccounts;
                }

                foreach (RedirectGatewayComponent.GatewayAccountItem accountItem in selectedAccounts)
                {
                    AccountItem item = new AccountItem(accountItem.Id, 0, accountItem.Name, null, accountItem.PublicName);
                    item.Amount = accountItem.Amount;
                    SelectedAccounts.Add(item);
                }
                TokenSelectedAccounts = SelectedAccounts;
            }
            return SelectedAccounts;
        }

        private void ShowTab()
        {

            liPayPal.AddCssClass("active");
            redirectDiv.AddCssClass("active");
            // Hide the default next button

            ((HtmlGenericControl)RockTransactionEntry.FindControl("liCreditCard")).RemoveCssClass("active");
            ((HtmlGenericControl)RockTransactionEntry.FindControl("liACH")).RemoveCssClass("active");
            ((HtmlGenericControl)RockTransactionEntry.FindControl("divCCPaymentInfo")).RemoveCssClass("active");
            ((HtmlGenericControl)RockTransactionEntry.FindControl("divACHPaymentInfo")).RemoveCssClass("active");
        }

        /// <summary>
        /// Handles the SelectionChanged event of the btnAddAccount control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAddAccount_SelectionChanged(object sender, EventArgs e)
        {
            btnAddAccount = ((ButtonDropDownList)RockTransactionEntry.FindControl("btnAddAccount"));
            var selected = AvailableAccounts.Where(a => a.Id == (btnAddAccount.SelectedValueAsId() ?? 0)).ToList();
            AvailableAccounts = AvailableAccounts.Except(selected).ToList();
            GetSelectedAccounts().AddRange(selected);
        }

        /// <summary>
        /// Handles the Click event of the btnPaymentInfoNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPaymentInfoNext_Click(object sender, EventArgs e)
        {
            if (hfMyPaymentTab.Value == "PayPalExpress") {

                ShowTab();
                String errorMessage = string.Empty;

                var errorMessages = new List<string>();

                // Validate that an amount was entered
                if (GetSelectedAccounts().Sum(a => a.Amount) <= 0)
                {
                    errorMessages.Add("Make sure you've entered an amount for at least one account");
                }

                // Validate that no negative amounts were entered
                if (GetSelectedAccounts().Any(a => a.Amount < 0))
                {
                    errorMessages.Add("Make sure the amount you've entered for each account is a positive amount");
                }

                if (errorMessages.Any())
                {
                    errorMessage = errorMessages.AsDelimited("<br/>");

                    ShowMessage(NotificationBoxType.Danger, "Before we finish...", errorMessage);
                    return;
                }

                if (_payPalExpressGateway != null)
                {
                    if (_payPalExpressGatewayComponent is RedirectGatewayComponent)
                    {
                        var gatewayComponent = ((RedirectGatewayComponent)_payPalExpressGatewayComponent);
                        List<RedirectGatewayComponent.GatewayAccountItem> accountItems = new List<RedirectGatewayComponent.GatewayAccountItem>();
                        foreach (AccountItem account in GetSelectedAccounts())
                        {
                            RedirectGatewayComponent.GatewayAccountItem gatewayAccountItem = new RedirectGatewayComponent.GatewayAccountItem()
                            {
                                Amount = account.Amount,
                                CampusId = account.CampusId,
                                Id = account.Id,
                                Name = account.Name,
                                Order = account.Order,
                                PublicName = account.PublicName
                            };
                            accountItems.Add(gatewayAccountItem);
                        }
                        gatewayComponent.PreRedirect(_payPalExpressGateway, GetPaymentInfo(), accountItems, out errorMessage);
                        if (errorMessage.Length > 0)
                        {
                            ShowMessage(NotificationBoxType.Danger, "Before we finish...", errorMessage);
                        }
                        else
                        {
                            Response.Redirect(gatewayComponent.RedirectUrl);
                            Context.ApplicationInstance.CompleteRequest();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Handles the Click event of the btnConfirmationNext control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPayPalConfirmationNext_Click(object sender, EventArgs e)
        {
            string errorMessage = string.Empty;

            var transaction = _payPalExpressGatewayComponent.Charge(_payPalExpressGateway, GetPaymentInfo(), out errorMessage);
            if (errorMessage != string.Empty)
            {
                ShowMessage(NotificationBoxType.Danger, "Payment Processing Error", errorMessage);
                return;
            }
            if (transaction == null)
            {
                ShowMessage(NotificationBoxType.Danger, "Payment Error", "Invalid Transaction");
                return;
            }
            Person person = GetPerson(GetPaymentInfo(), true);
            RockContext rockContext = new RockContext();
            SaveTransaction(_payPalExpressGateway, _payPalExpressGatewayComponent, person, GetPaymentInfo(), transaction, rockContext);
            FinancialPaymentDetail paymentDetail = transaction.FinancialPaymentDetail.Clone(false);

            ShowSuccess(_payPalExpressGatewayComponent, person, GetPaymentInfo(), null, paymentDetail, rockContext);
        }


        /// <summary>
        /// Registers the startup script.
        /// </summary>
        private void RegisterScript()
        {

            string scriptFormat = @"
    Sys.Application.add_load(function () {{

        // Save the state of the selected payment type pill to a hidden field so that state can
        // be preserved through postback
        $('a[data-toggle=""pill""]').on('shown.bs.tab', function (e) {{
            var tabHref = $(e.target).attr(""href"");
            if (tabHref == '#{0}') {{
                $('#{2}').val('CreditCard');
            }} else if (tabHref.match(/#{1}/)) {{
                $('#{2}').val('PayPalExpress');
            }} else {{
                $('#{2}').val('ACH');
            }}
        }});
    }});

";
            string script = string.Format(
                scriptFormat,
                RockTransactionEntry.FindControl("divCCPaymentInfo").ClientID,         // {0}
                "divPayPalPaymentInfo",         // {1},
                hfMyPaymentTab.ClientID         // {2}
            );

            ScriptManager.RegisterStartupScript(Page, this.GetType(), "giving-profile-ppExpress", script, true);

        }

        private void SetTargetPerson(RockContext rockContext)
        {
            // If impersonation is allowed, and a valid person key was used, set the target to that person
            if (GetAttributeValue("Impersonation").AsBooleanOrNull() ?? false)
            {
                string personKey = PageParameter("Person");
                if (!string.IsNullOrWhiteSpace(personKey))
                {
                    _targetPerson = new PersonService(rockContext).GetByUrlEncodedKey(personKey);
                }
            }

            if (_targetPerson == null)
            {
                _targetPerson = CurrentPerson;
            }
        }

        private FinancialGateway GetGateway(RockContext rockContext, string attributeName)
        {
            var financialGatewayService = new FinancialGatewayService(rockContext);
            Guid? ccGatewayGuid = GetAttributeValue(attributeName).AsGuidOrNull();
            if (ccGatewayGuid.HasValue)
            {
                return financialGatewayService.Get(ccGatewayGuid.Value);
            }
            return null;
        }

        private GatewayComponent GetGatewayComponent(RockContext rockContext, FinancialGateway gateway)
        {
            if (gateway != null)
            {
                gateway.LoadAttributes(rockContext);
                return gateway.GetGatewayComponent();
            }
            return null;
        }



        /// <summary>
        /// Gets the payment schedule.
        /// </summary>
        /// <returns></returns>
        private PaymentSchedule GetSchedule()
        {
            if (RockTransactionEntry == null)
            {
                return null;
            }

            btnFrequency = ((ButtonDropDownList)RockTransactionEntry.FindControl("btnFrequency"));
            dtpStartDate = ((DatePicker)RockTransactionEntry.FindControl("dtpStartDate"));
            // Figure out if this is a one-time transaction or a future scheduled transaction
            if (GetAttributeValue("AllowScheduled").AsBoolean())
            {
                // If a one-time gift was selected for today's date, then treat as a onetime immediate transaction (not scheduled)
                int oneTimeFrequencyId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME).Id;
                if (btnFrequency.SelectedValue == oneTimeFrequencyId.ToString() && dtpStartDate.SelectedDate <= RockDateTime.Today)
                {
                    // one-time immediate payment
                    return null;
                }

                var schedule = new PaymentSchedule();
                schedule.TransactionFrequencyValue = DefinedValueCache.Read(btnFrequency.SelectedValueAsId().Value);
                if (dtpStartDate.SelectedDate.HasValue && dtpStartDate.SelectedDate > RockDateTime.Today)
                {
                    schedule.StartDate = dtpStartDate.SelectedDate.Value;
                }
                else
                {
                    schedule.StartDate = DateTime.MinValue;
                }

                return schedule;
            }

            return null;
        }

        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        private void ShowMessage(NotificationBoxType type, string title, string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                NotificationBox nb = nbConfirmationMessage;
                if (RockTransactionEntry != null)
                {
                    nb = (NotificationBox)RockTransactionEntry.FindControl("nbMessage");
                    hfCurrentPage = (HiddenField)RockTransactionEntry.FindControl("hfCurrentPage");
                    switch (hfCurrentPage.Value.AsInteger())
                    {
                        case 1: nb = (NotificationBox)RockTransactionEntry.FindControl("nbSelectionMessage"); break;
                        case 2: nb = (NotificationBox)RockTransactionEntry.FindControl("nbSelectionMessage"); break;
                        case 3: nb = (NotificationBox)RockTransactionEntry.FindControl("nbConfirmationMessage"); break;
                        case 4: nb = (NotificationBox)RockTransactionEntry.FindControl("nbSuccessMessage"); break;
                    }
                }

                nb.Text = text;
                nb.Title = string.IsNullOrWhiteSpace(title) ? "" : string.Format("<p>{0}</p>", title);
                nb.NotificationBoxType = type;
                nb.Visible = true;
            }
        }

        /// <summary>
        /// Gets the payment information.
        /// </summary>
        /// <returns></returns>
        private PaymentInfo GetPaymentInfo()
        {
            if (RockTransactionEntry != null)
            {
                paymentInfo = new PaymentInfo();
                acAddress = (AddressControl)RockTransactionEntry.FindControl("acAddress");
                pnbPhone = (PhoneNumberBox)RockTransactionEntry.FindControl("pnbPhone");
                paymentInfo.Amount = GetSelectedAccounts().Sum(a => a.Amount);
                paymentInfo.Email = ((TextBox)RockTransactionEntry.FindControl("txtEmail")).Text;
                paymentInfo.Phone = PhoneNumber.FormattedNumber(pnbPhone.CountryCode, pnbPhone.Number, true);
                paymentInfo.Street1 = acAddress.Street1;
                paymentInfo.Street2 = acAddress.Street2;
                paymentInfo.City = acAddress.City;
                paymentInfo.State = acAddress.State;
                paymentInfo.PostalCode = acAddress.PostalCode;
                paymentInfo.Country = acAddress.Country;

            }
            else if (PageParameter("token") != String.Empty)
            {
                if (paymentInfo != null)
                {
                    return paymentInfo;
                }
                paymentInfo = new PaymentInfo();
                String errorMessage = string.Empty;
                paymentInfo = ((org.secc.PayPalExpress.Gateway)_payPalExpressGatewayComponent).GetPaymentInfo(_payPalExpressGateway, PageParameter("token"), out errorMessage);
                if (errorMessage != string.Empty)
                {
                    ShowMessage(NotificationBoxType.Danger, "PayPal Error", errorMessage);
                }
                return paymentInfo;

            }

            return paymentInfo;
        }

        /// <summary>
        /// Gets the accounts.
        /// </summary>
        private void GetAccounts()
        {
            var rockContext = new RockContext();
            var selectedGuids = GetAttributeValues("Accounts").Select(Guid.Parse).ToList();
            bool showAll = !selectedGuids.Any();

            bool additionalAccounts = GetAttributeValue("AdditionalAccounts").AsBoolean(true);

            SelectedAccounts = new List<AccountItem>();
            AvailableAccounts = new List<AccountItem>();

            // Enumerate through all active accounts that are public
            foreach (var account in new FinancialAccountService(rockContext).Queryable()
                .Where(f =>
                   f.IsActive &&
                   f.IsPublic.HasValue &&
                   f.IsPublic.Value &&
                   (f.StartDate == null || f.StartDate <= RockDateTime.Today) &&
                   (f.EndDate == null || f.EndDate >= RockDateTime.Today))
                .OrderBy(f => f.Order))
            {
                var accountItem = new AccountItem(account.Id, account.Order, account.Name, account.CampusId, account.PublicName);
                if (showAll)
                {
                    SelectedAccounts.Add(accountItem);
                }
                else
                {
                    if (selectedGuids.Contains(account.Guid))
                    {
                        SelectedAccounts.Add(accountItem);
                    }
                    else
                    {
                        if (additionalAccounts)
                        {
                            AvailableAccounts.Add(accountItem);
                        }
                    }
                }
            }
        }


        private void SaveTransaction(FinancialGateway financialGateway, GatewayComponent gateway, Person person, PaymentInfo paymentInfo, FinancialTransaction transaction, RockContext rockContext)
        {
            var txnChanges = new List<string>();
            txnChanges.Add("Created Transaction");

            History.EvaluateChange(txnChanges, "Transaction Code", string.Empty, transaction.TransactionCode);

            transaction.AuthorizedPersonAliasId = person.PrimaryAliasId;
            History.EvaluateChange(txnChanges, "Person", string.Empty, person.FullName);

            transaction.TransactionDateTime = RockDateTime.Now;
            History.EvaluateChange(txnChanges, "Date/Time", null, transaction.TransactionDateTime);

            transaction.FinancialGatewayId = financialGateway.Id;
            History.EvaluateChange(txnChanges, "Gateway", string.Empty, financialGateway.Name);

            var txnType = DefinedValueCache.Read(new Guid(Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION));
            transaction.TransactionTypeValueId = txnType.Id;
            History.EvaluateChange(txnChanges, "Type", string.Empty, txnType.Value);

            transaction.Summary = paymentInfo.Comment1;
            History.EvaluateChange(txnChanges, "Summary", string.Empty, transaction.Summary);

            if (transaction.FinancialPaymentDetail == null)
            {
                transaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }
            transaction.FinancialPaymentDetail.SetFromPaymentInfo(paymentInfo, gateway, rockContext, txnChanges);

            Guid sourceGuid = Guid.Empty;
            if (Guid.TryParse(GetAttributeValue("Source"), out sourceGuid))
            {
                var source = DefinedValueCache.Read(sourceGuid);
                if (source != null)
                {
                    transaction.SourceTypeValueId = source.Id;
                    History.EvaluateChange(txnChanges, "Source", string.Empty, source.Value);
                }
            }

            foreach (var account in GetSelectedAccounts().Where(a => a.Amount > 0))
            {
                var transactionDetail = new FinancialTransactionDetail();
                transactionDetail.Amount = account.Amount;
                transactionDetail.AccountId = account.Id;
                transaction.TransactionDetails.Add(transactionDetail);
                // Put a breakdown of the details into the transaction summary column.
                if (transaction.Summary == null)
                {
                    transaction.Summary = "";
                }
                if (transaction.Summary.Length > 0)
                {
                    transaction.Summary += " ";
                }
                transaction.Summary += "F" + account.Id + ":" + account.Amount.FormatAsCurrency();
                History.EvaluateChange(txnChanges, account.Name, 0.0M.FormatAsCurrency(), transactionDetail.Amount.FormatAsCurrency());
            }

            var batchService = new FinancialBatchService(rockContext);

            // Get the batch
            var batch = batchService.Get(
                GetAttributeValue("BatchNamePrefix"),
                paymentInfo.CurrencyTypeValue,
                paymentInfo.CreditCardTypeValue,
                transaction.TransactionDateTime.Value,
                financialGateway.GetBatchTimeOffset());

            var batchChanges = new List<string>();

            if (batch.Id == 0)
            {
                batchChanges.Add("Generated the batch");
                History.EvaluateChange(batchChanges, "Batch Name", string.Empty, batch.Name);
                History.EvaluateChange(batchChanges, "Status", null, batch.Status);
                History.EvaluateChange(batchChanges, "Start Date/Time", null, batch.BatchStartDateTime);
                History.EvaluateChange(batchChanges, "End Date/Time", null, batch.BatchEndDateTime);
            }

            decimal newControlAmount = batch.ControlAmount + transaction.TotalAmount;
            History.EvaluateChange(batchChanges, "Control Amount", batch.ControlAmount.FormatAsCurrency(), newControlAmount.FormatAsCurrency());
            batch.ControlAmount = newControlAmount;

            transaction.BatchId = batch.Id;
            batch.Transactions.Add(transaction);

            rockContext.SaveChanges();

            HistoryService.SaveChanges(
                rockContext,
                typeof(FinancialBatch),
                Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                batch.Id,
                batchChanges
            );

            HistoryService.SaveChanges(
                rockContext,
                typeof(FinancialBatch),
                Rock.SystemGuid.Category.HISTORY_FINANCIAL_TRANSACTION.AsGuid(),
                batch.Id,
                txnChanges,
                person.FullName,
                typeof(FinancialTransaction),
                transaction.Id
            );

            SendReceipt(transaction.Id);

            TransactionCode = transaction.TransactionCode;
        }

        private void SendReceipt(int transactionId)
        {
            Guid? recieptEmail = GetAttributeValue("ReceiptEmail").AsGuidOrNull();
            if (recieptEmail.HasValue)
            {
                // Queue a transaction to send reciepts
                var newTransactionIds = new List<int> { transactionId };
                var sendPaymentRecieptsTxn = new Rock.Transactions.SendPaymentReciepts(recieptEmail.Value, newTransactionIds);
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue(sendPaymentRecieptsTxn);
            }
        }

        /// <summary>
        /// Gets the person.
        /// </summary>
        /// <param name="paymentInfo">The paymentInfo object to use as the base.</param>
        /// <param name="create">if set to <c>true</c> [create].</param>
        /// <returns></returns>
        private Person GetPerson(PaymentInfo paymentInfo, bool create)
        {
            Person person = null;
            var rockContext = new RockContext();
            var personService = new PersonService(rockContext);

            Group familyGroup = null;

            int personId = ViewState["PersonId"] as int? ?? 0;
            if (personId == 0 && _targetPerson != null)
            {
                personId = _targetPerson.Id;
            }

            if (personId != 0)
            {
                person = personService.Get(personId);
            }

            if (create)
            {
                if (person == null)
                {
                    // Check to see if there's only one person with same email, first name, and last name
                    if (!string.IsNullOrWhiteSpace(paymentInfo.Email) &&
                        !string.IsNullOrWhiteSpace(paymentInfo.FirstName) &&
                        !string.IsNullOrWhiteSpace(paymentInfo.LastName))
                    {
                        // Same logic as CreatePledge.ascx.cs
                        var personMatches = personService.GetByMatch(paymentInfo.FirstName, paymentInfo.LastName, paymentInfo.Email);
                        if (personMatches.Count() == 1)
                        {
                            person = personMatches.FirstOrDefault();
                        }
                        else
                        {
                            person = null;
                        }
                    }

                    if (person == null)
                    {
                        DefinedValueCache dvcConnectionStatus = DefinedValueCache.Read(GetAttributeValue("ConnectionStatus").AsGuid());
                        DefinedValueCache dvcRecordStatus = DefinedValueCache.Read(GetAttributeValue("RecordStatus").AsGuid());

                        // Create Person
                        person = new Person();
                        person.FirstName = paymentInfo.FirstName;
                        person.LastName = paymentInfo.LastName;
                        person.IsEmailActive = true;
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid()).Id;
                        if (dvcConnectionStatus != null)
                        {
                            person.ConnectionStatusValueId = dvcConnectionStatus.Id;
                        }

                        if (dvcRecordStatus != null)
                        {
                            person.RecordStatusValueId = dvcRecordStatus.Id;
                        }

                        // Create Person/Family
                        familyGroup = PersonService.SaveNewPerson(person, rockContext, null, false);
                    }

                    ViewState["PersonId"] = person != null ? person.Id : 0;
                }
            }

            if (create && person != null) // person should never be null at this point
            {
                person.Email = paymentInfo.Email;

                if (GetAttributeValue("DisplayPhone").AsBooleanOrNull() ?? false && !String.IsNullOrEmpty(paymentInfo.Phone))
                {
                    var numberTypeId = DefinedValueCache.Read(new Guid(Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME)).Id;
                    var phone = person.PhoneNumbers.FirstOrDefault(p => p.NumberTypeValueId == numberTypeId);
                    if (phone == null)
                    {
                        phone = new PhoneNumber();
                        person.PhoneNumbers.Add(phone);
                        phone.NumberTypeValueId = numberTypeId;
                    }
                    phone.Number = paymentInfo.Phone;
                }

                if (familyGroup == null)
                {
                    var groupLocationService = new GroupLocationService(rockContext);
                    if (GroupLocationId.HasValue)
                    {
                        familyGroup = groupLocationService.Queryable()
                            .Where(gl => gl.Id == GroupLocationId.Value)
                            .Select(gl => gl.Group)
                            .FirstOrDefault();
                    }
                    else
                    {
                        familyGroup = personService.GetFamilies(person.Id).FirstOrDefault();
                    }
                }

                rockContext.SaveChanges();

                if (familyGroup != null)
                {
                    GroupService.AddNewGroupAddress(
                        rockContext,
                        familyGroup,
                        GetAttributeValue("AddressType"),
                        paymentInfo.Street1, paymentInfo.Street2, paymentInfo.City, paymentInfo.State, paymentInfo.PostalCode, paymentInfo.Country,
                        true);
                }
            }

            return person;
        }


        private void ShowSuccess(GatewayComponent gatewayComponent, Person person, PaymentInfo paymentInfo, PaymentSchedule schedule, FinancialPaymentDetail paymentDetail, RockContext rockContext)
        {
            tdTransactionCodeReceipt.Description = TransactionCode;
            tdTransactionCodeReceipt.Visible = !string.IsNullOrWhiteSpace(TransactionCode);

            tdScheduleId.Description = ScheduleId.ToString();
            tdScheduleId.Visible = ScheduleId.HasValue;

            tdNameReceipt.Description = paymentInfo.FullName;
            tdPhoneReceipt.Description = paymentInfo.Phone;
            tdEmailReceipt.Description = paymentInfo.Email;
            tdAddressReceipt.Description = string.Format("{0} {1}, {2} {3}", paymentInfo.Street1, paymentInfo.City, paymentInfo.State, paymentInfo.PostalCode);

            rptAccountListReceipt.DataSource = GetSelectedAccounts().Where(a => a.Amount != 0);
            rptAccountListReceipt.DataBind();

            tdTotalReceipt.Description = paymentInfo.Amount.ToString("C");

            tdPaymentMethodReceipt.Description = paymentInfo.CurrencyTypeValue.Description;

            string acctNumber = paymentInfo.MaskedNumber;
            if (string.IsNullOrWhiteSpace(acctNumber) && paymentDetail != null && !string.IsNullOrWhiteSpace(paymentDetail.AccountNumberMasked))
            {
                acctNumber = paymentDetail.AccountNumberMasked;
            }
            tdAccountNumberReceipt.Description = acctNumber;
            tdAccountNumberReceipt.Visible = !string.IsNullOrWhiteSpace(acctNumber);

            tdWhenReceipt.Description = schedule != null ? schedule.ToString() : "Today";

            pnlConfirmation.Visible = false;
            pnlSuccess.Visible = true;
        }


    }
}
