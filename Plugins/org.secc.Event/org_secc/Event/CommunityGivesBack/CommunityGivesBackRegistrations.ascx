<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunityGivesBackRegistrations.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CommunityGivesBack.CommunityGivesBackRegistrations" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbError" runat="server" Visible="false" NotificationBoxType="Danger">
            <h3>School Not Found</h3>
        </Rock:NotificationBox>
        <asp:Panel ID="pnlSchoolDetail" runat="server" CssClass="panel panel-block" Visible="false">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lSchoolName" runat="server" />
                </h1>
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlStatus" runat="server" />
                </div>
            </div>
            <div class="panel-body">
                <fieldset id="fsSchool" runat="server">
                    <div class="row">
                        <div class="col-sm-6">
                            <Rock:RockLiteral ID="lTeacherName" runat="server" Label="Resource Teacher" />
                        </div>
                        <div class="col-sm-6">
                            <Rock:RockLiteral ID="lTeacherEmail" runat="server" Label="Teacher Email" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-6">
                            <Rock:RockLiteral ID="lTotalSponsorships" runat="server" Label="Total Sponsorships" />
                        </div>
                        <div class="col-sm-6">
                            <Rock:RockLiteral ID="lSponsoredCount" runat="server" Label="Sponsored" />
                        </div>
                    </div>
                </fieldset>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlRegistrations" runat="server" CssClass="panel panel-block" Visible="false">
            <div class="panel-heading">
                <h1 class="panel-title">Sponsorships</h1>
            </div>
            <div class="panel-body">
                <Rock:Grid ID="gSponsorships" runat="server" EmptyDataText="No Sponsorships Found" RowItemText="Sponsorship" AllowSorting="true" DataKeyNames="Id">
                    <Columns>
                        <Rock:RockBoundField DataField="SponsorName" HeaderText="Sponsor Name" SortExpression="SponsorNameReversed" />
                        <Rock:RockBoundField DataField="Email" HeaderText="Email" />
                        <Rock:RockBoundField DataField="MobilePhone" HeaderText="Mobile Phone" />
                        <Rock:RockBoundField DataField="Sponsorships" HeaderText="Sponsorships" SortExpression="Sponsorships" />
                        <Rock:BoolField DataField="SponsorSiblingGroup" HeaderText="Sponsor Sibling Group" SortExpression="SponsorSiblingGroup" />
                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>


    </ContentTemplate>
</asp:UpdatePanel>
