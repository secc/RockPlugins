<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MobileCheckinStart.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.MobileCheckinStart" %>


<script src="/SignalR/hubs"></script>
<style>
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

    .loading {
        text-align: center;
        margin-top: 40%;
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
                alert("Something broke, and the developer forgot to add an error message.")
            }
        });
    }

    var refreshPage = function () {
        window.location = "javascript:WebForm_DoPostBackWithOptions(new WebForm_PostBackOptions('<%= lbRefresh.UniqueID %>', '', true, '', '', false, true))";
    }

    var checkinComplete = function () {
        window.location = "javascript:WebForm_DoPostBackWithOptions(new WebForm_PostBackOptions('<%= lbCheckinComplete.UniqueID %>', '', true, '', '', false, true))";
    }
</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:LinkButton ID="lbCheckinComplete" runat="server" OnClick="lbCheckinComplete_Click" />
        <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click" />
        <asp:Label ID="lblActiveWhen" runat="server" CssClass="active-when hidden" />
        <asp:Panel runat="server" Visible="false" ID="pnlError">
            You must be logged in to use mobile check-in.
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlTutorial" Visible="false">
            <asp:Literal runat="server" ID="lTutorial" />
            <Rock:BootstrapButton runat="server" ID="btnTutorial" Text="Start" OnClick="btnTutorial_Click" CssClass="btn btn-default btn-block" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlSelectCampus" Visible="false">
            <asp:Literal runat="server" ID="lIntroduction" />
            <Rock:RockDropDownList runat="server" ID="ddlCampus" DataTextField="Text"
                DataValueField="Value" AutoPostBack="true" OnSelectedIndexChanged="ddlCampus_SelectedIndexChanged" />
            <br />
            <Rock:RockLiteral runat="server" ID="lCampusLava" />
            <Rock:BootstrapButton runat="server" ID="btnSelectCampus" Text="Begin Check-in" CssClass="btn btn-primary btn-block btn-select" OnClick="btnSelectCampus_Click" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlQr" Visible="false">
            <asp:Literal runat="server" ID="ltCodeInstructions" />
            <br />
            <asp:Image runat="server" ID="iQr" CssClass="img-responsive" />
            <div class="text-center">
                Your check-in place will be held until <b><asp:Literal runat="server" ID="ltValidUntil" /></b>.
                <br />
                After this time, you can still check-in, but reservations may be released if rooms fill.
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlLoading" Visible="false">
            <div class="loading">
                Loading your check-in options...
            <br />
                <div class="lds-ellipsis">
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
