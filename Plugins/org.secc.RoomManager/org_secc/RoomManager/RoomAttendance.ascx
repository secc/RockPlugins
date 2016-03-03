<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomAttendance.ascx.cs" Inherits="RockWeb.Plugins.org_secc.RoomManager.RoomAttendance" %>



<script type="text/javascript">
    var UpdPanelUpdate = function () {
        __doPostBack("<%= hfReloader.ClientID %>","");
    }

    var startTimer = function () {
        var timer = setInterval(function(){UpdPanelUpdate()}, 10000);
    }

    $(document).ready(startTimer);

</script>

<style type="text/css">
    video {
        max-width:90vw;
    }
</style>

<Rock:RockLiteral ID="ltDeviceName" runat="server"></Rock:RockLiteral>
<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-md-2 col-xs-6">
                 <Rock:ButtonDropDownList Title="Select Location" ID="ddlLocation" OnSelectionChanged="ddlLocation_SelectionChanged" runat="server"></Rock:ButtonDropDownList>
            </div>
            <div class="col-md-2 col-xs-6">
                 <Rock:BootstrapButton Visible="false" CssClass="btn btn-default" runat="server" ID="btnBarcode" Text="Barcode" OnClick="btnBarcode_Click"></Rock:BootstrapButton>
            </div>
        </div>
               
        <ul class="nav nav-tabs">
            <li class="active" runat="server" id="liCheckin">
                <Rock:BootstrapButton runat="server" ID="btnCheckin" Text="Check-In" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
            </li>
            <li runat="server" id="liCheckout">
                <Rock:BootstrapButton runat="server" ID="btnCheckout" Text="Check-Out" OnClick="btnCheckout_Click"></Rock:BootstrapButton>
            </li>
            <li runat="server" id="liTagSearch">
                <Rock:BootstrapButton runat="server" ID="btnShowSearch" Text="Move-Here" OnClick="btnShowSearch_Click"></Rock:BootstrapButton>
            </li>

        </ul>

        <asp:HiddenField ID="hfReloader"  runat="server"/>
        <div class="col-md-6 col-xs-12" id="mainDiv">
        <asp:Panel ID="pnlCheckin" runat="server">
            <asp:PlaceHolder ID="phCheckin" runat="server"></asp:PlaceHolder>
        </asp:Panel>

        <asp:Panel ID="pnlCheckout" runat="server" Visible="false">
            <asp:PlaceHolder ID="phCheckout" runat="server"></asp:PlaceHolder>
        </asp:Panel>

        <asp:Panel ID="pnlTagSearch" runat="server" Visible="false">
            <Rock:NotificationBox ID="nbSearch" runat="server" NotificationBoxType="Success"></Rock:NotificationBox>
            <b>Search By Tag Code:</b><br />
            <Rock:RockTextBox runat="server" ID="tbTagSearch" style="display:inline-block" Width="200px"></Rock:RockTextBox>
            <Rock:BootstrapButton runat="server" ID="btnTagSearch" OnClick="btnTagSearch_Click"  CssClass="btn btn-default" Text="<i class='fa fa-arrow-right'></i>"></Rock:BootstrapButton>
            <asp:PlaceHolder ID="phSearch" runat="server"></asp:PlaceHolder>
        </asp:Panel>

        <asp:Panel ID="pnlBarcode" runat="server">
        </asp:Panel>
        </div>
    </ContentTemplate>
</Rock:RockUpdatePanel>

    <div id="BarcodeActual" class="col-xs-12" style="display:none">
        <div id="interactive" class="viewport"></div>
        <script type="text/javascript" src="../plugins/org_secc/RoomManager/quagga.min.js"></script>
        <script type="text/javascript" src="../plugins/org_secc/RoomManager/live_w_locator.js"></script>
        </div>

<script type="text/javascript">
    var sendPostBack = function (code) {
        __doPostBack('<%= upMain.ClientID %>', code);
    }
</script>