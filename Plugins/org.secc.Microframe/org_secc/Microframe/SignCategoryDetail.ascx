<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignCategoryDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Microframe.SignCategoryDetail" %>

<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server" Visible="false">

            <asp:HiddenField ID="hfSignCategoryId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-server"></i>
                    <asp:Literal ID="lActionTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">

                <asp:ValidationSummary ID="ValidationSummary1" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <fieldset>

                    <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                    <Rock:NotificationBox ID="nbDuplicateSign" runat="server" NotificationBoxType="Warning" Title="Sorry" Visible="false" />

                    <Rock:DataTextBox ID="tbName" runat="server" SourceTypeName="org.secc.Microframe.Model.SignCategory, org.secc.Microframe" PropertyName="Name" Required="true" />


                    <div class="row">
                        <div class="col-md-12">
                            <Rock:DataTextBox CausesValidation="false" ID="tbDescription" runat="server" SourceTypeName="org.secc.Microframe.Model.SignCategory, org.secc.Microframe" PropertyName="Description" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <div class="actions">
                        <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                    </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
