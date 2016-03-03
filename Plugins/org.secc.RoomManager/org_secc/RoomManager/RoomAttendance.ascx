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



<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <ul class="nav nav-tabs">
            <li class="active" runat="server" id="liCheckin">
                <Rock:BootstrapButton runat="server" ID="btnCheckin" Text="Check-In" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
            </li>
            <li runat="server" id="liCheckout">
                <Rock:BootstrapButton runat="server" ID="btnCheckout" Text="Check-Out" OnClick="btnCheckout_Click"></Rock:BootstrapButton>
            </li>
            <li runat="server" id="liTagSearch">
                <Rock:BootstrapButton runat="server" ID="btnShowSearch" Text="Search By Tag" OnClick="btnShowSearch_Click"></Rock:BootstrapButton>
            </li>
            <li>
                <Rock:ButtonDropDownList Title="Select Location" ID="ddlLocation" OnSelectionChanged="ddlLocation_SelectionChanged" style="display:inline" runat="server"></Rock:ButtonDropDownList>
            </li>
        </ul>
        <asp:HiddenField ID="hfReloader"  runat="server"/>
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
    </ContentTemplate>
</Rock:RockUpdatePanel>