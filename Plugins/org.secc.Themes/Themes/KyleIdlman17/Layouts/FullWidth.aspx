<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<style>
	.nopadding {
	   padding: 0 !important;
	   margin: 0 !important;
	}
	</style>

	<main>

        <!-- Start Content Area -->

        <!-- Page Title -->
        <Rock:PageIcon ID="PageIcon" runat="server" /> <h1 class="pagetitle"><Rock:PageTitle ID="PageTitle" runat="server" /></h1>

        <!-- Breadcrumbs -->
        <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error no-index" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

        <Rock:Zone Name="Feature" runat="server" />

        <Rock:Zone Name="Main" runat="server" />

		<Rock:Zone Name="Footer" runat="server" />

        <!-- End Content Area -->

	</main>

</asp:Content>
