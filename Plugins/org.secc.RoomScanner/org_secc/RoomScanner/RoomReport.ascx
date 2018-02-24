<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomReport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.RoomScanner.RoomReport" %>

<style>
    .timelineTooltip {
        padding: 10px;
        font-family: 'Franklin Gothic Medium', 'Arial Narrow', Arial, sans-serif;
    }

    .dateUp {
        margin-top: -6px;
    }
</style>
<script type="text/javascript" src="https://www.gstatic.com/charts/loader.js"></script>

<script type="text/javascript">
    function alertAction() {
        var selection = chart.getSelection();
        console.log(selection);
        var message = '';

        for (var i = 0; i < selection.length; i++) {
            var item = selection[i];
            if (item.row != null) {
                document.location.href = "/Person/" + personLinks[item.row];
            }
        }
    }
</script>

<asp:UpdatePanel ID="pnlReport" runat="server" Class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <h4 class="pull-left">Room Scanner Report</h4>
            <div class="pull-right btn-toolbar">
                <Rock:DatePicker runat="server" ID="dpDate" OnTextChanged="dpDate_TextChanged" AutoPostBack="true" />
                <Rock:BootstrapButton runat="server" ID="btnGo" OnClick="btnGo_Click" Text="Go" CssClass="btn btn-primary dateUp" />
            </div>
        </div>
        <div class="panel-body" style="height: 700px">
            <div id="report" style="height: 700px"></div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

