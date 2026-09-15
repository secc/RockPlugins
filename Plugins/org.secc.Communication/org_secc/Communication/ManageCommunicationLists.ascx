<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ManageCommunicationLists.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.ManageCommunicationLists" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlKeyword">
            <h2>Subscribe to               
                <asp:Literal runat="server" ID="ltGroupName" /></h2>
            <div>
                <small>
                    <asp:Literal runat="server" ID="ltType" />
                </small>
            </div>
            <asp:Literal runat="server" ID="ltDescription"/>
            <Rock:NotificationBox runat="server" ID="nbAlreadySubscribed" NotificationBoxType="Success" />
            <asp:Literal runat="server" ID="ltAttributesHeader"/>
            <asp:PlaceHolder runat="server" ID="phGroupAttributes" />
            <Rock:RockCheckBox runat="server" ID="cbKeywordSmsConsent" Visible="false" />
            <asp:Literal runat="server" ID="lKeywordSmsDisclosure" />
            <Rock:BootstrapButton runat="server" ID="btnSubscribe" Text="Subscribe" CssClass="btn btn-primary" OnClick="btnSubscribe_Click" />
            <hr />
        </asp:Panel>
        <Rock:NotificationBox runat="server" id="nbNotice" Visible="false" />
        <Rock:NotificationBox runat="server" ID="nbSuccess" NotificationBoxType="Success" />
        <Rock:DynamicPlaceholder runat="server" ID="phGroups" />

    </ContentTemplate>
</asp:UpdatePanel>


<script>
    // Greys out any Subscribe control tagged data-requires-consent until its paired consent
    // box is ticked. Presentation only -- HasSmsConsent on the server is the real gate.
    (function () {
        function wire() {
            $('[data-requires-consent]').each(function () {
                var btn = $(this);
                var cb = $(document.getElementById(btn.attr('data-requires-consent')));
                if (!cb.length) { return; }
                function sync() {
                    var ok = cb.is(':checked');
                    btn.css({
                        opacity: ok ? '' : '0.5',
                        cursor: ok ? '' : 'not-allowed',
                        pointerEvents: ok ? '' : 'none'
                    }).attr('aria-disabled', !ok);
                }
                if (!btn.data('consentWired')) {
                    btn.data('consentWired', true);
                    btn.on('click', function (e) {
                        if (!cb.is(':checked')) { e.preventDefault(); e.stopImmediatePropagation(); return false; }
                    });
                }
                cb.off('change.consentGate').on('change.consentGate', sync);
                sync();
            });
        }
        if (window.Sys && Sys.Application && !window.__seccConsentGate) {
            window.__seccConsentGate = true;
            Sys.Application.add_load(wire);
        }
        if (window.jQuery) { jQuery(wire); }
    }());
</script>