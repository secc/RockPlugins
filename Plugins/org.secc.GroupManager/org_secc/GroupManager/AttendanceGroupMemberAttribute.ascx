<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceGroupMemberAttribute.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.AttendanceGroupMemberAttribute" %>

<asp:UpdatePanel ID="upnlcontent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnldetails" CssClass="js-group-panel" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading panel-follow clearfix">
                    <h1 class="panel-title pull-left">
                        <asp:Literal ID="lgroupiconhtml" runat="server" />
                        <asp:Literal ID="lreadonlytitle" runat="server" />
                    </h1>
                </div>
                <div class="panel-body">
                    <Rock:RockCheckBoxList ID="cbl" runat="server" Label="Partitions" />
                    <Rock:DateRangePicker ID="dp" runat="server" Label="Date Range"/>
                    <asp:LinkButton ID="BtnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary" OnClick="BtnSubmit_Click" />
                    <Rock:Grid ID="glist" runat="server" AllowSorting="true">
                        <Columns>
                            <Rock:RockBoundField DataField="MemberAttribute" HeaderText="Group Member Attribue" SortExpression="MemberAttribue" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>