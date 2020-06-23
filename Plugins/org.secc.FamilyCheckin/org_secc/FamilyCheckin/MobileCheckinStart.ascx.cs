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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using C5;
using CSScriptLibrary;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using Rock;
using Rock.Attribute;
using Rock.Blocks.Types.Mobile.Cms;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Mobile Check-in Start" )]
    [Category( "SECC > Check-in" )]
    [Description( "Start page for the mobile check-in process." )]

    [CodeEditorField( "Tutorial Text",
        "Tutorial text to help the user learn how to check-in.<span class='tip tip-html'></span>",
        Rock.Web.UI.Controls.CodeEditorMode.Html,
        key: AttributeKeys.TutorialText,
        order: 7
        )]

    [CodeEditorField( "Introduction Text",
        "Text which appears as above the select campus page.<span class='tip tip-html'></span>",
        Rock.Web.UI.Controls.CodeEditorMode.Html,
        key: AttributeKeys.IntroductionText,
        order: 8
        )]

    [CodeEditorField( "Code Instructions",
        "Text which appears above the qr code. <span class='tip tip-html'></span>",
        Rock.Web.UI.Controls.CodeEditorMode.Html,
        key: AttributeKeys.CodeInstructions,
        order: 9
        )]

    [CodeEditorField( "Post Check-in Instructions",
        "Text which appears immediatly after the user checks in. <span class='tip tip-html'></span>",
        Rock.Web.UI.Controls.CodeEditorMode.Html,
        key: AttributeKeys.PostCheckinInstructions,
        order: 10
        )]

    public partial class MobileCheckinStart : CheckInBlock
    {

        private static class AttributeKeys
        {
            public const string TutorialText = "TutorialText";
            public const string IntroductionText = "IntroductionText";
            public const string CodeInstructions = "CodeInstructions";
            public const string PostCheckinInstructions = "PostCheckinInstructions";
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/scripts/jquery.plugin.min.js" );
            RockPage.AddScriptLink( "~/scripts/jquery.countdown.min.js" );
            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", fingerprint: false );

            btnCancelReseration.Attributes["onclick"] = "javascript: return Rock.dialogs.confirmDelete(event, 'check-in reservation');";
        }

        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( CurrentUser == null )
                {
                    pnlError.Visible = true;
                    pnlSelectCampus.Visible = false;
                    return;
                }

                string kioskName = CurrentUser.UserName;

                var mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByFamilyGroupId( CurrentPerson.PrimaryFamilyId ?? 0 );
                if ( mobileCheckinRecord == null )
                {
                    mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByUserName( kioskName );
                }

                if ( mobileCheckinRecord != null )
                {
                    ShowQRCode( mobileCheckinRecord );
                    return;
                }


                BindDropDown();

                if ( this.IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) && PageParameter( "KioskName" ).IsNotNullOrWhiteSpace() )
                {
                    kioskName = PageParameter( "KioskName" );
                }

                ConfigureKiosk( kioskName );

                pnlTutorial.Visible = true;
                lTutorial.Text = GetAttributeValue( AttributeKeys.TutorialText );
                pnlSelectCampus.Visible = false;

            }
        }

        private void ShowQRCode( MobileCheckinRecordCache record )
        {
            pnlQr.Visible = true;
            iQr.ImageUrl = "/api/qr/" + record.AccessKey;
            ltCodeInstructions.Text = GetAttributeValue( AttributeKeys.CodeInstructions );
            if ( record.ReservedUntilDateTime.HasValue )
            {
                ltValidUntil.Text = record.ReservedUntilDateTime.Value.ToString( "h:mm tt" );
            }
            else
            {
                ltValidUntil.Text = "some time in the future";
            }

            RegisterSignalrScript( record.AccessKey );
        }

        private void RegisterSignalrScript( string accessKey )
        {
            var script = string.Format( @"
  $(function () {{
        var proxy = $.connection.rockMessageHub;
        proxy.client.mobilecheckincomplete = function (name, iscomplete) {{
            if (name == '{0}') {{
                if (iscomplete) {{
                    checkinComplete();
                }}
            }}
        }}
        $.connection.hub.start().done(function () {{
        }});
    }})
", accessKey );

            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "SignalRScript", script, true );
        }

        /// <summary>
        /// Sets up the kiosk for display.
        /// </summary>
        /// <param name="kioskName">Name of the kiosk.</param>
        private void ConfigureKiosk( string kioskName )
        {
            lIntroduction.Text = GetAttributeValue( AttributeKeys.IntroductionText );

            pnlSelectCampus.Visible = true;
            var rockContext = new RockContext();

            var mobileUserCategory = CategoryCache.Get( org.secc.FamilyCheckin.Utilities.Constants.KIOSK_CATEGORY_MOBILEUSER );

            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.Queryable( "KioskType" )
                .Where( k => k.CategoryId == mobileUserCategory.Id && k.Name == kioskName )
                .FirstOrDefault();

            if ( kiosk == null )
            {
                kiosk = new Kiosk
                {
                    Name = kioskName,
                    CategoryId = mobileUserCategory.Id,
                    Description = "Automatically created mobile Kiosk"
                };
                kioskService.Add( kiosk );
                rockContext.SaveChanges();
            }

            var kioskType = kiosk.KioskType;
            if ( kioskType != null )
            {
                ddlCampus.SetValue( kioskType.CampusId.ToString() );
            }
            else
            {
                ddlCampus.SetValue( CurrentPerson.PrimaryCampusId.ToString() );
            }
            UpdateKioskText();
        }

        private void BindDropDown()
        {
            var kioskTypes = KioskTypeCache.All()
                .Where( kt => kt.IsMobile && kt.Campus != null )
                .OrderBy( kt => kt.Campus.Name )
                .Select( kt => new { Text = kt.Campus.Name, Value = kt.Id.ToString() } )
                .ToList();

            ddlCampus.DataSource = kioskTypes;
            ddlCampus.DataBind();
        }


        protected void Timer1_Tick( object sender, EventArgs e )
        {
        }

        protected void ddlCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateKioskText();
        }

        private void UpdateKioskText()
        {
            if ( ddlCampus.SelectedValue.IsNotNullOrWhiteSpace() )
            {
                var kioskTypeId = ddlCampus.SelectedValue.AsInteger();
                var kioskType = KioskTypeCache.Get( kioskTypeId );

                lCampusLava.Text = kioskType.Message;
                DateTime? activeAt = null;
                bool isOpen = CheckinIsActive( kioskType, out activeAt );

                if ( isOpen )
                {
                    btnSelectCampus.Enabled = true;
                    btnSelectCampus.Text = "Begin Check-in";
                }
                else if ( activeAt.HasValue )
                {
                    btnSelectCampus.Enabled = false;
                    lblActiveWhen.Text = activeAt.Value.AddSeconds( 30 ).ToString( "o" );
                    RegisterCountdownScript();
                }
                else
                {
                    btnSelectCampus.Enabled = false;
                    btnSelectCampus.Text = "No More Check-ins Today";
                }
            }
        }

        private void RegisterCountdownScript()
        {
            var script = @"
$('.btn-select').countdown({until: new Date($('.active-when').text()),
    format:'hMS',
    labels: ['Years ', 'Months ', 'Weeks ', 'Days ', 'H ', 'M ', 'S '],
    labels1: ['Years ', 'Months ', 'Weeks ', 'Days ', 'H ', 'M ', 'S '],
    significant: 2,
    description: 'Until Active',
    expiryText: 'Refreshing...',
    onExpiry: refreshPage 
})";

            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "countdown", script, true );
        }

        private bool CheckinIsActive( KioskTypeCache kioskType, out DateTime? activeAt )
        {
            activeAt = null;
            var currentDateTime = RockDateTime.Now;

            if ( !kioskType.IsOpen( currentDateTime ) )
            {
                activeAt = kioskType.GetNextOpen( currentDateTime );
                return false;
            }
            else
            {
                var schedules = kioskType.CheckInSchedules;

                if ( schedules.Where( s => s.WasCheckInActive( currentDateTime ) ).Any() )
                {
                    return true;
                }
                else if ( schedules.Where( s => s.GetNextCheckInStartTime( currentDateTime ).HasValue ).Any() )
                {
                    activeAt = schedules
                        .Select( s => s.GetNextCheckInStartTime( currentDateTime ) )
                        .Where( a => a.HasValue
                        ).Min( a => a.Value );
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }

        protected void btnSelectCampus_Click( object sender, EventArgs e )
        {
            var kiosk = ConfigureKiosk();
            var kioskType = KioskTypeCache.Get( kiosk.KioskTypeId ?? 0 );

            DateTime? activeAt = null;
            if ( CheckinIsActive( kioskType, out activeAt ) )
            {
                ActivateCheckIn();
            }
        }

        private void ActivateCheckIn()
        {
            var localDeviceConfigCookie = Request.Cookies[CheckInCookieKey.LocalDeviceConfig].Value;
            var localDevice = localDeviceConfigCookie.FromJsonOrNull<LocalDeviceConfiguration>();
            CurrentCheckInState = new CheckInState( localDevice.CurrentKioskId.Value, localDevice.CurrentCheckinTypeId, localDevice.CurrentGroupTypeIds );
            var userloginCheckinSearchValue = DefinedValueCache.Get( org.secc.FamilyCheckin.Utilities.Constants.CHECKIN_SEARCH_TYPE_USERLOGIN );
            CurrentCheckInState.CheckIn.SearchType = userloginCheckinSearchValue;
            CurrentCheckInState.CheckIn.SearchValue = CurrentUser.UserName;
            Session["BlockGuid"] = BlockCache.Guid;
            SaveState();

            pnlSelectCampus.Visible = false;
            pnlLoading.Visible = true;

            var nextpageUrl = LinkedPageUrl( "NextPage" );
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "Load", "processMobileCheckin('" + nextpageUrl + "')", true );
        }

        private Kiosk ConfigureKiosk()
        {
            string kioskName = CurrentUser.UserName;

            if ( this.IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) && PageParameter( "KioskName" ).IsNotNullOrWhiteSpace() )
            {
                kioskName = PageParameter( "KioskName" );
            }

            var rockContext = new RockContext();

            var kioskTypeId = ddlCampus.SelectedValue.AsInteger();

            var kioskType = KioskTypeCache.Get( kioskTypeId );

            var mobileUserCategory = CategoryCache.Get( org.secc.FamilyCheckin.Utilities.Constants.KIOSK_CATEGORY_MOBILEUSER );

            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.Queryable( "KioskType" )
                .Where( k => k.CategoryId == mobileUserCategory.Id && k.Name == kioskName )
                .FirstOrDefault();

            if ( kiosk == null )
            {
                kiosk = new Kiosk
                {
                    Name = kioskName,
                    CategoryId = mobileUserCategory.Id,
                    Description = "Automatically created mobile Kiosk"
                };
                kioskService.Add( kiosk );
                rockContext.SaveChanges();
            }

            kiosk.KioskTypeId = kioskType.Id;

            DeviceService deviceService = new DeviceService( rockContext );
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

            if ( this.IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) && PageParameter( "datetime" ).IsNotNullOrWhiteSpace() )
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
            Session["KioskTypeId"] = kioskType.Id;
            Session["KioskMessage"] = kioskType.Message;
            KioskDevice.Remove( device.Id );
            SaveState();

            return kiosk;
        }

        protected void btnTutorial_Click( object sender, EventArgs e )
        {
            pnlTutorial.Visible = false;
            pnlSelectCampus.Visible = true;
            UpdateKioskText();
        }

        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            UpdateKioskText();
        }

        protected void lbCheckinComplete_Click( object sender, EventArgs e )
        {
            pnlQr.Visible = false;
            pnlPostCheckin.Visible = true;
            ltPostCheckin.Text = GetAttributeValue( AttributeKeys.PostCheckinInstructions );
        }

        protected void btnCancelReseration_Click( object sender, EventArgs e )
        {
            var mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByFamilyGroupId( CurrentPerson.PrimaryFamilyId ?? 0 );
            if ( mobileCheckinRecord == null )
            {
                string kioskName = CurrentUser.UserName;
                mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByUserName( kioskName );
            }

            if ( mobileCheckinRecord != null )
            {
                MobileCheckinRecordCache.CancelReservation( mobileCheckinRecord, true );
            }
            NavigateToCurrentPage();
        }
    }
}