<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AutoConfigure.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.AutoConfigure" %>

<script>
    var getClientName = function () {
        try {
            var client = window.external.GetClientName();
            __doPostBack("ClientName", client);
        }
        catch (e) {
            __doPostBack("UseDNS", "");
        }
    }
</script>


<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" Visible="false" ID="pnlConfig">
            <Rock:RockTextBox runat="server" ID="tbKioskName" Label="Kiosk Name"
                Help="Please enter the name of the device as it appears on the device label." Required="true" />
            <Rock:RockDropDownList runat="server" ID="ddlKioskType"
                Label="Kiosk Type" DataTextField="Name" DataValueField="Id" Required="true" />
            <Rock:BootstrapButton ID="btnStart" runat="server" OnClick="btnStart_Click" CssClass="btn btn-primary" Text="Start" />
        </asp:Panel>


        <asp:Panel ID="pnlManual" Visible="false" runat="server">
            <Rock:PanelWidget runat="server" Expanded="true" Title="Manual Configuration">
                Please select the kiosk type to use:
                <Rock:RockDropDownList runat="server" ID="ddlManualKioskType" Label="Kiosk Type" DataTextField="Name" DataValueField="Id" />
                <Rock:BootstrapButton ID="btnSelectKiosk" runat="server" OnClick="btnSelectKiosk_Click" CssClass="btn btn-primary" Text="Select" />
            </Rock:PanelWidget>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
