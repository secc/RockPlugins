<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupTypeChanger.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.GroupTypeChanger" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox runat="server" ID="nbMessage" Visible="false" NotificationBoxType="Success" Text="GroupType successfully changed."></Rock:NotificationBox>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h4 class="panel-title"><asp:Literal ID="ltName" runat="server"></asp:Literal></h4>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockLiteral ID="ltGroupTypeName" runat="server" Label="Current Group Type"></Rock:RockLiteral>
                    </div>
                    <div class="col-md-6">
                        <Rock:RockDropDownList runat="server" ID="ddlGroupTypes" Label="New Group Type" DataValueField="Id" DataTextField="Name"
                            AutoPostBack="true" OnSelectedIndexChanged="ddlGroupTypes_SelectedIndexChanged">
                        </Rock:RockDropDownList>
                    </div>
                </div>
            </div>
        </div>

        <div class="row">

            <asp:Panel runat="server" ID="pnlGroupAttributes" Visible="false">
                <div class="panel panel-block">
                    <div class="panel-heading">
                        <h4 class="panel-title">Group Attribute Mappings</h4>
                    </div>
                    <div class="panel-body">
                        <asp:PlaceHolder runat="server" ID="phGroupAttributes" />
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlMemberAttributes" Visible="false">
                <div class="panel panel-block">
                    <div class="panel-heading">
                        <h4 class="panel-title">Group Member Attribute Mappings</h4>
                    </div>
                    <div class="panel-body">
                        <asp:PlaceHolder runat="server" ID="phMemberAttributes" />
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlRoles" Visible="false">
                <div class="panel panel-block">
                    <div class="panel-heading">
                        <h4 class="panel-title">Group Member Role Mappings</h4>
                    </div>
                    <div class="panel-body">
                        <asp:PlaceHolder runat="server" ID="phRoles" />
                    </div>
                </div>
            </asp:Panel>
        </div>

        <div class="row">
            <div class="col-xs-12">
                <Rock:BootstrapButton runat="server" ID="btnSave" CssClass="btn btn-success" Text="Save"
                     Visible="false" OnClick="btnSave_Click"></Rock:BootstrapButton>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
