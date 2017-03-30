<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickCheckin" %>
<style>
    .row {
        margin-right: 0px;
        margin-left: 0px;
    }
</style>

<script>

    var showContent = function () {
        var content = document.getElementById("quickCheckinContent")
        content.style.transition = "linear .2s"
        content.style.transform = "translateX(100vw)"
        var pgt = document.getElementById("pgtSelect")
        pgt.style.transition = "linear .2s"
        pgt.style.transform = "translateX(100vw)"
    }

    var showPgt = function () {
        var content = document.getElementById("quickCheckinContent")
        content.style.transition = "linear .2s"
        content.style.transform = "translateX(0px)"
        var pgt = document.getElementById("pgtSelect")
        pgt.style.transition = "linear .2s"
        pgt.style.transform = "translateX(0px)"
    }

    var doCheckin = function () {
        setTimeout(
            function () {
                var content = document.getElementById("quickCheckinContainer");
                document.body.style.overflow = "hidden";
                content.style.transitionDuration = "0.2s";
                content.style.transform = "translateY(100vh)";

                var success = document.getElementById("success");
                success.style.display = "block";
                success.style.transform = "translateY(-90vh)";
                __doPostBack("<%= btnCheckin.UniqueID%>", "OnClick");
            }, 0
        )
    }

</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert runat="server" ID="maAlert"></Rock:ModalAlert>
        <asp:HiddenField runat="server" ID="hfCull" />
        <asp:Panel ID="pnlMain" runat="server" Style="margin-top: 10px;">
            <Rock:ModalDialog ID="mdChoose" CssClass="modal" runat="server">
                <Content>
                    <h1>Select Room To Check-in To:</h1>
                    <div class="row">
                        <asp:PlaceHolder runat="server" ID="phModal" />
                    </div>
                </Content>
            </Rock:ModalDialog>
            <Rock:ModalDialog ID="mdMinistrySafe" runat="server" CssClass="modal">
                <Content>
                    <div class="text-center">
                        <p style="font-size: 12em; color: #DB542D">
                            <i class="fa fa-exclamation-triangle"></i>
                        </p>
                        <p style="font-size: 4em;">
                            Our records show that you have not completed Ministry Safe training.
                            <br>
                            Please contact your team leader or a staff member as soon as possible.
                        </p>
                        <Rock:BootstrapButton ID="btnContinue" runat="server" CssClass="btn btn-primary" Text="Continue"
                            Font-Size="2em" OnClick="btnContinue_Click"></Rock:BootstrapButton>
                    </div>
                </Content>
            </Rock:ModalDialog>
            <Rock:ModalAlert ID="maNotice" runat="server" />
            <div class="container" id="quickCheckinContainer">
                <div id="pgtSelect">
                    <h1>
                        <asp:Literal ID="ltMessage" Text="" runat="server" />
                    </h1>
                    <asp:PlaceHolder runat="server" ID="phPgtSelect"></asp:PlaceHolder>
                </div>
                <div class="container" id="quickCheckinContent">
                    <div id="quickCheckinPeople">
                        <div class="header">
                            <span class="ParentGroupTypeHeader">
                                <Rock:BootstrapButton CssClass="headerPgt" ID="btnParentGroupTypeHeader" OnClick="btnParentGroupTypeHeader_Click"
                                    runat="server">
                                </Rock:BootstrapButton>
                            </span>
                            <Rock:BootstrapButton ID="btnInterfaceCheckin" runat="server" CssClass="btn btn-lg btn-primary doCheckin" OnClick="btnInterfaceCheckin_Click">Check-In</Rock:BootstrapButton>
                            <div style="height: 0px; width: 0px; visibility: hidden">
                                <Rock:BootstrapButton runat="server" ID="btnCheckin" CssClass="btn btn-lg btn-primary" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
                            </div>
                        </div>
                        <div id="peopleContainer" class="peopleContainer">
                            <asp:PlaceHolder runat="server" ID="phPeople" />
                        </div>
                    </div>
                </div>
            </div>
            <div class="cancelButton">
                <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-lg" OnClick="btnCancel_Click" DataLoadingText="<i class='fa fa-refresh fa-spin'></i>"><i class='fa fa-close'></i></Rock:BootstrapButton>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNoCheckin" class="text-center alert alert-warning" Visible="false" Style="margin-top: 100px">
            <h2>We are sorry</h2>
            <h3>There are no members of your family who are able to check-in at this kiosk right now.</h3>
            <h4>Check-in may become available for your family members at a future time today.
                <br />
                If you need assistance or believe this is in error, please contact one of our volunteers.</h4>
            <Rock:BootstrapButton runat="server" ID="btnNoCheckin" OnClick="btnNoCheckin_Click" Text="OK" CssClass="btn btn-primary btn-lg pull-right"></Rock:BootstrapButton>
            <br />
            <br />
        </asp:Panel>
        <div id="success" class="text-center alert alert-success successModal">

            <h2>Welcome.</h2>
            <h2>We are preparing your security labels now.</h2>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
