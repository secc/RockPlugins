<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginJS.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.LoginJS" %>
<asp:PlaceHolder ID="phLoginJS" runat="server" Visible="false">
    <script type="text/javascript">
        $('#siteLogin a').hide();
        $('#siteLoginMobile a').hide();
        $.get("/api/people/CurrentUser", function (data) {
            $('#siteLogin').html("<a href=\"javascript:\" class=\"account-menu-toggle\">"+data.FullName+" <span class=\"caret\"></span></a>");
            $('#siteLoginMobile').addClass("dropdown");

            $('#siteLoginMobile').html("<button id=\"mobileToggleAccount\" class=\"toggle\">"+data.FullName+"</button> \
                                        <ul> \
                                            <li><a href=\"/MyAccount\">My Account</a></li> \
                                            <li><a href=\"/MyEvents\">My Events</a></li> \
                                            <li><a href=\"/my-classes\">My Classes</a></li> \
                                            <li><a href=\"/Give\">Give</a></li> \
                                            <li><a href=\"?logout=true\">Log Out</a></li> \
                                        </ul>");

            bindAccountMenu($('.account-menu-toggle'));
        });
    </script>
</asp:PlaceHolder>
