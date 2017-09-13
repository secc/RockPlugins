<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PINManager.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Security.PINManager" %>

<asp:UpdatePanel ID="upnlPIN" runat="server" class="context-attribute-values">
    <ContentTemplate>

        <Rock:ModalDialog runat="server" ID="mdEditPin" Title="Manage PIN" OnSaveClick="mdEditPin_SaveClick" CancelLinkVisible="true">
            <Content>
                <asp:HiddenField runat="server" ID="hfPinID" />
                <asp:HiddenField runat="server" ID="hfConfirmDelete" />
                <div class="row">
                    <div class="col-sm-4">
                        <Rock:NotificationBox runat="server" ID="nbError" Dismissable="true" Visible="false" NotificationBoxType="Danger" />
                        <div class="alert alert-info">
                            <h4>PIN Requirements</h4>
                            <ul>
                                <li>Must contain <asp:Literal ID="ltMinimum" runat="server" /> or more numbers</li>
                                <li>Must only contain numbers</li>
                            </ul>
                        </div>
                        <Rock:BootstrapButton runat="server" CssClass="btn btn-block btn-danger" Text="Remove PIN"
                            ID="btnDelete" Visible="false" OnClick="btnDelete_Click"></Rock:BootstrapButton>
                    </div>
                    <div class="col-sm-8">
                        <Rock:RockTextBox runat="server" AutoCompleteType="None" ID="tbPin" Label="PIN"></Rock:RockTextBox>
                        <Rock:RockCheckBoxList runat="server" ID="cblPurpose" Label="Purpose"></Rock:RockCheckBoxList>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <section class="panel panel-persondetails">
            <div class="panel-heading rollover-container clearfix">
                <h3 class="panel-title pull-left">
                    <i class="fa fa-th"></i>&nbsp;PIN Manager
                </h3>
                    <div class="actions rollover-item pull-right">
                        <asp:LinkButton Visible="false" Text="<i class='fa fa-plus-square'></i>" CssClass="edit" runat="server" ID="lbAdd" OnClick="lbAdd_Click" />
                    </div>
            </div>
            <div class="panel-body">

                <asp:PlaceHolder runat="server" ID="phPin" />

            </div>
        </section>

    </ContentTemplate>
</asp:UpdatePanel>

