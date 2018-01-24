<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PreRegistration.ascx.cs" Inherits="Plugins_org_secc_FamilyCheckin_PreRegistration" %>
<style>
    .contact-card {
        background-color: rgba(0, 0, 0, 0.05);
        border-radius: 4px;
        padding: 10px;
        margin-top: 10px;
    }

        .contact-card .control-label {
            float: left;
            width: 40%;
            padding-right: 12px;
            text-align: right;
        }

    .review .control-label {
        float: left;
        width: auto;
        padding-right: 12px;
        text-align: left;
    }

    .contact-card .form-control-static, .review .form-control-static {
        width: 60%;
        padding-top: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        text-align: left;
        min-height: 20px !important;
        height: 20px;
        padding-bottom: 0px;
        white-space: nowrap;
    }

    .review .address .form-control-static {
        overflow: auto;
        height: auto;
        white-space: normal;
    }

    .contact-card .info {
        background: white;
        border-radius: 2px;
        padding: 10px;
        margin-bottom: 10px;
    }

    .contact-card .title {
        font-weight: bold;
        font-size: 16px;
        margin-bottom: 10px;
    }
</style>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlRegistration" Visible="true">
            <div class="form-container">
                <div class="row">
                    <h2>Tell Us About Your Family</h2>
                    <p class="step-description">We'll need some basic information about the parent(s) or guardian(s) who will be checking children in and out.</p>
                    <div class="col-md-6">
                        <h3>First Parent/Guardian</h3>
                        <div class="row">
                            <div class="col-sm-6">
                                <Rock:RockTextBox runat="server" Label="First Name" Required="true" ID="tbFirstname" />
                            </div>
                            <div class="col-sm-6">
                                <Rock:RockTextBox runat="server" Label="Last Name" Required="true" ID="tbLastName" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-6 col-md-8">
                                <Rock:PhoneNumberBox runat="server" Label="Phone" Required="true" ID="pnbPhone" />
                            </div>
                            <div class="col-sm-6 col-md-4">
                                <Rock:DatePicker runat="server" Label="Date of Birth" Required="true" ID="dpBirthday" StartView="decade" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-xs-12">
                                <Rock:EmailBox runat="server" ID="ebEmail" Label="Email" Required="true" />
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <h3>Second Parent/Guardian</h3>
                        <!-- Second Parent -->
                        <div class="row">
                            <div class="col-sm-6">
                                <Rock:RockTextBox runat="server" Label="First Name" Required="false" ID="tbFirstName2" />
                            </div>
                            <div class="col-sm-6">
                                <Rock:RockTextBox runat="server" Label="Last Name" Required="false" ID="tbLastName2" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-6 col-md-8">
                                <Rock:PhoneNumberBox runat="server" Label="Phone" Required="false" ID="pnbPhone2" />
                            </div>
                            <div class="col-sm-6 col-md-4">
                                <Rock:DatePicker runat="server" Label="Date of Birth" Required="false" ID="dpBirthday2" StartView="decade" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-xs-12">
                                <Rock:EmailBox runat="server" ID="ebEmail2" Label="Email" Required="false" />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-6">
                        <Rock:AddressControl runat="server" ID="acAddress" Required="true" Label="Address" />
                    </div>
                </div>

                <div class="row">
                    <div class="col-sm-12">
                        <span class="help-block">Note: The information above is required to ensure your children's safety.</span>
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" ID="btnRegistrationNext" Text="Next" CssClass="btn btn-info pull-right" OnClick="btnRegistrationNext_Click" />
            </div>
        </asp:Panel>


        <asp:Panel runat="server" ID="pnlChild" Visible="false">
            <div class="form-container">
                <h2>Tell Us About Each Child</h2>
                <p class="step-description">Please provide the information below about each child, so that we can get them into the right classroom. If you have additional children to check in, you will be able to add them on the next screen.</p>
                <div class="row">
                    <div class="col-xs-12">
                        <asp:ValidationSummary runat="server" CssClass="alert alert-danger" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-4">
                        <Rock:RockTextBox runat="server" Label="Child's First Name" Required="true" ID="tbChildFirstname" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockTextBox runat="server" Label="Child's Last Name" Required="true" ID="tbChildLastname" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:DatePicker runat="server" Label="Date of Birth" Required="true" ID="bpChildBirthday" StartView="decade" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3">
                        <Rock:RockRadioButtonList runat="server" ID="rblGender" Label="Gender" Required="true" RepeatDirection="Horizontal">
                            <asp:ListItem Text="Boy" Value="Male" />
                            <asp:ListItem Text="Girl" Value="Female" />
                        </Rock:RockRadioButtonList>
                    </div>
                    <div class="col-sm-3">
                        <Rock:GradePicker runat="server" ID="gpGrade" Label="Grade"></Rock:GradePicker>
                    </div>
                    <div class="col-sm-3">
                        <Rock:RockTextBox runat="server" ID="tbAllergies" Label="Allergies" />
                    </div>

                    <div class="col-sm-3">
                        <Rock:RockTextBox runat="server" ID="tbSpecialNote" Label="Medical/Special/Other Needs" />
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix text-right">
                <Rock:BootstrapButton runat="server" ID="btnChildAddAnother" Text="Add Another Child" CssClass="btn btn-info" OnClick="btnChildAddAnother_Click" />
                <Rock:BootstrapButton runat="server" ID="btnChildNext" Text="Next" CssClass="btn btn-info" OnClick="btnChildNext_Click" />
                <asp:LinkButton runat="server" ID="btnChildCancel" Text="Cancel" CssClass="btn btn-info" OnClick="btnChildCancel_Click" Visible="false" CausesValidation="false" />
            </div>
        </asp:Panel>


        <asp:Panel runat="server" ID="pnlChildSummary" Visible="false">
            <h2>Your Children's Information</h2>
            <p class="step-description">Here's what we have for your children. Does everything here look good?</p>
            <div class="row">
                <asp:PlaceHolder runat="server" ID="phChildSummary"></asp:PlaceHolder>
            </div>
            <div class="panel-footer clearfix text-right" style="margin-top:15px">
                <Rock:BootstrapButton runat="server" ID="btnChildAddAnotherFromSummary" Text="Add Another Child" CssClass="btn btn-info" OnClick="btnChildAddAnotherFromSummary_Click" />
                <Rock:BootstrapButton runat="server" ID="btnChildSummaryNext" Text="Next" CssClass="btn btn-info" OnClick="btnChildSummaryNext_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlCampus" Visible="false">
            <div class="form-container">
                <h2>Just a Few More Things</h2>
                <p class="step-description">Help us capture any information we may have missed.</p>
                <asp:Panel runat="server" ID="pnlAskCampus" class="row">
                    <div class="col-xs-12">
                        <Rock:CampusPicker runat="server" ID="cpCampus" Label="If you know, what campus will you be attending?" />
                    </div>
                </asp:Panel>
                <div class="row">
                    <div class="col-xs-12">
                        <Rock:RockTextBox runat="server" ID="tbExtraInformation" TextMode="MultiLine" Rows="5" Label="Extra information we need to know. (Bringing grandchildren or guests, security concerns, etc.)"></Rock:RockTextBox>
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix text-right">
                <Rock:BootstrapButton runat="server" ID="btnCampusBack" Text="Back" CssClass="btn btn-info" OnClick="btnCampusBack_Click" />
                <Rock:BootstrapButton runat="server" ID="btnCampusNext" Text="Next" CssClass="btn btn-info" OnClick="btnCampusNext_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlReview" Visible="false">
            <h2>Let's Review</h2>
            <p class="step-description">You're just about done. Let's just review your information and you will be all set!</p>
            <div class="review">
                <div class="row">
                    <div class="col-sm-4">
                        <h2>Parent/Guardian</h2>
                        <Rock:RockLiteral runat="server" Label="Name:" ID="rlName" />
                        <Rock:RockLiteral runat="server" Label="Birthdate:" ID="rlDOB" />
                        <Rock:RockLiteral runat="server" Label="Phone:" ID="rlPhone" />
                        <Rock:RockLiteral runat="server" Label="Email:" ID="rlEmail" />
                        <asp:Panel runat="server" ID="pnlParent2" Visible="false">
                            <hr />
                            <Rock:RockLiteral runat="server" Label="Name:" ID="rlName2" />
                            <Rock:RockLiteral runat="server" Label="Birthdate:" ID="rlDOB2" />
                            <Rock:RockLiteral runat="server" Label="Phone:" ID="rlPhone2" />
                            <Rock:RockLiteral runat="server" Label="Email:" ID="rlEmail2" />
                        </asp:Panel>
                    </div>
                    <div class="col-sm-4">
                        <h2>Children Registered</h2>
                        <asp:Panel runat="server" ID="pnlChildren"></asp:Panel>
                    </div>
                    <div class="col-sm-4">
                        <h2>Other Information</h2>
                        <Rock:RockLiteral runat="server" Label="Campus:" ID="rlCampus" />
                        <Rock:RockLiteral runat="server" Label="Address:" ID="rlAddress" CssClass="address" />
                        <Rock:RockLiteral runat="server" Label="Extra Information:" ID="rlExtraInformation" />
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix text-right">
                <Rock:BootstrapButton runat="server" ID="btnReviewBack" Text="Back" CssClass="btn btn-info" OnClick="btnReviewBack_Click" />
                <Rock:BootstrapButton runat="server" ID="btnReviewFinish" Text="Finish" CssClass="btn btn-info" OnClick="btnReviewFinish_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlConfirmation" Visible="false">
            <h2>You're All Set!</h2>
            <div class="form-container">
                <div class="row">
                    <asp:Panel runat="server" ID="pnlConfirmationContent" class="col-sm-12" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
