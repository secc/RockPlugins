<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LiveMonitor.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.LiveMonitor" %>

<style>
    .panel-heading {
        border-radius:0px;
    }
</style>

<script type="text/javascript">
    var timer;

    var UpdPanelUpdate = function () {
        console.log("Updating!");
        __doPostBack("<%= hfReloader.ClientID %>", "");
    }

    var startTimer = function () {
        clearInterval(timer);
        timer = setInterval(function () { UpdPanelUpdate() }, 10000);
        document.getElementById("htmlAutoRefresh").style.display = "none";
    }

    var stopTimer = function () {
        document.getElementById("htmlAutoRefresh").style.display = "block";
        clearInterval(timer);
    }
</script>

<asp:UpdatePanel ID="upDevice" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfReloader" runat="server" />
        <div class="panel panel-primary">
            <div class="panel-heading">
                <h3 class="panel-title">Check-In Live Monitor</h3>
                Check-in Rate: <i>
                <asp:Literal runat="server" ID="ltCount"></asp:Literal>
                per minute</i> / 
                CPU: <asp:Literal runat="server" ID="ltCpu"></asp:Literal>%
            </div>
            <div class="panel-body">
                <asp:PlaceHolder runat="server" ID="phAttendance" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">
    var prm = Sys.WebForms.PageRequestManager.getInstance();
    prm.add_initializeRequest(InitializeRequest);

    function InitializeRequest(sender, args) {
    }

</script>
