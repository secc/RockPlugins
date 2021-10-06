<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseDetail.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="ltTitle" runat="server" />
                </h1>
                <div class="panel-labels">
                    <asp:Literal ID="ltLabels" runat="server" />
                </div>
            </div>

            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <asp:Panel runat="server" ID="pnlView">
                    <div class="row">
                        <div class="col-md-8">
                            <Rock:RockLiteral runat="server" ID="ltName" Label="Name" />
                            <Rock:RockLiteral runat="server" ID="ltDescription" Label="Description" />
                            <Rock:RockLiteral runat="server" ID="ltSlug" Label="Slug" />
                            <Rock:RockLiteral runat="server" ID="ltAllowed" Visible="false" />
                            <Rock:RockLiteral runat="server" ID="ltExternalUrl" Label="External Course Url" Visible="false" />
                        </div>
                        <div class="col-md-4">
                            <asp:Literal ID="ltImage" runat="server" />
                        </div>
                    </div>
                    <br />
                    <Rock:BootstrapButton runat="server" ID="btnEdit" Text="Edit" CssClass="btn btn-primary" OnClick="btnEdit_Click" />
                    <asp:LinkButton runat="server" ID="btnDelete" Text="Delete" CssClass="btn" OnClick="btnDelete_Click" />
                    <Rock:SecurityButton ID="btnSecurity" runat="server" class="btn btn-sm btn-security pull-right" />
                </asp:Panel>

                <asp:Panel ID="pnlEdit" runat="server">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Course Name" ID="tbName" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <br />
                            <Rock:RockCheckBox runat="server" ID="cbIsActive" Text="Active" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Description" ID="tbDescription" TextMode="MultiLine" Height="200" />
                        </div>
                        <div class="col-md-6">
                            <Rock:ImageUploader runat="server" ID="uImage" Label="Image" />
                            <Rock:CategoryPicker runat="server" Label="Category" ID="pCategory" Required="true" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Icon Css Class" ID="tbIconCssClass" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Slug" Help="Handy way of making a nicer url." Required="true"
                                ID="tbSlug" AutoPostBack="true" OnTextChanged="tbSlug_TextChanged" />
                            <Rock:NotificationBox runat="server" ID="nbSlug" NotificationBoxType="Validation" Visible="false"
                                Text="This slug is taken by a different course. Please try a different slug." />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockDropDownList runat="server" ID="ddlViewSecurity" Label="Who Is Allowed To Take This Course" AutoPostBack="true" OnSelectedIndexChanged="ddlViewSecurity_SelectedIndexChanged">
                                <asp:ListItem Text="Anyone" Value="Anyone" />
                                <asp:ListItem Text="Limit By Group" Value="Group" />
                                <asp:ListItem Text="Limit By Data View" Value="DataView" />
                            </Rock:RockDropDownList>
                            <Rock:GroupPicker runat="server" ID="pAllowedGroup" Label="Allowed Group" Visible="false" />
                            <Rock:DataViewItemPicker runat="server" ID="pAllowedDataView" Label="Allowed Data View" Visible="false" />
                        </div>
                        <div class="col-md-6">
                            <Rock:UrlLinkBox runat="server" Label="External Course Url (Optional)" ID="tbExternalUrl"
                                Help="If you are using an external source for your lesson, enter the course's url otherwise leave blank.
                        You can still manage requirement and reporting through this course." />
                        </div>
                    </div>

                    <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CssClass="btn" CausesValidation="false" />
                </asp:Panel>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
