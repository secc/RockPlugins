<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonActions.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.PersonActions" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <style>
            li a {
                text-decoration: none !important;
            }
        </style>
        <div class="panel panel-default list-as-blocks clearfix">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fas fa-user-cog"></i>Person Actions
                </h1>
            </div>
            <div class="panel-body">
                <ul>
                    <li>
                        <asp:LinkButton ID="lbUpdatePin" runat="server" CommandName="update-pin">
                            <i class="fas fa-hashtag"></i>
                            <h3>Update PIN</h3>
                            <Rock:HighlightLabel ID="hlblPIN" runat="server" Visible="false" />                         
                        </asp:LinkButton>
                    </li>
                    <li>
                        <asp:LinkButton ID="lbChildcareCredits" runat="server" CommandName="add-childcare-credit">
                            <i class="fas fa-coins"></i>
                            <h3>Update Childcare Credits</h3>
                            <Rock:HighlightLabel ID="hlblChildcare" runat="server" Visible="false" />
                        </asp:LinkButton>
                    </li>
                    <li>
                        <asp:LinkButton ID="lbGroupFitnessCredit" runat="server" CommandName="add-groupfitness-sessions">
                            <i class="fas fa-coins"></i>
                            <h3>Update Group Fitness Sessions</h3>
                            <Rock:HighlightLabel ID="hlblGroupFitness" runat="server" Visible="false" />
                        </asp:LinkButton>
                    </li>
                </ul>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
