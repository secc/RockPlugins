<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomAttendance.ascx.cs" Inherits="RockWeb.Plugins.org_secc.RoomManager.RoomAttendance" %>

<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <ul class="nav nav-tabs">
            <li class="active" runat="server" id="liCheckin">
                <Rock:BootstrapButton runat="server" ID="btnCheckin" Text="Check-In" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
            </li>
            <li runat="server" id="liCheckout">
                <Rock:BootstrapButton runat="server" ID="btnCheckout" Text="Check-Out" OnClick="btnCheckout_Click"></Rock:BootstrapButton>
            </li>
            <Rock:BootstrapButton ID="btnChangeLocation" runat="server" OnClick="btnChangeLocation_Click" CssClass="pull-right btn  btn-default" Text="<i class='fa fa-arrow-right'></i>"></Rock:BootstrapButton><Rock:LocationPicker runat="server" CurrentPickerMode="Named" AllowedPickerModes="Named" ID="lpLocation" CssClass="pull-right"/>
        </ul>
        <asp:Panel ID="pnlCheckin" runat="server">
            <asp:PlaceHolder ID="phCheckin" runat="server"></asp:PlaceHolder>
        </asp:Panel>
        <asp:Panel ID="pnlCheckout" runat="server" Visible="false">
            <asp:PlaceHolder ID="phCheckout" runat="server"></asp:PlaceHolder>
        </asp:Panel>

    </ContentTemplate>
</Rock:RockUpdatePanel>