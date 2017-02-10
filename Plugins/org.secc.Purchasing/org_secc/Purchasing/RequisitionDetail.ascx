<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RequisitionDetail.ascx.cs"
    Inherits="RockWeb.Plugins.org_secc.Purchasing.RequisitionDetail" %>
<%@ Register Src="VendorSelect.ascx" TagName="VendorSelect" TagPrefix="secc" %>
<%@ Register Src="Notes.ascx" TagName="Notes" TagPrefix="secc" %>
<%@ Register Src="Attachments.ascx" TagName="Attachments" TagPrefix="secc" %>
<%@ Register Src="StaffPicker.ascx" TagName="StaffPicker" TagPrefix="secc" %>

<script type="text/javascript">
  
    function dateNeededChanged() {
        var selectedDateText = $("[id*=txtDateNeeded]").val();
        if (selectedDateText == "") {
            return;
        }
        var selectedDate = Date.parse(selectedDateText);

        if (isNaN(selectedDate)) {
            $("[id*=txtDateNeeded]").val("");
            alert("Date Needed is not valid. Please retry.");
            return;
        }

        var expeditedShippingWindowDays = parseInt( $( "[id*=hfExpeditedShippingDays]" ).val() );

        if (getMaxExpeditedShippingDate(expeditedShippingWindowDays) >= selectedDate) {
            showAllowExpedited(true);
        }
        else {
            showAllowExpedited(false);
        }

    }

    function showAllowExpedited(isVisibile)
    {
        if(isVisibile)
        {
            $("#ItemAllowExpedited").css("display", "inline");
        }
        else
        {
             $("#ItemAllowExpedited").css("display", "none");
        }
    }

    function getMaxExpeditedShippingDate(daysInWindow) {
        var expeditedDate = getTodayAtMidnight();
        expeditedDate.setDate(expeditedDate.getDate() + daysInWindow);

        return expeditedDate;
    }

    function getTodayAtMidnight() {
        var today = new Date(Date.now());
        var year = today.getFullYear();
        var month = today.getMonth();
        var date = today.getDate();

        return new Date(year, month, date);
    }

    function autoAdvanceCheck(e, charCount, currentControlID, nextControlID)
    {
        var keyPressed = e.keyCode || e.which;
        
        if(keyPressed == 9 || keyPressed == 16)
        {
            return;
        }

       var txtValue = $("[id*=" + currentControlID + "]").val();
       
       if(txtValue.length == charCount)
       {
            $("[id*=" + nextControlID + "]").focus();
       } 
    }

    function setItemDetailEditability(isReadOnly)
    {
        alert("yes");
        var className = "readOnly";
        if(isReadOnly)
        {
            $("#itemDetails").find("input[type=text]").addClass(className);
        }
        else
        {
            $("#itemDetails").find("input[type=text]").addClass(className);
        }

    }

    function checkAllItems() {
        var isChecked = false;
        if ($("[id*=chkItemDetailsAll]").attr("checked")) {
            isChecked = true;
        }
        var table = $("[id*=dgItems]");
        if (isChecked == true) {
            table.find("[id*=chkItemDetail]").attr("checked", "true");
        }
        else {
            table.find("[id*=chkItemDetail]").removeAttr("checked");
        }
    }

    function returnToRequesterCallback(noteID) {
        $("[id*=hfReturnToSenderNoteID]").val(noteID);
        $("[id*=btnReturnToRequesterPart2]").click();
    }
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () { $('body').removeClass('modal-open'); });
</script>

<style>
.nothing {
    border: 0;
    outline: none;
    background: transparent !important;
    box-shadow: none;
    padding: 0px;
    height: auto;
}
.table > tfoot > tr > td {
    background-color: #edeae6;
    color: #6a6a6a;
    font-weight: 600;
    border-color: #d8d1c8;
}
.form-group
{
    height: 65px;
}
@media print {
  .form-group {
      height: auto;
      font-size: 10px;
      height: 30px;
  }
  .form-control
  {
    border: 0;
    padding:0;
    overflow:visible;
    height: 20px;
    font-size: 10px;
  }
  h3 {
      font-weight: bolder;
      margin-top: 5px;
      font-size: 14px;
  }
  body {
      margin-left: 0mm; 
      margin-right: 0mm;
      width: 100%;
  }
  #page-wrapper {
      width: auto;
      left: 0px;
      margin: 0px;
      padding: 0px;
  }
  #content-wrapper {
      margin: -15px;
      width: 100%;
  }
  body,html {
      margin: 0px;
      padding: 0px;
  }
  #page-title {
      padding: 5px !important;
      margin-top: 15px !important;
  }
  #page-content {
      padding: 5px !important;
  }
}
@media print and (-moz-images-in-menus:0) {
  #content-wrapper {
      margin: 0px !important;
      border: 5px solid #FFF;
  }
}
@media print and (-ms-high-contrast: none), (-ms-high-contrast: active) {
  #content-wrapper {
      left: -70px;
  }
  .table>thead>tr>th
  {
      border: 2px solid #000 !important;
      border-collapse: collapse;
  }
  .table-bordered {
      border: 2px solid #000 !important;
      border-collapse: collapse;

  }
  .table-bordered>thead>tr>th, .table-bordered>tbody>tr>th, .table-bordered>tfoot>tr>th, .table-bordered>thead>tr>td, .table-bordered>tbody>tr>td, .table-bordered>tfoot>tr>td {
    border: 2px solid #000 !important;
    border-collapse: collapse;
  }
}
</style>

<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        
        <asp:HiddenField ID="hfReturnToSenderNoteID" runat="server" />
        <asp:Button ID="btnReturnToRequesterPart2" runat="server" OnClick="btnReturnToRequesterPart2_Click" style="display:none; visibility:hidden;" />
        <asp:HiddenField ID="hfExpeditedShippingDays" runat="server" />
        <input type="hidden" id="ihPersonList" runat="server" name="ihPersonList" />
        <asp:Button ID="btnRefresh" runat="server" OnClick="btnRefresh_Click" Style="visibility: hidden;
            display: none" Text="Refresh" />
        <div class="btn-group hidden-print" role="group" style="margin-bottom: 10px;">
            <asp:LinkButton ID="lbSave" CssClass="btn btn-default" runat="server" CommandName="Save" OnClick="ToolbarItem_click" Visible="false">Save</asp:LinkButton>
            <asp:LinkButton ID="lbAddItem" CssClass="btn btn-default" runat="server" CommandName="AddItem" OnClick="ToolbarItem_click" Visible="false">Add Item</asp:LinkButton>
            <asp:LinkButton ID="lbAddItemToPO" CssClass="btn btn-default" runat="server" CommandName="AddItemToPO" OnClick="ToolbarItem_click" Visible="false">Add Items to PO</asp:LinkButton>
            <asp:LinkButton ID="lbAddNote" CssClass="btn btn-default" runat="server" CommandName="AddNote" OnClick="ToolbarItem_click" Visible="false">Add Note</asp:LinkButton>
            <asp:LinkButton ID="lbAddAttachment" CssClass="btn btn-default" runat="server" CommandName="AddAttachment" OnClick="ToolbarItem_click" Visible="false">Add Attachment</asp:LinkButton>
            <asp:LinkButton ID="lbRequestApproval" CssClass="btn btn-default" runat="server" CommandName="requestapproval" OnClick="ToolbarItem_click" Visible="false">Select Approver</asp:LinkButton>
            <asp:LinkButton ID="lbSubmitToPurchasing" CssClass="btn btn-default" runat="server" CommandName="submitToPurchasing" OnClick="ToolbarItem_click" Visible="false">Submit to Purchasing</asp:LinkButton>
            <asp:LinkButton ID="lbAcceptRequisition" CssClass="btn btn-default" runat="server" CommandName="acceptrequisition" OnClick="ToolbarItem_click" Visible="false">Accept Requisition</asp:LinkButton>
            <asp:LinkButton ID="lbReturnToRequester" CssClass="btn btn-default" runat="server" CommandName="returntorequester" OnClick="ToolbarItem_click" Visible="false">Return to Requester</asp:LinkButton>
            <asp:LinkButton ID="lbCancel" CssClass="btn btn-default" runat="server" CommandName="cancelrequisition" OnClick="ToolbarItem_click" Visible="false">Cancel Requisition</asp:LinkButton>
            <asp:LinkButton ID="lbReopen" CssClass="btn btn-default" runat="server" CommandName="reopenrequisition" OnClick="ToolbarItem_click" Visible="false">Reopen Requisition</asp:LinkButton>

            <asp:LinkButton ID="lbReturn" CssClass="btn btn-default" runat="server" CommandName="return" OnClick="ToolbarItem_click" Visible="true">Return To List</asp:LinkButton>
        </div>
        <div id="pnlMain" class="panel panel-block">
            
            <div class="panel-heading">
                <div class="pull-right">
                    <asp:HyperLink ID="lnkNotes" runat="server" Visible="false" NavigateUrl="#catNotes"><i class="fa fa-sticky-note fa-2x" title="Notes"></i></asp:HyperLink>&nbsp; &nbsp;
                    <asp:HyperLink ID="lnkAttachments" runat="server" Visible="false" NavigateUrl="#catAttachments"><i class="fa fa-paperclip fa-2x" title="Attachments"></i></asp:HyperLink>
                </div>
                <h1 class="panel-title">
                    <asp:Label ID="lblTitle" runat="server" Text='<%= NewRequisitionTitleSetting %>' />
                </h1>
            </div>
            <div class="panel-body">
                <div id="divStatusNote" class="alert alert-danger" runat="server" visible="false">
                    <strong>This requisition is <%= StateType %>.</strong> <br />
                    <asp:Label ID="lblDisposition" runat="server" Visible='<%# String.IsNullOrEmpty(Disposition) %>'>
                        <strong>Disposition:</strong> &nbsp; <%= Disposition %>
                    </asp:Label>
                </div>
                <div id="content">
                    <div class="summary">
                        <h3>Summary</h3>
                            <div class="smallText">
                                Please enter the details of your request. Fields marked with <span class="required">*</span> are required.
                            </div>
                            <div id="summaryError" runat="server" class="alert alert-danger" role="alert" Visible="false">
                                <asp:Label ID="lblSummaryError" runat="server" />
                            </div>
                            <div class="row">
                                <div class="col-md-4 col-xs-12">
                                    <Rock:RockTextBox Label="Req. Title:" ID="txtTitle" runat="server" Visible="true" ReadOnly="false" MaxLength="200" CssClass="form-control" Required="true"/>
                                </div>
                                <div class="col-xs-4">
                                    <Rock:RockDropDownList Label="Type:" ID="ddlType" runat="server" CssClass="form-control" AutoPostBack="true" OnSelectedIndexChanged="ddlType_SelectedIndexChanged" Required="true" />
                                </div>
                                <div class="col-xs-4">
                                    <div class="form-group required">
                                        <label>Status:</label>
                                        <div>
                                            <asp:Label ID="lblStatus" runat="server" Text="Status" />
                                        </div>
                                    </div>
                                </div>

                                <div class="col-xs-4">
                                    <div class="form-group required">
                                        <label class="control-label">Requester:</label>
                                        <div>
                                            <secc:StaffPicker ID="ucStaffPicker" runat="server" AllowMultipleSelections="false" 
                                                ShowPersonDetailLink="true" ShowPhoto="true"/>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-xs-4">
                                    <Rock:RockTextBox Label="Deliver To:" Required="true" ID="txtDeliverTo" runat="server" Text="Deliver To" />
                                </div>
                                <div class="col-xs-4">
                                    <div class="form-group required">
                                        <label>Approved:</label>
                                        <div>
                                            <asp:Label ID="lblApproval" runat="server" Text="Status" />
                                        </div>
                                    </div>
                                </div>
                            
                                <div class="col-xs-4">
                                    <div class="form-group">
                                        <label class="control-label">Pref. Vendor:</label>
                                        <div>
                                            <asp:Label ID="lblVendor" runat="server" />
                                            <asp:LinkButton ID="lbVendorRemove" runat="server" Visible="false" OnClick="lbVendorRemove_click"
                                                 CausesValidation="false" CssClass="hidden-print">
                                                <i class="fa fa-times"></i>
                                            </asp:LinkButton>
                                            <asp:Button ID="btnVendorModalShow" runat="server" Text="..." CssClass="btn btn-default hidden-print"
                                                OnClick="btnVendorModalShow_click" CausesValidation="false"/>
                                        </div>
                                    </div>
                                </div>
                                <div id="divCapitalRequest" runat="server" visible="false" class="col-xs-4">
                                
                                    <div class="form-group">
                                        <label class="control-label">CER:</label>
                                        <div>
                                            <asp:HiddenField ID="hfCapitalRequest" runat="server" />
                                            <asp:Literal ID="lCapitalRequest" runat="server" />
                                            <asp:LinkButton ID="lbCapitalRequestRemove" runat="server" Visible="false" OnClick="lbCapitalRequestRemove_Click" Style="text-decoration:none;" >
                                                <i class="fa fa-times"></i>
                                            </asp:LinkButton>
                                            <asp:Button ID="btnCapitalRequestModalShow" runat="server" Text="..." CssClass="btn btn-default" OnClick="btnCapitalRequestModalShow_Click" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                    </div>
                    <pre class="hidden-print" style="margin-top: 10px;"><%= ScriptureTextSetting %></pre>
                    </div>
                    <div id="items">
                        <h3>Items</h3>
                      <Rock:Grid ID="dgItems" runat="server" AllowPaging="false" AllowSorting="false" OnRowDataBound="dgItems_OnRowDataBound" ShowFooter="true"
                            DataKeyField="ItemID" CssClass="list" AutoGenerateColumns="false" NoResultText="No Active Items" ShowActionRow="false"
                            OnPreRender="dgItems_PreRender" OnRowUpdating="dgItems_RowUpdating">
                            <Columns>
                                <Rock:RockBoundField DataField="ItemID" Visible="false" />
                                <Rock:RockBoundField HeaderText="Qty" DataField="Quantity" ItemStyle-Width="4%" ItemStyle-HorizontalAlign="Center"/>
                                <Rock:RockBoundField HeaderText="Qty Recv'd" DataField="QuantityReceived" ItemStyle-Width="4%" ItemStyle-HorizontalAlign="Center" />
                                <Rock:RockBoundField HeaderText="Item #" DataField="ItemNumber" ItemStyle-Width="10%" />
                                <Rock:RockBoundField HeaderText="Description" DataField="Description" ItemStyle-Width="30%" ItemStyle-CssClass="wrap" />
                                <Rock:RockBoundField HeaderText="Needed By" DataField="DateNeeded" ItemStyle-Width="5%" DataFormatString="{0:d}"  />
                                <Rock:BoolField HeaderText="Express Shipping" DataField="ExpeditedShipping" ItemStyle-Width="5%" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                <Rock:RockTemplateField SortExpression="AccountNumber" ItemStyle-Width="7%">
                                    <HeaderTemplate>
                                        Charge To</HeaderTemplate>
                                    <ItemTemplate>
                                        <%# DataBinder.Eval(Container.DataItem, "AccountNumber") %>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField HeaderText="Cost/Item" DataField="EstimatedCost" ItemStyle-Width="5%" DataFormatString="{0:c}" />
                                <Rock:RockBoundField HeaderText="Ext" DataField="LineItemCost" ItemStyle-Width="5%" DataFormatString="{0:c}" />
                                <Rock:RockTemplateField HeaderText="Purchase Orders" HeaderStyle-HorizontalAlign="Center" ItemStyle-Width="5%" ItemStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <asp:Literal ID="litPOs" runat="server"></asp:Literal>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockTemplateField HeaderStyle-CssClass="hidden-print" FooterStyle-CssClass="hidden-print" ItemStyle-CssClass="hidden-print">
                                    <ItemStyle HorizontalAlign="Right" Width="1%" />
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbEdit" runat="server" class="btn btn-default" CommandName="Update">
                                            <i class="fa fa-wrench" title="Edit"></i>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>

                                <Rock:RockTemplateField HeaderStyle-CssClass="hidden-print" FooterStyle-CssClass="hidden-print" ItemStyle-CssClass="hidden-print">
                                    <ItemStyle HorizontalAlign="Right" Width="1%" />
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbRemove" runat="server" class="btn btn-default" CommandName="Remove" >
                                            <i class="fa fa-remove" title="Delete"></i>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                            </Columns>
                        </Rock:Grid>
                    </div>
                    <div id="approvals"> 
                        <h3>Approval Requests</h3>
                        <Rock:Grid ID="dgApprovals" runat="server" AllowPaging="false" AllowSorting="false" OnReBind="dgApprovals_ReBind" NoResultText="No Approval Requests found"
                             DataKeyField="ApprovalID" CssClass="list" OnRowDataBound="dgApprovals_RowDataBound" OnRowCommand="dgApprovals_RowCommand"  ShowActionRow="false">
                            <Columns>
                                <Rock:RockBoundField HeaderText="Approval ID" DataField="ApprovalID" Visible = "false" />
                                <Rock:RockBoundField HeaderText="Approver" DataField="ApproverName" />
                                <Rock:RockBoundField HeaderText="Status" DataField="ApprovalStatus" />
                                <Rock:RockBoundField HeaderText="Date Approved" DataField="DateApproved" DataFormatString="{0:d}" />
                                <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" HeaderText="Options" HeaderStyle-Width="350px" HeaderStyle-CssClass="hidden-print" FooterStyle-CssClass="hidden-print" ItemStyle-CssClass="hidden-print">
                                    <ItemTemplate>
                                        <asp:LinkButton ID="lbResubmit" runat="server" Visible="false" Text="Resubmit" CommandName="resubmit" CssClass="btn btn-default" />
                                        <asp:LinkButton ID="lbApprove" runat="server" Visible="false" Text="Approve" CommandName="approve" CssClass="btn btn-default" />
                                        <asp:LinkButton ID="lbApproveForward" runat="server" Visible="false" Text="Approve & Forward" CommandName="approveForward" CssClass="btn btn-default" />
                                        <asp:LinkButton ID="lbDeny" runat="server" Visible="false" Text="Decline" CommandName="decline" CssClass="btn btn-default" />
                                        <asp:LinkButton ID="lbRemove" runat="server" CommandName="Remove" Visible="false" class="btn btn-default">
                                            <i class="fa fa-remove"></i>
                                        </asp:LinkButton>
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                            </Columns>
                        </Rock:Grid> 
                        <asp:HiddenField ID="hfApproverID" runat="server" />
                        <asp:Button ID="btnApproverAdd" runat="server" OnClick="btnApproverAdd_Click" style="visibility:hidden;display:none;" />
                    </div>
                
                    <div id="charges" >
                        <h3>Charges</h3>
                        <Rock:Grid ID="dgCharges" runat="server" AllowPaging="false" AllowSorting="false" DataKeyField="PaymentChargeID" NoResultText="No Charges found for this requisition." CssClass="list" ShowActionRow="false">
                            <Columns>
                                <Rock:RockBoundField HeaderText="PaymentChargeID" DataField="PaymentChargeID" Visible="false" />
                                <Rock:RockBoundField HeaderText="Payment Date" DataField="PaymentDate" DataFormatString="{0:d}" />
                                <Rock:RockBoundField HeaderText="Purchase Order" DataField="PurchaseOrderId" />
                                <Rock:RockBoundField HeaderText="Vendor" DataField="VendorName" />
                                <Rock:RockBoundField HeaderText="Payment Method" DataField="PaymentMethodName" />
                                <Rock:RockBoundField HeaderText="Account" DataField="Account" />
                                <Rock:RockBoundField HeaderText="Charge Amount" DataField="ChargeAmount" DataFormatString="{0:c}" />
                            </Columns>
                        </Rock:Grid>
                    
                    </div>
                    <div id="notes">
                        <h3>Notes</h3>
                        <secc:Notes ID="ucPurchasingNotes" runat="server" />
                    </div>
                    <div id="attachments">
                        <h3>Attachments</h3>
                        <secc:Attachments ID="ucAttachments" runat="server"/>
                    </div>
                </div>
                <div class="footer">
                    <%= FooterTextSetting %>
                </div>
        </div>
        <secc:StaffPicker ID="ucStaffPickerApprover" runat="server" OnSelect="SelectApprover_Click" />
        <Rock:ModalIFrameDialog ID="mpiAttachmentPicker" runat="server"/>
        <Rock:ModalDialog ID="mpChooseVendor" runat="server" Title="Choose Vendor" CancelControlID="btnVendorModalCancel"
            EnableViewState="true" OnSaveClick="btnVendorModalUpdate_click" ValidationGroup="VendorSelect">
            <Content>
                <asp:UpdatePanel ID="upChooseVendor" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div runat="server" id="divVendorInstructions" class="instructions smallText">
                            <%= ChooseVendorInstructionsSetting %>
                        </div>
                        <secc:VendorSelect ID="ucVendorSelect" runat="server" EnableViewState="true"/>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mpCancelPrompt" runat="server" Title="Cancel Requisition">
            <Content>
                    <style>
                        #<%=mpCancelPrompt.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                    </style>
                <div>
                    Are you sure that you would like to cancel this requisition request?
                </div>
                <div class="clearfix">
                    <div class="pull-right">
                        <asp:Button ID="btnCancelPromptYes" runat="server" OnClick="mpCancelPrompt_Click" CssClass="btn btn-primary" CommandName="cancelYes" Text="Yes" />
                        <asp:Button ID="btnCancelPromptNo" runat="server" OnClick="mpCancelPrompt_Click" CssClass="btn btn-default" CommandName="cancelNo" Text="No"/>
                    </div>
                </div>
            </Content>

        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mpSelectApprovalType" runat="server" Title="Select Approver" >
            <Content>
                <ContentTemplate>
                    <style>
                        #<%=mpSelectApprovalType.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                    </style>
                    <div class="row">
                        <div class="col-xs-12">
                            Who would you like to approve this requisition?
                        </div>
                    </div>
                    <div class="row">
                        <div class="pull-right">
                            <div class="col-xs-12">
                                <asp:Button ID="btnSelectApproverSelf" runat="server" cssclass="btn btn-primary" Text="Self" OnClick="btnSelectApproverSelf_Click" />
                                <asp:Button ID="btnSelectApproverOther" runat="server" CssClass="btn btn-default" Text="Other" OnClick="btnSelectApproverOther_Click" />
                            </div>
                        </div>
                    </div>
                </ContentTemplate>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mpPurchaseOrderSelect" runat="server" Title="Select Purchase Order">
            <Content>
                <asp:UpdatePanel ID="upPurchaseOrderSelect" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <style>
                            #<%=mpPurchaseOrderSelect.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                        </style>
                        <div id="purchaseOrderSelect">
                            <div style=" max-height:400px; overflow:auto;">
                                <Rock:Grid ID="dgSelectPurchaseOrder" runat="server" CssClass="list" AllowPaging="false" AllowSorting="false">
                                    <Columns>
                                        <Rock:RockTemplateField ItemStyle-Width="10%" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle">
                                            <ItemTemplate>
                                                <asp:RadioButton ID="rdoSelectPO" runat="server" GroupName="SelectPO" AutoPostBack="true" OnCheckedChanged="rdoSelectPO_CheckedChanged" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                        <Rock:RockBoundField HeaderText="PO Number" DataField="PurchaseOrderID" />
                                        <Rock:RockBoundField HeaderText="Type" DataField="Type" />
                                        <Rock:RockBoundField HeaderText="Vendor Name" DataField="VendorName" />
                                        <Rock:RockBoundField HeaderText="Created By" DataField="CreatedBy" />
                                        <Rock:RockBoundField HeaderText="Date Created" DataField="DateCreated" />
                                    </Columns>
                                </Rock:Grid>
                            </div>
                            <div style="padding-top:3px; text-align:right;">
                                <asp:Button ID="btnSelectPurchaseOrderSelect" runat="server" Text="Select PO"  CssClass="btn btn-primary" OnClick="btnSelectPurchaseOrderSelect_Click" />
                                <asp:Button ID="btnSelectPurchaseOrderNew" runat="server" Text="New PO"  CssClass="btn btn-info" OnClick="btnSelectPurchaseOrderNew_Click" />
                                <asp:Button ID="btnSelectPurchaseOrderCancel" runat="server" Text="Cancel" CssClass="btn btn-default" OnClick="btnSelectPurchaseOrderCancel_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnSelectPurchaseOrderSelect" />
                        <asp:PostBackTrigger ControlID="btnSelectPurchaseOrderNew" />
                        <asp:PostBackTrigger ControlID="btnSelectPurchaseOrderCancel" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpSelectPurchaseOrderItems" runat="server" Title="Select PO Items" SaveButtonText="Add" OnSaveClick="btnSelectPOItemsAdd_Click">
            <Content>
                <asp:UpdatePanel ID="upSelectPurchaseOrderItems" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="selectPurchaseOrderItems">
                            <asp:HiddenField ID="hfSelectPOItemsPONumber" runat="server" />
                            <div id="selectPurchaseOrderError">
                                <asp:Label ID="lblSelectPurchaseOrderError" runat="server" CssClass="smallText" Visible="false" style="color:Red;" />
                            </div>
                            <div class="form-inline">
                                <div class="form-group col-sm-3">
                                    <label>PO Number:</label>
                                    <asp:Label ID="lblSelectPOItemsPONumber" runat="server"></asp:Label>
                                </div>
                                <div class="form-group col-sm-6">
                                    <label>Vendor:</label>
                                    <asp:DropDownList class="form-control" ID="ddlSelectPOItemsVendor" label="Vendor:" runat="server" />
                                    <asp:Label label="Vendor:" ID="lblSelectPOItemsVendor" runat="server"></asp:Label>
                                </div>
                                <div class="form-group col-sm-3">
                                    <label>Type:</label>
                                    <asp:DropDownList class="form-control" ID="ddlSelectPOItemsType" runat="server" />
                                    <asp:Label ID="lblSelectPOItemsType" runat="server"></asp:Label>
                                </div>
                            </div>
                            <div style="width:99%; max-height:350px; overflow-y:auto; overflow-x:hidden;">
                                <Rock:Grid ID="dgSelectPOItems" DataKeyNames="ItemID" runat="server" class="list" AllowPaging="false" AllowSorting="false" OnRowDataBound="dgSelectPOItems_ItemDataBound">
                                    <Columns>
                                        <Rock:RockBoundField DataField="ItemNumber" HeaderText="Item Number" ItemStyle-Width="15%"
                                            HeaderStyle-HorizontalAlign="Center" />
                                        <Rock:RockBoundField DataField="Description" HeaderText="Description" ItemStyle-Width="20%" ItemStyle-CssClass="wrap" />
                                        <Rock:RockBoundField DataField="DateNeeded" HeaderText="Date Needed" ItemStyle-Width="10%"
                                            HeaderStyle-HorizontalAlign="Center" />
                                        <Rock:BoolField DataField="AllowExpedited" HeaderText="Expedite" ItemStyle-HorizontalAlign="Center"
                                            ItemStyle-Width="10%" />
                                        <Rock:RockBoundField DataField="QtyRequested" HeaderText="Qty Requested" ItemStyle-Width="10%"
                                            ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <Rock:RockBoundField DataField="QtyAssigned" HeaderText="Qty Assigned" ItemStyle-Width="10%"
                                            ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" />
                                        <Rock:RockTemplateField ItemStyle-Width="10%">
                                            <HeaderTemplate>
                                                Remaining
                                            </HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtQtyRemaining" CssClass="form-control" runat="server" Visible="false" Style="width: 35px;" />
                                                <asp:Label ID="lblQuantityRemaining" runat="server"
                                                    Visible="false" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                        <Rock:RockTemplateField  ItemStyle-Width="15%">
                                            <HeaderTemplate>
                                                Price</HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtRequisitionItemPrice" runat="server" CssClass="form-control" Visible="false"
                                                    Style="width: 100px;" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                    </Columns>
                                </Rock:Grid>
                            </div>
                            <%--<div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="" runat="server" Text="" CssClass="btn btn-primary" OnClick="btnSelectPOItemsAdd_Click" />
                                <asp:Button ID="btnSelectPOItemsReset" runat="server" Text="Reset" CssClass="smallText" OnClick="btnSelectPOItemsReset_Click" />
                                <asp:Button ID="btnSelectPOItemsCancel" runat="server" Text="Cancel" CssClass="smallText" OnClick="btnSelectPOItemsCancel_Click" />
                            </div>--%>
                        </div>
                    </ContentTemplate>
                    <%-- Triggers>
                        <asp:AsyncPostBackTrigger ControlID="btnSelectPurchaseOrderNew" />
                        <asp:AsyncPostBackTrigger ControlID="btnSelectPurchaseOrderSelect" />
                    </ --%>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpCERSelect" runat="server" SaveButtonText="Select" OnSaveClick="btnCERSelectChoose_Click">
            <Content>
                <asp:UpdatePanel id="upCERSelect" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <h3>Choose Capital Request</h3>
                        <div id="pnlCERSelect">
                            <Rock:Grid ID="grdCERSelect" runat="server" OnReBind="grdCERSelect_ReBind" AllowPaging="false" ShowActionRow="false" DataKeyNames="CapitalRequestId">
                                <Columns>
                                    <Rock:RockTemplateField>
                                        <ItemTemplate>
                                            <asp:RadioButton ID="rbCERSelect" runat="server" GroupName="CERSelect" AutoPostBack="true" OnCheckedChanged="rbCERSelect_CheckedChanged"  />
                                        </ItemTemplate>
                                    </Rock:RockTemplateField>
                                    <Rock:RockBoundField DataField="ProjectName" HeaderText="Project Name" SortExpression="ProjectName" />
                                    <Rock:RockBoundField DataField="RequestingMinistry" HeaderText="Ministry" SortExpression="RequestingMinistry" />
                                    <Rock:RockBoundField DataField="RequesterName" HeaderText="Requester" SortExpression="RequesterNameLastFirst" />
                                    <Rock:RockBoundField DataField="FullAccountNumber" HeaderText="Account" SortExpression="FullAccountNumber" />
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="btnCapitalRequestModalShow" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
            <%--Buttons>
                TODO: Make sure these buttons are tied to something
                <asp:Button ID="btnCERSelectChoose" runat="server" CssClass="smallText" Text="Choose" OnClick="btnCERSelectChoose_Click" />
                <asp:Button ID="btnCERSelectCancel" runat="server" CssClass="smallText" Text="Cancel" OnClick="btnCERSelectCancel_Click" />    
                </Buttons --%>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mpItemDetail" runat="server" CancelControlID="btnItemDetailsCancel" Title="Item Details">
            <Content DefaultButton="btnItemDetailsUpdate">
                <asp:UpdatePanel ID="upItemDetail" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <style>
                            #<%=mpItemDetail.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                        </style>
                        <asp:HiddenField ID="hfItemID" runat="server" />
                        <div id="ItemDetails">
                            <div class="instructions">
                                Please provide as much detail as possible for the item that you are requesting. Fields marked with <span class="required">*</span> are required.
                            </div>
                            <div class="smallText" style="color:Red;">
                                <asp:Label ID="lblItemDetailError" runat="server" Visible="false" />
                            </div>
                            <Rock:RockTextBox Label="Quantity" Id="txtItemQuantity" runat="server" Required="true" ValidationGroup="ItemDetail" />
                            <Rock:RockTextBox Label="Item Number" Id="txtItemNumber" runat="server" ValidationGroup="ItemDetail" />
                            <Rock:RockTextBox Label="Description" Id="txtItemDescription" TextMode="MultiLine" runat="server" Required="true" ValidationGroup="ItemDetail" />

                            <asp:Panel id="pnlItemDetailCompany" runat="server" class="hidden">
                                <Rock:RockDropDownList Label="Organization" ID="ddlItemCompany" runat="server" />
                            </asp:Panel>
                            <div class="row">
                                <div class="col-sm-3">
                                    <div class="form-group required">
                                        <label class="control-label">
                                            Account
                                        </label>
                                        <div>
                                            <div class="form-inline">
                                                <Rock:RockTextBox ID="txtItemFundNumber"  runat="server" MaxLength="3" Size="3" onKeyUp="autoAdvanceCheck(event, 3, 'txtItemFundNumber', 'txtItemDepartmentNumber');" Required="true" ValidationGroup="ItemDetail" />
                                                -
                                                <Rock:RockTextBox ID="txtItemDepartmentNumber" runat="server" MaxLength="4" Size="4" onKeyUp="autoAdvanceCheck(event, 3, 'txtItemDepartmentNumber', 'txtItemAccountNumber');" Required="true" ValidationGroup="ItemDetail" />
                                                -
                                                <Rock:RockTextBox ID="txtItemAccountNumber" runat="server" MaxLength="5" Size="5" Required="true" ValidationGroup="ItemDetail" />

                                            </div>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-3">
                                    <Rock:RockTextBox ID="txtItemPrice" Label="Price/Each" runat="server" ValidationGroup="ItemDetail" />
                                </div>
                                <div class="col-sm-3">
                                    <Rock:DatePicker ID="txtDateNeeded" runat="server" Label="Needed By" onChange="dateNeededChanged()" style="width:auto"/>
                                </div>
                                <div class="col-sm-3">
                                    <div id="ItemAllowExpedited" style="display: none;">
                                        <asp:CheckBox ID="chkItemAllowExpedited" runat="server" CssClass="smallText" Text="Allow Expedited Shipping." />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="clearfix">
                            <div class="pull-right">
                                <asp:Button ID="btnItemDetailsUpdate" class="btn btn-primary" runat="server" OnClick="btnItemDetailsUpdate_click"
                                    Text="Save & Close" ValidationGroup="ItemDetail"/>
                                <asp:Button ID="btnItemDetailsSaveNew" class="btn btn-info" runat="server" OnClick="btnItemDetialsSaveNew_click"
                                    Text="Save & Add New" ValidationGroup="ItemDetail" />
                                <asp:Button ID="btnItemDetailsReset" class="btn btn-default" runat="server" OnClick="btnItemDetailsReset_click"
                                    Text="Reset" ValidationGroup="ItemDetail" CausesValidation="false" />
                                <asp:Button ID="btnItemDetailsCancel" class="btn btn-default" runat="server" OnClick="btnItemDetailsCancel_click"
                                    Text="Close" ValidationGroup="ItemDetail" CausesValidation="false" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnItemDetailsUpdate" />
                        <asp:PostBackTrigger ControlID="btnItemDetailsCancel" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpItemPurchaseOrder" runat="server">
            <Content>
                <asp:UpdatePanel ID="upItemPurchaseOrder" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="itemPurchaseOrder" style="width:500px;">
                            <asp:HiddenField ID="hfIPOItemID" runat="server" />
                            <h3>Item PO's</h3>
                            <h4>Item Summary</h4>
                            <div style="width:100%;">
                                <div style="width:100px; float:left;" class="formLabel">Item #</div>
                                <div style="width:100px; float:left;" class="formItem"><asp:Label ID="lblIPOItemNumber" runat="server" /> </div>
                                <div style="width:100px; float:left;" class="formLabel">Description</div>
                                <div style="width:200px; float:left;" class="formItem"><asp:Label ID="lblIPODescription" runat="server" /></div>
                            </div>
                            <div style="width:100%;">
                                <div style="width:100px; float:left;" class="formLabel">Qty Requested</div>
                                <div style="width:100px; float:left;" class="formItem"><asp:Label id="lblIPOQtyRequested" runat="server" /></div>
                                <div style="width:100px; float:left;" class="formLabel">Qty Received</div>
                                <div style="width:50px; float:left;" class="formItem"><asp:Label ID="lblIPOQtyReceived" runat="server" /></div>
                                <div style="width:50px; float:left;" class="formLabel">Acct #</div>
                                <div style="width:100px; float:left;" class="formItem"><asp:Label ID="lblIPOAcctNumber" runat="server" />
                            </div>
                            <h4>Purchase Orders</h4>
                            <Rock:Grid ID="dgIPOPurchaseOrders" runat="server" CssClass="list" OnItemDataBound="dgIPOPurchaseOrders_ItemDataBound" ExportEnabled="false">
                                <Columns>
                                    <Rock:RockBoundField HeaderText="PurchaseOrderID" DataField="PurchaseOrderID" Visible="false" />
                                    <Rock:RockTemplateField HeaderText="PO Number" ItemStyle-Width="10%"  HeaderStyle-HorizontalAlign="Center">
                                        <ItemTemplate>
                                            <asp:HyperLink ID="hlPurchaseOrderID" runat="server" Target="_blank" Visible="false" />
                                            <asp:Label ID="lblPurchaseOrderID" runat="server" Visible="false" />
                                        </ItemTemplate>
                                    </Rock:RockTemplateField>
                                    <Rock:RockBoundField HeaderText="Type" DataField="PurchaseOrderType" ItemStyle-Width="20%" />
                                    <Rock:RockBoundField HeaderText="Vendor" DataField="VendorName" ItemStyle-Width="20%" />
                                    <Rock:RockBoundField HeaderText="Status" DataField="Status" ItemStyle-Width="20%" />
                                    <Rock:RockBoundField HeaderText="Qty Ordered" DataField="QtyOrdered" ItemStyle-Width="10%" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                                    <Rock:RockBoundField HeaderText="Qty Received" DataField="QtyReceived" ItemStyle-Width="10%" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                                </Columns>
                            </Rock:Grid>
                            <div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="btnIPOClose" runat="server" CssClass="smallText" Text="Close" OnClick="btnIPOClose_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <%-- Triggers>
                        <asp:AsyncPostBackTrigger ControlID="dgItems" EventName="ItemCommand" />
                    </--%>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="ucStaffPickerApprover" EventName="Select"/>
    </Triggers>
    <%--  Triggers>
        <asp:AsyncPostBackTrigger ControlID="btnItemDetailsUpdate" />
        <asp:AsyncPostBackTrigger ControlID="btnItemDetailsSaveNew" />
        <asp:AsyncPostBackTrigger ControlID="btnItemDetailsCancel" />
        <asp:AsyncPostBackTrigger ControlID="btnVendorModalUpdate" />
        <asp:AsyncPostBackTrigger ControlID="btnVendorModalReset" />
        <asp:AsyncPostBackTrigger ControlID="btnVendorModalCancel" />
        <asp:AsyncPostBackTrigger ControlID="btnCancelPromptYes" />
        <asp:AsyncPostBackTrigger ControlID="btnCancelPromptNo" />
        <asp:AsyncPostBackTrigger ControlID="btnSelectPOItemsAdd" />
        <asp:AsyncPostBackTrigger ControlID="btnSelectPOItemsCancel" />
        <asp:AsyncPostBackTrigger ControlID="ucPurchasingNotes" EventName="RefreshParent" />
        <asp:AsyncPostBackTrigger ControlID="ucAttachments" EventName="RefreshParent" />
        <asp:AsyncPostBackTrigger ControlID="btnSelectApproverSelf" EventName="Click" />
    </Triggers --%>
</asp:UpdatePanel>

