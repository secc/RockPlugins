<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignCodeManager.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Microframe.SignCodeManager" %>

<style>
    .codes {
        font-size: 1.3em;
    }
</style>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <asp:HiddenField runat="server" ID="hfSignCategory" />
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        <div class="content">
            <div class="row">
                <div class="col-sm-6">
                    <div class="panel panel-block">
                        <div class="panel-heading">
                            <h1 class="panel-title"><i class="fa fa-server"></i>
                                Sign Category List</h1>
                        </div>
                        <div class="panel-body">

                            <div class="grid grid-panel">
                                <Rock:Grid ID="gSignCategories" runat="server" ShowActionRow="false" ShowFooter="false" ShowHeader="false" AllowSorting="true" OnRowSelected="gSignCategories_Edit">
                                    <Columns>
                                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                                        <Rock:BadgeField DataField="Count" HeaderText="Count" SortExpression="Count" />
                                        
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="col-sm-6">
                    <asp:Panel CssClass="panel panel-block" runat="server" ID="pnlCategory" DefaultButton="btnAdd" Visible="false">
                        <div class="panel-heading">
                            <h1 class="panel-title"><i class="fa fa-wifi"></i>
                                <asp:Literal ID="ltCategory" runat="server" />
                            </h1>
                        </div>
                        <div class="panel-body">
                            <h2><asp:Literal runat="server" ID="ltCategoryName"></asp:Literal></h2>
                            <div class="input-group">
                                <Rock:RockTextBox runat="server" CssClass="form-control" ID="tbCode"></Rock:RockTextBox>
                                <span class="input-group-btn">
                                    <Rock:BootstrapButton runat="server" CssClass="btn btn-primary" ID="btnAdd"
                                        Text="Add Code" OnClick="btnAdd_Click"></Rock:BootstrapButton>
                                </span>
                            </div>
                            <h3>Codes:</h3>
                            <asp:PlaceHolder runat="server" ID="phCodes" />
                        </div>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
