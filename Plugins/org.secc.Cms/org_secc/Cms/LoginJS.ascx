<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginJS.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.LoginJS" %>
<asp:PlaceHolder ID="phLoginJS" runat="server" Visible="false">
    <script type="text/javascript">
        $('#siteLogin a').hide();
        $.get("/api/people/CurrentUser", function (data) {
            $('#siteLogin').html("<a href=\"/MyAccount\">"+data.FullName+"</a>");
        });
    </script>
</asp:PlaceHolder>