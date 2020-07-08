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
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using CSScriptLibrary;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Mobile Check-in Start" )]
    [Category( "SECC > Check-in" )]
    [Description( "Start page for the mobile check-in process." )]

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

    [CodeEditorField( "Not Logged In Message",
        "Message to show if the user is not logged in. <span class='tip tip-html'></span>",
        Rock.Web.UI.Controls.CodeEditorMode.Html,
        key: AttributeKeys.NotLoggedInMessage,
        order: 11
        )]

    [BooleanField( "Debug Mode",
        Description = "Turn on debug mode, for debugging and load testing.",
        DefaultBooleanValue = false,
        Key = AttributeKeys.DebugMode )]

    public partial class MobileCheckinStart : CheckInBlock
    {

        private static class AttributeKeys
        {
            internal const string IntroductionText = "IntroductionText";
            internal const string CodeInstructions = "CodeInstructions";
            internal const string PostCheckinInstructions = "PostCheckinInstructions";
            internal const string NotLoggedInMessage = "NotLoggedInMessage";
            internal const string DebugMode = "DebugMode";
        }

        private static class PageParameterKeys
        {
            internal const string CampusId = "CampusId";
            internal const string UserName = "UserName";
        }

        private Person currentPerson;
        private UserLogin currentUser;


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
            SetRefreshTimer();

            currentUser = CurrentUser;
            currentPerson = CurrentPerson;

            //This allows us to use a username to build use a username to test or debug
            if ( PageParameter( PageParameterKeys.UserName ).IsNotNullOrWhiteSpace()
                && ( this.IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) || GetAttributeValue( AttributeKeys.DebugMode ).AsBoolean() ) )
            {
                var username = PageParameter( PageParameterKeys.UserName );
                RockContext rockContext = new RockContext();
                UserLoginService userLoginService = new UserLoginService( rockContext );

                currentUser = userLoginService.Queryable().Where( u => u.UserName == username ).FirstOrDefault();
                if ( currentUser != null )
                {
                    currentPerson = currentUser.Person;
                }
            }

            if ( !Page.IsPostBack )
            {
                BindDropDown();
                UpdateView();
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

            SetRefreshTimer( 30 );
            RegisterSignalrScript( record.AccessKey );
        }

        private void SetRefreshTimer( int seconds = 60 * 60 )
        {
            RefreshNotiTimer.Interval = seconds * 1000;
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

            var rockContext = new RockContext();

            var mobileUserCategory = CategoryCache.Get( org.secc.FamilyCheckin.Utilities.Constants.KIOSK_CATEGORY_MOBILEUSER );

            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.Queryable( "KioskType" )
                .Where( k => k.Name == kioskName )
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

            KioskTypeCache kioskType = null;

            var campusParameter = PageParameter( PageParameterKeys.CampusId );
            if ( campusParameter.IsNotNullOrWhiteSpace() )
            {
                kioskType = KioskTypeCache.All().Where( k => k.IsMobile && k.CampusId == campusParameter.AsIntegerOrNull() ).FirstOrDefault();
            }

            if ( kioskType == null )
            {
                if ( kiosk.KioskTypeId.HasValue )
                {
                    kioskType = KioskTypeCache.Get( kiosk.KioskTypeId.Value );
                }
            }

            if ( kioskType != null )
            {
                ddlCampus.SetValue( kioskType.Id.ToString() );
            }
            else
            {
                ddlCampus.SetValue( currentPerson.PrimaryCampusId.ToString() );
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


        protected void RefreshNotiTimer_Tick( object sender, EventArgs e )
        {
            UpdateView();
        }

        private void UpdateView()
        {
            pnlQr.Visible = false;
            pnlLoading.Visible = false;
            pnlPostCheckin.Visible = false;
            pnlSelectCampus.Visible = false;

            if ( currentUser == null )
            {
                ltError.Text = GetAttributeValue( AttributeKeys.NotLoggedInMessage );
                pnlError.Visible = true;
                return;
            }

            string kioskName = currentUser.UserName;

            var mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByFamilyGroupId( currentPerson.PrimaryFamilyId ?? 0 );
            if ( mobileCheckinRecord == null )
            {
                mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByUserName( kioskName );
            }

            if ( mobileCheckinRecord != null )
            {
                ShowQRCode( mobileCheckinRecord );
                return;
            }
            else
            {
                var completeMobileCheckins = MobileCheckinRecordCache.All()
                    .Where( r => r.FamilyGroupId == currentPerson.PrimaryFamilyId && r.Status == MobileCheckinStatus.Complete )
                    .Any();
                if ( completeMobileCheckins )
                {
                    ShowCheckinCompletion();
                    return;
                }
            }


            ConfigureKiosk( kioskName );

            pnlSelectCampus.Visible = true;

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
                    btnSelectCampus.OnClientClick = "";
                    btnSelectCampus.Text = "Begin Check-in";
                }
                else if ( activeAt.HasValue )
                {
                    btnSelectCampus.Enabled = false;
                    btnSelectCampus.OnClientClick = "javascript:return false;";
                    lblActiveWhen.Text = activeAt.Value.AddSeconds( 30 ).ToString( "o" );
                    RegisterCountdownScript();
                }
                else
                {
                    btnSelectCampus.Enabled = false;
                    btnSelectCampus.OnClientClick = "javascript:return false;";
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
            CurrentCheckInState = new CheckInState( LocalDeviceConfig.CurrentKioskId.Value, LocalDeviceConfig.CurrentCheckinTypeId, LocalDeviceConfig.CurrentGroupTypeIds );
            var userloginCheckinSearchValue = DefinedValueCache.Get( org.secc.FamilyCheckin.Utilities.Constants.CHECKIN_SEARCH_TYPE_USERLOGIN );
            CurrentCheckInState.CheckIn.SearchType = userloginCheckinSearchValue;
            CurrentCheckInState.CheckIn.SearchValue = currentUser.UserName;
            Session["BlockGuid"] = BlockCache.Guid;
            SaveState();

            pnlSelectCampus.Visible = false;
            pnlLoading.Visible = true;

            var nextpageUrl = LinkedPageUrl( "NextPage" );
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "Load", "processMobileCheckin('" + nextpageUrl + "')", true );
        }

        private Kiosk ConfigureKiosk()
        {
            var rockContext = new RockContext();
            var kioskTypeId = ddlCampus.SelectedValue.AsInteger();

            var kioskType = KioskTypeCache.Get( kioskTypeId );
            string kioskName = currentUser.UserName;

            var mobileUserCategory = CategoryCache.Get( org.secc.FamilyCheckin.Utilities.Constants.KIOSK_CATEGORY_MOBILEUSER );

            var kioskService = new KioskService( rockContext );
            var kiosk = kioskService.Queryable( "KioskType" )
                .Where( k => k.Name == kioskName )
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
            }

            kiosk.KioskTypeId = kioskType.Id;
            rockContext.SaveChanges();

            DeviceService deviceService = new DeviceService( rockContext );

            var deviceName = "Mobile:" + kioskType.Name.RemoveAllNonAlphaNumericCharacters();

            //Load matching device and update or create information
            var device = deviceService.Queryable( "Location" ).Where( d => d.Name == deviceName ).FirstOrDefault();

            var dirty = false;

            //create new device to match our kiosk
            if ( device == null )
            {
                device = new Device();
                device.DeviceTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;
                device.Name = deviceName;
                deviceService.Add( device );
                device.PrintFrom = PrintFrom.Client;
                device.PrintToOverride = PrintTo.Default;
                dirty = true;
            }

            var deviceLocationIds = device.Locations.Select( l => l.Id );
            var ktLocationIds = kioskType.Locations.Select( l => l.Id );

            var unmatchedDeviceLocations = deviceLocationIds.Except( ktLocationIds ).Any();
            var unmatchedKtLocations = ktLocationIds.Except( deviceLocationIds ).Any();

            if ( unmatchedDeviceLocations || unmatchedKtLocations )
            {
                LocationService locationService = new LocationService( rockContext );
                device.Locations.Clear();
                foreach ( var loc in kioskType.Locations.ToList() )
                {
                    var location = locationService.Get( loc.Id );
                    device.Locations.Add( location );
                }
                dirty = true;
            }

            if ( this.IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
            {
                device.LoadAttributes();

                if ( PageParameter( "datetime" ).IsNotNullOrWhiteSpace() )
                {
                    device.SetAttributeValue( "core_device_DebugDateTime", PageParameter( "datetime" ) );
                }
                else
                {
                    device.SetAttributeValue( "core_device_DebugDateTime", "" );
                }
            }

            if ( dirty )
            {
                rockContext.SaveChanges();
                device.SaveAttributeValues( rockContext );
                KioskDevice.Remove( device.Id );
            }

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

            Session["KioskTypeId"] = kioskType.Id;
            SaveState();
            return kiosk;
        }

        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            UpdateKioskText();
        }

        protected void lbCheckinComplete_Click( object sender, EventArgs e )
        {
            ShowCheckinCompletion();
        }


        private void ShowCheckinCompletion()
        {
            pnlQr.Visible = false;
            pnlPostCheckin.Visible = true;
            ltPostCheckin.Text = GetAttributeValue( AttributeKeys.PostCheckinInstructions );

            //This is all an elaborate effort to get the family's checkin data in an organized fashion without touching the db
            //The thought is I can add more webservers I can't add more database servers right now.

            var personIds = CurrentPerson.PrimaryFamily.Members.Select( m => m.Person )
                .OrderBy( p => p.AgeClassification )
                .ThenBy( p => p.BirthDate )
                .Select( p => p.Id )
                .ToList();

            var attendances = AttendanceCache.All()
                .Where( a => personIds.Contains( a.PersonId ) && a.AttendanceState != AttendanceState.CheckedOut )
                .ToList();

            var scheduleIds = attendances.Select( a => a.ScheduleId ).Distinct().ToList();

            var tokenOccurrences = OccurrenceCache.All() //Just need the schedule data so we can order stuff.
                .Where( o => scheduleIds.Contains( o.ScheduleId ) )
                .DistinctBy( o => o.ScheduleId )
                .OrderBy( o => o.ScheduleStartTime )
                .ToList();

            var attendanceData = new StringBuilder();
            foreach ( var tokenOccurrence in tokenOccurrences )
            {
                if ( attendances.Where( a => a.ScheduleId == tokenOccurrence.ScheduleId ).Any() )
                {
                    attendanceData.Append( "<b>" + tokenOccurrence.ScheduleName + "</b><ul>" );
                    foreach ( var personId in personIds )
                    {
                        var attendance = attendances.FirstOrDefault( a => a.PersonId == personId && a.ScheduleId == tokenOccurrence.ScheduleId );
                        if ( attendance == null )
                        {
                            continue;
                        }

                        OccurrenceCache occurrence = OccurrenceCache.Get( attendance.OccurrenceAccessKey );
                        if ( occurrence == null )
                        {
                            continue;
                        }

                        attendanceData.Append( string.Format( "<li>{0}: {1} in {2}</li>", attendance.PersonName, occurrence.GroupName, occurrence.LocationName ) );
                    }
                    attendanceData.Append( "</ul>" );
                }
            }

            ltAttendance.Text = attendanceData.ToString();
        }

        protected void btnCancelReseration_Click( object sender, EventArgs e )
        {
            var mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByFamilyGroupId( currentPerson.PrimaryFamilyId ?? 0 );
            if ( mobileCheckinRecord == null )
            {
                string kioskName = currentUser.UserName;
                mobileCheckinRecord = MobileCheckinRecordCache.GetActiveByUserName( kioskName );
            }

            if ( mobileCheckinRecord != null )
            {
                MobileCheckinRecordCache.CancelReservation( mobileCheckinRecord, true );
            }
            NavigateToCurrentPage();
        }

        protected void btnNewCheckin_Click( object sender, EventArgs e )
        {
            ConfigureKiosk( CurrentUser.UserName );
            pnlPostCheckin.Visible = false;
            pnlSelectCampus.Visible = true;
        }

        class CheckedInMember
        {
            public string Name { get; set; }
            public List<string> AttendanceData { get; set; }
        }
    }
}