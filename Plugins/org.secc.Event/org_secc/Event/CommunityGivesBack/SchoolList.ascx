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

                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>