using org.secc.PayPalReporting.Model;
using System;
using System.ComponentModel;
using Rock;

namespace RockWeb.Plugins.org_secc.PayPalReporting { 

    [DisplayName("PayPal Reporting Transaction")]
    [Category("SECC > Finance")]
    [Description("Provides a way to view and edit transactions in the Custom PayPal Reporting table.")]
    public partial class Transaction : Rock.Web.UI.RockBlock
    {
        int transactionId = 0;
        protected void Page_Load(object sender, EventArgs e)
        {
            transactionId = PageParameter("TransactionId").AsInteger();
            if (transactionId > 0)
            {
                ShowDetail();
            }
        }

        private void ShowDetail()
        {
            pnlDetails.Visible = true;
            btnSave.Visible = false;
            TransactionService transactionService = new TransactionService(new org.secc.PayPalReporting.Data.PayPalReportingContext());
            org.secc.PayPalReporting.Model.Transaction txn = transactionService.Get(transactionId);
            lGatewayTransactionId.Text = txn.GatewayTransactionId;
            lMerchantTransactionId.Text = txn.MerchantTransactionId;
            lBillingFirstName.Text = txn.BillingFirstName;
            lBillingLastName.Text = txn.BillingLastName;
            lAmount.Text = txn.Amount.ToString();
            lFees.Text = txn.Fees.ToString();
            lTimeCreated.Text = txn.TimeCreated.ToString();
            lTenderType.Text = txn.TenderType;
            lType.Text = txn.Type;
            lComment1.Text = txn.Comment1;
            lComment2.Text = txn.Comment2;
            lBatchId.Text = txn.BatchId.ToString();
            lIsZeroFee.Text = txn.IsZeroFee.ToString();
        }

        protected void Edit_Click(object sender, EventArgs e)
        {
            pnlDetails.Visible = false;
            btnEdit.Visible = false;
            pnlEdit.Visible = true;
            btnSave.Visible = true;

            TransactionService transactionService = new TransactionService(new org.secc.PayPalReporting.Data.PayPalReportingContext());
            org.secc.PayPalReporting.Model.Transaction txn = transactionService.Get(transactionId);
            hdnTransactionId.Value = txn.Id.ToString();
            tbGatewayTransactionId.Text = txn.GatewayTransactionId;
            tbMerchantTransactionId.Text = txn.MerchantTransactionId;
            tbBillingFirstName.Text = txn.BillingFirstName;
            tbBillingLastName.Text = txn.BillingLastName;
            tbAmount.Text = txn.Amount.ToString();
            tbFees.Text = txn.Fees.ToString();
            tbTimeCreated.SelectedDateTime = txn.TimeCreated;
            tbTenderType.Text = txn.TenderType;
            tbType.Text = txn.Type;
            tbComment1.Text = txn.Comment1;
            tbComment2.Text = txn.Comment2;
            tbBatchId.Text = txn.BatchId.ToString();
            tbIsZeroFee.Checked = txn.IsZeroFee;
        }

        protected void Save_Click(object sender, EventArgs e)
        {

            var dbContext = new org.secc.PayPalReporting.Data.PayPalReportingContext();
            TransactionService transactionService = new TransactionService(dbContext);
            org.secc.PayPalReporting.Model.Transaction txn = transactionService.Get(hdnTransactionId.Value.AsInteger());
            txn.GatewayTransactionId = tbGatewayTransactionId.Text;
            txn.MerchantTransactionId = tbMerchantTransactionId.Text;
            txn.BillingFirstName = tbBillingFirstName.Text;
            txn.BillingLastName = tbBillingLastName.Text;
            txn.Amount = tbAmount.Text.AsDouble();
            txn.Fees = tbFees.Text.AsDouble();
            txn.TimeCreated = tbTimeCreated.SelectedDateTime;
            txn.TenderType = tbTenderType.Text;
            txn.Type = tbType.Text;
            txn.Comment1 = tbComment1.Text;
            txn.Comment2 = tbComment2.Text;
            txn.BatchId = tbBatchId.Text.AsInteger();
            txn.IsZeroFee = tbIsZeroFee.Checked;

            dbContext.SaveChanges();

            pnlDetails.Visible = true;
            btnEdit.Visible = true;
            pnlEdit.Visible = false;
            btnSave.Visible = false;

            ShowDetail();
        }
    }
}