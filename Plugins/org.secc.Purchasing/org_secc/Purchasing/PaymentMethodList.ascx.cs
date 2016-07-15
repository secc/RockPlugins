using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;

using Rock.Web.UI;
using Rock;
using org.secc.Purchasing;
using Rock.Web.UI.Controls;
using org.secc.Purchasing.DataLayer;
using Rock.Model;
using Rock.Attribute;
using System.ComponentModel;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName("Payment Method List")]
    [Category("SECC > Purchasing")]
    [Description("List all payment methods in the SECC Purchasing system.")]

    [BooleanField("Show Active By Default", "Show only active payment methods by default.", true)]
    [IntegerField("Credit Card Expriation Date Year Options", "The number of years in the future to show for possible credit card expiration dates when adding a credit card payment method. Default is 5", false, 5)]

    public partial class PaymentMethodList : RockBlock
    {
        bool? mCanEdit;



        #region Module Settings
        public bool ActiveByDefaultSetting { get {return GetAttributeValue("ShowActiveByDefault").AsBoolean(); } }

        public int CreditCardExpirationYearOptionsSetting { get { return GetAttributeValue("CreditCardExpriationDateYearOptions").AsInteger(); } }


        #endregion

        #region Properties
        private int PaymentMethodID
        {
            get
            {
                int pmid = 0;
                if (String.IsNullOrEmpty(Request.QueryString["MethodID"]) || !int.TryParse(Request.QueryString["MethodID"], out pmid))
                    if (ViewState[BlockId + "_PaymentMethodID"] != null)
                        int.TryParse(ViewState[BlockId + "_PaymentMethodID"].ToString(), out pmid);

                return pmid;
            }
            set
            {
                if (value > 0)
                    ViewState[BlockId + "_PaymentMethodID"] = value;
                else
                    ViewState.Remove(BlockId + "_PaymentMethodID");
            }
        }

        private bool CanEdit
        {
            get
            {
                if (mCanEdit == null)
                    mCanEdit = UserCanEdit;
                return (bool)mCanEdit;
            }
        }
        #endregion

        #region page events
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                SetDefaults();
                LoadListView();
            } 
            ClearStatus();
        }

        protected override void OnInit(EventArgs e)
        {
            dgPayMethods.Actions.AddClick += dgPayMethods_AddItem;
            dgPayMethods.RowCommand += new GridViewCommandEventHandler(dgPayMethods_ItemCommand);
            dgPayMethods.GridRebind += new GridRebindEventHandler(dgPayMethods_ReBind);

            base.OnInit(e);
        }

        protected void btnApplyFilter_Click(object sender, EventArgs e)
        {
            LoadPaymentMethods();
        }

        protected void btnClearFilter_Click(object sender, EventArgs e)
        {
            ClearFilters();
            LoadPaymentMethods();
        }

        protected void btnEdit_Click(object sender, EventArgs e)
        {
            switch (((Button)sender).CommandName.ToLower())
            {
                case "edit":
                case "save":
                    if (SavePaymentMethod())
                    {
                        LoadDetailView(true);
                        SetStatus(false, "Payment Method Saved.");
                    }
                    break;
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ClearForm();
            PopulateEditControls();
        }

        protected void btnReturn_Click(object sender, EventArgs e)
        {
            PaymentMethodID = 0;
            ClearForm();
            pnlDetail.Hide();
        }

        protected void chkActiveOnly_Changed(object sender, EventArgs e)
        {
            LoadPaymentMethods();
        }

        protected void tglCreditCard_Changed(object sender, EventArgs e)
        {
            bool showCreditCard = ((Toggle)sender).Checked;
            LoadDetailView(true);
            pnlCC.Visible = showCreditCard;
            ((Toggle)sender).Checked = showCreditCard;
        }

        protected void dgPayMethods_AddItem(object sender, EventArgs e)
        {
            PaymentMethodID = 0;
            if (CanEdit)
                LoadDetailView(true);
        }

        protected void dgPayMethods_ItemCommand(object sender, GridViewCommandEventArgs e)
        {
            int PMID = 0;
            if (e.CommandArgument != null && int.TryParse(e.CommandArgument.ToString(), out PMID))
            {
                PaymentMethodID = PMID;
                LoadDetailView(false);
            }
        }

        protected void dgPayMethods_ReBind(object sender, EventArgs e)
        {
            LoadPaymentMethods();
        }
        #endregion

        #region private

        public void BuildExpirationDateDropDowns()
        {
            ddlExpMonth.Items.Clear();
            ddlExpMonth.DataSource = GetMonthList();
            ddlExpMonth.DataValueField = "Key";
            ddlExpMonth.DataTextField = "Value";
            ddlExpMonth.DataBind();
            ddlExpMonth.Items.Insert(0, new ListItem("Month", ""));
            ddlExpMonth.SelectedIndex = 0;

            ddlExpYear.Items.Clear();
            ddlExpYear.DataSource = GetYears();
            ddlExpYear.DataValueField = "Key";
            ddlExpYear.DataTextField = "Value";
            ddlExpYear.DataBind();
            ddlExpYear.Items.Insert(0, new ListItem("Year", ""));
            ddlExpYear.SelectedIndex = 0;

        }

        public DataTable BuildDataTable(List<PaymentMethod> pmList)
        {
            DataTable dt = new DataTable("PaymentMethods");

            //add columns
            dt.Columns.Add(new DataColumn("PaymentMethodID", typeof(int)));
            dt.Columns.Add(new DataColumn("Name", typeof(string)));
            dt.Columns.Add(new DataColumn("Description", typeof(string)));
            dt.Columns.Add(new DataColumn("IsCreditCard", typeof(bool)));
            dt.Columns.Add(new DataColumn("Owner", typeof(string)));
            dt.Columns.Add(new DataColumn("ExpirationDate", typeof(string)));
            dt.Columns.Add(new DataColumn("Active", typeof(bool)));
            dt.Columns.Add(new DataColumn("ActiveString", typeof(string)));


            //add data
            foreach (PaymentMethod p in pmList)
            {
                DataRow dr = dt.NewRow();
                dr["PaymentMethodID"] = p.PaymentMethodID;
                dr["Name"] = p.Name;
                dr["Description"] = p.Description;

                if (p.CreditCard != null)
                {
                    dr["IsCreditCard"] = true;
                    dr["Owner"] = p.CreditCard.Cardholder.FullName;
                    dr["ExpirationDate"] = string.Format("{0:MMMM yyyy}", p.CreditCard.CardExpirationDate);
                }
                else
                {
                    dr["IsCreditCard"] = false;
                    dr["Owner"] = null;
                    dr["ExpirationDate"] = null;
                }
                dr["Active"] = p.Active;

                if (p.Active)
                    dr["ActiveString"] = "T";
                else
                    dr["ActiveString"] = "F";

                dt.Rows.Add(dr);
            }

            return dt;
        }

        private void ClearForm()
        {
            ClearStatus();
            //Clear View Fields
            lblName.Text = "&nbsp";
            lblDescription.Text = "&nbsp";
            lblAccountOwner.Text = "&nbsp";
            lblAccountLastFour.Text = "&nbsp";
            lblExpMonth.Text = "&nbsp";
            lblExpYear.Text = "&nbsp";

            //Clear Edit Fields
            txtName.Text = String.Empty;
            txtDescription.Text = String.Empty;
            tglCreditCard.Checked = false;
            pickerAccountOwner.PersonId = -1;
            txtAccountLastFour.Text = String.Empty;
            ddlExpMonth.SelectedIndex = 0;
            ddlExpYear.SelectedIndex = 0;
            chkActive.Checked = true;

            pnlCC.Visible = false;

        }

        private void ClearFilters()
        {
            txtFilterPaymentMethodName.Text = String.Empty;
            chkFilterCreditCard.Checked = false;
        }

        private void ConfigureGrid()
        {
            dgPayMethods.Visible = true;
            dgPayMethods.ItemType = "Payment Method";
            dgPayMethods.AllowSorting = true;
        }

        
        
        private void LoadPaymentMethods()
        {
            ConfigureGrid();

            List<PaymentMethod> PayMethods = PaymentMethod.GetPaymentMethods(chkActiveOnly.Checked).OrderBy(x => x.Name).ToList();

            SortProperty sortProperty = dgPayMethods.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    PayMethods = PayMethods.OrderBy( r => r.GetType().GetProperty( sortProperty.Property ) != null?r.GetType().GetProperty( sortProperty.Property ).GetValue( r ):null ).ToList();
                }
                else
                {
                    PayMethods = PayMethods.OrderByDescending( r => r.GetType().GetProperty( sortProperty.Property ) != null?r.GetType().GetProperty( sortProperty.Property ).GetValue( r ):null ).ToList();
                }
            }
            else
            {
                PayMethods = PayMethods.OrderBy( pm => pm.PaymentMethodID ).ToList();
            }

            if (!String.IsNullOrEmpty(txtFilterPaymentMethodName.Text))
                PayMethods.RemoveAll(p => !(p.Name.ToLower().Contains(txtFilterPaymentMethodName.Text.ToLower())));
            if (chkFilterCreditCard.Checked)
                PayMethods.RemoveAll(p => p.CreditCardID == 0);

            DataTable dt = BuildDataTable(PayMethods);
            dgPayMethods.DataSource = dt;
            dgPayMethods.DataBind();
        }

        private bool SavePaymentMethod()
        {
            bool Success = false;
            try
            {
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Payment Method is not valid", ValErrors);

                PaymentMethod p = null;

                if (PaymentMethodID > 0)
                    p = new PaymentMethod(PaymentMethodID);
                else
                    p = new PaymentMethod();

                p.Name = txtName.Text.Trim();

                p.Description = txtDescription.Text.Trim();
                p.Active = chkActive.Checked;

                if (tglCreditCard.Checked)
                {
                    if (p.CreditCard == null)
                        p.CreditCard = new CreditCard();
                    p.CreditCard.CardLastFour = txtAccountLastFour.Text.Trim();
                    if (pickerAccountOwner.PersonAliasId.HasValue)
                    {
                        p.CreditCard.CardHolderID = pickerAccountOwner.PersonAliasId.Value;
                    }
                    p.CreditCard.CardExpirationDate = GetExpirationDate();
                    p.Active = chkActive.Checked;
                }
                else
                {
                    p.CreditCardID = 0;
                    p.CreditCard = null;
                }

                p.Save(CurrentUser.UserName);

                if (PaymentMethodID == 0)
                    PaymentMethodID = p.PaymentMethodID;

                Success = true;
            }
            catch (RequisitionNotValidException rEx)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<ul type=\"disc\">");
                foreach (var item in rEx.InvalidProperties)
                {
                    sb.AppendFormat("<li>{0}-{1}</li>", item.Key, item.Value);
                }
                sb.Append("</ul>");
                SetStatus(true, sb.ToString());
                Success = false;
            }

            return Success;
        }

        private void SetDefaults()
        {
            chkActiveOnly.Checked = ActiveByDefaultSetting;
            ClearFilters();
            BuildExpirationDateDropDowns();
        }

        private void LoadListView()
        {
            LoadPaymentMethods();
        }

        private void LoadDetailView(bool isEditable)
        {
            lblStatus.InnerText = "";
            pnlDetail.Show();
            //pnlList.Visible = false;

            if (!CanEdit && isEditable)

                isEditable = false;

            //view controls
            lblName.Visible = !isEditable;
            lblDescription.Visible = !isEditable;
            lblCreditCard.Visible = !isEditable;
            lblAccountOwner.Visible = !isEditable;
            lblAccountLastFour.Visible = !isEditable;
            lblExpMonth.Visible = !isEditable;
            lblExpYear.Visible = !isEditable;
          

            //edit controls
            txtName.Visible = isEditable;
            txtDescription.Visible = isEditable;
            tglCreditCard.Visible = isEditable;
            pickerAccountOwner.Visible = isEditable;
            txtAccountLastFour.Visible = isEditable;
            ddlExpMonth.Visible = isEditable;
            ddlExpYear.Visible = isEditable;
            chkActive.Enabled = isEditable;
            


            if (isEditable)
            {
                btnEdit.Text = "Save";
                btnEdit.CommandName = "Save";
                btnEdit.Visible = true;
                hdnPaymentMethodId.Value = PaymentMethodID.ToString();

                btnReset.Visible = true;
                PopulateEditControls();
            }
            else
            {
                btnEdit.Visible = false;
                btnReset.Visible = false;
                PopulateViewControls();
                
            }
            
        }

        private void PopulateViewControls()
        {
            if (PaymentMethodID == 0)
                return;

            PaymentMethod p = new PaymentMethod(PaymentMethodID);
            lblName.Text = p.Name;

            if (!String.IsNullOrEmpty(p.Description))
            {
                lblDescription.Text = p.Description;
            }

            lblCreditCard.Text = "No";
            if (p.CreditCard != null)
            {
                lblCreditCard.Text = "Yes";
                tglCreditCard.Checked = true;
                pnlCC.Visible = true;
                lblAccountLastFour.Text = p.CreditCard.CardLastFour;

                if (p.CreditCard.Cardholder != null)
                {
                    lblAccountOwner.Text = p.CreditCard.Cardholder.FullName;
                }
                else
                {
                    lblAccountOwner.Text = "(not set)";
                }
                lblExpMonth.Text = string.Format("{0:MMMM}", p.CreditCard.CardExpirationDate);
                lblExpYear.Text = p.CreditCard.CardExpirationDate.Year.ToString();
            }
            else
            {
                pnlCC.Visible = false;
                tglCreditCard.Checked = false;
            }
        }

        private void PopulateEditControls()
        {
            if (PaymentMethodID == 0)
            {
                return;
            }

            PaymentMethod p = new PaymentMethod(PaymentMethodID);

            txtName.Text = p.Name;
            txtDescription.Text = p.Description;
            chkActive.Checked = p.Active;

            if (p.CreditCard != null)
            {
                tglCreditCard.Checked = true;
                pnlCC.Visible = true;
                txtAccountLastFour.Text = p.CreditCard.CardLastFour;

                if (p.CreditCard.Cardholder != null)
                {
                    pickerAccountOwner.PersonId = p.CreditCard.Cardholder.Id;
                    pickerAccountOwner.PersonName = p.CreditCard.Cardholder.FullName;
                }

                ddlExpMonth.SelectedValue = p.CreditCard.CardExpirationDate.Month.ToString();

                if (ddlExpYear.Items.FindByValue(p.CreditCard.CardExpirationDate.Year.ToString()) != null)
                    ddlExpYear.SelectedValue = p.CreditCard.CardExpirationDate.Year.ToString();
                else
                {
                    var years = GetYears();
                    years.Add(p.CreditCard.CardExpirationDate.Year, p.CreditCard.CardExpirationDate.Year);
                    ddlExpYear.DataSource = years;
                    ddlExpYear.DataBind();
                    ddlExpYear.SelectedValue = p.CreditCard.CardExpirationDate.Year.ToString();
                }
            }
            else
            {
                tglCreditCard.Checked = false;
                pnlCC.Visible = false;
            }
        }


        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (String.IsNullOrEmpty(txtName.Text))
                ValErrors.Add("Name", "Name is required.");
            if (PaymentMethodID == 0 && PaymentMethod.GetPaymentMethods(txtName.Text.Trim(), false).Count > 0)
            {
                ValErrors.Add("Name", "A payment method already exists with the name " + txtName.Text.Trim());
            }
            if (tglCreditCard.Checked)
            {
                if (string.IsNullOrEmpty(txtAccountLastFour.Text))
                    ValErrors.Add("Account Last 4", "Last 4 numbers of account number is required.");
                else if (!System.Text.RegularExpressions.Regex.IsMatch(txtAccountLastFour.Text.Trim(), @"^\d{4}$"))
                    ValErrors.Add("Account Last 4", "Last four numbers of account number must be numeric.");

                if (pickerAccountOwner.PersonAliasId <= 0)
                    ValErrors.Add("Account Owner", "Account Owner is required.");

                if (ddlExpMonth.Text == "Month" || ddlExpYear.Text == "Year")
                    ValErrors.Add("Expiration Date", "Expiration Date is required.");

                if (PaymentMethodID == 0 && GetExpirationDate() < DateTime.Now.Date)
                    ValErrors.Add("Expiration Date", "Expiration Date must be in the future.");
                if (GetExpirationDate() == DateTime.MinValue)
                    ValErrors.Add("Expiration Date", "Expiration Date is not valid.");

            }

            return ValErrors;
        }

        private Dictionary<int, string> GetMonthList()
        {
            Dictionary<int, string> Months = new Dictionary<int, string>();
            Months.Add(1, "January");
            Months.Add(2, "February");
            Months.Add(3, "March");
            Months.Add(4, "April");
            Months.Add(5, "May");
            Months.Add(6, "June");
            Months.Add(7, "July");
            Months.Add(8, "August");
            Months.Add(9, "September");
            Months.Add(10, "October");
            Months.Add(11, "November");
            Months.Add(12, "December");

            return Months;
        }

        private Dictionary<int,int> GetYears()
        {
            Dictionary<int, int> Years = new Dictionary<int, int>();
            int Year = DateTime.Now.Year;

            for (int i = 0; i < CreditCardExpirationYearOptionsSetting; i++)
            {
                Years.Add(Year, Year);
                Year++;
            }
            return Years;
        }

        private DateTime GetExpirationDate()
        {
            DateTime ExpirationDate = DateTime.MinValue;
            int Year;
            if (ddlExpMonth.SelectedIndex > 0 && ddlExpYear.SelectedIndex > 0)
            {
                ExpirationDate = new DateTime(int.Parse(ddlExpYear.SelectedValue), int.Parse(ddlExpMonth.SelectedValue), 1).AddMonths(1).AddDays(-1);
            }
            else if (ddlExpMonth.SelectedIndex > 0 && ddlExpYear.SelectedIndex == 0 && int.TryParse(ddlExpYear.Text, out Year))
                ExpirationDate = new DateTime(Year, int.Parse(ddlExpMonth.SelectedValue), 1).AddMonths(1).AddDays(-1);

            return ExpirationDate;
        }

        private void SetStatus(bool isError, string message)
        {
            lblStatus.Visible = true;
            lblStatus.InnerHtml = message;

            if (isError)
            {
                lblStatus.RemoveCssClass("alert-success");
                lblStatus.AddCssClass("alert alert-danger");
            }
            else
            {
                lblStatus.RemoveCssClass("alert-danger");
                lblStatus.AddCssClass("alert alert-success");
            }
        }

        private void ClearStatus()
        {
            lblStatus.Visible = false;
            lblStatus.InnerText = String.Empty;
            lblStatus.RemoveCssClass("alert");
            lblStatus.RemoveCssClass("alert-danger");
            lblStatus.RemoveCssClass("alert-success");
        }

        #endregion

        protected void Edit_Click(object sender, RowEventArgs e)
        {
            PaymentMethodID = e.RowKeyId;
            if (CanEdit)
                LoadDetailView(true);
        }
}



    public enum PaymentMethodVisibility
	{
        List,
        View,
        Edit            
	}
}