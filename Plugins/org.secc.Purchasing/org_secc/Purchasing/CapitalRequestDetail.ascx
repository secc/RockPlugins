<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CapitalRequestDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.CapitalRequestDetail" %>
<%@ Register Src="~/Plugins/org_secc/Purchasing/StaffPicker.ascx" TagPrefix="secc" TagName="StaffPicker" %>

<script>
    $(document).ready(function () {

        var pgReq = Sys.WebForms.PageRequestManager.getInstance();
        pgReq.add_initializeRequest(initializeRequest);
        pgReq.add_endRequest(endRequest);

        initPlaceholderMasks();
    });

    function initializeRequest(sender, args) {
        //intentionally left empty - CSF
    }

    function endRequest(sender, args) {
        initPlaceholderMasks();
    }

    function initPlaceholderMasks() {
        //$('[id*="txtGLAccount"]').mask("999-999-99999", { placeholder: " " });
        //$('[id*="txtDateRequested"]').mask("99/99/9999", { placeholder: " " });
        //$('[id*="txtInServiceDate"]').mask("99/99/9999", { placeholder: " " });
    }

    function confirmPreferred() {
        return confirm("Are you sure that you would like to update the preferred bid?");
    }

    confirmRemoveBid = function(event) {
        return Rock.dialogs.confirmDelete(event, 'bid');
    }
</script>

<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <div class="btn-group" role="group" style="margin-bottom: 10px;">
            <asp:LinkButton ID="lbMenuItem_EditSummary" CssClass="btn btn-default" runat="server" CommandName="editsummary" OnClick="lbMenuItem_Click" Visible="false">Edit Summary</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_AddBid" CssClass="btn btn-default" runat="server" CommandName="addbid" OnClick="lbMenuItem_Click" Visible="false">Add Bid</asp:LinkButton>
            <Rock:ButtonDropDownList ID="bddlMenuItem_AddApproval" runat="server" Title="Add Approver" OnSelectionChanged="lbMenuItem_Click">
                <asp:ListItem Text="Ministry Approval" Value="Ministry"></asp:ListItem>
                <asp:ListItem Text="Lead Team Approval" Value="LeadTeam"></asp:ListItem>
            </Rock:ButtonDropDownList>
            <asp:LinkButton ID="lbMenuItem_RequestApproval" CssClass="btn btn-default" runat="server" CommandName="requestapproval" OnClick="lbMenuItem_Click" Visible="false">Request Approval</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_AddRequisition" CssClass="btn btn-default" runat="server" CommandName="addrequisition" OnClick="lbMenuItem_Click" Visible="false">Add Requisition</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_Close" CssClass="btn btn-default" runat="server" CommandName="close" OnClick="lbMenuItem_Click" Visible="false">Close</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_Cancel" CssClass="btn btn-default" runat="server" CommandName="cancel" OnClick="lbMenuItem_Click" Visible="false">Cancel CER</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_Reopen" CssClass="btn btn-default" runat="server" CommandName="reopen" OnClick="lbMenuItem_Click" Visible="false">Reopen</asp:LinkButton>
            <asp:LinkButton ID="lbMenuItem_ReturnToList" CssClass="btn btn-default" runat="server" CommandName="returnToList" OnClick="lbMenuItem_Click" Visible="false">Return to List</asp:LinkButton>
        </div>
        <div id="pnlMain" class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lTitle" runat="server" />
                </h1>
            </div>
            <div class="panel-body">
                        
            <div class="alert alert-danger" runat="server" id="lSummaryError" visible="false">
            </div>    
            <asp:Panel ID="pnlCERError" runat="server" CssClass="alert alert-danger" Visible="false">
                <asp:Literal ID="lRequestError" runat="server" />
            </asp:Panel>


            <asp:Panel ID="pnlSummary" runat="server" Visible="false" ClientIDMode="Static">
                
                <asp:Panel ID="pnlSummaryView" runat="server" Visible="false" CssClass="panel-body">
                    <div class="row">
                        <div class="col-sm-6">
                            <div class="form-group">
                                <label>
                                    Project Name
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lProjectName" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Description
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lProjectDescription" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Purchase Cost
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lPurchaseCost" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Other Initial Cost
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lOtherInitialCost" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Ongoing Costs
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lOngoingCosts" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Requester
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lRequester" runat="server" />
                                </div>
                            </div>
                        </div>

                        <div class="col-sm-6">

                            <div class="form-group">
                                <label>
                                    Requesting Ministry
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lRequestingMinistry" runat="server" />
                                </div>
                            </div>
                            <asp:Panel ID="pnlSCCLocationView" runat="server" Visible="false" CssClass="form-group">
                                <label>
                                    Southeast Location
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lSCCLocation" runat="server" />
                                </div>
                            </asp:Panel>
                            <div class="form-group">
                                <label>
                                    Status
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lStatus" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Date Requested
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lDateRequested" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Item Location
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lItemLocation" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Anticipated In-Service Date
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lInServiceDate" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    General Ledger Account
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lGLAccount" runat="server" />
                                </div>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlSummaryEdit" runat="server" Visible="false">
                    <div class="row">
                        <div class="col-sm-6">
                            <div class="form-group required">
                                <label class="control-label">
                                    Project Name
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtProjectName" runat="server"  class="form-control"/>
                                </div>
                            </div>
                            <div class="form-group required">
                                <label class="control-label">
                                    Description
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group required">
                                <label class="control-label">
                                    Project Cost
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtProjectCost" runat="server" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Other Initial Cost
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtInitialCost" runat="server" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Ongoing Cost
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtOngoingCost" runat="server" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group required">
                                <label class="control-label">
                                    Requester
                                </label>
                                <div class="formItem">
                                    <secc:StaffPicker ID="prsnRequester" runat="server" AllowMultipleSelections="false" UserCanEdit="true"/>
                                </div>
                            </div>
                        </div>
                        <div class="col-sm-6">
                            <div class="form-group required" id="divRequestingMinistry" runat="server">
                                <label class="control-label">
                                    Requesting Ministry
                                </label>
                                <div class="formItem">
                                    <asp:HiddenField ID="hfRequestingMinistry" runat="server" />
                                    <asp:Literal ID="lRequestingMinistryEdit" runat="server" Visible="false" />
                                    <asp:DropDownList ID="ddlRequestingMinistry" runat="server" AutoPostBack="true" Visible="false" OnSelectedIndexChanged="ddlRequestingMinistry_SelectedIndexChanged" class="form-control" />
                                </div>
                            </div>
                            <asp:Panel ID="pnlSCCLocationEdit" runat="server" CssClass="form-group" Visible="false">
                                <label>
                                    SCC Location
                                </label>
                                <div class="formItem">
                                    <asp:DropDownList ID="ddlSCCLocation" runat="server" class="form-control" />
                                </div>
                            </asp:Panel>
                            <div class="form-group">
                                <label>
                                    Status
                                </label>
                                <div class="formItem">
                                    <asp:Literal ID="lStatusEdit" runat="server" />
                                </div>
                            </div>

                            <div class="form-group">
                                <label>
                                    Date Requested
                                </label>
                                <div class="formItem">
                                    <asp:HiddenField ID="hfDateRequestedEdit" runat="server" />
                                    <asp:Literal ID="lDateRequestedEdit" runat="server" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Item Location
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtItemLocation" runat="server" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>
                                    Anticipated In-Service Date
                                    <span class="instructions">(mm/dd/yyyy)</span>
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtInServiceDate" runat="server" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group required">
                                <label class="control-label">
                                    General Ledger Account
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtGLAccount" runat="server" class="form-control" />
                                </div>
                            </div>
                        </div>
                        <div class="pull-right" style="margin-right: 20px">
                            <asp:Button ID="btnSummarySave" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="btnSummarySave_Click" />
                            <asp:Button ID="btnSummaryCancel" runat="server" CssClass="btn btn-default" Text="Cancel" OnClick="btnSummaryCancel_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </asp:Panel>
            <asp:Panel ID="pnlBids" runat="server" Visible="false" ClientIDMode="Static" CssClass="summaryPanels">
                <div style="padding-bottom: 10px;">
                    <h3>Bids</h3>
                </div>

                <Rock:Grid ID="grdBids" runat="server" OnReBind="grdBids_ReBind" OnRowCommand="grdBids_RowCommand" OnRowDataBound="grdBids_RowDataBound" DataKeyField="BidId">
                    <Columns>
                        <Rock:RockBoundField DataField="BidId" Visible="false" />
                        <Rock:RockBoundField DataField="VendorName" SortExpression="VendorName" HeaderText="Vendor" />
                        <Rock:RockBoundField DataField="VendorContact" HeaderText="Contact" HtmlEncode="false" />
                        <Rock:RockBoundField DataField="BidAmount" DataFormatString="{0:c}" SortExpression="BidAmount" HeaderText="Amount" />
                        <asp:HyperLinkField DataTextField="QuoteTitle" HeaderText="Quote" DataNavigateUrlFields="QuoteGuid" DataNavigateUrlFormatString="/GetFile.ashx?guid={0}" Target="_blank" />
                        <Rock:RockTemplateField HeaderText="Is Preferred" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center">
                            <ItemTemplate>
                                <asp:LinkButton ID="lbSetPreferred" runat="server" OnClientClick="return confirmPreferred()" CommandName="setpreferred" Style="text-decoration: none; padding-left:5px;" Visible="false">
                                    <i ID="imgPreferredSelection" runat="server"></i>
                                </asp:LinkButton>
                                <i ID="imgIsPreferred" runat="server" Visible="false" class="fa fa-check-square-o"></i>
                            </ItemTemplate>
                           
                        </Rock:RockTemplateField>
                        <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-VerticalAlign="Top" HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="lbEdit" runat="server" Visible="false" CommandName="editbid" CssClass="btn btn-default" ToolTip="Edit">
                                    <i class="fa fa-pencil"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbRemove" runat="server" Visible="false" CommandName="remove" OnClientClick="return confirmRemoveBid(event);" CssClass="btn btn-default" ToolTip="Remove">
                                    <i class="fa fa-remove"></i>
                                </asp:LinkButton>
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                    </Columns>
                </Rock:Grid>
            </asp:Panel>
            <asp:Panel ID="pnlApproval" runat="server" Visible="false" ClientIDMode="Static" CssClass="summaryPanels">
                <div style="padding-bottom: 10px;">
                    <h3>Approval Requests</h3>
                </div>
                <Rock:Grid ID="grdApprovalRequests" runat="server" OnReBind="grdApprovalRequests_ReBind" OnRowCommand="grdApprovalRequests_RowCommand" OnRowDataBound="grdApprovalRequests_RowDataBound" DataKeyField="ApprovalId">
                    <Columns>
                        <Rock:RockBoundField DataField="ApprovalId" Visible="false" />
                        <Rock:RockBoundField DataField="ApprovalTypeName" SortExpression="ApprovalTypeName" HeaderText="Type" />
                        <Rock:RockBoundField DataField="ApproverFullName" SortExpression="ApproverFullName" HeaderText="Approver" />
                        <Rock:RockBoundField DataField="ApprovalStatusName" HeaderText="Status" />
                        <Rock:RockBoundField DataField="DateApprovedString" HeaderText="Date Approved" />
                        <Rock:RockBoundField DataField="LastComment" HeaderText="Note" ItemStyle-Width="15%" />
                        <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" HeaderText="Actions">
                            <ItemTemplate>
                                <asp:LinkButton ID="lbApprove" runat="server" CommandName="Approve" Visible="false" Title="Approve" CssClass="btn btn-default">
                                    <i class="fa fa-check"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbReturn" runat="server" CommandName="Return" Visible="false" Title="Return" CssClass="btn btn-default">
                                    <i class="fa fa-undo"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbRemove" runat="server" CommandName="remove" Visible="false" Title="Remove" CssClass="btn btn-default" >
                                    <i class="fa fa-remove"></i>
                                </asp:LinkButton>
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                    </Columns>
                </Rock:Grid>
            </asp:Panel>
            <asp:Panel ID="pnlRequisition" runat="server" Visible="false" ClientIDMode="Static" CssClass="summaryPanels">
                <div style="padding-bottom: 10px;">
                    <h3>Requisitions</h3>
                </div>
                <Rock:Grid ID="grdRequisitions" runat="server" OnReBind="grdRequisitions_ReBind" OnItemCommand="grdRequisitions_ItemCommand" OnItemDataBound="grdRequisitions_ItemDataBound" DataKeyField="RequisitionID">
                    <Columns>
                        <Rock:RockBoundField DataField="RequisitionId" Visible="false" />
                        
                        <Rock:RockTemplateField HeaderText="Title">                     
                            <ItemTemplate>
                                <asp:HyperLink runat="server" text='<%# Eval("Title") %>' NavigateUrl='<%# String.Format("{0}?RequisitionID={1}", RequisitionDetailPageSetting, Eval("RequisitionID")) %>'></asp:HyperLink> 
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                        <Rock:RockBoundField DataField="RequesterName" HeaderText="Requester" SortExpression="RequesterLastFirst" ItemStyle-Width="15%" />
                        <Rock:RockBoundField DataField="Status" HeaderText="Status" SortExpression="Status" ItemStyle-Width="15%" />
                        <Rock:RockBoundField DataField="CurrentChargeTotal" HeaderText="Current Charges" DataFormatString="{0:c}" ItemStyle-Width="15%" />
                        <Rock:BoolField HeaderText="Approved" HeaderStyle-HorizontalAlign="Center"
                            ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" DataField="IsApproved"
                            SortExpression="IsApproved" ItemStyle-Width="5%" />
                        <Rock:BoolField HeaderText="Accepted" HeaderStyle-HorizontalAlign="Center"
                            ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" DataField="IsAccepted"
                            SortExpression="IsAccepted" ItemStyle-Width="5%" />
                    </Columns>
                </Rock:Grid>
            </asp:Panel>
            </div>
        </div>
        <Rock:ModalDialog ID="mpBidDetail" runat="server" SaveButtonText="Save" OnSaveClick="btnBidSave_Click">
            <Content>
                <asp:UpdatePanel ID="upBidDetail" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="pnlBidDetail">
                            <asp:HiddenField ID="hfBidId" runat="server" />
                            <h3>Bid Detail</h3>
                            <div class="smallText" style="padding-top: 5px; padding-bottom: 5px; color: red;">
                                <asp:Literal ID="lBidError" runat="server" Visible="false" />
                            </div>
                            <div class="row">
                                <div class="col-sm-6">
                                    <div class="form-group required">
                                        <label class="control-label">
                                            Vendor
                                        </label>
                                        <div class="formItem">
                                            <asp:DropDownList CssClass="form-control" ID="ddlVendorList" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlVendorList_SelectedIndexChanged" />
                                        </div>
                                    </div>
                                    <asp:Panel ID="pnlVendorName" runat="server" CssClass="form-group required" Visible="false">
                                        <label class="control-label">
                                            Vendor Name
                                        </label>
                                        <div class="formItem">
                                            <asp:TextBox CssClass="form-control" ID="txtVendorName" runat="server" />
                                        </div>
                                    </asp:Panel>
                                    <div class="form-group">
                                        <label>
                                            Contact Name
                                        </label>
                                        <div class="formItem">
                                            <asp:TextBox CssClass="form-control" ID="txtVendorContactName" runat="server" />
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label>
                                            Contact Phone
                                        </label>
                                        <div class="form-inline">
                                            <Rock:PhoneNumberBox ID="txtVendorContactPhone" runat="server" Required="false" /> 
                                            <label>
                                                Ext.
                                            </label>
                                            <asp:TextBox CssClass="form-control" ID="txtVendorContactPhoneExtension" runat="server" />
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label>
                                            Contact Email
                                        </label>
                                        <div class="formItem">
                                            <asp:TextBox CssClass="form-control" ID="txtVendorContactEmail" runat="server" />
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-6">
                                    <div class="form-group required">
                                        <label class="control-label">
                                            Quoted Price
                                        </label>
                                        <div class="formItem">
                                            <asp:TextBox CssClass="form-control" ID="txtQuotedPrice" runat="server" />
                                        </div>
                                    </div>
                                    <div class="form-group required">
                                        <label class="control-label">
                                            Quote
                                        </label>
                                        <div class="formItem">
                                            <Rock:FileUploader ID="docQuote" runat="server" />
                                        </div>
                                    </div>
                                    <div class="form-inline">
                                        <asp:CheckBox ID="cbPreferredBid" runat="server" />
                                        <label>Is Preferred Bid</label>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mpReturnToRequesterReason" runat="server" SaveButtonText="Return" OnSaveClick="btnReturnToRequesterSave_Click">
            <Content>
                <asp:UpdatePanel ID="upReturnToRequesterReason" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="pnlReturnToRequesterReason">
                            <h3>Return to Requester</h3>
                            <asp:HiddenField ID="hfApprovalId" runat="server" />
                            <div class="field">
                                <label>
                                    Return Reason
                                </label>
                                <div class="formItem">
                                    <asp:TextBox ID="txtReturnReason" runat="server" TextMode="MultiLine" CssClass="form-control" />
                                </div>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpAddRequistion" runat="server" Title="Add Requisition" OnSaveClick="btnRequisitionAddSave_Click">
            <Content>
                <div id="pnlAddRequisition">
                    <div class="alert alert-danger" ID="lAddRequisitionError" runat="server" Visible="false" />
                    <Rock:RockTextBox Label="Requisition Title:" ID="txtRequisitionAddTitle" runat="server" CssClass="form-control" ValidationGroup="AddReq" Required="true" />
                    <div class="form-group">
                        <label>
                            Requester
                        </label>
                        <div class="formItem">
                            <secc:StaffPicker ID="spRequisitionRequester" runat="server" AllowMultipleSelections="false" UserCanEdit="true"/>
                        </div>
                    </div>
                    <Rock:RockTextBox Label="Deliver To:" ID="txtRequisitionAddDeliverTo" runat="server" CssClass="form-control" ValidationGroup="AddReq" Required="true" />

                    <div class="field">
                        <div class="formItem">
                            <asp:CheckBox ID="cbRequisitionAddOpenRequisition" runat="server" Text="Open Requisition in New Window" TextAlign="Right" />
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpCancelRequest" runat="server">
            <Content>
                <asp:UpdatePanel ID="upCancelRequest" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="pnlCancelRequest">
                            <h3>Confirm Cancellation</h3>
                            <div class="smallText" style="text-align: center;">
                                Are you sure that you would like to cancel this request?
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="btnCancelRequestConfirm" />
                        <asp:AsyncPostBackTrigger ControlID="btnCancelRequestCancel" />
                    </Triggers>
                </asp:UpdatePanel>
                <div style="text-align: center;">
                    <asp:Button ID="btnCancelRequestConfirm" runat="server" CssClass="button" Text="Yes" OnClick="btnCancelRequestConfirm_Click" />
                    <asp:Button ID="btnCancelRequestCancel" runat="server" CssClass="button" Text="No" OnClick="btnCancelRequestCancel_Click" />
                </div>
            </Content>
        </Rock:ModalDialog>
        <secc:StaffPicker ID="ucStaffSearchRequester" runat="server" AllowMultipleSelections="false" OnSelect="SelectApprover_Click"/>

    </ContentTemplate>
    <Triggers>
        <%-- <asp:AsyncPostBackTrigger ControlID="btnBidSave" />
        <asp:AsyncPostBackTrigger ControlID="btnBidClose" />
        <asp:AsyncPostBackTrigger ControlID="btnReturnToRequesterSave" />
        <asp:AsyncPostBackTrigger ControlID="btnReturnToRequesterCancel" />
        <asp:AsyncPostBackTrigger ControlID="btnRequisitionAddSave" />
        <asp:AsyncPostBackTrigger ControlID="btnRequisitionAddCancel" />
        <asp:AsyncPostBackTrigger ControlID="btnCancelRequestConfirm" />
        <asp:AsyncPostBackTrigger ControlID="btnCancelRequestCancel" />--%>

    </Triggers>
</asp:UpdatePanel>