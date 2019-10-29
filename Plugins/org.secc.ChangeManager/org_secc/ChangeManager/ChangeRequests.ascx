<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChangeRequests.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.ChangeRequests" %>
<asp:UpdatePanel runat="server" ID="upContent">
    <ContentTemplate>
        <Rock:GridFilter runat="server" ID="fRequests" OnApplyFilterClick="fRequests_ApplyFilterClick">
            <Rock:EntityTypePicker runat="server" ID="pEntityType" Label="Entity Type" />
            <Rock:RockCheckBox runat="server" ID="cbShowComplete" Label="Show Reviewed Requests" />
        </Rock:GridFilter>
        <Rock:Grid runat="server" ID="gRequests" DataKeyNames="Id" OnRowSelected="gRequests_RowSelected">
            <Columns>
                <Rock:RockBoundField HeaderText="Entity Name" DataField="Name" />
                <Rock:RockBoundField HeaderText="Entity Type" DataField="EntityType" />
                <Rock:RockBoundField HeaderText="Requestor" DataField="Requestor" />
                <Rock:DateTimeField HeaderText="Requested" DataField="Requested" />
                <Rock:BoolField HeaderText="Was Applied" DataField="Applied" />
                <Rock:BoolField HeaderText="Was Reviewed" DataField="WasReviewed" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
