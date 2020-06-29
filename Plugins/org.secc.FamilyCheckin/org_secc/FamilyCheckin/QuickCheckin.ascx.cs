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
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Exceptions;
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
    [DisplayName( "QuickCheckin" )]
    [Category( "SECC > Check-in" )]
    [Description( "QuickCheckin block for helping parents check in their family quickly." )]

    [TextField( "Complete Checkin Activity Name", "Workflow activity to run when the user completes check-in.", true, "Save Attendance", key: "CheckinActivity" )]
    [BooleanField( "Is Mobile Checkin", "If this block is used for mobile check-in set true", false, key: "IsMobileCheckin" )]

    [CodeEditorField( "Completion HTML",
        "Text that appears when someone completes their check-in proccess  <span class='tip tip-html'></span>", CodeEditorMode.Html,
        key: AttributeKeys.CompletionHTML,
        defaultValue: "<h2>Welcome.</h2> <h2>We are preparing your security labels now.</h2>",
        order: 100
        )]

    [CodeEditorField( "No Eligible Family Members",
        "Text that appears when no one in the family is able to check-in.  <span class='tip tip-html'></span>", CodeEditorMode.Html,
        key: AttributeKeys.NoneFound,
        defaultValue: "<h2>We are sorry</h2><h3>There are no members of your family who are able to check-in at this kiosk right now.</h3><h4>Check-in may become available for your family members at a future time today.<br />If you need assistance or believe this is in error, please contact one of our volunteers.</h4>",
        order: 101
        )]


    public partial class QuickCheckin : CheckInBlock
    {
        protected static class AttributeKeys
        {
            internal const string CompletionHTML = "CompletionHTML";
            internal const string NoneFound = "NoneFound";
        }

        #region Properties
        public List<int> SelectedSchedules
        {
            get
            {
                var selectedSchedules = Session["SelectedSchedules"];
                if ( selectedSchedules != null )
                {
                    return ( List<int> ) selectedSchedules;
                }
                return new List<int>();
            }
            set
            {
                Session["SelectedSchedules"] = value;
            }
        }
        #endregion

        #region PageCycle

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            mdChoose.Header.Visible = false;
            mdChoose.Footer.Visible = false;
            mdAddPerson.Header.Visible = false;
            mdAddPerson.Footer.Visible = false;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on load." ) );
                NavigateToPreviousPage();
                return;
            }

            CurrentCheckInState.CheckinTypeId = LocalDeviceConfig.CurrentCheckinTypeId;

            if ( !Page.IsPostBack )
            {
                ltCompletion.Text = GetAttributeValue( AttributeKeys.CompletionHTML );
                ltNoneFound.Text = GetAttributeValue( AttributeKeys.NoneFound );

                //Clear UI state from session
                Session["modalActive"] = false;
                if ( Session["modalActive"] != null )
                {
                    Session.Remove( "modalPerson" );
                }
                if ( Session["modalPerson"] != null )
                {
                    Session.Remove( "modalSchedule" );
                }

                List<string> errors = new List<string>();
                string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                try
                {
                    //Sometimes this blows up if the session state is lost
                    bool test = ProcessActivity( workflowActivity, out errors );
                }
                catch ( Exception ex )
                {
                    LogException( ex );
                    NavigateToPreviousPage();
                    Response.End();
                    return;
                }

                UpdateSelectedSchedules();
                var schedules = GetSchedules();

                if ( !schedules.Any() )
                {
                    pnlMain.Visible = false;
                    pnlNoCheckin.Visible = true;
                    var script = "setTimeout( function(){ __doPostBack( '" + btnCancel.UniqueID + "', 'OnClick' ); },15000);";
                    ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "goBack", script, true );
                    return;
                }

                if ( schedules.Count() > 1 )
                {
                    Session["QuickCheckinState"] = QuickCheckinState.Schedule;
                }
                else
                {
                    btnCheckinHeader.Text = schedules.FirstOrDefault().Schedule.Name;
                    Session["QuickCheckinState"] = QuickCheckinState.People;
                }

                if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
                {
                    NavigateToPreviousPage();
                    return;
                }

                foreach ( var person in CurrentCheckInState.CheckIn.CurrentFamily.People )
                {
                    person.Selected = false;
                    SaveState();
                }
            }

            QuickCheckinState quickCheckinState;

            if ( Session["QuickCheckinState"] != null && Session["QuickCheckinState"] is QuickCheckinState )
            {
                quickCheckinState = ( QuickCheckinState ) Session["QuickCheckinState"];
            }
            else
            {
                pnlMain.Visible = false;
                pnlNoCheckin.Visible = true;
                return;
            }

            switch ( quickCheckinState )
            {
                case QuickCheckinState.Schedule:
                    DisplayServiceOptions();
                    break;
                case QuickCheckinState.People:
                    lStyle.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                    DisplayPeople();
                    break;
                case QuickCheckinState.Checkin:
                    break;
                default:
                    break;
            }

            if ( Session["modalActive"] != null && ( bool ) Session["modalActive"] )
            {
                if ( Session["modalPerson"] != null && Session["modalSchedule"] != null )
                {
                    ShowRoomChangeModal( ( CheckInPerson ) Session["modalPerson"], ( CheckInSchedule ) Session["modalSchedule"] );
                }
                else
                {
                    ShowAddPersonModal();
                }

            }
        }

        #endregion

        #region Draw Logic


        /// <summary>
        /// Displays the screen which give the schedual options to select from
        /// If there is only one it automatically switches to just showing the check-in screen
        /// </summary>
        private void DisplayServiceOptions()
        {
            btnInterfaceCheckin.Visible = false;
            var schedules = GetSchedules();
            phServices.Controls.Clear();
            var selectedSchedules = SelectedSchedules;
            foreach ( var schedule in schedules )
            {
                BootstrapButton btnActive = new BootstrapButton();
                if ( selectedSchedules.Contains( schedule.Schedule.Id ) )
                {
                    btnActive.CssClass = "btn btn-block btn-selectSchedule btn-selectSchedule-active";
                    btnActive.Text = string.Format( "<div class='row'><div class='col-xs-2'><i class='fa fa-check-square-o'></i></div><div class='col-xs-10'>{0}</div></div>", schedule.Schedule.Name );
                }
                else
                {
                    btnActive.CssClass = "btn  btn-block btn-selectSchedule";
                    btnActive.Text = string.Format( "<div class='row'><div class='col-xs-2'><i class='fa fa-square-o'></i></div><div class='col-xs-10'>{0}</div></div>", schedule.Schedule.Name );
                }
                btnActive.Click += ( s, e ) =>
                {
                    ToggleSchedule( schedule );
                    DisplayServiceOptions();
                };
                btnActive.ID = "btnSelectSchedule" + schedule.Schedule.Id.ToString();
                btnActive.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i>";
                phServices.Controls.Add( btnActive );
            }

            if ( selectedSchedules.Any() )
            {
                BootstrapButton btnContinue = new BootstrapButton();
                btnContinue.CssClass = "btn btn-block btn-selectScheduleNext";
                btnContinue.Text = "Next";
                btnContinue.Click += ( s, e ) =>
                {
                    if ( selectedSchedules.Count() == 1 )
                    {
                        btnCheckinHeader.Text = schedules.Where( sc => sc.Schedule.Id == selectedSchedules[0] ).FirstOrDefault().Schedule.Name;
                    }
                    else
                    {
                        btnCheckinHeader.Text = "Check-in";
                    }

                    //deselect any selected unselected schedules
                    var unselectedSchedules = CurrentCheckInState
                         .CheckIn
                         .CurrentFamily
                         .People
                         .SelectMany( p => p.GroupTypes )
                         .SelectMany( gt => gt.Groups )
                         .SelectMany( g => g.Locations )
                         .SelectMany( l => l.Schedules.Where( sc => sc.Selected && !selectedSchedules.Contains( sc.Schedule.Id ) ) )
                         .ToList();
                    foreach ( var unselectedSchedule in unselectedSchedules )
                    {
                        unselectedSchedule.Selected = false;
                    }

                    SaveState();

                    Session["QuickCheckinState"] = QuickCheckinState.People;
                    DisplayServiceOptions();
                    DisplayPeople();
                    ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "transition", "setTimeout(function(){showContent()},50);", true );
                };
                btnContinue.ID = "btnScheduleNext";
                btnContinue.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i>";
                phServices.Controls.Add( btnContinue );
            }
        }

        private void DisplayPeople()
        {
            phPeople.Controls.Clear();
            if ( CurrentCheckInState == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on displaying people" ) );
                NavigateToPreviousPage();
                return;
            }

            var people = CurrentCheckInState.CheckIn.Families
                .SelectMany( f => f.People )
                .OrderBy( p => p.Person.BirthDate );

            //If all the people are can check-in relationships, only show them
            if ( !people.Where( p => p.FamilyMember ).Any() )
            {
                foreach ( var person in people )
                {
                    person.FamilyMember = true;
                }
                SaveState();
            }

            btnAddPerson.Visible = people.Where( p => !p.FamilyMember ).Any();

            int i = 0;
            Panel hgcRow = new Panel();

            foreach ( var checkinPerson in people.Where( p => p.FamilyMember ) )
            {
                //Unselect person if no groups selected
                if ( checkinPerson.Selected && !PersonHasSelectedOption( checkinPerson ) )
                {
                    checkinPerson.Selected = false;
                    SaveState();
                }
                //Display person checkin information

                i++;

                if ( i % 2 > 0 )
                {
                    hgcRow = new Panel();
                    hgcRow.ID = checkinPerson.Person.Id.ToString() + "_hgcRow";
                    phPeople.Controls.Add( hgcRow );
                    hgcRow.AddCssClass( "row" );
                }

                Panel hgcPadding = new Panel();
                hgcPadding.ID = checkinPerson.Person.Id.ToString() + "_hgcPadding";
                hgcPadding.AddCssClass( "col-xs-12 col-lg-6" );
                hgcRow.Controls.Add( hgcPadding );


                if ( GetCheckinSchedules( checkinPerson ).Count() > 0
                    && PersonHasCheckinAvailable( checkinPerson ) )
                { //Display check-in information
                    Panel hgcCell = new Panel();
                    hgcCell.ID = checkinPerson.Person.Id.ToString() + "hgcCell";
                    hgcCell.AddCssClass( "personContainer col-xs-12" );
                    hgcPadding.Controls.Add( hgcCell );

                    DisplayPersonButton( checkinPerson, hgcCell );
                    DisplayPersonCheckinAreas( checkinPerson, hgcCell );
                }
                else
                {   //Display can't check in information
                    Panel hgcCell = new Panel();
                    hgcCell.ID = checkinPerson.Person.Id.ToString() + "hgcCell_noOption";
                    hgcCell.AddCssClass( "personContainer col-xs-12" );
                    hgcPadding.Controls.Add( hgcCell );
                    DisplayPersonNoOptions( checkinPerson, hgcCell );
                }
            }
            if ( people.Where( p => p.Selected ).Any() )
            {
                btnInterfaceCheckin.Visible = true;
            }
            else
            {
                btnInterfaceCheckin.Visible = false;
            }
        }

        /// <summary>
        /// Draws the checkin areas for each schedule
        /// </summary>
        /// <param name="person"></param>
        /// <param name="hgcRow"></param>
        private void DisplayPersonCheckinAreas( CheckInPerson checkInPerson, Panel hgcRow )
        {
            List<CheckInSchedule> personSchedules = GetCheckinSchedules( checkInPerson );

            foreach ( var schedule in personSchedules )
            {
                Panel hgcAreaRow = new Panel();
                hgcAreaRow.CssClass = "g-padding-x-20--xs";
                hgcRow.Controls.Add( hgcAreaRow );
                hgcAreaRow.ID = checkInPerson.Person.Id.ToString() + schedule.Schedule.Id.ToString();

                hgcRow.AddCssClass( "row col-xs-12" );
                DisplayPersonSchedule( checkInPerson, schedule, hgcAreaRow );
            }
        }

        /// <summary>
        /// Draws the person entry if they have no available options.
        /// </summary>
        /// <param name="person"></param>
        /// <param name="hgcCell"></param>
        private void DisplayPersonNoOptions( CheckInPerson person, Panel hgcCell )
        {
            //Padding div to make it look nice.
            Panel hgcPadding = new Panel();
            hgcPadding.AddCssClass( "col-sm-4 col-xs-12 g-padding-x-20--xs g-margin-b-20--xs" );
            hgcCell.Controls.Add( hgcPadding );

            //Checkin Button
            var btnPerson = new Label();
            btnPerson.ID = person.Person.Guid.ToString();
            btnPerson.Enabled = false;
            hgcPadding.Controls.Add( btnPerson );

            var icon = "<i class='fa fa-user fa-5x'></i>";

            if ( person.Person.DaysToBirthday < 8 )
            {
                icon = "<i class='fa fa-birthday-cake fa-5x'></i>";
            }
            btnPerson.Text = icon + "<br/><span>" + person.Person.NickName + "</span>";
            btnPerson.CssClass = "btn btn-default btn-lg col-xs-12 disabled checkinPerson";

            Panel hgcAreaRow = new Panel();
            hgcAreaRow.ID = person.Person.Id.ToString() + "_noOptionHGCAreaRow";
            hgcAreaRow.CssClass = "g-padding-x-20--xs";
            hgcCell.AddCssClass( "row col-xs-12" );
            hgcCell.Controls.Add( hgcAreaRow );
            var btnMessage = new Label();
            btnMessage.ID = person.Person.Id.ToString() + "_noOptionButton";
            btnMessage.AddCssClass( "btn btn-default col-xs-8 disabled" );
            hgcAreaRow.Controls.Add( btnMessage );

            btnMessage.Text = "There are no classes available for " + person.Person.NickName + "<br> to check-in, or all rooms are currently full.";
            foreach ( var locationId in person.GroupTypes.SelectMany( gt => gt.Groups ).SelectMany( g => g.Locations ).Select( l => l.Location.Id ).ToList() )
            {
                var attendances = AttendanceCache.GetByLocationId( locationId );
                if ( attendances.Any( a => a.PersonId == person.Person.Id ) )
                {
                    btnMessage.Text = "<span class='center'>" + person.Person.NickName + " has already been checked-in.</span>";
                    btnPerson.Text = "<i class='fa fa-check-square-o fa-5x'></i><br/><span>" + person.Person.NickName + "</span>";
                    btnPerson.CssClass = "btn btn-default btn-lg col-xs-12 disabled checkinPerson";
                    break;
                }
            }
        }

        /// <summary>
        /// Displays the modal for putting can check-in relationships on the check-in page
        /// </summary>
        private void ShowAddPersonModal()
        {
            phAddPerson.Controls.Clear();
            var people = CurrentCheckInState.CheckIn.Families
                .SelectMany( f => f.People )
                .Where( p => !p.FamilyMember )
                .OrderByDescending( p => p.Person.Age );

            if ( !people.Any() )
            {
                Session["modalActive"] = false;
                mdAddPerson.Hide();
                DisplayPeople();
                return;
            }

            foreach ( var person in people )
            {
                Session["modalActive"] = true;
                Panel hgcPadding = new Panel();
                hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                hgcPadding.Style.Add( "padding", "5px" );
                phAddPerson.Controls.Add( hgcPadding );

                //Change room button
                BootstrapButton btnPerson = new BootstrapButton();
                btnPerson.ID = "btnAddPerson" + person.Person.Id.ToString();
                btnPerson.Text = "<span class='center'>" + person.Person.FullName + "</span>";

                btnPerson.CssClass = "btn btn-success btn-block";
                btnPerson.Click += ( s, e ) =>
                {
                    person.FamilyMember = true;
                    SaveState();
                    ShowAddPersonModal();
                };
                btnPerson.DataLoadingText = "<span class='center'><i class='fa fa-refresh fa-spin'></i> Adding: " + person.Person.FullName + "to check-in...</span>";
                hgcPadding.Controls.Add( btnPerson );
            }
            Panel hgcCancelPadding = new Panel();
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add( "padding", "5px" );
            phAddPerson.Controls.Add( hgcCancelPadding );

            BootstrapButton btnDone = new BootstrapButton();
            btnDone.ID = "btnDone";
            btnDone.Text = "<span class='center'>Done</span>";
            btnDone.CssClass = "btn btn-default col-md-8 col-xs-12 btn-block";
            btnDone.Click += ( s, e ) =>
            {
                Session["modalActive"] = false;
                mdAddPerson.Hide();
                DisplayPeople();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Closing...";
            hgcCancelPadding.Controls.Add( btnDone );

            mdAddPerson.Show();
        }

        /// <summary>
        /// Displays the locations for each schedule for a given person
        /// </summary>
        /// <param name="checkInPerson"></param>
        /// <param name="schedule"></param>
        /// <param name="hgcAreaRow"></param>
        private void DisplayPersonSchedule( CheckInPerson checkInPerson, CheckInSchedule schedule, Panel hgcAreaRow )
        {
            BootstrapButton btnSchedule = new BootstrapButton
            {
                Text = schedule.Schedule.Name + "<br>(Select Room To Checkin)",
                CssClass = "btn btn-default col-sm-8 scheduleNotSelected g-margin-b-20--xs",
                ID = checkInPerson.Person.Guid.ToString() + schedule.Schedule.Guid.ToString()
            };

            btnSchedule.Click += ( s, e ) =>
            {
                ShowRoomChangeModal( checkInPerson, schedule );
            };
            btnSchedule.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Loading Rooms...";

            CheckInGroupType groupType = checkInPerson.GroupTypes
                  .FirstOrDefault( gt => gt.Selected && gt.Groups.SelectMany( g => g.Locations ).SelectMany( l => l.Schedules.Where( s => s.Selected ) ).Select( s => s.Schedule.Guid ).Contains( schedule.Schedule.Guid ) == true );

            if ( groupType != null )
            {
                var group = checkInPerson.GroupTypes
                .SelectMany( gt => gt.Groups ).Where( g => g.Selected && g.Locations.Where( l => l.Schedules.Where( s => s.Selected ).Select( s => s.Schedule.Id ).Contains( schedule.Schedule.Id ) && l.Selected ).Any() )
                .FirstOrDefault();

                if ( group != null )
                {
                    var location = checkInPerson.GroupTypes
                        .SelectMany( gt => gt.Groups.Where( g => g.Selected && g.Group.Guid == group.Group.Guid ) )
                        .SelectMany( g => g.Locations.Where( l => l.Schedules.Where( s => s.Selected ).Select( s => s.Schedule.Id ).Contains( schedule.Schedule.Id ) && l.Selected ) )
                        .FirstOrDefault();

                    //If a room is selected
                    if ( location != null )
                    {
                        btnSchedule.CssClass = "btn btn-primary col-md-8 scheduleSelected g-margin-b-20--xs";
                        btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + location;
                    }
                }
            }
            hgcAreaRow.Controls.Add( btnSchedule );
        }

        /// <summary>
        /// Displays the modal for selecting a location for a given schedule
        /// </summary>
        /// <param name="checkinPerson"></param>
        /// <param name="schedule"></param>
        private void ShowRoomChangeModal( CheckInPerson checkinPerson, CheckInSchedule schedule )
        {
            List<CheckInGroupType> groupTypes = GetGroupTypes( checkinPerson, schedule );

            foreach ( var groupType in groupTypes )
            {
                List<CheckInGroup> groups = GetGroups( checkinPerson, schedule, groupType );

                foreach ( var group in groups )
                {
                    List<CheckInLocation> locations = GetLocations( checkinPerson, schedule, groupType, group );
                    foreach ( var location in locations )
                    {
                        if ( !OccurrenceCache.GetVolunteerOccurrences().Any( o => o.GroupId == group.Group.Id ) && !LocationScheduleOkay( location, schedule ) )
                        {
                            continue;
                        }

                        Panel hgcPadding = new Panel();
                        hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                        hgcPadding.Style.Add( "padding", "5px" );
                        phModal.Controls.Add( hgcPadding );

                        //Change room button
                        BootstrapButton btnRoom = new BootstrapButton();
                        btnRoom.ID = "btn" + checkinPerson.Person.Guid.ToString() + group.Group.Guid.ToString() + schedule.Schedule.Guid.ToString() + location.Location.Guid.ToString();
                        btnRoom.Text = groupType.GroupType.Name + ": " + group.Group.Name + "<br>" + location.Location.Name;

                        //Add location count
                        if ( CurrentCheckInType.DisplayLocationCount && !GetAttributeValue( "IsMobile" ).AsBoolean() )
                        {
                            btnRoom.Text += " (Count:" + AttendanceCache.GetByLocationIdAndScheduleId( location.Location.Id, schedule.Schedule.Id ).Where( a => !a.IsVolunteer ).Count().ToString() + ")";
                        }

                        btnRoom.CssClass = "btn btn-success btn-block";
                        btnRoom.Click += ( s, e ) =>
                        {
                            ChangeRoomSelection( checkinPerson, schedule, groupType, group, location );
                            Session["modalActive"] = false;
                            Session.Remove( "modalPerson" );
                            Session.Remove( "modalSchedule" );
                            mdChoose.Hide();
                            phPeople.Controls.Clear();
                            DisplayPeople();
                        };
                        btnRoom.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Changing room to: " + location.Location.Name;
                        hgcPadding.Controls.Add( btnRoom );
                    }
                }
            }
            Panel hgcCancelPadding = new Panel();
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add( "padding", "5px" );
            phModal.Controls.Add( hgcCancelPadding );

            BootstrapButton btnCancel = new BootstrapButton();
            btnCancel.ID = "c" + checkinPerson.Person.Guid.ToString() + schedule.Schedule.Guid.ToString();
            btnCancel.Text = "<span class='center'>(Do not check in at " + schedule.Schedule.Name + ")</span>";
            btnCancel.CssClass = "btn btn-default col-md-8 col-xs-12 btn-block";
            btnCancel.Click += ( s, e ) =>
            {
                ClearRoomSelection( checkinPerson, schedule );
                Session["modalActive"] = false;
                Session.Remove( "modalPerson" );
                Session.Remove( "modalSchedule" );
                mdChoose.Hide();
                phPeople.Controls.Clear();
                DisplayPeople();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Canceling...";
            hgcCancelPadding.Controls.Add( btnCancel );

            mdChoose.Title = "Choose Class";
            mdChoose.CancelLinkVisible = false;
            mdChoose.Show();
            Session["modalActive"] = true;
            Session["modalPerson"] = checkinPerson;
            Session["modalSchedule"] = schedule;
        }

        protected void CancelModal( object sender, EventArgs e )
        {
            Session["modalActive"] = false;
            mdChoose.Hide();
        }

        private void DisplayPersonButton( CheckInPerson person, Panel hgcRow )
        {
            //Padding div to make it look nice.
            Panel hgcPadding = new Panel();
            hgcPadding.AddCssClass( "col-sm-4 col-xs-12 g-padding-x-20--xs g-margin-b-20--xs" );
            hgcRow.Controls.Add( hgcPadding );

            //Checkin Button
            var btnPerson = new BootstrapButton();
            btnPerson.ID = person.Person.Guid.ToString();
            btnPerson.Click += ( s, e ) => { TogglePerson( person ); };
            hgcPadding.Controls.Add( btnPerson );

            var icon = "<i class='fa  fa-check-square-o fa-5x'></i>";

            if ( person.Person.DaysToBirthday < 8 )
            {
                icon = "<i class='fa fa-birthday-cake fa-5x'></i>";
            }

            if ( person.Selected )
            {
                btnPerson.DataLoadingText = icon + "<br /><span>Please Wait...</span>";
                btnPerson.Text = icon + "<br/><span>" + person.Person.NickName + "</span>";
                btnPerson.CssClass = "btn btn-success btn-lg col-xs-12 checkinPerson checkinPersonSelected";
            }
            else
            {
                btnPerson.DataLoadingText = "<i class='fa fa-square-o fa-5x'></i><br /><span> Please Wait...</span>";
                btnPerson.Text = "<i class='fa fa-square-o fa-5x'></i><br/><span>" + person.Person.NickName + "</span>";
                btnPerson.CssClass = "btn btn-default btn-lg col-xs-12 checkinPerson checkinPersonNotSelected";
            }
        }
        #endregion

        #region EventHandlers

        /// <summary>
        /// Adds or removes the schedule to the selected schedules list
        /// </summary>
        /// <param name="schedule"></param>
        private void ToggleSchedule( CheckInSchedule schedule )
        {
            var selectedSchedules = SelectedSchedules;
            if ( selectedSchedules.Contains( schedule.Schedule.Id ) )
            {
                selectedSchedules.Remove( schedule.Schedule.Id );
            }
            else
            {
                selectedSchedules.Add( schedule.Schedule.Id );

                //Remove overlapping schedules
                var otherSchedules = GetSchedules().Where( s => s.Schedule.Id != schedule.Schedule.Id );
                var start = schedule.Schedule.GetCalendarEvent().DTStart;
                var end = schedule.Schedule.GetCalendarEvent().DTEnd;

                foreach ( var otherSchedule in otherSchedules )
                {
                    if ( start.LessThan( otherSchedule.Schedule.GetCalendarEvent().DTEnd )
                        && otherSchedule.Schedule.GetCalendarEvent().DTStart.LessThan( end ) )
                    {
                        if ( selectedSchedules.Contains( otherSchedule.Schedule.Id ) )
                        {
                            selectedSchedules.Remove( otherSchedule.Schedule.Id );
                        }
                    }
                }
            }
            SelectedSchedules = selectedSchedules;
        }

        protected void btnInterfaceCheckin_Click( object sender, EventArgs e )
        {
            if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).SelectMany( f => f.People ).Where( p => p.Selected ).Any() )
            {
                NavigateToNextPage();
                return;
            }

            //Test for overloaded rooms
            var overload = false;
            foreach ( var person in CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Selected ) )
            {
                foreach ( var groupType in person.GroupTypes.Where( gt => gt.Selected ) )
                {
                    foreach ( var group in groupType.Groups.Where( g => g.Selected ) )
                    {
                        foreach ( var location in group.Locations.Where( l => l.Selected ) )
                        {
                            foreach ( var schedule in location.Schedules.Where( s => s.Selected ).ToList() )
                            {
                                var occurrence = OccurrenceCache.Get( group.Group.Id, location.Location.Id, schedule.Schedule.Id );
                                if ( occurrence == null || occurrence.IsFull )
                                {
                                    location.Schedules.Remove( schedule );
                                    overload = true;
                                    continue;
                                }

                                var hasSameScheduleAttendances = AttendanceCache.All()
                                    .Where( a => a.PersonId == person.Person.Id
                                              && a.AttendanceState != AttendanceState.CheckedOut
                                              && a.ScheduleId == schedule.Schedule.Id )
                                    .Any();

                                if ( hasSameScheduleAttendances )
                                {
                                    location.Schedules.Remove( schedule );
                                    overload = true;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
            if ( overload )
            {
                //a room just closed
                maAlert.Show( "We're sorry, but a location that you selected has just reached capacity. That location has been removed from your options, we are sorry for the inconvenience.", ModalAlertType.Alert );
                phPeople.Controls.Clear();
                DisplayPeople();
            }
            else
            {
                //trigger the final checkin process!
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "doCheckin", "doCheckin();", true );
            }
        }

        protected void btnContinue_Click( object sender, EventArgs e )
        {
            //trigger the final checkin process!
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "doCheckin", "doCheckin();", true );
        }

        /// <summary>
        /// Handles the click for adding a guest to the main screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void addPerson_Click( object sender, EventArgs e )
        {
            ShowAddPersonModal();
        }

        /// <summary>
        /// Change back to being able to select a different time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCheckinHeader_Click( object sender, EventArgs e )
        {
            Session["QuickCheckinState"] = QuickCheckinState.Schedule;
            lStyle.Text = "";
            DisplayServiceOptions();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToNextPage();
        }

        protected void btnCheckin_Click( object sender, EventArgs e )
        {
            if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).SelectMany( f => f.People ).Where( p => p.Selected ).Any() )
            {
                NavigateToNextPage();
                return;
            }

            //Check-in and print tags.
            List<string> errors = new List<string>();
            try
            {
                var activityName = GetAttributeValue( "CheckinActivity" );
                bool test = ProcessActivity( activityName, out errors );
            }
            catch ( Exception ex )
            {
                LogException( ex );
                NavigateToHomePage();
                return;
            }

            if ( GetAttributeValue( "IsMobileCheckin" ).AsBoolean() )
            {
                NavigateToHomePage();
            }
            else
            {
                ProcessLabels();
                pnlMain.Visible = false;
            }
        }

        protected void btnNoCheckin_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }


        #endregion

        #region Helper Methods
        /// <summary>
        /// Looks at the currently selected schedules and sets a session variable of the selected schedule Ids 
        /// </summary>
        private void UpdateSelectedSchedules()
        {
            if ( CurrentCheckInState == null
                || CurrentCheckInState.CheckIn == null
                || CurrentCheckInState.CheckIn.CurrentFamily == null
                || CurrentCheckInState.CheckIn.CurrentFamily.People == null )
            {
                NavigateToPreviousPage();
                return;
            }

            var schedules =
                CurrentCheckInState
                 .CheckIn
                 .CurrentFamily
                 .People
                 .SelectMany( p => p.GroupTypes )
                 .SelectMany( gt => gt.Groups )
                 .SelectMany( g => g.Locations )
                 .SelectMany( l => l.Schedules )
                 .OrderBy( s => s.Schedule.StartTimeOfDay )
                 .ToList();

            //if no schedule is selected lets go ahead and auto select the first one...
            if ( schedules.Any() && !schedules.Where( s => s.Selected ).Any() )
            {
                schedules.FirstOrDefault().Selected = true;
            }

            var selectedSchedules = new List<CheckInSchedule>();
            foreach ( var s in schedules.Where( _s => _s.Selected ).DistinctBy( _s => _s.Schedule.Id ).ToList() )
            {
                if ( DoesNotOverlap( s, selectedSchedules ) )
                {
                    selectedSchedules.Add( s );
                }
            }

            SelectedSchedules = selectedSchedules.Select( s => s.Schedule.Id ).ToList();
        }


        /// <summary>
        /// Tests to see if schedule overlaps any other selected schedule
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="selectedSchedules"></param>
        /// <returns></returns>
        private bool DoesNotOverlap( CheckInSchedule schedule, List<CheckInSchedule> selectedSchedules )
        {
            var start = schedule.Schedule.GetCalendarEvent().DTStart;
            var end = schedule.Schedule.GetCalendarEvent().DTEnd;

            foreach ( var otherSchedule in selectedSchedules )
            {
                if ( start.LessThan( otherSchedule.Schedule.GetCalendarEvent().DTEnd )
                    && otherSchedule.Schedule.GetCalendarEvent().DTStart.LessThan( end ) )
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets all of the schedules ordered by start time
        /// </summary>
        /// <returns></returns>
        private List<CheckInSchedule> GetSchedules()
        {
            return CurrentCheckInState
                .CheckIn
                .Families
                .SelectMany( f => f.People )
                .SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .DistinctBy( s => s.Schedule.Id )
                .OrderBy( s => s.Schedule.GetNextStartDateTime( RockDateTime.Now ) )
                .ToList();
        }

        /// <summary>
        /// This tests to see if the selected person has a place to check-in.
        /// It checks to see that each schedule has a valid location
        /// Or that the person is a volunteer
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        private bool PersonHasCheckinAvailable( CheckInPerson person )
        {
            var volunteerOccurreneces = OccurrenceCache.GetVolunteerOccurrences();
            var groups = person
                .GroupTypes
                .SelectMany( gt => gt.Groups )
                .ToList();
            foreach ( var group in groups )
            {
                foreach ( var location in group.Locations )
                {
                    foreach ( var schedule in location.Schedules )
                    {
                        if ( volunteerOccurreneces.Any( o => o.GroupId == group.Group.Id ) || LocationScheduleOkay( location, schedule ) )
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private List<CheckInSchedule> GetCheckinSchedules( CheckInPerson checkInPerson )
        {
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];

            return checkInPerson.GroupTypes
            .SelectMany( gt => gt.Groups )
            .SelectMany( g => g.Locations )
            .SelectMany( l => l.Schedules.Where( s => selectedSchedules.Contains( s.Schedule.Id ) ) )
            .OrderBy( s => s.Schedule.StartTimeOfDay )
            .DistinctBy( s => s.Schedule.Id ).ToList();
        }

        /// <summary>
        /// Selects all locations that are the same in a given group
        /// </summary>
        /// <param name="checkInPerson"></param>
        /// <param name="group"></param>
        /// <param name="room"></param>
        private void LinkLocations( CheckInPerson checkInPerson, CheckInGroup group, CheckInLocation room )
        {
            var groupTypes = checkInPerson.GroupTypes;
            foreach ( var groupType in groupTypes.ToList() )
            {
                //Deselect any other grouptype
                if ( !groupType.Groups.Contains( group ) )
                {
                    groupType.Selected = false;
                    groupType.PreSelected = false;
                    foreach ( var checkInGroup in groupType.Groups )
                    {
                        checkInGroup.Selected = false;
                        checkInGroup.PreSelected = false;
                        foreach ( var checkInLocation in checkInGroup.Locations )
                        {
                            checkInLocation.Selected = false;
                            checkInLocation.PreSelected = false;
                            foreach ( var checkInSchedule in checkInLocation.Schedules )
                            {
                                checkInSchedule.Selected = false;
                                checkInSchedule.PreSelected = false;
                            }
                        }
                    }
                    continue;
                }

                groupType.Selected = true;
                groupType.PreSelected = true;
                foreach ( var cGroup in groupType.Groups )
                {
                    if ( cGroup.Group.Id == group.Group.Id )
                    {
                        cGroup.Selected = true;
                        cGroup.PreSelected = true;
                        foreach ( var location in cGroup.Locations )
                        {
                            if ( location.Location.Id == room.Location.Id )
                            {
                                location.Selected = true;
                                location.PreSelected = true;
                                foreach ( var schedule in location.Schedules )
                                {
                                    schedule.Selected = true;
                                    schedule.PreSelected = true;
                                }
                            }
                            else
                            {
                                //Deselect other locations
                                location.Selected = false;
                                location.PreSelected = false;
                                foreach ( var schedule in location.Schedules )
                                {
                                    schedule.Selected = false;
                                    schedule.PreSelected = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        //If this is not the chosen group deselect
                        foreach ( var location in cGroup.Locations )
                        {
                            foreach ( var schedule in location.Schedules )
                            {
                                schedule.Selected = false;
                                schedule.PreSelected = false;
                            }
                            location.Selected = false;
                            location.PreSelected = false;
                        }
                        cGroup.Selected = false;
                        cGroup.PreSelected = false;
                    }
                }
            }
            SaveState();
        }

        /// <summary>
        /// Removes any schedules that overlap the selected schedule
        /// </summary>
        /// <param name="checkInPerson"></param>
        /// <param name="schedule"></param>
        private void RemoveOverlappingSchedules( CheckInPerson checkInPerson, CheckInSchedule schedule )
        {
            var otherSchedules = GetCheckinSchedules( checkInPerson ).Where( s => s.Schedule.Id != schedule.Schedule.Id );
            var start = schedule.Schedule.GetCalendarEvent().DTStart;
            var end = schedule.Schedule.GetCalendarEvent().DTEnd;

            foreach ( var otherSchedule in otherSchedules )
            {
                if ( start.LessThan( otherSchedule.Schedule.GetCalendarEvent().DTEnd )
                    && otherSchedule.Schedule.GetCalendarEvent().DTStart.LessThan( end ) )
                {
                    ClearRoomSelection( checkInPerson, otherSchedule );
                }
            }
        }

        /// <summary>
        /// Clears all room selections from room without clearing pre-selections
        /// </summary>
        /// <param name="person"></param>
        /// <param name="schedule"></param>
        private void ClearRoomSelection( CheckInPerson checkInPerson, CheckInSchedule schedule )
        {
            var locationLinkAttributeKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_LINK_LOCATIONS.AsGuid() ).Key;
            List<CheckInGroupType> groupTypes = GetGroupTypes( checkInPerson, schedule );

            foreach ( var groupType in groupTypes )
            {
                List<CheckInGroup> groups = GetGroups( checkInPerson, schedule, groupType );

                foreach ( var group in groups )
                {
                    List<CheckInLocation> rooms = GetLocations( checkInPerson, schedule, groupType, group );

                    foreach ( var room in rooms )
                    {
                        //Change scheduals in room to not selected
                        foreach ( var roomSchedule in room.Schedules )
                        {
                            if ( roomSchedule.Schedule.Guid == schedule.Schedule.Guid )
                            {
                                roomSchedule.Selected = false;
                                if ( group.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
                                {
                                    room.Selected = false;
                                }
                            }
                        }
                        //Set location as not selected if no schedules selected
                        if ( room.Schedules.Where( s => s.Selected == true ).Count() == 0 )
                        {
                            room.Selected = false;
                        }
                    }
                    //Set group as not selected if no locations selected
                    if ( group.Locations.Where( l => l.Selected == true ).Count() == 0 )
                    {
                        group.Selected = false;
                    }
                }
                //Set group type as not selected if no groups selected
                if ( groupType.Groups.Where( g => g.Selected == true ).Count() == 0 )
                {
                    groupType.Selected = false;
                }
            }
            SaveState();
        }

        private List<CheckInLocation> GetLocations( CheckInPerson checkInPerson, CheckInSchedule schedule, CheckInGroupType groupType, CheckInGroup group )
        {
            var locations = checkInPerson.GroupTypes
                        .Where( gt => gt.GroupType.Guid == groupType.GroupType.Guid )
                        .SelectMany( gt => gt.Groups )
                        .Where( g => g.Group.Guid == group.Group.Guid )
                        .SelectMany( g => g.Locations.Where(
                             l => l.Schedules.Where(
                                 s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ) );

            return locations.ToList();
        }

        private List<CheckInGroup> GetGroups( CheckInPerson checkInPerson, CheckInSchedule schedule, CheckInGroupType groupType )
        {
            return checkInPerson.GroupTypes
                .Where( gt => gt.GroupType.Guid == groupType.GroupType.Guid )
                .SelectMany( gt => gt.Groups )
                .Where( g => g.Locations.Where(
                     l => l.Schedules.Where(
                         s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ).Count() != 0 ).ToList();
        }

        private List<CheckInGroupType> GetGroupTypes( CheckInPerson checkInPerson, CheckInSchedule schedule )
        {
            return checkInPerson
                .GroupTypes
                .Where( gt => gt.Groups.Where( g => g.Locations.Where(
                      l => l.Schedules.Where(
                          s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ).Count() != 0 ).Count() != 0 ).ToList();
        }


        /// <summary>
        /// Changes the selection for a location removing any 
        /// </summary>
        /// <param name="checkInPerson"></param>
        /// <param name="schedule"></param>
        /// <param name="groupType"></param>
        /// <param name="group"></param>
        /// <param name="room"></param>
        private void ChangeRoomSelection( CheckInPerson checkInPerson, CheckInSchedule schedule,
            CheckInGroupType groupType, CheckInGroup group, CheckInLocation room )
        {
            ClearRoomSelection( checkInPerson, schedule );
            checkInPerson.Selected = true;

            var locationLinkAttributeKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_LINK_LOCATIONS.AsGuid() ).Key;
            if ( group.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
            {
                LinkLocations( checkInPerson, group, room );
            }

            RemoveOverlappingSchedules( checkInPerson, schedule );

            room.Selected = true;
            group.Selected = true;
            groupType.Selected = true;
            room.Schedules.Where( s => s.Schedule.Guid == schedule.Schedule.Guid ).FirstOrDefault().Selected = true;
            SaveState();
        }

        private bool PersonHasSelectedOption( CheckInPerson checkinPerson )
        {
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];

            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups.Where( g => g.Selected ) )
                .SelectMany( g => g.Locations.Where( l => l.Selected ) )
                .SelectMany( l => l.Schedules.Where( s => s.Selected && selectedSchedules.Contains( s.Schedule.Id ) ) )
                .Any();
        }

        private void TogglePerson( CheckInPerson person )
        {
            if ( person.Selected )
            {
                person.Selected = false;
            }
            else
            {
                person.Selected = true;
                EnsureGroupSelected( person );
            }

            SaveState();
            phPeople.Controls.Clear();
            DisplayPeople();
        }

        /// <summary>
        /// Selects one group and one location for every schedule if no groups are selected
        /// </summary>
        /// <param name="checkinPerson">CheckInPerson</param>
        private void EnsureGroupSelected( CheckInPerson checkinPerson )
        {
            var checkinSchedules = GetCheckinSchedules( checkinPerson );
            var volunteerGroupIds = OccurrenceCache.GetVolunteerOccurrences().Select( o => o.GroupId );
            foreach ( var checkinSchedule in checkinSchedules )
            {
                var checkinGroupTypes = GetGroupTypes( checkinPerson, checkinSchedule );

                var scheduleAlreadySelected = checkinGroupTypes
                    .SelectMany( gt => gt.Groups.Where( g => g.Selected ) )
                    .SelectMany( g => g.Locations.Where( l => l.Selected ) )
                    .SelectMany( l => l.Schedules.Where( s => s.Schedule.Id == checkinSchedule.Schedule.Id && s.Selected ) )
                    .Any();

                if ( scheduleAlreadySelected )
                {
                    continue;
                }

                List<CheckInGroup> checkInGroups = new List<CheckInGroup>();

                foreach ( var cgt in checkinGroupTypes )
                {
                    var cg = GetGroups( checkinPerson, checkinSchedule, cgt );
                    checkInGroups.AddRange( cg );
                }

                //Detect if the person can check in as a voulunteer
                bool isVounteer = false;
                foreach ( var checkinGroupId in checkInGroups.Select( g => g.Group.Id ) )
                {
                    if ( volunteerGroupIds.Contains( checkinGroupId ) )
                    {
                        isVounteer = true;
                        break;
                    }
                }

                //Volunteers need to select their own options
                if ( isVounteer )
                {
                    if ( !PersonHasSelectedOption( checkinPerson ) )
                    {
                        ShowRoomChangeModal( checkinPerson, checkinSchedule );
                    }
                    return;
                }

                //Order the groups by selected then preselected
                checkInGroups = checkInGroups
                    .OrderByDescending( g => g.Selected )
                    .ThenByDescending( g => g.PreSelected )
                    .ToList();

                //Move the first give priority to the top
                for ( var i = 0; i < checkInGroups.Count; i++ )
                {
                    if ( checkInGroups[i].Group.GetAttributeValue( "GivePriority" ).AsBoolean() )
                    {
                        var item = checkInGroups[i];
                        checkInGroups.RemoveAt( i );
                        checkInGroups.Insert( 0, item );
                        break;
                    }
                }

                bool complete = false;
                foreach ( var checkinGroup in checkInGroups )
                {
                    if ( complete )
                    {
                        continue;
                    }

                    var checkinGroupType = checkinGroupTypes.Where( cgt => cgt.Groups.Any( cg => cg.Group.Id == checkinGroup.Group.Id ) ).FirstOrDefault();
                    var checkinLocations = GetLocations( checkinPerson, checkinSchedule, checkinGroupType, checkinGroup );

                    var checkInLocations = checkinLocations
                    .OrderByDescending( l => l.Selected )
                    .ThenByDescending( l => l.PreSelected )
                    .ThenBy( l => AttendanceCache.GetByLocationIdAndScheduleId( l.Location.Id, checkinSchedule.Schedule.Id ).Where( a => !a.IsVolunteer ).Count() )
                    .ToList();

                    foreach ( var checkInLocation in checkinLocations )
                    {
                        if ( LocationScheduleOkay( checkInLocation, checkinSchedule ) )
                        {
                            var locationSchedule = checkInLocation.Schedules.Where( s => s.Schedule.Id == checkinSchedule.Schedule.Id ).FirstOrDefault();
                            if ( locationSchedule != null )
                            {
                                checkinGroupType.Selected = true;
                                checkinGroupType.PreSelected = true;
                                locationSchedule.Selected = true;
                                checkinGroup.Selected = true;
                                checkinGroup.PreSelected = true;
                                checkInLocation.Selected = true;
                                checkInLocation.PreSelected = true;

                                var locationLinkAttributeKey = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_LINK_LOCATIONS.AsGuid() ).Key;
                                if ( checkinGroup.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
                                {
                                    LinkLocations( checkinPerson, checkinGroup, checkInLocation );
                                }

                                RemoveOverlappingSchedules( checkinPerson, locationSchedule );
                                complete = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if the location has a schedule pair for this schedule
        /// If it does it then looks to see if it's pair is available
        /// If the second schedule is not available it returns false
        /// </summary>
        /// <param name="location"></param>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private bool LocationScheduleOkay( CheckInLocation location, CheckInSchedule schedule )
        {
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];
            if ( !selectedSchedules.Contains( schedule.Schedule.Id ) )
            {
                return false;
            }

            if ( location.Location.Attributes == null || !location.Location.Attributes.Any() )
            {
                location.Location.LoadAttributes();
            }
            var pairsList = location.Location.GetAttributeValue( "SchedulePairs" ).ToKeyValuePairList();
            var pairs = new Dictionary<int, int>();
            foreach ( var pair in pairsList )
            {
                pairs[pair.Key.AsInteger()] = ( ( string ) pair.Value ).AsInteger();
                pairs[( ( string ) pair.Value ).AsInteger()] = pair.Key.AsInteger();
            }

            if ( pairs.ContainsKey( schedule.Schedule.Id ) )
            {
                var secondScheduleId = pairs[schedule.Schedule.Id];

                //check to see if the schedule exists
                if ( !location.Schedules.Where( s => s.Schedule.Id == secondScheduleId ).Any() )
                {
                    //if the second schedule doesn't exist go ahead and approve
                    //this way we don't lock people out needlessly
                    return true;
                }

                //Check to see if the second schedule is in the selected schedules
                if ( !selectedSchedules.Contains( secondScheduleId ) )
                {
                    return false;
                }
            }

            if ( location.Location.GetAttributeValue( "MinimumActiveSchedules" ).AsInteger() > selectedSchedules.Count() )
            {
                return false;
            }

            return true;
        }


        private void ProcessLabels()
        {
            LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
            labelPrinter.PrintNetworkLabels();
            var script = labelPrinter.GetClientScript();
            script += "setTimeout( function(){ __doPostBack( '" + btnCancel.UniqueID + "', 'OnClick' ); },4000)";
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }

        #endregion
        enum QuickCheckinState
        {
            Schedule,
            People,
            Checkin
        }
    }
}