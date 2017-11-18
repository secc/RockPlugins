<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomReport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.RoomScanner.RoomReport" %>

<style>
    .timelineTooltip {
        padding: 10px;
        font-family: 'Franklin Gothic Medium', 'Arial Narrow', Arial, sans-serif;
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
            if (item.row != null && item.column != null) {
                message += '{row:' + item.row + ',column:' + item.column + '}';
            } else if (item.row != null) {
                message += '{row:' + item.row + '}';
            } else if (item.column != null) {
                message += '{column:' + item.column + '}';
            }
        }
        if (message == '') {
            message = 'nothing';
        }
        alert('You selected ' + message);
    }
</script>

<asp:UpdatePanel ID="pnlReport" runat="server" Class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <div class="pull-right">
                <Rock:BootstrapButton runat="server" ID="btnGo" OnClick="btnGo_Click" Text="Go" CssClass="pull-right btn btn-primary" />
                <Rock:DatePicker runat="server" ID="dpDate" CssClass="pull-right" />

            </div>
            <h4 class="pull-left">Room Scanner Report</h4>
        </div>
        <div class="panel-body" style="height: 700px">
            <div id="report" style="height: 700px"></div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

