<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PublishGroupList.ascx.cs" Inherits="RockWeb.Plugins.GroupManager.PublishGroupList" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:Grid runat="server" ID="gGroups" OnRowSelected="gGroups_RowSelected" DataKeyNames="Id">
            <Columns>
                <Rock:RockBoundField HeaderText="Group" DataField="Group.Name" />
                <Rock:PersonField HeaderText="Contact Person" DataField="ContactPersonAlias.Person.FullName" />
                <Rock:DateField HeaderText="Start Date" DataField="StartDateTime" />
                <Rock:DateField HeaderText="End Date" DataField="EndDateTime" />
                <Rock:EnumField HeaderText="Status" DataField="PublishGroupStatus" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
