<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HomeGroupAttendance.ascx.cs" Inherits="RockWeb.Blocks.Reporting.HomeGroups.HomeGroupAttendance" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:DateRangePicker runat="server" ID="dpStart" CssClass="pull-left"></Rock:DateRangePicker>
        <Rock:BootstrapButton runat="server" ID="btnGo" Text="Go" CssClass="btn btn-primary pull-left" OnClick="btnGo_Click"/>
        <asp:Panel runat="server" ID="pnlResult" Visible="false">
            <br /><br /><br />
            Number of Groups: <asp:Literal runat="server" ID="lGroups" /><br />
            Number of Members: <asp:Literal runat="server" ID="lMember" /><br />
            Groups Who Took Attendance: <asp:Literal runat="server" ID="ltGroupsAttendance" /><br />
            Total Accounted For: <asp:Literal runat="server" ID="lAccounted" /><br />
            Actual Attendance: <asp:Literal runat="server" ID="lActual" /><br />
            Attendance Percent: <asp:Literal runat="server" ID="lAttendance" />%<br />
            Extrapolated Attendance: <asp:Literal runat="server" ID="lExtrapolated" /><br />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
