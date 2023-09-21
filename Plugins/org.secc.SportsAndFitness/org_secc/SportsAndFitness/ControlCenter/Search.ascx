<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Search.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.Search" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlSeach" runat="server" >
            <div class="row">
                <div class="col-md-4 col-md-offset-8">
                    <Rock:RockTextBox ID="tbSearch" runat="server" CssClass="js-search" PrependText="<i class='fa fa-search'></i>" spellcheck="false" onkeydown="javascript:return handleSearchBoxKeyPress(this, event.keyCode);"  />
                </div>
            </div>
        </asp:Panel>
        <script>
            // handle onkeypress for the search box
            function handleSearchBoxKeyPress(element, keyCode) {
                if (keyCode == 13) {
                    window.location = "javascript:__doPostBack('<%=upMain.ClientID %>', 'search')";

                    // prevent double-postback
                    $(element).prop('disabled', true)
                        .attr('disabled', 'disabled')
                        .addClass('disabled');

                    return true;
                }
            }
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
