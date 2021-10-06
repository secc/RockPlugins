<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseList.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:Grid ID="gList" runat="server" AllowSorting="true" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
            <Columns>
                <Rock:RockBoundField DataField="Name" HeaderText="Name" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
