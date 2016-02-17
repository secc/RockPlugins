
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

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName("QuickSearch")]
    [Category("Check-in")]
    [Description("QuickSearch block for helping parents find their family quickly.")]
    [IntegerField("Minimum Phone Number Length", "Minimum length for phone number searches (defaults to 4).", false, 4)]
    [IntegerField("Maximum Phone Number Length", "Maximum length for phone number searches (defaults to 10).", false, 10)]
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

            if (!KioskCurrentlyActive)
            {
                NavigateToHomePage();
            }
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

            if (!Page.IsPostBack)
            {
                SaveState();
                Session["BlockGuid"] = BlockCache.Guid;
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
    }
}