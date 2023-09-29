<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GuestRegistration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.GuestRegistration" %>
<style type="text/css">
    .flex-container {
        display: flex;
    }

    .center-center {
        justify-content: center;
        align-items: center;
        top: 50%;
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
    }

    .btn-start {
        margin: 25px;
        font-size: 40px;
        font-weight: bolder;
        line-height: 1.5;
    }
    .btn-danger {
        background-color:#d4442e;
        color:#fff;
    }
    .input-existingGuest{
        text-align:center;
        font-size: 24px;
        font-weight:bold;
    }
</style>
<asp:UpdatePanel ID="upMain" runat="server">

    <ContentTemplate>
        <asp:Panel ID="pnlWelcome" runat="server" Visible="true">
            <asp:Literal ID="lWelcomeLava" runat="server" />
            <div id="divCommands" class="flex-container center-center">
                <asp:LinkButton ID="lbPreviousGuest" runat="server" CssClass="btn btn-lg btn-start btn-primary">Previous Guest</asp:LinkButton>
                <asp:LinkButton ID="lbNewGuest" runat="server" CssClass="btn btn-lg btn-start btn-primary">New Guest</asp:LinkButton>
            </div>
        </asp:Panel>


        <asp:Panel ID="pnlReturningGuest" runat="server" Visible="false" CssClass="container-fluid" DefaultButton="lbSearchReturningGuest">
            <Rock:NotificationBox ID="nbReturningGuest" runat="server" NotificationBoxType="Info" />
            <asp:ValidationSummary ID="valSummaryReturningGuest" runat="server" ValidationGroup="valReturningGuest" CssClass="alert alert-validation" />
            <asp:Literal ID="lReturningGuestMessage" runat="server" />
            <div class="row" style="padding-bottom: 15px;">
                <div class="col-sm-6 col-sm-offset-3">
                    <Rock:RockTextBox ID="tbReturningGuestNumber" runat="server" type="number" Required="true" CssClass="form-control input-lg input-existingGuest"
                        RequiredErrorMessage="Phone Number Required" ValidationGroup="valReturningGuest" />
                </div>
            </div>
            <div class="row" style="padding-bottom: 15px;">
                <div class="col-sm-6 col-sm-offset-3" style="text-align: center;">
                    <asp:LinkButton ID="lbSearchReturningGuest" runat="server" Text="Search" CssClass="btn btn-lg btn-primary" ValidationGroup="valReturningGuest" />
                    <asp:LinkButton ID="lbCancelReturningGuest" runat="server" Text="Cancel" CssClass="btn btn-lg btn-cancel" CausesValidation="false" />
                </div>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlNewGuest" runat="server" Visible="false" CssClass="container-fluid" DefaultButton="lbSaveNewGuest">
            <Rock:NotificationBox ID="nbNewGuest" runat="server" NotificationBoxType="Info" />
            <asp:ValidationSummary ID="valNewGuest" runat="server" CssClass="alert alert-validation" ValidationGroup="vgNewGuest" />
            <asp:Literal ID="lNewGuest" runat="server" />
            <asp:HiddenField ID="hfPersonGuidNewGuest" runat="server" />
            <div class="row">
                <div class="col-sm-6">
                    <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" RequiredErrorMessage="First Name is Required" ValidationGroup="vgNewGuest" />
                </div>
                <div class="col-sm-6">
                    <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true" RequiredErrorMessage="Last Name is Required" ValidationGroup="vgNewGuest" />
                </div>

            </div>
            <div class="row">
                <div class="col-sm-6">
                    <Rock:DatePicker ID="dpBirthDate" runat="server" Label="Birthdate" Required="true" RequiredErrorMessage="Birthdate is required" ValidationGroup="vgNewGuest" />
                </div>
                <div class="col-sm-6">
                    <Rock:RockDropDownList ID="ddlGender" runat="server" Label="Gender" Required="true" RequiredErrorMessage="Gender is required" ValidationGroup="vgNewGuest" >
                        <asp:ListItem Value="" Text="" Selected="True" />
                        <asp:ListItem Value="1" Text="Male" />
                        <asp:ListItem Value="2" Text="Female" />
                    </Rock:RockDropDownList>
                </div>

            </div>

            <div class="row">
                <div class="col-sm-6">
                    <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" TextMode="Email" Required="true" RequiredErrorMessage="Email is Required" ValidationGroup="vgNewGuest" />
                </div>
                <div class="col-sm-6">
                    <Rock:PhoneNumberBox ID="tbMobile" runat="server" Label="Mobile Number" Required="true" RequiredErrorMessage="Mobile Phone is Required" ValidationGroup="vgNewGuest" />
                </div>
            </div>
            <div class="actions pull-right">
                <asp:LinkButton ID="lbSaveNewGuest" runat="server" CssClass="btn btn-primary" Text="Next" ValidationGroup="valNewGuest" CausesValidation="true" />
                <asp:LinkButton ID="lbCancelNewGuest" runat="server" CssClass="btn btn-cancel" Text="Cancel" CausesValidation="false" />
            </div>

        </asp:Panel>
        <asp:Panel ID="pnlLoadGuest" runat="server" Visible="false" CssClass="container-fluid">
            <asp:Literal ID="lLoadGuestMessage" runat="server" />
            <div class="row">
                <div class="center-block" style="text-align:center;">
                    <div class="col-sm-6 col-sm-offset-3" >
                        <asp:LinkButton ID="lbGuestConfirm" runat="server" CssClass="btn btn-start btn-success" CausesValidation="false"><i class="fa fa-check"></i></asp:LinkButton>
                        <asp:LinkButton ID="lbGuestCancel" runat="server" CssClass="btn btn-start btn-danger"  CausesValidation="false" ><i class="fa fa-times"></i></asp:LinkButton>
                    </div>

                </div>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlFinish" runat="server" Visible="false" >
            <asp:Literal ID="lFinishMessage" runat="server" />
            <div class="actions pull-right">
                <asp:LinkButton ID="lbFinish" runat="server" Text="Finish" CssClass="btn btn-primary" />
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
