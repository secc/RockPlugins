<%@ Control Language="C#" AutoEventWireup="true" CodeFile="VendorList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.VendorList" %>
<asp:UpdatePanel ID="updVendorList" runat="server" UpdateMode="Conditional" class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-columns"></i>&nbsp;Vendor List</h1>
            <asp:LinkButton ID="lbNewVendor" runat="server" CssClass="btn-add btn btn-default btn-sm pull-right" OnClick="lbNewVendor_Click"><i class="fa fa-plus"></i>New Vendor</asp:LinkButton>
        </div>
        <div class="panel-body">
        <div class="grid grid-panel">
            <Rock:GridFilter runat="server" ID="gfRequisitions" OnApplyFilterClick="btnApplyFilter_Click">
                <Rock:RockTextBox Label="Vendor Name" ID="txtFilterVendorName" runat="server" class="formItem" MaxLength="100" Columns="50" />
                <Rock:RockCheckBox label="Include Inactive" ID="chkIncludeInactive" runat="server" Checked="false" />
            </Rock:GridFilter>
            <Rock:Grid ID="dgVendors" runat="server" AllowSorting="true" AllowPaging="true" DataKeyField="vendorID" CssClass="list">
                <Columns>
                    <Rock:RockBoundField HeaderText="ID" DataField="VendorID" Visible="false" />
                    <Rock:RockTemplateField HeaderText="Name">                     
                        <ItemTemplate>
                            <asp:HyperLink runat="server" text='<%# Eval("VendorName") %>' NavigateUrl='<%# String.Format("{0}?VendorID={1}", VendorDetailPageSetting, Eval("VendorID")) %>'></asp:HyperLink> 
                        </ItemTemplate>
                    </Rock:RockTemplateField>

                    <Rock:RockTemplateField HeaderText="Address">                     
                        <ItemTemplate>
                            <%# (Eval("Address") == null) ? "" : ((org.secc.Purchasing.Helpers.Address)Eval("Address")).ToArenaFormat().Replace("^", "&nbsp;") %>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                    <Rock:RockTemplateField HeaderText="Phone">                     
                        <ItemTemplate>
                            <%# (Eval("Phone") == null) ? "" : ((org.secc.Purchasing.Helpers.PhoneNumber)Eval("Phone")).ToArenaFormat().Replace("^", "&nbsp;") %>
                        </ItemTemplate>
                    </Rock:RockTemplateField> 
                    <asp:HyperLinkField HeaderText="Web" DataNavigateUrlFields="WebAddress" DataTextField="WebAddress" Target="_blank" />
                    <Rock:RockBoundField HeaderText="Terms" DataField="Terms" />
                    <Rock:BoolField HeaderText = "Active" SortExpression="Active" DataField="Active"/>
                </Columns>
            </Rock:Grid>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>