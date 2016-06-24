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
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Reflection;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "Group Fitness Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into group fitness." )]
    [TextField( "Checkin Activity", "Activity for completing checkin.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP_MEMBER, "Sessions Attribute", "Select the attribute used to filter by number of sessions.", true, false, order: 3 )]
    [GroupField( "Group Fitness Group", "Group that group fitness members are in. Needed for reading the number of sessions left.", true )]

    public partial class GroupFitness : CheckInBlock
    {

        private RockContext _rockContext;
        private string _groupSessionsKey;

        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
            }

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            _rockContext = new RockContext();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            var groupSessionsAttributeGuid = GetAttributeValue( "SessionsAttribute" ).AsGuid();
            if ( groupSessionsAttributeGuid != Guid.Empty )
            {
                _groupSessionsKey = AttributeCache.Read( groupSessionsAttributeGuid, _rockContext ).Key;
            }

            nbNotOpen.Visible = false;
            btnCheckin.Visible = false;

            if ( !Page.IsPostBack )
            {
                this.Page.Form.DefaultButton = lbSearch.UniqueID;


                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();
                //people can be preselected coming out of the workflow, we want to unselect them
            }
            else
            {
                if ( CurrentCheckInState.CheckIn.CurrentPerson != null )
                {
                    ShowPersonCheckin();
                }
            }
        }



        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            if ( string.IsNullOrWhiteSpace( tbPhone.Text ) )
            { return; }
            CurrentCheckInState.CheckIn.SearchValue = tbPhone.Text;
            CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER ); //this is a lie
            List<string> errors = new List<string>();
            try
            {
                bool test = ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errors );
            }
            catch
            {
                NavigateToPreviousPage();
                Response.End();
                return;
            }
            if ( CurrentCheckInState.CheckIn.CurrentFamily == null
                || CurrentCheckInState.CheckIn.CurrentPerson == null )
            {
                ShowPersonNotFound();
            }
            else
            {
                ShowPersonCheckin();
            }

        }

        private void ShowPersonCheckin()
        {
            pnlNotFound.Visible = false;
            pnlSearch.Visible = false;
            pnlCheckin.Visible = true;
            var person = CurrentCheckInState.CheckIn.CurrentPerson;
            ltNickName.Text = person.Person.NickName;
            var group = GetMembershipGroup();
            var groupMember = new GroupMemberService( _rockContext ).GetByGroupIdAndPersonId( group.Id, person.Person.Id ).FirstOrDefault();
            if ( groupMember == null )
            {
                ShowPersonNotFound();
                return;
            }
            groupMember.LoadAttributes();
            int sessions = groupMember.GetAttributeValue( _groupSessionsKey ).AsInteger();
            ltSessions.Text = sessions.ToString();

            if (sessions == 0 )
            {
                return;
            }

            //This is a cheat to know if any groups are active and display a no active message if not.
            var checkinCount = 0;

            phClasses.Controls.Clear();

            foreach ( var groupType in person.GroupTypes )
            {
                foreach ( var chGroup in groupType.Groups )
                {
                    foreach ( var location in chGroup.Locations )
                    {
                        foreach ( var schedule in location.Schedules )
                        {
                            Panel pnlClass = new Panel();
                            pnlClass.Style.Add( "margin", "5px" );
                            phClasses.Controls.Add( pnlClass );

                            BootstrapButton btnSelect = new BootstrapButton();
                            if ( schedule.Selected )
                            {
                                btnSelect.Text = "<i class='fa fa-check-square-o'></i>";
                                btnSelect.CssClass = "btn btn-success btn-lg";
                            }
                            else
                            {
                                btnSelect.Text = "<i class='fa fa-square-o'></i>";
                                btnSelect.CssClass = "btn btn-default btn-lg";
                            }
                            btnSelect.ID = "s" + chGroup.Group.Id.ToString() + location.Location.Id.ToString() + schedule.Schedule.Id.ToString();
                            btnSelect.Click += ( s, e ) => { ToggleClass( chGroup, location, schedule, schedule.Selected ); };
                            pnlClass.Controls.Add( btnSelect );

                            Literal ltClassName = new Literal();
                            ltClassName.Text = " " + chGroup.Group.Name + " @ " + schedule.Schedule.Name + " in " + location.Location.Name;
                            pnlClass.Controls.Add( ltClassName );
                            checkinCount++;
                        }
                    }
                }
            }
            if ( checkinCount == 0 )
            {
                nbNotOpen.Visible = true;
            }
        }

        private void ToggleClass( CheckInGroup chGroup, CheckInLocation chLocation, CheckInSchedule chSchedule, bool alreadySelected )
        {
            foreach ( var groupType in CurrentCheckInState.CheckIn.CurrentPerson.GroupTypes )
            {
                groupType.Selected = true;
                foreach ( var group in groupType.Groups )
                {
                    group.Selected = false;
                    foreach ( var location in group.Locations )
                    {
                        location.Selected = false;
                        foreach ( var schedule in location.Schedules )
                        {
                            schedule.Selected = false;
                        }
                    }
                }
            }
            if ( !alreadySelected )
            {
                chGroup.Selected = true;
                chLocation.Selected = true;
                chSchedule.Selected = true;
                btnCheckin.Visible = true;
            }
            SaveState();
            ShowPersonCheckin();
        }

        private void ShowPersonNotFound()
        {
            pnlCheckin.Visible = false;
            pnlSearch.Visible = false;
            pnlNotFound.Visible = true;
        }

        protected void Timer1_Tick( object sender, EventArgs e )
        {

        }

        private Rock.Model.Group GetMembershipGroup()
        {
            string membershipGroupGuid = null;
            membershipGroupGuid = GetAttributeValue( "GroupFitnessGroup" );
            if ( string.IsNullOrWhiteSpace( membershipGroupGuid ) )
            {
                return null;
            }
            var membershipGroup = new GroupService( _rockContext ).Get( membershipGroupGuid.AsGuid() );
            return membershipGroup;
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            tbPhone.Text = "";
            CurrentCheckInState.CheckIn = new CheckInStatus();
            SaveState();
            pnlDone.Visible = false;
            pnlCheckin.Visible = false;
            pnlNotFound.Visible = false;
            pnlSearch.Visible = true;
        }

        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            var checkinAction = GetAttributeValue( "CheckinActivity" );
            List<string> Errors;
            ProcessActivity( checkinAction, out Errors );
            ProcessLabels();
            pnlCheckin.Visible = false;
            pnlDone.Visible = true;
        }
        private void ProcessLabels()
        {
            LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
            labelPrinter.PrintNetworkLabels();
            var script = labelPrinter.GetClientScript();
            script += "setTimeout( function(){ __doPostBack( '" + btnDone.UniqueID + "', 'OnClick' ); },6000)";
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }
    }
}