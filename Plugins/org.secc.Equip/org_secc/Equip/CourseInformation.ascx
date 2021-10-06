<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseInformation.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseInformation" %>

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
                            <Rock:RockLiteral runat="server" ID="ltDescription" Label="Description" />
                            <Rock:RockLiteral runat="server" ID="ltStatus" Label="Completion Data" />
                            <Rock:RockLiteral runat="server" ID="ltAllowed" Visible="false" />
                            <Rock:RockLiteral runat="server" ID="ltRequirements" Label="Segments Required To Complete This Course" />
                            <Rock:RockLiteral runat="server" ID="ltExternalUrl" Visible="false" Label="External Course Url" />
                        </div>
                        <div class="col-md-4">
                            <asp:Literal ID="ltImage" runat="server" />
                        </div>
                    </div>
                    <Rock:BootstrapButton runat="server" ID="btnEdit" Text="Manage Course" CssClass="btn btn-primary" OnClick="btnEdit_Click" />
                    <h3>Course Participant Information</h3>
                    <Rock:Grid runat="server" ID="gReport">
                        <Columns>
                            <Rock:PersonField HeaderText="Person" DataField="Person" />
                            <Rock:DateField HeaderText="Times Completed" DataField="TimesCompleted" />
                            <Rock:DateField HeaderText="Completion Date" DataField="CompletedDateTime" />
                        </Columns>
                    </Rock:Grid>
                </asp:Panel>

                <asp:Panel ID="pnlEdit" runat="server">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Course Name" ID="tbName" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:CategoryPicker runat="server" Label="Category" ID="pCategory" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Description" ID="tbDescription" TextMode="MultiLine" Height="200" />
                        </div>
                        <div class="col-md-6">
                            <Rock:ImageUploader runat="server" ID="uImage" Label="Image" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" Label="Slug" Help="Handy way of making a nicer url." Required="true"
                                ID="tbSlug" AutoPostBack="true" OnTextChanged="tbSlug_TextChanged" />
                            <Rock:NotificationBox runat="server" ID="nbSlug" NotificationBoxType="Validation" Visible="false"
                                Text="This slug is taken by a different course. Please try a different slug." />
                        </div>
                        <div class="col-md-6">
                            <Rock:UrlLinkBox runat="server" Label="External Course Url (Optional)" ID="tbExternalUrl"
                                Help="If you are using an external source for your lesson, enter the course's url otherwise leave blank.
                        You can still manage requirement and reporting through this course." />
                        </div>
                        <div class="col-md-12">
                            <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                            <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CssClass="btn" CausesValidation="false" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
