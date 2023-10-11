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
    .actions-center {
        text-align:center;
    }
    table.emergencyContact {
        border: 1px solid #484848;
        margin-bottom:15px;
    }

    .emergencyContact th {
        font-weight: bold !important;
        font-size: 1em !important;
    }

    .emergencyContactAdd .row {
        margin-bottom:15px;
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

            <div class="actions actions-center" >
                <asp:LinkButton ID="lbGuestConfirm" runat="server" CssClass="btn btn-start btn-success" CausesValidation="false"><i class="fa fa-check"></i></asp:LinkButton>
                <asp:LinkButton ID="lbGuestCancel" runat="server" CssClass="btn btn-start btn-danger"  CausesValidation="false" ><i class="fa fa-times"></i></asp:LinkButton>
            </div>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlEmergencyContactConfirm" runat="server" Visible="false">
            <asp:Literal ID="lEmergencyContactConfirmMessage" runat="server"></asp:Literal>
            <asp:Repeater ID="rEmergencyContactConfirm" runat="server" Visible="false">
                <HeaderTemplate>
                    <table class="table table-striped emergencyContact">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Phone Number</th>
                                <th>Relationship</th>
                            </tr>
                        </thead>
                        <tbody>
                </HeaderTemplate>
                <ItemTemplate>
                            <tr>
                                <td><%# Eval("FullName") %></td>
                                <td><%# Eval("PhoneNumber") %></td>
                                <td><%# Eval("Relationship") %></td>
                            </tr>
                </ItemTemplate>
                <FooterTemplate>
                        </tbody>
                    </table>
                </FooterTemplate>
            </asp:Repeater>
            <div class="actions actions-center">

                    <asp:LinkButton ID="lbEmergencyContactConfirm" runat="server" CssClass="btn btn-success" CausesValidation="false">Confirm</asp:LinkButton>
                    <asp:LinkButton ID="lbEmergencyContactEdit" runat="server" CssClass="btn btn-primary" CausesValidation="false">Edit</asp:LinkButton>
            </div>
        </asp:Panel>
        <asp:Panel ID="pnlEmergencyContactEdit" runat="server" Visible="false">
            <h3>Update Emergency Contacts</h3>
            <asp:Literal ID="lEmergencyContactEdit" runat="server" />
            <asp:Panel ID="pnlEmergencyContactAddUpdate" CssClass="emergencyContactAdd" runat="server" Visible="false">
                <div class="row">
                    <div class="col-sm-3 col-sm-offset-3">
                        <label>First Name</label>
                    </div>
                    <div class="col-sm-3">
                        <Rock:RockTextBox ID="tbEmergencyContactFirstName" runat="server" Required="true" ValidationGroup="vg-emergency" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3 col-sm-offset-3">
                        <label>Last Name</label>
                    </div>
                    <div class="col-sm-3">
                        <Rock:RockTextBox ID="tbEmergencyContactLastName" runat="server" Required="true" ValidationGroup="vg-emergency" /> 
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3 col-sm-offset-3">
                        <label>Phone Number</label>
                    </div>
                    <div class="col-sm-3">
                        <Rock:PhoneNumberBox ID="phEmergencyContactPhone" runat="server" Required="true" ValidationGroup="vg-emergency" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-3 col-sm-offset-3">
                        <label>Relationship</label>
                    </div>
                    <div class="col-sm-3">
                        <Rock:RockDropDownList ID="ddlEmergencyContactRelationshp" runat="server" Required="true" ValidationGroup="vg-emergency">
                            <asp:ListItem Text="" Value="" />
                            <asp:ListItem Text="Spouse" Value="Spouse" />
                            <asp:ListItem Text="Parent" Value="Parent" />
                            <asp:ListItem Text="Sibling" Value="Sibling" />
                            <asp:ListItem Text="Grandparent" Value="Grandparent" />
                            <asp:ListItem Text="Friend" Value="Friend" />
                            <asp:ListItem Text="Other" Value="Other" />
                        </Rock:RockDropDownList>
                    </div>
                </div>
                <div class="actons">
                    <span class="pull-right">
                        <asp:LinkButton ID="lbEmergencyContactSave" runat="server" CssClass="btn btn-primary">Save</asp:LinkButton>
                        <asp:LinkButton ID="lbEmergencyContactCancel" runat="server" CssClass="btn btn-default">Cancel</asp:LinkButton>
                    </span>
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlEmergencyContactList" runat="server" Visible="false">
                <asp:Repeater ID="rEmergencyContactList" runat="server">
                    <HeaderTemplate>
                        <table class="table table-striped emergencyContact">
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Phone Number</th>
                                    <th>Relationship</th>
                                    <th>&nbsp;</th>
                                </tr>
                            </thead>
                            <tbody>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <tr>
                            <td><%# Eval("FullName") %></td>
                            <td><%# Eval("PhoneNumber") %></td>
                            <td><%# Eval("Relationship") %></td>
                            <td>
                                <span class="pull-right">
                                <asp:LinkButton ID="lbEdit" runat="server" CssClass="btn btn-sm btn-primary" CommandName="edit" CommandArgument='<%# Eval("MatrixItemId") %>' ><i class="fa fa-pencil"></i></asp:LinkButton>
                                <asp:LinkButton ID="lRemove" runat="server" CssClass="btn btn-sm btn-danger" CommandName="remove" CommandArgument='<%# Eval("MatrixItemId") %>' ><i class="fa fa-times"></i></asp:LinkButton>
                                </span>
                            </td>
                        </tr>
                    </ItemTemplate>
                    <FooterTemplate>
                        </tbody>
                        </table>
                    </FooterTemplate>
                </asp:Repeater>
            </asp:Panel>
        </asp:Panel>

        <asp:Panel ID="pnlFinish" runat="server" Visible="false" >
            <asp:Literal ID="lFinishMessage" runat="server" />
            <div class="actions pull-right">
                <asp:LinkButton ID="lbFinish" runat="server" Text="Finish" CssClass="btn btn-primary" />
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
