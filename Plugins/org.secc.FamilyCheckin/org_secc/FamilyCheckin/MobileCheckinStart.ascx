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
            <br />
            <asp:Image runat="server" ID="iQr" CssClass="img-responsive" Style="width: 50%; margin: auto;" />
            <br />
            <div class="text-center">
                Your check-in place will be held until <b>
                    <asp:Literal runat="server" ID="ltValidUntil" /></b>.
                <br />
                After this time, you can still check-in, but reservations may be released if rooms fill.
                <br />
                <br />
                <asp:LinkButton Text="Cancel My Check-in Reservation" runat="server" ID="btnCancelReseration"
                    OnClick="btnCancelReseration_Click" CssClass="btn btn-primary" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlLoading" Visible="false">
            <div class="loading">

                <div class="secc-cube-grid">
                    <div class="secc-cube secc-cube1"></div>
                    <div class="secc-cube secc-cube2"></div>
                    <div class="secc-cube secc-cube3"></div>
                    <div class="secc-cube secc-cube4"></div>
                    <div class="secc-cube secc-cube5"></div>
                    <div class="secc-cube secc-cube6"></div>
                    <div class="secc-cube secc-cube7"></div>
                    <div class="secc-cube secc-cube8"></div>
                    <div class="secc-cube secc-cube9"></div>
                </div>
                <br />
                <br />
                Loading your check-in options...
               
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlPostCheckin" Visible="false">
            <asp:Literal runat="server" ID="ltPostCheckin" />
            <asp:Literal runat="server" ID="ltAttendance" />
            <Rock:BootstrapButton runat="server" ID="btnNewCheckin" CssClass="btn btn-default btn-block"
                Text="New Mobile Check-in" OnClick="btnNewCheckin_Click"/>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
