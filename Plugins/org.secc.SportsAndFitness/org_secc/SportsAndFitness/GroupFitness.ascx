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
<style>
    hr {
        visibility:hidden;
    }
</style>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maError" runat="server" />
        <asp:Panel runat="server" ID="pnlSearch">
            <h1>Welcome!</h1>
            <h2>Please scan your card to sign into a group fitness class.</h2>
            <Rock:RockTextBox ID="tbPhone" CssClass="checkin-phone-entry" autocomplete="off" runat="server" style="opacity:0; height:0px;"/>
            <asp:LinkButton runat="server" ID="lbSearch" OnClick="lbSearch_Click" Text="SEARCH" style="opacity:0"></asp:linkbutton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNotFound" Visible="false">
           <Rock:NotificationBox runat="server" ID="nbNotFound" NotificationBoxType="Warning">
               <h2>Person Not Found</h2>
               We're sorry, we could not find your information in our system. If you think this is in error please
               contact one of our volunteers or staff.
           </Rock:NotificationBox>
            <Rock:BootstrapButton runat="server" ID="btnBack" CssClass="btn btn-danger" Text="Back" OnClick="btnCancel_Click"></Rock:BootstrapButton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlCheckin" Visible="false" CssClass="well">
            <h1>
                Welcome <asp:Literal runat="server" ID="ltNickName" />
            </h1>
            <h2>You have <asp:Literal runat="server" ID="ltSessions"/> sessions remaining.</h2>
            <br /><br />
            <Rock:NotificationBox runat="server" ID="nbNotOpen" NotificationBoxType="Info" Visible="false">There are no current group fitness sessions available to check-in at this time</Rock:NotificationBox>
            <asp:PlaceHolder runat="server" id="phClasses"/>
            <Rock:BootstrapButton runat="server" ID="btnCheckin" Visible="false" CssClass="btn btn-success btn-lg" Text="Check-In To Class" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
            <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-danger btn-lg" Text="Back" OnClick="btnCancel_Click"></Rock:BootstrapButton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlDone" Visible="false">
            <Rock:NotificationBox ID="nbText" NotificationBoxType="Success" runat="server">
                <h1>Welcome!</h1>
                We are preparing your name tag now. Thank you for joining us today.
            </Rock:NotificationBox>
            <Rock:BootstrapButton runat="server" ID="btnDone" OnClick="btnCancel_Click"></Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
        <Triggers>
        <asp:AsyncPostBackTrigger  ControlID="Timer1" EventName="Tick" />
    </Triggers>
</asp:UpdatePanel>
<asp:Timer ID="Timer1" runat="server" Interval="300000" OnTick="Timer1_Tick">
</asp:Timer>