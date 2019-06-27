<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChangeRequests.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.ChangeRequests" %>
<asp:UpdatePanel runat="server" ID="upContent">
    <ContentTemplate>
        <Rock:Grid runat="server" ID="gRequests" DataKeyNames="Id" OnRowSelected="gRequests_RowSelected" OnRowDataBound="gRequests_RowDataBound">
            <Columns>
                <Rock:RockBoundField HeaderText="Entity Name" DataField="Name" />
                <Rock:RockBoundField HeaderText="Entity Type" DataField="EntityType.Name" />
                <Rock:RockBoundField HeaderText="Requestor" DataField="CreatedByPersonAlias.Person.FullName" />
                <Rock:BoolField HeaderText="Complete" DataField="IsComplete" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
