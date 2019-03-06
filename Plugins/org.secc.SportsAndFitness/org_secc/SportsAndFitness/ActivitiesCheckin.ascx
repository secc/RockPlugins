<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ActivitiesCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ActivitiesCheckin" %>
<style>
    .checkin-header {
        position: fixed;
        top: 0;
        left: 0;
        background-color: #ff7e4e;
        border-bottom: black solid 1px;
        width: 100vw;
        padding: 10px;
    }

    .btn-danger {
        background-color: white;
    }

    .pull-right {
        margin-right: 20px;
    }

    .js-modaldialog-cancel-link {
        visibility: hidden;
    }

    .radio {
        display: inline;
    }
</style>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdCamera">
            <Content>
                <div id="videoDiv" class="text-center">
                    <video id="video" width="640" height="480" autoplay></video>
                    <br />
                    <a href="javascript:void(0);" id="snap" class="btn btn-lg btn-primary"><i class="fa fa-camera"></i>Snap Photo</a>
                </div>
                <div id="photoDiv" class="text-center hidden">
                    <canvas id="canvas" width="640" height="480"></canvas>
                    <br />
                    <a href="javascript:void(0);" class="btn btn-lg btn-danger" id="retakeImage"><i class="fa fa-times"></i>Retake Photo</a>
                    <a href="javascript:void(0);" class="btn btn-lg btn-success" id="saveImage"><i class="fa fa-check"></i>Keep Photo</a>
                </div>
                <br />
                <div class="text-center">
                    <Rock:BootstrapButton runat="server" CssClass="btn btn-danger" Text="Cancel" ID="btnCameraCancel" OnClick="btnCameraCancel_Click"></Rock:BootstrapButton>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdAddPerson" CloseLinkVisible="false">
            <Content>
                <div class="row">
                    <asp:PlaceHolder runat="server" ID="phAddPerson" />
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdSearchPerson" Title="Add New Guest" CloseLinkVisible="false">
            <Content>
                <div class="container">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" ID="tbSearch"></Rock:RockTextBox>
                        </div>
                        <div class="col-md-4">
                            <Rock:BootstrapButton runat="server" ID="btnPhone" Text="Search By Phone" OnClick="btnPhone_Click" CssClass="btn btn-primary" />
                            <Rock:BootstrapButton runat="server" ID="btnName" Text="Search By Name" OnClick="btnName_Click" CssClass="btn btn-primary " />
                        </div>
                        <div class="col-md-2">
                            <Rock:BootstrapButton runat="server" ID="btnCancelSearch" Text="Cancel" OnClick="btnCancelSearch_Click" CssClass="btn btn-danger pull-right" />
                        </div>
                    </div>
                    <br />
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:Grid runat="server" ID="gPeopleToAdd" Visible="false" ShowActionRow="false" ShowFooter="false"
                                DataKeyNames="Id" AllowPaging="false">
                                <Columns>
                                    <Rock:RockBoundField DataField="Person.FullName" HeaderText="Name" />
                                    <Rock:RockBoundField DataField="Person.Email" HeaderText="Email" />
                                    <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" />
                                    <Rock:RockBoundField DataField="Address" HeaderText="Address" />
                                    <Rock:LinkButtonField Text="Add Guest" HeaderText="Person To Add" ID="lbAddGuest" OnClick="lbAddGuest_Click"></Rock:LinkButtonField>
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </div>
                    <br />
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:BootstrapButton runat="server" ID="btnOpenCreatePerson" Visible="false"
                                Text="Create New Person" CssClass="btn btn-default" OnClick="btnOpenCreatePerson_Click"></Rock:BootstrapButton>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdCreatePerson">
            <Content>
                <div class="row">
                    <div class="col-xs-12 col-sm-5">
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" ValidationGroup="CreatePerson" Required="true" />
                    </div>
                    <div class="col-xs-12 col-sm-5">
                        <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" ValidationGroup="CreatePerson" Required="true" />
                    </div>
                    <div class="col-xs-12 col-sm-2">
                        <Rock:RockDropDownList ID="ddlSuffix" runat="server" Label="Suffix" CssClass="input-width-md" />
                    </div>

                    <div class="col-xs-6 ">
                        <Rock:RockRadioButtonList ID="rblGender" runat="server" Label="Gender" ValidationGroup="CreatePerson" Required="true" />
                    </div>
                    <div class="col-xs-6 ">
                        <Rock:BirthdayPicker runat="server" ID="bpBirthday" Label="Birthday" RequireYear="true" ValidationGroup="CreatePerson" Required="true" />
                    </div>
                    <div class="col-sm-5">
                        <Rock:EmailBox ID="ebEmail" runat="server" Label="Email Address" ValidationGroup="CreatePerson" Required="true"></Rock:EmailBox>
                    </div>
                    <div class="col-sm-5">
                        <Rock:PhoneNumberBox ID="pnbPhone" runat="server" Label="Phone Number" ValidationGroup="CreatePerson" Required="true"></Rock:PhoneNumberBox>
                    </div>
                    <div class="col-sm-2">
                        <Rock:Toggle runat="server" ID="cbSMS" Label="Is Cell Phone?" OnCssClass="btn-success"
                            OffCssClass="btn-danger" Checked="true" OnText="Yes" OffText="No" />
                    </div>
                </div>
                <Rock:CampusPicker runat="server" ID="cpNewFamilyCampus" DataValueField="Id" DataTextField="Name"
                    ValidationGroup="CreatePerson" Required="true" SelectedCampusId="1">
                </Rock:CampusPicker>
                <Rock:AddressControl runat="server" ID="acNewFamilyAddress" ValidationGroup="CreatePerson" Required="true" />
                <Rock:BootstrapButton runat="server" CausesValidation="true" ValidationGroup="CreatePerson" ID="btnNewFamily" Text="Create New Guest"
                    CssClass="btn btn-primary" OnClick="btnNewFamily_Click" />
                <asp:LinkButton runat="server" ID="lbCancel" CausesValidation="false" Text="Cancel" CssClass="btn btn-danger" OnClick="lbCancel_Click">
                </asp:LinkButton>
            </Content>
        </Rock:ModalDialog>


        <Rock:ModalAlert ID="maError" runat="server" />
        <asp:Panel runat="server" ID="pnlMain">
            <div style="margin-top: 50px">
                <div class="container-fluid">
                    <div class="row">
                        <Rock:NotificationBox NotificationBoxType="Validation" runat="server" Dismissable="true"></Rock:NotificationBox>
                        <asp:PlaceHolder runat="server" ID="phMembers"></asp:PlaceHolder>
                    </div>
                </div>
            </div>
            <div class="checkin-header">
                <Rock:BootstrapButton runat="server" ID="btnAdd" CssClass="btn btn-default btn-lg" OnClick="btnAdd_Click"><i class="fa fa-plus"></i> Add Person</Rock:BootstrapButton>
                <Rock:BootstrapButton ID="btnCheckin" CausesValidation="true" runat="server" Text="Check-In" CssClass="btn btn-lg btn-success pull-right" OnClick="btnCheckin_Click"><i class="fa fa-check"></i> Check In</Rock:BootstrapButton>
                <Rock:BootstrapButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-lg btn-danger pull-right" OnClick="btnCancel_Click"><i class="fa fa-times"></i> Cancel</Rock:BootstrapButton>
            </div>

        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNoEligibleMembers" Visible="false">
            <Rock:NotificationBox ID="nbNoEligibleMembers" CssClass="noMembers" Text="There are no eligible people for check-in today." runat="server"
                NotificationBoxType="Warning"></Rock:NotificationBox>
            <Rock:BootstrapButton ID="btnBack" runat="server" Text="Back" OnClick="btnBack_Click" CssClass="btn btn-lg btn-primary"></Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
