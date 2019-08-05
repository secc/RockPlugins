<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PublishGroupLava.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.PublishGroupLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="cblCampus" />
        <asp:AsyncPostBackTrigger ControlID="cblCategory" />
    </Triggers>
    <ContentTemplate>

        <asp:Panel ID="pnlFilters" CssClass="col-md-3 hidden-print" runat="server">
             <%-- Note: RockControlWrapper/Div/CheckboxList is being used instead of just a RockCheckBoxList, because autopostback does not currently work for RockControlCheckbox--%>
            <Rock:RockControlWrapper ID="rcwCampus" runat="server" Label="Filter by Campus">
                <div class="controls">
                    <asp:CheckBoxList ID="cblCampus" RepeatDirection="Vertical" runat="server" DataTextField="Name" DataValueField="Id"
                        OnSelectedIndexChanged="cblCampus_SelectedIndexChanged" AutoPostBack="true" />
                </div>
            </Rock:RockControlWrapper>
            <Rock:RockControlWrapper ID="rcwCategory" runat="server" Label="Filter by Category">
                <div class="controls">
                    <asp:CheckBoxList ID="cblCategory" RepeatDirection="Vertical" runat="server" DataTextField="Value" DataValueField="Id" OnSelectedIndexChanged="cblCategory_SelectedIndexChanged" AutoPostBack="true" />
                </div>
            </Rock:RockControlWrapper>
        </asp:Panel>

       

        <asp:Literal ID="lOutput" runat="server"></asp:Literal>
    
        <asp:Literal ID="lDebug" Visible="false" runat="server"></asp:Literal>

      

    </ContentTemplate>
</asp:UpdatePanel>