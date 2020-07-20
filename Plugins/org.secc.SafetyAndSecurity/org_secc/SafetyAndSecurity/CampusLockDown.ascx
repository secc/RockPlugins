<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampusLockDown.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SafetyAndSecurity.CampusLockDown" %>


<style type="text/css">
    .buttonClass {
        display: inline-block;
        font-size: 2rem;
        color: #484848;
        margin: 0px 5% 0px auto;
        white-space: normal;
        background-color: #eee;
        border: 1px solid #d4d4d4;
        border-radius: 5px;
        text-transform: uppercase;
        font-weight: 800;
        min-height: 180px;
        vertical-align: middle;
        line-height: 180px;
        white-space: nowrap;
        -webkit-transition: all 200ms linear;
        -moz-transition: all 200ms linear;
        -o-transition: all 200ms linear;
        transition: all 200ms linear;
    }

        .buttonClass:last-of-type {
            margin: 0px auto 0px auto;
        }

        .buttonClass:hover {
            border: solid 1px #ebccd1;
            background-color: #f2dede;
            color: #a94442;
        }

    .campusClass h3 {
        padding: 30px;
        margin-bottom: 5px;
        font-weight: 700;
        text-transform: uppercase;
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
    }
</style>



<asp:UpdatePanel ID="upCampus" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog ID="mdCustomMessage" runat="server" Title="Create Lockdown Alert" OnSaveClick="mdCustomMessage_SaveClick"  SaveButtonText="Save">
            <Content>
                <%-- single-line textbox --%>
                <p>Alert Name:</p>
                <Rock:RockTextBox ID="tbAlertName" runat="server" CssClass="js-sms-text-message" Placeholder="Type Alert Name" Required="false"  ValidateRequestMode="Disabled" />

                <%-- multi-line textbox --%>
                <p>Message:</p>
                <Rock:RockTextBox ID="tbAlertMessage" runat="server" CssClass="js-sms-text-message" TextMode="MultiLine" Rows="3" Placeholder="Type a message" Required="false"  ValidateRequestMode="Disabled" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalAlert ID="maPopup" runat="server" />

        <asp:Panel runat="server" ID="pnlCampuses">
            <div class="panel panel-default list-as-blocks clearfix">
                <div class="panel-heading">
                    <i class="fas fa-shield-alt"></i>
                    Lock Down Protocol
      
                </div>
                <div class="panel-body">
                    <h3 style="margin-bottom: 15px; font-weight: 600;">Select a Campus:</h3>
                    <ul>

                        <asp:Repeater runat="server" ID="rptCampuses" OnItemCommand="rptCampuses_ItemCommand">
                            <ItemTemplate>
                                <li>
                                    <div style="padding: 30px auto;">
                                        <asp:LinkButton ID="btnCampus" runat="server" CssClass="campusClass" Text='<%# "<h3>" + Eval("Name") + "</h3>" %>' CommandArgument='<%# Eval("Id") %>' />
                                    </div>
                                </li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </div>
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlInfo" Visible="false">
            <div class="panel-heading">
                <asp:Literal Text="Information" runat="server" ID="ltHeading" />
            </div>
            <div class="panel-body">
                <asp:Literal Text="" runat="server" ID="ltBody" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlMain" Visible="true">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <i class="fas fa-shield-alt"></i>
                    <asp:Label ID="lbLockDown" runat="server" Text="Lock Down Protocol" />
                    <Rock:ButtonDropDownList ID="bddlCampus" runat="server" FormGroupCssClass="panel-options pull-right"
                        Title="Campus" SelectionStyle="Checkmark" OnSelectionChanged="bddlCampus_SelectionChanged" DataTextField="Name" DataValueField="Id" />
                </div>
                <div class="panel-body">
                    <div class="text-center">
                        <div class="alert alert-warning" role="alert">
                            You are about to send a <strong>Lock Down Alert</strong> for <span style="font-weight: 700; text-decoration: underline;">
                                <asp:Label ID="lCampusTitle" runat="server" /></span>! 
                        </div>
                        <div style="display: inline-block;">
                            <Rock:RockLiteral ID="lAlertTitle" runat="server" />
                            <i>
                                <Rock:RockLiteral ID="lMessage" runat="server" /></i>
                        </div>
                        <div style="display: inline-block;">
                            <asp:LinkButton ID="btnEdit" runat="server" Title="Edit Message" CssClass="btn btn-sm btn-square btn-default " OnClick="btnEdit_Click">
                            <i class="fa fa-pencil"></i> </asp:LinkButton>
                        </div>

                    </div>
                    <br />
                    <div style="text-align: center">
                        <asp:LinkButton ID="btnStaffVol" runat="server" Width="300"
                            OnClick="btnStaffVol_Click"  Text="Staff & Volunteers" CssClass="btn btn-default buttonClass" />

                        <asp:LinkButton ID="btnStaff" runat="server" Width="300"
                            OnClick="btnStaff_Click"  Text="Staff" CssClass="btn btn-default buttonClass" />
                        <br />
                        <br />
                    </div>
                </div>
            </div>
        </asp:Panel>

 

    </ContentTemplate>
</asp:UpdatePanel>
