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
    .CampusDropdown {
        position:absolute;
        right:50px;
        width:200px;
    }
</style>



<asp:UpdatePanel ID="upCampus" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog ID="mdLockdownAlert" runat="server" Title="Send Lockdown Alert" OnSaveClick="mdLockdownAlert_SaveClick" SaveButtonText="Cancel" SaveButtonCssClass="btn btn-link" CancelLinkVisible="false">
            <Content>
                <%-- show current alert title and message --%>
                 <div class="panel-body" style="text-align:center;">
                            <Rock:RockLiteral ID="lAlertTitle" Label="Alert Title" runat="server" />
                            <Rock:RockLiteral ID="lMessage" Label="Alert Message" runat="server" />
                        </div>

                <%-- send buttons --%>
                <div style="text-align: center">
                        <asp:LinkButton ID="btnStaffVol" runat="server" Width="300"
                            OnClick="btnStaffVol_Click"  Text="Staff & Volunteers" CssClass="btn btn-default buttonClass" />

                        <asp:LinkButton ID="btnStaff" runat="server" Width="300"
                            OnClick="btnStaff_Click"  Text="Staff Only" CssClass="btn btn-default buttonClass" />
                    </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdCustomMessage" runat="server" Title="Create Lockdown Alert" SaveButtonText="Send" OnSaveClick="mdCustomMessage_SaveClick">
            <Content>
                <%-- single-line textbox --%>
                <Rock:RockTextBox Label="Alert Title" ID="tbAlertName" runat="server" Placeholder="Type Alert Title" Required="true" />

                <%-- multi-line textbox --%>
                <Rock:RockTextBox Label="Alert Message" ID="tbAlertMessage" runat="server" TextMode="MultiLine" Placeholder="Type a message" Required="true" />

                <%-- select audience --%>
                <Rock:RockCheckBox ID="cbCustomMessageIncludeVols" Label="Include Volunteers" Text="Yes" runat="server" />

                
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
                    <i class="fas fa-shield-alt" style="padding-right:1em;"></i>
                    <asp:Label ID="lbLockDown" runat="server" Text="Lock Down Protocol" />
                    <Rock:ButtonDropDownList ID="bddlCampus" runat="server" FormGroupCssClass="panel-options pull-right CampusDropdown"
                        Title="Campus" SelectionStyle="Checkmark" OnSelectionChanged="bddlCampus_SelectionChanged" DataTextField="Name" DataValueField="Id" AutoPostBack="true" />
                </div>
                <div class="panel-body">
                    <div class="text-center">
                        <div>
                            <h2><asp:Label ID="lCampusTitle" runat="server" /></h2>
                            <div style="display: inline-block;">
                        </div>
                        </div>
                       
                        

                    </div>
                    <br />
                    <div style="text-align: center">
                        <asp:LinkButton ID="btnLockdownAlert" runat="server" Width="300"
                            OnClick="btnLockdownAlert_Click"  Text="Lockdown Alert" CssClass="btn btn-default buttonClass" />

                        <asp:LinkButton ID="btnCustomAlert" runat="server" Width="300"
                            OnClick="btnCustomAlert_Click"  Text="Custom Alert" CssClass="btn btn-default buttonClass" />
                    </div>
                </div>
            </div>
        </asp:Panel>

 

    </ContentTemplate>
</asp:UpdatePanel>
