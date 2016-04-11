<%@ Control Language="C#" AutoEventWireup="true" CodeFile="POList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.POList" %>
<%@ Register Src="~/Plugins/org_secc/Purchasing/StaffSearch.ascx" TagPrefix="secc" TagName="StaffSearch" %>

<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <div id="divMain">
            <div id="pnlFilter">
                <div id="filterImg">
                    <img src="/images/filter.gif" border="0" alt="filter" />
                </div>
                <div id="filters">
                    <div>
                        <div class="formLabel" style="width:10%;float:left; ">
                            Status:
                        </div>                    
                        <div class="formItem" style="width:90%; float:left; margin-left:0px; margin-right: 0px;">
                            <a onclick="$('table#<%=cbListStatus.ClientID%> input[type=checkbox]').attr('checked', true);" class="link">Check All</a>&nbsp;
		                    <a onclick="$('table#<%=cbListStatus.ClientID%> input[type=checkbox]').attr('checked', false);" class="link">Uncheck All</a>
                            <asp:CheckBoxList id="cbListStatus" runat="server" RepeatColumns="6" RepeatDirection="horizontal" CssClass="smallText"  TextAlign="Right" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%;float:left;">
                            Type:
                        </div>
                        <div class="formItem" style="width:90%;float:left;">
                            <asp:CheckBoxList ID="cbListType" runat="server" RepeatColumns="6" RepeatDirection="Horizontal" CssClass="smallText" TextAlign="Right" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%;float:left;">
                            Vendor:
                        </div>
                        <div class="formItem" style="width:90%; float:left;">
                           <asp:DropDownList ID="ddlVendor" runat="server" CssClass="smallText" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%; float:left;">
                            PO Number:
                        </div>
                        <div class="formItem" style="width:90%; float:left;">
                            <asp:TextBox id="txtPONumber" runat="server" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%;float:left;">
                            Order Date
                        </div>
                        <div class="formItem" style="width:90%;float:left;">
                            From: <Rock:DatePicker ID="txtOrderFrom" runat="server" />
                            To: <Rock:DatePicker ID="txtOrderTo" runat="server" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%; float:left;">
                            Ordered By:
                        </div>
                        <div class="formItem" style="width:90%; float:left;">
                            <asp:HiddenField ID="hfFilterOrderedBy" runat="server" />
                            <asp:Label ID="lblFilterOrderedBy" runat="server" Text="(all)"  />
                            <asp:LinkButton ID="lbRemoveOrderedBy" runat="server" OnClick="lbRemoveOrderedBy_Click" Visible="false">
                                <img src="/images/delete.png" alt="Remove" style="border:0px; vertical-align:baseline;" /></asp:LinkButton>
                            <asp:Button ID="btnFilterOrderedBySelect" Text="..." runat="server" OnClick="btnFilterSubmittedBySelect_Click" CssClass="smallText" />
                            <asp:Button ID="btnFilterOrderedByRefresh" runat="server" OnClick="btnFilterSubmittedByRefresh_Click" style="visibility:hidden; display:none;" />
                        </div>
                    </div>
                    <div>
                        <div class="formLabel" style="width:10%; float:left;">
                            Show Inactive
                        </div>
                        <div class="formItem" style="width:90%; float:left;">
                            <asp:CheckBox ID="chkShowInactive" runat="server" />
                        </div>
                    </div>
                    <div style="width:100%;">
                        <asp:Button ID="btnFilterApply" runat="server" CssClass="smallText" Text="Apply Filter" OnClick="btnFilterApply_Click" />
                        <asp:Button ID="btnFilterReset" runat="server" CssClass="smallText" Text="Clear Filter" OnClick="btnFilterClear_Click" />
                    </div>
                </div>
                <div style="width:100%; text-align:right; ">
                    <asp:LinkButton ID="lbAddPO" runat="server" CssClass="smallText" OnClick="lbAddPO_Click" style="padding:2px; margin-right:2px;"><img src="/images/addButton.png" alt="Create" style="vertical-align:bottom; margin-right:2px; border: 0; border-style:none;" />New</asp:LinkButton>
                </div> 
            </div>    
            <Rock:Grid ID="dgPurchaseOrders" runat="server" CssClass="list" 
                OnReBind="dgPurchaseOrders_Rebind" OnItemCommand="dgPurchaseOrders_ItemCommand" >
                <Columns>
                   <Rock:RockBoundField HeaderText="PurchaseOrderID" DataField="PurchaseOrderID" Visible="false" />
                   <Rock:RockTemplateField HeaderText="PO Number">     
                        <ItemTemplate>
                            <asp:HyperLink runat="server" text='<%# Eval("PurchaseOrderID") %>' NavigateUrl='<%# String.Format("{0}?poid={1}", PurchaseOrderDetailPageSetting, Eval("PurchaseOrderID")) %>'></asp:HyperLink> 
                        </ItemTemplate>
                   </Rock:RockTemplateField>
                   <Rock:RockBoundField HeaderText="Vendor" DataField="VendorName" SortExpression="VendorName" />
                   <Rock:RockBoundField HeaderText="Type" DataField="POType" SortExpression="POType" />
                   <Rock:RockBoundField HeaderText="Status" DataField="Status" SortExpression="Status" />
                   <Rock:RockBoundField HeaderText="Items Details" DataField="ItemDetails" SortExpression="ItemDetails" />
                   <Rock:RockBoundField HeaderText="Total Payments" DataField="TotalPayments" SortExpression="TotalPayments" />
                   <Rock:RockBoundField HeaderText="Notes" DataField="NoteCount" SortExpression="NoteCount" />
                   <Rock:RockBoundField HeaderText="Attachments" DataField="AttachmentCount" SortExpression="AttachmentCount" />
                </Columns>
            </Rock:Grid>
        </div>
        <secc:StaffSearch ID="ucStaffSearch" runat="server" AllowMultipleSelections="false" ShowPersonDetailLink="false" />
    </ContentTemplate>
</asp:UpdatePanel>