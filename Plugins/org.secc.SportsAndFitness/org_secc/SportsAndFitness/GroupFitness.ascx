<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupFitness.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.GroupFitness" %>
<script>

    Sys.Application.add_load(function ()
    {
        $('.tenkey a.digit').click(function ()
        {
            $phoneNumber = $("input[id$='tbPhone']");
            $phoneNumber.val($phoneNumber.val() + $(this).html());
        });
        $('.tenkey a.back').click(function ()
        {
            $phoneNumber = $("input[id$='tbPhone']");
            $phoneNumber.val($phoneNumber.val().slice(0, -1));
        });
        $('.tenkey a.clear').click(function ()
        {
            $phoneNumber = $("input[id$='tbPhone']");
            $phoneNumber.val('');
            $phoneNumber.focus();
        });

        $('.checkin-phone-entry').focus();

        $('.checkin-phone-entry').blur(function ()
        {
            setTimeout(function ()
            {
                 $('.checkin-phone-entry').focus();
            }, 100)
        })
    })
    var resetTimeout;

    var startTimeout = function ()
    {
        clearTimeout(resetTimeout);
        resetTimeout = setTimeout(function () { location.reload(); }, 300000)
    }

    var stopTimeout = function ()
    {
        clearTimeout(resetTimeout);
        $('.checkin-phone-entry').focus();
    }

</script>
<style>
    hr {
        visibility: hidden;
    }

    .digit, .command, .search {
        font-size: 2em;
        height: 75px;
        padding-top: 20px;
    }

    .checkin-phone-entry {
        font-size: 3em;
        height: 70px;
        margin-bottom: 5px;
    }

    h1 {
        font-size: 4em;
    }

    h2 {
        font-size: 3em;
    }

    .btn-block {
        font-size: 2em;
    }

    .alert {
        font-size: 1.5em;
    }

    .well .btn-danger, .well .btn-success {
        height: 60px;
        font-size: 2em;
    }

    .ClassList {
        font-size: 1.5em;
    }

        .ClassList .btn {
            height: 75px;
            width: 75px;
            font-size: 2em;
        }

</style>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maError" runat="server" />
        <asp:Panel runat="server" ID="pnlSearch">
            <div class="col-sm-6">
                <asp:Panel ID="pnlMessage" runat="server">
                    <h1>Welcome!</h1>
                    <h2>Please enter your phone number to sign into a group fitness class.</h2>
                </asp:Panel>
                <asp:PlaceHolder runat="server" ID="phPeople" />
            </div>
            <div class="tenkey checkin-phone-keypad col-sm-6">
                <Rock:RockTextBox ID="tbPhone" CssClass="checkin-phone-entry" autocomplete="off" runat="server" />
                <div>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">1</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">2</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">3</a>
                </div>
                <div>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">4</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">5</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">6</a>
                </div>
                <div>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">7</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">8</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">9</a>
                </div>
                <div>
                    <a href="#" class="btn btn-default btn-lg command back col-xs-4">Back</a>
                    <a href="#" class="btn btn-default btn-lg digit col-xs-4">0</a>
                    <a href="#" class="btn btn-default btn-lg command clear col-xs-4">Clear</a>
                </div>
                <Rock:BootstrapButton runat="server" ID="lbSearch" OnClick="lbSearch_Click"
                    CssClass="btn btn-primary btn-lg col-xs-12 search" DataLoadingText="Searching.." Text="SEARCH">
                </Rock:BootstrapButton>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNotFound" Visible="false">
            <Rock:NotificationBox runat="server" ID="nbNotFound" NotificationBoxType="Warning">
               <h3>Group Fitness Record Not Found</h3>
               We're sorry, we could not find your information in our system. If you think this is in error please
               contact one of our volunteers or staff. 
            </Rock:NotificationBox>
            <Rock:BootstrapButton runat="server" ID="btnBack" CssClass="btn btn-danger btn-lg" Text="Back" OnClick="btnCancel_Click"></Rock:BootstrapButton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlCheckin" Visible="false" CssClass="well">
            <h1>Welcome
                <asp:Literal runat="server" ID="ltNickName" />
            </h1>
            <h2>You have
                <asp:Literal runat="server" ID="ltSessions" />
                sessions remaining.</h2>
            <br />
            <br />
            <Rock:NotificationBox runat="server" ID="nbNotOpen" NotificationBoxType="Info" Visible="false">There are no current group fitness sessions available to check-in at this time</Rock:NotificationBox>
            <div class="ClassList">
                <asp:PlaceHolder runat="server" ID="phClasses" />
            </div>
            <br />
            <br />
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
        <asp:AsyncPostBackTrigger ControlID="Timer1" EventName="Tick" />
    </Triggers>
</asp:UpdatePanel>
<asp:Timer ID="Timer1" runat="server" Interval="300000" OnTick="Timer1_Tick">
</asp:Timer>
