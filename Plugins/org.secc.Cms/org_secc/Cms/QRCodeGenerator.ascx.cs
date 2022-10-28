// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace RockWeb.Plugins.org_secc.Cms
{

    [DisplayName( "QR Code Generator" )]
    [Category( "SECC > CMS" )]
    [Description( "Creates a shortlink from a provided URL and returns a QR code" )]

    #region Block Attributes

    [SiteField(
        "Default Site",
        Key = AttributeKey.DefaultSite,
        Description = "The default site for shortlinks",
        IsRequired = true,
        Order = 0 )]
    [IntegerField(
        "Minimum Token Length",
        Key = AttributeKey.MinimumTokenLength,
        Description = "The minimum number of characters for the token.",
        IsRequired = false,
        DefaultIntegerValue = 7,
        Order = 1 )]
    [FileField(
        "Logo file",
        Key = AttributeKey.Logo,
        Description = "Logo to brand QR code",
        Order = 2 )]
    [TextField(
        "Root URL",
        Key = AttributeKey.RootURL,
        Description = "The URL to use as the base of the shortlink (if blank, this will be the URL of the Default Site)",
        IsRequired = false,
        Order = 3 )]

    #endregion Block Attributes

    public partial class QRCodeGenerator : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string DefaultSite = "DefaultSite";
            public const string MinimumTokenLength = "MinimumTokenLength";
            public const string Logo = "Logo";
            public const string RootURL = "RootUrl";


        }

        #endregion Attribute Keys

        #region Base Control Methods



        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );


        }

        #endregion

        protected void btnCreate_Click( object sender, EventArgs e )
        {
            var url = ulbUrl.Text.Trim();
            if ( !url.IsValidUrl() )
            {
                nbError.Text = "Please enter a valid URL.";
                nbError.Visible = true;
            }
            else
            {
                nbError.Visible = false;
                PageShortLink link = null;
                var _minTokenLength = GetAttributeValue( "MinimumTokenLength" ).AsIntegerOrNull() ?? 7;
                var _defaultSite = GetAttributeValue( "DefaultSite" ).AsIntegerOrNull();
                var _logo = GetAttributeValue( "Logo" ).AsGuidOrNull();
                var _rootUrl = GetAttributeValue( "RootUrl" );

                int? siteId = _defaultSite.Value;

                RockContext rockContext = new RockContext();
                var pageShortLinkService = new PageShortLinkService( rockContext );
                var token = pageShortLinkService.GetUniqueToken( ( int ) siteId, _minTokenLength );

                var errors = new List<string>();


                if ( !siteId.HasValue )
                {
                    errors.Add( "Please select a valid site." );
                }

                if ( token.IsNullOrWhiteSpace() || token.Length < _minTokenLength )
                {
                    errors.Add( string.Format( "Please enter a token that is a least {0} characters long.", _minTokenLength ) );
                }
                else if ( siteId.HasValue && !pageShortLinkService.VerifyUniqueToken( siteId.Value, 0, token ) )
                {
                    errors.Add( "The selected token is already being used. Please enter a different token." );
                }

                if ( url.IsNullOrWhiteSpace() )
                {
                    errors.Add( "Please enter a valid URL." );
                }

                if ( errors.Any() )
                {
                    nbError.Text = "Please correct the following:<ul><li>" + errors.AsDelimited( "</li><li>" ) + "</li></ul>";
                    nbError.Visible = true;
                    return;
                }

                // Figure out how to create unique link per person?
                //var currentUser = UserLoginService.GetCurrentUser();
                //int? currentPersonId = currentUser != null ? currentUser.PersonId : null;

                var existingLink = pageShortLinkService.Queryable().Where( l => l.Url == url ).FirstOrDefault();
                if ( existingLink != null )
                {
                    link = new PageShortLink
                    {
                        Url = existingLink.Url,
                        Token = existingLink.Token,
                        SiteId = existingLink.SiteId
                    };
                }

                else
                {
                    link = new PageShortLink
                    {
                        SiteId = siteId.Value,
                        Token = token,
                        Url = url
                    };
                    pageShortLinkService.Add( link );
                    rockContext.SaveChanges();
                };

                var siteDomainService = new SiteDomainService( rockContext );
                var siteUrl = siteDomainService.GetBySiteId( link.SiteId ).FirstOrDefault();

                var qrCodeUrl = "/GetQRCode.ashx?data=";
                if ( _rootUrl != null )
                {
                    qrCodeUrl += $"{_rootUrl}/{link.Token}&outputType=svg";
                }
                else
                {
                    qrCodeUrl += $"http://{siteUrl}/{link.Token}&outputType=svg";
                }

                String logoUrl = "";
                if ( _logo != null )
                {
                    logoUrl = $"/GetImage.ashx?guid={_logo}";
                }


                String csname1 = "qrCodeImageScript" + RockDateTime.Now.Ticks;
                Type cstype = this.GetType();
                var script = string.Format( @"
var canvas = document.getElementById('qrCodeCanvas');
var context = canvas.getContext('2d');
var qrCodeDiv = document.getElementById('QrCodeDiv');

var img1 = new Image();
var img2 = new Image();

img1.onload = function() {{
    canvas.width = qrCodeDiv.offsetWidth;
    canvas.height = qrCodeDiv.offsetWidth;
    img2.src = '{0}';
}};
img2.onload = function() {{
    context.globalAlpha = 1.0;
    context.drawImage(img1, 0, 0, canvas.width, canvas.height);
    context.drawImage(img2,
        ( canvas.width / 2 ) - ( ( canvas.width / 5) / 2 ),
        ( canvas.height / 2 ) - ( ( canvas.height / 5 ) / 2 ),
        ( canvas.width / 5 ),
        ( canvas.height / 5 ));

    var aTag = document.createElement('a');
    aTag.download = ""QRCode.png"";
    aTag.href = canvas.toDataURL(""image/png"");
    aTag.innerHTML = ""Download"";
    aTag.className += ""btn btn-primary"";
    var qrCodeDiv = document.getElementById('QrCodeDiv');
    qrCodeDiv.appendChild(aTag);
}};       

img1.src = '{1}';", logoUrl, qrCodeUrl );
                ScriptManager.RegisterStartupScript( upnlQRCodeGenerator, typeof( UpdatePanel ), csname1, script, true );

            }

        }

    }
}
