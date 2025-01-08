<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonActions.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.PersonActions" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <style>
            li a {
                text-decoration: none !important;
            }

            #divChildcareModal .row {
                padding-bottom: 15px;
            }

            #divChildcareModal textarea {
                border: 0px;
            }

            .pinList {
                padding-left: 0px;
            }

                .pinList li {
                    padding-left: 15px;
                    font-weight: bold;
                    line-height: 1.3em;
                }

                .pinList .actions {
                    opacity: .2;
                    -webkit-transition: opacity .5s ease-out;
                    -moz-transition: opacity .5s ease-out;
                    -o-transition: opacity .5s ease-out;
                    transition: opacity .5s ease-out;
                }

                    .pinList .actions:hover {
                        opacity: 1
                    }
        </style>
        <div class="panel panel-default list-as-blocks clearfix">
            <div class="panel-heading">
                <h1 class="panel-title">Person Actions</h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbPersonActions" runat="server" />
                <ul>
                    <asp:Repeater ID="rptActions" runat="server" OnItemCommand="rptActions_ItemCommand">
                        <ItemTemplate>
                            <li>
                                <asp:LinkButton ID="lbAction" runat="server" CausesValidation="false" CommandName='<%# Eval("CommandName") %>' >
                                    <i class="<%# Eval("IconCSS") %>"></i>
                                    <h3><%# Eval("Name") %></h3>
                                    <asp:Literal ID="lCount" runat="server" />
                                </asp:LinkButton>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </div>
        </div>
        <Rock:ModalDialog ID="mdlPINs" runat="server" Title="Manage PINs" SaveButtonText="Update" SaveButtonCausesValidation="false" OnSaveClick="mdlPINs_SaveClick" CancelLinkVisible="false" >
            <Content>
                <div class="row">
                    <div class="col-xs-12">
                        <table class="table table-bordered">
                            <thead>
                                <tr>
                                    <th>PIN Number</th>
                                    <th>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                <asp:Repeater ID="rptPIN" runat="server" OnItemCommand="rptPIN_ItemCommand"  OnItemDataBound="rptPIN_ItemDataBound">
                                    <ItemTemplate>
                                        <tr>
                                            <td><%# Eval("PIN") %></td>
                                            <td>
                                                <asp:LinkButton ID="lbDeletePIN" runat="server" CommandName="DeletePIN" CssClass="btn btn-delete"><i class="fa fa-remove"</asp:LinkButton>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </tbody>
                        </table>
                    </div>
                </div>
                <Rock:NotificationBox ID="nbPIN" runat="server" NotificationBoxType="Info" />
                <div class="row">
                    <div class="col-xs-6 col-md-3 ">
                        <Rock:RockTextBox ID="tbPIN" Label="Add New PIN" runat="server" />
                    </div>
                    <div class="col-xs-6 col-md-9" style="padding-top:30px;">
                        <asp:LinkButton ID="lbSavePIN" runat="server" CssClass="btn btn-sm btn-primary" CausesValidation="false">Add</asp:LinkButton>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdlAddCredits" runat="server" SaveButtonText="Add Credits" >
            <Content>
                <div class="row">
                    <div class="col-xs-6">
                        <label class="control-label">Existing Credits</label>
                    </div>
                    <div class="col-xs-6" >
                        <Rock:NumberBox ID="tbExistingCredits" runat="server" CssClass="input-width-md" ReadOnly="true" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-xs-6">
                        <label class="control-label">Credits To Add</label>
                    </div>
                    <div class="col-xs-6">
                        <Rock:NumberBox ID="tbCreditsToAdd" runat="server" CssClass="input-width-md" Placeholder="0"/>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
