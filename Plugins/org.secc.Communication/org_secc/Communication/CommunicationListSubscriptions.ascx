<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunicationListSubscriptions.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.CommunicationListSubscriptions" %>
<asp:UpdatePanel ID="upCommunicationLists" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><Rock:RockLiteral ID="lBlockTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbMain" runat="server" NotificationBoxType="Info" Visible="false" />
                <asp:Panel ID="pnlIntroduction" runat="server" Visible ="false" >
                    <Rock:RockLiteral ID="lIntroduction" runat="server" />
                </asp:Panel>
                <asp:Panel ID="pnlLists" runat="server">
                    <asp:Repeater ID="rptLists" runat="server">
                        <ItemTemplate>
                            <h3><%# Eval("PublicName") %></h3>
                            <p><%# Eval("CommunicationList.Description") %></p>
                            <br />
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>

            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>