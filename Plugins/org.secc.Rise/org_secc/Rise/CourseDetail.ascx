<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.CourseDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:ModalDialog runat="server" ID="mdEnrollGroup" Title="Enroll Group"
            OnSaveClick="mdEnrollGroup_SaveClick" SaveButtonText="Enroll">
            <Content>
                <Rock:NotificationBox runat="server" NotificationBoxType="Warning" Title="Warning"
                    Text="After enrolling a group in a course, all group members will be send an email from Rise notifying them of the new enrollment." />
                <Rock:GroupPicker runat="server" ID="pGroup" Label="Group" />
            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-chalkboard"></i>
                    <asp:Literal ID="ltName" runat="server" />
                </h1>
                <span class="label label-type" title="">
                    <asp:Literal ID="lIsArchived" runat="server" /></span>
            </div>
            <div class="panel-body">
                <Rock:RockLiteral runat="server" ID="ltUrl" Label="Url" />
                <div class="row">
                    <div class="col-sm-4">
                        <Rock:CategoryPicker runat="server" ID="pCategories" Label="Categories"
                            AllowMultiSelect="true" EntityTypeName="org.secc.Rise.Model.Course" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockCheckBox runat="server" ID="cbLibrary" Label="Avaliable To All" />
                    </div>
                </div>

                <asp:PlaceHolder runat="server" ID="phAttributes" />

                <Rock:BootstrapButton runat="server" ID="btnSave" CssClass="btn btn-primary" Text="Save" OnClick="btnSave_Click" />
                <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CausesValidation="false" CssClass="btn btn-link" />
        </asp:Panel>
        </div>
        <asp:Panel runat="server" ID="pnlGroups">
            <h3>Enrolled Groups</h3>
            <Rock:Grid runat="server" ID="gGroups" DataKeyNames="Id">
                <Columns>
                    <Rock:RockBoundField HeaderText="Group" DataField="Name" />
                    <Rock:DeleteField ID="btnGroupDelete" OnClick="btnGroupDelete_Click" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>


    </ContentTemplate>
</asp:UpdatePanel>
