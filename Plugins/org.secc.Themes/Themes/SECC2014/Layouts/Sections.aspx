<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>
<%@ Register TagPrefix="SECC" Namespace="org.secc.Cms" Assembly="org.secc.Cms" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<Rock:Zone Name="Feature" runat="server" />

    <!-- Start Content Area -->

	<!-- Page Title -->
    <Rock:PageIcon ID="PageIcon" runat="server" /> <h1><Rock:PageTitle ID="PageTitle" runat="server" /></h1>

    <!-- Breadcrumbs -->
    <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

    <!-- Ajax Error -->
    <div class="alert alert-danger ajax-error" style="display:none">
        <p><strong>Error</strong></p>
        <span class="ajax-error-message"></span>
    </div>

    <Rock:Zone Name="Main" runat="server" />

	<SECC:SectionZone Name="Section 1" runat="server" />
    <SECC:SectionZone  Name="Section 2" runat="server" />
	<Rock:Zone Name="Section 3" runat="server" />
    <Rock:Zone Name="Section 4" runat="server" />
	<Rock:Zone Name="Section 5" runat="server" />
	<Rock:Zone Name="Section 7" runat="server" />
    <Rock:Zone Name="Section 8" runat="server" />
    <Rock:Zone Name="Section 9" runat="server" />
    <Rock:Zone Name="Section 10" runat="server" />

    <!-- End Content Area -->

</asp:Content>
