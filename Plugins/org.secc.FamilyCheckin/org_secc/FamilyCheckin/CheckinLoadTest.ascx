<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinLoadTest.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.CheckinLoadTest" %>

<script>
    var reload = function ()
    {
        setTimeout(
            function ()
            {
                console.log("Reloading");
                location.reload();
            }, 200
            );
    };
</script>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Literal Text="No Errors" ID="ltErrors" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>
