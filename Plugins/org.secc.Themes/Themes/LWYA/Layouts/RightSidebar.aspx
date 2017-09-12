<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

    <Rock:Zone Name="PageInfo" runat="server" />

    <section class="container page_content_wrap">


        <!-- Start Content Area -->

        <!-- Page Title -->
        <Rock:PageIcon ID="PageIcon" runat="server" /> <h1><Rock:PageTitle ID="PageTitle" runat="server" /></h1>
        <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />

        <!-- Ajax Error -->
        <div class="alert alert-danger ajax-error" style="display:none">
            <p><strong>Error</strong></p>
            <span class="ajax-error-message"></span>
        </div>

        <div class="row">
            <div class="col-md-12">
                <Rock:Zone Name="Feature" runat="server" />
            </div>
        </div>

        <div class="row">
            <div class="col-md-8 col-lg-9">
                <Rock:Zone Name="Main" runat="server" />
            </div>
            <div class="col-md-4 col-lg-3 g-margin-b-60--xs">

                <Rock:Zone Name="Sidebar 1" runat="server" />

                <!--
                 *******************************************
                 **** Developer Note: This is a Widget Made for Tags (Add this when Tags is built for blogging within ROCK)
                 **** Developer: Steven Schulte
                 **** Date: 07/10/2017
                 ********************************************
                <div class="aside widget widget_tag_cloud">
                    <h6 class="widget_title">Tags</h6>
                    <div class="tagcloud">
                        <a href="#" class="tag-cloud-link" aria-label="change (5 items)">change</a>
                        <a href="#" class="tag-cloud-link" aria-label="chirstian (1 item)">chirstian</a>
                        <a href="#" class="tag-cloud-link" aria-label="christian (1 item)">christian</a>
                        <a href="#" class="tag-cloud-link" aria-label="church (6 items)">church</a>
                        <a href="#" class="tag-cloud-link" aria-label="family (5 items)">family</a>
                        <a href="#" class="tag-cloud-link" aria-label="god (7 items)">god</a>
                        <a href="#" class="tag-cloud-link" aria-label="holy (6 items)">holy</a>
                        <a href="#" class="tag-cloud-link" aria-label="lecture (2 items)">lecture</a>
                        <a href="#" class="tag-cloud-link" aria-label="marriage (1 item)">marriage</a>
                        <a href="#" class="tag-cloud-link" aria-label="post format (10 items)">post format</a>
                        <a href="#" class="tag-cloud-link" aria-label="religion (6 items)">religion</a>
                        <a href="#" class="tag-cloud-link" aria-label="salvation (5 items)">salvation</a>
                        <a href="#" class="tag-cloud-link" aria-label="seminar (2 items)">seminar</a>
                    </div>
                </div> -->
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

        <!-- End Content Area -->

    </section>

</asp:Content>
