<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RiseSamlRedirect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.RiseSamlRedirect" %>
<style>
    .lds-roller {
        display: inline-block;
        position: relative;
        width: 80px;
        height: 80px;
    }

        .lds-roller div {
            animation: lds-roller 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
            transform-origin: 40px 40px;
        }

            .lds-roller div:after {
                content: " ";
                display: block;
                position: absolute;
                width: 7px;
                height: 7px;
                border-radius: 50%;
                background: #aaa;
                margin: -4px 0 0 -4px;
            }

            .lds-roller div:nth-child(1) {
                animation-delay: -0.036s;
            }

                .lds-roller div:nth-child(1):after {
                    top: 63px;
                    left: 63px;
                }

            .lds-roller div:nth-child(2) {
                animation-delay: -0.072s;
            }

                .lds-roller div:nth-child(2):after {
                    top: 68px;
                    left: 56px;
                }

            .lds-roller div:nth-child(3) {
                animation-delay: -0.108s;
            }

                .lds-roller div:nth-child(3):after {
                    top: 71px;
                    left: 48px;
                }

            .lds-roller div:nth-child(4) {
                animation-delay: -0.144s;
            }

                .lds-roller div:nth-child(4):after {
                    top: 72px;
                    left: 40px;
                }

            .lds-roller div:nth-child(5) {
                animation-delay: -0.18s;
            }

                .lds-roller div:nth-child(5):after {
                    top: 71px;
                    left: 32px;
                }

            .lds-roller div:nth-child(6) {
                animation-delay: -0.216s;
            }

                .lds-roller div:nth-child(6):after {
                    top: 68px;
                    left: 24px;
                }

            .lds-roller div:nth-child(7) {
                animation-delay: -0.252s;
            }

                .lds-roller div:nth-child(7):after {
                    top: 63px;
                    left: 17px;
                }

            .lds-roller div:nth-child(8) {
                animation-delay: -0.288s;
            }

                .lds-roller div:nth-child(8):after {
                    top: 56px;
                    left: 12px;
                }

    @keyframes lds-roller {
        0% {
            transform: rotate(0deg);
        }

        100% {
            transform: rotate(360deg);
        }
    }
</style>

<script src="/SignalR/hubs"></script>
<script type="text/javascript">
    $(function () {
        var proxy = $.connection.rockMessageHub;

        proxy.client.receiveNotification = function (name, message) {
            if (name == '<%=this.SignalRNotificationKey %>') {

                if (message == 'success') {
                    window.location = window.location;
                }
                else if (message == 'fail') {
                    $('#creatingAccount').html('<h3>Something Went Wrong</h3>We\'re sorry. There was in issue with your request. Please try again later. If this problem continues please contact ithelp@secc.org');
                }
            }
        }

        $.connection.hub.start().done(function () {
            // hub started... do stuff here if you want to let the user know something
        });
    })
</script>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlCreateAccount" CssClass="text-center" Visible="false">
            <div id="creatingAccount">
                <div class="lds-roller">
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                    <div></div>
                </div>
                <h3>Creating Account Please Wait</h3>
                We're setting up your account with our learning provider. This may take a minute...
                <br />
                <iframe id="ifBackground" runat="server" class="hidden"></iframe>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlDebug" Visible="false">
            <asp:Literal runat="server" ID="ltDebug" />
            <asp:LinkButton Text="Post Data" runat="server" ID="btnPost" OnClick="btnPost_Click" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
