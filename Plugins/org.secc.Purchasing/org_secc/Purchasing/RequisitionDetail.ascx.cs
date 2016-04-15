using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.Purchasing;
using org.secc.Purchasing.Accounting;
using Rock.Web.UI;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock;
using Rock.Web.Cache;
using System.Web.UI.HtmlControls;
using Rock.Data;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName("Requisition Detail")]
    [Category("Purchasing")]
    [Description("Requisition Details page (display/edit a single Req).")]

    [TextField("New Requisition Title", "Title to show for new requisitions", false, "", "General")]
    [TextField("Footer Text", "Text to display in page footer.", false, "", "General")]
    [TextField("Scripture Text", "Highlighted scripture to display on page.", false, "", "General")]
    [TextField("Choose Vendor Instructions", "Text to display in choose vendor modal window. Default is blank.", false, "", "Vendor")]
    [DefinedTypeField("Default Requisition Type", "The requisition type that is selected by default when creating a new requisition", true, "BD564328-4C68-4BC1-9F82-63621192AB8A")]
    [BooleanField("Allow New Vendor Selection", "True/False flag to indicate if the user is allowed to enter a preferred vendor that is not currently on the vendor list.", true, "Vendor")]
    [IntegerField("Expedited Shipping Window", "Number of days from current date to prompt user for permission to use expedited shipping. (Default is 7 days from current date).", false, 7, "General")]
    [BooleanField("Display Inactive Items", "Display item detail records that have been marked as inactive. Default is false", false, "Items")]
    [LinkedPage("Person Detail Page", "Person Detail Page", false, "", "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "MinistryAreaAttributeID", "Ministry Area Attribute ID. Default is 63.", false, false, "", "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "PositionAttributeID", "Position Attribute ID. Default is 29.", false, false, "", "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "MinistryLocationAttributeID", "Ministry Location Attribute ID. Default is 14031.", false, false, "", "Staff Selector")]
    [BooleanField("Send Approval Request Notification", "Send Approval Request Notification to new approvers when they are added to the requisition.", false, "Notifications")]
    [BooleanField("Send Requisition Approved Notification", "Send notification to requester when the requisition has been approved.", true, "Notifications")]
    [SystemEmailField("Requisition Approved Notification Template", "Communication Template for Approval Request Notification.", true, "", "Notifications")]
    [SystemEmailField("Approval Request Notification Template", "Communication Template for Approval Request Notification.", true, "", "Notifications")]
    [SystemEmailField("Approval Declined Notification Template", "Communication Template for Approval Declined Notifications.", true, "", "Notifications")]
    [BooleanField("Send emails to template recipients only.", "Only send email messages to recipients that are included in the email template. This is setting for test purposes only.", false, "Notifications")]
    [BooleanField("Prompt for Note on Decline", "Prompt user for note when declining a requisition.", false, "Approvals")]
    [SystemEmailField("New Requisition in Purchasing Queue Notification", "Notification for when new requisition has been added to the purchasing queue.", true, "", "Notifications")]
    [SystemEmailField("Requisition Submitted To Purchasing Notification", "Notification to requester when requisition has been submitted to purchasing.", true, "", "Notifications")]
    [SystemEmailField("Requisition Returned To Requester Notification", "Notification to requester when requisition has been returned to requester.", true, "", "Notifications")]
    [SystemEmailField("Requisition Accepted by Purchasing Notification", "Notification to requester when purchasing has accepted the requisition.", true, "", "Notifications")]
    [LinkedPage("Requisition List Page", "Page that shows the requisition list", true, "", "General")]
    [LinkedPage("Purchase Order Detail Page", "Page that shows the Purchase Order", true, "", "General")]
    [LinkedPage("Capital Request Detail Page", "Page that shows the Capital Request", true, "", "General")]
    [BooleanField("Enable Notifications", "Enable Notification Emails", false, "General")]
    [SecurityRoleField("Requester roles that override notification block during beta.", "Requester roles that override notification block during beta period. If a requester is in this role, their requisitions will still trigger email notifications. This is a temporary role during the beta period.", false, "", "Notifications")]
    public partial class RequisitionDetail : RockBlock
    {

        #region Fields
        Requisition mRequisition;
        bool? mUserCanEditItem;
        private int mItemCount = 0;
        Rock.Data.RockContext rockContext = new Rock.Data.RockContext();
        DefinedValueService definedValueService = null;
        SystemEmailService systemEmailService = null;
        PersonAliasService personAliasService = null;
        UserLoginService userLoginService = null;
        AttributeService attributeService = null;
        #endregion


        #region ModuleSettings
        public string NewRequistionTitleSetting { get { return GetAttributeValue("New Requisition Title"); } }

        public string FooterTextSetting { get { return GetAttributeValue("FooterText"); } }

        public string ScriptureTextSetting { get { return GetAttributeValue("ScriptureText"); } }

        public string ChooseVendorInstructionsSetting { get { return GetAttributeValue("ChooseVendorInstructions"); } }

        public int? DefaultRequistionTypeSetting { get { return GetAttributeValue("DefaultRequistionType").AsIntegerOrNull(); } }

        public bool AllowNewVendorSelectionSetting { get { return bool.Parse(GetAttributeValue("AllowNewVendorSelection")); } }

        public string ExpeditedShippingWindowDaysSetting { get { return GetAttributeValue("ExpeditedShippingWindowDays"); } }

        public bool ShowInactiveItemsSetting { get { return GetAttributeValue("ShowInactiveItemsSetting").AsBoolean(); } }

        public string PersonDetailPageSetting { get { return GetAttributeValue("PersonDetailPage"); } }

        public Rock.Model.Attribute MinistryAreaAttribute
        {
            get
            {
                Guid? guid = GetAttributeValue("MinistryAreaAttributeID").AsGuidOrNull();
                if (guid.HasValue)
                {
                    return attributeService.Get(guid.Value);
                }
                return null;
            }
        }

        public Rock.Model.Attribute PositionAttribute
        {
            get
            {
                Guid? guid = GetAttributeValue("PositionAttributeID").AsGuidOrNull();
                if (guid.HasValue)
                {
                    return attributeService.Get(guid.Value);
                }
                return null;
            }
        }

        public Rock.Model.Attribute MinistryLocationAttribute
        {
            get
            {
                Guid? guid = GetAttributeValue("MinistryLocationAttributeID").AsGuidOrNull();
                if (guid.HasValue)
                {
                    return attributeService.Get(guid.Value);
                }
                return null;
            }
        }

        public bool SendApprovalRequestNotificationSetting
        {
            get
            {
                bool sendNotificaiton = false;

                bool.TryParse(GetAttributeValue("SendApprovalRequestNotification"), out sendNotificaiton);

                return sendNotificaiton;
            }
        }

        public bool SendRequisitionApprovedNotificationSetting
        {
            get
            {
                bool sendNotification = false;
                bool.TryParse(GetAttributeValue("SendRequisitionApprovedNotification"), out sendNotification);
                return sendNotification;
            }
        }

        public int RequisitionApprovedNotificationTemplateSetting
        {
            get
            {
                int templateID = 0;
                int.TryParse(GetAttributeValue("RequisitionApprovedNotificationTemplate"), out templateID);

                return templateID;
            }
        }
        public int ApprovalRequestNotificationTemplateSetting
        {
            get
            {
                int templateID = 0;
                int.TryParse(GetAttributeValue("ApprovalRequestNotificationTemplate"), out templateID);

                return templateID;

            }
        }

        public int ApprovalDeclinedNotificationTemplateSetting
        {
            get
            {
                int templateID = 0;
                int.TryParse(GetAttributeValue("ApprovalDeclinedNotificationTemplate"), out templateID);

                return templateID;

            }
        }
        public bool SendEmailsToTemplateRecepientsOnlySetting
        {
            get
            {
                bool templateOnly = false;
                bool.TryParse(GetAttributeValue("SendEmailsToTemplateRecepientsOnly"), out templateOnly);

                return templateOnly;
            }
        }

        public bool PromptForNoteOnDecline
        {
            get
            {
                bool promptForNote = false;

                bool.TryParse(GetAttributeValue("PromptForNoteOnDecline"), out promptForNote);

                return promptForNote;
            }
        }

        public Guid? NewRequisitionInPurchasingQueueNotificationSetting
        {
            get
            {
                return GetAttributeValue("NewRequisitioninPurchasingQueueNotification").AsGuidOrNull();
            }
        }

        public Guid? RequisitionSubmittedToPurchasingSetting
        {
            get
            {
                return GetAttributeValue("RequisitionSubmittedToPurchasingNotification").AsGuidOrNull();
            }
        }

        public int ReturnedToRequesterNotificationSetting
        {
            get
            {
                int templateID = 0;
                int.TryParse(GetAttributeValue("ReturnedToRequesterNotification"), out templateID);

                return templateID;

            }
        }

        public int AcceptedByPurchasingNotificationSetting
        {
            get
            {
                int templateID = 0;
                int.TryParse(GetAttributeValue("AcceptedByPurchasingNotification"), out templateID);

                return templateID;

            }
        }

        public Guid RequisitionListPageSetting
        {
            get
            {
                return GetAttributeValue("RequisitionListPage").AsGuid();
            }
        }

        public string PurchaseOrderDetailPageSetting
        {
            get
            {
                return GetAttributeValue("PurchaseOrderDetailPage");
            }
        }

        public string CapitalRequestDetailPageSetting
        {
            get
            {
                return GetAttributeValue("CapitalRequestDetailPage");
            }
        }

        public bool EnableNotificationSetting
        {
            get
            {
                return GetAttributeValue("EnableNotifications").AsBoolean();
            }
        }
        
        public List<int> BetaRequesterNotificationOverrideRolesSetting
        {
            get 
            {
                List<int> RoleList = new List<int>();
                string settingValue = GetAttributeValue("BetaRequesterNotificationOverrideRoles");
                if(!String.IsNullOrEmpty(settingValue))
                {
                    RoleList.AddRange(settingValue.Split(",".ToCharArray()).Select(r => int.Parse(r)));
                }
                //return Setting("BetaRequesterNotificationOverrideRoles", "", false).Split(",".ToCharArray()).Select(r => int.Parse(r)).ToList();
                return RoleList;
            }
        }

        
        #endregion

        #region Properties
        protected int RequisitionID
        {
            get
            {
                int reqID = 0;

                if (ViewState[BlockId + "_RequisitionID"] != null)
                {
                    int.TryParse(ViewState[BlockId + "_RequisitionID"].ToString(), out reqID);
                }
                if (reqID == 0 && Request.QueryString["RequisitionID"] != null)
                    int.TryParse(Request.QueryString["RequisitionID"], out reqID);

                return reqID;
            }
            set
            {
                ViewState[BlockId + "_RequisitionID"] = value;
            }
        }

        protected Requisition CurrentRequisition
        {
            get
            {
                if (mRequisition == null && RequisitionID > 0)
                    CurrentRequisition = new Requisition(RequisitionID);
                return mRequisition;
            }
            set
            {
                mRequisition = value;
            }
        }

        protected string StateType 
        {
            get
            {
                string stateType = String.Empty;
                if (ViewState[BlockId + "_StateType"] != null)
                    stateType = ViewState[BlockId + "_StateType"].ToString();
                return stateType;
            }
            set
            {
                ViewState[BlockId + "_StateType"] = value;
            }
        
        }
        protected string Disposition
        {
            get
            {
                string disposition = String.Empty;
                if (ViewState[BlockId + "_Disposition"] != null)
                    disposition = ViewState[BlockId + "_Disposition"].ToString();
                return disposition;
            }
            set
            {
                ViewState[BlockId + "_Disposition"] = value;
            }
        }

        private int ApprovedStatusLUID
        {
            get
            {
                int approvedStatusLUID = 0;
                if (ViewState[BlockId + "_ApprovedStatusLUID"] != null)
                    approvedStatusLUID = (int)ViewState[BlockId + "_ApprovedStatusLUID"];
                else
                {
                    approvedStatusLUID = Approval.ApprovedStatusLUID();
                    ViewState[BlockId + "_ApprovedStatusLUID"] = approvedStatusLUID;
                }

                return approvedStatusLUID;
            }
        }

        private int ApprovedForwardStatusLUID
        {
            get
            {
                int approvedForwardStatusLUID = 0;
                if (ViewState[BlockId + "_ApprovedForwardStatusLUID"] != null)
                    approvedForwardStatusLUID = (int)ViewState[BlockId + "_ApprovedForwardStatusLUID"];
                else
                {
                    approvedForwardStatusLUID = Approval.ApprovedAndForwardLUID();
                    ViewState[BlockId + "_ApprovedForwardStatusLUID"] = approvedForwardStatusLUID;
                }

                return approvedForwardStatusLUID;
            }
        }

        private int NotApprovedStatusLUID
        {
            get
            {
                int notApprovedStatusLUID = 0;
                if (ViewState[BlockId + "_NotApprovedStatusLUID"] != null)
                    notApprovedStatusLUID = (int)ViewState[BlockId + "_NotApprovedStatusLUID"];
                else
                {
                    notApprovedStatusLUID = Approval.NotApprovedStatusLUID();
                    ViewState[BlockId + "_NotApprovedStatusLUID"] = notApprovedStatusLUID;
                }

                return notApprovedStatusLUID;
            }
        }

        private int PendingApprovalStatusLUID
        {
            get
            {
                int pendingApprovalLUID = 0;
                if (ViewState[BlockId + "_PendingApprovalStatusLUID"] != null)
                    pendingApprovalLUID = (int)ViewState[BlockId + "_PendingApprovalStatusLUID"];
                else
                {
                    pendingApprovalLUID = Approval.PendingApprovalStatusLUID();
                    ViewState[BlockId + "_PendingApprovalStatusLUID"] = pendingApprovalLUID;
                }
                return pendingApprovalLUID;
            }
        }

        private bool UserIsApprover
        {
            get
            {
                bool isApprover = false;

                if (ViewState[BlockId + "_UserIsApprover"] != null)
                {
                    isApprover = (bool)ViewState[BlockId + "_UserIsApprover"];
                }
                else if (CurrentRequisition != null)
                {
                    isApprover = CurrentRequisition.UserIsApprover(CurrentPerson.PrimaryAliasId.Value);
                    ViewState[BlockId + "_UserIsApprover"] = isApprover;
                }

                return isApprover;
            }
        }

        private bool UserIsCreator
        {
            get
            {
                bool isCreator = false;
                if (ViewState[BlockId + "_UserIsCreator"] != null)
                {
                    isCreator = (bool)ViewState[BlockId + "_UserIsCreator"];
                }
                else if (CurrentRequisition != null && CurrentRequisition.CreatedBy.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
                {
                    isCreator = true;
                    ViewState[BlockId + "_UserIsCreator"] = isCreator;
                }

                return isCreator;
            }
        }

        private bool RequesterIsInMyMinitry
        {
            get
            {
                bool inSameMinistry = false;

                if (ViewState[BlockId + "_RequesterIsInMyMinistry"] != null)
                {
                    inSameMinistry = (bool)ViewState[BlockId + "_RequesterIsInMyMinistry"];
                }
                else
                {
                    if (CurrentRequisition != null && MinistryAreaAttribute != null)
                    {
                        inSameMinistry = CurrentRequisition.RequesterIsInMyMinistry(CurrentPerson.PrimaryAliasId.Value, MinistryAreaAttribute.Key);
                        ViewState[BlockId + "_RequesterIsInMyMinistry"] = inSameMinistry;
                    }
                }

                return inSameMinistry;
            }
        }

        private bool CreatorIsInMyMinistry
        {
            get
            {
                bool inSameMinistry = false;

                if (ViewState[BlockId + "_CreatorIsInMyMinistry"] != null)
                {
                    inSameMinistry = (bool)ViewState[BlockId + "_CreatorIsInMyMinistry"];
                }
                else
                {
                    if (CurrentRequisition != null && MinistryAreaAttribute != null)
                    {
                        inSameMinistry = CurrentRequisition.CreatorIsInMyMinistry(CurrentPerson.PrimaryAliasId.Value, MinistryAreaAttribute.Key);
                        ViewState[BlockId + "_CreatorIsInMyMinistry"] = inSameMinistry;
                    }
                }

                return inSameMinistry;
            }
        }

        


        #endregion

        #region Page Events
        protected void Page_Load(object sender, EventArgs e)
        {


            if (!Page.IsPostBack)
            {
                RequisitionID = 0;
                LoadRequisitionTypes();
                LoadRequisition();

                hfExpeditedShippingDays.Value = ExpeditedShippingWindowDaysSetting;
                ucVendorSelect.CanAddNewVendor = AllowNewVendorSelectionSetting;
                ucStaffPicker.MinistryAreaAttributeGuid = MinistryAreaAttribute.Guid;
                ucStaffPicker.PositionAttributeGuid = PositionAttribute.Guid;
            }
            string baseUrl = CurrentPageReference.BuildUrl();

            /*if (CurrentRequisition != null && CurrentRequisition.RequisitionID > 0)
            {
                baseUrl = string.Format("~/default.aspx?page={0}&RequisitionID={1}", CurrentPageReference.PageId, CurrentRequisition.RequisitionID);
            }
            else
            {
                baseUrl = string.Format("~/default.aspx?page={0}", CurrentPageReference.PageId);
            }*/

            lnkAttachments.NavigateUrl = baseUrl + "#attachments";
            lnkNotes.NavigateUrl = baseUrl + "#notes";

            dgItems.ShowFooter = true;
        }
        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            ucStaffPickerApprover.MinistryAreaAttributeGuid = MinistryAreaAttribute.Guid;
            ucStaffPickerApprover.PositionAttributeGuid = PositionAttribute.Guid;
        }

        protected override void OnInit(EventArgs e)
        {

            definedValueService = new DefinedValueService(rockContext);
            systemEmailService = new SystemEmailService(rockContext);
            personAliasService = new PersonAliasService(rockContext);
            userLoginService = new UserLoginService(rockContext);
            attributeService = new AttributeService(rockContext);

            //mpiAttachmentPicker.Url = "/DocumentBrowser.aspx?callback=selectDocument&SelectedID=#selectedID#&DocumentTypeID=#documentTypeID#";
            mpiAttachmentPicker.Height = 300;
            mpiAttachmentPicker.Width = 320;
            //mpiAttachmentPicker.JSFunctionName = "openChooseDocumentWindow(selectedID, documentTypeID)";
            //mpiAttachmentPicker.Title = "Attach Item";

            ucPurchasingNotes.RefreshParent += new EventHandler(ucNotes_RefreshParent);
            ucAttachments.RefreshParent += new EventHandler(ucAttachments_RefreshParent);

            dgItems.Actions.AddClick += new EventHandler(dgItems_AddItem);
            dgItems.RowCommand += new GridViewCommandEventHandler(dgItems_ItemCommand);
            dgItems.GridRebind += new GridRebindEventHandler(dgItems_ReBind);
            base.OnInit(e);
        }

        protected override void OnPreRender(EventArgs e)
        {
            LoadToolbarLinks();
            base.OnPreRender(e);
        }

        protected void btnItemDetailsUpdate_click(object sender, EventArgs e)
        {
            if (SaveItemDetail())
            {
                LoadItems();
                mpItemDetail.Hide();
            }
        }

        protected void btnItemDetialsSaveNew_click(object sender, EventArgs e)
        {
            if(SaveItemDetail())
                PopulateItemDetailData(0);
            LoadItems();
        }

        protected void btnItemDetailsReset_click(object sender, EventArgs e)
        {
            int itemID = 0;
            int.TryParse(hfItemID.Value, out itemID);

            ClearItemDetailModal();
            PopulateItemDetailData(itemID);
        }

        protected void btnItemDetailsCancel_click(object sender, EventArgs e)
        {
            ClearItemDetailModal();
            LoadItems();
            mpItemDetail.Hide();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(ihPersonList.Value))
                return;

            string[] personList = ihPersonList.Value.Split(",".ToCharArray());
            int pID = 0;

            if (personList.Length > 0 && int.TryParse(personList[0], out pID))
            {
                ClearRequester();
                SetRequester(pID);
            }

            ihPersonList.Value = String.Empty;
        }

        protected void btnReturnToRequesterPart2_Click(object sender, EventArgs e)
        {
            CurrentRequisition.DateSubmitted = DateTime.MinValue;
            CurrentRequisition.DateAccepted = DateTime.MinValue;
            CurrentRequisition.StatusLUID = Requisition.ReturnedToRequesterLUID();
            CurrentRequisition.Save( CurrentUser.UserName );

            int noteID = 0;

            if(int.TryParse(hfReturnToSenderNoteID.Value, out noteID))
            {
                org.secc.Purchasing.Note n = new org.secc.Purchasing.Note(noteID);
                SendReturnToRequesterNotification(n.Body);
                LoadRequisition();
            }
        }

        protected void btnVendorModalUpdate_click(object sender, EventArgs e)
        {
            if (ucVendorSelect.Update())
            {
                mpChooseVendor.Hide();
                UpdateVendorLabel();
            }
        }

        protected void btnVendorModalReset_click(object sender, EventArgs e)
        {
            ucVendorSelect.Reset();
        }

        protected void btnVendorModalCancel_click(object sender, EventArgs e)
        {
            ucVendorSelect.Reset();
            mpChooseVendor.Hide();
        }

        protected void btnVendorModalShow_click(object sender, EventArgs e)
        {
            bool CanEdit = CanUserEditSummary();
            SetVendorModalButtonStatus(CanEdit);
            divVendorInstructions.Visible = (CanEdit && !String.IsNullOrEmpty(ChooseVendorInstructionsSetting));
            ucVendorSelect.Show();
            mpChooseVendor.Show();

        }

        protected void dgItems_AddItem(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void dgItems_ItemCommand(object sender, GridViewCommandEventArgs e)
        {
            bool ReloadItems = false;

            int Argument = 0;
            int.TryParse(e.CommandArgument.ToString(), out Argument);

            switch (e.CommandName.ToLower())
            {

                case "remove":
                    DeleteItem(Argument);
                    ReloadItems = true;
                    break;
                case "update":
                    LoadItemDetailModal(Argument);
                    break;
                case "loadpurchaseorder":
                    ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "LoadPOPopup" + DateTime.Now.Ticks, "window.open(\"/default.aspx?page=" + PurchaseOrderDetailPageSetting + "&poid=" + Argument.ToString() + "\",\"_blank\");", true);
                    break;
                case "viewpolist":
                    ShowItemPurchaseOrdersModal(Argument);
                    break;
                default:
                    break;
            }
            if (ReloadItems)
                LoadItems();
        }

        protected void dgItems_OnRowDataBound(object sender, GridViewRowEventArgs e)
        {
            //if (e.Item.ItemType == ListItemType.Header)
            //{
            //    CheckBox chkAll = (CheckBox)e.Item.FindControl("chkItemDetailsAll");
            //    chkAll.Visible = mItemCount > 0;
            //    `
            //}
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                LinkButton lbRemove = (LinkButton)e.Row.FindControl("lbRemove");
                LinkButton lbEdit = (LinkButton)e.Row.FindControl("lbEdit");
                Literal litPOs = (Literal)e.Row.FindControl("litPOs");

                RequisitionItemListItem Item = (RequisitionItemListItem)e.Row.DataItem;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (Item.PONumbers.Count == 0)
                {
                    litPOs.Text = "N/A";
                }
                else
                {
                    for (int i = 0; i < Item.PONumbers.Count(); i++)
                    {
                        if (UserCanEdit)
                        {
                            sb.AppendFormat("<a href=\"/default.aspx?page={0}&poid={1}\" target=\"_blank\">{2}</a>", PurchaseOrderDetailPageSetting, Item.PONumbers[i], Item.PONumbers[i]);
                        }
                        else
                        {
                            sb.Append(Item.PONumbers[i]);
                        }

                        if (i < (Item.PONumbers.Count() - 1))
                        {
                            sb.Append(", ");
                        }
                    }

                    litPOs.Text = sb.ToString();
                }

                lbRemove.CommandArgument = Item.ItemID.ToString();
                lbEdit.CommandArgument = Item.ItemID.ToString();
                lbRemove.Visible = UserCanDeleteItem(Item.ItemID);
                e.Row.Cells[11].Visible = UserCanDeleteItem(Item.ItemID);
                lbEdit.Visible = CanUserEditItemDetail();
                e.Row.Cells[12].Visible = CanUserEditItemDetail();
            }
        }

        protected void dgItems_PreRender(object sender, EventArgs e)
        {
            if (dgItems.HeaderRow == null)
                return;

            // If we have no rows, just hide the last 2 columns and return
            if (dgItems.Rows.Count == 0)
            {
                dgItems.HeaderRow.Cells[dgItems.HeaderRow.Cells.Count - 1].Visible = false;
                dgItems.HeaderRow.Cells[dgItems.HeaderRow.Cells.Count - 2].Visible = false;
                return;
            }
            // Hide all the header rows
            foreach (TableCell theCell in dgItems.HeaderRow.Cells)
            {
                theCell.Visible = false;
            }
            // Hide all the footer rows
            if (dgItems.FooterRow != null) { 
                foreach (TableCell theCell in dgItems.FooterRow.Cells)
                {
                    theCell.Visible = false;
                }
            }
            
            foreach (GridViewRow theRow in dgItems.Rows)
            {
                for (var i = 0; i<theRow.Cells.Count; i++)
                {
                    TableCell theCell = theRow.Cells[i];
                    dgItems.HeaderRow.Cells[i].Visible = dgItems.HeaderRow.Cells[i].Visible || theCell.Visible;
                    if (dgItems.FooterRow != null)
                    {
                        dgItems.FooterRow.Cells[i].Visible = dgItems.FooterRow.Cells[i].Visible || theCell.Visible;
                    }
                }
            }
        }

        protected void dgItems_ReBind(object sender, EventArgs e)
        {
            LoadItems();
        }

        protected void lbVendorRemove_click(object sender, EventArgs e)
        {
            ucVendorSelect.Clear();
            UpdateVendorLabel(); 
        }



        protected void ToolbarItem_click(object sender, EventArgs e)
        {

            try
            {
                LinkButton lb = (LinkButton)sender;

                bool HasChanged = HasSummaryChanged();

                // Do not complete rest of checks if return is requested
                if (lb.CommandName.ToLower() == "return")
                {
                    ReturnToList();
                    return;
                }


                if (RequisitionID == 0 && !HasChanged)
                    return;

                if (HasChanged || (CurrentRequisition != null && CurrentRequisition.MinistryLUID == 0) )
                    SaveSummary();

                switch (lb.CommandName.ToLower())
                {
                    case "acceptrequisition":
                        AcceptRequisition();
                        break;
                    case "addattachment":
                        LoadAddAttachment();
                        break;
                    case "additem":
                        LoadItemDetailModal(0);
                        break;
                    case "addnote":
                        LoadNoteDetail();
                        break;
                    case "save":
                        break;
                    case "requestapproval":
                        ShowSelectApproverDialog();
                        break;
                    case "submittopurchasing":
                        SendToPurchasing(CurrentRequisition.IsApproved);
                        break;
                    case "returntorequester":
                        ReturnToRequesterStep1();
                        break;
                    case "cancelrequisition":
                        ShowCancelPrompt();
                        break;
                    case "additemtopo":
                        ShowSelectPurchaseOrderModal();
                        break;
                    default:
                        break;
                }
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    if (RequisitionID > 0)
                    {
                        CurrentRequisition = new Requisition(RequisitionID);
                        LoadRequisition();
                    }
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error has occurred while saving requisition.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }
                    sb.Append("</ul>");
                    SetSummaryError(sb.ToString());

                }
                else
                    throw rEx;
            }            
        }

        protected void ucAttachments_RefreshParent(object sender, EventArgs e)
        {
            CurrentRequisition.RefreshAttachments();
            LoadIcons();
        }

        protected void ucNotes_RefreshParent(object sender, EventArgs e)
        {
            CurrentRequisition.RefreshNotes();
            LoadIcons();
        }

        protected void mpCancelPrompt_Click(object sender, EventArgs e)
        {
            Button btnMPCancel = (Button)sender;

            switch (btnMPCancel.CommandName.ToLower())
            {
                case "cancelyes":
                    CancelRequisition();
                    LoadRequisition();
                    break;
                case "cancelno":
                    break;
            }

            mpCancelPrompt.Hide();
        }

        protected void ddlType_SelectedIndexChanged( object sender, EventArgs e )
        {
            int typeLUID = 0;
            bool requiresCER = false;

            if ( int.TryParse( ddlType.SelectedValue, out typeLUID ) )
            {
                DefinedValueService definedValueService = new DefinedValueService(new Rock.Data.RockContext());
                var typeLU = definedValueService.Get(typeLUID );
                if (typeLU != null && typeLU.AttributeValues["CERRequired"].Value.AsBoolean())
                {
                    requiresCER = true;
                }
            }

            divCapitalRequest.Visible = requiresCER;

        }
       
        #endregion

        #region Private

        private bool CanUserEditSummary()
        {
            bool isEditable = false;

            if (CurrentRequisition != null )
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Active)
                {
                    //If person is requester or creator
                    if (UserCanEdit)
                    {
                        isEditable = true;
                    }
                    else if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID ||
                            UserIsCreator ||
                            RequesterIsInMyMinitry ||
                            CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID())
                            isEditable = true;
                    }
                }
            }
            else
            {
                isEditable = true;
            }

            return isEditable;
        }

        private bool CanUserEditItemDetail()
        {
            bool isEditable = false;

            if (CurrentRequisition != null)
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Active)
                {
                    //If person is requester or creator
                    if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID ||
                            UserIsCreator ||
                            RequesterIsInMyMinitry ||
                            CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID())
                            isEditable = true;
                    }
                    //If person is editor
                    if (!isEditable && UserCanEdit)
                    {
                        if (CurrentRequisition.IsOpen)
                            isEditable = true;
                    }
                }
            }
            else
            {
                isEditable = true;
            }

            return isEditable;
        }

        private bool CanUserAddNewItem()
        {
            bool isEditable = false;

            if (CurrentRequisition != null)
            {
                if ( CurrentRequisition.IsOpen && CurrentRequisition.Active )
                {
                    //If person is requester or creator
                    if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID ||
                            UserIsCreator ||
                            RequesterIsInMyMinitry ||
                            CreatorIsInMyMinistry)
                    {
                        if ((CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID()) && !CurrentRequisition.IsApproved )
                            isEditable = true;
                    }
                    //If person is editor
                    if (!isEditable && UserCanEdit)
                    {
                        if (CurrentRequisition.IsOpen)
                            isEditable = true;
                    }
                }
            }
            else
            {
                isEditable = true;
            }

            return isEditable;
        }

        private bool CanUserEditNotes()
        {
            bool isEditable = false;

            if (CurrentRequisition != null)
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Active)
                {
                    //If person is requester or creator
                    if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID ||
                            UserIsCreator ||
                            RequesterIsInMyMinitry ||
                            CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || CurrentRequisition.StatusLUID == Requisition.ApprovedLUID() || CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID())
                            isEditable = true;
                    }
                    if (!isEditable && UserIsApprover && CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID())
                        isEditable = true;
                    if (!isEditable && UserCanEdit && CurrentRequisition.IsOpen)
                        isEditable = true;
                }
            }
            else
            {
                isEditable = true;
            }

            return isEditable;
        }

        private bool CanUserEditAttachments()
        {
            bool isEditable = false;

            if (CurrentRequisition != null)
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Active)
                {
                    //If person is requester or creator
                    if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID ||
                            UserIsCreator ||
                            RequesterIsInMyMinitry ||
                            CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || 
                            CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID() || CurrentRequisition.StatusLUID == Requisition.ApprovedLUID())
                            isEditable = true;
                    }
                    //If person is editor
                    if (!isEditable && UserCanEdit && CurrentRequisition.IsOpen)
                        isEditable = true;
                }
            }
            else
            {
                isEditable = true;
            }

            return isEditable;

        }

        private bool CanUserAddItemToPO()
        {
            bool canUserAddToPO = false;
            if (CurrentRequisition != null && CurrentRequisition.Active && (CurrentRequisition.StatusLUID == Requisition.AcceptedByPurchasingLUID() || CurrentRequisition.StatusLUID == Requisition.PartiallyOrderedLUID()))
            {
                if ( CurrentRequisition.Items.Where(
                        ri => ri.Active
                            && ri.POItems
                                .Where( poi => poi.Active
                                        && poi.PurchaseOrder.Active
                                        && poi.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID() )
                                .Sum( poi => poi.Quantity ) < ri.Quantity
                    ).Count() > 0 )
                {

                    if ( UserCanEdit )
                    {
                        canUserAddToPO = true;
                    }
                }
            }

            return canUserAddToPO;
        }

        private bool CanUserCancelRequisition()
        {
            bool canCancel = false;

            if (CurrentRequisition != null && CurrentRequisition.Active && CurrentRequisition.IsOpen && 
                CurrentRequisition.Items.Where( i => i.Active
                    && i.POItems.Count( poi => poi.Active
                        && poi.PurchaseOrder.Active
                        && poi.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID() ) > 0 )
                    .Count() == 0)
            {
                if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID || UserIsCreator)
                    if ( CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID() ||
                        CurrentRequisition.StatusLUID == Requisition.ApprovedLUID() )
                        canCancel = true;
                if (!canCancel && UserCanEdit)
                    canCancel = true;
            }
            return canCancel;
        }

        private bool CanUserEditApprovals()
        {
            bool isEditable = false;

            if (CurrentRequisition != null)
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Active && CurrentRequisition.Items.Where( x=> x.Active).Count() > 0)
                {
                    if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID || UserIsCreator || RequesterIsInMyMinitry || CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID() || CurrentRequisition.StatusLUID == Requisition.ApprovedLUID())
                            isEditable = true;
                    }
                    if (!isEditable && UserCanEdit)
                        isEditable = true;
                    if (!isEditable && UserIsApprover && CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID() && CurrentRequisition.IsOpen)
                        isEditable = true;
                }
            }
            else
            {
                isEditable = false;
            }

            return isEditable;
        }

        private bool CanUserSubmitToPurchasing()
        {
            bool canSubmit = false;

            if (CurrentRequisition != null)
            {
                if (CurrentRequisition.IsOpen && CurrentRequisition.Items.Count > 0 && CurrentRequisition.Active)
                {
                    if (UserCanEdit)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.DraftLUID() || CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID() || CurrentRequisition.StatusLUID == Requisition.ApprovedLUID())
                        {
                            canSubmit = true;
                        }
                    }
                    else if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID || UserIsCreator || RequesterIsInMyMinitry || CreatorIsInMyMinistry)
                    {
                        if (CurrentRequisition.StatusLUID == Requisition.ApprovedLUID() || (CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() && CurrentRequisition.IsApproved))
                        {
                            canSubmit = true;
                        }
                    }

                }
            }
            else
            {
                if(UserCanEdit)
                {
                    canSubmit = true;
                }
            }

            return canSubmit;
        }

        private bool CanUserAcceptOrReturnRequisition()
        {
            bool canAccept = false;

            if (CurrentRequisition != null && CurrentRequisition.IsOpen && CurrentRequisition.Active)
            {

                if (UserCanEdit && CurrentRequisition.StatusLUID == Requisition.SubmittedToPurchasingLUID())
                    canAccept = true;
            }

            return canAccept;
        }

        private void ClearRequester()
        {
            //hdnRequesterID.Value = String.Empty;
            //lblRequesterName.Text = String.Empty;
        }

        private void ClearSummary()
        {
            ucVendorSelect.Clear();
            ClearRequester();
            if (ddlType.Items.FindByValue("") != null)
                ddlType.SelectedValue = "";
            SetRequester(CurrentPerson.PrimaryAliasId.Value);

            txtDeliverTo.Text = String.Empty;
            txtTitle.Text = String.Empty;

            lblTitle.Text = NewRequistionTitleSetting;
            lblStatus.Text = Requisition.GetStatuses(true).OrderBy(x => x.Order).FirstOrDefault().Value;
            lblApproval.Text = String.Empty;
            UpdateVendorLabel();
        }


        private void ConfigureItemGrid()
        {
            dgItems.Visible = true;
            dgItems.ItemType = "Items";
            /*dgItems.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgItems.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgItems.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);
            */
            dgItems.AllowSorting = true;
            /*dgItems.NoResultText = "No Items found on requisition";
            dgItems.MergeEnabled = false;
            dgItems.EditEnabled = false;
            dgItems.MailEnabled = false;
            dgItems.AddEnabled = false;
            dgItems.DeleteEnabled = false;
            dgItems.SourceTableKeyColumnName = "ItemID";
            dgItems.SourceTableOrderColumnName = "Description";
            dgItems.AddImageUrl = "~/images/addButton.png";
             */
        }

        private void DeleteItem(int itemID)
        {
            RequisitionItem item = CurrentRequisition.Items.FirstOrDefault(i => i.ItemID == itemID);

            if (item != null && item.CanBeDeleted())
                item.SoftDelete(CurrentUser.UserName);
        }

        protected bool UserCanDeleteItem(int itemID)
        {
            bool CanBeDeleted = false;
            if (!CanUserEditItemDetail())
                return CanBeDeleted;

            if (RequisitionID > 0 && CurrentRequisition.Items != null)
                CanBeDeleted =  CurrentRequisition.Items.FirstOrDefault(i => i.ItemID == itemID).CanBeDeleted();

            return CanBeDeleted;
        }

        private void LoadAddAttachment()
        {
            if (ucAttachments.Identifier == 0)
                ucAttachments.Identifier = RequisitionID;

            mdAttachment.Show();
        }

        private void LoadApprovals()
        {
            BindApprovalRequestGrid();
        }

        private void LoadAttachments()
        {
            ucAttachments.ReadOnly = !CanUserEditAttachments();
            ucAttachments.LoadAttachmentControl(typeof(Requisition).ToString(), RequisitionID);
            ucAttachments.CurrentUser = CurrentUser;
        }

        private void LoadCharges()
        {
            BindCharges();
        }

        private void LoadIcons()
        {
            if (CurrentRequisition != null && CurrentRequisition.RequisitionID > 0)
            {
                lnkAttachments.Visible = CurrentRequisition.Attachments.Where(a => a.Active).Count() > 0;
                lnkNotes.Visible = CurrentRequisition.Notes.Where(a => a.Active).Count() > 0;
            }
        }

        private void LoadRequisition()
        {
            LoadIcons();
            LoadSummary();
            LoadItems();
            LoadNotes();
            LoadAttachments();
            LoadApprovals();
            LoadCharges();
        }

        private void LoadNotes()
        {
            ucPurchasingNotes.ReadOnly = !CanUserEditNotes();
            ucPurchasingNotes.UserHasParentEditPermission = UserCanEdit;
            ucPurchasingNotes.LoadNoteList(typeof(Requisition).ToString(), RequisitionID);
            ucPurchasingNotes.CurrentUserName = CurrentUser.UserName;

        }

        private void LoadSummary()
        {
            ClearSummary();
            SetSummaryVisibility();

            if (RequisitionID > 0)
            {
                lblTitle.Text = CurrentRequisition.Title;
                lblStatus.Text = CurrentRequisition.Status.Value;
                lblStatus.Text = CurrentRequisition.Status.Value;

                if (CurrentRequisition.IsApproved)
                    lblApproval.Text = "Yes";
                else
                    lblApproval.Text = "No";

                if (ddlType.Items.FindByValue(CurrentRequisition.RequisitionTypeLUID.ToString()) != null)
                    ddlType.SelectedValue = CurrentRequisition.RequisitionTypeLUID.ToString();
                else
                {
                    ddlType.SelectedValue = "";
                    ddlType.Text = CurrentRequisition.RequisitionType.Value;
                }
                SetRequester(CurrentRequisition.RequesterID);


                txtTitle.Text = CurrentRequisition.Title;
                txtDeliverTo.Text = CurrentRequisition.DeliverTo;

                SetStatusNote();
                LoadVendor();
                SetCapitalRequest( CurrentRequisition.CapitalRequestId );
            }
        }

        private void LoadItems()
        {

            ConfigureItemGrid();



            int itemCount = 0;
            int itemReceivedCount = 0;

            List<RequisitionItemListItem> ItemList = null;
            if (CurrentRequisition != null)
            {
                ItemList = CurrentRequisition.GetListItems().OrderBy(i => i.ItemID).ToList();

                if (!ShowInactiveItemsSetting)
                    ItemList.RemoveAll(i => i.Active == false);

                mItemCount = ItemList.Count;


                itemCount = ItemList.Sum(x => (int?) x.Quantity ?? 0);
                itemReceivedCount = ItemList.Sum(x => (int?)x.QuantityReceived ?? 0);
            }

            dgItems.DataSource = ItemList;

            if (itemCount > 0)
            {
                dgItems.FooterStyle.CssClass = dgItems.FooterStyle.CssClass + " itemTotals";
                dgItems.Columns[1].FooterStyle.HorizontalAlign = HorizontalAlign.Right;
                dgItems.Columns[1].FooterText = itemCount.ToString();


                dgItems.Columns[2].FooterStyle.HorizontalAlign = HorizontalAlign.Right;
                dgItems.Columns[2].FooterText = itemReceivedCount.ToString();
            }
            else
            {
                dgItems.FooterStyle.CssClass = dgItems.FooterStyle.CssClass.Replace(" itemTotals", "");
            }

            dgItems.DataBind();



            
        }

        private void LoadNoteDetail()
        {
            ucPurchasingNotes.ResetVariableProperties();
            if (ucPurchasingNotes.Identifier == 0)
                ucPurchasingNotes.Identifier = RequisitionID;
            ucPurchasingNotes.ReadOnly = !CanUserEditNotes();
            ucPurchasingNotes.ShowNoteDetail();
            ucPurchasingNotes.CurrentUserName = CurrentUser.UserName;

        }

        private void LoadRequisitionTypes()
        {
            ddlType.Items.Clear();
            List<DefinedValue> Types = Requisition.GetRequisitionTypes(true);
            ddlType.DataTextField = "Value";
            ddlType.DataValueField = "Id";
            ddlType.DataSource = Types;
            ddlType.DataBind();

            ddlType.Items.Insert(0, new ListItem("", ""));
        }


        private void LoadVendor()
        {
            PreferredVendor PV = CurrentRequisition.PreferredVendor;
            if (PV == null)
                return;

            if (PV.VendorID > 0)
            {
                lblVendor.Text = PV.Vendor.VendorName;
                lbVendorRemove.Visible = CanUserEditSummary();
                ucVendorSelect.VendorID = PV.VendorID;
                ucVendorSelect.VendorName = PV.Vendor.VendorName;

                if (PV.Vendor.Address != null)
                    ucVendorSelect.VendorAddress = PV.Vendor.Address.ToArenaFormat();
                else
                    ucVendorSelect.VendorAddress = String.Empty;

                if (PV.Vendor.Phone != null)
                    ucVendorSelect.VendorPhone = PV.Vendor.Phone.ToArenaFormat();
                else
                    ucVendorSelect.VendorPhone = String.Empty;

                if (!String.IsNullOrEmpty(PV.Vendor.WebAddress))
                    ucVendorSelect.VendorWebAddress = PV.Vendor.WebAddress;
                else
                    ucVendorSelect.VendorWebAddress = String.Empty;
            }
            else
            {
                lblVendor.Text = PV.Name;
                ucVendorSelect.VendorID = 0;
                ucVendorSelect.VendorName = PV.Name;
                lbVendorRemove.Visible = CanUserEditSummary();
                if (PV.Address != null)
                    ucVendorSelect.VendorAddress = PV.Address.ToArenaFormat();
                else
                    ucVendorSelect.VendorAddress = String.Empty;
               
                if (PV.Phone != null)
                    ucVendorSelect.VendorPhone = PV.Phone.ToArenaFormat();
                else
                    ucVendorSelect.VendorPhone = String.Empty;
                
                if (!String.IsNullOrEmpty(PV.WebAddress))
                    ucVendorSelect.VendorWebAddress = PV.WebAddress;
                else
                    ucVendorSelect.VendorWebAddress = String.Empty;
            }
        }

        private void LoadToolbarLinks()
        {
            lbSave.Visible = CanUserEditSummary();
            lbAddItem.Visible = CanUserAddNewItem();
            lbAddNote.Visible = CanUserEditNotes();
            lbAddAttachment.Visible = CanUserEditAttachments();
            lbRequestApproval.Visible = CanUserEditApprovals();
            lbSubmitToPurchasing.Visible = CanUserSubmitToPurchasing();
            lbCancel.Visible = CanUserCancelRequisition();
            lbAddItemToPO.Visible = CanUserAddItemToPO();
            bool CanAcceptOrReturn = CanUserAcceptOrReturnRequisition();
            lbAcceptRequisition.Visible = CanAcceptOrReturn;
            lbReturnToRequester.Visible = CanAcceptOrReturn;


        }

        private void SetRequester(int selectedPersonID)
        {
            if (selectedPersonID > 0)
            {
                Person SelectedPerson = personAliasService.Get(selectedPersonID).Person;
                if (SelectedPerson.Id > 0)
                {
                    //hdnRequesterID.Value = SelectedPerson.Id.ToString();
                    ucStaffPicker.StaffPersonAliasId = SelectedPerson.PrimaryAliasId;
                    //lblRequesterName.Text = SelectedPerson.FullName;
                }
            }
        }

        private void SetSummaryError(string errorText)
        {
            summaryError.Visible = !String.IsNullOrEmpty(errorText);
            lblSummaryError.Text = errorText;
        }

        private void SetSummaryVisibility()
        {
            bool IsEditable = CanUserEditSummary();

            if (!IsEditable)
            {
                txtTitle.AddCssClass("nothing");
                ddlType.Enabled = false;
                ddlType.AddCssClass("nothing");
                txtTitle.ReadOnly = true;
                txtDeliverTo.AddCssClass("nothing");
                txtDeliverTo.ReadOnly = true;
            }

            if (UserCanEdit)
            {
                ucStaffPicker.UserCanEdit = IsEditable;
            }
            else
            {
                ucStaffPicker.UserCanEdit = false;
            }

            lbVendorRemove.Visible = IsEditable;
            btnVendorModalShow.Visible = IsEditable;

            ucVendorSelect.IsReadOnly = !IsEditable;

            bool cerVisibility = false;
            int reqTypeLUID = 0;

            if ( !int.TryParse( ddlType.SelectedValue, out reqTypeLUID ) )
            {
                if ( CurrentRequisition != null )
                {
                    reqTypeLUID = CurrentRequisition.RequisitionTypeLUID;
                }
            }

            if ( reqTypeLUID > 0 )
            {

                var typeLU = definedValueService.Get(reqTypeLUID);
                typeLU.LoadAttributes();
                if ( typeLU != null && typeLU.AttributeValues["CERRequired"].Value.AsBoolean() )
                {
                    cerVisibility = true;
                }
            }

            divCapitalRequest.Visible = cerVisibility;
        }

        private void SetStatusNote()
        {
            bool showStatusNote = false;

            if (CurrentRequisition != null)
            {
                CurrentRequisition.Status.LoadAttributes();
                if (!CurrentRequisition.Active)
                {
                    StateType = "not active";
                    Disposition = String.Empty;
                    showStatusNote = true;
                }
                else if (CurrentRequisition.Status.AttributeValues["IsClosed"].Value.AsBoolean())
                {
                    StateType = "Closed";
                    Disposition = CurrentRequisition.Status.Value;
                    showStatusNote = true;
                }
            }

            divStatusNote.Visible = showStatusNote;
        }

        private void SetVendorModalButtonStatus(bool enabled)
        {
            // TODO: Show/Hide action buttons
            //btnVendorModalUpdate.Enabled = enabled;
            //btnVendorModalReset.Enabled = enabled; 
        }

        private void UpdateVendorLabel()
        {
            if (!String.IsNullOrEmpty(ucVendorSelect.VendorName))
            {
                lblVendor.Text = ucVendorSelect.VendorName;
                lbVendorRemove.Visible = true;
            }
            else
            {
                lblVendor.Text = "(none selected)";
                lbVendorRemove.Visible = false;
            }
        }

        private bool HasSummaryChanged()
        {
            bool HasChanged = false;
            if (!CanUserEditSummary())
                return false;
            if (RequisitionID == 0)
            {
                if (!String.IsNullOrWhiteSpace(txtTitle.Text))
                    HasChanged = true;
                if (!HasChanged && !String.IsNullOrWhiteSpace(txtDeliverTo.Text))
                    HasChanged = true;
                if (!HasChanged && (ucVendorSelect.VendorID > 0 || !String.IsNullOrWhiteSpace(ucVendorSelect.VendorName)))
                    HasChanged = true;
            }

            else
            {
                if (txtTitle.Text.Trim() != CurrentRequisition.Title.Trim())
                    HasChanged = true;
                if (!HasChanged && int.Parse(ddlType.SelectedValue) != CurrentRequisition.RequisitionTypeLUID)
                    HasChanged = true;
                if (!HasChanged && ucStaffPicker.StaffPersonAliasId != CurrentRequisition.RequesterID)
                    HasChanged = true;
                if (!HasChanged && txtDeliverTo.Text != CurrentRequisition.DeliverTo)
                    HasChanged = true;
                if (CurrentRequisition.PreferredVendor != null)
                {
                    if (!HasChanged && ucVendorSelect.VendorID != CurrentRequisition.PreferredVendor.VendorID)
                        HasChanged = true;
                    if (!HasChanged && ucVendorSelect.VendorName != CurrentRequisition.PreferredVendor.Name)
                        HasChanged = true;
                    if (!HasChanged && CurrentRequisition.PreferredVendor.Phone != null && ucVendorSelect.VendorPhone != CurrentRequisition.PreferredVendor.Phone.ToArenaFormat())
                        HasChanged = true;
                    else if (!HasChanged && CurrentRequisition.PreferredVendor.Phone == null && !String.IsNullOrEmpty(ucVendorSelect.VendorPhone))
                        HasChanged = true;
                    if (!HasChanged && CurrentRequisition.PreferredVendor.Address != null && ucVendorSelect.VendorAddress != CurrentRequisition.PreferredVendor.Address.ToArenaFormat())
                        HasChanged = true;
                    else if (!HasChanged && CurrentRequisition.PreferredVendor.Address == null && !String.IsNullOrEmpty(ucVendorSelect.VendorAddress))
                        HasChanged = true;
                    if (!HasChanged && CurrentRequisition.PreferredVendor.WebAddress != ucVendorSelect.VendorWebAddress)
                        HasChanged = true;
                }
                else
                {
                    if (!HasChanged && ucVendorSelect.VendorID > 0)
                        HasChanged = true;
                    if (!HasChanged && !String.IsNullOrEmpty(ucVendorSelect.VendorName))
                        HasChanged = true;
                    if (!HasChanged && !String.IsNullOrEmpty(ucVendorSelect.VendorAddress))
                        HasChanged = true;
                    if (!HasChanged && !String.IsNullOrEmpty(ucVendorSelect.VendorPhone))
                        HasChanged = true;
                    if (!HasChanged && !String.IsNullOrEmpty(ucVendorSelect.VendorWebAddress))
                        HasChanged = true;
                }

                int capitalRequestId = 0;

                if (!HasChanged && int.TryParse( hfCapitalRequest.Value, out capitalRequestId ) )
                {
                    if ( CurrentRequisition.CapitalRequestId != capitalRequestId )
                    {
                        HasChanged = true;
                    }
                }
            }

            return HasChanged;
        }

        private void SaveSummary()
        {

            SetSummaryError(String.Empty);
            if (CurrentRequisition == null)
                CurrentRequisition = new Requisition();

            CurrentRequisition.Title = txtTitle.Text.Trim();
            if (ddlType.SelectedValue != "")
                CurrentRequisition.RequisitionTypeLUID = int.Parse(ddlType.SelectedValue);
            if (ucStaffPicker.StaffPersonAliasId.HasValue)
                CurrentRequisition.RequesterID = ucStaffPicker.StaffPersonAliasId.Value;
            CurrentRequisition.DeliverTo = txtDeliverTo.Text;

            UpdatePreferredVendor();


            if ( CurrentRequisition.MinistryLUID == 0 )
            {
                var MinistryAttribute = CurrentRequisition.Requester.Attributes.Where( x => x.Key == MinistryAreaAttribute.Key ).FirstOrDefault();

                if (MinistryAttribute.Value != null )
                {
                    CurrentRequisition.MinistryLUID = MinistryAttribute.Value.Id;
                }
            }

            if ( CurrentRequisition.LocationLUID == 0 )
            {
                var LocationAttribute = CurrentRequisition.Requester.Attributes.Where( x => x.Key == MinistryLocationAttribute.Key ).FirstOrDefault();

                if (LocationAttribute.Value != null )
                {
                    CurrentRequisition.LocationLUID = LocationAttribute.Value.Id;
                }
            }


            int capitalRequestId = 0;

            if ( int.TryParse( hfCapitalRequest.Value, out capitalRequestId ) )
            {
                CurrentRequisition.CapitalRequestId = capitalRequestId;
            }

            CurrentRequisition.Save(CurrentUser.UserName);
            RequisitionID = CurrentRequisition.RequisitionID;

            LoadSummary();  
        }

        private void UpdatePreferredVendor()
        {
            if (CurrentRequisition.PreferredVendor == null && (ucVendorSelect.VendorID > 0 || !String.IsNullOrWhiteSpace(ucVendorSelect.VendorName)))
            {
                CurrentRequisition.PreferredVendor = new PreferredVendor();
            }
            else if (ucVendorSelect.VendorID <= 0 && String.IsNullOrWhiteSpace(ucVendorSelect.VendorName))
            {
                CurrentRequisition.PreferredVendor = null;
                return;
            }



            if (ucVendorSelect.VendorID == 0 && !String.IsNullOrEmpty(ucVendorSelect.VendorName))
            {
                CurrentRequisition.PreferredVendor.VendorID = ucVendorSelect.VendorID;
                CurrentRequisition.PreferredVendor.Name = ucVendorSelect.VendorName.Trim();

                if (!String.IsNullOrWhiteSpace(ucVendorSelect.VendorAddress))
                    CurrentRequisition.PreferredVendor.Address = new org.secc.Purchasing.Helpers.Address(ucVendorSelect.VendorAddress);
                else
                    CurrentRequisition.PreferredVendor.Address = null;

                if (!String.IsNullOrWhiteSpace(ucVendorSelect.VendorPhone))
                    CurrentRequisition.PreferredVendor.Phone = new org.secc.Purchasing.Helpers.PhoneNumber(ucVendorSelect.VendorPhone);
                else
                    CurrentRequisition.PreferredVendor.Phone = null;

                CurrentRequisition.PreferredVendor.WebAddress = ucVendorSelect.VendorWebAddress;
            }
            else if(ucVendorSelect.VendorID > 0)
            {
                CurrentRequisition.PreferredVendor.VendorID = ucVendorSelect.VendorID;
                CurrentRequisition.PreferredVendor.Name = String.Empty;
                CurrentRequisition.PreferredVendor.Address = null;
                CurrentRequisition.PreferredVendor.Phone = null;
                CurrentRequisition.PreferredVendor.WebAddress = String.Empty;
            }
        }

        private string BuildValidationErrorMessage(Dictionary<string, string> ValErrors)
        {
            if (ValErrors.Count == 0)
                return String.Empty;    

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<ul type=\"disc\">");
            foreach (var error in ValErrors)
            {
                sb.AppendFormat("<li>{0} - {1}</li>", error.Key, error.Value);
            }
            sb.Append("</ul>");

            return sb.ToString();
        }

        private string GetFixedWidthValue( string value, int maxLength )
        {
            string updatedValue = string.Empty;
            int valueLength = value.Length;

            if ( valueLength > maxLength )
                updatedValue = value.Substring( 0, maxLength );
            else
                updatedValue = value.PadRight( maxLength );

            return updatedValue;

        }

        private void MergeTemplateFields( ref SystemEmail notificationTemplate, Dictionary<string, string> HTMLMerge, Dictionary<string, string> textMerge )
        {
            if (notificationTemplate == null)
            {
                return;
            }
            System.Text.StringBuilder htmlMessage = new System.Text.StringBuilder( notificationTemplate.Body );

            foreach ( KeyValuePair<string, string> kvp in HTMLMerge )
            {
                htmlMessage.Replace( kvp.Key, kvp.Value );
            }

            notificationTemplate.Body = htmlMessage.ToString();
        }



        private string GetItemListForCommunication( bool isHtml )
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if ( isHtml )
            {
                sb.AppendLine( "<table border=\"0\" cellspacing=\"2\" cellpadding=\"2\">" );
                sb.AppendLine( "<tr><th>Qty</th><th>Description</th><th>Account</th><th>Cost/Item</th><th>Line Total</th></tr>" );
                foreach ( var Item in CurrentRequisition.Items.Where( i => i.Active == true ).ToList() )
                {
                    sb.Append( "<tr>" );
                    sb.AppendFormat( "<td>{0}</td>", Item.Quantity );
                    sb.AppendFormat( "<td>{0}</td>", Item.Description );
                    sb.AppendFormat( "<td>{0}-{1}-{2}</td>", Item.FundID, Item.DepartmentID, Item.AccountID );
                    sb.AppendFormat( "<td>{0}</td>", Item.Price == 0 ? "(unknown)" : string.Format( "{0:c}", Item.Price ) );
                    sb.AppendFormat( "<td>{0}</td>", Item.Price == 0 ? "(unknown)" : string.Format( "{0:c}", Item.Price * Item.Quantity ) );
                    sb.Append( "</tr>" );
                }
                sb.AppendLine( "</table>" );
            }
            else
            {
                sb.Append( GetFixedWidthValue( "Qty", 7 ) + " " );
                sb.Append( GetFixedWidthValue( "Description", 30 ) + " " );
                sb.Append( GetFixedWidthValue( "Account", 15 ) );
                sb.Append( GetFixedWidthValue( "Item Cost", 15 ) );
                sb.Append( GetFixedWidthValue( "Line Total", 15 ) );
                sb.Append( "\n" );
                sb.AppendLine( "".PadRight( 84, '-' ) );

                foreach ( var Item in CurrentRequisition.Items.Where( i => i.Active == true ).ToList() )
                {
                    sb.Append( GetFixedWidthValue( Item.Quantity.ToString(), 7 ) + " " );
                    sb.Append( GetFixedWidthValue( Item.Description, 30 ) + " " );
                    sb.Append( GetFixedWidthValue( string.Format( "{0}-{1}-{2}", Item.FundID, Item.DepartmentID, Item.AccountID ), 15 ) );
                    sb.Append( GetFixedWidthValue( Item.Price == 0 ? "(unknown)" : string.Format( "{0:c}", Item.Price ), 15 ) );
                    sb.Append( GetFixedWidthValue( Item.Price == 0 ? "(unknown)" : string.Format( "{0:c}", Item.Price * Item.Quantity ), 15 ) );
                    sb.Append( "\n" );
                }
            }

            return sb.ToString();
        }

        private void SendToPurchasing( bool isApproved )
        {
            CurrentRequisition.StatusLUID = Requisition.SubmittedToPurchasingLUID();
            CurrentRequisition.DateSubmitted = DateTime.Now;
            CurrentRequisition.IsApproved = isApproved;
            CurrentRequisition.Save( CurrentUser.UserName );

            SendPurchasingTeamNotification();
            SendRequesterSentToPurchasingNotification();

            LoadRequisition();
        }

        private void SendRequesterSentToPurchasingNotification()
        {
            SystemEmail ct = systemEmailService.Get(RequisitionSubmittedToPurchasingSetting.Value);
            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string, string> TextMerge = new Dictionary<string, string>();

            HTMLMerge.Add( "##Requester##", CurrentRequisition.Requester.FirstName );
            TextMerge.Add( "##Requester##", CurrentRequisition.Requester.FirstName );

            HTMLMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );
            TextMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );

            HTMLMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );
            TextMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );

            string Link = GetRequisitionLink();
            HTMLMerge.Add( "##RequisitionLink##", string.Format( "<a href=\"{0}\">{0}</a>", Link ) );
            TextMerge.Add( "##RequisitionLink##", string.Format( "<a href=\"{0}\">{0}</a>", Link ) );

            MergeTemplateFields( ref ct, HTMLMerge, TextMerge );
            SendCommunication( ct, null, CurrentRequisition.Requester );
        }

        private void SendPurchasingTeamNotification()
        {
            SystemEmail ctOriginal = systemEmailService.Get(NewRequisitionInPurchasingQueueNotificationSetting.Value);

            String[] emails = ctOriginal.To.Split(new char[] { ',', ';' });
            PersonService personService = new PersonService(new Rock.Data.RockContext());
            foreach (var email in emails)
            {
                Person recepient = personService.GetByEmail(email).FirstOrDefault();
                SystemEmail ct = ctOriginal;
                Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
                Dictionary<string, string> TextMerge = new Dictionary<string, string>();

                HTMLMerge.Add( "##RecipientName##", recepient.FirstName );
                TextMerge.Add( "##RecipientName##", recepient.FirstName );

                HTMLMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );
                TextMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );

                HTMLMerge.Add( "##Requester##", CurrentRequisition.Requester.FullName );
                TextMerge.Add( "##Requester##", CurrentRequisition.Requester.FullName );

                HTMLMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );
                TextMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );

                HTMLMerge.Add( "##IsApproved##", CurrentRequisition.IsApproved ? "Yes" : "No" );
                TextMerge.Add( "##IsApproved##", CurrentRequisition.IsApproved ? "Yes" : "No" );

                string link = GetRequisitionLink();
                HTMLMerge.Add( "##RequisitionLink##", string.Format( "<a href=\"{0}\">{0}</a>", link ) );
                TextMerge.Add( "##RequisitionLink##", link );

                int ItemDetailCount = CurrentRequisition.GetItemDetailCount( true );
                HTMLMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );
                TextMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );

                MergeTemplateFields( ref ct, HTMLMerge, TextMerge );

                SendCommunication( ct, null, recepient );
            }
        }

        private void AcceptRequisition()
        {
            CurrentRequisition.DateAccepted = DateTime.Now;
            CurrentRequisition.AcceptedByID = CurrentPerson.PrimaryAliasId.Value;
            CurrentRequisition.StatusLUID = Requisition.AcceptedByPurchasingLUID();
            CurrentRequisition.Save( CurrentUser.UserName );
            LoadRequisition();

            SendRequisitionAcceptedNotification();
        }

        private void ReturnToRequesterStep1()
        {
            ucPurchasingNotes.ResetVariableProperties();
            ucPurchasingNotes.Instructions = "Please provide comments for returning requisition to requester.";
            ucPurchasingNotes.Callback = "returnToRequesterCallback(##NoteID##);";
            ucPurchasingNotes.NotePrefix = "Return to Requester -";
            ucPurchasingNotes.CancelButtonText = "Cancel Return";
            ucPurchasingNotes.AllowCancel = true;
            ucPurchasingNotes.ShowNoteDetail();
        }

        private void SendRequisitionAcceptedNotification()
        {
            SystemEmail ct = systemEmailService.Get(AcceptedByPurchasingNotificationSetting);

            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string, string> TextMerge = new Dictionary<string, string>();

            HTMLMerge.Add( "##Requester##", CurrentRequisition.Requester.NickName );
            TextMerge.Add( "##Requester##", CurrentRequisition.Requester.NickName );

            HTMLMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );
            TextMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );

            HTMLMerge.Add( "##Approved##", CurrentRequisition.IsApproved ? "Yes" : "No" );
            TextMerge.Add( "##Approved##", CurrentRequisition.IsApproved ? "Yes" : "No" );

            int ItemDetailCount = CurrentRequisition.GetItemDetailCount( true );
            HTMLMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );
            TextMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );

            HTMLMerge.Add( "##DateAccepted##", string.Format( "{0:g}", CurrentRequisition.DateAccepted ) );
            TextMerge.Add( "##DateAccepted##", string.Format( "{0:g}", CurrentRequisition.DateAccepted ) );

            HTMLMerge.Add( "##AcceptedBy##", CurrentRequisition.AcceptedBy.FullName );
            TextMerge.Add( "##AcceptedBy##", CurrentRequisition.AcceptedBy.FullName );

            HTMLMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );
            TextMerge.Add( "##DateSubmitted##", string.Format( "{0:g}", CurrentRequisition.DateSubmitted ) );

            string Link = GetRequisitionLink();
            HTMLMerge.Add( "##RequisitionLink##", string.Format( "<a href=\"{0}\">{0}</a>", Link ) );
            TextMerge.Add( "##RequisitionLink##", Link );

            MergeTemplateFields( ref ct, HTMLMerge, TextMerge );
            SendCommunication( ct, null, CurrentRequisition.Requester );
        }

        private void SendReturnToRequesterNotification( string noteText )
        {
            SystemEmail ct = systemEmailService.Get(ReturnedToRequesterNotificationSetting);

            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string, string> TextMerge = new Dictionary<string, string>();
            int ItemDetailCount = CurrentRequisition.GetItemDetailCount( true );
            string Link = GetRequisitionLink();
            HTMLMerge.Add( "##Requester##", CurrentRequisition.Requester.NickName );
            HTMLMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );
            HTMLMerge.Add( "##Approved##", CurrentRequisition.IsApproved ? "Yes" : "No" );
            HTMLMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );
            HTMLMerge.Add( "##ReturnedBy##", CurrentPerson.FullName );
            HTMLMerge.Add( "##ReasonForReturn##", noteText.Replace( "Return to Requester -", "" ) );
            HTMLMerge.Add( "##RequisitionLink##", string.Format( "<a href=\"{0}\">{0}</a>", Link ) );

            TextMerge.Add( "##Requester##", CurrentRequisition.Requester.NickName );
            TextMerge.Add( "##RequisitionTitle##", CurrentRequisition.Title );
            TextMerge.Add( "##Approved##", CurrentRequisition.IsApproved ? "Yes" : "No" );
            TextMerge.Add( "##ItemCount##", ItemDetailCount.ToString() );
            TextMerge.Add( "##ReturnedBy##", CurrentPerson.FullName );
            TextMerge.Add( "##ReasonForReturn##", noteText.Replace( "Return to Requester -", "" ) );


            TextMerge.Add( "##RequisitionLink##", Link );

            MergeTemplateFields( ref ct, HTMLMerge, TextMerge );
            SendCommunication( ct, null, CurrentRequisition.Requester );
        }

        private void ShowCancelPrompt()
        {
            mpCancelPrompt.Title = "Cancel Requisition";
            mpCancelPrompt.Show();
        }

        private void ReturnToList()
        {
            NavigateToPage(RequisitionListPageSetting, null);
        }

        private void CancelRequisition()
        {
            if ( CurrentRequisition != null )
                CurrentRequisition.Cancel( CurrentUser.UserName );
        }
        protected void btnCapitalRequestModalShow_Click( object sender, EventArgs e )
        {
            LoadCERList();
            mpCERSelect.Show();
        }
        protected void lbCapitalRequestRemove_Click( object sender, EventArgs e )
        {
            SetCapitalRequest( 0 );
        }

        private void SetCapitalRequest( int capitalRequestId )
        {
            bool isEditable = CanUserEditSummary();
            hfCapitalRequest.Value = capitalRequestId.ToString();

            if ( capitalRequestId > 0 )
            {
                var capitalRequest = new CapitalRequest( capitalRequestId );
                lCapitalRequest.Text = String.Format( "<a href=\"/default.aspx?page={0}&CER={1}\" target=\"_blank\" class=\"CERLink\">{2}</a>", CapitalRequestDetailPageSetting, capitalRequestId, capitalRequest.ProjectName );
            }
            else
            {
                lCapitalRequest.Text = "(None Selected)";
            }

            lbCapitalRequestRemove.Visible = ( isEditable && capitalRequestId > 0 );
            btnCapitalRequestModalShow.Visible = isEditable;

        }


        #endregion

        #region Item Detail Modal
        private void BindCompanyList()
        {
            ddlItemCompany.Items.Clear();

            ddlItemCompany.DataSource = Company.GetCompanies().OrderBy(c => c.CompanyName);
            ddlItemCompany.DataValueField = "CompanyID";
            ddlItemCompany.DataTextField = "CompanyName";
            ddlItemCompany.DataBind();
        }

        private void ClearItemDetailModal()
        {
            hfItemID.Value = "0";
            SetItemDetailErrorMessage(String.Empty);
            txtItemQuantity.Text = string.Empty;
            txtItemNumber.Text = string.Empty;
            txtItemDescription.Text = string.Empty;
            txtDateNeeded.Text = string.Empty;
            txtItemPrice.Text = string.Empty;
            txtItemFundNumber.Text = string.Empty;
            txtItemDepartmentNumber.Text = string.Empty;
            txtItemAccountNumber.Text = string.Empty;
            chkItemAllowExpedited.Checked = false;
            
            int DefaultCompanyID = GetUserCompanyID(Company.GetDefaultCompany().CompanyID);

            if(ddlItemCompany.Items.FindByValue(DefaultCompanyID.ToString()) != null)
            {
                ddlItemCompany.SelectedValue = DefaultCompanyID.ToString();
            }

        }

        private void LoadItemDetailModal(int itemID)
        {
            BindCompanyList();
            PopulateItemDetailData(itemID);
            SetItemDetailEditability(!CanUserEditItemDetail());
            mpItemDetail.Show();

        }

        private void PopulateItemDetailData(int itemID)
        {
            ClearItemDetailModal();

            if(itemID > 0)
            {
                RequisitionItem i = CurrentRequisition.Items.Where(ri => ri.ItemID == itemID).FirstOrDefault();

                hfItemID.Value = i.ItemID.ToString();
                ddlItemCompany.SelectedValue = i.CompanyID.ToString();
                txtItemQuantity.Text = i.Quantity.ToString();
                txtItemNumber.Text = i.ItemNumber;
                txtItemDescription.Text = i.Description;
                txtItemFundNumber.Text = i.FundID.ToString();
                txtItemDepartmentNumber.Text = i.DepartmentID.ToString();
                txtItemAccountNumber.Text = i.AccountID.ToString();
                
                if(i.DateNeeded > DateTime.MinValue)
                    txtDateNeeded.Text = string.Format("{0:d}", i.DateNeeded);

                if (i.Price != 0)
                    txtItemPrice.Text = i.Price.ToString("0.00");
                
                chkItemAllowExpedited.Checked = i.IsExpeditiedShippingAllowed;

                ScriptManager.RegisterStartupScript(this, typeof(UserControl), "ShowExpeditedLink", "showAllowExpedited(" + i.IsExpeditiedShippingAllowed.ToString().ToLower() + ");", true);

            }
            else if ( CurrentRequisition.CapitalRequestId > 0 )
            {
                ddlItemCompany.SelectedValue = CurrentRequisition.CapitalRequest.GLCompanyId.ToString();
                txtItemFundNumber.Text = CurrentRequisition.CapitalRequest.GLFundId.ToString();
                txtItemDepartmentNumber.Text = CurrentRequisition.CapitalRequest.GLDepartmentId.ToString();
                txtItemAccountNumber.Text = CurrentRequisition.CapitalRequest.GLAccountId.ToString();
            }


        }

        private int GetUserCompanyID(int defaultCompanyID)
        {
            int companyID = 0;

            CurrentPerson.LoadAttributes();
            
            String avc = CurrentPerson.GetAttributeValue(MinistryLocationAttribute.Key);
            DefinedValue ministryLocation = definedValueService.Get(avc.AsGuid());
            if (ministryLocation != null) {
                ministryLocation.LoadAttributes();
                companyID = ministryLocation.GetAttributeValue("CompanyCode").AsInteger();
            }

            if (companyID == 0)
                companyID = defaultCompanyID;

            return companyID;
        }

        private void SetItemDetailEditability(bool isReadOnly)
        {

            txtItemQuantity.ReadOnly = isReadOnly;
            txtItemDescription.ReadOnly = isReadOnly;
            txtDateNeeded.ReadOnly = isReadOnly;

            if ( CurrentRequisition.CapitalRequestId <= 0 || UserCanEdit)
            {
                txtItemFundNumber.ReadOnly = isReadOnly;
                txtItemDepartmentNumber.ReadOnly = isReadOnly;
                txtItemAccountNumber.ReadOnly = isReadOnly;
                ddlItemCompany.Enabled = !isReadOnly;
            }
            else
            {
                txtItemFundNumber.ReadOnly = true;
                txtItemDepartmentNumber.ReadOnly = true;
                txtItemAccountNumber.ReadOnly = true;
                ddlItemCompany.Enabled = false;
            }

            txtItemPrice.ReadOnly = isReadOnly;


            if (UserCanEdit)
            {
                pnlItemDetailCompany.CssClass = "visible";
            }
            else
            {
                pnlItemDetailCompany.CssClass = "hidden";
            }

        }

        private bool SaveItemDetail()
        {
            bool IsSuccessful = false;
            try
            {
                int itemID = 0;
                if (!int.TryParse(hfItemID.Value, out itemID))
                    return IsSuccessful;

                RequisitionItem Item;
                if (itemID > 0)
                {
                    Item = CurrentRequisition.Items.FirstOrDefault(i => i.ItemID == itemID);
                    if (Item == null)
                        throw new RequisitionException("Item is not part of the current requisition.");
                }
                else
                {
                    Item = new RequisitionItem();
                }

                int quantity = 0;
                if (int.TryParse(txtItemQuantity.Text, out quantity))
                {
                    Item.Quantity = quantity;
                }
                Item.Description = txtItemDescription.Text.Trim();
                Item.ItemNumber = txtItemNumber.Text.Trim();
                
                if(!String.IsNullOrEmpty(txtDateNeeded.Text))
                    Item.DateNeeded = DateTime.Parse(txtDateNeeded.Text);
                
                int companyID = 0;
                if(int.TryParse(ddlItemCompany.SelectedValue, out companyID))
                {
                    Item.CompanyID = companyID;
                }

                int fundID = 0;
                if(int.TryParse(txtItemFundNumber.Text, out fundID))
                {
                    Item.FundID = fundID;
                }

                int departmentID = 0;
                if (int.TryParse(txtItemDepartmentNumber.Text, out departmentID))
                {
                    Item.DepartmentID = departmentID;
                }

                int accountID = 0;
                if (int.TryParse(txtItemAccountNumber.Text, out accountID))
                {
                    Item.AccountID = accountID;
                }

                Decimal ItemPrice = 0;
                if ( decimal.TryParse( txtItemPrice.Text.Trim(), out ItemPrice ) || String.IsNullOrWhiteSpace( txtItemPrice.Text ) )
                {
                    Item.Price = ItemPrice;
                }

                Item.IsExpeditiedShippingAllowed = chkItemAllowExpedited.Checked;

                CurrentRequisition.SaveItem(Item, CurrentUser.UserName, true);

                LoadRequisition();
                hfItemID.Value = Item.ItemID.ToString();
                //PopulateItemDetailData(Item.ItemID);
                IsSuccessful = true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    string ErrorMessage = BuildValidationErrorMessage(((RequisitionNotValidException)rEx.InnerException).InvalidProperties);
                    SetItemDetailErrorMessage(ErrorMessage);
                }
                else throw rEx;
            }

            return IsSuccessful;

        }

        public void SetItemDetailErrorMessage(string message)
        {
            lblItemDetailError.Text = message;
            lblItemDetailError.Visible = !String.IsNullOrEmpty(message);
        }

        #endregion

        #region Approvals

        private bool UserCanEditApproval()
        {
            return true;
        }

        private void AddApprover(int approverID)
        {
            Approval a = null;
            int approvalID = 0;


            if (CurrentRequisition.Approvals.Where(app => app.ApproverID == approverID).Count() > 0)
            {
                a = CurrentRequisition.Approvals.Where(app => app.ApproverID == approverID).FirstOrDefault();
                a.ApprovalStatusLUID = PendingApprovalStatusLUID;
                a.DateApproved = DateTime.MinValue;
                a.Active = true;
            }
            else
            {
                a = new Approval();
                a.ObjectTypeName = typeof( Requisition ).ToString();
                a.Identifier = RequisitionID;
                a.ApproverID = approverID;
                a.ApprovalTypeLUID = Approval.RequisitionApprovalTypeLUID();

                if (a.ApproverID == CurrentPerson.PrimaryAliasId)
                {
                    a.ApprovalStatusLUID = ApprovedStatusLUID;
                    a.DateApproved = DateTime.Now;
                }
                else
                    a.ApprovalStatusLUID = PendingApprovalStatusLUID;
            }

            a.Save(CurrentUser.UserName);
            approvalID = a.ApprovalID;
           
            if (approvalID > 0)
            {
                if ( approverID != CurrentPerson.PrimaryAliasId )
                {
                    SendApprovalRequestNotification( approvalID );
                    CurrentRequisition.IsApproved = false;
                }
                else
                {
                    CurrentRequisition.IsApproved = true;
                }

                DefinedValue definedValue = definedValueService.Get(Requisition.ApprovedLUID());
                if (CurrentRequisition.Status.Order < definedValue.Order)
                {
                    if ( CurrentRequisition.IsApproved )
                    {
                        CurrentRequisition.StatusLUID = Requisition.ApprovedLUID();
                    }
                    else
                    {
                        CurrentRequisition.StatusLUID = Requisition.PendingApprovalLUID();
                    }
                }

                if ( CurrentRequisition.HasChanged() )
                {
                    CurrentRequisition.Save( CurrentUser.UserName );
                }

                //if (CurrentRequisition.DateSubmitted == DateTime.MinValue)
                //{
                //    if (approverID == CurrentPerson.PrimaryAliasId)
                //    {
                //        CurrentRequisition.StatusLUID = Requisition.ApprovedLUID();
                //        CurrentRequisition.IsApproved = true;
                //    }
                //    else
                //    {
                //        CurrentRequisition.StatusLUID = Requisition.PendingApprovalLUID();
                //        CurrentRequisition.IsApproved = false;
                //    }
                //    CurrentRequisition.Save(CurrentUser.UserName);

                //}
                LoadRequisition();
            }
            
        }

        private void BindApprovalRequestGrid()
        {
            ConfigureApprovalRequestGrid();
            dgApprovals.DataSource = GetApprovalRequestDataTable();
            dgApprovals.DataBind();
        }

        private void ConfigureChargesGrid()
        {
            dgCharges.Visible = true;
            dgCharges.ItemType = "Items";
            /*dgCharges.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgCharges.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgCharges.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgCharges.AllowSorting = false;
            /*dgCharges.MergeEnabled = false;
            dgCharges.EditEnabled = false;
            dgCharges.MailEnabled = false;
            dgCharges.AddEnabled = false;
            dgCharges.ExportEnabled = false;
            dgCharges.DeleteEnabled = false;
            dgCharges.SourceTableKeyColumnName = "PaymentChargeID";
            dgCharges.SourceTableOrderColumnName = "PaymentDate";*/
        }

        private void BindCharges()
        {
            ConfigureChargesGrid();

            List<RequisitionChargeSummary> rcs = new List<RequisitionChargeSummary>();

            if (CurrentRequisition != null)
            {
                rcs.AddRange( CurrentRequisition.GetChargeSummary() );
            }

            dgCharges.DataSource = rcs;
            dgCharges.DataBind();

        }

        private DataTable GetApprovalRequestDataTable()
        {
            DataTable ApprovalRequests = new DataTable();

            ApprovalRequests.Columns.AddRange(new DataColumn[] { new DataColumn("ApprovalID", typeof(int)), 
                                                                new DataColumn("ApproverName", typeof(string)), 
                                                                new DataColumn("ApprovalStatus", typeof(string)), 
                                                                new DataColumn("DateApproved", typeof(string)), 
                                                                new DataColumn("CreatedByUser", typeof(string)), 
                                                                new DataColumn("ApproverPersonID", typeof(int)),
                                                                new DataColumn("ApprovalStatusLUID", typeof(int))});
            if (RequisitionID > 0)
            {
                foreach (Approval approval in CurrentRequisition.Approvals.Where(a => a.Active))
                {
                    DataRow dr = ApprovalRequests.NewRow();
                    dr["ApprovalID"] = approval.ApprovalID;
                    dr["ApproverName"] = approval.Approver.FullName;
                    dr["ApprovalStatus"] = approval.ApprovalStatus;
                    dr["CreatedByUser"] = approval.CreatedByUserID;
                    dr["ApproverPersonID"] = approval.ApproverID;
                    dr["ApprovalStatusLUID"] = approval.ApprovalStatusLUID;
                    dr["DateApproved"] = approval.DateApproved == DateTime.MinValue ? "N/A" : string.Format("{0:g}", approval.DateApproved);

                    ApprovalRequests.Rows.Add(dr);
                }
            }
            return ApprovalRequests;
        }

        private void ConfigureApprovalRequestGrid()
        {
            dgApprovals.Visible = true;
            dgApprovals.ItemType = "Items";
            /*dgApprovals.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgApprovals.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgApprovals.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);*/
            dgApprovals.AllowSorting = false;
            /*dgApprovals.MergeEnabled = false;
            dgApprovals.EditEnabled = false;
            dgApprovals.MailEnabled = false;
            dgApprovals.AddEnabled = false;
            dgApprovals.ExportEnabled = false;
            dgApprovals.DeleteEnabled = false;
            dgApprovals.SourceTableKeyColumnName = "ApprovalID";
            dgApprovals.SourceTableOrderColumnName = "ApprovalLevelLUID";*/

        }


        protected void dgApprovals_ItemCommand(object sender, DataGridCommandEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                int ApprovalID = 0;
                if (e.CommandArgument == null || !int.TryParse(e.CommandArgument.ToString(), out ApprovalID))
                    return;

                switch (e.CommandName.ToLower())
                {
                    case "approve":
                        RequisitionApprove(ApprovalID);
                        break;
                    case "approveforward":
                        RequisitionApproveForward(ApprovalID);
                        break;
                    case "decline":
                        RequisitionDecline(ApprovalID);
                        break;
                    case "remove":
                        RemoveApproval(ApprovalID);
                        break;
                    case "resubmit":
                        ResubmitApproval(ApprovalID);
                        break;
                    default:
                        break;
                }
            }
        }

        protected void dgApprovals_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                if ( CurrentRequisition == null || !CurrentRequisition.Active )
                {
                    return;
                }

                DataRowView drv = (DataRowView)e.Item.DataItem;

                LinkButton lbApprove = (LinkButton)e.Item.FindControl("lbApprove");
                lbApprove.CommandArgument = drv["ApprovalID"].ToString();
                LinkButton lbDeny = (LinkButton)e.Item.FindControl("lbDeny");
                lbDeny.CommandArgument = drv["ApprovalID"].ToString();
                LinkButton lbRemove = (LinkButton)e.Item.FindControl("lbRemove");
                lbRemove.CommandArgument = drv["ApprovalID"].ToString();
                LinkButton lbResubmit = (LinkButton)e.Item.FindControl("lbResubmit");
                lbResubmit.CommandArgument = drv["ApprovalID"].ToString();
                LinkButton lbApproveForward = (LinkButton)e.Item.FindControl("lbApproveForward");
                lbApproveForward.CommandArgument = drv["ApprovalID"].ToString();


                bool UserCanApprove = CurrentPerson.PrimaryAliasId == (int)drv["ApproverPersonID"] && (int)drv["ApprovalStatusLUID"] == PendingApprovalStatusLUID;

                UserLogin userLogin = userLoginService.GetByUserName(drv["CreatedByUser"].ToString());

                lbRemove.Visible = userLogin.Person.PrimaryAliasId == CurrentPerson.PrimaryAliasId && (int)drv["ApprovalStatusLUID"] != ApprovedStatusLUID && (int)drv["ApprovalStatusLUID"] != ApprovedForwardStatusLUID;
                lbDeny.Visible = UserCanApprove;
                lbApproveForward.Visible = UserCanApprove;
                lbApprove.Visible = UserCanApprove;
                
                if((CurrentRequisition.Requester.PrimaryAliasId == CurrentPerson.PrimaryAliasId || CurrentUser.UserName == CurrentRequisition.CreatedByUserID) && (int)drv["ApprovalStatusLUID"] == NotApprovedStatusLUID)
                    lbResubmit.Visible = true;
                else
                    lbResubmit.Visible = false;

            }
        }
        
        protected void dgApprovals_ReBind(object sender, EventArgs e)
        {
            BindApprovalRequestGrid();
        }

        protected void btnApproverAdd_Click(object sender, EventArgs e)
        {
            int approverID = 0;
            if (String.IsNullOrEmpty(hfApproverID.Value) || !int.TryParse(hfApproverID.Value, out approverID))
                return;

            AddApprover(approverID);
            
        }

        private void ShowApprovalTypeDialog()
        {
            mpSelectApprovalType.Show();
        }

        private void ShowSelectApproverDialog()
        {
            if (CurrentRequisition != null && CurrentRequisition.RequisitionID > 0)
            {
                if (CurrentPerson.PrimaryAliasId == CurrentRequisition.RequesterID && CurrentRequisition.Approvals.Where(a => a.Active && a.ApproverID == CurrentPerson.PrimaryAliasId).Count() == 0)
                {
                    mpSelectApprovalType.Show();
                }
                else
                {
                    ucStaffPickerApprover.MinistryAreaAttributeGuid = MinistryAreaAttribute.Guid;
                    ucStaffPickerApprover.PositionAttributeGuid = PositionAttribute.Guid;
                    ucStaffPickerApprover.Show();
                    //TODO: fix this
                    //ShowStaffSelector("Select Approver", hfApproverID.ClientID, btnApproverAdd.ClientID);
                }
            }
        }
        private void RequisitionApprove(int approvalID)
        {
            Approval approvalRequest = CurrentRequisition.Approvals.FirstOrDefault(a => a.ApprovalID == approvalID);
            approvalRequest.Approve(CurrentUser.UserName);
            CurrentRequisition.RefreshApprovals();

            SendApprovedNotification(approvalID);
            if(CurrentRequisition.Approvals.Where(a => a.ApprovalStatusLUID != ApprovedStatusLUID && a.ApprovalStatusLUID != ApprovedForwardStatusLUID && a.Active).Count() == 0)
            {

                CurrentRequisition.StatusLUID = Requisition.ApprovedLUID();
                CurrentRequisition.IsApproved = true;
                CurrentRequisition.Save(CurrentUser.UserName);
                LoadRequisition();
            }
            else
            {
                BindApprovalRequestGrid();
            }
            //SendApprovalRequestNotification(approvalID);
        }

        private void RequisitionApproveForward(int approvalID)
        {
            Approval approvalRequest = CurrentRequisition.Approvals.FirstOrDefault(a => a.ApprovalID == approvalID);
            approvalRequest.Approve(CurrentUser.UserName, Approval.ApprovedAndForwardLUID());
            //TODO; Fix this
            //ShowStaffSelector("Select Approver", hfApproverID.ClientID, btnApproverAdd.ClientID);

        }

        private void RequisitionDecline(int approvalID)
        {
            Approval approvalRequest = CurrentRequisition.Approvals.FirstOrDefault(a => a.ApprovalID == approvalID);
            approvalRequest.Decline(CurrentUser.UserName);

            if (CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID())
            {
                CurrentRequisition.StatusLUID = Requisition.ReturnedToRequesterLUID();
                CurrentRequisition.Save(CurrentUser.UserName);
            }
            if (PromptForNoteOnDecline)
            {
                ucPurchasingNotes.ResetVariableProperties();
                ucPurchasingNotes.Instructions = "Please provide comments for declining requisition.";

                ucPurchasingNotes.ShowNoteDetail();
            }


            SendDeclineNotification(approvalID);
            LoadRequisition();
        }

        private void RemoveApproval(int approvalID)
        {
            Approval approvalRequest = CurrentRequisition.Approvals.FirstOrDefault(a => a.ApprovalID == approvalID);
            approvalRequest.Delete(CurrentUser.UserName);
            CurrentRequisition.RefreshApprovals();

            if (CurrentRequisition.Approvals.Where(x => x.Active).Count() == 0  && CurrentRequisition.StatusLUID == Requisition.PendingApprovalLUID())
            {
                CurrentRequisition.StatusLUID = Requisition.DraftLUID();
                CurrentRequisition.Save(CurrentUser.UserName);
            }

            LoadRequisition();
        }

        private void ResubmitApproval(int approvalID)
        {
            Approval approvalRequest = CurrentRequisition.Approvals.FirstOrDefault(a => a.ApprovalID == approvalID);
            approvalRequest.Resubmit(CurrentUser.UserName);

            if (CurrentRequisition.StatusLUID == Requisition.ReturnedToRequesterLUID() || CurrentRequisition.StatusLUID == Requisition.DraftLUID())
            {
                CurrentRequisition.StatusLUID = Requisition.PendingApprovalLUID();
                CurrentRequisition.Save(CurrentUser.UserName);
            }

            SendApprovalRequestNotification(approvalID);
            LoadRequisition();

        }

        private void SendDeclineNotification(int approvalID)
        {
            Approval DeclinedApproval = new Approval(approvalID);

            SystemEmail notificationTemplate = systemEmailService.Get(ApprovalDeclinedNotificationTemplateSetting);
            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string,string> TextMerge = new Dictionary<string, string>();

            HTMLMerge.Add("##Requester##", CurrentRequisition.Requester.NickName);
            TextMerge.Add("##Requester##", CurrentRequisition.Requester.NickName);

            HTMLMerge.Add("##ApproverName##", DeclinedApproval.Approver.FullName);
            TextMerge.Add("##ApproverName##", DeclinedApproval.Approver.FullName);

            HTMLMerge.Add("##RequisitionTitle##", CurrentRequisition.Title);
            TextMerge.Add("##RequisitionTitle##", CurrentRequisition.Title);

            HTMLMerge.Add("##RequisitionLink##", string.Format("<a href=\"{0}\">{0}</a>", GetRequisitionLink()));
            TextMerge.Add("##RequisitionLink##", GetRequisitionLink());

            MergeTemplateFields(ref notificationTemplate, HTMLMerge, TextMerge);

            SendCommunication(notificationTemplate, DeclinedApproval.Approver, CurrentRequisition.Requester);

        }

        private void SendApprovedNotification(int approvalID)
        {
            if (!SendRequisitionApprovedNotificationSetting)
                return;

            Approval a = new Approval(approvalID);

            SystemEmail notificationTemplate = systemEmailService.Get(RequisitionApprovedNotificationTemplateSetting);

            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string, string> textmerge = new Dictionary<string, string>();

            HTMLMerge.Add("##RequesterName##", CurrentRequisition.Requester.NickName);
            textmerge.Add("##RequesterName##", CurrentRequisition.Requester.NickName);

            HTMLMerge.Add("##RequisitionTitle##", CurrentRequisition.Title);
            textmerge.Add("##RequisitionTitle##", CurrentRequisition.Title);

            HTMLMerge.Add("##ApproverName##", a.Approver.FullName);
            textmerge.Add("##ApproverName##", a.Approver.FullName);

            HTMLMerge.Add("##DateApproved##", a.DateApproved.ToShortDateString() + ' ' + a.DateApproved.ToShortTimeString());
            textmerge.Add("##DateApproved##", a.DateApproved.ToShortDateString() + ' ' + a.DateApproved.ToShortTimeString());

            string Link = GetRequisitionLink();
            HTMLMerge.Add("##RequisitionLink##", string.Format("<a href=\"{0}\">{0}</a>", Link));
            textmerge.Add("##RequisitionLink##", Link);

            MergeTemplateFields(ref notificationTemplate, HTMLMerge, textmerge);
            SendCommunication(notificationTemplate, CurrentPerson, CurrentRequisition.Requester);

        }

        private void SendApprovalRequestNotification(int approvalID)
        {
            if (!SendApprovalRequestNotificationSetting)
                return;

            Approval a = new Approval(approvalID);

            SystemEmail notificationTemplate = systemEmailService.Get(ApprovalRequestNotificationTemplateSetting);

            Dictionary<string, string> HTMLMerge = new Dictionary<string, string>();
            Dictionary<string, string> textMerge = new Dictionary<string,string>();
            
            HTMLMerge.Add("##ApproverName##", a.Approver.NickName);
            textMerge.Add("##ApproverName##", a.Approver.NickName);

            HTMLMerge.Add("##RequisitionTitle##", CurrentRequisition.Title);
            textMerge.Add("##RequisitionTitle##", CurrentRequisition.Title);

            HTMLMerge.Add("##ApprovalRequester##", CurrentPerson.FirstName);
            textMerge.Add("##ApprovalRequester##", CurrentPerson.FirstName);

            HTMLMerge.Add("##Requester##", CurrentRequisition.Requester.FullName);
            textMerge.Add("##Requester##", CurrentRequisition.Requester.FullName);

            HTMLMerge.Add("##RequisitionLink##", string.Format("<a href=\"{0}\">{0}</a>", GetRequisitionLink()));
            textMerge.Add("##RequisitionLink##", GetRequisitionLink());

            HTMLMerge.Add("##ItemList##", GetItemListForCommunication(true));
            textMerge.Add("##ItemList##", GetItemListForCommunication(false));


            MergeTemplateFields(ref notificationTemplate, HTMLMerge, textMerge);
            SendCommunication(notificationTemplate, CurrentPerson, a.Approver);
        }

        private string GetRequisitionLink()
        {
            // TODO: Doublecheck this;
            return CurrentPageReference.BuildUrl();
            /*string.Format("http://{0}/default.aspx?page={1}&RequisitionID={2}",
                CurrentPortal.Domain,
                CurrentPortalPage.PortalPageID,
                RequisitionID);*/

            
        }

        private void SendCommunication(SystemEmail notificationTemplate, Person sender, Person recepient)
        {
            if (notificationTemplate == null)
                return;

            string fromName;
            string fromEmail;
            string replyToEmail = String.Empty;
            if (sender != null)
            {
                //get sender's SECC Email Address
                fromName = sender.FullName;
                fromEmail = sender.Email;


                //if no SECC email found substitute the one in the template
                if (!fromEmail.Contains(@"@secc.org"))
                {
                    fromEmail = notificationTemplate.From;
                    replyToEmail = sender.Email;
                }
                else
                {
                    replyToEmail = fromEmail;
                }
            }
            else
            {
                fromName = notificationTemplate.FromName;
                fromEmail = notificationTemplate.From;
            }
            string RecepientEmail = recepient.Email + ";";
    
                if (EnableNotificationSetting || RequesterIsInBetaGroup())
                    Rock.Communication.Email.Send(fromName + "<"+fromEmail+">", notificationTemplate.Subject, new List<String>() { RecepientEmail}, replyToEmail, notificationTemplate.Body);
        }

        private bool RequesterIsInBetaGroup()
        {
            bool isInBetaGroup = false;
            if (CurrentRequisition == null || CurrentRequisition.RequisitionID <= 0)
            {
                return isInBetaGroup;
            }

            if (BetaRequesterNotificationOverrideRolesSetting.Count > 0)
            {
                PersonAlias requester = personAliasService.Get(CurrentRequisition.RequesterID);
                foreach(int roleId in BetaRequesterNotificationOverrideRolesSetting)
                {
                    if (Rock.Security.Role.Read(roleId).IsPersonInRole(requester.Person.Guid)) {
                        isInBetaGroup = true;
                    }
                }
            }

            return isInBetaGroup;
        }
        #endregion

        #region Purchase Order Select
        protected void btnSelectPurchaseOrderSelect_Click(object sender, EventArgs e)
        {
            int PurchaseOrderID = 0;
            foreach (DataGridItem item in dgSelectPurchaseOrder.Rows)
            {
                RadioButton rb = (RadioButton)item.FindControl("rdoSelectPO");
                if (rb != null && rb.Checked)
                {
                    int.TryParse(item.Cells[1].Text, out PurchaseOrderID);
                    break;
                }
            }

            ShowSelectPOItem(PurchaseOrderID);
            HideSelectPurchaseOrderModal();

        }

        protected void btnSelectPurchaseOrderNew_Click(object sender, EventArgs e)
        {
            ShowSelectPOItem(0);
            HideSelectPurchaseOrderModal();
        }
        protected void btnSelectPurchaseOrderCancel_Click(object sender, EventArgs e)
        {
            HideSelectPurchaseOrderModal();
        }

        private void BindSelectPurchaseOrderGrid()
        {
            ConfigureSelectPurchaseOrderGrid();
            dgSelectPurchaseOrder.DataSource = GetOpenPurchaseOrders();
            dgSelectPurchaseOrder.DataBind();
        }

        private void ConfigureSelectPurchaseOrderGrid()
        {
            dgSelectPurchaseOrder.Visible = true;
            dgSelectPurchaseOrder.ItemType = "PurchaseOrder";
            /*dgSelectPurchaseOrder.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgSelectPurchaseOrder.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgSelectPurchaseOrder.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);
            */
            dgSelectPurchaseOrder.AllowSorting = false;
            /*
            dgSelectPurchaseOrder.NoResultText = "No Purchase Order found";
            dgSelectPurchaseOrder.MergeEnabled = false;
            dgSelectPurchaseOrder.EditEnabled = false;
            dgSelectPurchaseOrder.MailEnabled = false;
            dgSelectPurchaseOrder.AddEnabled = false;
            dgSelectPurchaseOrder.DeleteEnabled = false;
            dgSelectPurchaseOrder.SourceTableKeyColumnName = "PurchaseOrderID";
            dgSelectPurchaseOrder.SourceTableOrderColumnName = "PurchaseOrderID";
            dgSelectPurchaseOrder.AddImageUrl = "~/images/addButton.png";
             */
        }

        private DataTable GetOpenPurchaseOrders()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("PurchaseOrderID", typeof(int)),
                new DataColumn("VendorName", typeof(string)),
                new DataColumn("Type", typeof(string)),
                new DataColumn("CreatedBy", typeof(string)),
                new DataColumn("DateCreated", typeof(string))
            });

            foreach (PurchaseOrder po in PurchaseOrder.LoadOpenPOs())
            {
                DataRow dr = dt.NewRow();
                dr["PurchaseOrderID"] = po.PurchaseOrderID;
                dr["VendorName"] = po.Vendor == null ? "(not selected}" : po.Vendor.VendorName;
                dr["Type"] = po.PurchaseOrderType.Value;
                dr["CreatedBy"] = po.CreatedBy == null || po.CreatedBy.PrimaryAliasId <= 0 ? po.CreatedByUserID : po.CreatedBy.FullName;
                dr["DateCreated"] = po.DateCreated.ToShortDateString();
                dt.Rows.Add(dr);
            }

            return dt;
        }

        private void HideSelectPurchaseOrderModal()
        {
            mpPurchaseOrderSelect.Hide();
        }

        private void ShowSelectPurchaseOrderModal()
        {
            BindSelectPurchaseOrderGrid();
            mpPurchaseOrderSelect.Show();
        }
        #endregion

        #region Select PO Items

        protected void btnSelectPOItemsAdd_Click(object sender, EventArgs e)
        {
            SetSelectPurchaseOrderItemError(string.Empty);
            if (UpdatePurchaseOrderItems())
            {
                ScriptManager.RegisterStartupScript(upSelectPurchaseOrderItems, upSelectPurchaseOrderItems.GetType(), "Show PO Number" + DateTime.Now.Ticks, string.Format("alert(\"Items added to PO Number: {0}\");", hfSelectPOItemsPONumber.Value), true);
                HideSelectPOItems();
                LoadItems();
                
            }
            else
            {
                BindSelectPOItemsGrid();
            }
        }

        protected void btnSelectPOItemsReset_Click(object sender, EventArgs e)
        {
            int poID = 0;
            int.TryParse(hfSelectPOItemsPONumber.Value, out poID);
            ClearSelectPOItemFields();
            PopulateSelectPOItemFields(poID);
            BindSelectPOItemsGrid();
        }

        protected void btnSelectPOItemsCancel_Click(object sender, EventArgs e)
        {
            HideSelectPOItems();
        }

        protected void dgSelectPOItems_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView drv = (DataRowView)e.Item.DataItem;
                int QtyRemaining = 0;
                int.TryParse(drv["QtyRemaining"].ToString(), out QtyRemaining);

                TextBox txtQtyRemaining = (TextBox)e.Item.FindControl("txtQtyRemaining");
                Label lblQuantityRemaining = (Label)e.Item.FindControl("lblQuantityRemaining");
                TextBox txtRequisitionItemPrice = (TextBox)e.Item.FindControl("txtRequisitionItemPrice");

                if (QtyRemaining > 0)
                {
                    txtQtyRemaining.Text = QtyRemaining.ToString();
                    txtRequisitionItemPrice.Text = String.Empty;
                    txtQtyRemaining.Visible = true;
                    lblQuantityRemaining.Visible = false;
                    txtRequisitionItemPrice.Visible = true;

                    if ( drv["PricePerItem"] != null && (decimal)drv["PricePerItem"] != 0 )
                    {
                        txtRequisitionItemPrice.Text = string.Format( "{0:N2}", drv["PricePerItem"] );
                    }
                }
                else
                {
                    txtQtyRemaining.Visible = false;
                    txtRequisitionItemPrice.Visible = false;
                    lblQuantityRemaining.Visible = true;
                }

            }
        }

        private void BindSelectPOItemsGrid()
        {
            ConfigureSelectPOItemsGrid();
            dgSelectPOItems.DataSource = GetItemsForPO();
            dgSelectPOItems.DataBind();
        }

        private void BindSelectPOItemsTypeList()
        {
            ddlSelectPOItemsType.Items.Clear();
            ddlSelectPOItemsType.DataSource = PurchaseOrder.GetPurchaseOrderTypes(true).OrderBy(x => x.Order);
            ddlSelectPOItemsType.DataValueField = "LookupID";
            ddlSelectPOItemsType.DataTextField = "Value";
            ddlSelectPOItemsType.DataBind();


        }

        private void BindSelectPOItemsVendorList()
        {
            ddlSelectPOItemsVendor.Items.Clear();
            ddlSelectPOItemsVendor.DataSource = Vendor.LoadVendors(true).OrderBy(v => v.VendorName);
            ddlSelectPOItemsVendor.DataValueField = "VendorID";
            ddlSelectPOItemsVendor.DataTextField = "VendorName";
            ddlSelectPOItemsVendor.DataBind();

            ddlSelectPOItemsVendor.Items.Insert(0, new ListItem("--Select--", "0"));

        }

        private void ClearSelectPOItemFields()
        {
            SetSelectPurchaseOrderItemError(String.Empty);
            hfSelectPOItemsPONumber.Value = String.Empty;
            ddlSelectPOItemsVendor.SelectedIndex = 0;
            lblSelectPOItemsPONumber.Text = "(new)";
            ddlSelectPOItemsVendor.SelectedIndex = 0;
            lblSelectPOItemsType.Text = String.Empty;
        }

        private void ConfigureSelectPOItemsGrid()
        {
            dgSelectPOItems.Visible = true;
            dgSelectPOItems.ItemType = "Item";
            /*dgSelectPOItems.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgSelectPOItems.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgSelectPOItems.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);
            */
            dgSelectPOItems.AllowSorting = false;
            /*
            dgSelectPOItems.NoResultText = "Items Not Found";
            dgSelectPOItems.MergeEnabled = false;
            dgSelectPOItems.EditEnabled = false;
            dgSelectPOItems.MailEnabled = false;
            dgSelectPOItems.AddEnabled = false;
            dgSelectPOItems.DeleteEnabled = false;
            dgSelectPOItems.SourceTableKeyColumnName = "ItemID";
            dgSelectPOItems.SourceTableOrderColumnName = "ItemID";
            dgSelectPOItems.AddImageUrl = "~/images/addButton.png";
            */
        }



        private DataTable GetItemsForPO()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
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

            foreach (RequisitionItem i in CurrentRequisition.Items.Where(x => x.Active))
            {
                int QuantityAssigned = i.POItems.Where(x => x.Active 
                        && x.PurchaseOrder.Active 
                        && x.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID()  )
                    .Select(p => p.Quantity).Sum();
                int QuantityRemaining = i.Quantity - QuantityAssigned;
                DataRow dr = dt.NewRow();
                dr["ItemID"] = i.ItemID;
                dr["ItemNumber"] = i.ItemNumber;
                dr["Description"] = i.Description;
                dr["DateNeeded"] = i.DateNeeded == DateTime.MinValue ? "" : i.DateNeeded.ToShortDateString();
                dr["AllowExpedited"] = i.IsExpeditiedShippingAllowed;
                dr["QtyRequested"] = i.Quantity;
                dr["QtyAssigned"] = QuantityAssigned;
                dr["QtyRemaining"] = QuantityRemaining;
                dr["PricePerItem"] = i.Price;

                dt.Rows.Add(dr);
            }

            return dt;
        }

        private void HideSelectPOItems()
        {
            ClearSelectPOItemFields();
            CurrentRequisition.RefreshItems();
            LoadItems();
            mpSelectPurchaseOrderItems.Hide();

        }

        private void PopulateSelectPOItemFields(int poID)
        {
            if (poID > 0)
            {
                PurchaseOrder PO = new PurchaseOrder(poID);
                if (ddlSelectPOItemsVendor.Items.FindByValue(PO.VendorID.ToString()) != null)
                    ddlSelectPOItemsVendor.SelectedValue = PO.VendorID.ToString();

                if (ddlSelectPOItemsType.Items.FindByValue(PO.PurchaseOrderTypeLUID.ToString()) != null)
                    ddlSelectPOItemsType.SelectedValue = PO.PurchaseOrderTypeLUID.ToString();

                lblSelectPOItemsType.Text = PO.PurchaseOrderType.Value;

                lblSelectPOItemsVendor.Text = PO.Vendor.VendorName;
                lblSelectPOItemsPONumber.Text = PO.PurchaseOrderID.ToString();

            }

            hfSelectPOItemsPONumber.Value = poID.ToString();

            SetSelectPOItemFieldVisibility(poID == 0);
        }

        private void SetSelectPOItemFieldVisibility(bool isNew)
        {
            ddlSelectPOItemsVendor.Visible = isNew;
            lblSelectPOItemsVendor.Visible = !isNew;

            ddlSelectPOItemsType.Visible = isNew;
            lblSelectPOItemsType.Visible = !isNew;

        }

        private void SetSelectPurchaseOrderItemError(string msg)
        {
            lblSelectPurchaseOrderError.Text = msg;
            lblSelectPurchaseOrderError.Visible = !String.IsNullOrEmpty(msg);
        }

        private void ShowSelectPOItem(int poID)
        {
            BindSelectPOItemsVendorList();
            BindSelectPOItemsTypeList();
            ClearSelectPOItemFields();
            PopulateSelectPOItemFields(poID);
            BindSelectPOItemsGrid();
            mpSelectPurchaseOrderItems.Show();
        }

        private bool UpdatePurchaseOrderItems()
        {
            bool HasBeenUpdated = false;
            try
            {

                int PurchaseOrderID = 0;
                int VendorID = 0;
                int TypeID = 0;
                int.TryParse(hfSelectPOItemsPONumber.Value, out PurchaseOrderID);
                int.TryParse(ddlSelectPOItemsType.SelectedValue, out TypeID);
                int.TryParse(ddlSelectPOItemsVendor.SelectedValue, out VendorID);

                List<ItemToAddToPO> AddItems = new List<ItemToAddToPO>();

                foreach (DataGridItem dgi in dgSelectPOItems.Rows)
                {
                    int itemID = 0;
                    int QtyRequested = 0;
                    decimal ItemPrice = 0;
                    int.TryParse(dgi.Cells[0].Text, out itemID);
                    TextBox tbQty = (TextBox)dgi.FindControl("txtQtyRemaining");
                    TextBox tbPrice = (TextBox)dgi.FindControl("txtRequisitionItemPrice");

                    if (tbQty != null && tbQty.Visible)
                        int.TryParse(tbQty.Text, out QtyRequested);
                    if (tbPrice != null && tbQty.Visible)
                        decimal.TryParse(tbPrice.Text, out ItemPrice);

                    if (QtyRequested > 0)
                    {
                        AddItems.Add(new ItemToAddToPO(itemID, QtyRequested, ItemPrice));
                    }
                }

                if (AddItems.Count == 0)
                {
                    SetSelectPurchaseOrderItemError("No items were added to Purchase Order.");
                    return false;
                }

                PurchaseOrder PO = null;
                if (PurchaseOrderID == 0)
                {
                    PO = new PurchaseOrder();
                    if (VendorID == 0 || TypeID == 0)
                    {
                        SetSelectPurchaseOrderItemError("Vendor and PO Type is required.");
                        return false;
                    }

                    PO.PurchaseOrderTypeLUID = TypeID;
                    PO.VendorID = VendorID;

                    Vendor poVendor = new Vendor(VendorID);

                    if (poVendor != null && !String.IsNullOrEmpty(poVendor.Terms))
                    {
                        PO.Terms = poVendor.Terms;
                    }

                    PO.StatusLUID = PurchaseOrder.PurchaseOrderStatusOpenLUID();
                    PO.Save(CurrentUser.UserName);
                    PurchaseOrderID = PO.PurchaseOrderID;
                    hfSelectPOItemsPONumber.Value = PO.PurchaseOrderID.ToString();
                }
                else
                {
                    PO = new PurchaseOrder(PurchaseOrderID);
                }

                PO.UpdatePOItems(AddItems, CurrentUser.UserName);

                CurrentRequisition.RefreshItems();
                HasBeenUpdated = true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException != null && rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append("An error has occurred while adding items to PO.");
                    sb.Append("<ul type=\"disc\">");
                    foreach (var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties)
                    {
                        sb.AppendFormat("<li>{0} - {1}</li>", item.Key, item.Value);
                    }

                    sb.Append("</ul>");
                    SetSelectPurchaseOrderItemError(sb.ToString());
                }
                else
                {
                    throw rEx;
                }
            }
            return HasBeenUpdated;
        }

        protected void rdoSelectPO_CheckedChanged( object sender, EventArgs e )
        {
            RadioButton selectedRB = (RadioButton)sender;

            foreach ( DataGridItem item in dgSelectPurchaseOrder.Rows )
            {
                if ( item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem )
                {
                    RadioButton rb = (RadioButton)item.FindControl( "rdoSelectPO" );

                    if ( rb.ClientID != selectedRB.ClientID )
                    {
                        rb.Checked = false;
                    }
                }
            }
        }

        #endregion

        #region Item Purchase Orders

        protected void btnIPOClose_Click(object sender, EventArgs e)
        {
            HideItemPurchaseOrdersModal();
        }

        protected void dgIPOPurchaseOrders_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView drv = (DataRowView)e.Item.DataItem;
                int POID = int.Parse(drv["PurchaseOrderID"].ToString());

                HyperLink hLinkPO = (HyperLink)e.Item.FindControl("hlPurchaseOrderID");
                Label lblPO = (Label)e.Item.FindControl("lblPurchaseOrderID");
                hLinkPO.Text = POID.ToString();
                hLinkPO.NavigateUrl = string.Format("~/default.aspx?page={0}&poid={1}", PurchaseOrderDetailPageSetting, POID);
                lblPO.Text = POID.ToString();

                if (UserCanEdit)
                {
                    hLinkPO.Visible = true;
                    lblPO.Visible = false;
                }
                else
                {
                    hLinkPO.Visible = false;
                    lblPO.Visible = true;
                }
            }
        }

        private void BindItemPOGrid(RequisitionItem reqItem)
        {
            ConfigureItemPurchaseOrderGrid();
            DataTable dt = new DataTable();

            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("PurchaseOrderID", typeof(int)),
                new DataColumn("PurchaseOrderType", typeof(string)),
                new DataColumn("VendorName", typeof(string)),
                new DataColumn("Status", typeof(string)),
                new DataColumn("QtyOrdered", typeof(string)),
                new DataColumn("QtyReceived", typeof(string))
                });

            if (reqItem != null)
            {
                var POSummary = reqItem.POItems.Where(poi => poi.Active)
                                .Select(x => new
                                {
                                    PurchaseOrderID = x.PurchaseOrderID,
                                    QtyOrdered = x.Quantity,
                                    QtyReceived = ((int?)x.ReceiptItems.Where(ri => ri.Active).Select(ri => ri.QuantityReceived).Sum() ?? 0)
                                }).GroupBy(xg => xg.PurchaseOrderID).Select(g => new
                                {
                                    PurchaseOrder = new PurchaseOrder(g.Key),
                                    QtyOrdered = g.Sum(q => q.QtyOrdered),
                                    QtyReceived = g.Sum(q => q.QtyReceived)
                                });

                foreach (var i in POSummary)
                {
                    DataRow dr = dt.NewRow();
                    dr["PurchaseOrderID"] = i.PurchaseOrder.PurchaseOrderID;
                    dr["PurchaseOrderType"] = i.PurchaseOrder.PurchaseOrderType.Value;
                    dr["VendorName"] = i.PurchaseOrder.Vendor.VendorName;
                    dr["Status"] = i.PurchaseOrder.Status.Value;
                    dr["QtyOrdered"] = i.QtyOrdered;
                    dr["QtyReceived"] = i.QtyReceived;

                    dt.Rows.Add(dr);
                }
            }

            dgIPOPurchaseOrders.DataSource = dt;
            dgIPOPurchaseOrders.DataBind();
        }

        private void ClearItemPurchaseOrdersModal()
        {
            lblIPOItemNumber.Text = "&nbsp;";
            lblIPODescription.Text = "&nbsp;";
            lblIPOQtyRequested.Text = "&nbsp;";
            lblIPOQtyReceived.Text = "&nbsp;";
            lblIPOAcctNumber.Text = "&nbsp;";
        }

        private void ConfigureItemPurchaseOrderGrid()
        {
            dgIPOPurchaseOrders.Visible = true;
            dgIPOPurchaseOrders.ItemType = "PurchaseOrder";
            /*dgIPOPurchaseOrders.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgIPOPurchaseOrders.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgIPOPurchaseOrders.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);
            */
            dgIPOPurchaseOrders.AllowSorting = false;
            /*dgIPOPurchaseOrders.NoResultText = "Item has not been assigend to Purhchase Order";
            dgIPOPurchaseOrders.MergeEnabled = false;
            dgIPOPurchaseOrders.EditEnabled = false;
            dgIPOPurchaseOrders.MailEnabled = false;
            dgIPOPurchaseOrders.AddEnabled = false;
            dgIPOPurchaseOrders.ExportEnabled = false;
            dgIPOPurchaseOrders.DeleteEnabled = false;*/
            dgIPOPurchaseOrders.AllowPaging = false;
            /*dgIPOPurchaseOrders.SourceTableKeyColumnName = "PurchaseOrderID";
            dgIPOPurchaseOrders.SourceTableOrderColumnName = "PurchaseOrderID";
            dgIPOPurchaseOrders.AddImageUrl = "~/images/addButton.png";*/
        }

        private void HideItemPurchaseOrdersModal()
        {
            ClearItemPurchaseOrdersModal();
            mpItemPurchaseOrder.Hide();
        }

        private void PopulateItemPurchaseOrdersModal(int itemID)
        {
            if (CurrentRequisition == null)
                return;
            RequisitionItem ReqItem = CurrentRequisition.Items.FirstOrDefault(x => x.ItemID == itemID);

            if (ReqItem != null)
            {
                if(!String.IsNullOrEmpty(ReqItem.ItemNumber))
                    lblIPOItemNumber.Text = ReqItem.ItemNumber;
                if(!String.IsNullOrEmpty(ReqItem.Description))
                    lblIPODescription.Text = ReqItem.Description;

                lblIPOQtyRequested.Text = ReqItem.Quantity.ToString();
                lblIPOQtyReceived.Text = ReqItem.POItems.Where(x => x.Active).Select(x => x.ReceiptItems.Where(ri => ri.Active).Select(ri => ri.QuantityReceived).Sum()).Sum().ToString();

                lblIPOAcctNumber.Text = String.Format("{0}-{1}-{2}", ReqItem.FundID, ReqItem.DepartmentID, ReqItem.AccountID);

                BindItemPOGrid(ReqItem);
            }
        }

        private void ShowItemPurchaseOrdersModal(int itemID)
        {
            ClearItemPurchaseOrdersModal();
            PopulateItemPurchaseOrdersModal(itemID);
            mpItemPurchaseOrder.Show();
        }

        #endregion

        #region Select Approver Modal
        protected void btnSelectApproverSelf_Click(object sender, EventArgs e)
        {
            if (CurrentRequisition.Approvals.Where(a => a.ApproverID == CurrentPerson.PrimaryAliasId && a.Active).Count() == 0)
            {
                AddApprover(CurrentPerson.PrimaryAliasId.Value);
            }

            mpSelectApprovalType.Hide();
        }

        protected void btnSelectApproverOther_Click(object sender, EventArgs e)
        {
            mpSelectApprovalType.Hide();
            ucStaffPickerApprover.Show();
        }

        protected void SelectApprover_Click(object sender, EventArgs e)
        {
            PersonAlias test = ((StaffPicker)sender).StaffPerson;
            AddApprover(test.AliasPersonId);
            CurrentRequisition = new Requisition(RequisitionID);
            LoadRequisition();

            // Clear the picker back out
            ucStaffPickerApprover.StaffPerson = null;
        }
        
        #endregion

        #region CER Select Modal
        protected void grdCERSelect_ReBind( object sender, EventArgs e )
        {
            LoadCERList();
        }

        private void ConfigureCERList()
        {
            grdCERSelect.Visible = true;
            grdCERSelect.ItemType = "CapitalRequest";
            /*grdCERSelect.ItemBgColor = CurrentPortalPage.Setting( "ItemBgColor", string.Empty, false );
            grdCERSelect.ItemAltBgColor = CurrentPortalPage.Setting( "ItemAltBgColor", string.Empty, false );
            grdCERSelect.ItemMouseOverColor = CurrentPortalPage.Setting( "ItemMouseOverColor", string.Empty, false );*/
            grdCERSelect.AllowSorting = true;
            /*grdCERSelect.MergeEnabled = false;
            grdCERSelect.EditEnabled = false;
            grdCERSelect.MailEnabled = false;
            grdCERSelect.AddEnabled = false;
            grdCERSelect.ExportEnabled = false;
            grdCERSelect.DeleteEnabled = false;
            grdCERSelect.SourceTableKeyColumnName = "CapitalRequestId";
            grdCERSelect.SourceTableOrderColumnName = "ProjectName";*/
        }

        private void LoadCERList()
        {
            ConfigureCERList();
            Dictionary<string, string> filter = new Dictionary<string, string>();

            filter.Add( "PersonId", CurrentPerson.PrimaryAliasId.ToString() );
            filter.Add( "UserId", CurrentUser.UserName );
            CurrentPerson.LoadAttributes();
            Guid? definedValueGuid = CurrentPerson.GetAttributeValue(MinistryAreaAttribute.Key).AsGuidOrNull();
            if (definedValueGuid.HasValue) {
                filter.Add("MinistryId", definedValueService.Get(definedValueGuid.Value).Id.ToString());
            }
            DefinedValue openStatus = definedValueService.Get(new Guid(CapitalRequest.LOOKUP_STATUS_OPEN_GUID));
            filter.Add( "StatusLUID", openStatus.Id.ToString() );
            filter.Add( "Show_Me", true.ToString() );
            filter.Add( "Show_Ministry", true.ToString() );
            filter.Add( "Show_Approver", false.ToString() );

            if ( UserCanEdit )
            {
                filter.Add( "Show_All", true.ToString() );
            }
            else
            {
                filter.Add( "Show_All", false.ToString() );
            }

            grdCERSelect.DataSource = CapitalRequest.GetCapitalRequestList( filter );
            grdCERSelect.DataBind();
        }
        protected void rbCERSelect_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton selectedRb = (RadioButton)sender;

            foreach ( GridViewRow item in grdCERSelect.Rows )
            {
                if ( item.RowType == DataControlRowType.DataRow)
                {
                    RadioButton itemRB = (RadioButton)item.FindControl( "rbCERSelect" );
                    if ( itemRB.ClientID != selectedRb.ClientID )
                    {
                        itemRB.Checked = false;
                    }
                }
            }
        }
        protected void btnCERSelectChoose_Click( object sender, EventArgs e )
        {
            int selectedCERId = 0;

            foreach (GridViewRow item in grdCERSelect.Rows)
            {
                if (item.RowType == DataControlRowType.DataRow)
                {
                    RadioButton itemRb = (RadioButton)item.FindControl( "rbCERSelect" );

                    if ( itemRb.Checked )
                    {
                        selectedCERId = grdCERSelect.DataKeys[item.RowIndex].Value.ToString().AsInteger();
                        break;
                    }
                }
            }

            SetCapitalRequest( selectedCERId );
            mpCERSelect.Hide();
        }
        protected void btnCERSelectCancel_Click( object sender, EventArgs e )
        {
            mpCERSelect.Hide();
        }

        #endregion
        protected void dgItems_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            return;

        }

        protected void mdAttachment_SaveClick( object sender, EventArgs e )
        {
            var attachmentParent = Attachment.GetPurchasingDocumentType();

            RockContext rockContext = new RockContext();
            var binaryFileService = new BinaryFileService( rockContext );

            //get the binary file
            var binaryFile = binaryFileService.Get( fuprAttachment.BinaryFileId.Value );

            //set binary file type
            binaryFile.BinaryFileType = new BinaryFileTypeService( rockContext )
                .Get( attachmentParent.Guid );

            //change settigns and save
            binaryFile.IsTemporary = false;
            binaryFile.Description = tbAttachmentDesc.Text;
            rockContext.SaveChanges();

            var attachment = new Attachment();
            attachment.ParentObjectTypeName = typeof(Requisition).ToString();
            attachment.ParentIdentifier = PageParameter( "RequisitionID" ).AsInteger();
            attachment.BlobID = binaryFile.Id;
            attachment.Save(CurrentUser.UserName);
            mdAttachment.Hide();
            LoadAttachments();
        }
    }
}