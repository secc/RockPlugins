<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<main class="container-fluid">

		<div class="row">
            <div class="col-xs-12" style="padding: 0px;">
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

	        <Rock:Zone Name="Feature" runat="server" />
	        <Rock:Zone Name="Main" runat="server" />

	        <!-- End Content Area -->
			</div>
		</div>

	</main>

</asp:Content>
