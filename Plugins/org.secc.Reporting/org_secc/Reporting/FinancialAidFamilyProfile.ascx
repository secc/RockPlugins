<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FinancialAidFamilyProfile.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.FinancialAidFamilyProfile" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:Grid runat="server" ID="gFamilyProfile" AllowSorting="False" AllowPaging="False" ShowActionRow="false" ShowActionsInHeader="false">
            <Columns>
                <asp:BoundField DataField="FirstName" HeaderText="First Name" SortExpression="FirstName" />
                <asp:BoundField DataField="LastName" HeaderText="Last Name" SortExpression="LastName" />
                <asp:BoundField DataField="Event" HeaderText="Event" SortExpression="Event" />
                <asp:BoundField DataField="Campus" HeaderText="Campus" SortExpression="Campus" />
                <asp:BoundField DataField="ApplicationYear" HeaderText="Application Year" SortExpression="ApplicationYear" />
                <asp:BoundField DataField="Status" HeaderText="Status" SortExpression="Status" />
                <asp:BoundField DataField="DiscountCode" HeaderText="Discount Code" SortExpression="DiscountCode" />
                <asp:BoundField DataField="DiscountAmount" HeaderText="Amount" SortExpression="DiscountAmount" DataFormatString="{0:C}" />
                <asp:HyperLinkField DataNavigateUrlFormatString="/CampFinancialAid/139/{0}"  DataNavigateUrlFields="Id"  Text="Application" Target="_blank" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>