<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AutoConfigure.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.AutoConfigure" %>

<script>
    var setClientName = function (clientName) {
        __doPostBack("ClientName", clientName);
    }

    var getClientName = function () {
        try {
            try {
                window.location = window.location;
                console.log("Newer Client")
                var browserCommand = {
                    eventName: "GETCLIENT"
                };
                window.external.notify(JSON.stringify(browserCommand));
            }
            catch (e) { //Older clients
                console.log(e)
                console.log("Older Client")
                var client = window.external.GetClientName();
                setClientName(client);
            }
            
        }
        catch (e) {
            __doPostBack("UseDNS", "");
            console.log("DNS Mode")
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
