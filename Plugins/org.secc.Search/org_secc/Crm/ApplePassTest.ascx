<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApplePassTest.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Crm.ApplePassTest" %>
<asp:UpdatePanel ID="upMain" runat="server" >
    <ContentTemplate>
        <div class="row">
            <div class="col-sm-12">
                <Rock:PersonPicker ID="ppApplePassPerson" runat="server" Label="Person" />
            </div>
        </div>

        <div class="actions">
            <asp:LinkButton ID="lbTestPass" runat="server" CssClass="btn btn-primary">Test Pass</asp:LinkButton>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">
    function GeneratePass ( personGuid )
    {
        $( "#hfPassInfo" ).val( personGuid );;
        $( "#lbGenerateApplePass" ).trigger( "click" );
    }

</script>