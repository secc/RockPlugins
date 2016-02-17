<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickCheckin" %>
<style>
.vcenter {
    margin:5px;
    display: inline-block;
    vertical-align: middle;
    float: none;
}
</style>

<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <script>
        
        Sys.Application.add_load(function () {
            $('.tenkey a.digit').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val() + $(this).html());
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.back').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val().slice(0, -1));
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.clear').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val('');
                findFamilies($phoneNumber.val());
            });

            // set focus to the input unless on a touch device
            var isTouchDevice = 'ontouchstart' in document.documentElement;
            if (!isTouchDevice) {
                $('.checkin-phone-entry').focus();
            }
        });

        var chooseFamily = function(event){
            __doPostBack("ChooseFamily", this.id);
        }

        
    </script>
    <Rock:ModalDialog ID="mdChooseClass" runat="server">
        <Content>
        </Content>
    </Rock:ModalDialog>
    <Rock:ModalAlert ID="maNotice" runat="server" />
<div class="container">
    <asp:PlaceHolder runat="server" ID="phParentGroupTypes"/>
    <br /><br /><br /><br />
    <asp:PlaceHolder runat="server" ID="phPeople"/>
    <Rock:BootstrapButton runat="server" ID="btnCheckin" CssClass="btn btn-lg btn-primary col-xs-6" OnClick="btnCheckin_Click">Check-In</Rock:BootstrapButton>
    <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-lg btn-default col-xs-6" OnClick="btnCancel_Click">Cancel</Rock:BootstrapButton>
</div>
    </ContentTemplate>
</asp:UpdatePanel>
