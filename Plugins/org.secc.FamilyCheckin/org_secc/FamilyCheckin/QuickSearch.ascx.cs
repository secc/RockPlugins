
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
    [IntegerField( "Minimum Phone Number Length", "Minimum length for phone number searches (defaults to 4).", false, 4 )]
    [IntegerField( "Maximum Phone Number Length", "Maximum length for phone number searches (defaults to 10).", false, 10 )]
    [TextField("Search Regex", "Regular Expression to run the search input through before sending it to the workflow. Useful for stripping off characters.", false)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.CHECKIN_SEARCH_TYPE, "Search Type", "The type of search to use for check-in (default is phone number).", true, false, Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER, order: 4 )]
    public partial class QuickSearch : CheckInBlock
    {

        protected int minLength;
        protected int maxLength;



        protected override void OnInit( EventArgs e )
        {

            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/checkin-core.js" );

            if (!KioskCurrentlyActive)
            {
                NavigateToHomePage();
            }
        }

        protected override void OnLoad( EventArgs e )
        {           
            base.OnLoad( e );
            //if (Request.QueryString["test"] != null)
            //{
            //string json = "{\"name\":\"Joe\"}";
            //Response.Clear();
            //Response.ContentType = "application/json; charset=utf-8";
            //Response.Write(json);
            //Response.End();
            //}

            minLength = int.Parse(GetAttributeValue("MinimumPhoneNumberLength"));
            maxLength = int.Parse(GetAttributeValue("MaximumPhoneNumberLength"));

            if ( !Page.IsPostBack )
            {
                this.Page.Form.DefaultButton = lbSearch.UniqueID;

                // set search type
                var searchTypeValue = GetAttributeValue( "SearchType" ).AsGuid();
                if ( searchTypeValue == Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER.AsGuid() )
                {
                    pnlSearchName.Visible = false;
                    pnlSearchPhone.Visible = true;
                    lPageTitle.Text = "Search By Phone";
                }
                else if ( searchTypeValue == Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME.AsGuid() )
                {
                    pnlSearchName.Visible = true;
                    pnlSearchPhone.Visible = false;
                    lPageTitle.Text = "Search By Name";
                }
                else
                {
                    pnlSearchName.Visible = true;
                    pnlSearchPhone.Visible = false;
                    txtName.Label = "Name or Phone";
                    lPageTitle.Text = "Search By Name or Phone";
                }
            }
        }

        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if (KioskCurrentlyActive)
            {
                int minLength = int.Parse(GetAttributeValue("MinimumPhoneNumberLength"));
                int maxLength = int.Parse(GetAttributeValue("MaximumPhoneNumberLength"));
                if (tbPhone.Text.Length >= minLength && tbPhone.Text.Length <= maxLength)
                {
                    string searchInput = tbPhone.Text;

                    // run regex expression on input if provided
                    if (!string.IsNullOrWhiteSpace(GetAttributeValue("SearchRegex")))
                    {
                        Regex regex = new Regex(GetAttributeValue("SearchRegex"));
                        Match match = regex.Match(searchInput);
                        if (match.Success)
                        {
                            if (match.Groups.Count == 2)
                            {
                                searchInput = match.Groups[1].ToString();
                            }
                        }
                    }
                    CurrentCheckInState.CheckIn.UserEnteredSearch = true;
                    CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;
                    CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read(Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER);
                    CurrentCheckInState.CheckIn.SearchValue = searchInput;
                    List<CheckInFamily> families = CurrentCheckInState.CheckIn.Families;

                    //Activate workflow to find our families who match 
                    string workflowActivity = GetAttributeValue("WorkflowActivity");
                    List<string> errors;
                    ProcessActivity(workflowActivity, out errors);
                        if (families.Count > 0)
                    {
                        //DisplayFamilies(families);
                    }
                    //ProcessSelection();
                }
            }
        }


        private void BbFamily_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }



        protected void lbBack_Click( object sender, EventArgs e )
        {
            CancelCheckin();
        }

        protected void lbCancel_Click( object sender, EventArgs e )
        {
            CancelCheckin();
        }
    }
}