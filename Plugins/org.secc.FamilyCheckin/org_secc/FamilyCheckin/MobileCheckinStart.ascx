<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MobileCheckinStart.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.MobileCheckinStart" %>


<script src="/SignalR/hubs"></script>
<style>
    .loading {
        text-align: center;
        margin-top: 40%;
    }

        .loading div {
            margin: auto;
        }

    .lds-ellipsis {
        display: inline-block;
        position: relative;
        width: 80px;
        height: 80px;
    }

        .lds-ellipsis div {
            position: absolute;
            top: 33px;
            width: 13px;
            height: 13px;
            border-radius: 50%;
            background: #fff;
            animation-timing-function: cubic-bezier(0, 1, 1, 0);
        }

            .lds-ellipsis div:nth-child(1) {
                left: 8px;
                animation: lds-ellipsis1 0.6s infinite;
            }

            .lds-ellipsis div:nth-child(2) {
                left: 8px;
                animation: lds-ellipsis2 0.6s infinite;
            }

            .lds-ellipsis div:nth-child(3) {
                left: 32px;
                animation: lds-ellipsis2 0.6s infinite;
            }

            .lds-ellipsis div:nth-child(4) {
                left: 56px;
                animation: lds-ellipsis3 0.6s infinite;
            }

    @keyframes lds-ellipsis1 {
        0% {
            transform: scale(0);
        }

        100% {
            transform: scale(1);
        }
    }

    @keyframes lds-ellipsis3 {
        0% {
            transform: scale(1);
        }

        100% {
            transform: scale(0);
        }
    }

    @keyframes lds-ellipsis2 {
        0% {
            transform: translate(0, 0);
        }

        100% {
            transform: translate(24px, 0);
        }
    }

    @media screen and (orientation:portrait) {
        .qr-code {
            margin: auto;
            padding: 1em;
            width: 50%; 
        }

    }
    @media screen and (orientation:landscape) {
        .qr-code {
            float: left;
            height: 50%;
            padding: 3em 2em;
            width: 30%;
        }
        .check-in-text{
            float: right;
            padding: 10% 0;
            width: 70%;
        }

    }

</style>

<script>
    var processMobileCheckin = function (nextUrl) {
        $.ajax({
            url: "/api/org.secc/familycheckin/ProcessMobileCheckin/param",
            dataType: "text",
            success: function (data) {
                window.location.href = nextUrl;
            },
            error: function (data) {
                alert("We're sorry, there was an issue with your request. Please see an attendance volunteer at your campus for assistance.");
                setTimeout(function () { window.location = window.location }, 5000);
            }
        });
    }

    var refreshPage = function () {
        window.location = "javascript:WebForm_DoPostBackWithOptions(new WebForm_PostBackOptions('<%= lbRefresh.UniqueID %>', '', true, '', '', false, true))";
    }
</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click" />
        <asp:Label ID="lblActiveWhen" runat="server" CssClass="active-when hidden" />
        <asp:Panel runat="server" Visible="false" ID="pnlError">
            <asp:Literal runat="server" ID="ltError" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlSelectCampus" Visible="false">
            <asp:Literal runat="server" ID="lIntroduction" />
            <Rock:RockDropDownList runat="server" ID="ddlCampus" DataTextField="Text"
                DataValueField="Value" AutoPostBack="true" OnSelectedIndexChanged="ddlCampus_SelectedIndexChanged" />
            <br />
            <Rock:RockLiteral runat="server" ID="lCampusLava" />
            <asp:LinkButton runat="server" ID="btnSelectCampus" Text="Begin Check-in" CssClass="se-btn se-btn--app-highlight btn-block btn-select" OnClick="btnSelectCampus_Click" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlQr" Visible="false">
            <div class="row">
                <asp:Literal runat="server" ID="ltCodeInstructions" />
                <div class="qr-code">
                    <asp:Image runat="server" ID="iQr" CssClass="img-responsive" />
                </div>
                <div class="text-center check-in-text">
                    Your check-in place will be held until <b>
                        <asp:Literal runat="server" ID="ltValidUntil" /></b>.
                    <br />
                    After this time, you can still check-in, but reservations may be released if rooms fill.
                    <br />
                    <br />
                    <asp:LinkButton Text="Cancel My Check-in Reservation" runat="server" ID="btnCancelReservation"
                        OnClick="btnCancelReservation_Click" CssClass="se-btn se-btn--app-highlight" />
                </div>
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlLoading" Visible="false">
            <div class="loading">
                <div class="lds-ellipsis">
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                </div>
                <br />
                Loading your check-in options...
               
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlPostCheckin" Visible="false">
            <div>
                <asp:Literal runat="server" ID="ltPostCheckin" />
            </div>
            <div>
                <asp:Literal runat="server" ID="ltAttendance" />
            </div>
            <Rock:BootstrapButton runat="server" ID="btnNewCheckin" CssClass="se-btn se-btn--app-highlight btn-block"
                Text="New Mobile Check-in" OnClick="btnNewCheckin_Click" />
        </asp:Panel>

        <asp:Timer ID="RefreshNotiTimer" runat="server" Interval="60000" OnTick="RefreshNotiTimer_Tick">
        </asp:Timer>
    </ContentTemplate>
</asp:UpdatePanel>
