<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PublishGroupList.ascx.cs" Inherits="RockWeb.Plugins.GroupManager.PublishGroupList" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:ModalAlert runat="server" ID="maAlert" />

        <Rock:GridFilter runat="server" ID="fGroups" OnApplyFilterClick="fGroups_ApplyFilterClick">
            <Rock:PersonPicker runat="server" Label="Contact Person" ID="pContactPerson" />
            <Rock:RockCheckBoxList runat="server" Label="Status" ID="cblStatus" />
        </Rock:GridFilter>

        <Rock:Grid runat="server" ID="gGroups" OnRowSelected="gGroups_RowSelected" DataKeyNames="Id">
            <Columns>
                <Rock:RockBoundField HeaderText="Group" DataField="Group.Name" />
                <Rock:PersonField HeaderText="Contact Person" DataField="ContactPersonAlias.Person.FullName" />
                <Rock:DateField HeaderText="Start Date" DataField="StartDateTime" />
                <Rock:DateField HeaderText="End Date" DataField="EndDateTime" />
                <Rock:EnumField HeaderText="Status" DataField="PublishGroupStatus" />
                <Rock:RockTemplateField HeaderText="Actions" ID="lfActions">
                    <ItemTemplate>
                        <asp:HiddenField ID="hfPublishGroupId" runat="server" Value='<%# Eval("Id") %>' />
                        <asp:HiddenField ID="hfGroupId" runat="server" Value='<%# Eval("GroupId") %>' />
                        <asp:LinkButton ID="btnLink" Text="<i class='fa fa-link'></i>" CssClass="btn btn-default" runat="server" OnClick="btnLink_Click" />
                        <asp:LinkButton ID="btnEdit" Text="<i class='fa fa-edit'></i>" CssClass="btn btn-default" runat="server" OnClick="btnEdit_Click" />
                        <asp:LinkButton ID="btnDelete" Text="<i class='fa fa-times'></i>" CssClass="btn btn-danger" runat="server"
                            OnClick="btnDelete_Click" OnClientClick="javascript: return Rock.dialogs.confirmDelete(event, 'publish group');" />
                    </ItemTemplate>
                </Rock:RockTemplateField>
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
