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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;


using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Purchasing;

namespace RockWeb.Plugins.org_secc.Purchasing
{

    [DisplayName("Capital Request Detail")]
    [Category("SECC > Purchasing")]
    [Description("Capital Request Detail page (display/edit a single CR).")]
    [LinkedPage("List Page", "The page that displays the capital request list.", true)]
    [LinkedPage("Person Detail Page", "Person Detail Page", false)]
    [BooleanField("Allow Ministry Selection", "A true/false flag that indicates if the user is able to select the requesting ministry. The default value is true.", false, "Summary Settings")]
    [BooleanField("Allow Requester Selection", "A true/false flag that indicates if the user is able to change the requester. The default value is true.", true, "Summary Settings")]
    [DefinedTypeField("Location Lookup Type", "The lookup type that contains the location values. If no value is provided, the location filed will not be visible.", false, "", "Summary Settings")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Location Attribute", " Location Attribute.", false, false, null, "Summary Settings")]
    [DefinedTypeField("Ministry Area Lookup Type", "The lookup type that the ministry area values are pulled from. The default value is \"Internal Departments\".", false, "BBA23298-F3E8-4477-8DD9-7CC8DF01AE7B","Summary Settings")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Ministry Area Person Attribute", "The Id of the person attribute that stores the user's Ministry Area.", false, false, null, "Summary Settings")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Position Person Attribute", "The Id of the person attribute that stores the user's job position.", false, false, null, "Staff Selector")]
    [TextField("Default Title", "The default title for the capital request.", false, "", "Summary Settings")]
    [IntegerField("Minimum Required Bids", "The minimum number of bids that are required for a capital request to be submitted for Ministry Approval. Default is 1.", false, 0, "Bid Settings")]
    [BinaryFileTypeField("Quote Document Type", "The document type that is used for quotes.", true, null, "Bid Settings")]
    [SystemEmailField("Ministry Approval Notification Template", "The email template that is used to notify the ministry approver that they have a capital request waiting for their approval.", true, "", "Approval")]
    [SystemEmailField("Finance Approval Notification Template", "The email template that is used to notify the finance approver that they have a capital request waiting for their approval.", true, "", "Approval")]
    [GroupField("Finance Approver Tag", "The tag that contains the list of individuals who can approve capital requests.", true, null, "Approval")]
    [SystemEmailField("Approved Notification Template", "The email template that is used to notify the requester that the capital request has been approved.", true, "", "Approval")]
    [SystemEmailField("Returned Notification Template", "The email template that is used to notify the requester that the capital request has been returned/denied by a requester.", true, "", "Approval")]
    [LinkedPage("Requisition Detail Page", "The page to use to display the detail of the requisition.", true, null, "Requisition")]
    public partial class CapitalRequestDetail : RockBlock
    {
        #region Fields
        private CapitalRequest mCapitalRequest = null;
        private bool? mUserCanEdit = null;
        DefinedValueService definedValueService = new DefinedValueService(new Rock.Data.RockContext());
        #endregion

        #region Module Settings
        public Guid ListPageSetting
        {
            get
            {
                return GetAttributeValue("ListPage").AsGuid();

            }
        }

        public bool AllowMinistrySelectionSetting
        {
            get
            {
                return bool.Parse(GetAttributeValue("AllowMinistrySelection"));
            }
        }

       public bool AllowRequesterSelectionSetting
        {
            get
            {
                return bool.Parse(GetAttributeValue("AllowRequesterSelection"));
            }
        }

        public Guid? LocationLookupTypeSetting
        {
            get
            {
                return GetAttributeValue("LocationLookupType").AsGuidOrNull();
            }
        }

        public Guid? LocationAttributeSetting
        {
            get
            {
                return GetAttributeValue("LocationAttribute").AsGuidOrNull();
            }
        }



        public Guid MinistryAreaLookupTypeSetting
        {
            get
            {
                return GetAttributeValue( "MinistryAreaLookupType").AsGuid();
            }
        }

       public Guid MinistryAreaPersonAttributeSetting
        {
            get
            {
                return GetAttributeValue("MinistryAreaPersonAttribute").AsGuid();
            }
        }

       public Guid PositionPersonAttributeSetting
        {
            get
            {
                return GetAttributeValue("PositionPersonAttribute").AsGuid();
            }
        }

        public string PersonDetailPageSetting 
        { 
            get 
            {
                return GetAttributeValue("PersonDetailPage") ?? "7";
            } 
        }

        public string DefaultTitleSetting
        {
            get
            {
                return GetAttributeValue("DefaultTitle")??"New Request";
            }
        }

        public int MinimumRequiredBidsSetting
        {
            get
            {
                return GetAttributeValue("MinimumRequiredBids").AsIntegerOrNull() ?? 1;
            }
        }
        
        public Guid QuoteDocumentTypeSetting
        {
            get
            {
                return GetAttributeValue("QuoteDocumentType").AsGuid();
            }
        }

        public Guid? MinistryApprovalNotificationTemplateSetting
        {
            get
            {
                return GetAttributeValue("MinistryApprovalNotificationTemplate").AsGuidOrNull();
            }
        }

        public Guid? FinanceApprovalNotificationTemplateSetting
        {
            get
            {
                return GetAttributeValue("FinanceApprovalNotificationTemplate").AsGuidOrNull();
            }
        }

        public Group FinanceApproverGroup
        {
            get
            {
                GroupService groupService = new GroupService(new RockContext());
                return groupService.Get(GetAttributeValue("FinanceApproverTag").AsGuid());

            }
        }

        public Guid? RequestApprovedNotificationTemplateSetting
        {
            get
            {
                return GetAttributeValue("ApprovedNotificationTemplate").AsGuidOrNull();

            }
        }

        public Guid? RequestReturnedNotificationTemplateSetting
        {
            get
            {
                return GetAttributeValue("ReturnedNotificationTemplate").AsGuidOrNull();
            }
        }

        public String RequisitionDetailPageSetting
        {
            get
            {

                PageService pageService = new PageService(new Rock.Data.RockContext());
                return "~/page/" + pageService.Get(new Guid(GetAttributeValue("RequisitionDetailPage"))).Id;
            }
        }


        #endregion

        #region Properties
        private int CERId
        {
            get
            {
                int requestID = 0;
                string vsKeyName = string.Format( "{0}_CERId", BlockId);

                if ( ViewState[vsKeyName] == null )
                {
                    if ( !String.IsNullOrEmpty( Request.QueryString["CER"] ) )
                    {
                        if(int.TryParse( Request.QueryString["CER"], out requestID ))
                        {
                            ViewState[vsKeyName] = requestID;
                        }
                        
                    }
                }
                else
                {
                    requestID = (int)ViewState[vsKeyName];
                }
                return requestID;
            }
            set
            {
                string vsKey = string.Format("{0}_CERId", BlockId);
                ViewState[vsKey] = value;
            }
        }

        private CapitalRequest CurrentCapitalRequest
        {
            get
            {
                if ( ( mCapitalRequest == null && CERId > 0 ) || 
                    ( CERId > 0 && mCapitalRequest != null && mCapitalRequest.CapitalRequestId != CERId ) )
                {
                    mCapitalRequest = new CapitalRequest(CERId);
                    mCapitalRequest.Status.LoadAttributes();
                    mCapitalRequest.CurrentPerson = CurrentPerson;
                }

                return mCapitalRequest;
            }
            set
            {
                mCapitalRequest = value;
            }
        }


        #endregion

        #region Module Events
        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            prsnRequester.MinistryAreaAttributeGuid = MinistryAreaPersonAttributeSetting;
            prsnRequester.PositionAttributeGuid = PositionPersonAttributeSetting;
            ucStaffSearchRequester.MinistryAreaAttributeGuid = MinistryAreaPersonAttributeSetting;
            ucStaffSearchRequester.PositionAttributeGuid = PositionPersonAttributeSetting;
            spRequisitionRequester.MinistryAreaAttributeGuid = MinistryAreaPersonAttributeSetting;
            spRequisitionRequester.PositionAttributeGuid = PositionPersonAttributeSetting;
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            docQuote.BinaryFileTypeGuid = QuoteDocumentTypeSetting;
        }

        protected void Page_Load( object sender, EventArgs e )
        {
            CurrentPerson.LoadAttributes();
            if ( !Page.IsPostBack )
            {
                LoadMinistryAreaList();
                LoadLocationList();
                LoadVendorList();
                LoadRequest(); 
            }
        }

        protected void lbMenuItem_Click( object sender, EventArgs e )
        {
            if (sender is ButtonDropDownList )
            {
                ButtonDropDownList bddl = ( ButtonDropDownList ) sender;

                switch ( bddl.SelectedValue )
                {
                    case "Ministry":
                        ucStaffSearchRequester.Title = "Ministry Approver";
                        ucStaffSearchRequester.MinistryAreaAttributeGuid = MinistryAreaPersonAttributeSetting;
                        ucStaffSearchRequester.PositionAttributeGuid = PositionPersonAttributeSetting;
                        ucStaffSearchRequester.Show();
                        break;
                    case "LeadTeam":
                        ucStaffSearchRequester.Title = "Lead Team Approver";
                        ucStaffSearchRequester.MinistryAreaAttributeGuid = MinistryAreaPersonAttributeSetting;
                        ucStaffSearchRequester.PositionAttributeGuid = PositionPersonAttributeSetting;
                        ucStaffSearchRequester.Show();
                        break;
                    default:
                        bddl.ClearSelection();
                        break;
                }

                

            }
            if (sender is LinkButton)
            { 
                LinkButton lb = (LinkButton)sender;

                switch ( lb.CommandName.ToLower() )
                {
                    case "addrequisition":
                        ShowAddRequisitionModal();
                        break;
                    case "editsummary":
                        LoadSummary( true );
                        break;
                    case "returntolist":
                        ReturnToList();
                        break;
                    case "addbid":
                        LoadBidDetail( 0 );
                        break;
                    case "requestapproval":
                        RequestApproval();
                        break;
                    case "close":
                        CloseRequest();
                        break;
                    case "cancel":
                        CancelRequestStep1();
                        break;
                    case "reopen":
                        ReopenRequest();
                        break;
                    default:
                        break;
                }
            }

        }


        #endregion

        #region Summary Events

        protected void ddlRequestingMinistry_SelectedIndexChanged( object sender, EventArgs e )
        {
            hfRequestingMinistry.Value = ddlRequestingMinistry.SelectedValue;
        }

        protected void btnSummarySave_Click( object sender, EventArgs e )
        {
            SetSummaryError( null );
            if ( SaveCapitalRequestSummary() )
            {
                LoadRequest();
            }

        }

        protected void btnSummaryCancel_Click( object sender, EventArgs e )
        {
            if ( CERId <= 0 )
            {
                ReturnToList();
            }
            else
            {
                LoadSummary( false );
            }
        }

        #endregion

        #region Approval Grid Events

        protected void grdApprovalRequests_ReBind( object sender, EventArgs e )
        {
            LoadApprovalRequests();
        }
        protected void grdApprovalRequests_RowCommand(object source, GridViewCommandEventArgs e)
        {
            int approvalId = 0;

            if ( !int.TryParse( e.CommandArgument.ToString(), out approvalId ) )
            {
                return;
            }

            switch ( e.CommandName.ToLower() )
            {
                case "approve":
                    ApproveRequest( approvalId );
                    break;
                case "return":
                    ReturnRequest( approvalId );
                    break;
                case "remove":
                    RemoveApprovalRequest( approvalId );
                    break;
                default:
                    break;
            }
        }
        protected void grdApprovalRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                bool canApprove = false;
                bool canRemove = false;
                ApprovalListItem a = (ApprovalListItem)e.Row.DataItem;

                if ( a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() && a.ApproverId == CurrentPerson.PrimaryAliasId && a.ApprovalStatusLUID == Approval.PendingApprovalStatusLUID() )
                {
                    canApprove = true;
                }
                else if ( a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() && UserIsFinanceApprover() && a.ApprovalStatusLUID == Approval.PendingApprovalStatusLUID() )
                {
                    canApprove = true;
                }

                if (a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() && a.CreatedByPersonId == CurrentPerson.PrimaryAliasId && a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID())
                {
                    canRemove = true;
                }


                LinkButton lbApprove = (LinkButton)e.Row.FindControl("lbApprove");
                LinkButton lbReturn = (LinkButton)e.Row.FindControl("lbReturn");
                LinkButton lbRemove = (LinkButton)e.Row.FindControl("lbRemove");

                lbApprove.Visible = canApprove;
                lbApprove.CommandArgument = a.ApprovalId.ToString();
                lbReturn.Visible = canApprove;
                lbReturn.CommandArgument = a.ApprovalId.ToString();
                lbRemove.Visible = canRemove;
                lbRemove.CommandArgument = a.ApprovalId.ToString();
                bool alreadyVisible = grdApprovalRequests.Columns[e.Row.Cells.Count - 1].Visible;
                grdApprovalRequests.Columns[e.Row.Cells.Count - 1].Visible = alreadyVisible || canApprove || canRemove;
            }
        }
        #endregion

        #region Bid Grid Events
        protected void grdBids_ReBind( object sender, EventArgs e )
        {
            LoadBids();
        }
        protected void grdBids_RowCommand(object source, GridViewCommandEventArgs e)
        {
            int bidID = 0;
            if ( !int.TryParse( e.CommandArgument.ToString(), out bidID ) )
            {
                return;
            }

            switch ( e.CommandName.ToLower() )
            {
                case "remove":
                    RemoveBid( bidID );
                    break;
                case "setpreferred":
                    SetPreferredBid( bidID );
                    break;
                case "editbid":
                    LoadBidDetail( bidID );
                    break;
                default:
                    break;
            }
        }
        protected void grdBids_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                // Display the company name in italics.
                e.Row.Cells[1].Text = "<i>" + e.Row.Cells[1].Text + "</i>";

                var bid = (CapitalRequestBidListItem)e.Row.DataItem;
                bool canEdit = UserCanEditBids();
                LinkButton lbEdit = (LinkButton)e.Row.FindControl("lbEdit");
                LinkButton lbRemove = (LinkButton)e.Row.FindControl("lbRemove");
                LinkButton lbSetPreferred = (LinkButton)e.Row.FindControl("lbSetPreferred");
                HtmlGenericControl  imgSelectPreferred = (HtmlGenericControl )e.Row.FindControl("imgPreferredSelection");

                Control imgIsPreferred = e.Row.FindControl( "imgIsPreferred" );

                lbSetPreferred.Visible = false;
                imgIsPreferred.Visible = false;

                if ( canEdit )
                {
                    lbSetPreferred.Visible = true;
                    lbSetPreferred.CommandArgument = bid.BidID.ToString();
                    imgSelectPreferred.AddCssClass(bid.IsPreferredBid ? "fa fa-check-square-o" : "fa fa-square-o");
                }
                else
                {
                    if(bid.IsPreferredBid)
                    {
                        imgIsPreferred.Visible = true;
                    }
                }

                lbEdit.Visible = canEdit;
                lbEdit.CommandArgument = bid.BidID.ToString();

                lbRemove.Visible = canEdit;
                lbRemove.CommandArgument = bid.BidID.ToString();

            }
        }
        #endregion

        #region Bid Model Events
        protected void ddlVendorList_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlVendorList.SelectedValue == "-1" )
            {
                pnlVendorName.Visible = true;
                txtVendorName.Text = null;
            }
            else
            {
                pnlVendorName.Visible = false;
            }
        }
        protected void btnBidSave_Click( object sender, EventArgs e )
        {
            if ( SaveBid() )
            {
                mpBidDetail.Hide();
                LoadRequest();
                upMain.Update();
            }
        }
        #endregion

        #region Cancel Request Modal
        protected void btnCancelRequestConfirm_Click( object sender, EventArgs e )
        {
            CancelRequestStep2();
            mpCancelRequest.Hide();
        }
        protected void btnCancelRequestCancel_Click( object sender, EventArgs e )
        {
            mpCancelRequest.Hide();
        }
        #endregion

        #region Requisition Grid Events
        protected void grdRequisitions_ReBind( object sender, EventArgs e )
        {
            LoadRequisitions();
        }
        protected void grdRequisitions_ItemCommand( object source, DataGridCommandEventArgs e )
        {

        }
        protected void grdRequisitions_ItemDataBound( object sender, DataGridItemEventArgs e )
        {

        }
        #endregion 

        #region Requisition Modal Events
        protected void btnRequisitionAddSelectRequester_Click( object sender, EventArgs e )
        {
            /*ucStaffSearchRequester.Title = "Requester Select";
            ucStaffSearchRequester.ParentPersonControlID = hfRequisitionAddSearchResults.ClientID;
            ucStaffSearchRequester.ParentRefreshButtonID = btnRequisitionAddRequesterSet.ClientID;
            ucStaffSearchRequester.MinistryAreaAttributeID = MinistryAreaPersonAttributeSetting;
            ucStaffSearchRequester.PositionAttributeID = PositionPersonAttributeSetting;
            ucStaffSearchRequester.PersonDetailPage = PersonDetailPageSetting;
            ucStaffSearchRequester.Show();*/
        }

        protected void btnRequisitionAddSave_Click( object sender, EventArgs e )
        {
            int requesterId = 0;
            SetAddRequisitionErrorMessage( null );
            try
            {

                int requisitionID = CurrentCapitalRequest.AddRequisition( txtRequisitionAddTitle.Text, spRequisitionRequester.StaffPersonAliasId.Value, txtRequisitionAddDeliverTo.Text, CurrentUser.UserName );

                mpAddRequistion.Hide();

                if ( requisitionID > 0 && cbRequisitionAddOpenRequisition.Checked )
                {
                    PageService pageService = new PageService( new RockContext() );
                    string reqLink = string.Format( "/page/{0}?RequisitionId={1}", pageService.Get( new Guid( GetAttributeValue( "RequisitionDetailPage" ) ) ).Id, requisitionID );

                    string script = string.Format( "window.open(\"{0}\", \"_blank\");", reqLink );

                    ScriptManager.RegisterStartupScript( mpAddRequistion, mpAddRequistion.GetType(), "AddRequisition" + DateTime.Now.Ticks, script, true );
                }

                LoadRequest();
            }
            catch ( RequisitionException rEx )
            {
                if(rEx.InnerException == null || rEx.InnerException.GetType() != typeof(RequisitionNotValidException))
                {
                    throw rEx;
                }

                RequisitionNotValidException rnvEx = (RequisitionNotValidException)rEx.InnerException;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append( "An error occurred while saving requisition. The following fields are invalid." );
                sb.Append( "<ul type=\"disc\">" );
                foreach ( var item in rnvEx.InvalidProperties )
                {
                    sb.AppendFormat( "<li>{0} - {1}</li>", item.Key, item.Value );
                }

                sb.Append( "</ul>" );

                SetAddRequisitionErrorMessage( sb.ToString() );
            }

        }

        protected void btnRequisitionAddCancel_Click( object sender, EventArgs e )
        {
            mpAddRequistion.Hide();
        }

        #endregion

        #region Return to Requester Events

        protected void btnReturnToRequesterSave_Click( object sender, EventArgs e )
        {
            int approvalId = 0;

            int.TryParse( hfApprovalId.Value, out approvalId );

            if ( approvalId <= 0 || String.IsNullOrWhiteSpace( txtReturnReason.Text ) )
            {
                return;
            }

            ReturnToRequesterStepTwo( approvalId, txtReturnReason.Text );
        }
        protected void btnReturnToRequesterCancel_Click( object sender, EventArgs e )
        {
            mpReturnToRequesterReason.Hide();
        }

        #endregion

        #region Private Methods

        private void ApproveRequest( int approvalId )
        {
            var approvalReq = CurrentCapitalRequest.ApprovalRequests.FirstOrDefault( a => a.ApprovalID == approvalId );

            if ( approvalReq == null )
            {
                return;
            }

            approvalReq.ApprovalStatusLUID = Approval.ApprovedStatusLUID();
            approvalReq.DateApproved = DateTime.Now;

            

            if ( approvalReq.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() )
            {
                approvalReq.Save( CurrentUser.UserName );

                var pendingMinistryRequestsCount = CurrentCapitalRequest.ApprovalRequests
                                            .Where( a => a.Active )
                                            .Where( a => a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() )
                                            .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() || a.ApprovalStatusLUID == Approval.PendingApprovalStatusLUID() )
                                            .Count();
                if ( pendingMinistryRequestsCount > 0 )
                {
                    RequestMinistryApproval();
                }
                else
                {
                    RequestFinanceApproval();
                }
                
            }
            else
            {
                approvalReq.ApproverID = CurrentPerson.PrimaryAliasId??0;
                approvalReq.Save( CurrentUser.UserName );
                CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_OPEN_GUID, CurrentUser.UserName );
                CurrentCapitalRequest.SendApprovedNotification( RequestApprovedNotificationTemplateSetting, GetCERLink() );
            }

            LoadRequest();
        }

        private void CancelRequestStep1()
        {
            SetSummaryError( null );
            var activeRequisitions = CurrentCapitalRequest.Requisitions
                                        .Where( r => r.Active )
                                        .Where( r => r.StatusLUID != Requisition.CancelledLUID() )
                                        .Count();

            if ( activeRequisitions > 0 )
            {
                SetSummaryError( "Unable to cancel. Capital Request has active Requisitions." );
                return;
            }

            mpCancelRequest.Show();
        }

        private void CancelRequestStep2()
        {
            CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_CANCELLED_GUID, CurrentUser.UserName );
            LoadRequest();
        }

        private bool CapitalRequestHasChanged()
        {
            bool hasChanged = false;
            if ( CurrentCapitalRequest == null )
            {
                if ( !String.IsNullOrWhiteSpace( txtProjectName.Text ) )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && String.IsNullOrWhiteSpace( txtDescription.Text ) )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && String.IsNullOrWhiteSpace( txtProjectCost.Text ) )
                {
                    hasChanged = true;
                }
            }
            else
            {
                if ( CurrentCapitalRequest.ProjectName != txtProjectName.Text.Trim() )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.ProjectDescription != txtDescription.Text.Trim() )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.PurchaseCost.ToString() != txtProjectCost.Text.Trim() )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.InstallationCost.ToString() != txtInitialCost.Text.Trim() )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.OngoingMaintenanceCost.ToString() != txtOngoingCost.Text.Trim() )
                {
                    hasChanged = true;
                }
                if (!hasChanged && CurrentCapitalRequest.RequesterId != prsnRequester.StaffPersonAliasId)
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.MinistryLUID.ToString() != hfRequestingMinistry.Value )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.RequestedOn.ToShortDateString() != hfDateRequestedEdit.Value )
                {
                    hasChanged = true;
                }
                if ( !hasChanged && CurrentCapitalRequest.AnticipatedInserviceDate.ToShortDateString() != txtInServiceDate.Text )
                {
                    hasChanged = true;
                }
                
                if(!hasChanged && CurrentCapitalRequest.GLAccountNumberFormatted != txtGLAccount.Text)
                {
                    hasChanged = true;
                }

            }

            return hasChanged;
        }

        private void CloseRequest()
        {
            SetSummaryError( null );
            var openRequisitions = CurrentCapitalRequest.Requisitions
                                    .Where( r => r.Active )
                                    .Where(r => !r.Status.AttributeValues["IsClosed"].Value.AsBoolean())
                                    .Count();

            if ( openRequisitions > 0 )
            {
                SetSummaryError( "Unable to close. Capital Request has open Requisitions." );
                return;
            }

            CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_CLOSED_GUID, CurrentUser.UserName );
            LoadRequest();
        }

        private void ConfigureApprovalRequestGrid()
        {
            pnlApproval.Visible = false;
            grdApprovalRequests.Visible = false;
            grdApprovalRequests.ItemType = "Approval";
            grdApprovalRequests.AllowSorting = false;
            grdApprovalRequests.AllowPaging = false;
            grdApprovalRequests.Columns[grdApprovalRequests.Columns.Count - 1].Visible = false;

        }

        private void ConfigureBidGrid()
        {
            pnlBids.Visible = false;
            grdBids.Visible = false;
            grdBids.ItemType = "CapitalRequestBid";
            /*grdBids.ItemBgColor = CurrentPortalPage.Setting( "ItemBgColor", string.Empty, false );
            grdBids.ItemAltBgColor = CurrentPortalPage.Setting( "ItemAltBgColor", string.Empty, false );
            grdBids.ItemMouseOverColor = CurrentPortalPage.Setting( "ItemMouseOverColor", string.Empty, false );*/
            grdBids.AllowSorting = false;
            /*grdBids.NoResultText = "No Bids found";
            grdBids.MergeEnabled = false;
            grdBids.EditEnabled = false;
            grdBids.MailEnabled = false;
            grdBids.AddEnabled = false;
            grdBids.ExportEnabled = false;
            grdBids.DeleteEnabled = false;*/
            grdBids.AllowPaging = false;
            /*grdBids.SourceTableKeyColumnName = "BidID";
            grdBids.SourceTableOrderColumnName = "BidID";
            grdBids.AddImageUrl = "~/images/addButton.png";*/
        }

        private void ConfigureRequisitionGrid()
        {   pnlRequisition.Visible = false;
            grdRequisitions.Visible = false;
            grdRequisitions.ItemType = "Requisition";
            grdRequisitions.AllowSorting = false;
            grdRequisitions.AllowPaging = false;
        }

        private string GetCERLink()
        {
            return GlobalAttributesCache.Value("InternalApplicationRoot").ReplaceLastOccurrence("/", "") +
                "/page/" + CurrentPageReference.PageId + "?CER=" + CurrentCapitalRequest.CapitalRequestId;
        }

        private int GetCurrentUsersLocation()
        {
            if (LocationAttributeSetting.HasValue) { 
                CurrentPerson.LoadAttributes();
                Guid dv = CurrentPerson.GetAttributeValue(AttributeCache.Read(LocationAttributeSetting.Value).Key).AsGuid();
                return DefinedValueCache.Read(dv).Id;
            }
            return 0;
        }

        private int GetCurrentUsersMinistryArea()
        {
            CurrentPerson.LoadAttributes();
            if ( MinistryAreaPersonAttributeSetting != null && AttributeCache.Read( MinistryAreaPersonAttributeSetting ) != null && CurrentPerson.GetAttributeValue( AttributeCache.Read( MinistryAreaPersonAttributeSetting ).Key ) != null)
            {
                Guid dv = CurrentPerson.GetAttributeValue( AttributeCache.Read( MinistryAreaPersonAttributeSetting ).Key ).AsGuid();
                return DefinedValueCache.Read( dv ).Id;
            }
            return 0;
        }

        private void LoadApprovalRequests()
        {
            ConfigureApprovalRequestGrid();

            if ( CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId <= 0 )
            {
                return;
            }

            var approvalRequests = CurrentCapitalRequest.ApprovalRequests
                                    .Where( a => a.Active )
                                    .OrderBy( a => a.ApprovalType.Order )
                                    .Select( a => new ApprovalListItem()
                                            {
                                                ApprovalId = a.ApprovalID,
                                                ApprovalTypeLUID = a.ApprovalTypeLUID,
                                                ApprovalTypeName = a.ApprovalType != null ? a.ApprovalType.Value : null,
                                                ApproverId = a.Approver != null ? (int?)a.Approver.PrimaryAliasId : null,
                                                ApproverFullName = a.Approver != null ? a.Approver.FullName : null,
                                                ApprovalStatusLUID = a.ApprovalStatusLUID,
                                                ApprovalStatusName = a.ApprovalStatus != null ? a.ApprovalStatus.Value : null,
                                                DateApprovedString = a.DateApproved > DateTime.MinValue ? a.DateApproved.ToShortDateString()+ " " + a.DateApproved.ToShortTimeString() : "(not approved)",
                                                LastComment = a.ApprovalStatusLUID == Approval.ApprovedStatusLUID() ? String.Empty : 
                                                    a.GetLastNote(),
                                                CreatedByPersonId = a.CreatedBy.PrimaryAliasId.Value
                                            } );

            if ( approvalRequests.Count() == 0 )
            {
                return;
            }

            grdApprovalRequests.DataSource = approvalRequests.ToList();
            grdApprovalRequests.DataBind();
            pnlApproval.Visible = true;
            grdApprovalRequests.Visible = true;
            grdApprovalRequests.ActionRow.Visible = false;

        }

        private void LoadBidDetail( int bidId )
        {
            ResetBidDetailFields();
            PopulateBidDetail( bidId );
            mpBidDetail.Show();

        }

        private void LoadBids()
        {
            ConfigureBidGrid();
           
            if ( CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId <= 0 )
            {
                return;
            }

            var Bids = CurrentCapitalRequest.Bids.Where( b => b.Active )
                        .Select( b => new CapitalRequestBidListItem
                        {
                            BidID = b.BidID,
                            VendorName = b.VendorNameValue,
                            VendorContact = b.VendorContact,
                            BidAmount = b.BidAmount,
                            QuoteTitle = b.QuoteBlob == null ? String.Empty : ( !String.IsNullOrEmpty( b.QuoteBlob.FileName ) ? b.QuoteBlob.FileName : b.QuoteBlob.TypeName ),
                            QuoteGuid = b.QuoteBlob == null ? Guid.Empty : b.QuoteBlob.Guid,
                            IsPreferredBid = b.IsPreferredBid,
                            Active = b.Active
                        } )
                        .ToList();

            if ( Bids.Count == 0 )
            {
                return;
            }

            grdBids.DataSource = Bids;
            grdBids.DataBind();

            pnlBids.Visible = true;
            grdBids.Visible = true;
            grdBids.ActionRow.Visible = false;
        }

        private void LoadLocationList()
        {
            ddlSCCLocation.Items.Clear();

            if ( LocationLookupTypeSetting.HasValue )
            {
                ddlSCCLocation.DataSource = definedValueService.GetByDefinedTypeGuid( LocationLookupTypeSetting.Value )
                                                    .OrderBy(l => l.Order)
                                                    .Select( l => new { l.Id, l.Value } )
                                                    .ToList();

                ddlSCCLocation.DataValueField = "Id";
                ddlSCCLocation.DataTextField = "Value";
                ddlSCCLocation.DataBind();

                ddlSCCLocation.Items.Insert( 0, new ListItem( "[Select]", "0" ) );
            }
        }

        private void LoadMinistryAreaList()
        {
            ddlRequestingMinistry.Items.Clear();

            ddlRequestingMinistry.DataSource = definedValueService.GetByDefinedTypeGuid(MinistryAreaLookupTypeSetting).Select(l => new { l.Id, l.Value }).OrderBy(l => l.Value).ToList();
            ddlRequestingMinistry.DataValueField = "Id";
            ddlRequestingMinistry.DataTextField = "Value";
            ddlRequestingMinistry.DataBind();

            ddlRequestingMinistry.Items.Insert( 0, new ListItem( "[Select]", "0" ) );
        }
        
        private void LoadRequest()
        {
            SetRequestError( null );
            mUserCanEdit = null;
            pnlSummary.Visible = false;
            SetMenuItemVisibility();

            if ( !UserCanView() )
            {
                SetRequestError( "Unable to view Capital Request. Click \"Return to List\" to continue." );
                return;
            }

            if ( CERId <= 0 && CurrentCapitalRequest == null )
            {
                LoadSummary( true );
            }
            else if( CurrentCapitalRequest.CapitalRequestId == CERId)
            {
                LoadSummary( false );
                LoadBids();
                LoadApprovalRequests();
                LoadRequisitions();

                if ( !CurrentCapitalRequest.Active )
                {
                    SetRequestError( "This Capital Request is not active." );
                }

                if (CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean())
                {
                    SetRequestError( string.Format( "This Capital Request is {0}.", CurrentCapitalRequest.Status.Value ) );
                }
            }
            else
            {
                SetRequestError( "Capital Request not found. Click \"Return to List\" to continue." );

            }
        }

        private void LoadRequisitions()
        {
            ConfigureRequisitionGrid();


            var requisitions = CurrentCapitalRequest.Requisitions
                                .Where( r => r.Active )
                                .Where( r => r.StatusLUID != Requisition.CancelledLUID() )
                                .Select( r => new RequisitionListItem()
                                        {
                                            RequisitionID = r.RequisitionID,
                                            Title = r.Title,
                                            RequesterID = r.Requester.PrimaryAliasId.Value,
                                            RequesterName = r.Requester.FullName,
                                            RequesterLastFirst = string.Format( "{0}, {1}", r.Requester.LastName, r.Requester.NickName ),
                                            Status = r.Status.Value,
                                            RequisitionType = r.RequisitionType.Value,
                                            DateSubmitted = r.DateSubmitted,
                                            IsApproved = r.IsApproved,
                                            IsAccepted = r.DateAccepted > DateTime.MinValue,
                                            CurrentChargeTotal = r.Charges.Where( c => c.Active ).Sum( c => c.Amount )
                                        } )
                                .OrderBy( r => r.RequisitionID );


            grdRequisitions.DataSource = requisitions.ToList();
            grdRequisitions.DataBind();

            bool isVisible = requisitions.Count() > 0;

            pnlRequisition.Visible = isVisible;
            grdRequisitions.Visible = isVisible;
            grdRequisitions.ActionRow.Visible = false;
        }

        private void LoadSummary( bool isEditable )
        {
            SetTitle();
            pnlSummary.Visible = true;
            SetSummaryError( String.Empty );
            pnlSummaryView.Visible = !isEditable;
            pnlSummaryEdit.Visible = isEditable;

            if ( isEditable )
            {
                LoadSummaryEdit();
            }
            else
            {
                LoadSummaryView();
            }
        }

        private void LoadSummaryEdit()
        {
            lbMenuItem_EditSummary.Visible = false;
            ResetSummaryEditFields();
            PopulateSummaryEditFields();

            bool approverCanBeSet = true;

            if(CurrentCapitalRequest != null)
            {

                DefinedValue MinistryApprovalLU = definedValueService.GetByGuid(new Guid(CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID));
                approverCanBeSet = CurrentCapitalRequest.Status.Order < MinistryApprovalLU.Order;
            }
            

            ddlRequestingMinistry.Visible = AllowMinistrySelectionSetting;
            divRequestingMinistry.Visible = AllowMinistrySelectionSetting;

            prsnRequester.Visible = AllowRequesterSelectionSetting;
        }

        private void LoadVendorList()
        {
            ddlVendorList.Items.Clear();

            ddlVendorList.DataSource = Vendor.LoadVendors( true ).Select( v => new { v.VendorID, v.VendorName } ).OrderBy( v => v.VendorName );
            ddlVendorList.DataValueField = "VendorID";
            ddlVendorList.DataTextField = "VendorName";
            ddlVendorList.DataBind();

            ddlVendorList.Items.Insert( 0, new ListItem( "[Select]", "0" ));
            ddlVendorList.Items.Insert( 1, new ListItem( "[Not Listed]", "-1" ) );
        }

        private void LoadSummaryView()
        {
            lbMenuItem_EditSummary.Visible = UserCanEditSummary();
            ResetSummaryViewFields();
            PopulateSummaryViewFields();
        }

        private void PopulateBidDetail( int bidId )
        {
            if ( CurrentCapitalRequest == null )
            {
                return;
            }

            var bid = CurrentCapitalRequest.Bids.FirstOrDefault( b => b.BidID == bidId );

            if ( bid == null )
            {
                return;
            }
            hfBidId.Value = bid.BidID.ToString();
            if ( bid.VendorID > 0 )
            {
                ddlVendorList.SelectedValue = bid.VendorID.ToString();
                pnlVendorName.Visible = false;
            }
            else
            {
                ddlVendorList.SelectedValue = "-1";
                pnlVendorName.Visible = true;
                txtVendorName.Text = bid.VendorName;
            }

            if ( bid.VendorContactPhone != null && !String.IsNullOrWhiteSpace( bid.VendorContactPhone.Number ) )
            {
                //txtVendorContactPhone.Number = bid.VendorContactPhone.FormatNumber( false );

                //txtVendorContactPhone.Extension = bid.VendorContactPhone.Extension;
            }

            txtVendorContactName.Text = bid.VendorContactName;
            txtVendorContactEmail.Text = bid.VendorContactEmail;

            txtQuotedPrice.Text = string.Format( "{0:N2}", bid.BidAmount );
            docQuote.BinaryFileId = bid.QuoteBlobID;
            cbPreferredBid.Checked = bid.IsPreferredBid;
           
        }

        private void PopulateSummaryEditFields()
        {
            if ( CurrentCapitalRequest == null )
            {
                return;
            }
            txtProjectName.Text = CurrentCapitalRequest.ProjectName.Trim();
            txtDescription.Text = CurrentCapitalRequest.ProjectDescription.Trim();
            txtProjectCost.Text = CurrentCapitalRequest.PurchaseCost.ToString( "N2" );

            if ( CurrentCapitalRequest.InstallationCost > 0 )
            {
                txtInitialCost.Text = CurrentCapitalRequest.InstallationCost.ToString( "N2" );
            }

            if ( CurrentCapitalRequest.OngoingMaintenanceCost > 0 )
            {
                txtOngoingCost.Text = CurrentCapitalRequest.OngoingMaintenanceCost.ToString( "N2" );
            }

            if ( CurrentCapitalRequest.RequesterId > 0  )
            {
                SetRequesterEdit( CurrentCapitalRequest.RequesterId );
            }

            if ( CurrentCapitalRequest.MinistryLUID > 0 )
            {
                hfRequestingMinistry.Value = CurrentCapitalRequest.MinistryLUID.ToString();
                ddlRequestingMinistry.SelectedValue = CurrentCapitalRequest.MinistryLUID.ToString();
            }

            if ( LocationLookupTypeSetting.HasValue )
            {
                ddlSCCLocation.SelectedValue = CurrentCapitalRequest.LocationLUID.ToString();
            }

            if ( CurrentCapitalRequest.StatusLUID > 0 )
            {
                lStatusEdit.Text = CurrentCapitalRequest.Status.Value;
            }

            if ( CurrentCapitalRequest.RequestedOn > DateTime.MinValue )
            {
                hfDateRequestedEdit.Value = CurrentCapitalRequest.RequestedOn.ToShortDateString();
                lDateRequestedEdit.Text = CurrentCapitalRequest.RequestedOn.ToShortDateString();
            }

            txtItemLocation.Text = CurrentCapitalRequest.ItemLocation;

            if ( CurrentCapitalRequest.AnticipatedInserviceDate > DateTime.MinValue )
            {
                txtInServiceDate.Text = CurrentCapitalRequest.AnticipatedInserviceDate.ToShortDateString();
            }

            if ( CurrentCapitalRequest.GLFundId > 0 && CurrentCapitalRequest.GLDepartmentId > 0 && CurrentCapitalRequest.GLAccountId > 0 )
            {
                txtGLAccount.Text = string.Format( "{0}-{1}-{2}", CurrentCapitalRequest.GLFundId, CurrentCapitalRequest.GLDepartmentId, CurrentCapitalRequest.GLAccountId );
            }
        }

        private void PopulateSummaryViewFields()
        {
            if(CurrentCapitalRequest == null)
            {
                return;
            }

            lProjectName.Text = CurrentCapitalRequest.ProjectName;
            lProjectDescription.Text = CurrentCapitalRequest.ProjectDescription;

            if ( CurrentCapitalRequest.PurchaseCost > 0 )
            {
                lPurchaseCost.Text = string.Format( "{0:c}", CurrentCapitalRequest.PurchaseCost );
            }

            if ( CurrentCapitalRequest.InstallationCost > 0 )
            {
                lOtherInitialCost.Text = string.Format( "{0:c}", CurrentCapitalRequest.InstallationCost );
            }

            if ( CurrentCapitalRequest.OngoingMaintenanceCost > 0 )
            {
                lOngoingCosts.Text = string.Format( "{0:c}", CurrentCapitalRequest.OngoingMaintenanceCost );
            }


            if ( CurrentCapitalRequest.RequesterId > 0 )
            {
                lRequester.Text = CurrentCapitalRequest.Requester.FullName;
            }

            if ( CurrentCapitalRequest.MinistryLUID > 0 )
            {
                lRequestingMinistry.Text = CurrentCapitalRequest.Ministry.Value;
            }

            if ( LocationLookupTypeSetting.HasValue )
            {
                string locationName = null;
                switch ( CurrentCapitalRequest.LocationLUID )
                {
                    case -1:
                        locationName = "Multiple";
                        break;
                    case 0:
                        locationName = "Not Selected";
                        break;
                    default:
                        locationName = CurrentCapitalRequest.Location.Value;
                        break;
                }
                lSCCLocation.Text = locationName;
                pnlSCCLocationView.Visible = true;
            }


            if ( CurrentCapitalRequest.StatusLUID > 0 )
            {
                lStatus.Text = CurrentCapitalRequest.Status.Value;
            }

            if ( !String.IsNullOrWhiteSpace( CurrentCapitalRequest.ItemLocation ) )
            {
                lItemLocation.Text = CurrentCapitalRequest.ItemLocation;
            }

            if ( CurrentCapitalRequest.RequestedOn > DateTime.MinValue )
            {
                lDateRequested.Text = CurrentCapitalRequest.RequestedOn.ToShortDateString();
            }

            if ( CurrentCapitalRequest.AnticipatedInserviceDate > DateTime.MinValue )
            {
                lInServiceDate.Text = CurrentCapitalRequest.AnticipatedInserviceDate.ToShortDateString();
            }

            if ( CurrentCapitalRequest.GLFundId > 0 && CurrentCapitalRequest.GLDepartmentId > 0 && CurrentCapitalRequest.GLAccountId > 0 )
            {
                lGLAccount.Text = string.Format( "{0}-{1}-{2}", CurrentCapitalRequest.GLFundId, CurrentCapitalRequest.GLDepartmentId, CurrentCapitalRequest.GLAccountId );
            }
        }

        private void PromptUserForReturnReason( int approvalId )
        {
            txtReturnReason.Text = null;
            hfApprovalId.Value = approvalId.ToString();
            mpReturnToRequesterReason.Show();
        }

        private void RemoveApprovalRequest( int approvalId )
        {
            CurrentCapitalRequest.RemoveApprovalRequest( approvalId, CurrentUser.UserName );
            LoadApprovalRequests();
            ucStaffSearchRequester.Visible = false;
        }

        private void RemoveBid( int bidId )
        {
            CurrentCapitalRequest.RemoveBid( bidId, CurrentUser.UserName );
            LoadBids();
        }

        private void ReopenRequest()
        {
            CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_OPEN_GUID, CurrentUser.UserName );
            LoadRequest();
        }

        private void RequestApproval()
        {
            int notCompletedMinistryApprovalCount = CurrentCapitalRequest.ApprovalRequests
                                            .Where( a => a.Active )
                                            .Where( a => a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() )
                                            .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID())
                                            .Count();

            int notApprovedFinanceApprovalCount = CurrentCapitalRequest.ApprovalRequests
                                                .Where( a => a.Active )
                                                .Where( a => a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() )
                                                .Where( a => a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() )
                                                .Count();

            bool requestSent = false;
            if ( notCompletedMinistryApprovalCount > 0 )
            {
                requestSent =  RequestMinistryApproval();
            } else { 
                requestSent = RequestFinanceApproval();
            }

            if ( requestSent )
            {
                LoadRequest();
            }
            

        }

        private bool RequestFinanceApproval()
        {
            bool requestSent = true;
            try
            {
                CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_PENDING_FINANCE_APPROVAL_GUID, CurrentUser.UserName );
                CurrentCapitalRequest.RequestFinanceApproval(FinanceApprovalNotificationTemplateSetting, FinanceApproverGroup.Id, CurrentUser.UserName, GetCERLink());
                
            }
            catch ( RequisitionException rEx )
            {
                if ( CurrentCapitalRequest.StatusLUID == definedValueService.GetByGuid(new Guid( CapitalRequest.LOOKUP_STATUS_PENDING_FINANCE_APPROVAL_GUID )).Id)
                {
                    CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID, CurrentUser.UserName );
                }

                if ( rEx.InnerException == null || rEx.InnerException.GetType() != typeof( RequisitionNotValidException ) )
                {
                    throw rEx;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append( "An error occurred while saving the capital request." );
                sb.Append( "<ul type=\"disc\">" );

                foreach ( var item in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties )
                {
                    sb.AppendFormat( "<li>{0}-{1}</li>", item.Key, item.Value );
                }

                sb.Append( "</ul>" );

                SetSummaryError( sb.ToString() );
                requestSent = false;
            }
            return requestSent;
            
        }

        private bool RequestMinistryApproval()
        {
            bool requestSent = true;

            try
            {
                CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID, CurrentUser.UserName );
                CurrentCapitalRequest.RequestApproval( MinistryApprovalNotificationTemplateSetting, CurrentUser.UserName, GetCERLink() );
            }
            catch ( RequisitionException rEx )
            {
                if ( CurrentCapitalRequest.StatusLUID == 
                definedValueService.GetByGuid(new Guid( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID )).Id)
                {
                    CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_NEW_GUID, CurrentUser.UserName );
                }

                if ( rEx.InnerException == null || rEx.InnerException.GetType() != typeof( RequisitionNotValidException ) )
                {
                    throw rEx;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append( "An error occurred while saving the capital request." );
                sb.Append( "<ul type=\"disc\">" );

                foreach ( var item in ( (RequisitionNotValidException)rEx.InnerException ).InvalidProperties )
                {
                    sb.AppendFormat( "<li>{0}-{1}</li>", item.Key, item.Value );
                }

                sb.Append( "</ul>" );

                SetSummaryError( sb.ToString() );
                requestSent = false;
            }

            return requestSent;
        }

        private void ResetAddRequisitionModal()
        {
            SetAddRequisitionErrorMessage( null );
            txtRequisitionAddTitle.Text = null;
            spRequisitionRequester.StaffPersonAliasId = CurrentPerson.PrimaryAliasId;
            txtRequisitionAddDeliverTo.Text = null;
            cbRequisitionAddOpenRequisition.Checked = false;
        }

        private void ResetBidDetailFields()
        {
            SetBidErrorMessage( null );
            hfBidId.Value = "0";
            ddlVendorList.SelectedIndex = 0;
            pnlVendorName.Visible = false;
            txtVendorName.Text = null;
            txtVendorContactName.Text = null;
            //txtVendorContactPhone.PhoneNumber = null;
            //txtVendorContactPhone.Extension = null;
            txtVendorContactEmail.Text = null;
            txtQuotedPrice.Text = null;
            cbPreferredBid.Checked = false;

            docQuote.BinaryFileId = 0;

        }

        private void ResetSummaryEditFields()
        {
            txtProjectName.Text = null;
            txtDescription.Text = null;
            txtProjectCost.Text = null;
            txtInitialCost.Text = null;
            txtOngoingCost.Text = null;
            SetRequesterEdit( 0 );            

            int currentUsersMinistry = GetCurrentUsersMinistryArea();
            hfRequestingMinistry.Value = currentUsersMinistry.ToString();
            ddlRequestingMinistry.SelectedValue = currentUsersMinistry.ToString();

            if ( LocationLookupTypeSetting.HasValue )
            {
                pnlSCCLocationEdit.Visible = true;
                ddlSCCLocation.SelectedValue = GetCurrentUsersLocation().ToString();
            }


            lStatusEdit.Text = definedValueService.GetByDefinedTypeGuid(new Guid(CapitalRequest.STATUS_LOOKUP_TYPE_GUID)).OrderBy(l => l.Order).FirstOrDefault().Value;
            lDateRequestedEdit.Text = DateTime.Now.ToShortDateString();
            hfDateRequestedEdit.Value = DateTime.Now.ToShortDateString();
            txtItemLocation.Text = null;
            txtInServiceDate.Text = null;
            txtGLAccount.Text = null;
        }

        private void ResetSummaryViewFields()
        {
            lProjectName.Text = "&nbsp;";
            lProjectDescription.Text = "&nbsp;";
            lPurchaseCost.Text = "&nbsp;";
            lOtherInitialCost.Text = "&nbsp;";
            lOngoingCosts.Text = "&nbsp;";
            lRequester.Text = CurrentPerson.FullName;
            if (GetCurrentUsersMinistryArea() > 0) {
                lRequestingMinistry.Text = definedValueService.Get(GetCurrentUsersMinistryArea()).Value;
            }

            if ( LocationLookupTypeSetting.HasValue )
            {
                if (GetCurrentUsersLocation() > 0)
                {
                    pnlSCCLocationView.Visible = true;
                    lSCCLocation.Text = definedValueService.Get(GetCurrentUsersLocation()).Value;
                }
            }
            else
            {
                pnlSCCLocationView.Visible = false;
                lSCCLocation.Text = "&nbsp;";
            }

            
            lStatus.Text = definedValueService.GetByDefinedTypeGuid( new Guid( CapitalRequest.STATUS_LOOKUP_TYPE_GUID ) ).OrderBy( l => l.Order ).FirstOrDefault().Value;
            lDateRequested.Text = DateTime.Now.ToShortDateString();
            lItemLocation.Text = "&nbsp;";
            lInServiceDate.Text = "&nbsp;";
            lGLAccount.Text = "(not selected)";
        }

        private void ReturnRequest( int approvalId )
        {
            PromptUserForReturnReason( approvalId );
        }

        private void ReturnToRequesterStepTwo( int approvalId, string returnReason )
        {
            var approval = CurrentCapitalRequest.ApprovalRequests.Where( a => a.ApprovalID == approvalId ).FirstOrDefault();

            if ( approval == null || approval.ApprovalID <= 0 )
            {
                return;
            }

            mpReturnToRequesterReason.Hide();

            approval.ApprovalStatusLUID = Approval.NotApprovedStatusLUID();

            if ( approval.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() )
            {
                approval.ApproverID = CurrentPerson.PrimaryAliasId.Value;
            }

            approval.Save( CurrentUser.UserName );
            approval.AddNote( returnReason, CurrentUser.UserName );

            CurrentCapitalRequest.ChangeStatus( CapitalRequest.LOOKUP_STATUS_RETURNED_TO_REQUESTER_GUID, CurrentUser.UserName );
            CurrentCapitalRequest.RefreshApprovalRequests();
            
            CurrentCapitalRequest.SendReturnToRequesterNotification( RequestReturnedNotificationTemplateSetting, approval.ApprovalID, GetCERLink() );
            LoadRequest();
        }

        private void ReturnToList()
        {
            NavigateToPage(ListPageSetting, null);
        }

        private bool SaveBid()
        {
            try
            {
                SetBidErrorMessage( null );
                int bidId = 0;

                if ( !int.TryParse( hfBidId.Value, out bidId ) )
                {
                    throw new RequisitionException( "Bid Id is not valid." );
                }

                CapitalRequestBid bid = null;
                if ( bidId > 0 )
                {
                    bid = CurrentCapitalRequest.Bids.FirstOrDefault( b => b.BidID == bidId );
                }
                else
                {
                    bid = new CapitalRequestBid();
                    bid.CapitalRequestID = CurrentCapitalRequest.CapitalRequestId;
                }

                bool originalPerferredStatus = bid.IsPreferredBid;

                int selectedVendor = int.Parse( ddlVendorList.SelectedValue );

                if ( selectedVendor > 0 )
                {
                    bid.VendorID = selectedVendor;
                    bid.VendorName = null;
                }
                else
                {
                    bid.VendorID = 0;
                    bid.VendorName = txtVendorName.Text.Trim();
                }

                bid.VendorContactName = txtVendorContactName.Text.Trim();

                if (!String.IsNullOrWhiteSpace(txtVendorContactPhone.Text))
                {
                    bid.VendorContactPhone = new org.secc.Purchasing.Helpers.PhoneNumber(PhoneNumber.CleanNumber(txtVendorContactPhone.Text), txtVendorContactPhoneExtension.Text);
                }
                else
                {
                    bid.VendorContactPhone = null;
                }

                bid.VendorContactEmail = txtVendorContactEmail.Text.Trim();

                decimal quotePrice = 0;
                if ( decimal.TryParse( txtQuotedPrice.Text, out quotePrice ) || string.IsNullOrWhiteSpace( txtQuotedPrice.Text ) )
                {
                    bid.BidAmount = quotePrice;
                }
                if (docQuote.BinaryFileId.HasValue) {
                    // Set the temporary flag to 0
                    RockContext context = new RockContext();
                    BinaryFileService bfs = new BinaryFileService(context);
                    var file = bfs.Get(docQuote.BinaryFileId.Value);
                    file.IsTemporary = false;
                    context.SaveChanges();

                    bid.QuoteBlobID = docQuote.BinaryFileId.Value;

                }
                bid.Save( CurrentUser.UserName );

                if ( cbPreferredBid.Checked != originalPerferredStatus )
                {
                    CurrentCapitalRequest.SetPreferredBid( bid.BidID, CurrentUser.UserName );
                }

                return true;
            }
            catch ( RequisitionException rEx )
            {
                if ( rEx.InnerException == null || rEx.InnerException.GetType() != typeof( RequisitionNotValidException ) )
                {
                    throw rEx;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append( "An error has occurred while saving bid." );
                sb.Append( "<ul>" );

                foreach ( var item in ( (RequisitionNotValidException)rEx.InnerException ).InvalidProperties )
                {
                    sb.AppendFormat( "<li>{0} - {1}</li>", item.Key, item.Value );
                }

                sb.Append( "</ul>" );
                SetBidErrorMessage( sb.ToString() );
                return false;
            }
           
        }

        private bool SaveCapitalRequestSummary()
        {

            try
            {
                if ( !CapitalRequestHasChanged() )
                {
                    return false;
                }

                if ( CurrentCapitalRequest == null )
                {
                    CurrentCapitalRequest = new CapitalRequest();
                }

                CurrentCapitalRequest.ProjectName = txtProjectName.Text.Trim();
                CurrentCapitalRequest.ProjectDescription = txtDescription.Text.Trim();

                decimal projectCost = 0;
                if ( String.IsNullOrWhiteSpace(txtProjectCost.Text) || decimal.TryParse( txtProjectCost.Text.Trim(), out projectCost ) )
                {
                    CurrentCapitalRequest.PurchaseCost = projectCost;
                }

                decimal installCost = 0;
                if ( String.IsNullOrWhiteSpace(txtInitialCost.Text) ||  decimal.TryParse( txtInitialCost.Text.Trim(), out installCost ) )
                {
                    CurrentCapitalRequest.InstallationCost = installCost;
                }
                

                decimal ongoingCost = 0;
                if ( String.IsNullOrWhiteSpace(txtOngoingCost.Text) ||  decimal.TryParse( txtOngoingCost.Text.Trim(), out ongoingCost ) )
                {
                    CurrentCapitalRequest.OngoingMaintenanceCost = ongoingCost;
                }

                if (prsnRequester.StaffPersonAliasId.HasValue) CurrentCapitalRequest.RequesterId = prsnRequester.StaffPersonAliasId.Value;

                int requestingMinistry = 0;
                if ( int.TryParse( hfRequestingMinistry.Value.Trim(), out requestingMinistry ) && requestingMinistry > 0 )
                {
                    CurrentCapitalRequest.MinistryLUID = requestingMinistry;
                }

                int locationLUID = 0;
                if ( LocationLookupTypeSetting.HasValue && int.TryParse( ddlSCCLocation.SelectedValue, out locationLUID ) )
                {
                    CurrentCapitalRequest.LocationLUID = locationLUID;
                }

                DateTime dateRequested = DateTime.MinValue;
                if ( String.IsNullOrWhiteSpace(hfDateRequestedEdit.Value) || ( DateTime.TryParse( hfDateRequestedEdit.Value.Trim(), out dateRequested ) && dateRequested > DateTime.MinValue ) )
                {
                    CurrentCapitalRequest.RequestedOn = dateRequested;
                }

                DateTime inServiceDate = DateTime.MinValue;
                if ( String.IsNullOrWhiteSpace(txtInServiceDate.Text) || ( DateTime.TryParse( txtInServiceDate.Text.Trim(), out inServiceDate ) && inServiceDate > DateTime.MinValue ) )
                {
                    CurrentCapitalRequest.AnticipatedInserviceDate = inServiceDate;
                }

                CurrentCapitalRequest.ItemLocation = txtItemLocation.Text;

                if ( !String.IsNullOrWhiteSpace( txtGLAccount.Text ) )
                {
                    string[] acctParts = txtGLAccount.Text.Split( "-".ToCharArray() );

                    if ( acctParts.Length == 3 )
                    {
                        int fundNum = 0;
                        int deptNum = 0;
                        int acctNum = 0;

                        if ( int.TryParse( acctParts[0], out fundNum )
                            && int.TryParse( acctParts[1], out deptNum )
                            && int.TryParse( acctParts[2], out acctNum ) )
                        {
                            CurrentCapitalRequest.GLFundId = fundNum;
                            CurrentCapitalRequest.GLDepartmentId = deptNum;
                            CurrentCapitalRequest.GLAccountId = acctNum;
                        }
                    }
                }
                else
                {
                    CurrentCapitalRequest.GLFundId = null;
                    CurrentCapitalRequest.GLDepartmentId = null;
                    CurrentCapitalRequest.GLAccountId = null;
                }

                CurrentCapitalRequest.Save( CurrentUser.UserName );
                CERId = CurrentCapitalRequest.CapitalRequestId;
                return true;
            }
            catch ( RequisitionException rEx )
            {
                if ( rEx.InnerException != null && rEx.InnerException.GetType() == typeof( RequisitionNotValidException ) )
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append( "An error occurred while saving the capital request." );
                    sb.Append( "<ul type=\"disc\">" );
                    foreach ( var prop in ((RequisitionNotValidException)rEx.InnerException).InvalidProperties )
                    {
                        sb.AppendFormat( "<li>{0} - {1}</li>", prop.Key, prop.Value );
                    }
                    sb.Append( "</ul>" );

                    SetSummaryError( sb.ToString() );
                    return false;
                }
                else
                {
                    throw rEx;
                }
            }
 
        }

        private void SetAddRequisitionErrorMessage( string message )
        {
            lAddRequisitionError.InnerHtml = message;

            lAddRequisitionError.Visible = !String.IsNullOrWhiteSpace( message );
        }

        private void SetBidErrorMessage( string message )
        {
            lBidError.Text = message;

            lBidError.Visible = !String.IsNullOrWhiteSpace( message );
        }

        private void SetMenuItemVisibility()
        {
            lbMenuItem_AddBid.Visible = UserCanEditBids();
            bddlMenuItem_AddApproval.Visible = UserCanAddMinistryApprovalRequest();
            lbMenuItem_RequestApproval.Visible = UserCanRequestApproval();
            lbMenuItem_AddRequisition.Visible = UserCanAddRequisition();
            lbMenuItem_Cancel.Visible = UserCanCancelCER();
            lbMenuItem_Close.Visible = UserCanCloseCER();
            lbMenuItem_Reopen.Visible = UserCanReopen();
            lbMenuItem_ReturnToList.Visible = true;
        }

        private void SetPreferredBid( int bidId )
        {
            CurrentCapitalRequest.SetPreferredBid( bidId, CurrentUser.UserName );
            LoadBids();
        }

        private void SetRequestError( string message )
        {
            lRequestError.Text = message;
            pnlCERError.Visible = !String.IsNullOrWhiteSpace( message );
        }

        private void SetRequesterEdit( int requesterId )
        {

            if (requesterId > 0)
            {
                PersonAliasService personAliasService = new PersonAliasService(new Rock.Data.RockContext());
                Person requester = personAliasService.Get(requesterId).Person;
                prsnRequester.StaffPerson = requester.PrimaryAlias;
            }
            else
            {
                prsnRequester.StaffPerson = CurrentPerson.PrimaryAlias;
            }
        }


        private void SetSummaryError(string errorMessage)
        {
            lSummaryError.InnerHtml = errorMessage;
            lSummaryError.Visible = !String.IsNullOrWhiteSpace( errorMessage );
        }

        private void SetTitle()
        {
            if ( CurrentCapitalRequest == null || String.IsNullOrWhiteSpace( CurrentCapitalRequest.ProjectName ) )
            {
                lTitle.Text = DefaultTitleSetting;
            }
            else
            {
                lTitle.Text = CurrentCapitalRequest.ProjectName.Trim();
            }
        }

        private void ShowAddRequisitionModal()
        {
            ResetAddRequisitionModal();
            mpAddRequistion.Show();
        }


        private bool UserCanAddRequisition()
        {
            if ( CERId <= 0 || CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId != CERId )
            {
                return false;
            }

            if(CurrentCapitalRequest.StatusLUID != definedValueService.Get(new Guid(CapitalRequest.LOOKUP_STATUS_OPEN_GUID)).Id)
            {
                return false;
            }

            if ( !CurrentCapitalRequest.Active )
            {
                return false;
            }

            if (CurrentCapitalRequest.Requester.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            if (CurrentCapitalRequest.CreatedByPerson.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            if ( CurrentCapitalRequest.MinistryLUID == GetCurrentUsersMinistryArea() )
            {
                return true;
            }

            if ( UserCanEdit )
            {
                return true;
            }

            return false;
        }

        private bool UserCanEditBids()
        {
            if ( CERId <= 0 || CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId != CERId )
            {
                return false;
            }

            if ( !CurrentCapitalRequest.Active )
            {
                return false;
            }

            // CER is closed
            CurrentCapitalRequest.Status.LoadAttributes();
            if (CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean())
            {
                return false;
            }

            //Created by Person is current person
            if (CurrentCapitalRequest.CreatedByPerson != null && CurrentCapitalRequest.CreatedByPerson.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            //Requester is current person
            if (CurrentCapitalRequest.Requester != null && CurrentCapitalRequest.Requester.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            //CurrentPerson is in Requesting Ministry
            if ( CurrentCapitalRequest.MinistryLUID == GetCurrentUsersMinistryArea() )
            {
                return true;
            }

            //CurrentPerson is editor
            if (UserCanEdit)
            {
                return true;
            }

            return false;
        }

        private bool UserCanCancelCER()
        {
            if ( CERId <= 0 || CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId != CERId )
            {
                return false;
            }

            if ( !CurrentCapitalRequest.Active )
            {
                return false;
            }
            if ( CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean() )
            {
                return false;
            }

            int activeRequisitions = CurrentCapitalRequest.Requisitions
                                        .Where( r => r.Active )
                                        .Where( r => r.StatusLUID != Requisition.CancelledLUID() )
                                        .Count();

            if(activeRequisitions == 0)
            {
                return true;
            }

            return false;
        }

        private bool UserCanCloseCER()
        {
            if ( CERId <= 0 || CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId != CERId )
            {
                return false;
            }

            if ( !CurrentCapitalRequest.Active )
            {
                return false;
            }


            if (!UserCanEdit)
            {
                return false;
            }

            int openStatusLUID = definedValueService.Get( new Guid( CapitalRequest.LOOKUP_STATUS_OPEN_GUID ) ).Id;
            CurrentCapitalRequest.Requisitions.ForEach(r => r.Status.LoadAttributes());
            int openRequisitionCount = CurrentCapitalRequest.Requisitions
                                        .Where( r => r.Active )
                                        .Where(r => !r.Status.AttributeValues["IsClosed"].Value.AsBoolean())
                                        .Count();

            int activeRequisitionCount = CurrentCapitalRequest.Requisitions
                                            .Where( r => r.Active )
                                            .Where( r => r.StatusLUID != Requisition.CancelledLUID() )
                                            .Count();

            if ( CurrentCapitalRequest.StatusLUID == openStatusLUID && openRequisitionCount == 0 && activeRequisitionCount > 0 )
            {
                return true;
            }

            return false;
        }

        private bool UserCanEditSummary()
        {

            if ( mUserCanEdit != null )
            {
                return (bool)mUserCanEdit;
            }

            if ( CurrentCapitalRequest == null || !CurrentCapitalRequest.Active )
            {
                return false;
            }

            //User has Edit rights
            if (UserCanEdit & !CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean())
            {
                mUserCanEdit = true;
                return (bool)mUserCanEdit;
            }

            var pendingMinistryApprovalStatus = definedValueService.Get( new Guid( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID ) );

            //User is Creator and request hasn't been sent for approval and is not closed
            if (CurrentPerson.PrimaryAliasId == CurrentCapitalRequest.RequesterId || CurrentPerson.PrimaryAliasId == CurrentCapitalRequest.CreatedByPerson.PrimaryAliasId)
            {

                if (!CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean() && CurrentCapitalRequest.Status.Order < pendingMinistryApprovalStatus.Order)
                {
                    mUserCanEdit = true;
                    return (bool)mUserCanEdit;
                }
            }

            if ( CurrentCapitalRequest.MinistryLUID == GetCurrentUsersMinistryArea() )
            {
                if (!CurrentCapitalRequest.Status.AttributeValues["IsClosed"].Value.AsBoolean() && CurrentCapitalRequest.Status.Order < pendingMinistryApprovalStatus.Order)
                {
                    mUserCanEdit = true;
                    return (bool)mUserCanEdit;
                }
            }
            mUserCanEdit = false;
            return (bool)mUserCanEdit;
        }

        private bool UserCanReopen()
        {
            if ( CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId <= 0 )
            {
                return false;
            }

            DefinedValue closedStatus = definedValueService.Get( new Guid( CapitalRequest.LOOKUP_STATUS_CLOSED_GUID ) );

            if ( CurrentCapitalRequest.StatusLUID != closedStatus.Id )
            {
                return false;
            }

            if ( UserCanEdit)
            {
                return true;
            }

            return false;
        }

        private bool UserCanRequestFinanceApproval()
        {
            if ( !UserCanEditSummary() )
            {
                return false;
            }

            // Non-finance approval Requests exist
            return CurrentCapitalRequest.ApprovalRequests
                    .Where( a => a.Active )
                    .Where( a => a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() )
                    .Count() > 0
            &&
            // Non-finance approval requests are all complete
            CurrentCapitalRequest.ApprovalRequests
                    .Where( a => a.Active )
                    .Where( a => a.ApprovalTypeLUID != Approval.FinanceApprovalTypeLUID() )
                    .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() )
                    .Count() == 0
                &&
            // No finance approval requests exist
            CurrentCapitalRequest.ApprovalRequests
                    .Where( a => a.Active )
                    .Where( a => a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() )
                        .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() )
                        .Count() == 0;
        }

        private bool UserCanRequestApproval()
        {
            if ( CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId <= 0 )
            {
                return false;
            }

            if(!CurrentCapitalRequest.Active)
            {
                return false;
            }

            if ( CurrentCapitalRequest.Bids.Count < MinimumRequiredBidsSetting )
            {
                return false;
            }
            if ( CurrentCapitalRequest.ApprovalRequests
                    .Where( a => a.Active )
                    .Where( a => a.ApprovalStatusLUID == Approval.NotSubmittedStatusLUID() || a.ApprovalStatusLUID == Approval.NotApprovedStatusLUID() )
                    .Count() == 0 )
            {
                return false;
            }

            if ( UserCanEditSummary() && CurrentCapitalRequest.Status.Order < definedValueService.Get( new Guid( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID ) ).Order )
            {
                return true;
            }

            return false;
        }

        private bool UserCanView()
        {
            //new CER
            if ( CERId <= 0 || CurrentCapitalRequest == null || CurrentCapitalRequest.CapitalRequestId != CERId )
            {
                return true;
            }

            //User is Requester
            if (CurrentCapitalRequest.Requester.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            //User is Creator
            if (CurrentCapitalRequest.CreatedByPerson.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            //User is Editor (Admin)
            if ( UserCanEdit )
            {
                return true;
            }

            // User is in Requesting Ministry
            if ( CurrentCapitalRequest.MinistryLUID == GetCurrentUsersMinistryArea() )
            {
                return true;
            }

            //User is an approver
            if (CurrentCapitalRequest.ApprovalRequests.Where(a => a.Active).Where(a => a.ApproverID == CurrentPerson.PrimaryAliasId).Count() > 0)
            {
                return true;
            }

            //User is a Finance Approver and request has a Finance Approval Request
            if ( UserIsFinanceApprover() && CurrentCapitalRequest.ApprovalRequests.Where( a => a.Active ).Where( a => a.ApprovalTypeLUID == Approval.FinanceApprovalTypeLUID() ).Count() > 0 )
            {
                return true;
            }

            return false;
        }

        private bool UserCanAddMinistryApprovalRequest()
        {
            if ( CERId == 0 || CurrentCapitalRequest.CapitalRequestId <= 0 || !CurrentCapitalRequest.Active )
            {
                return false;
            }


            DefinedValue FinanceStatus = definedValueService.Get(new Guid(CapitalRequest.LOOKUP_STATUS_PENDING_FINANCE_APPROVAL_GUID));
            if ( CurrentCapitalRequest.Status.Order > FinanceStatus.Order )
            {
                return false;
            }

            if (CurrentCapitalRequest.RequesterId == CurrentPerson.PrimaryAliasId || CurrentCapitalRequest.CreatedByPerson.PrimaryAliasId == CurrentPerson.PrimaryAliasId)
            {
                return true;
            }

            if ( UserCanEdit )
            {
                return true;
            }

            bool UserIsMinistryApprover = CurrentCapitalRequest.ApprovalRequests.Where( a => a.Active )
                                            .Where( a => a.ApprovalTypeLUID == Approval.MinistryApprovalTypeLUID() )
                                            .Where( a => a.ApprovalStatusLUID == Approval.PendingApprovalStatusLUID() )
                                            .Where(a => a.Approver.PrimaryAliasId == CurrentPerson.PrimaryAliasId).Count() > 0;

            if ( UserIsMinistryApprover )
            {
                return true;
            }

            return false;
        }

        private bool UserIsFinanceApprover()
        {
            string viewStateKey = string.Format( "{0}_FinanceApprover", BlockId );

            if ( ViewState[viewStateKey] != null )
            {
                return (bool)ViewState[viewStateKey];
            }

            Boolean isApprover = FinanceApproverGroup.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active && m.Person.Id == CurrentPerson.Id ).Count() > 0;

            ViewState[viewStateKey] = isApprover;

            return isApprover;
        }


        #endregion

        protected void SelectApprover_Click( object sender, EventArgs e )
        {
            PersonAlias test = ( ( StaffPicker ) sender ).StaffPerson;

            // Set the approval type;
            int approvalTypeId = Approval.MinistryApprovalTypeLUID();
            switch ( bddlMenuItem_AddApproval.SelectedValue )
            {
                case "Finance":
                    approvalTypeId = Approval.FinanceApprovalTypeLUID();
                    break;
                case "LeadTeam":
                    approvalTypeId = Approval.LeadTeamApprovalTypeLUID();
                    break;
            }

            CurrentCapitalRequest.AddApprovalRequest( test.Id, CurrentUser.UserName, approvalTypeId );

            // If this is already in a pending status, go ahead and immediately send the approval out.
            if ( CurrentCapitalRequest.Status.Order >= definedValueService.Get( new Guid( CapitalRequest.LOOKUP_STATUS_PENDING_MINISTRY_APPROVAL_GUID ) ).Order )
            {
                RequestApproval();
            }
            LoadRequest();

            // Clear the picker back out
            ucStaffSearchRequester.StaffPerson = null;

            LoadApprovalRequests();

            ucStaffSearchRequester.Visible = false;

            upMain.Update();
        }
}
}

