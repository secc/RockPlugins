<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LinkListEditUsers.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.LinkListEditUsers" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfContentChannelItemId" runat="server" />
        <asp:HiddenField ID="hfSecurityGroupId" runat="server" />
        <Rock:NotificationBox ID="nbNotification" runat="server" />
        <asp:Panel ID="pnlLinkList" runat="server" Visible="false">
            <asp:Literal ID="lListName" runat="server" />
            <Rock:Grid ID="gListUsers" runat="server" EmptyDataText="No Users Found" AllowSorting="true" RowItemText="Users">
                <Columns>
                    <asp:TemplateField HeaderText="Name" SortExpression="Person.FullNameReversed">
                        <ItemTemplate>
                            <asp:Label ID="lName" runat="server" />
                            <asp:LinkButton ID="lbRemove" runat="server" CommandName="RemoveMember" CssClass="btn btn-sm btn-danger"><i class="fa fa-times" /></asp:LinkButton>

                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </Rock:Grid>
            <Rock:ModalDialog ID="mdAddGroupMember" runat="server" Title="Add Editor">
                <Content>
                    <Rock:PersonPicker ID="ppGroupMember" runat="server" Label="Select Editor" />
                    <div class="actions">
                        <asp:LinkButton ID="lbAddGroupMember" runat="server" CssClass="btn btn-primary">Add Group Member</asp:LinkButton>
                        <asp:LinkButton ID="lbCancel" runat="server" CssClass="btn btn-cancel">Cancel </asp:LinkButton>
                    </div>
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
