<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AutoConfigure.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.AutoConfigure" %>

<script>
    var getClientName = function ()
    {
        try {
            var client = window.external.GetClientName();
            __doPostBack("ClientName", client);
        }
        catch (e)
        {
            __doPostBack("UseDNS", "");
        }
    }
</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" Visible="false" ID="pnlMain">
            <Rock:NotificationBox runat="server" NotificationBoxType="Danger" ID="nbNotFound">
        <h2>Sorry</h2>
            There was a problem with the configuration of this kiosk. Please contact an administrator to resolve.
            </Rock:NotificationBox>
            <Rock:NotificationBox runat="server" ID="nbNotConfigured" Visible="false">
        <h2>Block not configured</h2>
        This block needs to know which page to forward checkin to.
            </Rock:NotificationBox>
            <asp:Literal Text="text" ID="ltDNS" runat="server" />
        </asp:Panel>
    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="Timer1" EventName="Tick" />
    </Triggers>
</asp:UpdatePanel>
<asp:Timer ID="Timer1" runat="server" Interval="30000" OnTick="Timer1_Tick">
</asp:Timer>
