<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Microframe.SignDetail" %>

<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdSignCategories" ValidateRequestMode="Disabled" ValidationGroup="None" OnSaveClick="mdSignCategories_SaveClick" CancelLinkVisible="true" Title="Add Category">
            <Content>
                <Rock:RockDropDownList DataValueField="Id" DataTextField="Name" runat="server" ID="ddlSignCategories" Label="Category">
                </Rock:RockDropDownList>
            </Content>
        </Rock:ModalDialog>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server" Visible="false">


            <asp:HiddenField ID="hfSignId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-wifi"></i>
                    <asp:Literal ID="lActionTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">

                <asp:ValidationSummary ID="ValidationSummary1" ValidationGroup="Main" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <fieldset>

                    <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                    <Rock:NotificationBox ID="nbDuplicateSign" runat="server" NotificationBoxType="Warning" Title="Sorry" Visible="false" />
                    <Rock:DataTextBox ID="tbName" ValidationGroup="Main" runat="server" SourceTypeName="org.secc.Microframe.Model.Sign, org.secc.Microframe" PropertyName="Name" Required="true" />
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:DataTextBox ID="tbIPAddress" ValidationGroup="Main" runat="server" SourceTypeName="org.secc.Microframe.Model.Sign, org.secc.Microframe" PropertyName="IPAddress" Required="true" />
                        </div>
                        <div class="col-md-4">
                            <Rock:DataTextBox ID="tbPort" ValidationGroup="Main" runat="server" SourceTypeName="org.secc.Microframe.Model.Sign, org.secc.Microframe" PropertyName="Port" Required="true" />
                        </div>
                        <div class="col-md-4">
                            <Rock:DataTextBox ID="tbPIN" ValidationGroup="Main" runat="server" SourceTypeName="org.secc.Microframe.Model.Sign, org.secc.Microframe" PropertyName="PIN" Required="true" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:DataTextBox ID="tbDescription" ValidationGroup="Main" runat="server" SourceTypeName="org.secc.Microframe.Model.Sign, org.secc.Microframe" PropertyName="Description" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <h3>Sign Categories</h3>
                            <Rock:Grid ID="gCategories" runat="server" DisplayType="Light" RowItemText="Sign Category"
                                 ShowConfirmDeleteDialog="false">
                                <Columns>
                                    <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                                    <Rock:DeleteField OnClick="gCategories_Delete" />
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </div>
                    <div class="actions">
                        <asp:LinkButton ID="btnSave" ValidationGroup="Main" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                    </div>
            </div>
            </fieldset>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
