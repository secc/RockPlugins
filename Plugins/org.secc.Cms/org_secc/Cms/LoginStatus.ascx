<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LoginStatus.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.LoginStatus" %>

<style>
    /* General Notification Styles */
    .notification {
        display: inline-block;
        text-align: center;
        vertical-align: middle;
        white-space: nowrap;
        /* because only the first line is indented */
        line-height: 1;
    }
    /* Notification Specific Styles */
    a.dropdown-toggle {
        /* CSS animations work with IE11, Microsoft Edge 17+,
        Chrome 49+, Firefox 61+, Safari 11.1+, iOS Safari 10.3+,
        Chrome for Android 67+ and Samsung Internet 4+. */
        /* Pulsing Notification by User Profile Image */
    }

    @keyframes shadow-pulse {
        0% {
            box-shadow: 0 0 0 0px rgba(255, 255, 255, 0.4);
        }

        100% {
            box-shadow: 0 0 0 10px rgba(255, 255, 255, 0);
        }
    }

    a.dropdown-toggle .notification {
        animation: shadow-pulse 1s infinite;
        background-color: #fff !important;
        color: #fff;
        padding: 0px;
        text-indent: -9999px;
        /* sends the text off-screen */
        border-radius: 100%;
        height: 12px;
        /* be sure to set height & width */
        width: 12px;
        min-width: 10px;
        position: relative;
        top: -35px;
        right: -2px;
    }

    ul.dropdown-menu {
        /* Notification # in Navbar Dropdown */
    }

        ul.dropdown-menu .notification {
            background-color: #484848;
            color: white;
            padding: 5px;
            border-radius: 15px;
            height: 12px;
            min-width: 12px;
            font-weight: 600;
            font-size: 12px;
            line-height: 1;
            margin-left: 5px;
            box-sizing: content-box;
            margin-top: -5px;
        }

        ul.dropdown-menu a:hover .notification {
            background-color: white;
            color: black;
        }
</style>


<ul class="nav navbar-nav loginstatus">
    <li class="dropdown" id="liDropdown" runat="server">

        <a class="dropdown-toggle navbar-link" href="#" data-toggle="dropdown">
            <div id="divProfilePhoto" runat="server" class="profile-photo">
                <asp:Literal runat="server" ID="lNotifications" />
            </div>
            <asp:PlaceHolder ID="phHello" runat="server">
                <asp:Literal ID="lHello" runat="server" /></asp:PlaceHolder>

            <b class="fa fa-caret-down"></b>
        </a>

        <ul class="dropdown-menu">
            <asp:PlaceHolder ID="phMyAccount" runat="server">
                <li>
                    <asp:HyperLink ID="hlMyAccount" runat="server" Text="My Account" />
                </li>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="phMySettings" runat="server">
                <li>
                    <asp:HyperLink ID="hlMySettings" runat="server" Text="My Settings" />
                </li>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="phMyProfile" runat="server">
                <li>
                    <asp:HyperLink ID="hlMyProfile" runat="server" Text="My Profile" />
                </li>
            </asp:PlaceHolder>
            <asp:PlaceHolder ID="phMyDashboard" runat="server">
                <li>
                    <asp:HyperLink ID="hlMyDashboard" runat="server" Text="My Dashboard" />
                </li>
            </asp:PlaceHolder>
            <asp:Literal ID="lDropdownItems" runat="server" />
            <li class="divider"></li>
            <li>
                <asp:LinkButton ID="lbLoginLogout" runat="server" OnClick="lbLoginLogout_Click" CausesValidation="false"></asp:LinkButton></li>
        </ul>

    </li>
    <li id="liLogin" runat="server" visible="false">
        <asp:LinkButton ID="lbLogin" runat="server" OnClick="lbLoginLogout_Click" CausesValidation="false" Text="Login"></asp:LinkButton></li>
</ul>
<asp:HiddenField ID="hfActionType" runat="server" />

