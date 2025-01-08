<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SchoolList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CommunityGivesBack.SchoolList" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlSchoolList" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">School List</h1>
            </div>
            <div class="panel-body">
                <Rock:Grid ID="gSchoolList" runat="server" AllowSorting="true" RowItemText="Schools" DataKeyNames="Id">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                        <Rock:RockBoundField DataField="TeacherName" HeaderText="Teacher" SortExpression="Teacher" />
                        <Rock:RockBoundField DataField="TotalSponsorships" HeaderText="Total Sponsorships" SortExpression="TotalSponsorships" />
                        <Rock:RockBoundField DataField="Sponsored" HeaderText="Sponsored" SortExpression="Sponsored" />
                        <Rock:RockBoundField DataField="AvailableSponsorships" HeaderText="Remaining" SortExpression="AvailableSponsorships" />
                        <Rock:EditField OnClick="gSchoolListItem_Edit" />
                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>
        <Rock:ModalDialog id="mdlSchoolEdit" runat="server" Title="Add/Update School" ValidationGroup="valSchool" SaveButtonText="Save" >
            <Content>
                <asp:ValidationSummary ID="valSchool" runat="server" CssClass="alert alert-validation" HeaderText="Please update the following:" />
                <Rock:RockTextBox ID="tbSchoolName" runat="server" Label="School" ValidationGroup="valSchool" Required="true" />
                <Rock:RockTextBox ID="tbTeacherName" runat="server" Label="Teacher Name" ValidationGroup="valSchool" Required="true" />
                <Rock:EmailBox ID="tbTeacherEmail" runat="server" Label="Teacher Email" ValidationGroup="valSchool" Required="true" />
                <Rock:NumberBox ID="tbSponsorships" runat="server" Label="Students to Sponsor" ValidationGroup="valSchool" Required="true" />
                <Rock:RockCheckBox ID="cbActive" runat="server" Label="Active" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>