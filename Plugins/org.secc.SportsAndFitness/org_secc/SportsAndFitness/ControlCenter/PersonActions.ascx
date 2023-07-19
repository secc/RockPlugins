<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonActions.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.PersonActions" %>
<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <style>
            li a {
                text-decoration: none !important;
            }

            #divChildcareModal .row {
                padding-bottom: 15px;
            }

            #divChildcareModal textarea
            {
                border:0px;
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
                        <a href="#" id="lbUpdatePin" onclick="javascript:return runCmd('update-pin');">
                            <i class="fas fa-hashtag"></i>
                            <h3>Update PIN</h3>
                            <Rock:HighlightLabel ID="hlblPIN" runat="server" Visible="false" />
                        </a>
                    </li>
                    <li>
                        <a href="#" id="lbChildcareCredits" onclick="javascript: return runCmd('add-childcare-credits');">
                            <i class="fas fa-coins"></i>
                            <h3>Update Childcare Credits</h3>
                            <Rock:HighlightLabel ID="hlblChildcare" runat="server" Visible="false" />
                        </a>
                    </li>
                    <li>
                        <a href="#" id="lbGroupFitnessCredits" onclick="javascript: return runCmd('add-groupfitness-credits');">
                            <i class="fas fa-coins"></i>
                            <h3>Update Group Fitness Sessions</h3>
                            <Rock:HighlightLabel ID="hlblGroupFitness" runat="server" Visible="false" />
                        </a>
                    </li>
                </ul>
            </div>
        </div>
        <script type="text/javascript">
            function runCmd ( action )
            {
                var panelClientId =  "<%= upMain.ClientID %>";
                __doPostBack( panelClientId, action );
                return false;
            }
        </script>
    </ContentTemplate>

</asp:UpdatePanel>

<asp:UpdatePanel ID="upModals" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <Rock:ModalDialog ID="mdGroupFitness" runat="server" Title="Group Fitness" ValidationGroup="vgGroupFitness" SaveButtonText="Add" CancelLinkVisible="true">
            <Content>
                <asp:HiddenField ID="hfGroupMemberId" runat="server" />
                <Rock:NotificationBox ID="nbGroupFitness" runat="server" />
                <asp:ValidationSummary ID="vsGFModal" runat="server" CssClass="alert alert-validation" ValidationGroup="vgGroupFitness" />
                <div class="row">
                    <div class="col-sm-6 col-md-3">
                        <Rock:RockLiteral ID="lGFBeginningCreditsLbl" runat="server" CssClass="control-label">Beginning Credits</Rock:RockLiteral>
                    </div>
                    <div class="com-sm-6 col-md-9">
                        <Rock:RockLiteral ID="lGFBeginningCredits" runat="server" CssClass="control-label" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-6 col-md-3">
                        <span class="control-label">Credits to Add</span>
                    </div>
                    <div class="col-sm-6 col-md-9">
                        <Rock:RockTextBox ID="tbGFCreditsToAdd" runat="server" Required="true"
                            TextMode="Number" RequiredErrorMessage="Credits to Add is required." ValidationGroup="vgGroupFitness" />
                        <asp:RangeValidator ID="rngValGFCreditsToAdd" runat="server" ControlToValidate="tbGFCreditsToAdd"
                            Type="Integer" MinimumValue="0" MaximumValue="500"
                            ErrorMessage="Credits to add must be 0 or greater." ValidationGroup="vgGroupFitness" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-6 col-md-3">
                        <span class="control-label">Notes</span>
                    </div>
                    <div class="col-sm-6 col-md-9">
                        <Rock:RockTextBox ID="tbGFNotes" runat="server" Required="false" TextMode="MultiLine" />
                    </div>
                </div>

            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdChildcare" runat="server" Title="Childcare" ValidationGroup="vgChildcare" SaveButtonText="Add" CancelLinkVisible="true">
            <Content>
                <div id="divChildcareModal">
                    <asp:HiddenField ID="hfChildcareFamilyId" runat="server" />
                    <Rock:NotificationBox ID="nbChildcare" runat="server" Visible="false" />
                    <asp:ValidationSummary ID="vsChildcareModel" runat="server" CssClass="alert alert-validation" ValidationGroup="vgChildcare" />
                    <div class="row">
                        <div class="col-sm-6 col-md-3">
                            <span class="control-label">Beginning Credits</span>
                        </div>
                        <div class="col-sm-6 col-md-9">
                            <Rock:RockTextBox ID="tbCCBeginningCredits" runat="server" TextMode="Number" ReadOnly="true" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-6 col-md-3">
                            <span class="control-label">Credits To Add</span>
                        </div>
                        <div class="col-sm-6 col-md-9">
                            <Rock:RockTextBox ID="tbCCCreditsToAdd" runat="server" Required="true" TextMode="Number"
                                RequiredErrorMessage="Credits to Add is required" ValidationGroup="vgChildcare" />
                            <asp:RangeValidator ID="rngValCCCreditsToAdd" runat="server" ControlToValidate="tbCCCreditsToAdd"
                                Type="Integer" MinimumValue="0" MaximumValue="500" ValidationGroup="vgChildcare"
                                ErrorMessage="Credits to add must be 0 or greater." />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-6 col-md-3">
                            <span class="control-label">Notes</span>
                        </div>
                        <div class="col-sm-6 col-md-9">
                            <Rock:RockTextBox ID="tbCCNotes" runat="server" Required="false" TextMode="MultiLine" />
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>

