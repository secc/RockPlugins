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
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using DotLiquid.Util;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Migrations;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "AutoConfigure" )]
    [Category( "SECC > Check-in" )]
    [Description( "Checkin auto configure block" )]

    [BooleanField( "Manual",
        Description = "Allow for manual configuration",
        Key = AttributeKeys.Manual )]

    [BooleanField( "Legacy Mode",
        Description = "Allow for the using of the hostname to set the kiosk.",
        DefaultBooleanValue = true,
        Key = AttributeKeys.LegacyMode )]

    public partial class AutoConfigure : CheckInBlock
    {
        static class AttributeKeys
        {
            internal const string LegacyMode = "LegacyMode";
            internal const string Manual = "Manual";
        }

        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( GetAttributeValue( "Manual" ).AsBoolean() && CurrentUser != null )
                {
                    ShowManual();
                    return;
                }

                Kiosk kiosk;

                kiosk = GetKioskFromURL();
                if ( kiosk != null )
                {
                    SetKiosk( kiosk, false );
                    return;
                }

                kiosk = GetKioskFromCookie();
                if ( kiosk != null )
                {
                    SetKiosk( kiosk );
                    return;
                }

                if ( GetAttributeValue( AttributeKeys.LegacyMode ).AsBoolean() )
                {
                    //Use javascript to get data
                    ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "GetClient", "setTimeout(function(){getClientName()},100);", true );
                    return;
                }
                else
                {
                    ShowKioskConfig();
                }
            }
            else if ( Request["__EVENTTARGET"] == "ClientName"   // If postback from javascript
                && GetAttributeValue( AttributeKeys.LegacyMode ).AsBoolean() )

            {
                //Use Kiosk given client name
                Kiosk kiosk = GetOrCreateKiosk( Request["__EVENTARGUMENT"] );
                SetKiosk( kiosk );
                return;
            }
            else if ( Request["__EVENTTARGET"] == "UseDNS" )
            {
                //if the Javascript request throws an error
                //try to get kiosk name via Javascript
                try
                {
                    var ip = Rock.Web.UI.RockPage.GetClientIpAddress();
                    var kiosk = GetOrCreateKiosk( System.Net.Dns.GetHostEntry( ip ).HostName );
                    SetKiosk( kiosk );
                    return;
                }
                catch
                {
                    ShowKioskConfig();
                }
            }
        }

        private Kiosk GetKioskFromURL()
        {
            if ( PageParameter( "KioskName" ).IsNotNullOrWhiteSpace() || PageParameter( "datetime" ).IsNotNullOrWhiteSpace() )
            {
                if ( CurrentUser == null || !UserCanEdit )
                {
                    var site = RockPage.Layout.Site;
                    if ( site.LoginPageId.HasValue )
                    {
                        site.RedirectToLoginPage( true );
                        return null;
                    }
                    else
                    {
                        FormsAuthentication.RedirectToLoginPage();
                        return null;
                    }
                }
            }
            return GetKioskByName( PageParameter( "KioskName" ) );
        }

        private Kiosk GetKioskByName( string kioskName )
        {
            return new KioskService( new RockContext() ).Queryable().AsNoTracking().Where( k => k.Name == kioskName ).FirstOrDefault();
        }

        private void ShowKioskConfig()
        {
            if ( CurrentUser == null || !UserCanEdit )
            {
                var site = RockPage.Layout.Site;
                if ( site.LoginPageId.HasValue )
                {
                    site.RedirectToLoginPage( true );
                    return;
                }
                else
                {
                    FormsAuthentication.RedirectToLoginPage();
                    return;
                }
            }

            BindDropDownLists();

            pnlConfig.Visible = true;
            pnlManual.Visible = false;
        }

        private void SetCookie( Kiosk kiosk )
        {
            var kioskNameCookie = this.Page.Request.Cookies[Constants.COOKIE_KIOSK_NAME];
            if ( kioskNameCookie == null )
            {
                kioskNameCookie = new System.Web.HttpCookie( Constants.COOKIE_KIOSK_NAME );
            }

            if ( kiosk.AccessKey.IsNullOrWhiteSpace() )
            {
                RockContext rockContext = new RockContext();
                KioskService kioskService = new KioskService( rockContext );
                kiosk = kioskService.Get( kiosk.Id );
                kiosk.AccessKey = Guid.NewGuid().ToString();
                rockContext.SaveChanges();
            }

            kioskNameCookie.Expires = RockDateTime.Now.AddYears( 1 );

            string encryptedAccessKey;

            if ( Encryption.TryEncryptString( kiosk.AccessKey, out encryptedAccessKey ) )
            {
                kioskNameCookie.Value = encryptedAccessKey;
                Page.Response.Cookies.Set( kioskNameCookie );
            }

        }

        private Kiosk GetKioskFromCookie()
        {
            var kioskNameCookie = Page.Request.Cookies[Constants.COOKIE_KIOSK_NAME];
            if ( kioskNameCookie == null || kioskNameCookie.Value.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var cookieKey = Encryption.DecryptString( kioskNameCookie.Value );

            return new KioskService( new RockContext() ).Queryable().AsNoTracking().Where( k => k.AccessKey == cookieKey ).FirstOrDefault();
        }

        private void ShowManual()
        {
            if ( CurrentUser != null )
            {
                pnlConfig.Visible = false;
                pnlManual.Visible = true;
                BindDropDownLists();
            }
        }

        private Kiosk GetOrCreateKiosk( string kioskName )
        {
            return GetOrCreateKiosk( kioskName, 0 );
        }

        private Kiosk GetOrCreateKiosk( string kioskName, int kioskTypeId )
        {
            var rockContext = new RockContext();

            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.GetByClientName( kioskName );
            if ( kiosk == null )
            {
                kiosk = new Kiosk
                {
                    Name = kioskName,
                    Description = "Automatically created Kiosk"
                };
                kioskService.Add( kiosk );

            }
            if ( kioskTypeId != 0 )
            {
                kiosk.KioskTypeId = kioskTypeId;
            }
            kiosk.CategoryId = CategoryCache.GetId( Constants.KIOSK_CATEGORY_STATION.AsGuid() );
            rockContext.SaveChanges();

            //Fresh version with new context.
            kiosk = new KioskService( new RockContext() ).Get( kiosk.Id );

            return kiosk;
        }

        private void SetKiosk( Kiosk kiosk, bool setCookie = true )
        {
            if ( kiosk == null )
            {
                return;
            }

            if ( setCookie )
            {
                SetCookie( kiosk );
            }

            if ( kiosk.KioskType == null )
            {
                tbKioskName.Text = kiosk.Name;
                ShowKioskConfig();
                return;
            }

            ActivateKiosk( kiosk, true );
        }


        private void ActivateKiosk( Kiosk kiosk, bool logout )
        {
            RockContext rockContext = new RockContext();
            DeviceService deviceService = new DeviceService( rockContext );
            KioskService kioskService = new KioskService( rockContext );

            //The kiosk can come in a variety of states.
            //Get a fresh version with our context to avoid context errors.
            kiosk = kioskService.Get( kiosk.Id );

            //Load matching device and update or create information
            var device = deviceService.Queryable().Where( d => d.Name == kiosk.Name ).FirstOrDefault();

            //create new device to match our kiosk
            if ( device == null )
            {
                device = new Device();
                device.DeviceTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;
                device.Name = kiosk.Name;
                deviceService.Add( device );
            }

            device.LoadAttributes();
            device.IPAddress = kiosk.IPAddress;
            device.Locations.Clear();
            foreach ( var loc in kiosk.KioskType.Locations.ToList() )
            {
                device.Locations.Add( loc );
            }
            device.PrintFrom = kiosk.PrintFrom;
            device.PrintToOverride = kiosk.PrintToOverride;
            device.PrinterDeviceId = kiosk.PrinterDeviceId;
            rockContext.SaveChanges();

            if ( PageParameter( "DateTime" ).AsDateTime().HasValue )
            {
                device.SetAttributeValue( "core_device_DebugDateTime", PageParameter( "datetime" ) );
            }
            else
            {
                device.SetAttributeValue( "core_device_DebugDateTime", "" );
            }
            device.SaveAttributeValues( rockContext );

            LocalDeviceConfig.CurrentKioskId = device.Id;
            LocalDeviceConfig.CurrentGroupTypeIds = kiosk.KioskType.GroupTypes.Select( gt => gt.Id ).ToList();
            LocalDeviceConfig.CurrentCheckinTypeId = kiosk.KioskType.CheckinTemplateId;

            CurrentCheckInState = null;
            CurrentWorkflow = null;

            var kioskTypeCookie = this.Page.Request.Cookies["KioskTypeId"];

            if ( kioskTypeCookie == null )
            {
                kioskTypeCookie = new System.Web.HttpCookie( "KioskTypeId" );
            }

            kioskTypeCookie.Expires = RockDateTime.Now.AddYears( 1 );
            kioskTypeCookie.Value = kiosk.KioskType.Id.ToString();

            this.Page.Response.Cookies.Set( kioskTypeCookie );

            Session["KioskTypeId"] = kiosk.KioskType.Id;
            Session["KioskMessage"] = kiosk.KioskType.Message;

            //Clean things up so we have the freshest possible version.
            KioskTypeCache.Remove( kiosk.KioskTypeId ?? 0 );
            KioskDevice.Remove( device.Id );

            Dictionary<string, string> pageParameters = new Dictionary<string, string>();
            if ( kiosk.KioskType.Theme.IsNotNullOrWhiteSpace() && !GetAttributeValue( "Manual" ).AsBoolean() )
            {
                LocalDeviceConfig.CurrentTheme = kiosk.KioskType.Theme;
                pageParameters.Add( "theme", LocalDeviceConfig.CurrentTheme );
            }

            if ( logout )
            {
                pageParameters.Add( "logout", "true" );
            }

            SaveState();

            NavigateToNextPage( pageParameters );
        }

        protected void btnSelectKiosk_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var kioskTypeService = new KioskTypeService( rockContext );
            var kioskType = kioskTypeService.Get( ddlManualKioskType.SelectedValue.AsInteger() );
            if ( kioskType == null )
            {
                return;
            }


            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.GetByClientName( CurrentUser.UserName );
            if ( kiosk == null )
            {
                kiosk = new Kiosk();
                kiosk.Name = CurrentUser.UserName;
                kiosk.Description = "Automatically created personal Kiosk";
                kioskService.Add( kiosk );
            }
            kiosk.KioskTypeId = kioskType.Id;
            kiosk.CategoryId = CategoryCache.GetId( Constants.KIOSK_CATEGORY_STAFFUSER.AsGuid() );
            rockContext.SaveChanges();

            SetBlockUserPreference( "KioskTypeId", kioskType.Id.ToString() );

            ActivateKiosk( kiosk, false );
        }

        private void BindDropDownLists( Kiosk kiosk = null )
        {
            RockContext rockContext = new RockContext();
            KioskTypeService kioskTypeService = new KioskTypeService( rockContext );

            var kioskTypes = kioskTypeService
                .Queryable()
                .OrderBy( t => t.Name )
                .Select( t => new
                {
                    t.Name,
                    t.Id
                } )
                .ToList();

            ddlManualKioskType.DataSource = kioskTypes;
            ddlManualKioskType.DataBind();

            ddlKioskType.DataSource = kioskTypes;
            ddlKioskType.DataBind();

            var preSelectedKioskTypeId = GetBlockUserPreference( "KioskTypeId" ).AsInteger();
            if ( kioskTypes.Where( k => k.Id == preSelectedKioskTypeId ).Any() )
            {
                ddlManualKioskType.SelectedValue = preSelectedKioskTypeId.ToString();
            }
        }

        protected void btnStart_Click( object sender, EventArgs e )
        {
            if ( tbKioskName.Text.IsNullOrWhiteSpace() )
            {
                return;
            }

            var kiosk = GetOrCreateKiosk( tbKioskName.Text, ddlKioskType.SelectedValueAsId() ?? 0 );
            SetKiosk( kiosk );
        }
    }
}