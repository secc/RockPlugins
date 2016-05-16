using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using org.secc.Purchasing;
using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Model;
using org.secc.PDF;

namespace RockWeb.Plugins.org_secc.Purchasing
{

    [DisplayName("Purchase Order Detail")]
    [Category("SECC > Purchasing")]
    [Description("Manage a single Purchase Order.")]

    [TextField("Default Ship To Name", "Default value for Ship to (company) name. Setting defaults to Southeast Christian Church", false, "Southeast Christian Church", "Ship To")]
    [TextField("Default Ship To Attention", "Default value for Ship To Attention. Setting defaults to (blank)", false, "", "Ship To")]
    [CampusField("Default Ship To Campus", "Default ship to campus. Setting defaults to 920 campus.", false, "1", "Ship To")]
    [BooleanField("Show Inactive Vendors", "Show Inactive Vendors by default. Setting defaults to false", false, "Vendors")]
    [LinkedPage("Purchase Order List Page", "Purchase Order List Page", true)]
    [LinkedPage("Requisition Detail Page", "Requisition Detail Page", true)]
    [IntegerField("Ministry Person Attribute ID", "Ministry Person AttributeID", true)]
    [CodeEditorField("PDF Report Lava", "HTML/Lava for generating the PDF Report", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, true,"{% include '~/Plugins/org_secc/Purchasing/PurchaseOrderPDF.lava' %}", "PDF Report")]
    [WorkflowTypeField("PDF Report Workflow", "The workflow type to activate to merge the output PDF", false, false, null, "PDF Report")]
    [TextField("PDF Report Activity", "The workflow activity to merge the output PDF", false, "Lava", "PDF Report")]
    [GroupField("Receiving User Group", "Group that contains list of staff/volunteers who can receive items into purchasing/receiving.", true)]
    [LinkedPage("Person Detail Page", "Person Detail Page", false, "7", "Staff Selector")]

    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Ministry Area Person Attribute", "The person attribute that stores the user's Ministry Area.", false, false, null, "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Position Person Attribute", "The person attribute that stores the user's job position.", false, false, null, "Staff Selector")]
    public partial class PODetail : RockBlock
    {
        #region Fields
        protected PurchaseOrder mPurchaseOrder;
        private PersonAliasService personAliasService;
        private DefinedValueService definedValueService;
        private CampusService campusService;
        private AttributeService attributeService;
        private Rock.Data.RockContext rockContext;
        #endregion

        #region Module Settings
        public string DefaultShipToNameSetting { get { return GetAttributeValue("DefaultShipToName"); } }

        public string DefaultShipToAttentionSetting { get { return GetAttributeValue("DefaultShipToAttention"); } }

        public int DefaultShipToCampusSetting
        {
            get
            {
                return GetAttributeValue("DefaultShipToCampus").AsInteger();
            }
        }

        public bool ShowInactiveVendorsSetting
        {
            get
            {
                return GetAttributeValue("ShowInactiveVendor").AsBoolean();
            }
        }

        public string PurchaseOrderListPageSetting { get { return GetAttributeValue("PurchaseOrderListPage"); } }

        public string RequisitionDetailPageSetting { get { return GetAttributeValue("RequisitionDetailPage"); } }


        public Guid MinistryPersonAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("MinistryPersonAttributeID").AsGuid();
            }
        }

        public int ReceivingUserTagSetting
        {
            get
            {
                Guid? selectedTag = GetAttributeValue("ReceivingUserGroup").AsGuidOrNull();
                if (selectedTag.HasValue) {
                    GroupService groupService = new GroupService(rockContext);
                    return groupService.Get(selectedTag.Value).Id;
                }
                return 0;
            }
        }

        public string PersonDetailPageSetting { get { return GetAttributeValue("PersonDetailPage"); } }


        public Guid MinistryAreaAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("MinistryAreaPersonAttribute").AsGuid();
            }
        }

        public Guid PositionAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("PositionPersonAttribute").AsGuid();
            }
        }
        #endregion

        #region Properties
        protected int PurchaseOrderID
        {
            get
            {
                int poID = 0;
                if (ViewState[BlockId + "_PurchaseOrderID"] != null)
                {
                    int.TryParse(ViewState[BlockId + "_PurchaseOrderID"].ToString(), out poID);
                }
                if (poID <= 0 && !String.IsNullOrEmpty(Request.QueryString["poid"]))
                {
                    int.TryParse(Request.QueryString["poid"], out poID);
                }

                return poID;
            }
            set
            {
                ViewState[BlockId + "_PurchaseOrderID"] = value;
            }
        }

        protected PurchaseOrder CurrentPurchaseOrder
        {
            get
            {
                if ( mPurchaseOrder == null && PurchaseOrderID > 0 )
                {
                    mPurchaseOrder = new PurchaseOrder( PurchaseOrderID );

                }

                //if ( mPurchaseOrder!= null &&  mPurchaseOrder.PurchaseOrderID == 0 )
                //    mPurchaseOrder = null;

                return mPurchaseOrder;
            }
            set
            {
                mPurchaseOrder = value;
            }
        }
        #endregion

        #region Page Events

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
             Button button = new Button()
            {
                ID = "btnPaymentMethodPaymentAdd",
                CssClass = "btn btn-primary",
                Text = "Add Payment"
            };
            button.Click += btnPaymentMethodPaymentAdd_Click;
            mpPayments.Footer.Controls.Add(button);
            
            button = new Button()
            {
                ID = "btnPaymentMethodPaymentAddCharges",
                CssClass = "btn btn-primary",
                Text = "Update Charges"
            };
            button.Click += btnPaymentMethodPaymentUpdateCharges_Click;
            mpPayments.Footer.Controls.Add(button);

            ucStaffSearch.MinistryAreaAttributeGuid = MinistryAreaAttributeIDSetting;
            ucStaffSearch.PositionAttributeGuid = PositionAttributeIDSetting;

            ucAttachments.Identifier = PurchaseOrderID;
            ucAttachments.ObjectTypeName = typeof(PurchaseOrder).ToString();
            ucAttachments.CurrentUser = CurrentUser;
        }

        protected override void OnInit(EventArgs e)
        {
            rockContext = new Rock.Data.RockContext();
            personAliasService = new PersonAliasService(rockContext);
            definedValueService = new DefinedValueService(rockContext);
            campusService = new CampusService(rockContext);
            attributeService = new AttributeService(rockContext);

            ucNotes.RefreshParent += new EventHandler(ucNotes_RefreshParent);
            ucNotes.CurrentUserName = CurrentUser.UserName;
            ucNotes.UserHasParentEditPermission = UserCanEdit;
            ucAttachments.RefreshParent += new EventHandler(ucAttachments_RefreshParent);

            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            SetSummaryError(String.Empty);
            if (!Page.IsPostBack)
            {
                hlinkPOAlert.NavigateUrl = string.Format("~/default.aspx?page={0}", PurchaseOrderListPageSetting);
                PurchaseOrderID = 0;
                BindPOTypes();
                BindDefaultPayMethods();
                LoadPO();
            }

            string baseUrl = String.Empty;
            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0)
            {
                baseUrl = string.Format("~/page/{0}?poid={1}", CurrentPageReference.PageId, CurrentPurchaseOrder.PurchaseOrderID);
            }
            else
            {
                baseUrl = string.Format("~/page/{0}", CurrentPageReference.PageId);
            }

            lnkAttachments.NavigateUrl = baseUrl + "#catAttachments";
            lnkNotes.NavigateUrl = baseUrl + "#catNotes";


            dgItems.ShowFooter = true;
            Page.PreRenderComplete += Page_PreRenderComplete;
        }

        void Page_PreRenderComplete(object sender, EventArgs e)
        {
            HtmlAnchor cancelLink = (HtmlAnchor)mpChooseRequisition.Footer.FindControl("cancelLink");
            cancelLink.InnerText = "Cancel";
            cancelLink.AddCssClass("btn-default");
            cancelLink.RemoveCssClass("btn-link");
            cancelLink = (HtmlAnchor)mpRequisitionItems.Footer.FindControl("cancelLink");
            cancelLink.InnerText = "Cancel";
            cancelLink.AddCssClass("btn-default");
            cancelLink.RemoveCssClass("btn-link");
            cancelLink = (HtmlAnchor)mpPayments.Footer.FindControl("cancelLink");
            cancelLink.InnerText = "Cancel";
            cancelLink.AddCssClass("btn-default");
            cancelLink.RemoveCssClass("btn-link");
            cancelLink = (HtmlAnchor)mpReceivePackage.Footer.FindControl("cancelLink");
            cancelLink.InnerText = "Cancel";
            cancelLink.AddCssClass("btn-default");
            cancelLink.RemoveCssClass("btn-link");
            cancelLink = (HtmlAnchor)mpShipToSelect.Footer.FindControl("cancelLink");
            cancelLink.InnerText = "Cancel";
            cancelLink.AddCssClass("btn-default");
            cancelLink.RemoveCssClass("btn-link");
            
        }

        protected override void OnPreRender(EventArgs e)
        {
            SetToolbarVisibility();
            base.OnPreRender(e);
        }

        protected void dgItems_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                bool IsEditable = CanUserEditItem() && (int)((DataRowView)e.Item.DataItem)["QuantityReceived"] == 0;
                LinkButton lbDetails = (LinkButton)e.Item.FindControl("lbDetails");
                LinkButton lbRemove = (LinkButton)e.Item.FindControl("lbRemove");

                lbRemove.Visible = IsEditable;
                lbRemove.CommandArgument = ((DataRowView)e.Item.DataItem)["POItemID"].ToString();
                lbDetails.CommandArgument = ((DataRowView)e.Item.DataItem)["POItemID"].ToString();
            }
        }

        protected void dgItems_ItemCommand(object sender, CommandEventArgs e)
        {
            int ItemID = 0;
            int.TryParse(e.CommandArgument.ToString(), out ItemID);
            switch (e.CommandName.ToLower())
            {
                case "details":
                    ShowItemDetailsModal(ItemID);
                    break;
                case "remove":
                    RemoveItemFromPO(ItemID);
                    break;
                default:
                    break;
            }
        }

        protected void dgPayments_ItemCommand(object sender, CommandEventArgs e)
        {
            int PaymentID = 0;
            int.TryParse(e.CommandArgument.ToString(), out PaymentID);
            switch (e.CommandName.ToLower())
            {
                case "details":
                    ShowPaymentDetailModal(PaymentID);
                    break;
                case "remove":
                    RemovePayment(PaymentID);
                    break;
            }
        }

        protected void dgPayments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                LinkButton lbDetails = (LinkButton)e.Row.FindControl("lbDetails");
                LinkButton lbRemove = (LinkButton)e.Row.FindControl("lbRemove");

                lbDetails.CommandArgument = drv["PaymentID"].ToString();
                lbRemove.CommandArgument = drv["PaymentID"].ToString();

                lbRemove.Visible = CanUserEditPayments();
            }
        }

        protected void dgReceivingHistory_ItemCommand(object sender, CommandEventArgs e)
        {
            int receiptId = 0;

            if(int.TryParse(e.CommandArgument.ToString(), out receiptId))
            {
                switch (e.CommandName.ToLower())
                {
                    case "showreceipt":
                        ShowReceivePackageModel(receiptId);
                        break;
                    case "removereceipt":
                        RemoveReceipt(receiptId);
                        break;  
                }
            }
        }

        protected void dgReceivingHistory_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                LinkButton lbShowReceipt = (LinkButton)e.Row.FindControl("lbShowReceipt");
                LinkButton lbRemoveReceipt = (LinkButton)e.Row.FindControl("lbRemoveReceipt");

                lbShowReceipt.CommandArgument = drv["ReceiptID"].ToString();
                lbRemoveReceipt.CommandArgument = drv["ReceiptID"].ToString();

                lbRemoveReceipt.Visible = !CurrentPurchaseOrder.IsClosed() && CurrentPurchaseOrder.Active;

            }
        }

        protected void lbChangeVendor_Click(object sender, EventArgs e)
        {
            ShowVendorModal();
        }

        protected void lbChangeShipTo_Click(object sender, EventArgs e)
        {
            ShowShipToModal();
        }

        protected void ToolbarItem_Click(object sender, EventArgs e)
        {
            LinkButton ToolbarLink = (LinkButton)sender;

            if (ToolbarLink.CommandName.ToLower() == "return")
            {
                RedirectToList();
                return;
            }

            //if (CurrentPurchaseOrder == null || CurrentPurchaseOrder.HasChanged())
            //{
            //    SetSummaryError(String.Empty);
            //    if (!SaveSummary())
            //        return;
            //}

            if(!SaveSummary())
                return;

            switch (ToolbarLink.CommandName.ToLower())
            {
                case "additem":
                    ShowRequisitionModal();
                    break;
                case "addnote":
                    ShowAddNoteDialog();
                    break;
                case "addattachment":
                    ShowAddAttachmentDialog();
                    break;
                case "order":
                    ShowOrderSubmitModal();
                    break;
                case "receive":
                    ShowReceivePackageModel(0);
                    break;
                case "addpayment":
                    ShowPaymentDetailModal(0);
                    break;
                case "markasbilled":
                    MarkAsBilled();
                    break;
                case "close":
                    ClosePO();
                    break;
                case "reopen":
                    ReopenPO();
                    break;
                case "print":
                    ShowPrintPOPopup();
                    break;
                case "cancel":
                    CancelPO();
                    break;
                
            }
        }

        protected void ucAttachments_RefreshParent(object sender, EventArgs e)
        {
            CurrentPurchaseOrder.RefreshNotes();
            LoadIcons();
        }

        protected void ucNotes_RefreshParent(object sender, EventArgs e)
        {
            CurrentPurchaseOrder.RefreshNotes();
            LoadIcons();
        }
        #endregion

        #region Private
        private void BindDefaultPayMethods()
        {
            ddlDefaultPayMethod.Items.Clear();
            ddlDefaultPayMethod.DataSource = PaymentMethod.LoadPaymentMethods().Where(pm => pm.Active).OrderBy(pm => pm.Name);
            ddlDefaultPayMethod.DataValueField = "PaymentMethodID";
            ddlDefaultPayMethod.DataTextField = "Name";
            ddlDefaultPayMethod.DataBind();

            ddlDefaultPayMethod.Items.Insert(0, new ListItem(String.Empty, "0"));
        }
        
        private void BindPOTypes()
        {
            ddlType.Items.Clear();
            ddlType.DataSource = PurchaseOrder.GetPurchaseOrderTypes(true).OrderBy(x => x.Order);
            ddlType.DataValueField = "Id";
            ddlType.DataTextField = "Value";
            ddlType.DataBind();
        }

        private void BindPaymentGrid()
        {
            ConfigurePaymentGrid();
            dgPayments.DataSource = GetPayments();
            dgPayments.DataBind();

        }

        private void BindPOItems()
        {
            ConfigurePOItemGrid();

            dgItems.DataSource = GetPOItems();
            dgItems.DataBind();
        }

        private void BindReceivingHistoryGrid()
        {
            ConfigureReceivingHistoryGrid();

            dgReceivingHistory.DataSource = GetReceivingHistory();
            dgReceivingHistory.DataBind();
        }

        private bool CanUserCancel()
        {
            bool canCancel = false;
            if ( UserCanEdit )
            {
                if ( CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0 && CurrentPurchaseOrder.Active
                    && CurrentPurchaseOrder.Status.Order < definedValueService.Get( PurchaseOrder.PurchaseOrderStatusPartiallyReceived ).Order )
                {
                    canCancel = true;
                }
            }
            return canCancel;
        }

        private bool CanUserEditAttachments()
        {
            bool canEdit = false;

            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0 && CurrentPurchaseOrder.Active)
            {
                CurrentPurchaseOrder.Status.LoadAttributes();
                if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean())
                {
                    canEdit = true;
                }
            }
            return canEdit;

        }

        private bool CanUserOrder()
        {
            bool CanOrder = false;

            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderStatusOpenLUID() && CurrentPurchaseOrder.Active)
                    CanOrder = true;
            }

            return CanOrder;
        }

        private bool CanUserReceive()
        {
            bool CanReceive = false;
            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0 && CurrentPurchaseOrder.DateOrdered > DateTime.MinValue && CurrentPurchaseOrder.DateReceived == DateTime.MinValue)
            {
                CurrentPurchaseOrder.Status.LoadAttributes();
                if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean() && CurrentPurchaseOrder.Active)
                {
                    CanReceive = true;
                }
            }

            return CanReceive;
        }

        private bool CanUserEditNotes()
        {
            bool canEdit = false;

            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0 && CurrentPurchaseOrder.Active)
            {
                CurrentPurchaseOrder.Status.LoadAttributes();
                if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean())
                {
                    canEdit = true;
                }
            }
            return canEdit;
        }

        private bool CanUserEditSummary()
        {
            bool canEdit = false;

            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0)
                {
                    CurrentPurchaseOrder.Status.LoadAttributes();
                    if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean() && CurrentPurchaseOrder.Active)
                    {
                        canEdit = true;
                    }
                }
                else if (CurrentPurchaseOrder == null)
                    canEdit = true;
            }

            return canEdit;
        }

        private bool CanUserEditItem()
        {
            bool canEdit = false;

            if (UserCanEdit)
            {
                //if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.Status.Qualifier != "Y")
                //    canEdit = true;
                //else if (CurrentPurchaseOrder == null)
                //    canEdit = true;

                if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0 && CurrentPurchaseOrder.Active)
                {
                    CurrentPurchaseOrder.Status.LoadAttributes();
                    if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean())
                    {
                        canEdit = true;
                    }
                }
                else if (CurrentPurchaseOrder == null || CurrentPurchaseOrder.PurchaseOrderID == 0)
                {
                    canEdit = true;
                }
            }

            return canEdit;
        }

        private bool CanUserEditPayments()
        {
            bool canEdit = false;

            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null  && CurrentPurchaseOrder.DateOrdered > DateTime.MinValue)
                {
                    CurrentPurchaseOrder.Status.LoadAttributes();
                    if (CurrentPurchaseOrder.StatusLUID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean() && CurrentPurchaseOrder.Active)
                    {
                        canEdit = true;
                    }
                }
            }

            return canEdit;
        }

        private bool CanUserMarkAsBilled()
        {
            bool canMark = false;

            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderStatusReceivedLUID() && CurrentPurchaseOrder.Active)
                    canMark = true;
            }

            return canMark;
        }

        private bool CanUserClose()
        {
            bool CanClose = false;
            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null && (CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderStatusBilledLUID() || CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderStatusReopenedLUID()) 
                        && CurrentPurchaseOrder.Active )
                    CanClose = true;
            }

            return CanClose;
        }

        private bool CanUserReopen()
        {
            bool CanReopen = false;

            if (UserCanEdit)
            {
                if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0)
                {
                    if ( CurrentPurchaseOrder.StatusLUID > 0 && CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderStatusClosedLUID() )
                    {
                        CanReopen = true;
                    }
                }
            }

            return CanReopen;
        }

        private bool CanUserSavePO()
        {
            bool CanSave = true;

            if ( CurrentPurchaseOrder != null )
            {
                CurrentPurchaseOrder.Status.LoadAttributes();
                if ( CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean() || !CurrentPurchaseOrder.Active )
                {
                    CanSave = false;
                }
            }


            return CanSave;
        }

        private void ClearSummary()
        {
            SetSummaryError(String.Empty);
            lblPONum.Text = "New";
            ddlType.SelectedIndex = 0;
            lblType.Text = "&nbsp;";
            ddlDefaultPayMethod.SelectedIndex = 0;
            lblDefaultPayMethod.Text = "&nbsp;";
            lblCreatedBy.Text = "N/A";
            lblCreatedOn.Text = "N/A";
            lblStatus.Text = PurchaseOrder.GetPurchaseOrderStatuses(true).OrderBy(x => x.Order).FirstOrDefault().Value;
            lblOrderedBy.Text = "(not ordered)";
            lblOrderedOn.Text = "(not ordered)";
            lblReceivedOn.Text = "(not ordered)";

            lblVendorName.Text = "(not selected)";
            lblVendorAddress.Text = String.Empty;
            lblVendorCSZ.Text = String.Empty;
            lblVendorWebAddress.Text = String.Empty;
            divVendorAddress.Visible = false;
            divVendorWebAddress.Visible = false;

            lblShipToName.Text = "(not selected)";
            lblShipToAttn.Text = String.Empty;
            lblShipToAddress.Text = String.Empty;
            lblShipToCity.Text = String.Empty;
            lblShipToState.Text = String.Empty;
            lblShipToZip.Text = String.Empty;
            divShipToAddress.Visible = false;
            divShipToAttention.Visible = false;
        }

        private void CancelPO()
        {
            try
            {
                if ( CurrentPurchaseOrder == null )
                    return;

                CurrentPurchaseOrder.Cancel( CurrentUser.UserName );
                LoadPO();

            }
            catch ( RequisitionException rEx )
            {
                if ( rEx.InnerException != null && rEx.InnerException.GetType() == typeof( RequisitionNotValidException ) )
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append( "Unable to cancel purchase order." );
                    sb.Append( "<ul type=\"disc\">" );
                    foreach ( var item in ( (RequisitionNotValidException)rEx.InnerException ).InvalidProperties )
                    {
                        sb.AppendFormat( "<li>{0} - {1}</li>", item.Key, item.Value );
                    }
                    sb.Append( "</ul>" );

                    SetSummaryError( sb.ToString() );
                }
            }
        }

        private void ClosePO()
        {
            try
            {
                if (CurrentPurchaseOrder == null)
                    return;

                CurrentPurchaseOrder.Close(CurrentUser.UserName);
                LoadPO();
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("Unable to close purchase order.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }
                    sb.Append("</ul>");

                    SetSummaryError(sb.ToString());
                }
            }

        }

        private void ConfigurePaymentGrid()
        {
            dgPayments.Visible = true;
            dgPayments.ItemType = "Items";
            /*dgPayments.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgPayments.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgPayments.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgPayments.AllowSorting = false;
            /*dgPayments.MergeEnabled = false;
            dgPayments.EditEnabled = false;
            dgPayments.MailEnabled = false;
            dgPayments.AddEnabled = false;
            dgPayments.ExportEnabled = false;
            dgPayments.DeleteEnabled = false;
            dgPayments.SourceTableKeyColumnName = "PaymentID";
            dgPayments.SourceTableOrderColumnName = "PaymentID";
            dgPayments.NoResultText = "No payments found.";*/
        }

        private void ConfigureReceivingHistoryGrid()
        {
            dgReceivingHistory.Visible = true;
            dgReceivingHistory.ItemType = "Items";
            /*dgReceivingHistory.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgReceivingHistory.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgReceivingHistory.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgReceivingHistory.AllowSorting = false;
            /*dgReceivingHistory.MergeEnabled = false;
            dgReceivingHistory.EditEnabled = false;
            dgReceivingHistory.MailEnabled = false;
            dgReceivingHistory.AddEnabled = false;
            dgReceivingHistory.ExportEnabled = false;
            dgReceivingHistory.DeleteEnabled = false;
            dgReceivingHistory.SourceTableKeyColumnName = "ReceiptID";
            dgReceivingHistory.SourceTableOrderColumnName = "ReceiptID";
            dgReceivingHistory.NoResultText = "No Packages received";*/
        }

        private void ConfigurePOItemGrid()
        {
            dgItems.Visible = true;
            dgItems.ItemType = "Items";
            /*dgItems.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgItems.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgItems.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgItems.AllowSorting = false;
            /*dgItems.MergeEnabled = false;
            dgItems.EditEnabled = false;
            dgItems.MailEnabled = false;
            dgItems.AddEnabled = false;
            dgItems.ExportEnabled = false;
            dgItems.DeleteEnabled = false;
            dgItems.SourceTableKeyColumnName = "ItemID";
            dgItems.SourceTableOrderColumnName = "ItemID";
            dgItems.NoResultText = "No items found";*/

            HyperLinkField hlc = (HyperLinkField)dgItems.Columns[8];
            hlc.DataNavigateUrlFormatString = "~/default.aspx?page=" + RequisitionDetailPageSetting + "&RequisitionID={0}";
        }

        private DataTable GetReceivingHistory()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("ReceiptID", typeof(int)),
                new DataColumn("DateReceived", typeof(string)),
                new DataColumn("CarrierName", typeof(string)),
                new DataColumn("ReceivedBy", typeof(string)),
                new DataColumn("TotalItems", typeof(int))
            });

            if (CurrentPurchaseOrder == null)
                return dt;

            foreach (Receipt r in CurrentPurchaseOrder.Receipts.Where(x => x.Active))
            {
                DataRow dr = dt.NewRow();
                dr["ReceiptID"] = r.ReceiptID;
                dr["DateReceived"] = r.DateReceived.ToShortDateString();
                dr["CarrierName"] = r.ShippingCarrier.Value;
                dr["ReceivedBy"] = r.ReceivedBy.FullName;
                dr["TotalItems"] = r.ReceiptItems.Select(x => x.QuantityReceived).Sum();

                dt.Rows.Add(dr);
            }

            return dt;
        }

        private DataTable GetPayments()
        {
            DataTable PaymentTable = new DataTable();

            PaymentTable.Columns.AddRange(new DataColumn[] {
                new DataColumn("PaymentID", typeof(int)),
                new DataColumn("PaymentDate", typeof(string)),
                new DataColumn("PaymentMethod", typeof(string)),
                new DataColumn("PaymentAmount", typeof(decimal)),
                new DataColumn("FullyApplied", typeof(bool)),
                new DataColumn("CreatedByUserID", typeof(string)),
                new DataColumn("CreatedByName", typeof(string))
            });

            if (CurrentPurchaseOrder == null)
                return PaymentTable;

            foreach (Payment p in CurrentPurchaseOrder.Payments.Where(pay => pay.Active).OrderBy(pay => pay.PaymentID))
            {
                Decimal ChargesAbs = Math.Abs(p.Charges.Where(x => x.Active).Select(x => x.Amount).Sum());
                DataRow dr = PaymentTable.NewRow();
                dr["PaymentID"] = p.PaymentID;
                dr["PaymentDate"] = p.PaymentDate.ToShortDateString();
                dr["PaymentMethod"] = p.PaymentMethod.Name;
                dr["PaymentAmount"] = p.PaymentAmount;
                dr["FullyApplied"] = Math.Abs(p.PaymentAmount) <= ChargesAbs;
                dr["CreatedByUserID"] = p.CreatedByUserID;

                if (p.CreatedBy == null || p.CreatedBy.PrimaryAliasId <= 0)
                    dr["CreatedByName"] = p.CreatedByUserID;
                else
                    dr["CreatedByName"] = p.CreatedBy.FullName;

                PaymentTable.Rows.Add(dr);
            }

            return PaymentTable;
        }

        private DataTable GetPOItems()
        {
            DataTable POItems = new DataTable();
            POItems.Columns.AddRange(new DataColumn[]{
                new DataColumn("POItemID", typeof(int)),
                new DataColumn("ItemID", typeof(int)),
                new DataColumn("Quantity", typeof(int)),
                new DataColumn("QuantityReceived", typeof(int)),
                new DataColumn("ItemNumber", typeof(string)),
                new DataColumn("Description", typeof(string)),
                new DataColumn("IsExpedited", typeof(bool)),
                new DataColumn("DateNeeded", typeof(string)),
                new DataColumn("AccountNumber", typeof(string)),
                new DataColumn("RequisitionID", typeof(int)),
                new DataColumn("Price", typeof(string)),
                new DataColumn("Extension", typeof(string))
            });

            int totalQuantity = 0;
            int totalReceivedQty = 0;
            if (CurrentPurchaseOrder != null)
            {
                foreach (PurchaseOrderItem poi in CurrentPurchaseOrder.Items.Where(x => x.Active).OrderBy(x => x.PurchaseOrderItemID))
                {
                    int receivedQty = poi.ReceiptItems.Where(x => x.Active).Select(x => x.QuantityReceived).Sum();
                    DataRow dr = POItems.NewRow();
                    dr["POItemID"] = poi.PurchaseOrderItemID;
                    dr["ItemID"] = poi.ItemID;
                    dr["Quantity"] = poi.Quantity;
                    dr["QuantityReceived"] = receivedQty;
                    dr["ItemNumber"] = poi.RequisitionItem.ItemNumber;
                    dr["IsExpedited"] = poi.RequisitionItem.IsExpeditiedShippingAllowed;
                    dr["DateNeeded"] = poi.RequisitionItem.DateNeeded > DateTime.MinValue ? poi.RequisitionItem.DateNeeded.ToShortDateString() : String.Empty;
                    dr["Description"] = poi.RequisitionItem.Description;
                    dr["AccountNumber"] = string.Format("{0}-{1}-{2}", poi.RequisitionItem.FundID, poi.RequisitionItem.DepartmentID, poi.RequisitionItem.AccountID);
                    dr["RequisitionID"] = poi.RequisitionItem.RequisitionID;
                    dr["Price"] = poi.Price == 0 ? String.Empty : string.Format("{0:c}", poi.Price);
                    dr["Extension"] = poi.Price == 0 ? String.Empty : string.Format("{0:c}", poi.Price * poi.Quantity);

                    POItems.Rows.Add(dr);

                    totalQuantity += poi.Quantity;
                    totalReceivedQty += receivedQty;
                    
                }
                
                if(totalQuantity > 0)
                {
                    dgItems.Columns[1].FooterText = totalQuantity.ToString();
                    dgItems.Columns[1].FooterStyle.HorizontalAlign = HorizontalAlign.Right;
                    dgItems.Columns[1].FooterStyle.VerticalAlign = VerticalAlign.Top;
                    dgItems.Columns[2].FooterText = totalReceivedQty.ToString();
                    dgItems.Columns[2].FooterStyle.HorizontalAlign = HorizontalAlign.Right;
                    dgItems.Columns[2].FooterStyle.VerticalAlign = VerticalAlign.Top;

                    System.Text.StringBuilder sbHeaders = new System.Text.StringBuilder();

                    sbHeaders.Append("<div style=\"font-weight:bold;\">Subtotal:</div>");
                    sbHeaders.Append("<div style=\"font-weight:bold;\">Shipping:</div>");
                    sbHeaders.Append("<div style=\"font-weight:bold;\">Tax/Other:</div>");
                    sbHeaders.Append("<div style=\"font-weight:bold;\">Total:</div>");

                    dgItems.Columns[9].FooterText = sbHeaders.ToString();
                    dgItems.Columns[9].FooterStyle.HorizontalAlign = HorizontalAlign.Right;

                    
                }

            }
            return POItems;
        }

        private void LoadAttachments()
        {
            ucAttachments.CurrentUser = CurrentUser;
            ucAttachments.ReadOnly = !CanUserEditAttachments();
            ucAttachments.LoadAttachmentControl(typeof(PurchaseOrder).ToString(), PurchaseOrderID);
        }

        private void LoadIcons()
        {
            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0)
            {
                lnkAttachments.Visible = CurrentPurchaseOrder.Attachments.Where(x => x.Active).Count() > 0;
                lnkNotes.Visible = CurrentPurchaseOrder.Notes.Where(x => x.Active).Count() > 0;
            }
        }

        private void LoadItems()
        {
            BindPOItems();
            LoadItemTotals();
        }

        private void LoadItemTotals()
        {
            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.Items.Where(i => i.Active).Count() > 0)
            {
                dgItems.ShowFooter = true;
                decimal ItemSubtotal = CurrentPurchaseOrder.Items.Where(x => x.Active).Select(x => (x.Price * x.Quantity)).Sum();
                decimal Total = ItemSubtotal + CurrentPurchaseOrder.ShippingCharge + CurrentPurchaseOrder.OtherCharge;
                bool ShowTextbox = CanUserEditItem();

                var c = dgItems.FooterRow.Cells[dgItems.FooterRow.Cells.Count - 2];

                Label lblItemSubtotal = (Label)c.FindControl("lblSubTotal");
                Label lblItemShipping = (Label)c.FindControl("lblShipping");
                TextBox txtItemShipping = (TextBox)c.FindControl("txtShipping");
                Label lblItemTax = (Label)c.FindControl("lblTax");
                TextBox txtItemTax = (TextBox)c.FindControl("txtTax");
                Label lblItemTotal = (Label)c.FindControl("lblItemGridTotal");
                
                lblItemSubtotal.Text = ItemSubtotal.ToString("0.00");
                lblItemShipping.Text = CurrentPurchaseOrder.ShippingCharge.ToString( "0.00;(0.00)" );
                txtItemShipping.Text = CurrentPurchaseOrder.ShippingCharge.ToString("0.00");
                lblItemTax.Text = CurrentPurchaseOrder.OtherCharge.ToString("0.00;(0.00)");
                txtItemTax.Text = CurrentPurchaseOrder.OtherCharge.ToString("0.00");
                lblItemTotal.Text = Total.ToString("0.00");

                lblItemShipping.Visible = !ShowTextbox;
                txtItemShipping.Visible = ShowTextbox;

                lblItemTax.Visible = !ShowTextbox;
                txtItemTax.Visible = ShowTextbox;          

            }
        }

        private void LoadNotes()
        {
            ucNotes.ReadOnly = !CanUserEditNotes();
            ucNotes.LoadNoteList(typeof(PurchaseOrder).ToString(), PurchaseOrderID);
        }

        private void LoadPO()
        {
            if (PurchaseOrderID > 0 && (CurrentPurchaseOrder == null || CurrentPurchaseOrder.PurchaseOrderID <= 0))
            {
                ShowAlert("Purchase Order not found. Please return to the Purchase Order List and retry selection.", true);
            }
            else
            {

                LoadSummary();
                LoadItems();
                LoadReceivingHistory();
                LoadNotes();
                LoadAttachments();
                LoadPayments();
                LoadIcons();
            }
        }

        private void LoadReceivingHistory()
        {
            if (CurrentPurchaseOrder != null)
            {
                BindReceivingHistoryGrid();
            }
        }

        private void LoadShipTo(bool useDefault)
        {
            if (CurrentPurchaseOrder != null)
            {
                if (!String.IsNullOrEmpty(CurrentPurchaseOrder.ShipToName))
                    lblShipToName.Text = CurrentPurchaseOrder.ShipToName;
                if (!String.IsNullOrEmpty(CurrentPurchaseOrder.ShipToAttn))
                {
                    divShipToAttention.Visible = true;
                    lblShipToAttn.Text = CurrentPurchaseOrder.ShipToAttn;
                }
                if (CurrentPurchaseOrder.ShipToAddress != null && CurrentPurchaseOrder.ShipToAddress.IsValid())
                {
                    divShipToAddress.Visible = true;
                    lblShipToAddress.Text = CurrentPurchaseOrder.ShipToAddress.StreetAddress;
                    lblShipToCity.Text = CurrentPurchaseOrder.ShipToAddress.City;
                    lblShipToState.Text = CurrentPurchaseOrder.ShipToAddress.State;
                    lblShipToZip.Text = CurrentPurchaseOrder.ShipToAddress.PostalCode;
                }
            }
            else if (useDefault)
            {
                if (!String.IsNullOrEmpty(DefaultShipToNameSetting))
                    lblShipToName.Text = DefaultShipToNameSetting;
                divShipToAttention.Visible = !String.IsNullOrEmpty(DefaultShipToAttentionSetting);
                lblShipToAttn.Text = DefaultShipToAttentionSetting;


                if (DefaultShipToCampusSetting > 0)
                {
                    hfCampusID.Value = DefaultShipToCampusSetting.ToString();
                    Rock.Model.Campus ShipToCampus = campusService.Get(DefaultShipToCampusSetting);
                    if (ShipToCampus.LocationId > 0)
                    {
                        divShipToAddress.Visible = true;
                        lblShipToAddress.Text = ShipToCampus.Location.Street1 + " " + ShipToCampus.Location.Street2;
                        lblShipToCity.Text = ShipToCampus.Location.City;
                        lblShipToState.Text = ShipToCampus.Location.State;
                        lblShipToZip.Text = ShipToCampus.Location.PostalCode;
                    }
                }

            }
        }

        private void LoadPayments()
        {
            BindPaymentGrid();
        }

        private void LoadSummary()
        {
            ClearSummary();

            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.PurchaseOrderID > 0)
            {
                lblPONum.Text = CurrentPurchaseOrder.PurchaseOrderID.ToString();
                lblType.Text = CurrentPurchaseOrder.PurchaseOrderType.Value;

                if (ddlType.Items.FindByValue(CurrentPurchaseOrder.PurchaseOrderTypeLUID.ToString()) != null)
                    ddlType.SelectedValue = CurrentPurchaseOrder.PurchaseOrderTypeLUID.ToString();

                if (CurrentPurchaseOrder.CreatedBy != null && CurrentPurchaseOrder.CreatedBy.PrimaryAliasId > 0)
                    lblCreatedBy.Text = CurrentPurchaseOrder.CreatedBy.FullName;
                else
                    lblCreatedBy.Text = CurrentPurchaseOrder.CreatedByUserID;

                if (CurrentPurchaseOrder.DateCreated > DateTime.MinValue)
                    lblCreatedOn.Text = string.Format("{0:d}", CurrentPurchaseOrder.DateCreated);

                if (CurrentPurchaseOrder.OrderedByID > 0)
                    lblOrderedBy.Text = CurrentPurchaseOrder.OrderedBy.FullName;

                if (CurrentPurchaseOrder.DateOrdered > DateTime.MinValue)
                    lblOrderedOn.Text = string.Format("{0:d}", CurrentPurchaseOrder.DateOrdered);

                if (CurrentPurchaseOrder.StatusLUID > 0)
                    lblStatus.Text = CurrentPurchaseOrder.Status.Value;

                if (CurrentPurchaseOrder.DateReceived > DateTime.MinValue)
                    lblReceivedOn.Text = string.Format("{0:d}", CurrentPurchaseOrder.DateReceived);

                if (CurrentPurchaseOrder.DefaultPaymentMethodID > 0)
                {
                    ddlDefaultPayMethod.SelectedValue = CurrentPurchaseOrder.DefaultPaymentMethodID.ToString();
                    lblDefaultPayMethod.Text = CurrentPurchaseOrder.DefaultPaymentMethod.Name;
                }

                if ( !CurrentPurchaseOrder.Active )
                {
                    SetSummaryError( "This purchase order is not active." );
                }

                LoadVendor(CurrentPurchaseOrder.Vendor, CurrentPurchaseOrder.Terms);
                LoadShipTo(false);

            }
            else
            {
                LoadShipTo(true);
            }

            SetSummaryVisibility(CanUserEditSummary());
        }

        private void LoadVendor(Vendor v, string terms)
        {
            if (v != null)
            {
                hfVendorID.Value = v.VendorID.ToString();
                lblVendorName.Text = v.VendorName;
                if (v.Address != null)
                {
                    divVendorAddress.Visible = true;
                    lblVendorAddress.Text = v.Address.StreetAddress;
                    lblVendorCSZ.Text = string.Format("{0}, {1} {2}", v.Address.City, v.Address.State, v.Address.PostalCode);

                }
                if (!String.IsNullOrEmpty(v.WebAddress))
                {
                    divVendorWebAddress.Visible = true;
                    lblVendorWebAddress.Text = string.Format("<a href=\"{0}\" target=\"_blank\">{0}</a>", v.WebAddress);
                }
                else
                {
                    divVendorWebAddress.Visible = false;
                    lblVendorWebAddress.Text = string.Empty;
                }

                if (!String.IsNullOrEmpty(terms))
                {
                    divVendorTerms.Visible = true;
                    lblVendorTerms.Text = terms;
                }
                else
                {
                    divVendorTerms.Visible = false;
                    lblVendorTerms.Text = string.Empty;
                }

            }
        }

        private void MarkAsBilled()
        {
            if (CurrentPurchaseOrder == null)
                return;

            if (CurrentPurchaseOrder.Payments.Where(p => p.Active).Count() == 0)
            {
                SetSummaryError("Can not mark as billed - No payments found for purchase order.");
                return;
            }

            if (CurrentPurchaseOrder.Status.Order < definedValueService.Get(PurchaseOrder.PurchaseOrderStatusReceived).Order)
            {
                SetSummaryError("Can not mark as billed - order has not been fully received.");
                return;
            }

            CurrentPurchaseOrder.MarkAsBilled(CurrentUser.UserName);
            LoadPO();

        }

        private void RedirectToList()
        {
            NavigateToPage(PurchaseOrderListPageSetting.AsGuid(), null);
        }

        private void RemoveItemFromPO(int poItemID)
        {
            CurrentPurchaseOrder.Status.LoadAttributes();
            if (poItemID > 0 && !CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean())
            {
                if (CurrentPurchaseOrder.RemoveItem(poItemID, CurrentUser.UserName))
                {
                    LoadItems();
                }
                else
                {
                    SetSummaryError("Unable to remove selected item from Purchase Order");
                }
            }
        }

        private void RemovePayment(int paymentID)
        {
            if (CurrentPurchaseOrder.RemovePayment(paymentID, CurrentUser.UserName))
                LoadPO();
            else
                SetSummaryError("Unable to remove selected payment.");
        }

        private void RemoveReceipt(int receiptID)
        {
            if (CurrentPurchaseOrder.RemoveReceipt(receiptID, CurrentUser.UserName))
                LoadPO();
            else
                SetSummaryError("Unable to remove receipt.");
        }

        private void ReopenPO()
        {
            if (CurrentPurchaseOrder == null)
                return;

            CurrentPurchaseOrder.Status.LoadAttributes();
            if (!CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean())
                SetSummaryError("Reopen PO - Current Purchase Order is open.");

            CurrentPurchaseOrder.Reopen(CurrentUser.UserName);
            LoadPO();
        }

        private bool SaveSummary()
        {



            int VendorID = 0;
            int.TryParse(hfVendorID.Value, out VendorID);
            //if (VendorID > 0)
            //    CurrentPurchaseOrder.VendorID = VendorID;
            //else
            //{
            //    SetSummaryError("Please select vendor before saving.");
            //    return false;
            //}

            if ( VendorID <= 0 )
            {
                SetSummaryError( "Please select vendor before saving." );
                return false;
            }

            if ( CurrentPurchaseOrder == null )
                CurrentPurchaseOrder = new PurchaseOrder();

            CurrentPurchaseOrder.VendorID = VendorID;
            

            int POType = 0;
            if(int.TryParse(ddlType.SelectedValue, out POType))
                CurrentPurchaseOrder.PurchaseOrderTypeLUID = POType;
            if(CurrentPurchaseOrder.StatusLUID <= 0)
                CurrentPurchaseOrder.StatusLUID = PurchaseOrder.GetPurchaseOrderStatuses(true).OrderBy(x => x.Order).FirstOrDefault().Id;

            if (ddlDefaultPayMethod.SelectedIndex > 0)
            {
                CurrentPurchaseOrder.DefaultPaymentMethodID = int.Parse(ddlDefaultPayMethod.SelectedValue);
            }
            else
            {
                CurrentPurchaseOrder.DefaultPaymentMethodID = 0;
            }

            CurrentPurchaseOrder.ShipToName = lblShipToName.Text;
            CurrentPurchaseOrder.ShipToAttn = lblShipToAttn.Text;

            CurrentPurchaseOrder.Terms = lblVendorTerms.Text;

            org.secc.Purchasing.Helpers.Address a = new org.secc.Purchasing.Helpers.Address(lblShipToAddress.Text, lblShipToCity.Text, lblShipToState.Text, lblShipToZip.Text);
            if (a.IsValid())
                CurrentPurchaseOrder.ShipToAddress = a;


            decimal ShippingCharge = 0;
            decimal OtherCharge = 0;

            if (CanUserEditItem() && CurrentPurchaseOrder.Items.Count > 0)
            {
                var c = dgItems.FooterRow.Cells[dgItems.FooterRow.Cells.Count - 2];

                TextBox txtItemShipping = (TextBox)c.FindControl("txtShipping");
                TextBox txtItemTax = (TextBox)c.FindControl("txtTax");

                if (txtItemShipping.Visible && decimal.TryParse(txtItemShipping.Text, out ShippingCharge))
                    CurrentPurchaseOrder.ShippingCharge = ShippingCharge;

                if (txtItemTax.Visible && decimal.TryParse(txtItemTax.Text, out OtherCharge))
                    CurrentPurchaseOrder.OtherCharge = OtherCharge;
            }

            if (CurrentPurchaseOrder.HasChanged())
            {
                CurrentPurchaseOrder.Save(CurrentUser.UserName);
                PurchaseOrderID = CurrentPurchaseOrder.PurchaseOrderID;

                LoadSummary();
                LoadItemTotals();
            }
            return true;

        }

        private void SetSummaryError(string errorMessage)
        {
            lblSummaryError.Text = errorMessage;
            lblSummaryError.Visible = !String.IsNullOrEmpty(errorMessage);
        }

        private void SetSummaryVisibility(bool isEditable)
        {
            ddlType.Visible = isEditable;
            lbChangeVendor.Visible = isEditable;

            lblType.Visible = !isEditable;
        }

        private void ShowAddAttachmentDialog()
        {
            ucAttachments.Show();
        }


        private void ShowAddNoteDialog()
        {
            ucNotes.ResetVariableProperties();
            if (ucNotes.Identifier <= 0)
                ucNotes.Identifier = PurchaseOrderID;
            ucNotes.ReadOnly = !CanUserEditNotes();
            ucNotes.ShowNoteDetail();
        }

        private void ShowAlert(string alertMsg, bool hideMain)
        {
            lblPOAlert.Text = alertMsg;
            ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "ShowAlert" + DateTime.Now.Ticks, "showAlert(true, " + hideMain.ToString().ToLower() + ");", true);
            
        }

        private void HideAlert()
        {
            ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "ShowAlert" + DateTime.Now.Ticks, "showAlert(false);", true);
        }

        #endregion

        #region Vendor Modal
        protected void btnVendorAdd_Click(object sender, EventArgs e)
        {
            if (AddNewVendor())
            {
                BindVendorList(chkVendorShowInactive.Checked);
                int vendorID = 0;
                int.TryParse(hfVendorSelectID.Value, out vendorID);

                if (vendorID > 0 && ddlVendorSelect.Items.FindByValue(vendorID.ToString()) != null)
                    ddlVendorSelect.SelectedValue = vendorID.ToString();
            }
        }

        protected void btnVendorCancel_Click(object sender, EventArgs e)
        {
            CloseVendorModal();
        }

        protected void btnVendorReset_Click(object sender, EventArgs e)
        {
            ClearVendorModalFields();
            int vendorID = 0;
            int.TryParse(ddlVendorSelect.SelectedValue, out vendorID);

            if (vendorID > 0)
                PopulateVendorModalFields(vendorID);

        }

        protected void btnVendorSelect_Click(object sender, EventArgs e)
        {
            if (SelectVendor())
            {
                CloseVendorModal();
            }
        }

        protected void chkVendorShowInactive_Changed(object sender, EventArgs e)
        {
            int selectedVendor = 0;
            int.TryParse(ddlVendorSelect.SelectedValue, out selectedVendor);
            BindVendorList(chkVendorShowInactive.Checked);

            if (selectedVendor > 0 && ddlVendorSelect.Items.FindByValue(selectedVendor.ToString()) != null)
                ddlVendorSelect.SelectedValue = selectedVendor.ToString();
        }

        protected void ddlVendorSelect_IndexChanged(object sender, EventArgs e)
        {
            ClearVendorModalFields();
            int selectedVendor = 0;
            int.TryParse(ddlVendorSelect.SelectedValue, out selectedVendor);

            if (selectedVendor == 0)
            {
                btnVendorAdd.CssClass = btnVendorAdd.CssClass.Replace("btnAddHide", "");
                btnVendorSelect.CssClass = btnVendorSelect.CssClass + " btnAddHide";

            }
            PopulateVendorModalFields(selectedVendor);
        }

        private bool AddNewVendor()
        {
            bool hasBeenAdded = false;
            SetVendorError(String.Empty);
            try
            {
                if (String.IsNullOrEmpty(txtVendorName.Text))
                {
                    SetVendorError("Vendor name is required");
                    return false;
                }
                if (Vendor.LoadByName(txtVendorName.Text, false).Count > 0)
                {
                    SetVendorError("Vendor already exists.");
                    return false;
                }

                Vendor v = new Vendor();
                v.VendorName = txtVendorName.Text;
                if (!String.IsNullOrEmpty(txtVendorAddress.Text) && !String.IsNullOrEmpty(txtVendorCity.Text) && !String.IsNullOrEmpty(txtVendorState.Text) && !String.IsNullOrEmpty(txtVendorZip.Text))
                {
                    org.secc.Purchasing.Helpers.Address a = new org.secc.Purchasing.Helpers.Address(txtVendorAddress.Text, txtVendorCity.Text, txtVendorState.Text, txtVendorZip.Text);
                    if (a.IsValid())
                        v.Address = a;
                }

                if (!String.IsNullOrEmpty(txtVendorPhone.Text))
                {
                    org.secc.Purchasing.Helpers.PhoneNumber p = new org.secc.Purchasing.Helpers.PhoneNumber(txtVendorPhone.Text, txtVendorPhoneExtn.Text);
                    if (p.IsValid())
                        v.Phone = p;
                }

                if (!String.IsNullOrEmpty(txtVendorWebAddress.Text))
                    v.WebAddress = txtVendorWebAddress.Text;

                v.Save(CurrentUser.UserName);
                hfVendorSelectID.Value = v.VendorID.ToString();

                BindVendorList(chkVendorActive.Checked);
                PopulateVendorModalFields(v.VendorID);
                hasBeenAdded = true;
            }
            catch (VendorException ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(VendorNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("Vendor is not valid.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (KeyValuePair<string, string> item in ((VendorNotValidException)ex.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }
                    sb.Append("</ul>");

                    SetVendorError(sb.ToString());
                }
                else
                {
                    throw ex;
                }
            }
            return hasBeenAdded;
        }

        private void BindVendorList(bool showInactive)
        {
            ddlVendorSelect.Items.Clear();
            ddlVendorSelect.DataSource = Vendor.LoadVendors(!showInactive).OrderBy(x => x.VendorName);
            ddlVendorSelect.DataValueField = "VendorID";
            ddlVendorSelect.DataTextField = "VendorName";
            ddlVendorSelect.DataBind();

            ddlVendorSelect.Items.Insert(0, new ListItem("--Select--", "-1"));
            ddlVendorSelect.Items.Insert(1, new ListItem("[New]", "0"));
        }

        private void ClearVendorModalFields()
        {
            SetVendorError(String.Empty);
            txtVendorName.Text = String.Empty;
            lblVendorSelectName.Text = "&nbsp;";
            txtVendorAddress.Text = String.Empty;
            lblVendorSelectAddress.Text = "&nbsp;";
            txtVendorCity.Text = String.Empty;
            lblVendorSelectCity.Text = "&nbsp;";   
            txtVendorState.Text = String.Empty;
            lblVendorSelectState.Text = "&nbsp;";
            txtVendorZip.Text = string.Empty;
            lblVendorSelectZip.Text = "&nbsp;";
            txtVendorPhone.Text = string.Empty;
            lblVendorSelectPhone.Text = "&nbsp;";
            txtVendorPhoneExtn.Text = string.Empty;
            lblVendorSelectPhoneExtn.Text = "&nbsp;";
            txtVendorWebAddress.Text = string.Empty;
            lblVendorSelectWebAddress.Text = "&nbsp;";
            txtVendorSelectTerms.Text = string.Empty;
            lblVendorSelectTerms.Text = "&nbsp;";
            chkVendorActive.Checked = false;

            if (!btnVendorAdd.CssClass.Contains("btnAddHide"))
                btnVendorAdd.CssClass = btnVendorAdd.CssClass + " btnAddHide";

            btnVendorSelect.CssClass = btnVendorSelect.CssClass.Replace("btnAddHide", "");
        }

        private void CloseVendorModal()
        {
            hfVendorSelectID.Value = String.Empty;
            ClearVendorModalFields();
            mpVendorSelect.Hide();
        }

        private bool HasPurchaseOrderBeenOrdered()
        {
            return CurrentPurchaseOrder.StatusLUID == PurchaseOrder.PurchaseOrderOrderedStatusLUID();
        }

        private bool IsPurchaseOrderClosed()
        {
            CurrentPurchaseOrder.Status.LoadAttributes();
            return CurrentPurchaseOrder.Status.GetAttributeValue("IsClosed").AsBoolean();
        }

        private void PopulateVendorModalFields(int vendorID)
        {
            if (vendorID > 0)
            {
                hfVendorSelectID.Value = vendorID.ToString();
                Vendor v = new Vendor(vendorID);

                if (!String.IsNullOrEmpty(v.VendorName))
                {
                    txtVendorName.Text = v.VendorName;
                    lblVendorSelectName.Text = v.VendorName;
                }
                if (v.Address != null)
                {
                    txtVendorAddress.Text = v.Address.StreetAddress;
                    lblVendorSelectAddress.Text = v.Address.StreetAddress;
                    txtVendorCity.Text = v.Address.City;
                    lblVendorSelectCity.Text = v.Address.City;
                    txtVendorState.Text = v.Address.State;
                    lblVendorSelectState.Text = v.Address.State;
                    txtVendorZip.Text = v.Address.PostalCode;
                    lblVendorSelectZip.Text = v.Address.PostalCode;
                }
                if (v.Phone != null)
                {
                    txtVendorPhone.Text = v.Phone.FormatNumber(false);
                    lblVendorSelectPhone.Text = v.Phone.FormatNumber(false);
                    txtVendorPhoneExtn.Text = v.Phone.Extension;
                    lblVendorSelectPhoneExtn.Text = v.Phone.Extension;
                }
                if (!String.IsNullOrEmpty(v.WebAddress))
                {
                    txtVendorWebAddress.Text = v.WebAddress;
                    lblVendorSelectWebAddress.Text = v.WebAddress;
                }

                string Terms = String.Empty;
                if (CurrentPurchaseOrder != null && !String.IsNullOrEmpty(CurrentPurchaseOrder.Terms))
                {
                    Terms = CurrentPurchaseOrder.Terms;
                }
                else if(!String.IsNullOrEmpty(v.Terms))
                {
                    Terms = v.Terms;
                }

                txtVendorSelectTerms.Text = Terms;
                lblVendorSelectTerms.Text = !String.IsNullOrEmpty(Terms) ? Terms : "&nbsp;";


                chkVendorActive.Checked = v.Active;
            }

            SetVendorModalVisibility(vendorID == 0);
            
        }

        private void ShowPrintPOPopup()
        {
            if (CurrentPurchaseOrder == null)
                return;

            PDFWorkflowObject pdfWorkflowObject = new PDFWorkflowObject();

            pdfWorkflowObject.LavaInput = GetAttributeValue("PDFReportLava");

            pdfWorkflowObject.MergeObjects = new Dictionary<string, object>();
            pdfWorkflowObject.MergeObjects.Add("Vendor", CurrentPurchaseOrder.Vendor.VendorName);
            pdfWorkflowObject.MergeObjects.Add("ShipTo", 
                new Dictionary<String, String> {
                    { "Name", CurrentPurchaseOrder.ShipToName },
                    { "Attn", CurrentPurchaseOrder.ShipToAttn },
                    { "Street1", CurrentPurchaseOrder.ShipToAddress != null?CurrentPurchaseOrder.ShipToAddress.StreetAddress:null},
                    { "City", CurrentPurchaseOrder.ShipToAddress != null?CurrentPurchaseOrder.ShipToAddress.City:null},
                    { "State", CurrentPurchaseOrder.ShipToAddress != null?CurrentPurchaseOrder.ShipToAddress.State:null},
                    { "Zip", CurrentPurchaseOrder.ShipToAddress != null?CurrentPurchaseOrder.ShipToAddress.PostalCode:null },
 
                });
            List<Dictionary<String, Object>> purchaseOrderItems = new List<Dictionary<String, Object>>();
            foreach (PurchaseOrderItem item in CurrentPurchaseOrder.Items) 
            {
                purchaseOrderItems.Add(new Dictionary<String, Object> {
                    { "Quantity", item.Quantity},
                    { "ItemNumber", item.RequisitionItem.ItemNumber},
                    { "Description", item.RequisitionItem.Description},
                    { "Price", item.Price}
                });
            }
            pdfWorkflowObject.MergeObjects.Add("PurchaseOrderItems", purchaseOrderItems);

            pdfWorkflowObject.MergeObjects.Add("PurchaseOrder", 
                new Dictionary<String, Object> {
                    { "PurchaseOrderID", CurrentPurchaseOrder.PurchaseOrderID }
 
                });

            pdfWorkflowObject.MergeObjects.Add("SubTotal", CurrentPurchaseOrder.Items.Where(x => x.Active).Select(x => (x.Price * x.Quantity)).Sum());
            pdfWorkflowObject.MergeObjects.Add("ShippingCharge", CurrentPurchaseOrder.ShippingCharge);
            pdfWorkflowObject.MergeObjects.Add("OtherCharge", CurrentPurchaseOrder.OtherCharge);

            pdfWorkflowObject.MergeObjects.Add("OrderedBy", CurrentPurchaseOrder.OrderedBy.FullName);
            pdfWorkflowObject.MergeObjects.Add("OrderedDate", CurrentPurchaseOrder.DateOrdered);

            Guid workflowTypeGuid = Guid.NewGuid();
            if (Guid.TryParse(GetAttributeValue("PDFReportWorkflow"), out workflowTypeGuid))
            {
                var workflowTypeService = new WorkflowTypeService(rockContext);
                var workflowType = workflowTypeService.Get(workflowTypeGuid);
                if (workflowType != null)
                {
                    var workflow = Workflow.Activate(workflowType, "PDFLavaWorkflow");

                    List<string> workflowErrors;
                    var workflowService = new WorkflowService(rockContext);
                    var workflowActivity = GetAttributeValue("PDFReportActivity");
                    var activityType = workflowType.ActivityTypes.Where(a => a.Name == workflowActivity).FirstOrDefault();
                    if (activityType != null)
                    {
                        WorkflowActivity.Activate(activityType, workflow, rockContext);
                        if (workflowService.Process(workflow, pdfWorkflowObject, out workflowErrors))
                        {
                            var b = pdfWorkflowObject.RenderedPDF.DatabaseData.Content;
                            Response.Clear();
                            Response.Buffer = true;
                            //Response.Write(pdfWorkflowObject.RenderedXHTML);
                            //Response.End();
                            Response.ContentType = pdfWorkflowObject.RenderedPDF.MimeType;
                            Response.AddHeader("content-disposition", "attachment;filename=\"PurchaseOrder-" + CurrentPurchaseOrder.PurchaseOrderID +".pdf\"");
                            Response.OutputStream.Write(b, 0, b.Length);
                            Response.End();
                        }
                    }
                }
            }
        }

        private bool SelectVendor()
        {
            bool IsVendorSelected = false;
            SetVendorError(String.Empty);
            int VendorID = 0;
            int.TryParse(ddlVendorSelect.SelectedValue, out VendorID);

            if (VendorID <= 0)
            {
                SetVendorError("Vendor Not Selected");
                return IsVendorSelected;
            }

            Vendor SelectedVendor = new Vendor(VendorID);
            if (SelectedVendor.VendorID > 0)
            {
                LoadVendor(SelectedVendor, txtVendorSelectTerms.Text);
                IsVendorSelected = true;

            }
            else
                SetVendorError("Selected vendor not found.");

            return IsVendorSelected;

        }

        private void SetVendorModalVisibility(bool canEdit)
        {
            txtVendorName.Visible = canEdit;
            txtVendorAddress.Visible = canEdit;
            txtVendorCity.Visible = canEdit;
            txtVendorState.Visible = canEdit;
            txtVendorZip.Visible = canEdit;
            txtVendorPhone.Visible = canEdit;
            txtVendorPhoneExtn.Visible = canEdit;
            txtVendorWebAddress.Visible = canEdit;
            lblVendorSelectStreetHeader.Visible = canEdit;
            lblVendorSelectCityHeader.Visible = canEdit;
            lblVendorSelectStateHeader.Visible = canEdit;
            lblVendorSelectZipHeader.Visible = canEdit;
            lblVendorSelectPhoneExtnHeader.Visible = canEdit;
            

            lblVendorSelectName.Visible = !canEdit;
            lblVendorSelectAddress.Visible = !canEdit;
            lblVendorSelectCity.Visible = !canEdit;
            lblVendorSelectState.Visible = !canEdit;
            lblVendorSelectZip.Visible = !canEdit;
            lblVendorSelectPhone.Visible = !canEdit;
            lblVendorSelectPhoneExtn.Visible = !canEdit;
            lblVendorSelectWebAddress.Visible = !canEdit;

            if (ddlVendorSelect.SelectedValue == "-1")
            {
                txtVendorSelectTerms.Visible = false;
                lblVendorSelectTerms.Visible = true;
            }
            else
            {
                txtVendorSelectTerms.Visible = true;
                lblVendorSelectTerms.Visible = false;
            }
        }

        private void SetToolbarVisibility()
        {
            lbToolbarSave.Visible = CanUserSavePO();
            lbToolbarAddItem.Visible = CanUserEditItem();
            lbToolbarAddNote.Visible = CanUserEditNotes();
            lbToolbarAddAttachment.Visible = CanUserEditAttachments();
            lbToolbarOrder.Visible = CanUserOrder();
            lbToolbarAddPayment.Visible = CanUserEditPayments();
            lbToolbarMarkAsBilled.Visible = CanUserMarkAsBilled();
            lbToolbarClose.Visible = CanUserClose();
            lbToolbarReopen.Visible = CanUserReopen();
            lbToolbarReceive.Visible = CanUserReceive();
            lbToolbarReturn.Visible = true;
            lbToolbarPrintPO.Visible = !(CurrentPurchaseOrder == null);
            lbToolbarCancel.Visible = CanUserCancel();

        }
        
        private void ShowVendorModal()
        {
            
            hfVendorSelectID.Value = hfVendorID.Value;
            chkVendorShowInactive.Checked = ShowInactiveVendorsSetting;
            ClearVendorModalFields();
            int VendorID = 0;

            int.TryParse(hfVendorID.Value, out VendorID);
            BindVendorList(chkVendorShowInactive.Checked);

            if (VendorID > 0)
            {
                if (ddlVendorSelect.Items.FindByValue(VendorID.ToString()) != null)
                {
                    ddlVendorSelect.SelectedValue = VendorID.ToString();
                }

                PopulateVendorModalFields(VendorID);
            }
            else
            {
                ddlVendorSelect.SelectedIndex = 0;
                SetVendorModalVisibility(false);
            }

            mpVendorSelect.Show();
        }

        private void SetVendorError(string errorMsg)
        {
            lblVendorSelectError.Text = errorMsg;
            lblVendorSelectError.Visible = !String.IsNullOrEmpty(errorMsg);
        }
        #endregion

        #region Ship to Modal
      protected void btnShipToCancel_Click(object sender, EventArgs e)
        {
            CloseShipToModal();
        }

        protected void btnShipToReset_Click(object sender, EventArgs e)
        {
            ClearShipToModal(true);
            PopulateModalShipToAddress();
        }

        protected void btnShipToSubmit_Click(object sender, EventArgs e)
        {
            UpdateShipToAddress();
            CloseShipToModal();
        }

        protected void ddlShipToCampus_IndexChanged(object sender, EventArgs e)
        {
            int CampusID = 0;

            int.TryParse(ddlShipToCampus.SelectedValue, out CampusID);
            ClearShipToModal(false);
            PopulateModalShipToAddress(CampusID);
        }

        public void BindCampusList()
        {
            ddlShipToCampus.Items.Clear();

            ddlShipToCampus.DataSource = campusService.Queryable().Where(c => c.LocationId > 0).OrderBy(c => c.Name).ToList();
            ddlShipToCampus.DataValueField = "Id";
            ddlShipToCampus.DataTextField = "Name";
            ddlShipToCampus.DataBind();

            ddlShipToCampus.Items.Insert(0, new ListItem("--Select--", "-1"));

            ddlShipToCampus.Items.Insert(ddlShipToCampus.Items.Count, new ListItem("[Other]", "0"));

        }

        private void ClearShipToModal(bool clearNameAttention)
        {
            if (clearNameAttention)
            {
                txtShipToName.Text = String.Empty;
                txtShipToAttention.Text = String.Empty;
            }

            txtShipToAddress.Text = String.Empty;
            txtShipToCity.Text = String.Empty;
            txtShipToState.Text = String.Empty;
            txtShipToZip.Text = String.Empty;
        }

        private void CloseShipToModal()
        {
            ClearShipToModal(true);
            mpShipToSelect.Hide();
        }

        private void PopulateModalShipToAddress(int campusID)
        {
            if (campusID > 0)
            {
                Rock.Model.Campus c = campusService.Get(campusID);
                if (c.Location != null)
                {
                    txtShipToAddress.Text = c.Location.Street1 + " " + c.Location.Street2;
                    txtShipToCity.Text = c.Location.City;
                    txtShipToState.Text = c.Location.State;
                    txtShipToZip.Text = c.Location.PostalCode;
                }

            }
        }

        private void PopulateModalShipToAddress()
        {
            int CampusID = 0;
            int.TryParse(hfCampusID.Value, out CampusID);

            if (ddlShipToCampus.Items.FindByValue(CampusID.ToString()) != null)
                ddlShipToCampus.SelectedValue = CampusID.ToString();

            txtShipToName.Text = lblShipToName.Text;
            txtShipToAttention.Text = lblShipToAttn.Text;
            txtShipToAddress.Text = lblShipToAddress.Text;
            txtShipToCity.Text = lblShipToCity.Text;
            txtShipToState.Text = lblShipToState.Text;
            txtShipToZip.Text = lblShipToZip.Text;
        }

        private void ShowShipToModal()
        {
            BindCampusList();
            PopulateModalShipToAddress();
            mpShipToSelect.Show();
        }

        private void UpdateShipToAddress()
        {
            hfCampusID.Value = ddlShipToCampus.SelectedValue;
            lblShipToName.Text = txtShipToName.Text;
            lblShipToAttn.Text = txtShipToAttention.Text;
            divShipToAttention.Visible = !String.IsNullOrEmpty(txtShipToAttention.Text);
            divShipToAddress.Visible = !String.IsNullOrEmpty(txtShipToAddress.Text);
            lblShipToAddress.Text = txtShipToAddress.Text;
            lblShipToCity.Text = txtShipToCity.Text;
            lblShipToState.Text = txtShipToState.Text;
            lblShipToZip.Text = txtShipToZip.Text;
        }
        #endregion

        #region Choose Requisition Modal
        protected void ddlChooseRequisitionMinistry_IndexChanged(object sender, EventArgs e)
        {
            ddlChooseRequisitionRequester.SelectedIndex = -1;

            BindRequisitionList(true, false,true);

        }

        protected void ddlChooseRequisitionRequester_IndexChanged(object sender, EventArgs e)
        {
            int requesterID = 0;
            if (int.TryParse(ddlChooseRequisitionRequester.SelectedValue, out requesterID) && requesterID > 0)
                BindRequisitionList(true,false, false);
            else if (ddlChooseRequisitionRequester.SelectedIndex == 0)
            {
                BindRequisitionList(true, false, true);
            }
        }

        protected void btnChooseRequisitionSubmit_Click(object sender, RowEventArgs e)
        {
            SetRequisitionErrorMessage(String.Empty);
            int RequisitionID = e.RowKeyId;
            if (RequisitionID > 0)
            {
                HideRequisitionModel();
                ShowRequisitionItemModal(RequisitionID);
            }
            else
            {
                SetRequisitionErrorMessage("Please select a requisition.");
            }
        }

        protected void btnChooseRequisitionCancel_Click(object sender, EventArgs e)
        {
            HideRequisitionModel();
        }

        private void ClearRequisitionModal()
        {
            SetRequisitionErrorMessage(String.Empty);
            ddlChooseRequisitionMinistry.ClearSelection();
            ddlChooseRequisitionMinistry.Items.Clear();

            ddlChooseRequisitionRequester.ClearSelection();
            ddlChooseRequisitionRequester.Items.Clear();

            ClearRequisitionDataGrid();
        }

        private void ClearRequisitionDataGrid()
        {
            ConfigureRequisitionList();
            BindRequisitionList(false, false, false);
        }

        private void ConfigureRequisitionList()
        {
            dgChooseRequisitions.Visible = true;
            dgChooseRequisitions.ItemType = "Requisions";
            /*dgChooseRequisitions.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgChooseRequisitions.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgChooseRequisitions.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgChooseRequisitions.AllowSorting = false;
            /*dgChooseRequisitions.MergeEnabled = false;
            dgChooseRequisitions.EditEnabled = false;
            dgChooseRequisitions.MailEnabled = false;
            dgChooseRequisitions.AddEnabled = false;
            dgChooseRequisitions.ExportEnabled = false;
            dgChooseRequisitions.DeleteEnabled = false;
            dgChooseRequisitions.SourceTableKeyColumnName = "RequisitionID";
            dgChooseRequisitions.SourceTableOrderColumnName = "RequisitionID";
            dgChooseRequisitions.NoResultText = "No Requisitions with unassigned items found.";*/
        }

        private void BindRequisitionList(bool populate, bool bindMinistryFilter, bool bindRequesterFilter)
        {
            dgChooseRequisitions.Visible = true;

            ConfigureRequisitionList();

            var Reqs = Requisition.LoadAcceptedRequisitonsWithItemsNotOnPO(attributeService.Get(MinistryAreaAttributeIDSetting).Id);

            if (ddlChooseRequisitionMinistry.SelectedIndex > 0)
            {
                Reqs = Reqs.Where(x => x.MinistryID == int.Parse(ddlChooseRequisitionMinistry.SelectedValue)).ToList();
            }

            if (ddlChooseRequisitionRequester.SelectedIndex > 0)
            {
                Reqs = Reqs.Where(x => x.RequesterID == int.Parse(ddlChooseRequisitionRequester.SelectedValue)).ToList();
            }
            DataTable dtRequisitions = new DataTable();
            dtRequisitions.Columns.AddRange(new DataColumn[]
                    {
                        new DataColumn("RequisitionID", typeof(int)),
                        new DataColumn("Title", typeof(string)),
                        new DataColumn("RequesterName", typeof(string)),
                        new DataColumn("DateSubmitted", typeof(string)),
                        new DataColumn("IsApproved", typeof(bool))
                    });

            if (populate)
            {
                foreach (var item in Reqs)
                {
                    DataRow dr = dtRequisitions.NewRow();
                    dr["RequisitionID"] = item.RequisitionID;
                    dr["Title"] = item.Title;
                    dr["RequesterName"] = item.RequesterName;
                    dr["DateSubmitted"] = item.DateSubmitted;
                    dr["IsApproved"] = item.IsApproved;
                    dtRequisitions.Rows.Add(dr);
                }
            }

            dgChooseRequisitions.DataSource = dtRequisitions;
            dgChooseRequisitions.DataBind();

            if (bindRequesterFilter)
            {
                ddlChooseRequisitionRequester.Items.Clear();


                ddlChooseRequisitionRequester.DataSource = Reqs.Select(r => new { r.RequesterID, r.RequesterName, r.RequesterLastFirst }).Distinct().OrderBy(r => r.RequesterLastFirst);
                ddlChooseRequisitionRequester.DataValueField = "RequesterID";
                ddlChooseRequisitionRequester.DataTextField = "RequesterName";
                ddlChooseRequisitionRequester.DataBind();

                ddlChooseRequisitionRequester.Items.Insert(0, new ListItem("[All]", "0"));
            }

            if(bindMinistryFilter)
            {

                ddlChooseRequisitionMinistry.Items.Clear();
                var Ministries = Reqs.Select(r => new { r.MinistryID, r.MinistryName }).Distinct().OrderBy(r => r.MinistryName);

                ddlChooseRequisitionMinistry.DataSource = Ministries;
                ddlChooseRequisitionMinistry.DataValueField = "MinistryID";
                ddlChooseRequisitionMinistry.DataTextField = "MinistryName";
                ddlChooseRequisitionMinistry.DataBind();

                ddlChooseRequisitionMinistry.Items.Insert(0, new ListItem("[All]", "0"));
            }
        }

        //private DataTable GetRequisitions(bool populate)
        //{
        //    DataTable dtRequisitions = new DataTable();
        //    dtRequisitions.Columns.AddRange(new DataColumn[]
        //        {
        //            new DataColumn("RequisitionID", typeof(int)),
        //            new DataColumn("Title", typeof(string)),
        //            new DataColumn("RequesterName", typeof(string)),
        //            new DataColumn("DateSubmitted", typeof(string)),
        //            new DataColumn("IsApproved", typeof(bool))
        //        });

        //    if (!populate)
        //        return dtRequisitions;

        //    var Requisitions = Requisition.LoadAcceptedRequisitonsWithItemsNotOnPO();

        //    int RequesterID = 0;
        //    if (int.TryParse(ddlChooseRequisitionRequester.SelectedValue, out RequesterID) && RequesterID > 0)
        //        Requisitions = Requisitions.Where(x => x.RequesterID == RequesterID).ToList();

        //    int MinistryLUID = 0;
        //    if (int.TryParse(ddlChooseRequisitionMinistry.SelectedValue, out MinistryLUID) && MinistryLUID > 0)
        //        Requisitions = Requisitions.Where(x => Arena.Custom.SECC.Purchasing.Helpers.Person.GetPersonIDByAttributeValue(MinistryPersonAttributeIDSetting, MinistryLUID).Contains(x.RequesterID)).ToList();

        //    foreach (var item in Requisitions)
        //    {
        //        DataRow dr = dtRequisitions.NewRow();
        //        dr["RequisitionID"] = item.RequisitionID;
        //        dr["Title"] = item.Title;
        //        dr["RequesterName"] = item.Requester != null ? item.Requester.FullName : String.Empty;
        //        dr["DateSubmitted"] = item.DateSubmitted != DateTime.MinValue ? item.DateSubmitted.ToShortDateString() : String.Empty;
        //        dr["IsApproved"] = item.IsApproved;

        //        dtRequisitions.Rows.Add(dr);
        //    }

        //    return dtRequisitions;
        //}

        private void HideRequisitionModel()
        {
            ClearRequisitionModal();
            mpChooseRequisition.Hide();
        }

        private void SetRequisitionErrorMessage(string msg)
        {
            lblChooseRequisitionError.InnerText = msg;
            lblChooseRequisitionError.Visible = !String.IsNullOrEmpty(msg);
        }

        private void ShowRequisitionModal()
        {
            ClearRequisitionModal();
            BindRequisitionList(true, true, true);

            mpChooseRequisition.Show();
        }

        private void ShowStaffSelector(string title, string parentPersonIDControl, string refreshButtonControlID)
        {
            ucStaffSearch.Title = title;
            /*ucStaffSearch.ParentPersonControlID = parentPersonIDControl;
            ucStaffSearch.ParentRefreshButtonID = refreshButtonControlID;
            ucStaffSearch.MinistryAreaAttributeID = MinistryAreaAttributeIDSetting;
            ucStaffSearch.PositionAttributeID = PositionAttributeIDSetting;
            ucStaffSearch.PersonDetailPage = PersonDetailPageSetting;*/
            ucStaffSearch.Show();
        }

        #endregion

        #region Requisition Item Model

        protected void btnRequisitionItemsAddToPO_Click(object sender, EventArgs e)
        {
            if (UpdatePOItems())
            {
                HideRequisitionItemModal();
                LoadItems();
            }
        }

        protected void btnRequisitionItemsCancel_Click(object sender, EventArgs e)
        {
            CurrentPurchaseOrder.RefreshItems();
            LoadItems();
            HideRequisitionItemModal();
        }

        protected void dgRequisitionItems_ItemDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                TextBox txtQtyRemaining = (TextBox)e.Row.FindControl("txtRequisitionItemQtyRemaining");
                Label lblQtyRemaining = (Label)e.Row.FindControl("lblRequisitionItemQtyRemaining");
                TextBox txtPrice = (TextBox)e.Row.FindControl("txtRequisitionItemPrice");

                if ((int)drv["QtyRemaining"] == 0)
                {
                    lblQtyRemaining.Text = drv["QtyRemaining"].ToString();
                    lblQtyRemaining.Visible = true;
                    txtQtyRemaining.Visible = false;
                }
                else
                {
                    txtQtyRemaining.Text = drv["QtyRemaining"].ToString();
                    txtQtyRemaining.Visible = true;
                    lblQtyRemaining.Visible = false;
                    txtPrice.Visible = true;

                    if ( drv["PricePerItem"] != null && (decimal)drv["PricePerItem"] != 0 )
                    {
                        txtPrice.Text = string.Format( "{0:N2}", drv["PricePerItem"] );
                    }
                }

                
            }
        }

        private void BindRequisitionItemList(Requisition r)
        {
            ConfigureRequisitionItemList();
            dgRequisitionItems.DataSource = GetRequisitionItems(false, r);
            dgRequisitionItems.DataBind();

        }

        private void ClearRequisitionItemList()
        {
            GetRequisitionItems(false, null);
        }

        private void ClearRequisitionItemModal()
        {
            SetRequistionItemError(String.Empty);
            hfRequisitionID.Value = String.Empty;
            lblRequisitionItemsRequester.Text = String.Empty;
            lblRequisitionItemsTitle.Text = String.Empty;
            ClearRequisitionItemList();
        }

        private void ConfigureRequisitionItemList()
        {
            dgRequisitionItems.Visible = true;
            dgRequisitionItems.ItemType = "Requisions";
            /*dgRequisitionItems.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgRequisitionItems.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgRequisitionItems.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgRequisitionItems.AllowSorting = false;
            /*dgRequisitionItems.MergeEnabled = false;
            dgRequisitionItems.EditEnabled = false;
            dgRequisitionItems.MailEnabled = false;
            dgRequisitionItems.AddEnabled = false;
            dgRequisitionItems.ExportEnabled = false;
            dgRequisitionItems.DeleteEnabled = false;
            dgRequisitionItems.SourceTableKeyColumnName = "ItemID";
            dgRequisitionItems.SourceTableOrderColumnName = "ItemID";
            dgRequisitionItems.NoResultText = "No Items found.";*/
        }

        private List<ItemToAddToPO> GetSelectedItemQuantity()
        {
            List<ItemToAddToPO> Items = new List<ItemToAddToPO>();

            foreach (GridViewRow item in dgRequisitionItems.Rows)
            {
                if (item.RowType == DataControlRowType.DataRow)
                {
                    TextBox qtyTextBox = (TextBox)item.FindControl("txtRequisitionItemQtyRemaining");
                    TextBox priceTextBox = (TextBox)item.FindControl("txtREquisitionItemPrice");

                    if (qtyTextBox != null && qtyTextBox.Visible)
                    {
                        int QtyRequested = 0;
                        int.TryParse(qtyTextBox.Text, out QtyRequested);

                        decimal Price = 0;
                        if(priceTextBox != null && priceTextBox.Visible)
                            decimal.TryParse(priceTextBox.Text, out Price);
                        
                        if (QtyRequested > 0)
                        {
                            Items.Add(new ItemToAddToPO(int.Parse(item.Cells[0].Text), QtyRequested, Price));
                        }
                    }
                }
            }

            return Items;
        }

        private DataTable GetRequisitionItems(bool returnEmpty, Requisition r)
        {
            DataTable ri = new DataTable();
            ri.Columns.AddRange(new DataColumn[] {
                new DataColumn("ItemID", typeof(int)),
                new DataColumn("ItemNumber", typeof(string)),
                new DataColumn("Description", typeof(string)),
                new DataColumn("DateNeeded", typeof(string)),
                new DataColumn("AllowExpedited", typeof(bool)),
                new DataColumn("QtyRequested", typeof(int)),
                new DataColumn("QtyAssigned", typeof(int)),
                new DataColumn("QtyRemaining", typeof(int)),
                new DataColumn("PricePerItem", typeof(decimal))
            });


            if (r == null || returnEmpty)
                return ri;


            foreach (RequisitionItem i in r.Items.Where(x => x.Active))
            {
                DataRow dr = ri.NewRow();
                int QtyAssigned = 0;
                dr["ItemID"] = i.ItemID;
                dr["ItemNumber"] = i.ItemNumber;
                dr["Description"] = i.Description;
                dr["DateNeeded"] = i.DateNeeded != DateTime.MinValue ? string.Format("{0:d}", i.DateNeeded) : String.Empty;
                dr["AllowExpedited"] = i.IsExpeditiedShippingAllowed;
                dr["QtyRequested"] = i.Quantity;
                QtyAssigned = i.POItems.Where(x => x.Active == true).Where(x => x.PurchaseOrder.Active).Where(x => x.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID()).Select(x => x.Quantity == null ? 0 : x.Quantity).Sum();
                dr["QtyAssigned"] = QtyAssigned;
                dr["QtyRemaining"] = i.Quantity - QtyAssigned;
                dr["PricePerItem"] = i.Price;

                ri.Rows.Add(dr);
            }
            return ri;
        }

        private void HideRequisitionItemModal()
        {
            ClearRequisitionItemModal();
            mpRequisitionItems.Hide();
        }

        private void LoadRequisitionItemData(int requisitionID)
        {
            Requisition r = new Requisition(requisitionID);
            hfRequisitionID.Value = r.RequisitionID.ToString();
            lblRequisitionItemsTitle.Text = r.Title;
            lblRequisitionItemsRequester.Text = r.Requester.FullName;
            BindRequisitionItemList(r);
        }

        private void SetRequistionItemError(string msg)
        {
            lblRequisitionItemError.InnerText = msg;
            lblRequisitionItemError.Visible = !String.IsNullOrEmpty(msg);
        }

        private void ShowRequisitionItemModal(int requisitionID)
        {
            ClearRequisitionItemModal();
            LoadRequisitionItemData(requisitionID);
            mpRequisitionItems.Show();
        }

        private bool UpdatePOItems()
        {
            bool IsSuccess = false;
            try
            {
                List<ItemToAddToPO> NewPOItemList = GetSelectedItemQuantity();
                CurrentPurchaseOrder.UpdatePOItems(NewPOItemList, CurrentUser.UserName);

                return true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("Purchase order Items are not valid.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }
                    sb.Append("</ul>");

                    SetRequistionItemError(sb.ToString());
                }
                else
                    throw rEx;
            }
            CurrentPurchaseOrder.RefreshItems();
            if (!IsSuccess)
            {
                int RequisitionID = 0;
                if (int.TryParse(hfRequisitionID.Value, out RequisitionID))
                    BindRequisitionItemList(new Requisition(RequisitionID));
            }
            else
            {
                LoadItems();
            }
            return IsSuccess;
        }
         
        #endregion

        #region OrderSumbit

        protected void btnOrderSubmit_Click(object sender, EventArgs e)
        {
            DateTime OrderDate;
            DateTime.TryParse(txtDateSubmitted.Text, out OrderDate);

            CurrentPurchaseOrder.SubmitOrder(OrderDate, CurrentPerson.PrimaryAliasId.Value, CurrentUser.UserName);
            if (!String.IsNullOrEmpty(txtOrderNotes.Text))
                CurrentPurchaseOrder.SaveNote(txtOrderNotes.Text, CurrentUser.UserName);

            mpOrderSubmit.Hide();
            LoadPO();
        }

        protected void btnOrderSubmitCancel_Click(object sender, EventArgs e)
        {
            ResetOrderSubmitModal();
            mpOrderSubmit.Hide();
        }
        private void ResetOrderSubmitModal()
        {
            txtOrderNotes.Text = String.Empty;
            txtDateSubmitted.Text = DateTime.Now.ToShortDateString();
        }

        private void ShowOrderSubmitModal()
        {
            ResetOrderSubmitModal();
            mpOrderSubmit.Show();
        }
        #endregion

        #region Receive Package
        protected void btnReceivePackageCancel_Click(object sender, EventArgs e)
        {
            HideReceivePackageModel();
        }

        protected void btnReceivePackageReset_Click(object sender, EventArgs e)
        {
            ResetReceivePackageModel();
        }

        protected void btnReceivePackageSubmit_Click(object sender, EventArgs e)
        {
            SetReceivePackageError(String.Empty);
            if(SavePackage())
            {
                HideReceivePackageModel();
                LoadPO();
                BindReceiveItemList();
            }


        }

        protected void btnOtherReceiverModalShow_Click(object sender, EventArgs e)
        {
            ShowOtherReceiverModal();
        }

        protected void btnOtherReceiverSelect_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterClientScriptBlock(upReceivePackage, upReceivePackage.GetType(), "ResetReceivingZindex" + DateTime.Now.Ticks, "$(\"#[id*=mpReceivePackage_pnlMPE]\").css(\"z-index\", \"10001\");", true);

            LoadOtherReceiver();
        }

        protected void ddlReceivedByUser_SelectedIndexChanged(Object sender, EventArgs e)
        {

            if (ddlReceivedByUser.SelectedValue == "-1")
            {
                pnlOtherReceiver.Visible = true;
                ShowOtherReceiverModal();
            }
            else
            {
                pnlOtherReceiver.Visible = false;
            }


            ClearOtherReceiverFields();


        }

        protected void dgReceivePackageItems_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                TextBox txtQtyReceiving = (TextBox)e.Row.FindControl("txtReceivePackageQtyReceiving");
                Label lblQtyReceiving = (Label)e.Row.FindControl("lblReceivePackageQtyReceiving");

                DataRowView drv = (DataRowView)e.Row.DataItem;
                int QtyToReceive = ((int)drv["QtyOrdered"] - (int)drv["PreviouslyReceived"]);
                int ReceiptID = 0;
                int.TryParse(hfReceiptID.Value, out ReceiptID);

                if (ReceiptID > 0)
                {
                    lblQtyReceiving.Text = drv["QtyReceiving"].ToString();
                    lblQtyReceiving.Visible = true;
                }
                else
                {
                    if (QtyToReceive > 0)
                    {
                        txtQtyReceiving.Visible = true;
                    }
                    else
                    {
                        lblQtyReceiving.Text = "0";
                        lblQtyReceiving.Visible = true;
                    }
                }
            }
        }

        protected void dgReceivePackageItems_Rebind(object sender, EventArgs e)
        {
            BindReceiveItemList();
        }

        protected void lbOtherReceiverRemove_Click(object sender, EventArgs e)
        {
            ClearOtherReceiverFields();
        }



        private void BindCarrierList()
        {
            ddlReceivePackageCarriers.Items.Clear();
            ddlReceivePackageCarriers.DataSource = Receipt.GetShippingCarriers(true).OrderBy(x => x.Value);
            ddlReceivePackageCarriers.DataValueField = "Id";
            ddlReceivePackageCarriers.DataTextField = "Value";
            ddlReceivePackageCarriers.DataBind();

            ddlReceivePackageCarriers.Items.Insert(0, new ListItem("--Select--", "0"));
        }

        private void BindReceiveItemList()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[]{
                new DataColumn("POItemID", typeof(int)),
                new DataColumn("QtyOrdered", typeof(int)),
                new DataColumn("ItemNumber", typeof(string)),
                new DataColumn("Description", typeof(string)),
                new DataColumn("DeliverTo", typeof(string)),
                new DataColumn("DateNeeded", typeof(string)),
                new DataColumn("PreviouslyReceived", typeof(int)),
                new DataColumn("QtyReceiving", typeof(int))
            });

            int ReceiptID = 0;
            int.TryParse(hfReceiptID.Value, out ReceiptID);

            foreach (PurchaseOrderItem poi in CurrentPurchaseOrder.Items.Where(x => x.Active))
            {
                DataRow dr = dt.NewRow();
                dr["POItemId"] = poi.PurchaseOrderItemID;
                dr["QtyOrdered"] = poi.Quantity;
                dr["ItemNumber"] = poi.RequisitionItem.ItemNumber;
                dr["Description"] = poi.RequisitionItem.Description;
                dr["DateNeeded"] = poi.RequisitionItem.DateNeeded > DateTime.MinValue ? poi.RequisitionItem.DateNeeded.ToShortDateString() : "";
                dr["DeliverTo"] = poi.RequisitionItem.Requisition.DeliverTo;
                int PreviouslyReceived = 0;
                int QuantityRecieving = 0;
                if (ReceiptID == 0)
                {
                    PreviouslyReceived = poi.ReceiptItems.Where(x => x.Active).Select(x => x.QuantityReceived).Sum();
                }
                else
                {
                    QuantityRecieving = poi.ReceiptItems.Where(x => x.ReceiptID == ReceiptID && x.Active).Select(x => x.QuantityReceived).FirstOrDefault();
                    PreviouslyReceived = poi.ReceiptItems.Where(x => x.ReceiptID != ReceiptID && x.Active).Select(x => x.QuantityReceived).Sum();
                }
                dr["PreviouslyReceived"] = PreviouslyReceived;
                dr["QtyReceiving"] = QuantityRecieving;
                dt.Rows.Add(dr);
            }
            ConfigureReceivePackageItems();
            dgReceivePackageItems.DataSource = dt;
            dgReceivePackageItems.DataBind();
        }

        private void BindReceiverList()
        {
            if (ReceivingUserTagSetting == 0)
                return;

            GroupService groupService = new GroupService(rockContext);
            Group pmc = groupService.Get(ReceivingUserTagSetting);

            //Arena.Core.ProfileMemberCollection pmc = new Arena.Core.ProfileMemberCollection(ReceivingUserTagSetting);

            Dictionary<int, string> ConnectedMembers = new Dictionary<int, string>();
            foreach (GroupMember pm in pmc.Members)
            {
                if (pm.GroupMemberStatus == GroupMemberStatus.Active)
                    ConnectedMembers.Add(pm.PersonId, pm.Person.FullName);
            }

            ddlReceivedByUser.Items.Clear();
            ddlReceivedByUser.DataValueField = "Key";
            ddlReceivedByUser.DataTextField = "Value";
            ddlReceivedByUser.DataSource = ConnectedMembers;
            ddlReceivedByUser.DataBind();

            ddlReceivedByUser.Items.Insert(0, new ListItem("--Select--", "0"));
            ddlReceivedByUser.Items.Insert(ddlReceivedByUser.Items.Count, new ListItem("Other", "-1"));

        }

        private void ClearOtherReceiverFields()
        {
            /*lblOtherReceiverName.Text = "&nbsp;";
            lbOtherReceiverRemove.Visible = false;
            hfOtherReceiverPersonID.Value = String.Empty;*/


        }

        private void ConfigureReceivePackageItems()
        {
            dgReceivePackageItems.Visible = true;
            dgReceivePackageItems.ItemType = "Requisions";
            /*dgReceivePackageItems.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgReceivePackageItems.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgReceivePackageItems.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgReceivePackageItems.AllowSorting = false;
            /*dgReceivePackageItems.MergeEnabled = false;
            dgReceivePackageItems.EditEnabled = false;
            dgReceivePackageItems.MailEnabled = false;
            dgReceivePackageItems.AddEnabled = false;
            dgReceivePackageItems.ExportEnabled = false;
            dgReceivePackageItems.DeleteEnabled = false;
            dgReceivePackageItems.SourceTableKeyColumnName = "ItemID";
            dgReceivePackageItems.SourceTableOrderColumnName = "ItemID";
            dgReceivePackageItems.NoResultText = "No Items found.";*/
        }

        private Dictionary<int, int> GetItemsToReceive()
        {
            Dictionary<int, int> Items = new Dictionary<int, int>();

            foreach (GridViewRow item in dgReceivePackageItems.Rows)
            {
                if (item.RowType == DataControlRowType.DataRow)
                {
                    TextBox txtReceivePackageQtyReceiving = (TextBox)item.FindControl("txtReceivePackageQtyReceiving");
                    int QtyReceiving = 0;
                    int POItemID = dgReceivePackageItems.DataKeys[item.RowIndex][0].ToString().AsInteger();

                    if (!String.IsNullOrEmpty(txtReceivePackageQtyReceiving.Text) && int.TryParse(txtReceivePackageQtyReceiving.Text, out QtyReceiving) && QtyReceiving > 0)
                        Items.Add(POItemID, QtyReceiving);
                }
            }

            return Items;
        }

        private void HideReceivePackageModel()
        {
            hfReceiptID.Value = "0";
            mpReceivePackage.Hide();
        }

        private void LoadOtherReceiver()
        {
            int personID = 0;
            int receiptID = 0;
            /*if (int.TryParse(hfOtherReceiverPersonID.Value, out personID))
            {
                Person p = personAliasService.Get(personID).Person;

                lblOtherReceiverName.Text = p.FullName;

                if (int.TryParse(hfReceiptID.Value, out receiptID) && receiptID == 0)
                {
                    lbOtherReceiverRemove.Visible = true;
                }
            }*/
        }

        private void ResetReceivePackageModel()
        {
            SetReceivePackageError(String.Empty);
            ddlReceivePackageCarriers.SelectedValue = "0";
            ddlReceivedByUser.SelectedValue = "0";
            ClearOtherReceiverFields();
            pnlOtherReceiver.Visible = false;

            txtReceivePackageDateReceived.Text = DateTime.Now.ToShortDateString();

            lblReceivePackageCarriers.Text = String.Empty;
            lblReceivePackageDateReceived.Text = String.Empty; 
            BindReceiveItemList();
            
        }

        private bool SavePackage()
        {
            bool isSuccessful = false;
            try
            {
                Dictionary<int, int> ItemsToReceive = GetItemsToReceive();
                if (ItemsToReceive.Count > 0)
                {
                    int receiverPersonId = 0;
                    int carrierId = 0;

                    if (ddlReceivedByUser.SelectedValue == "-1" && ucStaffSearch.StaffPersonAliasId.HasValue)
                    {
                        receiverPersonId = ucStaffSearch.StaffPersonAliasId.Value;
                    }
                    else
                    {
                        int.TryParse(ddlReceivedByUser.SelectedValue, out receiverPersonId);
                    }

                    int.TryParse(ddlReceivePackageCarriers.SelectedValue, out carrierId);
                    CurrentPurchaseOrder.ReceivePackage(receiverPersonId, carrierId, txtReceivePackageDateReceived.Text, ItemsToReceive, CurrentUser.UserName);
                }

                isSuccessful = true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error has occurred while saving package");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var valError in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", valError.Key, valError.Value);
                    }

                    sb.Append("</ul>");
                    SetReceivePackageError(sb.ToString());
                }
                else
                    throw rEx;
            }

            return isSuccessful;
        }

        private void ShowOtherReceiverModal()
        {
            ScriptManager.RegisterStartupScript(upReceivePackage, upReceivePackage.GetType(), "UpdateReceivingZIndex" + DateTime.Now.Ticks, "$(\"#[id*=mpReceivePackage_pnlMPE]\").css(\"z-index\", \"10001\");", true);
            ShowStaffSelector("Other Receiver", null, null);


        }

        private void ShowReceivePackageModel(int receiptID)
        {
            hfReceiptID.Value = receiptID.ToString();
            BindCarrierList();
            BindReceiverList();
            ResetReceivePackageModel();

            if (receiptID > 0)
            {
                PopulateReceivePackageFields(receiptID);
            }
            SetReceivePackageVisibility(receiptID == 0);
            
            mpReceivePackage.Show();
        }

        private void PopulateReceivePackageFields(int receiptID)
        {
            Receipt Receipt = CurrentPurchaseOrder.Receipts.Where(r => r.ReceiptID == receiptID).FirstOrDefault();

            lblReceivePackageDateReceived.Text = Receipt.DateReceived.ToShortDateString();
            lblReceivePackageCarriers.Text = Receipt.ShippingCarrier.Value;
            lblRecevedByUser.Text = Receipt.ReceivedBy.NickName + " " + Receipt.ReceivedBy.LastName;

        }

        private void SetReceivePackageVisibility(bool isNew)
        {
            ddlReceivePackageCarriers.Visible = isNew;
            txtReceivePackageDateReceived.Visible = isNew;
            lblReceivePackageCarriers.Visible = !isNew;
            lblReceivePackageDateReceived.Visible = !isNew;
            mpReceivePackage.SaveButtonText = isNew?"Receive":String.Empty;
            lblRecevedByUser.Visible = !isNew;
            ddlReceivedByUser.Visible = isNew;
        }


        private void SetReceivePackageError(string msg)
        {
            lblReceivePackageError.InnerHtml = msg;
            lblReceivePackageError.Visible = !String.IsNullOrEmpty(msg);
        }
        #endregion

        #region Payment Details
        protected void btnPaymentMethodPaymentAdd_Click(object sender, EventArgs e)
        {
            AddPayment();
        }
        protected void btnPaymentMethodPaymentClose_Click(object sender, EventArgs e)
        {
            HidePaymentDetailModal();
        }
        protected void btnPaymentMethodPaymentReset_Click(object sender, EventArgs e)
        {
            int paymentID = 0;
            int.TryParse(hfPaymentID.Value, out paymentID);
            ResetPaymentDetialFields();
            LoadPaymentMethodFields(paymentID);
        }

        protected void btnPaymentMethodPaymentUpdateCharges_Click(object sender, EventArgs e)
        {
            SetPaymentMethodError(String.Empty);
            int PaymentID = 0;
            int.TryParse(hfPaymentID.Value, out PaymentID);

            if (UpdatePaymentCharges(PaymentID))
            {
                ResetPaymentDetialFields();
                HidePaymentDetailModal();
            }
        }

        protected void btnPaymentMethodPaymentResetCharges_Click(object sender, EventArgs e)
        {
            int paymentID = 0;
            int.TryParse(hfPaymentID.Value, out paymentID);
            ResetPaymentDetialFields();
            LoadPaymentMethodFields(paymentID);
        }

        protected void dgPaymentDetailCharges_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                TextBox txtChargeAmount = (TextBox)e.Row.FindControl("txtChargeAmount");
                Label lblChargeAmount = (Label)e.Row.FindControl("lblChargeAmount");
                Decimal ChargeAmount = 0;
                decimal.TryParse(drv["ChargeAmount"].ToString(), out ChargeAmount);
                txtChargeAmount.Text = ChargeAmount.ToString("0.00");
                lblChargeAmount.Text = ChargeAmount.ToString("0.00");

                if (CanUserEditPayments())
                {
                    txtChargeAmount.Visible = true;
                    lblChargeAmount.Visible = false;
                }
                else
                {
                    txtChargeAmount.Visible = false;
                    lblChargeAmount.Visible = true;
                }
            }
        }

        private void AddPayment()
        {
            try
            {
                SetPaymentMethodError(string.Empty);
                int paymentTypeID = 0;
                DateTime paymentDate = DateTime.MinValue;
                Decimal paymentAmount = 0;

                int.TryParse(ddlPaymentMethodPaymentType.SelectedValue, out paymentTypeID);
                DateTime.TryParse(txtPaymentMethodPaymentDate.Text, out paymentDate);
                decimal.TryParse(txtPaymentMethodPaymentAmount.Text, out paymentAmount);
                int paymentID =  CurrentPurchaseOrder.AddPayment(paymentTypeID, paymentDate, paymentAmount, CurrentUser.UserName);
                
                if (paymentID > 0)
                {
                    ResetPaymentDetialFields();
                    LoadPaymentMethodFields(paymentID);
                }
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error occurred while adding a payment");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }

                    sb.Append("</ul>");

                    SetPaymentMethodError(sb.ToString());
                }
                else
                {
                    throw rEx;
                }
            }

        }

        private void BindPaymentChargesGrid(Payment pymt)
        {
            ConfigurePaymentChargesGrid();

            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("RequisitionID", typeof(int)),
                new DataColumn("CompanyID", typeof(int)),
                new DataColumn("AccountNumber", typeof(string)),
                new DataColumn("FiscalYearStart", typeof(DateTime)),
                new DataColumn("Requisition", typeof(string)),
                new DataColumn("ChargeAmount", typeof(string))
            });

            var RequisitionAccount = CurrentPurchaseOrder.Items.Where(i => i.Active)
                    .Select(i => new
                    {
                        RequisitionID = i.RequisitionItem.RequisitionID,
                        RequisitionTitle = i.RequisitionItem.Requisition.Title,
                        CompanyID = i.RequisitionItem.CompanyID,
                        FundID = i.RequisitionItem.FundID,
                        DepartmentID = i.RequisitionItem.DepartmentID,
                        AccountID = i.RequisitionItem.AccountID,
                        FYStartDate = i.RequisitionItem.FYStartDate,
                        POItemId = i.PurchaseOrderItemID
                    })
                    .GroupBy(i => new
                        {
                            i.RequisitionID,
                            i.RequisitionTitle,
                            i.CompanyID,
                            i.FundID,
                            i.DepartmentID,
                            i.AccountID,
                            i.FYStartDate
                        })
                    .Select(g => new
                        {
                            g.Key.RequisitionID,
                            g.Key.RequisitionTitle,
                            g.Key.CompanyID,
                            g.Key.FundID,
                            g.Key.DepartmentID,
                            g.Key.AccountID,
                            g.Key.FYStartDate,
                            MinPOItemId = g.Min(a => a.POItemId)
                        }
                    ).OrderBy(g => g.MinPOItemId);

            foreach (var ra in RequisitionAccount)
            {
                var Charge = pymt.Charges.FirstOrDefault(c => c.RequisitionID == ra.RequisitionID
                                                            && c.CompanyID == ra.CompanyID
                                                            && c.FundID == ra.FundID
                                                            && c.DepartmentID == ra.DepartmentID
                                                            && c.AccountID == ra.AccountID
                                                            && c.FYStartDate == ra.FYStartDate
                                                            && c.Active);



                DataRow dr = dt.NewRow();
                dr["RequisitionID"] = ra.RequisitionID;
                dr["CompanyID"] = ra.CompanyID;
                dr["AccountNumber"] = string.Format("{0}-{1}-{2}", ra.FundID, ra.DepartmentID, ra.AccountID);
                dr["FiscalYearStart"] = ra.FYStartDate;
                dr["Requisition"] = string.Format("{0} - {1}", ra.RequisitionID, ra.RequisitionTitle);
                if (Charge != null)
                    dr["ChargeAmount"] = Charge.Amount;
                else
                {
                    if (RequisitionAccount.Count() > 1)
                    {
                        dr["ChargeAmount"] = 0;
                    }
                    else
                    {
                        dr["ChargeAmount"] = pymt.PaymentAmount;
                    }
                }      
                dt.Rows.Add(dr);
            }

            dgPaymentDetailCharges.DataSource = dt;
            dgPaymentDetailCharges.DataBind();
            /*dgPaymentDetailCharges.Columns[0].Visible = false;
            dgPaymentDetailCharges.Columns[1].Visible = false;
            dgPaymentDetailCharges.Columns[2].Visible = false;
            dgPaymentDetailCharges.Columns[3].Visible = false;*/
        }

        private void BindPaymentMethodList()
        {
            ddlPaymentMethodPaymentType.Items.Clear();

            ddlPaymentMethodPaymentType.DataSource = PaymentMethod.LoadPaymentMethods().Where(m => m.Active).OrderBy(m => m.Name);
            ddlPaymentMethodPaymentType.DataValueField = "PaymentMethodID";
            ddlPaymentMethodPaymentType.DataTextField = "Name";
            ddlPaymentMethodPaymentType.DataBind();

            ddlPaymentMethodPaymentType.Items.Insert(0, new ListItem("--Select--", "0"));
        }

        private void ConfigurePaymentChargesGrid()
        {
            dgPaymentDetailCharges.Visible = true;
            dgPaymentDetailCharges.ItemType = "Items";
            /*dgPaymentDetailCharges.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgPaymentDetailCharges.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgPaymentDetailCharges.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgPaymentDetailCharges.AllowSorting = false;
            /*dgPaymentDetailCharges.MergeEnabled = false;
            dgPaymentDetailCharges.EditEnabled = false;
            dgPaymentDetailCharges.MailEnabled = false;
            dgPaymentDetailCharges.AddEnabled = false;
            dgPaymentDetailCharges.ExportEnabled = false;
            dgPaymentDetailCharges.DeleteEnabled = false;
            dgPaymentDetailCharges.SourceTableKeyColumnName = "RequisitionID";
            dgPaymentDetailCharges.SourceTableOrderColumnName = "RequisitionID";
            dgPaymentDetailCharges.NoResultText = "No charges found.";*/
        }

        private void HidePaymentDetailModal()
        {
            ResetPaymentDetialFields();
            LoadPayments();
            mpPayments.Hide();
        }

        private void LoadPaymentMethodFields(int paymentID)
        {
            hfPaymentID.Value = paymentID.ToString();

            if (paymentID > 0)
            {
                Payment pymt = CurrentPurchaseOrder.Payments.FirstOrDefault(p => p.PaymentID == paymentID);

                if (ddlPaymentMethodPaymentType.Items.FindByValue(pymt.PaymentMethodID.ToString()) != null)
                    ddlPaymentMethodPaymentType.SelectedValue = pymt.PaymentMethodID.ToString();
                lblPaymentMethodPaymentType.Text = pymt.PaymentMethod.Name;

                txtPaymentMethodPaymentDate.Text = pymt.PaymentDate.ToShortDateString();
                lblPaymentMethodPaymentDate.Text = pymt.PaymentDate.ToShortDateString();

                txtPaymentMethodPaymentAmount.Text = pymt.PaymentAmount.ToString();
                lblPaymentMethodPaymentAmount.Text = string.Format("{0:c}", pymt.PaymentAmount);

                BindPaymentChargesGrid(pymt);
            }

            SetPaymentDetailVisibility(paymentID == 0);
        }

        private void ResetPaymentDetialFields()
        {
            SetPaymentMethodError(String.Empty);
            hfPaymentID.Value = String.Empty;

            if (CurrentPurchaseOrder != null && CurrentPurchaseOrder.DefaultPaymentMethodID > 0)
            {
                if (ddlPaymentMethodPaymentType.Items.FindByValue(CurrentPurchaseOrder.DefaultPaymentMethodID.ToString()) != null)
                {
                    ddlPaymentMethodPaymentType.SelectedValue = CurrentPurchaseOrder.DefaultPaymentMethodID.ToString();
                    lblPaymentMethodPaymentType.Text = CurrentPurchaseOrder.DefaultPaymentMethod.Name;
                }
                else
                {
                    ddlPaymentMethodPaymentType.SelectedValue = "0";
                    lblPaymentMethodPaymentType.Text = "&nbsp;";
                }
            }
            else
            {
                ddlPaymentMethodPaymentType.SelectedValue = "0";
                lblPaymentMethodPaymentType.Text = "&nbsp;";
            }

            txtPaymentMethodPaymentDate.Text = DateTime.Now.ToShortDateString();
            lblPaymentMethodPaymentDate.Text = "&nbsp;";

            txtPaymentMethodPaymentAmount.Text = String.Empty;
            lblPaymentMethodPaymentAmount.Text = "&nbsp;";
        }

        private void ShowPaymentDetailModal(int paymentID)
        {
            BindPaymentMethodList();
            ResetPaymentDetialFields();
            LoadPaymentMethodFields(paymentID);
            mpPayments.Show();
        }

        private void SetPaymentDetailVisibility(bool isNew)
        {
            ddlPaymentMethodPaymentType.Visible = isNew;
            lblPaymentMethodPaymentType.Visible = !isNew;

            txtPaymentMethodPaymentDate.Visible = isNew;
            lblPaymentMethodPaymentDate.Visible = !isNew;

            txtPaymentMethodPaymentAmount.Visible = isNew;
            lblPaymentMethodPaymentAmount.Visible = !isNew;

            mpPayments.Footer.FindControl("btnPaymentMethodPaymentAdd").Visible = isNew;
            mpPayments.Footer.FindControl("btnPaymentMethodPaymentAddCharges").Visible = !isNew;

            divPaymentMethodCharges.Visible = !isNew;
        }

        private void SetPaymentMethodError(string msg)
        {
            lblPaymentMethodError.InnerText = msg;
            lblPaymentMethodError.Visible = !String.IsNullOrEmpty(msg);
        }

        private bool UpdatePaymentCharges(int paymentID)
        {
            bool UpdatedSuccessfully = false;
            try
            {
                Payment Pay = CurrentPurchaseOrder.Payments.FirstOrDefault(x => x.PaymentID == paymentID);

                foreach (GridViewRow dgi in dgPaymentDetailCharges.Rows)
                {
                    if(dgi.RowType == DataControlRowType.DataRow)
                    {
                        int RequisitionID = dgPaymentDetailCharges.DataKeys[dgi.RowIndex][0].ToString().AsInteger();
                        int CompanyID = dgPaymentDetailCharges.DataKeys[dgi.RowIndex][1].ToString().AsInteger();
                        int FundID = 0;
                        int DeptID = 0;
                        int AcctID = 0;
                        DateTime? FYStartDate = dgPaymentDetailCharges.DataKeys[dgi.RowIndex][3].ToString().AsDateTime();
                        decimal ChargeAmount = 0;
                        string[] AcctPartArr = dgPaymentDetailCharges.DataKeys[dgi.RowIndex][2].ToString().Split("-".ToCharArray());

                        if (AcctPartArr.Length == 3)
                        {
                            int.TryParse(AcctPartArr[0], out FundID);
                            int.TryParse(AcctPartArr[1], out DeptID);
                            int.TryParse(AcctPartArr[2], out AcctID);
                        }

                        TextBox txtCharge = (TextBox)dgi.FindControl("txtChargeAmount");

                        if (txtCharge != null && txtCharge.Visible)
                            decimal.TryParse(txtCharge.Text, out ChargeAmount);

                        if (RequisitionID > 0 && CompanyID > 0 && FundID > 0 && DeptID > 0 && AcctID > 0 && FYStartDate.HasValue)
                        {
                            PaymentCharge Charge = Pay.Charges.Where(c => c.RequisitionID == RequisitionID
                                                                        && c.CompanyID == CompanyID
                                                                        && c.FundID == FundID
                                                                        && c.DepartmentID == DeptID
                                                                        && c.AccountID == AcctID
                                                                        && c.FYStartDate == FYStartDate.Value
                                                                        && c.Active).FirstOrDefault();

                            if (Charge == null)
                            {
                                Charge = new PaymentCharge();
                                Charge.PaymentID = Pay.PaymentID;
                                Charge.RequisitionID = RequisitionID;
                                Charge.CompanyID = CompanyID;
                                Charge.FundID = FundID;
                                Charge.DepartmentID = DeptID;
                                Charge.AccountID = AcctID;
                                Charge.FYStartDate = FYStartDate.Value;
                            }

                            Charge.Amount = ChargeAmount;
                            Charge.Save(CurrentUser.UserName);
                        }

                    }
                }
                UpdatedSuccessfully = true;
                CurrentPurchaseOrder.RefreshPayments();
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error has occurred while saving payment charges.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }

                    sb.Append("</ul>");
                    SetPaymentMethodError(sb.ToString());
                }
                else
                    throw rEx;

            }

            return UpdatedSuccessfully;
        }

        #endregion

        #region Item Details
        protected void btnIDClose_Click(object sender, EventArgs e)
        {
            HideItemDetailsModal();
        }

        protected void btnIDReset_Click(object sender, EventArgs e)
        {
            int POIID = 0;
            int.TryParse(hfIDItemID.Value, out POIID);
            ClearItemDetailsModal();
            PopulateItemDetailsModal(POIID);
        }

        protected void btnIDUpdate_Click(object sender, EventArgs e)
        {
            SetItemDetailsModalErrors(String.Empty);
            if (UpdateItemDetails())
            {
                HideItemDetailsModal();
                LoadItems();
            }
        }

        private void BindItemDetailsReceiptsGrid(PurchaseOrderItem poi)
        {

            ConfigureItemDetailsReceiptsGrid();
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("ReceiptID", typeof(int)),
                new DataColumn("Carrier", typeof(string)),
                new DataColumn("DateReceived", typeof(string)),
                new DataColumn("QtyReceived", typeof(int)),
                new DataColumn("ReceivedBy", typeof(string))
            });

            if (poi != null)
            {
                var ReceiptSummary =  poi.ReceiptItems.Where(ri => ri.Active).GroupBy(rig => rig.ReceiptID)
                                    .Select(rig => new
                                    {
                                        Receipt = new Receipt(rig.Key),
                                        QtyReceived = rig.Sum(q => q.QuantityReceived)
                                    });

                foreach (var i in ReceiptSummary)
                {
                    DataRow dr = dt.NewRow();
                    dr["ReceiptID"] = i.Receipt.ReceiptID;
                    dr["Carrier"] = i.Receipt.ShippingCarrier;
                    dr["DateReceived"] = i.Receipt.DateReceived.ToShortDateString();
                    dr["QtyReceived"] = i.QtyReceived;
                    dr["ReceivedBy"] = i.Receipt.ReceivedBy.FullName;

                    dt.Rows.Add(dr);
                }
            }

            dgIDReceipts.DataSource = dt;
            dgIDReceipts.DataBind();
        }

        private void ClearItemDetailsModal()
        {
            SetItemDetailsModalErrors(String.Empty);
            hfIDItemID.Value = String.Empty;
            lblIDItemNumber.Text = "&nbsp;";
            lblIDDescription.Text = "&nbsp;";
            lblIDDateNeeded.Text = "&nbsp;";
            lblIDQtyAssigned.Text = "&nbsp;";
            lblIDReceivedUnassignedValue.Label = "&nbsp;";
            lblIDReceivedUnassignedValue.Text = "&nbsp;";
            lblIDAccount.Text = "&nbsp;";
            lblIDPrice.Text = "&nbsp;";

            imgIDExpedite.Text = "No";

            txtIDQtyAssigned.Text = String.Empty;
            txtIDPrice.Text = String.Empty;

            BindItemDetailsReceiptsGrid(null);
        }

        private void ConfigureItemDetailsReceiptsGrid()
        {
            dgIDReceipts.Visible = true;
            dgIDReceipts.ItemType = "ReceiptID";
            /*dgIDReceipts.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgIDReceipts.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgIDReceipts.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgIDReceipts.AllowSorting = false;
            /*dgIDReceipts.MergeEnabled = false;
            dgIDReceipts.EditEnabled = false;
            dgIDReceipts.MailEnabled = false;
            dgIDReceipts.AddEnabled = false;
            dgIDReceipts.ExportEnabled = false;
            dgIDReceipts.DeleteEnabled = false;
            dgIDReceipts.SourceTableKeyColumnName = "ReceiptID";
            dgIDReceipts.SourceTableOrderColumnName = "ReceiptID";
            dgIDReceipts.NoResultText = "Item has not been received.";*/
        }
        
        private void HideItemDetailsModal()
        {
            ClearItemDetailsModal();
            mpItemDetails.Hide();
        }

        private void PopulateItemDetailsModal(int poiID)
        {
            if (CurrentPurchaseOrder == null)
                return;

            PurchaseOrderItem POI = CurrentPurchaseOrder.Items.FirstOrDefault(x => x.PurchaseOrderItemID == poiID);

            if (POI != null)
            {
                hfIDItemID.Value = POI.PurchaseOrderItemID.ToString();
                if (!String.IsNullOrEmpty(POI.RequisitionItem.ItemNumber))
                    lblIDItemNumber.Text = POI.RequisitionItem.ItemNumber;
                if (!String.IsNullOrEmpty(POI.RequisitionItem.Description))
                    lblIDDescription.Text = POI.RequisitionItem.Description;
                if (POI.RequisitionItem.DateNeeded > DateTime.MinValue)
                    lblIDDateNeeded.Text = POI.RequisitionItem.DateNeeded.ToShortDateString();
                txtIDQtyAssigned.Text = POI.Quantity.ToString();
                lblIDQtyAssigned.Text = POI.Quantity.ToString();

                imgIDExpedite.Text = POI.RequisitionItem.IsExpeditiedShippingAllowed?"Yes":"No";

                if (POI.PurchaseOrder.DateOrdered > DateTime.MinValue)
                {
                    lblIDReceivedUnassignedValue.Label = "Qty Received";
                    lblIDReceivedUnassignedValue.Text = POI.ReceiptItems.Where(x => x.Active).Select(x => x.QuantityReceived).Sum().ToString();

                }
                else
                {
                    lblIDReceivedUnassignedValue.Label = "Qty Unassigned";
                    lblIDReceivedUnassignedValue.Text = (POI.RequisitionItem.Quantity - POI.RequisitionItem.POItems.Where(x => x.Active).Select(x => x.Quantity).Sum()).ToString();
                }

                lblIDAccount.Text = string.Format("{0}-{1}-{2}", POI.RequisitionItem.FundID, POI.RequisitionItem.DepartmentID, POI.RequisitionItem.AccountID);

                if ( POI.Price != null && POI.Price != 0 )
                {
                    lblIDPrice.Text = POI.Price.ToString( "0.00;(0.00)" );
                    txtIDPrice.Text = POI.Price.ToString( "0.00" );
                }
                BindItemDetailsReceiptsGrid(POI);
                SetItemDetailsVisibility();
            }
        }

        private void ShowItemDetailsModal(int poiID)
        {
            if (CurrentPurchaseOrder == null || poiID <= 0)
                return;
            ClearItemDetailsModal();
            PopulateItemDetailsModal(poiID);
            mpItemDetails.Show();
        }

        private void SetItemDetailsModalErrors(string msg)
        {
            lblIDError.InnerHtml = msg;
            lblIDError.Visible = !String.IsNullOrEmpty(msg);
        }

        private void SetItemDetailsVisibility()
        {
            bool UserCanEdit = CanUserEditItem();
            bool ShowQtyTB = UserCanEdit;
            bool ShowPriceTB = UserCanEdit;

            txtIDQtyAssigned.Visible = ShowQtyTB;
            lblIDQtyAssigned.Visible = !ShowQtyTB;

            txtIDPrice.Visible = ShowPriceTB;
            lblIDPrice.Visible = !ShowPriceTB;

            if (!(ShowQtyTB || ShowPriceTB)) {
                mpItemDetails.SaveButtonText = String.Empty;
            }
        }

        private bool UpdateItemDetails()
        {
            bool SuccessfullyUpdated = false;
            try
            {
                int POIID = 0;
                int.TryParse(hfIDItemID.Value, out POIID);
                PurchaseOrderItem POI = CurrentPurchaseOrder.Items.FirstOrDefault(x => x.PurchaseOrderItemID == POIID);

                if (POI == null || POI.PurchaseOrderItemID == 0)
                    return SuccessfullyUpdated;

                int QtyAssigned = 0;
                decimal Price = 0;

                if (txtIDQtyAssigned.Visible && int.TryParse(txtIDQtyAssigned.Text, out QtyAssigned))
                {
                    //Quantity was updated and was less than the qty received.
                    if (POI.ReceiptItems.Where(ri => ri.Active).Select(ri => (int?)ri.QuantityReceived ?? 0).Sum() > QtyAssigned && POI.Quantity != QtyAssigned)
                    {
                        Dictionary<string, string> errors = new Dictionary<string, string>();
                        errors.Add("Quantity", "Item quantity is less than the quantity received.");

                        throw new RequisitionException("An error has occurred updating purchase order item", new RequisitionNotValidException("Purchase order item not valid,", errors));

                    }
                    else
                    {
                        POI.Quantity = QtyAssigned;
                    }
                }

                if ( txtIDPrice.Visible && ( decimal.TryParse( txtIDPrice.Text, out Price ) || String.IsNullOrWhiteSpace( txtIDPrice.Text ) ) )
                {
                    POI.Price = Price;
                }
                POI.Save(CurrentUser.UserName);

                POI.RequisitionItem.Requisition.SyncStatus(CurrentUser.UserName);
                CurrentPurchaseOrder.RefreshItems();
                SuccessfullyUpdated = true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error occurred while updating Item Detail");
                    sb.Append("<ul type=\"disc\">");

                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }
                    sb.Append("</ul>");

                    SetItemDetailsModalErrors(sb.ToString());
                }
                else
                {
                    throw rEx;
                }
            }

            return SuccessfullyUpdated;
        }

        #endregion
    }

}