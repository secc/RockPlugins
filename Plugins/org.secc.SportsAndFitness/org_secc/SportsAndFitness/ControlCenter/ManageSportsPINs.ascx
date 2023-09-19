<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ManageSportsPINs.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.ManageSportsPINs" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <style>
            .pinList li {
                padding-left: 15px;
                font-weight: bold;
                line-height: 1.3em;
            }

            .pinList .actions {
                opacity: .2;
                -webkit-transition: opacity .5s ease-out;
                -moz-transition: opacity .5s ease-out;
                -o-transition: opacity .5s ease-out;
                transition: opacity .5s ease-out;
            }

                .pinList .actions:hover {
                    opacity: 1
                }
        </style>
        <asp:HiddenField ID="hfPersonId" runat="server" />
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <asp:Panel ID="pnlAlert" runat="server" CssClass="alert alert-warning" Visible="false">
                <h3><i class="fa fa-exclamation-triangle"></i> <asp:Literal ID="lAlertTitle" runat="server" /></h3>
                <asp:Literal ID="lAlertText" runat="server" />
            </asp:Panel>
            <ul class="pinList">
                <asp:Repeater ID="rSportsPins" runat="server">
                    <ItemTemplate>
                        <div class="row">
                            <li>
                                <div class="col-xs-6">
                                    <%# Eval("UserName") %>
                                </div>
                                <div class="col-xs-6 actions">
                                    <asp:LinkButton ID="lbRemove" runat="server" CommandName="RemovePIN" CommandArgument='<%# Eval("Id") %>' CssClass="btn btn-xs btn-danger"><i class="fa fa-times"></i></asp:LinkButton>
                                </div>
                            </li>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
            <div class="row">
                <div class="col-xs-6">
                    <Rock:RockTextBox ID="tbPIN" TextMode="Number" runat="server" Placeholder="PIN" />
                </div>
                <div class="col-xs-6">
                    <asp:LinkButton ID="lbAdd" runat="server" CssClass="btn btn-primary" Text="Add" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
