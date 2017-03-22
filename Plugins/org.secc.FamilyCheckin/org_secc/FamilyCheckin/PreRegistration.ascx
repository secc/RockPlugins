<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PreRegistration.ascx.cs" Inherits="Plugins_org_secc_FamilyCheckin_PreRegistration" %>
<style>
    .contact-card {
        background-color:rgba(0, 0, 0, 0.05);
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
    .contact-card .form-control-static {
        float: left;
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
    .contact-card .form-group {
        margin-bottom: 0px !important;
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
<asp:Panel runat="server" ID="pnlRegistration" Visible="false">
    
        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">Tell Us About the Parent/Guardian</h2>
                    
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlType" runat="server" LabelType="Type" />
                </div>
            </div>
            <div class="panel-body">
                <p class="step-description">We'll need some basic information about the parent or guardian who will be checking children in and out.</p>
				<div class="form-container">
					<div class="row">
						<div class="col-sm-6">
						    <Rock:RockTextBox runat="server" Label="Parent/Guardian First Name" Required="true" id="tbFirstname" />
                        </div>
						<div class="col-sm-6">
						    <Rock:RockTextBox runat="server" Label="Parent/Guardian Last Name" Required="true" id="tbLastName" />
                        </div>
					</div>
					<div class="row">
						<div class="col-sm-4 col-md-2">
						    <Rock:DatePicker runat="server" Label="Date of Birth" Required="true" id="dpBirthday" StartView="decade" />
						</div>
						<div class="col-sm-4 col-md-5">
						    <Rock:PhoneNumberBox runat="server" Label="Phone" Required="true" id="pnbPhone" />
						</div>
						<div class="col-sm-4 col-md-5">
                            <Rock:EmailBox runat="server" ID="ebEmail" Label="Email"  Required="true" />
						</div>
					</div>
                    <div class="row">
                        <div class="col-sm-12">
                            <span class="help-block">Note: The information above is required because it is necessary for matching records in our system.</span>
                        </div>
                    </div>
				</div>
            </div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" id="btnRegistrationNext" Text="Next" CssClass="pull-right btn btn-primary" OnClick="btnRegistrationNext_Click" />
            </div>
        </div>
</asp:Panel>


    <asp:Panel runat="server" ID="pnlChild" Visible="true">

        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">Tell Us About Each Child</h2>
            </div>
            <div class="panel-body">
                <p class="step-description">Please provide the information below about each child, so that we can get them into the right classroom. If you have additional children to check in, you will be able to add them on the next screen.</p>
				<div class="form-container">
                    <div class="row">
                        <div class="col-xs-12">
                            <asp:ValidationSummary runat="server" CssClass="alert alert-danger" />
                        </div>
                    </div>
					<div class="row">
						<div class="col-sm-4">
						    <Rock:RockTextBox runat="server" Label="Child's First Name" Required="true" id="tbChildFirstname" Text="Isaiah" />
						</div>
						<div class="col-sm-4">
						    <Rock:RockTextBox runat="server" Label="Child's Last Name" Required="true" id="tbChildLastname" Text="Bump" />
						</div>
						<div class="col-sm-4">
						    <Rock:DatePicker runat="server" Label="Date of Birth" Required="true" id="bpChildBirthday" Text="07/09/2009" StartView="decade" />
						</div>
					</div>
					<div class="row">
						<div class="col-sm-3">
                            <Rock:RockRadioButtonList runat="server" ID="rblGender" Label="Gender" Required="true" RepeatDirection="Horizontal" >
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
                            <Rock:RockTextBox runat="server" ID="tbSpecialNeeds" Label="Special Needs" />
						</div>
					</div>
				</div>
            </div>
            <div class="panel-footer clearfix text-right">
                <Rock:BootstrapButton runat="server" id="btnChildAddAnother" Text="Add Another Child" CssClass="btn btn-info" OnClick="btnChildAddAnother_Click" />
                <Rock:BootstrapButton runat="server" id="btnChildNext" Text="Next" CssClass="btn btn-primary" OnClick="btnChildNext_Click" />
            </div>
        </div>
    </asp:Panel>


    <asp:Panel runat="server" ID="pnlChildSummary" Visible="false">

        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">Your Children's Information</h2>
            </div>
            <div class="panel-body">
                <p class="step-description">Here's what we have for your children. Does everything here look good?</p>
                <div class="row">
                    <asp:PlaceHolder runat="server" ID="phChildSummary"></asp:PlaceHolder>
                </div>
            </div>
            <div class="panel-footer clearfix text-right">
                <Rock:BootstrapButton runat="server" id="btnChildAddAnotherFromSummary" Text="Add Another Child" CssClass="btn btn-info" OnClick="btnChildAddAnotherFromSummary_Click" />
                <Rock:BootstrapButton runat="server" id="btnChildSummaryNext" Text="Next" CssClass="btn btn-primary" OnClick="btnChildSummaryNext_Click" />
            </div>
        </div>
    </asp:Panel>

    <asp:Panel runat="server" ID="pnlCampus" Visible="false">

        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">Just a Few More Things</h2>
            </div>
            <div class="panel-body">
                <p class="step-description">Please let us know which service you'll be attending and how we can reach you.</p>
				<div class="form-container">
					<div class="row">
						<div class="col-sm-6">
                            <Rock:CampusPicker runat="server" ID="cpCampus" Label="Which Campus Are You Attending?" Required="true" />
							<p class="input-instruction">
								If you aren't sure which campus you will be attending, or if you decide to attend a campus other than the one you select here, that's OK. You will be able to check in your children at any of our campuses.
							</p>
						</div>
						<div class="col-sm-6">
                            <Rock:AddressControl runat="server" ID="adAddress" Label="Address" />
						</div>
					</div>
				</div>
			</div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" id="btnCampusNext" Text="Next" CssClass="pull-right btn btn-primary" OnClick="btnCampusNext_Click" />
            </div>
		</div>
    </asp:Panel>

    <asp:Panel runat="server" ID="pnlReview" Visible="false">
        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">Let's Review</h2>
            </div>
            <div class="panel-body">
                <p class="step-description">You're just about done. Let's just review your information and you will be all set!</p>
				<div class="form-container">
					<div class="row">
						<div class="col-sm-4">
							<h2>Parent/Guardian Information</h2>
                            <div class="form-inline">
                                <label class="form-label">Name: </label>
                                <asp:Literal runat="server" ID="lName" /><br />

                                <label class="form-label">Phone: </label>
                                <asp:Literal runat="server" ID="lPhone" /><br />

                                <label class="form-label">Date of Birth: </label>
                                <asp:Literal runat="server" ID="lDOB" />
                            </div>
						</div>
						<div class="col-sm-4">
							<h2>Children Registered</h2>
							<div id="children-names">Braden Ball, age 7<br />Charlie Ball, age 5<br />Matthew Schmidt, age 7<br/>Hannah Schmidt, age 9<br />Brady Byers, age 6</div>
							<button type="button" class="action" id="ReviewChildren">Review complete details</button>
						</div>
						<div class="col-sm-4">
							<h2>Campus &amp; Contact Information</h2>
                            
                            <div class="form-inline">
                                <label class="form-label">Campus: </label>
                                <asp:Literal runat="server" ID="lCampus" /><br />

                                <label class="form-label">Email: </label>
                                <asp:Literal runat="server" ID="lEmail" />
                            </div>
						</div>
					</div>
				</div>
			</div>
            <div class="panel-footer clearfix">
                <Rock:BootstrapButton runat="server" id="btnReviewFinish" Text="Finish" CssClass="pull-right btn btn-primary" OnClick="btnReviewFinish_Click" />
            </div>
		</div>
    </asp:Panel>

    <asp:Panel runat="server" ID="pnlConfirmation" Visible="false">
        <div class="panel panel-block">
            <div class="panel-heading">
                <h2 class="panel-title">You're All Set!</h2>
            </div>
            <div class="panel-body">
                <p class="step-description">We're so excited to worship with you!</p>
				<div class="form-container">
					<div class="row">
						<div class="col-sm-12">
							<p>If you have any questions, please don't hesitate to <a href="contact.php">contact us</a>.</p>

							<h2>Now What?</h2>
							<ul>
								<li>When you arrive, just head to the SE!Kids Check-in Desk to check-in your children.</li>
								<li>You will receive a tag to place on each child, as well as a tag for you to use to pick up your children after the service.</li>
								<li>Then, just take your children to the room listed on their tag.</li>
								<li>When the service is over, return to the same room where you dropped off your children and present your other tag to check them out.</li>
							</ul>
						</div>
					</div>
				</div>
			</div>
		</div>
    </asp:Panel>