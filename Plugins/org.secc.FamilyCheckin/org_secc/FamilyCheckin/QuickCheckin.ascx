<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickCheckin" %>
<style>
    .row {
        margin-right: 0px;
        margin-left: 0px;
    }
    .modal-content {
        position: initial;
        left: unset;
        top: unset;
        transform: unset;
        min-height: unset;

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
                window.scrollTo(0, 0);
                var content = document.getElementById("quickCheckinContainer");
                document.body.style.overflow = "hidden";
                content.style.transitionDuration = "0.2s";
                content.style.transform = "translateY(200vh)";

                var success = document.getElementById("success");
                success.style.display = "block";
                success.style.transform = "translateY(-90vh)";
                __doPostBack("<%= btnCheckin.UniqueID%>", "OnClick");
                $('.doCheckin').hide();
            }, 0
        )
    }

    var completeMCR = function () {
        __doPostBack("<%= btnCompleteMCRActual.UniqueID%>", "OnClick");
    }

    var cancelMCR = function () {
        __doPostBack("<%= btnCancelMCRActual.UniqueID%>", "OnClick");
    }

    var navigatePrevious = function () {
        setTimeout(function () { __doPostBack("<%= btnNavigatePrevious.UniqueID%>", "OnClick"); }, 4000)
    }

</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Label runat="server" ID="lStyle" />
        <Rock:ModalAlert runat="server" ID="maAlert" />
        <asp:HiddenField runat="server" ID="hfCull" />
        <Rock:ModalDialog ID="mdMCR" CssClass="modal" runat="server">
            <Content>
                <h1>Complete Mobile Check-in</h1>
                <h4>It looks like you have a mobile check-in ready.<br />
                    You can finish checking in or cancel your reservation and start a new check-in.
                </h4>
                <br />
                <br />
                <asp:LinkButton Text="<br>Complete Check-in<br><br>" CssClass="btn btn-primary btn-lg btn-block" runat="server"
                    CausesValidation="false" ID="btnCompleteMCR" OnClick="btnCompleteMCR_Click" />
                <br />
                <br />
                <asp:LinkButton Text="Cancel And Start New Check-in" CssClass="btn btn-danger " runat="server"
                    CausesValidation="false" ID="btnCancelMCR" OnClick="btnCancelMCR_Click" />
            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlMain" runat="server" Style="margin-top: 10px;">
            <Rock:ModalDialog ID="mdChoose" CssClass="modal" runat="server">
                <Content>
                    <h1>Select Room To Check-in To:</h1>
                    <div class="row">
                        <asp:PlaceHolder runat="server" ID="phModal" />
                    </div>
                </Content>
            </Rock:ModalDialog>
            <Rock:ModalDialog runat="server" ID="mdAddPerson" CssClass="modal">
                <Content>
                    <h1>Add Person To Check-in:</h1>
                    <br />
                    <br />
                    <div class="row">
                        <asp:PlaceHolder runat="server" ID="phAddPerson" />
                    </div>
                </Content>
            </Rock:ModalDialog>
            <Rock:ModalAlert ID="maNotice" runat="server" />
            <div class="container" id="quickCheckinContainer">
                <div id="pgtSelect">
                    <h1>Please Select One Or More Services To Check-in
                    </h1>
                    <asp:PlaceHolder runat="server" ID="phServices"></asp:PlaceHolder>
                </div>
                <div class="container" id="quickCheckinContent">
                    <div id="quickCheckinPeople">
                        <div class="header">
                            <span class="CheckinHeader">
                                <Rock:BootstrapButton ID="btnCheckinHeader" OnClick="btnCheckinHeader_Click"
                                    Text="Check-in" runat="server" />
                            </span>
                            <Rock:BootstrapButton ID="btnInterfaceCheckin" runat="server" CssClass="btn btn-success doCheckin" OnClick="btnInterfaceCheckin_Click">Check-In</Rock:BootstrapButton>
                            <Rock:BootstrapButton runat="server" ID="btnAddPerson" CssClass="btn btn-lg btn-default addPerson" OnClick="addPerson_Click">+</Rock:BootstrapButton>
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
            <asp:Literal runat="server" ID="ltNoneFound" />
            <Rock:BootstrapButton runat="server" ID="btnNoCheckin" OnClick="btnNoCheckin_Click" Text="<span class='center'>OK</span>" CssClass="btn btn-primary btn-lg pull-right"></Rock:BootstrapButton>
            <br />
            <br />
        </asp:Panel>
        <div id="success" class="text-center alert alert-success successModal">
            <asp:Literal runat="server" ID="ltCompletion" />
        </div>
        <asp:LinkButton runat="server" Text="_" ID="btnCompleteMCRActual" CssClass="hidden" OnClick="btnCompleteMCRActual_Click" />
        <asp:LinkButton runat="server" Text="_" ID="btnCancelMCRActual" CssClass="hidden" OnClick="btnCancelMCRActual_Click" />
        <asp:LinkButton runat="server" Text="_" ID="btnNavigatePrevious" CssClass="hidden" OnClick="btnNavigatePrevious_Click" />
    </ContentTemplate>
</asp:UpdatePanel>
