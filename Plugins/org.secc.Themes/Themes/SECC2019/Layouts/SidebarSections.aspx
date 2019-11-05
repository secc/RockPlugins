<%@ Page Language="C#" MasterPageFile="Site.Master" AutoEventWireup="true" Inherits="Rock.Web.UI.RockPage" %>

<asp:Content ID="ctMain" ContentPlaceHolderID="main" runat="server">

	<Rock:Zone Name="Feature" runat="server" />

    <style>
    #wrapper {
        padding-left: 0;
        -webkit-transition: all 0.5s ease;
        -moz-transition: all 0.5s ease;
        -o-transition: all 0.5s ease;
        transition: all 0.5s ease;
        position: relative;
    }

    #sidebar-wrapper {
        z-index: 1000;
        position: absolute;
        left: 250px;
        width: 0;
        height: 100%;
        margin-left: -250px;
        overflow-y: auto;
        background: #ffffff;
        -webkit-transition: all 0.5s ease;
        -moz-transition: all 0.5s ease;
        -o-transition: all 0.5s ease;
        transition: all 0.5s ease;
    }


    #page-content-wrapper {
        position: relative;
        background-color: #F5F5F5;
        padding: 20px;
    }

    /* Sidebar Styles */
    .sidebar-nav {
        position: absolute;
        top: 0;
        width: 250px;
        margin: 0;
        padding: 0;
        list-style: none;
        padding-top: 20px;
    }

    .sidebar-nav li {
        text-indent: 20px;
        line-height: 40px;
        margin: 20px 0px;
        border-left: 0px solid;
        -webkit-transition: border .1s ease-out;
        -moz-transition: border .1s ease-out;
        -o-transition: border .1s ease-out;
        transition: border .1s ease-out;
    }

	.sidebar-nav li.active {
		border-left: 10px solid #ccc;
	}

    .sidebar-nav li:hover {
        border-left: 10px solid #ccc;
    }

    .sidebar-nav li i {
        vertical-align: middle;
        padding-right: 10px;
    }

    .sidebar-nav li a {
        display: block;
        text-decoration: none;
        color: #000;
    }

    .sidebar-nav li a:hover {
        text-decoration: none;
        color: rgba(0,0,0,0.5);
        background: rgba(255,255,255,0.2);
    }

    .sidebar-nav li a:active,
    .sidebar-nav li a:focus {
        text-decoration: none;
    }

    .sidebar-nav > .sidebar-brand {
        height: 65px;
        font-size: 18px;
        line-height: 60px;
    }

    .sidebar-nav > .sidebar-brand a {
        color: #999999;
    }

    .sidebar-nav > .sidebar-brand a:hover {
        color: #fff;
        background: none;
    }

    @media(min-width:768px) {
        #wrapper {
            padding-left: 250px;
        }

        #sidebar-wrapper {
            width: 250px;
        }

        #page-content-wrapper {
            padding: 4rem;
            position: relative;
        }
    }
    </style>

    <div id="wrapper">

        <!-- Sidebar -->
        <div id="sidebar-wrapper">
            <ul class="sidebar-nav">
                <li>
                    <a href="#"><i class="fas fa-th-large fa-2x"></i> Dashboard</a>
                </li>
                <li>
                    <a href="#"><i class="fal fa-heart fa-2x"></i> Giving</a>
                </li>
                <li>
                    <a href="#"><i class="fal fa-users fa-2x"></i> Groups & Events</a>
                </li>
                <li class="active">
                    <a href="#"><i class="fal fa-user-circle fa-2x"></i> Account</a>
                </li>
            </ul>
        </div>
        <!-- /#sidebar-wrapper -->

        <div id="page-content-wrapper">
            <main class="container-fluid g-padding-x-0--xs">
                <!-- Start Content Area -->
                <!-- Page Title -->
                <Rock:PageIcon ID="PageIcon" runat="server" />
                <h1 class="g-font-family--secondary g-margin-b-60--xs g-padding-x-20--xs"><Rock:PageTitle ID="PageTitle" runat="server" /></h1>
                <Rock:PageBreadCrumbs ID="PageBreadCrumbs" runat="server" />
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
                <!-- End Content Area -->
            </main>
        </div>

    </div>

</asp:Content>
