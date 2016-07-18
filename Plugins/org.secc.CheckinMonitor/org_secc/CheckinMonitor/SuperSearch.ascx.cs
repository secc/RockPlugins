using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Super Search" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays keypad for searching on phone numbers." )]
    [BooleanField( "Add Family Option", "Should the option to add a new family be available after search?", true )]
    public partial class SuperSearch : CheckInBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                this.Page.Form.DefaultButton = lbSearch.UniqueID;
                CurrentWorkflow = null;
                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();
            }
            else
            {
                DisplayFamilies();
            }
        }

        protected void lbSearch_Click( object sender, EventArgs e )
        {
            Search();

        }

        private void Search()
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            CurrentCheckInState.CheckIn.Families = new List<CheckInFamily>();

            string searchInput = tbPhone.Text.Trim();
            if ( string.IsNullOrWhiteSpace( searchInput ) )
            {
                return;
            }

            CurrentCheckInState.CheckIn.SearchValue = searchInput;

            if ( Regex.IsMatch( searchInput, @"^\d+$" ) )
            {
                CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );
            }
            else
            {
                CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_NAME );
            }


            CurrentCheckInState.CheckIn.UserEnteredSearch = true;
            CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;
            ProcessSelection();
        }

        protected void ProcessSelection()
        {
            List<string> errorMessages;
            ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errorMessages );
            if ( errorMessages.Any() )
            {
                maWarning.Show( "Error processing workflow activity.", Rock.Web.UI.Controls.ModalAlertType.Alert );
                return;
            }

            //sort by last name
            CurrentCheckInState.CheckIn.Families = CurrentCheckInState.CheckIn.Families.OrderBy( f => f.Caption ).ToList();

            DisplayFamilies();
            SaveState();
        }

        private void DisplayFamilies()
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

            phFamilies.Controls.Clear();
            foreach ( var family in CurrentCheckInState.CheckIn.Families )
            {
                BootstrapButton btnFamily = new BootstrapButton();
                btnFamily.CssClass = "btn btn-default btn-block";
                btnFamily.Text = family.Caption + "<br>" + family.SubCaption;
                btnFamily.ID = family.Group.Id.ToString();
                btnFamily.Click += ( s, e ) =>
                 {
                     family.Selected = true;
                     SaveState();
                     NavigateToNextPage();
                 };
                phFamilies.Controls.Add( btnFamily );
            }
            if ( GetAttributeValue( "AddFamilyOption" ).AsBoolean() )
            {
                BootstrapButton btnNewFamily = new BootstrapButton();
                btnNewFamily.CssClass = "btn btn-primary btn-block";
                btnNewFamily.Text = "Add New Family";
                btnNewFamily.ID = "NewFamily";
                btnNewFamily.Click += ( s, e ) =>
                {
                    NavigateToNextPage();
                };
                phFamilies.Controls.Add( btnNewFamily );
            }

        }

        protected void Timer1_Tick( object sender, EventArgs e )
        {
        }
    }
}