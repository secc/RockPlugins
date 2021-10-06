<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseOutline.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseOutline" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:PlaceHolder ID="phCourseDetails" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>
