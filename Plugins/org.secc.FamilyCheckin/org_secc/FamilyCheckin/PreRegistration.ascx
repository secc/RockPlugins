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
                    <h1 class="g-padding-t-50--xs g-padding-l-15--xs g-margin-b-20--xs g-font-size-30--xs g-font-family--secondary">Tell Us About Your Family</h1>
                    <p class="step-description g-padding-l-15--xs g-margin-b-30--xs g-font-family--primary">We'll need some basic information about the parent(s) or guardian(s) who will be checking children in and out.</p>
                    <div class="col-md-6">
                        <h3 class="g-font-family--primary">First Parent/Guardian</h3>
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
                                <Rock:PhoneNumberBox runat="server" Label=" Mobile Phone" Required="true" ID="pnbPhone" />
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
                        <h3 class="g-font-family--primary">Second Parent/Guardian</h3>
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
                                <Rock:PhoneNumberBox runat="server" Label="Mobile Phone" Required="false" ID="pnbPhone2" />
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
                <Rock:BootstrapButton runat="server" ID="btnRegistrationNext" Text="Next" CssClass="btn btn-primary" OnClick="btnRegistrationNext_Click" />
            </div>
        </asp:Panel>


        <asp:Panel runat="server" ID="pnlChild" Visible="false">
            <div class="form-container">
                <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">Tell Us About Each Child</h1>
                <p class="step-description g-margin-b-30--xs g-font-family--primary">Please provide the information below about each child, so that we can get them into the right classroom. If you have additional children to check in, you will be able to add them on the next screen.</p>
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
                    <div class="col-sm-4">
                        <Rock:RockCheckBox runat="server" ID="cbGuest" Label="Guest?" Help="Check this box if this child is a guest rather than a member of your immediate family" />                        
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockRadioButtonList runat="server" ID="rblGender" Label="Gender" Required="true" RepeatDirection="Horizontal">
                            <asp:ListItem Text="Boy" Value="Male" />
                            <asp:ListItem Text="Girl" Value="Female" />
                        </Rock:RockRadioButtonList>
                    </div>
                    <div class="col-sm-4">
                        <Rock:GradePicker runat="server" ID="gpGrade" Label="Grade"></Rock:GradePicker>
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3">
                        <Rock:Toggle runat="server" ID="cbHasAllergies" Label="Does this child have allergies?" OnCheckedChanged="cbHasAllergies_CheckedChanged" AutoPostBack="true" OnText="Yes" OffText="No" />
                    </div>
                    <div class="col-sm-9">
                        <Rock:RockTextBox runat="server" ID="tbAllergies" Label="Allergies" visible="false" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3">
                        <Rock:Toggle runat="server" ID="cbHasSpecialNote" Label="Does this child have Medical/Special/Other Needs?" OnCheckedChanged="cbHasSpecialNote_CheckedChanged" AutoPostBack="true" OnText="Yes" OffText="No" />
                    </div>
                    <div class="col-sm-9">
                        <Rock:RockTextBox runat="server" ID="tbSpecialNote" Label="Medical/Special/Other Needs" visible="false" />
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" ID="btnChildAddAnother" Text="Add Another Child" CssClass="btn btn-primary" OnClick="btnChildAddAnother_Click" />
                <Rock:BootstrapButton runat="server" ID="btnChildNext" Text="Next" CssClass="btn btn-primary" OnClick="btnChildNext_Click" />
                <asp:LinkButton runat="server" ID="btnChildCancel" Text="Cancel" CssClass="btn btn-primary" OnClick="btnChildCancel_Click" Visible="false" CausesValidation="false" />
            </div>
        </asp:Panel>


        <asp:Panel runat="server" ID="pnlChildSummary" Visible="false">
            <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">Your Children's Information</h1>
            <p class="step-description g-margin-b-30--xs g-font-family--primary">Here's what we have for your children. Does everything here look good?</p>
            <div class="row">
                <asp:PlaceHolder runat="server" ID="phChildSummary"></asp:PlaceHolder>
            </div>
            <div class="panel-footer clearfix" style="margin-top:15px">
                <Rock:BootstrapButton runat="server" ID="btnChildAddAnotherFromSummary" Text="Add Another Child" CssClass="btn btn-primary" OnClick="btnChildAddAnotherFromSummary_Click" />
                <Rock:BootstrapButton runat="server" ID="btnChildSummaryNext" Text="Next" CssClass="btn btn-primary" OnClick="btnChildSummaryNext_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlAdult" Visible="false">
            <div class="form-container">
                <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">Tell Us About Each Known Adult</h1>
                <p class="step-description g-margin-b-30--xs g-font-family--primary">Please provide the information below about each known adult who should be allowed to check in your child(ren). If you have additional adults to add, you will be able to add them on the next screen.</p>
                <div class="row">
                    <div class="col-sm-6">
                        <Rock:RockTextBox runat="server" Label="Adult's First Name" Required="true" ID="tbAdultFirstName" />
                    </div>
                    <div class="col-sm-6">
                        <Rock:RockTextBox runat="server" Label="Adult's Last Name" Required="true" ID="tbAdultLastName" />
                    </div>
                </div>
                <p>Please enter at least one of the following:</p>
                <div class="row">
                    <asp:CustomValidator runat="server" ID="cvKnownAdultInfo" OnServerValidate="cvKnownAdultInfo_ServerValidate" ErrorMessage="Please enter at least one of the following: mobile phone number, date of birth, or email address" CssClass="alert alert-danger g-margin-b-10--xs"/>
                    <div class="col-sm-9">
                        <Rock:PhoneNumberBox runat="server" Label="Mobile Phone" Required="false" ID="pnbAdultPhone" />
                    </div>
                    <div class="col-sm-3">
                        <Rock:DatePicker runat="server" Label="Date of Birth" Required="false" ID="dpAdultDateOfBirth" StartView="decade" />
                    </div>
                    <div class="col-sm-12">
                        <Rock:EmailBox runat="server" ID="ebAdultEmail" Label="Email" Required="false" />
                    </div>
                </div>                
            </div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" ID="btnAdultAddAnother" Text="Add Another Adult" CssClass="btn btn-primary" OnClick="btnAdultAddAnother_Click" />
                <Rock:BootstrapButton runat="server" ID="btnAdultNext" Text="Next" CssClass="btn btn-primary" OnClick="btnAdultNext_Click" />
                <asp:LinkButton runat="server" ID="btnAdultCancel" Text="Cancel" CssClass="btn btn-primary" OnClick="btnAdultCancel_Click" Visible="true" CausesValidation="false" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlAdditionalInfo" Visible="false">
            <div class="form-container">
                <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">Just a Few More Things</h1>
                <p class="step-description g-margin-b-30--xs g-font-family--primary">Help us capture any information we may have missed.</p>
                <div class="row well g-padding-x-0--xs g-width-100-percent--xs g-margin-x-auto--xs">
                    <div class="container">
                        <h3 class="g-font-family--primary">Known Relationships</h3>
                        <p>Additional adults who are allowed to check in your child(ren)</p>
                        <div class="row">
                            <asp:PlaceHolder runat="server" ID="phKnownRelationshipSummary"></asp:PlaceHolder>
                        </div>
                        <Rock:BootstrapButton runat="server" ID="btnAddKnownRelationship" Text="Add Known Relationship" CssClass="btn btn-primary" OnClick="btnAddKnownRelationship_Click" />
                    </div>
                </div>
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
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" ID="btnCampusBack" Text="Back" CssClass="btn btn-primary" OnClick="btnCampusBack_Click" />
                <Rock:BootstrapButton runat="server" ID="btnCampusNext" Text="Next" CssClass="btn btn-primary" OnClick="btnCampusNext_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlReview" Visible="false">
            <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">Let's Review</h1>
            <p class="step-description g-margin-b-30--xs g-font-family--primary">You're just about done. Let's just review your information and you will be all set!</p>
            <div class="review">
                <div class="row">
                    <div class="col-sm-4">
                        <h3 class="g-font-family--primary g-margin-b-10--xs">Parent/Guardian</h3>
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
                        <h3 class="g-font-family--primary g-margin-b-10--xs">Children Registered</h3>
                        <asp:Panel runat="server" ID="pnlChildren"></asp:Panel>
                    </div>
                    <div class="col-sm-4">
                        <h3 class="g-font-family--primary g-margin-b-10--xs">Other Information</h3>
                        <Rock:RockLiteral runat="server" Label="Campus:" ID="rlCampus" />
                        <Rock:RockLiteral runat="server" Label="Address:" ID="rlAddress" CssClass="address" />
                        <Rock:RockLiteral runat="server" Label="Extra Information:" ID="rlExtraInformation" />
                        <asp:Panel runat="server" ID="pnlKnownAdults" Visible="false">    
                            <h3 class="g-font-family--primary g-margin-b-10--xs">Known Adults</h3>
                            <asp:Panel runat="server" ID="pnlKnownAdultsList"></asp:Panel>    
                        </asp:Panel>
                    </div>                    
                </div>
                <div class="row well g-padding-x-0--xs g-width-100-percent--xs g-margin-x-auto--xs">
                    <div class="col-sm-12">
                        <asp:CustomValidator runat="server" ID="cvMedicalConsent" OnServerValidate="cvMedicalConsent_ServerValidate" ErrorMessage="Medical Consent is required" CssClass="alert alert-danger g-margin-b-10--xs"/>
                        <Rock:RockCheckBox runat="server" Label="I authorize first aid and/or medical treatment for my child and I release Southeast Christian Church, its employees, and its volunteers from any and all responsibility, including negligence." ID="cbMedicalConsent" Required="true" RequiredErrorMessage="Medical Consent is required" CssClass="required border-danger" />
                    </div>
                    <div class="col-sm-9">
                        <Rock:RockTextBox runat="server" ID="tbSignature" Label="Please Enter Your Full Name" Required="true" RequiredErrorMessage="Signature is required" />
                    </div>
                    <div class="col-sm-3">
                        <Rock:DatePicker runat="server" ID="dpSignatureDate" Label="Date" Required="true" RequiredErrorMessage="Date is required" Enabled="false" Visible="false"/>
                    </div>
                </div>
            </div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" ID="btnReviewBack" Text="Back" CssClass="btn btn-primary" OnClick="btnReviewBack_Click" CausesValidation="false" />
                <Rock:BootstrapButton runat="server" ID="btnReviewFinish" Text="Finish" CssClass="btn btn-primary" OnClick="btnReviewFinish_Click" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlConfirmation" Visible="false">
            <h1 class="g-padding-t-50--xs g-margin-b-30--xs g-font-size-30--xs g-font-family--secondary">You're All Set!</h1>
            <div class="form-container">
                <div class="row">
                    <asp:Panel runat="server" ID="pnlConfirmationContent" class="col-sm-12" />
                </div>
            </div>
        </asp:Panel>
                
        <script type="text/javascript">
            // Add Pre-K to the Grade Picker
            function addPreK ()
            {
                var gradePicker = document.querySelector( "[id$='gpGrade']" );
                if ( gradePicker == null )
                {
                    return;
                }
                var option = document.createElement( "option" );
                option.text = "Pre-K / Preschool";
                option.value = "";
                gradePicker.add( option, gradePicker.options[ 1 ] );
            }

            $( document ).ready( function ()
            {
                addPreK()
            } );

            Sys.WebForms.PageRequestManager.getInstance().add_pageLoaded( function ()
            {
                addPreK()
            } );
        </script>
        
    </ContentTemplate>    
</asp:UpdatePanel>
