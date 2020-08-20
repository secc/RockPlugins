<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PublishGroupRegistration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.PublishGroupRegistration" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbNotice" runat="server" Visible="false" NotificationBoxType="Danger" />
        <Rock:NotificationBox ID="nbWarning" runat="server" Visible="false" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlView" runat="server">

            <asp:Literal ID="lLavaOverview" runat="server" />
            <asp:Literal ID="lLavaOutputDebug" runat="server" />

            <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
            <asp:Panel runat="server" ID="pnlForm">

                <div class="row">
                    <asp:Panel ID="pnlCol1" runat="server">
                        <div class="row">
                            <div class="col-sm-6"><Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true"></Rock:RockTextBox></div>
                            <div class="col-sm-6"><Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true"></Rock:RockTextBox></div>
                        </div>
                        
                        
                        <Rock:DatePartsPicker ID="dppDOB" runat="server" Label="Birth Date" FutureYearCount="0" />
                        <div class="row">
                            <asp:Panel ID="pnlHomePhone" runat="server" CssClass="col-sm-6">
                                    <Rock:PhoneNumberBox ID="pnHome" runat="server" Label="Home Phone" />
                            </asp:Panel>
                            <asp:Panel ID="pnlCellPhone" runat="server" CssClass="col-sm-6">
                                
                                <div class="pull-right" style="margin-left: 15px;">
                                    <Rock:RockCheckBox ID="cbSms" runat="server" Label="&nbsp;" Text="Enable SMS" />
                                </div>
                                <Rock:PhoneNumberBox ID="pnCell" runat="server" Label="Cell Phone" />
                            </asp:Panel>
                        </div>
                        <Rock:EmailBox ID="tbEmail" runat="server" Label="Email"></Rock:EmailBox>
                        <Rock:AddressControl ID="acAddress" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlCol2" runat="server" CssClass="col-md-6">
                        <div class="row">
                            <div class="col-sm-6"><Rock:RockTextBox ID="tbSpouseFirstName" runat="server" Label="Spouse First Name"></Rock:RockTextBox></div>
                            <div class="col-sm-6"><Rock:RockTextBox ID="tbSpouseLastName" runat="server" Label="Spouse Last Name"></Rock:RockTextBox></div>
                        </div>
                        <Rock:DatePartsPicker ID="dppSpouseDOB" runat="server" Label="Spouse Birth Date" FutureYearCount="0" />
                        <div class="pull-right" style="margin-left: 15px;">
                            <Rock:RockCheckBox ID="cbSpouseSms" runat="server" Label="&nbsp;" Text="Enable SMS" />
                        </div>
                        <Rock:PhoneNumberBox ID="pnSpouseCell" runat="server" Label="Spouse Cell Phone" />
                        <Rock:EmailBox ID="tbSpouseEmail" runat="server" Label="Email" />
                    </asp:Panel>

                </div>

                <div class="actions">
                    <asp:LinkButton ID="btnRegister" runat="server" CssClass="btn btn-primary" OnClick="btnRegister_Click" />
                </div>

            </asp:Panel>
        </asp:Panel>

        <asp:Panel ID="pnlResult" runat="server" Visible="false">
            <asp:Literal ID="lResult" runat="server" />
            <asp:Literal ID="lResultDebug" runat="server" />
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
