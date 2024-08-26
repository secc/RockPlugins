<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunityGivesBackRegistration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CommunityGivesBack.CommunityGivesBackRegistration" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlAcknowledgement" CssClass="panel panel-default" runat="server" Visible="false">
            <div class="panel-heading">Acknowledgement</div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-xs-12">
                        <asp:Literal ID="lAcknowledgement" runat="server" />
                    </div>
                    <div class="col-xs-12">
                        <asp:Panel ID="pnlValidateTermsAgree" runat="server" CssClass="alert alert-validation" Visible="false">
                            Please Review the Following:
                            <ul>
                                <li>Please agree to share information with JCPS to participate in this program.</li>
                            </ul>
                        </asp:Panel>

                        <Rock:RockCheckBox ID="cbAgreeToTerms" runat="server" Label="I have read and agree to these terms." AutoPostBack="true" OnCheckedChanged="cbAgreeToTerms_CheckedChanged" />
                        <span class="pull-right">
                            <asp:Button ID="btnAcknowledgeNext" runat="server" CssClass="btn btn-primary" Text="Next &rsaquo;" OnClick="btnAcknowledgeNext_Click" Enabled="false" />
                        </span>
                    </div>
                </div>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlContactInformation" runat="server" CssClass="panel panel-default" Visible="false">
            <div class="panel-heading">Contact Information</div>
            <div class="panel-body">
                <asp:ValidationSummary ID="vsContactInfo" runat="server" CssClass="alert alert-validation" HeaderText="Please Review the Following:" ValidationGroup="valContact" />
                <div class="row">
                    <div class="col-sm-6">
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Required="true" Label="First Name"
                            RequiredErrorMessage="First Name is required." ValidationGroup="valContact" />
                    </div>
                    <div class="col-sm-6">
                        <Rock:RockTextBox ID="tbLastName" runat="server" Required="true" Label="Last Name"
                            RequiredErrorMessage="Last Name is required." ValidationGroup="valContact" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-6">
                        <Rock:EmailBox ID="tbEmail" runat="server" Required="true" Label="Email"
                            RequiredErrorMessage="Email Address is required." ValidationGroup="valContact" />
                    </div>
                    <div class="col-sm-6">
                        <Rock:PhoneNumberBox ID="tbMobilePhone" runat="server" Required="true" Label="Mobile Phone"
                            RequiredErrorMessage="Mobile Phone is required." ValidationGroup="valContact" />
                    </div>
                </div>
                <div class="actions">
                    <asp:Button ID="btnContactBack" runat="server" CssClass="btn btn-back" Text="&lsaquo; Back" OnClick="btnContactBack_Click" Enabled="true" CausesValidation="false" />
                    <span class="pull-right">
                        <asp:Button ID="btnContactNext" runat="server" CssClass="btn btn-primary" Text="Next &rsaquo;" OnClick="btnContactNext_Click" Enabled="true" ValidationGroup="valContact" />
                    </span>
                </div>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlSelectSchool" runat="server" CssClass="panel panel-default" Visible="false">
            <div class="panel-heading">School Selection</div>
            <div class="panel-body">
                <asp:ValidationSummary ID="vsSchool" runat="server" CssClass="alert alert-validation" HeaderText="Please Review the Following." ValidationGroup="valSchool" />

                <div class="row">
                    <div class="col-md-6 col-sm-9 col-xs-12">
                        <Rock:RockDropDownList ID="ddlSchools" runat="server" Required="true" Label="I will sponsor JCPS student(s) at"
                            RequiredErrorMessage="School selection is required" AutoPostBack="true" OnSelectedIndexChanged="ddlSchools_SelectedIndexChanged" ValidationGroup="valSchool" />
                    </div>
                </div>

                <Rock:NumberUpDown ID="nudSponsorships" runat="server" Required="true" Label="How many students are you willing to sponsor?" Minimum="1"
                    RequiredErrorMessage="Please select the number of students you would like to sponsor" ValidationGroup="valSchool" />
                <Rock:RockRadioButtonList ID="rblSiblingGroups" runat="server" RepeatDirection="Horizontal" Label="Are you willing to sponsor a sibling group?" Required="true"
                    RequiredErrorMessage="Please select if you are willing to sponsor a sibling group." ValidationGroup="valSchool">
                    <asp:ListItem Text="Yes" Value="1" />
                    <asp:ListItem Text="No" Value="0" />
                </Rock:RockRadioButtonList>
                <div class="actions">
                    <asp:Button ID="btnSchoolBack" runat="server" CssClass="btn btn-back" Text="&lsaquo; Back" OnClick="btnSchoolBack_Click" Enabled="true" CausesValidation="false" />
                    <span class="pull-right">
                        <asp:Button ID="btnSchoolNext" runat="server" CssClass="btn btn-primary" Text="Next &rsaquo;" OnClick="btnSchoolNext_Click" Enabled="true" CausesValidation="true" ValidationGroup="valSchool" />
                    </span>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
