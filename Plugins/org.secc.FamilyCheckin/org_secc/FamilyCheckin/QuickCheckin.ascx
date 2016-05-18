<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickCheckin" %>
<style>
    .row {
        margin-right: 0px;
        margin-left: 0px;
    }

    .modal {
        width: 90vw;
        height: 90vh;
    }

    .close {
        visibility: hidden;
    }

    .header {
        border-radius: 3px;
        background-color: #F5F5F5;
        padding: 20px;
        margin-bottom: 15px;
    }
</style>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <script>

            var showContent = function ()
            {
                var content = document.getElementById("quickCheckinContent")
                content.style.transition = "linear .2s"
                content.style.transform = "translateX(100vw)"
                var pgt = document.getElementById("pgtSelect")
                pgt.style.transition = "linear .2s"
                pgt.style.transform = "translateX(100vw)"
            }

            var showPgt = function ()
            {
                var content = document.getElementById("quickCheckinContent")
                content.style.transition = "linear .2s"
                content.style.transform = "translateX(0px)"
                var pgt = document.getElementById("pgtSelect")
                pgt.style.transition = "linear .2s"
                pgt.style.transform = "translateX(0px)"
            }

            var doCheckin = function ()
            {
                setTimeout(
                    function ()
                    {
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
        <asp:Panel ID="pnlMain" runat="server" Style="margin-top: 10px;">
            <Rock:ModalDialog ID="mdChoose" CssClass="modal" runat="server">
                <Content>
                    <div class="row">
                        <asp:PlaceHolder runat="server" ID="phModal" />
                    </div>
                </Content>
            </Rock:ModalDialog>
            <Rock:ModalAlert ID="maNotice" runat="server" />
            <div class="container" id="quickCheckinContainer">
                <div id="pgtSelect">
                    <h1>
                        <asp:Literal ID="ltMessage" Text="Where would you like to check-in to today?" runat="server" />
                    </h1>
                    <asp:PlaceHolder runat="server" ID="phPgtSelect"></asp:PlaceHolder>
                </div>
                <div class="container" id="quickCheckinContent">
                    <div style="margin: 0px auto; width: 1410px">
                        <div class="header">
                            <span class="ParentGroupTypeHeader">
                                <Rock:BootstrapButton CssClass="headerPgt" ID="btnParentGroupTypeHeader" OnClick="btnParentGroupTypeHeader_Click"
                                    runat="server">
                                </Rock:BootstrapButton>
                            </span>
                            <span class="pull-right" style="padding-top: 10px;">
                                <a href="javascript:doCheckin()" class="btn btn-lg btn-primary doCheckin">Check-In</a>
                                <Rock:BootstrapButton runat="server" Visible="false" ID="btnCheckin" CssClass="btn btn-lg btn-primary" OnClick="btnCheckin_Click"></Rock:BootstrapButton>

                            </span>
                        </div>
                        <asp:PlaceHolder runat="server" ID="phPeople" />
                    </div>
                </div>
            </div>
            <div class="cancelButton">
                <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-lg" OnClick="btnCancel_Click" DataLoadingText="<i class='fa fa-refresh fa-spin'></i>"><i class='fa fa-close'></i></Rock:BootstrapButton>
            </div>
        </asp:Panel>
        <div id="success" class="text-center alert alert-success successModal">

            <h2>Welcome.</h2>
            <h2>We are printing your name tags now.</h2>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
