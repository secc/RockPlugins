<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CacheTagsCheckList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.CacheTagsCheckList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-body">
                <Rock:NotificationBox ID="nbMessage" runat="server" Visible="false" Dismissable="true" />
                <Rock:RockCheckBoxList ID="cbl" runat="server" Label="Cache Tags" />
                <asp:LinkButton ID="btnClearCache" runat="server"  OnClick="btnClearCache_Click" CausesValidation="false">
                    <i class="fa fa-repeat"></i> Clear Cache
                </asp:LinkButton>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>