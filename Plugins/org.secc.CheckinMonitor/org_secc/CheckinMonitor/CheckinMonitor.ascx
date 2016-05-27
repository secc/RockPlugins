<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinMonitor.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.CheckinMonitor" %>

<asp:UpdatePanel ID="upDevice" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <asp:PlaceHolder runat="server" ID="phContent" />
    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger  ControlID="Timer1" EventName="Tick" />
    </Triggers>
</asp:UpdatePanel>

<asp:Timer ID="Timer1" runat="server" Interval="5000" OnTick="Timer1_Tick">
</asp:Timer>