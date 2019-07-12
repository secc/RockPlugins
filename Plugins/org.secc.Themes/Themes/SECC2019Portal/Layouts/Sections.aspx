<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>
<script runat="server">

    // keep code below to call base class init method

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnPreRender( EventArgs e )
    {
        base.OnPreRender( e );

        var rockPage = this.Page as Rock.Web.UI.RockPage;
        if (rockPage != null)
        {
            var pageCache = Rock.Web.Cache.PageCache.Get( rockPage.PageId );
            if (pageCache != null )
            {
                if (pageCache.PageDisplayTitle == false || string.IsNullOrWhiteSpace( rockPage.PageTitle ) )
                {
                    secPageTitle.Visible = false;
                }
            }
        }
    }

</script>

<%@ Register TagPrefix="SECC" Namespace="org.secc.Cms" Assembly="org.secc.Cms" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<Rock:Zone Name="Feature" runat="server" />

  <!-- Start Content Area -->
  <section id="secPageTitle" class="page-title g-bg-color--gray-lightest" runat="server">
    <div class="container">
    	<div class="row g-padding-y-50--xl">
        <div class="col-xs-12">
          <h1 class="g-font-size--jumbo-1 g-font-family--secondary"><Rock:PageIcon ID="PageIcon" runat="server" /><Rock:PageTitle ID="PageTitle" runat="server" /></h1>
        </div>
    	</div>
		</div>
  </section>

    <!-- Breadcrumbs -->
    <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

    <!-- Ajax Error -->
    <div class="alert alert-danger ajax-error" style="display:none">
        <p><strong>Error</strong></p>
        <span class="ajax-error-message"></span>
    </div>

    <Rock:Zone Name="Main" runat="server" />
	  <SECC:SectionZone Name="Section Intro" runat="server" />
	  <SECC:SectionZone Name="Section Serve" runat="server" />
	  <SECC:SectionZone Name="Section 1" runat="server" />
      <SECC:SectionZone  Name="Section 2" runat="server" />
	  <SECC:SectionZone Name="Section 3" runat="server" />
	<Rock:Zone Name="Section 4" runat="server" />
	<Rock:Zone Name="Section 5" runat="server" />
	<Rock:Zone Name="Section 7" runat="server" />
    <Rock:Zone Name="Section 8" runat="server" />
    <Rock:Zone Name="Section 9" runat="server" />
    <Rock:Zone Name="Section 10" runat="server" />

  <!-- End Content Area -->

</asp:Content>
