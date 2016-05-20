<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SuperSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.SuperSearch" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

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
                });

                // set focus to the input unless on a touch device
                var isTouchDevice = 'ontouchstart' in document.documentElement;
                if (!isTouchDevice)
                {
                    $('.checkin-phone-entry').focus();
                }
            });

        </script>

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <div class="row">
            <div class="col-xs-12 col-sm-4" style="margin-bottom:20px">

                <Rock:RockTextBox ID="tbPhone" MaxLength="10" CssClass="checkin-phone-entry" runat="server" Label="Phone Number" />

                <div class="tenkey checkin-phone-keypad">
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
                </div>
                <Rock:BootstrapButton CssClass="btn btn-primary col-xs-12" ID="lbSearch" runat="server" OnClick="lbSearch_Click" Text="Search" DataLoadingText="Searching..."></Rock:BootstrapButton>
            </div>

            <div class="col-xs-12 col-sm-8">
                <asp:PlaceHolder ID="phFamilies" runat="server" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
