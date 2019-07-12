<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPublishList.ascx.cs" Inherits="RockWeb.Plugins.GroupManager.GroupPublishList" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:Grid runat="server" ID="gGroups">
            <Columns>
                <Rock:RockBoundField HeaderText="Group" DataField="Group.Name" />
                <Rock:PersonField HeaderText="Requestor" DataField="ContactPersonAlias.Person.FullName" />
                <Rock:DateField HeaderText="Start Date" DataField="StartDateTime" />
                <Rock:DateField HeaderText="End Date" DataField="EndDateTime" />
                <Rock:EnumField HeaderText="Status" DataField="PublishGroupStatus" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
