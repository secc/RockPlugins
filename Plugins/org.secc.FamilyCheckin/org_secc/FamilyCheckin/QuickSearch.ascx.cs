
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName("QuickSearch")]
    [Category("Check-in")]
    [Description("QuickSearch block for helping parents find their family quickly.")]
    [IntegerField("Minimum Phone Number Length", "Minimum length for phone number searches (defaults to 4).", false, 4)]
    [IntegerField("Maximum Phone Number Length", "Maximum length for phone number searches (defaults to 10).", false, 10)]
    [IntegerField("Refresh Interval", "How often (seconds) should page automatically query server for new Check-in data", false, 10)]
    [TextField("Search Regex", "Regular Expression to run the search input through before sending it to the workflow. Useful for stripping off characters.", false)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.CHECKIN_SEARCH_TYPE, "Search Type", "The type of search to use for check-in (default is phone number).", true, false, Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER, order: 4)]
    public partial class QuickSearch : CheckInBlock
    {

        protected int minLength;
        protected int maxLength;



        protected override void OnInit(EventArgs e)
        {

            base.OnInit(e);

            //RockPage.AddScriptLink("~/Scripts/iscroll.js");
            //RockPage.AddScriptLink("~/Scripts/CheckinClient/checkin-core.js");

            if (CurrentCheckInState == null)
            {
                NavigateToPreviousPage();
                return;
            }

            RockPage.AddScriptLink("~/scripts/jquery.plugin.min.js");
            RockPage.AddScriptLink("~/scripts/jquery.countdown.min.js");

            RegisterScript();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            minLength = int.Parse(GetAttributeValue("MinimumPhoneNumberLength"));
            maxLength = int.Parse(GetAttributeValue("MaximumPhoneNumberLength"));

            if (Request["__EVENTTARGET"] == "ChooseFamily")
            {
                ChooseFamily(Request["__EVENTARGUMENT"]);
            }

            if (!Page.IsPostBack && CurrentCheckInState != null)
            {
                string script = string.Format(@"
    <script>
        $(document).ready(function (e) {{
            if (localStorage) {{
                localStorage.theme = '{0}'
                localStorage.checkInKiosk = '{1}';
                localStorage.checkInGroupTypes = '{2}';
            }}
        }});
    </script>
", CurrentTheme, CurrentKioskId, CurrentGroupTypeIds.AsDelimited(","));
                    phScript.Controls.Add(new LiteralControl(script));


                CurrentWorkflow = null;
                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();
                Session["BlockGuid"] = BlockCache.Guid;
                RefreshView();
            }
        }

        private void ChooseFamily(string familyIdAsString)
        {
            int familyId = Int32.Parse(familyIdAsString);
            CurrentCheckInState = (CheckInState)Session["CheckInState"];
            ClearSelection();
            CheckInFamily selectedFamily = CurrentCheckInState.CheckIn.Families.FirstOrDefault(f => f.Group.Id == familyId);
            if (selectedFamily != null)
            {
                try
                {
                    //clear QCPeople session object and get it ready for quick checkin.
                    Session.Remove("qcPeople");
                }
                catch { } 
                selectedFamily.Selected = true;
                SaveState();
                NavigateToNextPage();
            }
        }


        private void ClearSelection()
        {
            foreach (var family in CurrentCheckInState.CheckIn.Families)
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
        protected void lbRefresh_Click(object sender, EventArgs e)
        {
            RefreshView();
        }


        /// <summary>
        /// Refreshes the view.
        /// </summary>
        private void RefreshView()
        {
            hfRefreshTimerSeconds.Value = GetAttributeValue("RefreshInterval");
            pnlNotActive.Visible = false;
            pnlNotActiveYet.Visible = false;
            pnlClosed.Visible = false;
            pnlActive.Visible = false;
            ManagerLoggedIn = false;
            lblActiveWhen.Text = string.Empty;

            if (CurrentCheckInState == null)
            {
                NavigateToPreviousPage();
                return;
            }

            if (CurrentCheckInState.Kiosk.FilteredGroupTypes(CurrentCheckInState.ConfiguredGroupTypes).Count == 0)
            {
                pnlNotActive.Visible = true;
            }
            else if (!CurrentCheckInState.Kiosk.HasLocations(CurrentCheckInState.ConfiguredGroupTypes))
            {
                DateTime activeAt = CurrentCheckInState.Kiosk.FilteredGroupTypes(CurrentCheckInState.ConfiguredGroupTypes).Select(g => g.NextActiveTime).Min();
                lblActiveWhen.Text = activeAt.ToString("o");
                pnlNotActiveYet.Visible = true;
            }
            else if (!CurrentCheckInState.Kiosk.HasActiveLocations(CurrentCheckInState.ConfiguredGroupTypes))
            {
                pnlClosed.Visible = true;
            }
            else
            {
                pnlActive.Visible = true;
            }
        }
        
        /// <summary>
        /// Registers the script.
        /// </summary>
        private void RegisterScript()
        {
            // Note: the OnExpiry property of the countdown jquery plugin seems to add a new callback
            // everytime the setting is set which is why the clearCountdown method is used to prevent 
            // a plethora of partial postbacks occurring when the countdown expires.
            string script = string.Format(@"

var timeoutSeconds = $('.js-refresh-timer-seconds').val();
if (timeout) {{
    window.clearTimeout(timeout);
}}
var timeout = window.setTimeout(refreshKiosk, timeoutSeconds * 1000);

var $ActiveWhen = $('.active-when');
var $CountdownTimer = $('.countdown-timer');

function refreshKiosk() {{
    window.clearTimeout(timeout);
    if ($('input[id$= \'tbPhone\']').value.lenght>3){{
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
    $CountdownTimer.countdown({{
        until: timeActive, 
        compact:true, 
        onExpiry: clearCountdown
    }});
}}

", this.Page.ClientScript.GetPostBackEventReference(lbRefresh, ""));
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "RefreshScript", script, true);
        }
    }
}