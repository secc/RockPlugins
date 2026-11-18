<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rock.Web.UI.DialogPage" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="Rock" %>

<!DOCTYPE html>

<script runat="server">
    
    /// <summary>
    /// An optional subtitle
    /// </summary>
    /// <value>
    /// The sub title.
    /// </value>
    public override string SubTitle
    {
        get
        {
            return lSubTitle.Text.TrimStart( "<small>".ToCharArray() ).TrimEnd( "</small>".ToCharArray() );
        }
        set
        {
            lSubTitle.Text = string.IsNullOrWhiteSpace( value ) ? "" : "<small>" + value + "</small>";
        }
    }

    /// <summary>
    /// Gets or sets the close message.
    /// </summary>
    /// <value>
    /// The close message.
    /// </value>    
    public override string CloseMessage
    {
        get
        {
            return hfCloseMessage.Value;
        }
        set
        {
            hfCloseMessage.Value = value;
        }
    }

    /// <summary>
    /// Handles the Click event of the btnSave control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void btnSave_Click( object sender, EventArgs e )
    {
        base.FireSave( sender, e );
    }

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnInit( EventArgs e )
    {
        base.OnInit( e );

        lTitle.Text = Request.QueryString["t"] ?? "Title";

        btnSave.Text = Request.QueryString["pb"] ?? "Save";
        btnSave.Visible = btnSave.Text.Trim() != string.Empty;

        btnCancel.Text = Request.QueryString["sb"] ?? "Cancel";
        btnCancel.Visible = btnCancel.Text.Trim() != string.Empty;
        if ( !btnSave.Visible )
        {
            btnCancel.AddCssClass( "btn-primary" );
        }
    }    
    
</script>

<html class="no-js">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=10" />
    <title></title>


    <script src="<%# ResolveRockUrl("~/Scripts/jquery-3.3.1.min.js" ) %>"></script>
    <script src="<%# ResolveRockUrl("~/Scripts/jquery-migrate-3.0.1.min.js" ) %>"></script>

    <!-- Set the viewport width to device width for mobile -->
		<meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=2.0">

    <!-- SECC2019 Theme CSS Files -->
    <link rel="stylesheet" href="<%# ResolveRockUrl("~/Themes/SECC2019/Styles/layout.css", true) %>"/>
    <link rel="stylesheet" href="<%# ResolveRockUrl("~/Themes/SECC2019/Styles/main.css", true) %>"/>

    <!-- SECC2019Portal Theme CSS Files -->
    <link rel="stylesheet" href="<%# ResolveRockUrl("~~/Styles/seportal.css", true) %>"/>
    <link rel="stylesheet" href="<%# ResolveRockUrl("~/Styles/developer.css", true) %>"/>
    <asp:ContentPlaceHolder ID="css" runat="server" />

    <!-- Included JS Files -->
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/modernizr-custom.js" ) %>" ></script>
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/global-dist.js" ) %>" ></script>
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/isotope.pkgd.min.js" ) %>" ></script>
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/imagesloaded.pkgd.min.js" ) %>"></script>
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/smooth-scroll.min.js" ) %>" ></script>
    <script src="<%# ResolveRockUrl("~/Themes/SECC2019/Scripts/select2/select2.full.min.js" ) %>" ></script>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/jarallax/1.10.7/jarallax.js" ></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/object-fit-images/3.2.3/ofi.min.js" ></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jarallax/1.10.7/jarallax-video.js" ></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/vivus/0.4.4/vivus.min.js" ></script>
    <link href="/Themes/SECC2019/Assets/vendor/photo_swipe/photoswipe.css?v=4.1.2-1.0.4" />
    <link href="/Themes/SECC2019/Assets/vendor/photo_swipe/default-skin/default-skin.css?v=4.1.2-1.0.4" />
    <script src="/Themes/SECC2019/Assets/vendor/photo_swipe/photoswipe.js"></script>
    <script src="/Themes/SECC2019/Assets/vendor/photo_swipe/photoswipe-ui-default.js"></script>
	
    <style>
        html, body {
            height: auto;
            width: 100%;
            min-width: 100%;
            margin: 0 0 0 0;
            padding: 0 0 0 0;
            vertical-align: top;
        }
    </style>

</head>

<body id="dialog" class="rock-modal">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="sManager" runat="server" />
        <asp:UpdatePanel ID="updatePanelDialog" runat="server">
            <ContentTemplate>
                <div class="modal-content">
                    <Rock:HiddenFieldWithClass ID="hfCloseMessage" runat="server" CssClass="modal-close-message" />
                    <div class="modal-header">
                        <a id="closeLink" href="#" class="close" onclick="window.parent.Rock.controls.modal.close($(this).closest('.modal-content').find('.modal-close-message').first().val());">&times;</a>
                        <h3 class="modal-title">
                            <asp:Literal ID="lTitle" runat="server"></asp:Literal></h3>
                        <asp:Literal ID="lSubTitle" runat="server"></asp:Literal>
                    </div>

                    <div class="modal-body">

                        <!-- Ajax Error -->
                        <div class="alert alert-danger ajax-error" style="display:none">
                            <p><strong>Error</strong></p>
                            <span class="ajax-error-message"></span>
                        </div>

                        <Rock:Zone Name="Main" runat="server" />

                    </div>

                    <div class="modal-footer">
                        <asp:LinkButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-link" OnClientClick="window.parent.Rock.controls.modal.close($(this).closest('.modal-content').find('.modal-close-message').first().val());" CausesValidation="false" />
                        <asp:LinkButton ID="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click " />
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </form>
</body>


</html>
<script>
    Sys.Application.add_load(function () {
        Rock.controls.modal.updateSize();
    });
</script>
