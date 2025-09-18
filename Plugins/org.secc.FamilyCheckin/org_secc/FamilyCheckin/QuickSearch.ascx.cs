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
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Exceptions;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "QuickSearch" )]
    [Category( "SECC > Check-in" )]
    [Description( "QuickSearch block for helping parents find their family quickly." )]
    [TextField( "Person Search Activity", key: AttributeKeys.PersonSearchActivityName, required: false, order: 3, defaultValue: "Person Search" )]
    [TextField( "Save Attendance Activity", key: AttributeKeys.SaveAttendanceActivtyName, required: false, order: 3, defaultValue: "Save Attendance" )]
    [IntegerField( "Minimum Phone Number Length", "Minimum length for phone number searches (defaults to 4).", false, 4 )]
    [IntegerField( "Maximum Phone Number Length", "Maximum length for phone number searches (defaults to 10).", false, 10 )]
    [IntegerField( "Refresh Interval", "How often (seconds) should page automatically query server for new Check-in data", false, 10 )]
    [TextField( "Search Regex", "Regular Expression to run the search input through before sending it to the workflow. Useful for stripping off characters.", false )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.CHECKIN_SEARCH_TYPE, "Search Type", "The type of search to use for check-in (default is phone number).", true, false, Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER, order: 4 )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Aggregated Label", "Binary file that is the parent pickup label", false )]
    [CodeEditorField( "Default Content", "Default content to display", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 12 )]
    [CodeEditorField( "No Mobile Checkin Record", "Message to display when there is no mobile checkin record.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 13, key: AttributeKeys.NoMobileCheckinRecord )]
    [CodeEditorField( "Expired Checkin Record", "Message to display when the check-in record has been exprired/deleted.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 13, key: AttributeKeys.ExpiredMobileCheckinRecord )]
    [CodeEditorField( "Already Completed Checkin Record", "Message to display when the check-in record has been deleted.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 14, key: AttributeKeys.AlreadyCompleteCheckinRecord )]
    [CodeEditorField( "Completing Checkin Record", "Message to display when completing the mobile check-in record.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 15, key: AttributeKeys.CompletingMobileCheckin )]
    [CodeEditorField( "Fast Pass Not Found", "Message to display when a fast pass QR code fails to find a person.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 16, key: AttributeKeys.FastPassNotFound )]
    [CodeEditorField( "Already Checked In", "Message to display when a fast pass person is already checked in.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 17, key: AttributeKeys.AlreadyCheckedIn )]
    [CodeEditorField( "No Checkin Available", "Message to display when a fast pass person has no check-in options.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 18, key: AttributeKeys.NoCheckinAvailable )]
    [CodeEditorField( "Fast Pass Complete", "Message to display when a fast pass person has completed check-in.", CodeEditorMode.Html, CodeEditorTheme.Rock, 200, true, "", "", 20, key: AttributeKeys.FastPassComplete )]
    public partial class QuickSearch : CheckInBlock
    {

        protected static class AttributeKeys
        {
            internal const string NoMobileCheckinRecord = "NoMobileCheckinRecord";
            internal const string ExpiredMobileCheckinRecord = "ExpiredMobileCheckinRecord";
            internal const string AlreadyCompleteCheckinRecord = "AlreadyCompleteCheckinRecord";
            internal const string WrongCampusMessage = "WrongCampusMessage";
            internal const string CompletingMobileCheckin = "CompletingMobileCheckin";
            //Fastpass attributes
            internal const string PersonSearchActivityName = "PersonSearchActivityName";
            internal const string SaveAttendanceActivtyName = "SaveAttendanceActivityName";
            internal const string FastPassNotFound = "FastPassNotFound";
            internal const string AlreadyCheckedIn = "AlreadyCheckedIn";
            internal const string NoCheckinAvailable = "NoCheckinAvailable";
            internal const string FastPassComplete = "FastPassComplete";
        }

        protected int minLength;
        protected int maxLength;
        protected CheckinKioskTypeCache KioskType;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            Session["BlockGuid"] = BlockCache.Guid;

            if ( CurrentCheckInState == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on init" ) );
                NavigateToPreviousPage();
                return;
            }

            var kioskTypeCookie = this.Page.Request.Cookies["KioskTypeId"];
            if ( kioskTypeCookie != null )
            {
                KioskType = CheckinKioskTypeCache.Get( kioskTypeCookie.Value.AsInteger() );
            }

            if ( KioskType == null )
            {
                NavigateToHomePage();
            }

            RockPage.AddScriptLink( "~/scripts/jquery.plugin.min.js" );
            RockPage.AddScriptLink( "~/scripts/jquery.countdown.min.js" );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            RegisterScript();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            minLength = int.Parse( GetAttributeValue( "MinimumPhoneNumberLength" ) );
            maxLength = int.Parse( GetAttributeValue( "MaximumPhoneNumberLength" ) );

            if ( Request["__EVENTTARGET"] == "ChooseFamily" )
            {
                ChooseFamily( Request["__EVENTARGUMENT"] );
            }

            if ( !Page.IsPostBack && CurrentCheckInState != null )
            {
                CurrentWorkflow = null;
                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();

                RefreshView();
                if ( KioskType.Message.IsNotNullOrWhiteSpace() )
                {
                    ltContent.Text = KioskType.Message;
                }
                else
                {
                    ltContent.Text = GetAttributeValue( "DefaultContent" );
                }
            }
        }

        private void ChooseFamily( string familyIdAsString )
        {
            int familyId = Int32.Parse( familyIdAsString );
            CurrentCheckInState = ( CheckInState ) Session["CheckInState"];
            ClearSelection();
            CheckInFamily selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault( f => f.Group.Id == familyId );
            if ( selectedFamily != null )
            {
                try
                {
                    //clear QCPeople session object and get it ready for quick checkin.
                    Session.Remove( "qcPeople" );
                }
                catch { }
                selectedFamily.Selected = true;
                SaveState();
                NavigateToNextPage();
            }
        }


        private void ClearSelection()
        {
            foreach ( var family in CurrentCheckInState.CheckIn.Families )
            {
                family.Selected = false;
                family.People = new List<CheckInPerson>();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            RefreshView();
        }


        /// <summary>
        /// Refreshes the view.
        /// </summary>
        private void RefreshView()
        {
            if ( KioskType == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on refresh view" ) );
                NavigateToHomePage();
                return;
            }

            hfRefreshTimerSeconds.Value = GetAttributeValue( "RefreshInterval" );
            pnlNotActive.Visible = false;
            pnlNotActiveYet.Visible = false;
            pnlClosed.Visible = false;
            pnlActive.Visible = false;
            ManagerLoggedIn = false;
            lblActiveWhen.Text = string.Empty;

            if ( CurrentCheckInState == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on refresh view" ) );
                NavigateToPreviousPage();
                return;
            }

            lDebug.Text = CurrentCheckInState.Kiosk.Device.Name;

            if ( CurrentCheckInState.Kiosk.DebugDateTime.HasValue )
            {
                lDebug.Text += ": "+ CurrentCheckInState.Kiosk.DebugDateTime.Value.ToString();
            }

            var currentDateTime = CurrentCheckInState.Kiosk.DebugDateTime.HasValue ? CurrentCheckInState.Kiosk.DebugDateTime.Value : RockDateTime.Now;

            if ( !KioskType.IsOpen( currentDateTime ) )
            {
                DateTime? activeAt = KioskType.GetNextOpen( currentDateTime );
                if ( activeAt == null )
                {
                    pnlNotActive.Visible = true;
                    HideSign();
                }
                else
                {
                    lblActiveWhen.Text = ( activeAt ?? currentDateTime.Date.AddDays( 1 ) ).ToString( "o" );
                    pnlNotActiveYet.Visible = true;
                    HideSign();
                }
                ScriptManager.RegisterStartupScript( Page, Page.GetType(), "Set Kiosk Active", "kioskActive=false;", true );
            }
            else
            {
                var schedules = KioskType.CheckInSchedules;

                if ( schedules.Where( s => s.WasCheckInActive( currentDateTime ) ).Any() )
                {
                    pnlActive.Visible = true;
                    ShowWelcomeSign();
                    ScriptManager.RegisterStartupScript( Page, Page.GetType(), "Set Kiosk Active", "kioskActive=true;", true );
                }
                else if ( schedules.Where( s => s.GetNextCheckInStartTime( currentDateTime ).HasValue ).Any() )
                {
                    DateTime activeAt = schedules
                        .Select( s => s.GetNextCheckInStartTime( currentDateTime ) )
                        .Where( a => a.HasValue
                        ).Min( a => a.Value );
                    lblActiveWhen.Text = activeAt.ToString( "o" );
                    pnlNotActiveYet.Visible = true;
                    HideSign();
                    ScriptManager.RegisterStartupScript( Page, Page.GetType(), "Set Kiosk Active", "kioskActive=false;", true );
                }
                else
                {
                    pnlNotActive.Visible = true;
                    HideSign();
                    ScriptManager.RegisterStartupScript( Page, Page.GetType(), "Set Kiosk Active", "kioskActive=false;", true );
                }

            }
        }

        private void HideSign()
        {
            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "HideSign", "hideSign();", true );
        }

        private void ShowWelcomeSign()
        {
            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "ShowWelcomeSign", "showWelcome();", true );
        }

        private void ShowWrongCampusSign( string mobileCampus, string actualCampus )
        {
            var content = string.Format( "<h2>Uh Oh.</h2> You seem to be trying to check in to the {0} campus, but your reservation is for {1}.<br> Please see the check-in volunteer for assistance.", actualCampus, mobileCampus );
            MobileCheckinMessage( content, 20 );
        }

        /// <summary>
        /// Registers the script.
        /// </summary>
        private void RegisterScript()
        {
            // Note: the OnExpiry property of the countdown jquery plugin seems to add a new callback
            // everytime the setting is set which is why the clearCountdown method is used to prevent 
            // a plethora of partial postbacks occurring when the countdown expires.
            string script = string.Format( @"
var timeoutSeconds = $('.js-refresh-timer-seconds').val();
if (timeout) {{
    window.clearTimeout(timeout);
}}
var timeout = window.setTimeout(function(){{checkStatus( {1} )}}, timeoutSeconds * 1000);

var $ActiveWhen = $('.active-when');
var $CountdownTimer = $('.countdown-timer');

function refreshKiosk() {{
    window.clearTimeout(timeout);
    var input = $('input[id$= \'tbPhone\']').get(0);
    if (input) {{
        if (input.value.length<1) {{
            {0};
        }} else {{
            timeout = window.setTimeout(function(){{checkStatus( {1} )}}, timeoutSeconds * 1000)
        }}
    }} else {{
        {0};
    }}
}}

function clearCountdown() {{
    if ($ActiveWhen.text() != '')
    {{
        $ActiveWhen.text('');
        refreshKiosk();
    }}
}}

if ($ActiveWhen.text() != '')
{{
    var timeActive = new Date($ActiveWhen.text());
    timeActive.setSeconds(timeActive.getSeconds() + 15);
    $CountdownTimer.countdown({{
        until: timeActive, 
        compact:true, 
        onExpiry: clearCountdown
    }});
}}

", this.Page.ClientScript.GetPostBackEventReference( lbRefresh, "" ), KioskType.Id );
            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "RefreshScript", script, true );
        }

        protected void btnMobileCheckin_Click( object sender, EventArgs e )
        {
            if ( hfMobileAccessKey.Value.StartsWith( "MCR" ) )
            {
                MobileCheckin( hfMobileAccessKey.Value );
            }
            else if ( hfMobileAccessKey.Value.StartsWith( "PFP" ) )
            {
                FastPassCheckin( hfMobileAccessKey.Value.Substring( 3 ) );
            }
        }

        private void FastPassCheckin( string accessKey )
        {
            try
            {
                CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_SCANNED_ID );
                CurrentCheckInState.CheckIn.SearchValue = accessKey;
                CurrentCheckInState.CheckIn.ConfirmSingleFamily = false;
                CurrentCheckInState.CheckIn.UserEnteredSearch = false;

                string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                List<string> errors = new List<string>();
                bool test = ProcessActivity( workflowActivity, out errors );

                if ( !CurrentCheckInState.CheckIn.Families.Any() || !CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.HasValue )
                {
                    MobileCheckinMessage( GetAttributeValue( AttributeKeys.FastPassNotFound ) );
                    return;
                }

                CurrentCheckInState.CheckIn.Families.FirstOrDefault().Selected = true;
                test = ProcessActivity( GetAttributeValue( AttributeKeys.PersonSearchActivityName ), out errors );

                RockContext rockContext = new RockContext();
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                var personId = personAliasService.Queryable().AsNoTracking()
                    .Where( pa => pa.Id == CurrentCheckInState.CheckIn.CheckedInByPersonAliasId.Value )
                    .Select( pa => pa.PersonId )
                    .FirstOrDefault();

                var person = CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Person.Id == personId ).FirstOrDefault();
                var selected = false;

                if ( person == null )
                {
                    MobileCheckinMessage( GetAttributeValue( AttributeKeys.NoCheckinAvailable ), 4 );
                    return;
                }

                foreach ( var gt in person.GroupTypes.OrderByDescending( gt => gt.PreSelected ) )
                {
                    foreach ( var g in gt.Groups.OrderByDescending( g => g.PreSelected ) )
                    {
                        foreach ( var l in g.Locations.OrderByDescending( l => l.PreSelected ) )
                        {
                            foreach ( var s in l.Schedules )
                            {
                                s.Selected = true;
                                l.Selected = true;
                                g.Selected = true;
                                gt.Selected = true;
                                selected = true;
                            }
                            if ( selected )
                            {
                                break;
                            }
                        }
                        if ( selected )
                        {
                            break;

                        }
                    }
                    if ( selected )
                    {
                        break;
                    }
                }

                if ( !selected )
                {
                    if ( person.GroupTypes.SelectMany( gt => gt.Groups ).Any() )
                    {
                        MobileCheckinMessage( GetAttributeValue( AttributeKeys.AlreadyCheckedIn ), 4 );
                    }
                    else
                    {
                        MobileCheckinMessage( GetAttributeValue( AttributeKeys.NoCheckinAvailable ), 4 );
                    }
                    return;
                }

                test = ProcessActivity( GetAttributeValue( AttributeKeys.SaveAttendanceActivtyName ), out errors );

                LabelPrinter labelPrinter = new LabelPrinter()
                {
                    Request = Request,
                    Labels = person.GroupTypes.Where( gt => gt.Labels != null ).SelectMany( gt => gt.Labels ).ToList()
                };

                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
                MobileCheckinMessage( GetAttributeValue( AttributeKeys.FastPassComplete ), 2 );
            }
            catch ( Exception e )
            {
                LogException( e );
            }
        }

        private void MobileCheckin( string accessKey )
        {
            var mobileDidAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_DID_ATTEND ).Id;
            var mobileNotAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND ).Id;

            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );

            var mobileCheckinRecord = mobileCheckinRecordService.Queryable().Where( r => r.AccessKey == accessKey ).FirstOrDefault();

            if ( mobileCheckinRecord == null )
            {
                MobileCheckinMessage( GetAttributeValue( AttributeKeys.NoMobileCheckinRecord ) );
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Canceled )
            {
                MobileCheckinMessage( GetAttributeValue( AttributeKeys.ExpiredMobileCheckinRecord ) );
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Complete )
            {
                MobileCheckinMessage( GetAttributeValue( AttributeKeys.AlreadyCompleteCheckinRecord ) );
                return;
            }

            try
            {
                if ( mobileCheckinRecord == null )
                {
                    return;
                }

                if ( KioskType.CampusId.HasValue && KioskType.CampusId != 0 && KioskType.CampusId != mobileCheckinRecord.CampusId )
                {
                    ShowWrongCampusSign( mobileCheckinRecord.Campus.Name, KioskType.Campus.Name );
                    return;
                }

                List<CheckInLabel> labels = null;

                if ( mobileCheckinRecord.Attendances.Any( a => a.EndDateTime != null ) )
                {
                    var people = mobileCheckinRecord.Attendances.Select( a => a.PersonAlias.Person ).DistinctBy( p => p.Id ).ToList();
                    labels = CheckinLabelGen.GenerateLabels( people, CurrentCheckInState.Kiosk.Device, GetAttributeValue( "AggregatedLabel" ).AsGuidOrNull() );
                }
                else
                {
                    labels = JsonConvert.DeserializeObject<List<CheckInLabel>>( mobileCheckinRecord.SerializedCheckInState );
                }

                LabelPrinter labelPrinter = new LabelPrinter()
                {
                    Request = Request,
                    Labels = labels
                };

                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    if ( attendance.QualifierValueId == mobileDidAttendId )
                    {
                        attendance.DidAttend = true;
                        attendance.QualifierValueId = null;
                        attendance.StartDateTime = Rock.RockDateTime.Now;
                    }
                    else if ( attendance.QualifierValueId == mobileNotAttendId )
                    {
                        attendance.DidAttend = false;
                        attendance.QualifierValueId = null;
                    }
                    attendance.Note = "Completed mobile check-in at: " + CurrentCheckInState.Kiosk.Device.Name;
                }

                mobileCheckinRecord.Status = MobileCheckinStatus.Complete;

                rockContext.SaveChanges();

                //wait until we successfully save to update cache
                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    AttendanceCache.AddOrUpdate( attendance );

                }
                MobileCheckinRecordCache.Update( mobileCheckinRecord.Id );
                MobileCheckinMessage( GetAttributeValue( AttributeKeys.CompletingMobileCheckin ), 5 );

                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
            }
            catch ( Exception e )
            {
                LogException( new Exception( "There was an issue completing a mobile check-in record. See Inner Exception for details", e ) );
            }
        }



        private void MobileCheckinMessage( string message, int secondsOpen = 10 )
        {
            message = message.RemoveCrLf();
            secondsOpen = secondsOpen * 1000;
            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "ShowMobileMessage", "showMobileDialog('" + message + "'," + secondsOpen.ToString() + ");", true );
        }

        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "ReRunSearch", "doSearch(true)", true );
        }
    }
}