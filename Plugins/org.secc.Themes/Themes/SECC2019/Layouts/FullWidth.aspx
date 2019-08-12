<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<Rock:Zone Name="Feature" runat="server" />

	<div class="container-fluid g-padding-x-0--xs">
		<div class="g-padding-x-20--xs g-padding-y-20--xs">
			<!-- Page Title -->
			<Rock:PageIcon ID="PageIcon" runat="server" /> <h1><Rock:PageTitle ID="PageTitle" runat="server" /></h1>
		</div>
		<!-- Breadcrumbs -->
		<Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />
	</div>

	<main class="container g-margin-t-0--xs">

        <!-- Start Content Area -->
		<br />

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

        <div class="row">
            <div class="col-md-12">
                <Rock:Zone Name="Main" runat="server" />
            </div>
        </div>

        <div class="row">
            <div class="col-md-12">
                <Rock:Zone Name="Section A" runat="server" />
            </div>
        </div>

        <div class="row">
            <div class="col-md-4">
                <Rock:Zone Name="Section B" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section C" runat="server" />
            </div>
            <div class="col-md-4">
                <Rock:Zone Name="Section D" runat="server" />
            </div>
        </div>
		<br />
        <!-- End Content Area -->

	</main>

</asp:Content>
