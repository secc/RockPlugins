<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SuperCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.SuperCheckin" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <Rock:ModalDialog ID="mdCheckin" runat="server" Title="Check-In" OnSaveClick="mdCheckin_SaveClick" CancelLinkVisible="false" SaveButtonText="Save Check-In State">
            <Content>
                <Rock:Toggle runat="server" ID="cbSuperCheckin" OnCssClass="btn-success"
                    OffCssClass="btn-danger" OnCheckedChanged="cbSuperCheckin_CheckedChanged"
                    Checked="false" Label="Super Check-In" />
                <asp:PlaceHolder ID="phCheckin" runat="server" />
            </Content>
        </Rock:ModalDialog>

        <!-- New Family -->
        <asp:Panel ID="pnlNewFamily" Visible="false" runat="server">
            New Family
        </asp:Panel>

        <!-- Manage Family -->
        <asp:Panel ID="pnlManageFamily" Visible="false" runat="server" CssClass="container">
            <div class="row">
                <div class="col-md-3" style="background-color: black; color: white;">
                    <h2>Family Members</h2>
                    <asp:PlaceHolder runat="server" ID="phFamilyMembers" />
                    <Rock:BootstrapButton runat="server" ID="btnNewMember" Text="Add Person" OnClick="btnNewMember_Click"
                        CssClass="btn btn-primary btn-block btn-lg"></Rock:BootstrapButton>
                    <Rock:BootstrapButton runat="server" ID="btnBack" CssClass="btn btn-danger btn-block" Text="<i class='fa fa-arrow-left' aria-hidden='true'></i>" OnClick="btnBack_Click" />
                </div>
                <div class="col-md-9">
                    <!-- Personal Info Pannel-->
                    <asp:Panel runat="server" ID="pnlPersonInformation" Visible="false">
                        <div class="row">
                            <div class="col-md-8">
                                <h1>
                                    <asp:Literal Text="" ID="ltName" runat="server" />
                                </h1>

                            </div>
                            <div class="col-md-4">
                                <Rock:BootstrapButton ID="btnCheckin" CssClass="btn btn-primary btn-lg"
                                    runat="server" Text="<i class='fa fa-check' aria-hidden='true'></i>" OnClick="btnCheckin_Click" />
                                <Rock:BootstrapButton ID="btnPrint" CssClass="btn btn-primary btn-lg"
                                    runat="server" Text="<i class='fa fa-print' aria-hidden='true'></i>" OnClick="btnPrint_Click" />
                            </div>
                        </div>
                        <asp:PlaceHolder runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlAddPerson" runat="server" Visible="false">
                        <h1>Add New Person</h1>
                        <Rock:Toggle runat="server" ID="cbRelationship" Checked="true" OnText="Immediate Family Member" OffText="Other Relationship"
                            Label="Relationship" Help="Is the child part of the family (immediate), or a relative or friend (other)."
                            OnCssClass="btn-success" OffCssClass="btn-success" />
                        <div class="row">
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockTextBox ID="tbNewPersonFirstName" runat="server" Label="First Name"  />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockTextBox ID="tbNewPersonLastName" runat="server" Label="Last Name" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockDropDownList ID="ddlNewPersonSuffix" runat="server" Label="Suffix"  CssClass="input-width-md" />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:RockRadioButtonList ID="rblNewPersonGender" runat="server" Label="Gender" RepeatDirection="Horizontal"  />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:DatePicker ID="dpNewPersonBirthDate" runat="server" Label="Birthdate"  />
                            </div>
                            <div class="col-xs-12 col-sm-4">
                                <Rock:GradePicker ID="ddlGradePicker" runat="server" Label="Grade"  UseAbbreviation="true" UseGradeOffsetAsValue="true" />
                            </div>
                            <div class="col-xs-12 col-sm-6">
                                <Rock:RockTextBox runat="server" ID="tbAlergy" Label="Alergy Information"></Rock:RockTextBox>
                            </div>
                            <div class="col-xs-12 col-sm-6">
                                <Rock:RockTextBox runat="server" ID="tbLegal" Label="Legal Information"></Rock:RockTextBox>
                            </div>
                        </div>


                        <Rock:BootstrapButton runat="server" ID="btnSaveAddPerson" Text="Save New Person" CssClass="btn btn-default"
                             OnClick="btnSaveAddPerson_Click"></Rock:BootstrapButton>
                    </asp:Panel>

                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
