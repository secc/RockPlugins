<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupFitness.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.GroupFitness" %>
<script>

    Sys.Application.add_load(function ()
    {
        $('.checkin-phone-entry').focus();

        $('.checkin-phone-entry').blur(function ()
        {
            setTimeout(function ()
            {
                $('.checkin-phone-entry').focus();
            }, 100)
        })
    })
</script>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maError" runat="server" />
        <asp:Panel runat="server" ID="pnlSearch">
            Please scan your card!
            <Rock:RockTextBox ID="tbPhone" CssClass="checkin-phone-entry" autocomplete="off" runat="server"/>
            <asp:LinkButton runat="server" ID="lbSearch" OnClick="lbSearch_Click" Text="SEARCH"></asp:linkbutton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNotFound" Visible="false">
           <Rock:NotificationBox runat="server" ID="nbNotFound" NotificationBoxType="Warning">
               <h2>Person Not Found</h2>
               We're sorry, we could not find your information in our system. If you think this is in error please
               contact one of our volunteers or staff.
           </Rock:NotificationBox>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlCheckin" Visible="false">
            <h1>
                Welcome <asp:Literal runat="server" ID="ltNickName" />
            </h1>
            <h2>You have <asp:Literal runat="server" ID="ltSessions"/> sessions remaining.</h2>
        </asp:Panel>
    </ContentTemplate>
        <Triggers>
        <asp:AsyncPostBackTrigger  ControlID="Timer1" EventName="Tick" />
    </Triggers>
</asp:UpdatePanel>
<asp:Timer ID="Timer1" runat="server" Interval="300000" OnTick="Timer1_Tick">
</asp:Timer>