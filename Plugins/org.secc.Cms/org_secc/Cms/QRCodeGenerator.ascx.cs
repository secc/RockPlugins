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

    #endregion Block Attributes

    public partial class QRCodeGenerator : Rock.Web.UI.RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string DefaultSite = "DefaultSite";
            public const string MinimumTokenLength = "MinimumTokenLength";
            public const string Logo = "logo";

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
            var url = ulbUrl.Text;
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
                QrCode.ImageUrl = $"/GetQRCode.ashx?data=http://{siteUrl}/{link.Token}";
                QrCode.AlternateText = $"http://{siteUrl}/{token}";
                QrCode.Visible = true;
            }
        }

    }
}
