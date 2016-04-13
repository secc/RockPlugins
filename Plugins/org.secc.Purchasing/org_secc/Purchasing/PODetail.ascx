<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PODetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.PODetail" %>
<%@ Register Src="Notes.ascx" TagName="Notes" TagPrefix="secc" %>
<%@ Register Src="Attachments.ascx" TagName="Attachments" TagPrefix="secc" %>
<%@ Register Src="StaffPicker.ascx" TagName="StaffPicker" TagPrefix="secc" %>
<style>
.table > tfoot > tr > td {
    background-color: #edeae6;
    color: #6a6a6a;
    font-weight: 600;
    border-color: #d8d1c8;
}
</style>
<script type="text/javascript">
    function showAlert(isShown) {
        showAlert(isShown, true);
    }

    function showAlert(isShown, hideMain) {
        if (isShown == true) {
            $("#pnlError").css("display", "inherit");
            $("#pnlError").css("visibility", "visible");

            if (hideMain == true) {
                $("#pnlMain").css("display", "none");
                $("#pnlMain").css("visibility", "hidden");
            }
            else {
                $("#pnlMain").css("display", "inherit");
                $("#pnlMain").css("visibility", "visible");
            }
        }
        else {
            $("#pnlMain").css("display", "inherit");
            $("#pnlMain").css("visibility", "visible");
            $("#pnlError").css("display", "none");
            $("#pnlError").css("visibility", "hidden");
        }
    }
</script>
<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <div id="pnlError" style="display: none; visibility: hidden;">
            <div class="statusNote">
                <asp:Label ID="lblPOAlert" runat="server" />
                <br />
                <asp:HyperLink ID="hlinkPOAlert" runat="server" Text="Return To List" />
            </div>
        </div>
        <div class="btn-group" role="group" style="margin-bottom: 10px;">
            <asp:LinkButton ID="lbToolbarSave" runat="server" CommandName="save" OnClick="ToolbarItem_Click"
                Visible="true" CssClass="btn btn-default">Save</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarAddItem" runat="server" CommandName="additem" OnClick="ToolbarItem_Click"
                Visible="true" CssClass="btn btn-default">Add Item</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarAddNote" runat="server" CommandName="addnote" OnClick="ToolbarItem_Click"
                Visible="true" CssClass="btn btn-default">Add Note</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarAddAttachment" runat="server" CommandName="addattachment"
                OnClick="ToolbarItem_Click" Visible="true" CssClass="btn btn-default">Add Attachment</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarOrder" runat="server" CommandName="order" OnClick="ToolbarItem_Click"
                Visible="true" CssClass="btn btn-default">Order</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarReceive" runat="server" CommandName="receive" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Receive</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarAddPayment" runat="server" CommandName="addpayment" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Add Payment</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarMarkAsBilled" runat="server" CommandName="markasbilled" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Mark as Billed</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarClose" runat="server" CommandName="close" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Close</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarPrintPO" runat="server" CommandName="print" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Print</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarReopen" runat="server" CommandName="reopen" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Reopen</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarCancel" runat="server" CommandName="cancel" OnClick="ToolbarItem_Click" 
                Visible="true" CssClass="btn btn-default">Cancel</asp:LinkButton>
            <asp:LinkButton ID="lbToolbarReturn" runat="server" CommandName="return" OnClick="ToolbarItem_Click"
                Visible="true" CssClass="btn btn-default">Return to List</asp:LinkButton></li>
        </div>
        
        <div id="pnlMain" class="panel panel-block">
            
            <div class="panel-heading">
                <h1 class="panel-title">Summary</h1>
            </div>
            <div class="panel-body">
                <div id="icons">
                    <asp:HyperLink ID="lnkNotes" runat="server" Visible="false" NavigateUrl="#catNotes"><i class="fa fa-sticky-note fa-2x" title="Notes"></i></asp:HyperLink>
                    <asp:HyperLink ID="lnkAttachments" runat="server" Visible="false" NavigateUrl="#catAttachments"><i class="fa fa-paperclip fa-2x" title="Attachments"></asp:HyperLink>
                </div>

                <div class="category" id="catSummary">
                    <div class="error">
                        <asp:Label ID="lblSummaryError" runat="server" Visible="false" />
                    </div>
                    <div class="row">
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="PO #:" ID="lblPONum" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockDropDownList Label="Type:" ID="ddlType" runat="server" CssClass="smallText" Visible="true"  />
                            <Rock:RockLiteral  Label="Type:" ID="lblType" runat="server" Visible="false" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                             <Rock:RockLiteral Label="Status:" ID="lblStatus" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockDropDownList Label="Payment Method:" ID="ddlDefaultPayMethod" runat="server" CssClass="smallText" Visible="true" />
                            <Rock:RockLiteral Label="Payment Method:" ID="lblDefaultPayMethod" runat="server" Visible="false" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="Created By:" ID="lblCreatedBy" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="Created On:" ID="lblCreatedOn" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="Ordered By:" ID="lblOrderedBy" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="Ordered On:" ID="lblOrderedOn" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <Rock:RockLiteral Label="Received On:" ID="lblReceivedOn" runat="server" />
                        </div>
                        <div class="col-md-3 col-sm-4">
                            <div class="row">
                                <div class="col-xs-6">
                                    <Rock:RockLiteral Label="Vendor:" CssClass="formLabel" ID="lblVendorName" runat="server" />
                                </div>
                                <div class="col-xs-6">
                                    <asp:LinkButton ID="lbChangeVendor" Text="Change" runat="server" CssClass="btn btn-small btn-default"
                                        OnClick="lbChangeVendor_Click" />
                                </div>
                            </div>
                            <asp:HiddenField ID="hfVendorID" runat="server" />
                            <div id="divVendorAddress" runat="server">
                                <asp:Label ID="lblVendorAddress" runat="server" /><br />
                                <asp:Label ID="lblVendorCSZ" runat="server" />
                            </div>
                            <div id="divVendorWebAddress" runat="server">
                                <asp:Label ID="lblVendorWebAddress" runat="server" />
                            </div>
                            <div id="divVendorTerms" runat="server">
                                <asp:Label ID="lblVendorTerms" runat="server" />
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="row">
                                <div class="col-xs-6">
                                    <Rock:RockLiteral Label="Ship To:" ID="lblShipToName" CssClass="formLabel" runat="server" />
                                </div>
                                <div class="col-xs-6">
                                    <asp:LinkButton ID="lbChangeShipTo" Text="Change" runat="server" CssClass="btn btn-small btn-default"
                                        OnClick="lbChangeShipTo_Click" />
                                </div>
                            </div>
                            <asp:HiddenField ID="hfCampusID" runat="server" />
                            <div id="divShipToAttention" runat="server">
                                Attn:
                                <asp:Label ID="lblShipToAttn" runat="server" />
                            </div>
                            <div id="divShipToAddress" runat="server">
                                <asp:Label ID="lblShipToAddress" runat="server" /><br />
                                <asp:Label ID="lblShipToCity" runat="server" />,
                                <asp:Label ID="lblShipToState" runat="server" />
                                <asp:Label ID="lblShipToZip" runat="server" />
                            </div>
                        </div>
                    </div>
                </div>
                <div class="category" id="catItems">
                    <h3>
                        Items</h3>
                    <Rock:Grid ID="dgItems" runat="server" CssClass="list" AllowPaging="false" AllowSorting="false"
                        OnItemDataBound="dgItems_ItemDataBound" DataKeyField="ItemID" ShowActionRow="false"
                        ShowFooter="true">
                        <Columns>
                            <Rock:RockBoundField DataField="POItemID" HeaderText="ItemID" Visible="false" />
                            <Rock:RockBoundField DataField="Quantity" HeaderText="Qty" ItemStyle-Width="5%" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" />
                            <Rock:RockBoundField DataField="QuantityReceived" HeaderText="Qty Recv'd " ItemStyle-Width="5%" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Right" />
                            <Rock:RockBoundField DataField="ItemNumber" HeaderText="Item Number" ItemStyle-Width="10%" />
                            <Rock:RockBoundField DataField="Description" HeaderText="Description" ItemStyle-Width="22%" ItemStyle-CssClass="wrap" />
                            <Rock:RockBoundField DataField="DateNeeded" HeaderText="Date Needed" ItemStyle-Width="10%" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                            <Rock:BoolField DataField="IsExpedited" HeaderText="Expedite" ItemStyle-Width="3%" ItemStyle-HorizontalAlign="Center" />
                            <Rock:RockBoundField DataField="AccountNumber" HeaderText="Account Number" ItemStyle-Width="10%" />
                            <asp:HyperLinkField DataTextField="RequisitionID" HeaderText="Requisition" DataNavigateUrlFields="RequisitionID"
                                ItemStyle-CssClass="smallText" ItemStyle-Width="5%" Target="_blank" />
                            <Rock:RockBoundField DataField="Price" HeaderText="Price" ItemStyle-Width="10%" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                            <Rock:RockTemplateField HeaderText="Extension" ItemStyle-Width="10%" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right">
                                <ItemTemplate><%# DataBinder.Eval(Container.DataItem, "Extension") %></ItemTemplate>
                                <FooterTemplate>
                                    <div style="text-align:right;">
                                        <div><asp:Label ID="lblSubTotal" runat="server" CssClass="smallText" /></div>
                                        <div><asp:TextBox ID="txtShipping" runat="server" CssClass="smallText"  style="width:80%; text-align:right;"/><asp:Label ID="lblShipping" runat="server" Visible="false" CssClass="smallText" /> </div>
                                        <div><asp:TextBox ID="txtTax" runat="server" CssClass="smallText" style="width:80%; text-align:right" /><asp:Label ID="lblTax" runat="server" Visible="false" CssClass="smallText" /></div>
                                        <div style="border-top: 1px solid black;"><asp:Label ID="lblItemGridTotal" runat="server" CssClass="smallText" /></div>
                                    </div>
                                </FooterTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockTemplateField HeaderText="Actions" HeaderStyle-HorizontalAlign="Right" >
                                <ItemTemplate>
                                    <div style="width:90px">
                                        <asp:LinkButton ID="lbDetails" runat="server" OnCommand="dgItems_ItemCommand" CommandName="details" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "POItemID") %>' CssClass="btn btn-default" ToolTip="Details">
                                            <i class="fa fa-bars"></i>
                                        </asp:LinkButton>&nbsp;<asp:LinkButton ID="lbRemove" runat="server" OnCommand="dgItems_ItemCommand" CommandName="remove" CssClass="btn btn-default" ToolTip="Remove">
                                            <i class="fa fa-remove"></i>
                                        </asp:LinkButton>
                                    </div>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
                <div class="category" id="catReceivingHistory">
                    <h3>Receiving History:</h3>
                    <Rock:Grid ID="dgReceivingHistory" runat="server" CssClass="list" AllowPaging="false" AllowSorting="false" 
                            OnItemDataBound="dgReceivingHistory_ItemDataBound" OnItemCommand="dgReceivingHistory_ItemCommand" ShowActionRow="false">
                        <Columns>
                            <Rock:RockBoundField HeaderText="ReceiptID" DataField="ReceiptID" Visible="false" />
                            <Rock:RockBoundField HeaderText="Date Received" DataField="DateReceived" />
                            <Rock:RockBoundField HeaderText="Carrier" DataField="CarrierName" />
                            <Rock:RockBoundField HeaderText="Received By" DataField="ReceivedBy" />
                            <Rock:RockBoundField HeaderText="Total Items" DataField="TotalItems" />
                            <Rock:RockTemplateField ItemStyle-Width="90" HeaderStyle-HorizontalAlign="Right" HeaderText="Actions">
                                <ItemTemplate>
                                    <div style="width:90px">
                                        <asp:LinkButton ID="lbShowReceipt" runat="server" CommandName="showreceipt" CssClass="btn btn-default" ToolTip="Show">
                                            <i class="fa fa-bars"></i>
                                        </asp:LinkButton>&nbsp;
                                        <asp:LinkButton ID="lbRemoveReceipt" runat="server" CommandName="removereceipt" CssClass="btn btn-default" ToolTip="Remove" Visible="false">
                                            <i class="fa fa-remove"></i>
                                        </asp:LinkButton>
                                    </div>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
                <div class="category" id="catPayments">
                    <h3>Payments</h3>
                    <Rock:Grid ID="dgPayments" runat="server" CssClass="list" AllowPaging="false" AllowSorting="false" 
                        OnItemDataBound="dgPayments_ItemDataBound" OnItemCommand="dgPayments_ItemCommand" ShowActionRow="false">
                        <Columns>
                            <Rock:RockBoundField HeaderText="PaymentID" DataField="PaymentID" Visible="false" />
                            <Rock:RockBoundField HeaderText="Payment Date" DataField="PaymentDate" />
                            <Rock:RockBoundField HeaderText="Payment Method" DataField="PaymentMethod" />
                            <Rock:BoolField HeaderText="Fully Applied" DataField="FullyApplied" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center"  />
                            <Rock:RockBoundField HeaderText="Created By" DataField="CreatedByName" />
                            <Rock:RockBoundField HeaderText="Payment Amount" DataField="PaymentAmount" DataFormatString="{0:c}" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" />
                            
                            <Rock:RockTemplateField ItemStyle-Width="90" HeaderStyle-HorizontalAlign="Right" HeaderText="Actions">
                                <ItemTemplate>
                                    <div style="width:90px">
                                        <asp:LinkButton ID="lbDetails" runat="server" CommandName="details" CssClass="btn btn-default" ToolTip="Details">
                                            <i class="fa fa-bars"></i>
                                        </asp:LinkButton>&nbsp;
                                        <asp:LinkButton ID="lbRemove" runat="server" CommandName="remove" CssClass="btn btn-default" ToolTip="Remove" >
                                            <i class="fa fa-remove"></i>
                                        </asp:LinkButton>
                                    </div>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>

                </div>
                <div class="category" id="catNotes">
                    <h3>
                        Notes</h3>
                    <secc:Notes ID="ucNotes" runat="server" />
                </div>
                <div class="category" id="catAttachments">
                    <h3>
                        Attachments</h3>
                    <secc:Attachments ID="ucAttachments" runat="server" />
                </div>
            </div>
        </div>
        <Rock:ModalDialog ID="mpVendorSelect" runat="server">
            <Content>
                <asp:UpdatePanel ID="upVendorSelect" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="vendorSelect">
                            <asp:HiddenField ID="hfVendorSelectID" runat="server" />
                            <h3>
                                Select Vendor</h3>
                            <div class="error">
                                <asp:Label ID="lblVendorSelectError" runat="server" Visible="false" />
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Vendor:</div>
                                <div class="vendorItem formItem">
                                    <asp:DropDownList ID="ddlVendorSelect" runat="server" CssClass="smallText" OnSelectedIndexChanged="ddlVendorSelect_IndexChanged"
                                        AutoPostBack="true" style="width:65%;" />
                                    <asp:CheckBox ID="chkVendorShowInactive" runat="server" CssClass="smallText" Text="Show Inactive"
                                        OnCheckedChanged="chkVendorShowInactive_Changed" AutoPostBack="true" style="width:35%;" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Name:</div>
                                <div class="vendorItem formItem">
                                    <asp:TextBox ID="txtVendorName" runat="server" Style="width: 175px;" /><asp:Label
                                        ID="lblVendorSelectName" runat="server" Visible="false" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Address:</div>
                                <div class="vendorItem formItem">
                                    <span style="width: 70px;">
                                        <asp:Label ID="lblVendorSelectStreetHeader" runat="server" Text="Street:" /></span>
                                    <asp:TextBox ID="txtVendorAddress" runat="server" Style="width: 175px;" /><asp:Label
                                        ID="lblVendorSelectAddress" runat="server" Visible="false" />
                                    <br />
                                    <span style="width: 80px;">
                                        <asp:Label ID="lblVendorSelectCityHeader" runat="server" Text="City:" /></span>
                                    <asp:TextBox ID="txtVendorCity" runat="server" Style="width: 75px;" /><asp:Label
                                        ID="lblVendorSelectCity" runat="server" Visible="false" />
                                    <asp:Label ID="lblVendorSelectStateHeader" runat="server" Visible="true" Text="State:&nbsp;" /><asp:TextBox
                                        ID="txtVendorState" runat="server" Style="width: 25px;" /><asp:Label ID="lblVendorSelectState"
                                            runat="server" Visible="false" />
                                    <asp:Label ID="lblVendorSelectZipHeader" runat="server" Visible="true" Text="Zip:&nbsp;" /><asp:TextBox
                                        ID="txtVendorZip" runat="server" Style="width: 50px;" /><asp:Label ID="lblVendorSelectZip"
                                            runat="server" Visible="false" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Phone:</div>
                                <div class="vendorItem formItem">
                                    <asp:TextBox ID="txtVendorPhone" runat="server" /><asp:Label ID="lblVendorSelectPhone"
                                        runat="server" Visible="false" />
                                    <asp:Label ID="lblVendorSelectPhoneExtnHeader" runat="server" Visible="true" Text="ext." />
                                    <asp:TextBox ID="txtVendorPhoneExtn" runat="server" /><asp:Label ID="lblVendorSelectPhoneExtn"
                                        runat="server" Visible="false" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Web Address:</div>
                                <div class="vendorItem formItem">
                                    <asp:TextBox ID="txtVendorWebAddress" runat="server" Style="width: 175px;" /><asp:Label
                                        ID="lblVendorSelectWebAddress" runat="server" Visible="false" /></div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Terms:
                                </div>
                                <div>
                                    <asp:TextBox ID="txtVendorSelectTerms" runat="server" style="width:175px;" />
                                    <asp:Label ID="lblVendorSelectTerms" runat="server" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Active:</div>
                                <div class="vendorItem formItem">
                                    <asp:CheckBox ID="chkVendorActive" runat="server" CssClass="smallText" /></div>
                            </div>
                            <div style="text-align: right; padding-top: 5px;">
                                <asp:Button ID="btnVendorAdd" runat="server" CssClass="smallText btnAddHide" Text="Add"
                                    OnClick="btnVendorAdd_Click" />
                                <asp:Button ID="btnVendorSelect" runat="server" CssClass="smallText" Text="Select"
                                    OnClick="btnVendorSelect_Click" />
                                <asp:Button ID="btnVendorReset" runat="server" CssClass="smallText" Text="Reset"
                                    OnClick="btnVendorReset_Click" />
                                <asp:Button ID="btnVendorCancel" runat="server" CssClass="smallText" Text="Cancel"
                                    OnClick="btnVendorCancel_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="lbChangeVendor" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpShipToSelect" runat="server">
            <Content>
                <asp:UpdatePanel ID="upShipTo" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="shipToModal">
                            <h3>
                                Ship To:</h3>
                            <div class="error">
                                <asp:Label ID="Label1" runat="server" Visible="false" />
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Campus:</div>
                                <div class="vendorItem formItem">
                                    <asp:DropDownList ID="ddlShipToCampus" runat="server" CssClass="smallText" OnSelectedIndexChanged="ddlShipToCampus_IndexChanged"
                                        AutoPostBack="true" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Name:</div>
                                <div class="vendorItem formItem">
                                    <asp:TextBox ID="txtShipToName" runat="server" Style="width: 175px;" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Attention:</div>
                                <div class="vendorItem formItem">
                                    <asp:TextBox ID="txtShipToAttention" runat="server" Style="width: 175px;" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="vendorHeader formLabel">
                                    Address:</div>
                                <div class="vendorItem formItem">
                                    <span style="width: 70px;">Street:</span>
                                    <asp:TextBox ID="txtShipToAddress" runat="server" Style="width: 175px;" />
                                    <br />
                                    <span style="width: 80px;">City:</span>
                                    <asp:TextBox ID="txtShipToCity" runat="server" Style="width: 75px;" />
                                    State:&nbsp;<asp:TextBox ID="txtShipToState" runat="server" Style="width: 25px;" />
                                    Zip:&nbsp;<asp:TextBox ID="txtShipToZip" runat="server" Style="width: 75px;" />
                                </div>
                            </div>
                            <div style="text-align: right; padding-top: 5px;">
                                <asp:Button ID="btnShipToSubmit" runat="server" CssClass="smallText" Text="Select"
                                    OnClick="btnShipToSubmit_Click" />
                                <asp:Button ID="btnShipToReset" runat="server" CssClass="smallText" Text="Reset"
                                    OnClick="btnShipToReset_Click" />
                                <asp:Button ID="btnShipToCancel" runat="server" CssClass="smallText" Text="Cancel"
                                    OnClick="btnShipToCancel_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="lbChangeShipTo" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpChooseRequisition" runat="server" Title="Choose Requisition">
            <Content>
                <asp:UpdatePanel ID="upChooseRequisition" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div class="alert alert-danger" ID="lblChooseRequisitionError" runat="server" Visible="false" />
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockDropDownList runat="server" ID="ddlChooseRequisitionMinistry" Label="Ministry"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlChooseRequisitionMinistry_IndexChanged" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockDropDownList runat="server" ID="ddlChooseRequisitionRequester" Label="Requester"
                                    AutoPostBack="true" OnSelectedIndexChanged="ddlChooseRequisitionRequester_IndexChanged" />
                            </div>
                        </div>
                        <div style="width:100%; max-height:350px; overflow-y:auto; overflow-x:hidden">
                            <Rock:Grid ID="dgChooseRequisitions" runat="server" CssClass="list" AllowSorting="false"
                                AllowPaging="false" ExportEnabled="false" OnRowSelected="btnChooseRequisitionSubmit_Click" 
                                DataKeyNames="RequisitionID" >
                                <Columns>
                                    <Rock:RockBoundField DataField="Title" HeaderText="Title" />
                                    <Rock:RockBoundField DataField="RequesterName" HeaderText="Requester" />
                                    <Rock:RockBoundField DataField="DateSubmitted" HeaderText="Submitted On" />
                                    <Rock:BoolField DataField="IsApproved" HeaderText="Approved" />
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="dgChooseRequisitions" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpRequisitionItems" runat="server" Title="Choose Items" SaveButtonText="Add To PO" OnSaveClick="btnRequisitionItemsAddToPO_Click">
            <Content>
                <asp:UpdatePanel ID="upRequisitionItems" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="chooseRequisitionItem">
                            <asp:HiddenField ID="hfRequisitionID" runat="server" />
                            <div class="alert alert-danger" ID="lblRequisitionItemError" runat="server" Visible="false" />
                            
                            <div class="row">
                                <div class="col-md-6">
                                    <Rock:RockLiteral Label="Requester" ID="lblRequisitionItemsRequester" runat="server" />
                                </div>
                                <div class="col-md-6">
                                     <Rock:RockLiteral Label="Title" ID="lblRequisitionItemsTitle" runat="server" />
                                </div>
                            </div>
                            <div style="width:98%; max-height:350px; overflow-y:auto; overflow-x:hidden;">
                                <Rock:Grid ID="dgRequisitionItems" runat="server" CssClass="list" DataKeyField="ItemID"
                                    AllowSorting="false" AllowPaging="false" DataKeyNames="ItemID" OnRowDataBound="dgRequisitionItems_ItemDataBound" ShowActionRow="false">
                                    <Columns>
                                        <Rock:RockBoundField DataField="ItemID" HeaderText="Item ID" />
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
                                                <asp:TextBox ID="txtRequisitionItemQtyRemaining" runat="server" Visible="false" Style="width: 25px;" />
                                                <asp:Label ID="lblRequisitionItemQtyRemaining" runat="server" CssClass="smallText"
                                                    Visible="false" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                        <Rock:RockTemplateField ItemStyle-Width="15%">
                                            <HeaderTemplate>
                                                Price</HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtRequisitionItemPrice" runat="server" CssClass="smallText" Visible="false"
                                                    Style="width: 50px;" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpOrderSubmit" runat="server">
            <Content>
                <asp:UpdatePanel ID="upOrderSubmit" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="orderSubmit">
                        <h3>Submit Order</h3>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:25%; float:left;">
                                    Order Date:
                                </div>
                                <div class="formItem" style="width:75%; float: left;">
                                    <Rock:DatePicker ID="txtDateSubmitted" runat="server" />
                                </div>
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:25%; float: left;">
                                    Order Notes:
                                </div>
                                <div class="formItem" style="width:75%; float:left;">
                                    <asp:TextBox ID="txtOrderNotes" runat="server" TextMode="MultiLine" style="width:250px; height:75px;" />
                                </div>
                            </div>
                            <div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="btnOrderSubmit" runat="server" Text="Submit Order" CssClass="smallText" OnClick="btnOrderSubmit_Click" />
                                <asp:Button ID="btnOrderSubmitCancel" runat='server' Text="Cancel" CssClass="smallText" OnClick="btnOrderSubmitCancel_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:AsyncPostBackTrigger ControlID="lbToolbarOrder" />
                    </Triggers>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpReceivePackage" runat="server">
            <Content>
                <asp:UpdatePanel ID="upReceivePackage" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="receivePackage">
                            <asp:HiddenField ID="hfReceiptID" runat="server" />
                            <h3>Receive Package</h3>
                            <div class="error">
                                <asp:Label ID="lblReceivePackageError" runat="server" Visible="false" />
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:20%; float:left;">Carrier</div>
                                <div class="formItem" style="width:80%; float:left;"><asp:DropDownList ID="ddlReceivePackageCarriers" runat="server" CssClass="smallText" />
                                    <asp:Label ID="lblReceivePackageCarriers" runat="server" Visible="false" /></div>
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:20%; float:left;">Date Received</div>
                                <div class="formItem" style="width:80%; float:left;"><Rock:DatePicker ID="txtReceivePackageDateReceived" runat="server" CssClass="smallText" />
                                    <asp:Label ID="lblReceivePackageDateReceived" runat="server" />
                                 </div>
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:20%; float:left;">Received By</div>
                                <div class="formItem" style="width:80%; float:left;">
                                    <asp:DropDownList ID="ddlReceivedByUser" runat="server" AutoPostBack="true" CssClass="smallText" OnSelectedIndexChanged="ddlReceivedByUser_SelectedIndexChanged" style="max-width:95%;" /><asp:Label ID="lblRecevedByUser" runat="server" />
                                </div>
                            </div>
                            <asp:Panel id="pnlOtherReceiver" runat="server" class="vendorRow" Visible="false" >
                                <div class="formLabel" style="width:20%; float:left;">Other Receiver</div>
                                <div class="formItem" style="width:80%; float:left; " >
                                    <asp:Label ID="lblOtherReceiverName" runat="server" CssClass="smallText" />
                                    <asp:LinkButton ID="lbOtherReceiverRemove" runat="server" OnClick="lbOtherReceiverRemove_Click" style="text-decoration: none;">
                                        <img style="border:0px;" alt="Remove" src="/images/delete.png" />
                                    </asp:LinkButton>
                                    <asp:Button ID="btnOtherReceiverModalShow" runat="server" OnClick="btnOtherReceiverModalShow_Click" Text="..." CssClass="smallText"  />
                                    <asp:HiddenField ID="hfOtherReceiverPersonID" runat="server" />
                                    <asp:Button ID="btnOtherReceiverSelect" runat="server" OnClick="btnOtherReceiverSelect_Click" style="visibility:hidden; display:none;" />
                                </div>
                            </asp:Panel>

                            <div class="vendorRow" style="margin-top:5px; padding-top:5px;max-height:350px;width:100%;overflow-x:hidden;overflow-y:auto;">
                                <Rock:Grid ID="dgReceivePackageItems" runat="server" CssClass="list" OnItemDataBound="dgReceivePackageItems_ItemDataBound" AllowPaging="false"  OnReBind="dgReceivePackageItems_Rebind">
                                    <Columns>
                                        <Rock:RockBoundField HeaderText="POItemId" DataField="POItemId" Visible="false" />
                                        <Rock:RockBoundField HeaderText="Qty Ordered" DataField="QtyOrdered" ItemStyle-Width="50px" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center"/>
                                        <Rock:RockBoundField HeaderText="Item Number" DataField="ItemNumber" ItemStyle-Width="100px"/>
                                        <Rock:RockBoundField HeaderText="Description" DataField="Description" ItemStyle-Width="175px" ItemStyle-CssClass="wrap" />
                                        <Rock:RockBoundField HeaderText="Date Needed" DataField="DateNeeded" ItemStyle-Width="75px" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                                        <Rock:RockBoundField HeaderText="Deliver To" DataField="DeliverTo" ItemStyle-Width="100px" />
                                        <Rock:RockBoundField HeaderText="Prev Received" DataField="PreviouslyReceived" ItemStyle-Width="50px" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center"  />
                                        <Rock:RockTemplateField HeaderText="Recieving" ItemStyle-Width="50px">
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtReceivePackageQtyReceiving" runat="server" CssClass="samllText" Visible="false" style="width:95%;" />
                                                <asp:Label ID="lblReceivePackageQtyReceiving" runat="server" Visible="false" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                    </Columns>
                                </Rock:Grid>
                                <secc:StaffPicker ID="ucStaffSearch" runat="server" AllowMultipleSelections="false" />
                            </div>
                            <div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="btnReceivePackageSubmit" runat="server" CssClass="smallText" Text="Receive" OnClick="btnReceivePackageSubmit_Click" />
                                <asp:Button ID="btnReceivePackageReset" runat="server" CssClass="smallText" Text="Reset" OnClick="btnReceivePackageReset_Click" />
                                <asp:Button ID="btnReceivePackageCancel" runat="server" CssClass="smallText" Text="Cancel" OnClick="btnReceivePackageCancel_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpPayments" runat="server" DefaultButtonControlID="btnPaymentMethodPaymentAdd" CancelControlID="btnPaymentMethodPaymentClose">
            <Content>
                <asp:UpdatePanel ID="upPayment" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="paymentDetails">
                            <h3>Payment Details</h3>
                            <asp:HiddenField ID="hfPaymentID" runat="server" />
                            <div class="error">
                                <asp:Label ID="lblPaymentMethodError" runat="server" />
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:40%; float:left;">Payment Method:</div>
                                <div class="formItem" style="width:60%; float:left;"><asp:DropDownList ID="ddlPaymentMethodPaymentType" runat="server" CssClass="smallText" /><asp:Label ID="lblPaymentMethodPaymentType" runat="server" Visible="false" /></div>
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:40%; float:left;">Invoice Date:</div>
                                <div class="formItem" style="width:60%; float:left;"><Rock:DatePicker ID="txtPaymentMethodPaymentDate" runat="server" /><asp:Label ID="lblPaymentMethodPaymentDate" runat="server" Visible="false" /></div>
                            </div>
                            <div class="vendorRow">
                                <div class="formLabel" style="width:40%; float:left;">Payment Amount:</div>
                                <div class="formItem" style="width:60%; float:left;"><asp:TextBox ID="txtPaymentMethodPaymentAmount" runat="server" /><asp:Label ID="lblPaymentMethodPaymentAmount" runat="server" Visible="false" /></div>
                            </div>
                            <div id="divPaymentMethodCharges" runat="server">
                                <h4>Charges</h4>
                                <Rock:Grid ID="dgPaymentDetailCharges" runat="server" CssClass="list" AllowPaging="false" AllowSorting="false" OnItemDataBound="dgPaymentDetialCharges_ItemDataBound" >
                                    <Columns>
                                        <Rock:RockBoundField DataField="RequisitionID" HeaderText="RequisitionID" Visible="false" />
                                        <Rock:RockBoundField DataField="CompanyID" HeaderText="CompanyID" Visible="false" />
                                        <Rock:RockBoundField DataField="AccountNumber" HeaderText="AccountNumber" Visible="false" />
                                        <Rock:RockBoundField DataField="FiscalYearStart" HeaderText="FiscalYearStart" Visible="false" />
                                        <Rock:RockBoundField DataField="Requisition" HeaderText="Requisition" Visible="true" ItemStyle-Width="40%" />
                                        <Rock:RockBoundField DataField="AccountNumber" HeaderText="Account" Visible="true" ItemStyle-Width="30%" />
                                        <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" ItemStyle-Width="30%">
                                            <HeaderTemplate>Charge Amount</HeaderTemplate>
                                            <ItemTemplate>
                                                <asp:TextBox ID="txtChargeAmount" runat="server" Visible="false" style="width:80%; text-align:right;" />
                                                <asp:Label id="lblChargeAmount" runat="server" Visible="false" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                    </Columns>
                                </Rock:Grid>
                            </div>
                            <div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="btnPaymentMethodPaymentAdd" runat="server" CssClass="smallText" Text="Add Payment" OnClick="btnPaymentMethodPaymentAdd_Click" />
                                <asp:Button ID="btnPaymentMethodPaymentReset" runat="server" CssClass="smallText" text="Reset" OnClick="btnPaymentMethodPaymentReset_Click" />
                                <asp:Button ID="btnPaymentMethodPaymementAddCharges" runat="server" CssClass="smallText" Text="Update Charges" OnClick="btnPaymentMethodPaymentUpdateCharges_Click" />
                                <asp:Button ID="btnPaymentMethodPaymentResetCharges" runat="server" CssClass="smallText" Text="Reset Charges" OnClick="btnPaymentMethodPaymentResetCharges_Click" />
                                <asp:Button ID="btnPaymentMethodPaymentClose" runat="server" CssClass="smallText" Text="Close" OnClick="btnPaymentMethodPaymentClose_Click" />

                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mpItemDetails" runat="server">
            <Content>
                <asp:UpdatePanel ID="upItemDetails" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div id="itemDetails">
                            <asp:HiddenField ID="hfIDItemID" runat="server" />
                            <h3>Item Details</h3>
                            <div class="error">
                                <asp:Label ID="lblIDError" runat="server" Visible="false" />
                            </div>
                            <h4>Summary</h4>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%;float:left;" class="formLabel">Item #</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Label ID="lblIDItemNumber" runat="server" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Description</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Label ID="lblIDDescription" runat="server" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Date Needed</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Label ID="lblIDDateNeeded" runat="server" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Expedite</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Image ID="imgIDExpedite" AlternateText="Yes" runat="server" Visible="false" ImageUrl="~/images/check.gif" /> &nbsp;</div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Qty Assigned</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:TextBox ID="txtIDQtyAssigned" runat="server" style="width:20%;" CssClass="smallText" Visible="false" /><asp:Label ID="lblIDQtyAssigned" runat="server" Visible="true" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel"><asp:Label ID="lblIDReceivedUnassignedHeader" runat="server" /></div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Label ID="lblIDReceivedUnassignedValue" runat="server" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Acct #</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:Label ID="lblIDAccount" runat="server" /></div>
                            </div>
                            <div class="vendorRow" style="overflow:hidden; padding-top:3px;">
                                <div style="width:40%; float:left;" class="formLabel">Price</div>
                                <div style="width:60%; float:left;" class="formItem"><asp:TextBox ID="txtIDPrice" runat="server" Visible="false" style="width:20%;" /><asp:Label ID="lblIDPrice" runat="server" Visible="true" /></div>
                            </div>    
                            <h4>Receipts</h4>
                            <Rock:Grid ID="dgIDReceipts" runat="server" CssClass="list">
                                <Columns>
                                    <Rock:RockBoundField HeaderText="ReceiptID" DataField="ReceiptID" Visible="false" />
                                    <Rock:RockBoundField HeaderText="Date Received" DataField="DateReceived" />
                                    <Rock:RockBoundField HeaderText="Carrier" DataField="Carrier" />
                                    <Rock:RockBoundField HeaderText="Qty Received" DataField="QtyReceived" />
                                    <Rock:RockBoundField HeaderText="Received By" DataField="ReceivedBy" />
                                </Columns>
                            </Rock:Grid>
                            <div style="text-align:right; padding-top:3px;">
                                <asp:Button ID="btnIDUpdate" runat="server" CssClass="smallText" Text="Update" OnClick="btnIDUpdate_Click" />
                                <asp:Button ID="btnIDReset" runat="server" CssClass="smallText" Text="Reset" OnClick="btnIDReset_Click" />
                                <asp:Button ID="btnIDClose" runat="server" CssClass="smallText" Text="Close" OnClick="btnIDClose_Click" />
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalIFrameDialog ID="mpiDocumentChooser" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>
