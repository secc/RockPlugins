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

using Rock;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;
using Rock.Attribute;
using System.Data.Entity;
using org.secc.FamilyCheckin.Exceptions;
using System.Collections.ObjectModel;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "QuickCheckin" )]
    [Category( "SECC > Check-in" )]
    [Description( "QuickCheckin block for helping parents check in their family quickly." )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Location Link Attribute", "Group attribute which determines if group is location linking." )]
    public partial class QuickCheckin : CheckInBlock
    {
        private string locationLinkAttributeKey = string.Empty;

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


            var locationLinkAtributeGuid = GetAttributeValue( "LocationLinkAttribute" ).AsGuid();
            if ( locationLinkAtributeGuid != Guid.Empty )
            {
                locationLinkAttributeKey = AttributeCache.Read( locationLinkAtributeGuid ).Key;
            }
            else
            {
                maAlert.Show( "LocationLink attribute not configured", ModalAlertType.Alert );
            }

            if ( !Page.IsPostBack )
            {
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

            var quickCheckinState = ( QuickCheckinState ) Session["QuickCheckinState"];

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
                    ShowRoomChangeModal( ( Person ) Session["modalPerson"], ( CheckInSchedule ) Session["modalSchedule"] );
                }
                else
                {
                    showAddPersonModal();
                }

            }
        }

        private void UpdateSelectedSchedules()
        {

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

            Session["SelectedSchedules"] =
                schedules.Where( s => s.Selected )
                .DistinctBy( s => s.Schedule.Id )
              .Select( s => s.Schedule.Id )
              .ToList();
        }

        private void DisplayServiceOptions()
        {
            var schedules = GetSchedules();
            phServices.Controls.Clear();
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];
            foreach ( var schedule in schedules )
            {
                BootstrapButton btnActive = new BootstrapButton();
                if ( selectedSchedules.Contains( schedule.Schedule.Id ) )
                {
                    btnActive.CssClass = "btn btn-block btn-selectSchedule btn-selectSchedule-active";
                }
                else
                {
                    btnActive.CssClass = "btn  btn-block btn-selectSchedule";
                }
                btnActive.Text = schedule.Schedule.Name;
                btnActive.Click += ( s, e ) => { ToggleSchedule( schedule ); DisplayServiceOptions(); };
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

        private void ToggleSchedule( CheckInSchedule schedule )
        {
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];
            if ( selectedSchedules.Contains( schedule.Schedule.Id ) )
            {
                selectedSchedules.Remove( schedule.Schedule.Id );
            }
            else
            {
                selectedSchedules.Add( schedule.Schedule.Id );

                //Remove overlapping schedules
                var otherSchedules = GetSchedules().Where( s => s.Schedule.Id != schedule.Schedule.Id );
                var start = schedule.Schedule.GetCalenderEvent().DTStart;
                var end = schedule.Schedule.GetCalenderEvent().DTEnd;

                foreach ( var otherSchedule in otherSchedules )
                {
                    if ( start.LessThan( otherSchedule.Schedule.GetCalenderEvent().DTEnd )
                        && otherSchedule.Schedule.GetCalenderEvent().DTStart.LessThan( end ) )
                    {
                        if ( selectedSchedules.Contains( otherSchedule.Schedule.Id ) )
                        {
                            selectedSchedules.Remove( otherSchedule.Schedule.Id );
                        }
                    }
                }
            }
            Session["SelectedSchedules"] = selectedSchedules;
        }

        private List<CheckInSchedule> GetSchedules()
        {
            return CurrentCheckInState
                .CheckIn
                .CurrentFamily
                .People
                .SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .DistinctBy( s => s.Schedule.Id )
                .OrderBy( s => s.Schedule.NextStartDateTime )
                .ToList();
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

            foreach ( var person in people.Where( p => p.FamilyMember ) )
            {
                //Unselect person if no groups selected
                if ( person.Selected && !PersonHasSelectedOption( person ) )
                {
                    person.Selected = false;
                    SaveState();
                }
                //Display person checkin information

                i++;

                if ( i % 2 > 0 )
                {
                    hgcRow = new Panel();
                    hgcRow.ID = person.Person.Id.ToString() + "_hgcRow";
                    phPeople.Controls.Add( hgcRow );
                    hgcRow.AddCssClass( "row" );
                }

                Panel hgcPadding = new Panel();
                hgcPadding.ID = person.Person.Id.ToString() + "_hgcPadding";
                hgcPadding.AddCssClass( "col-xs-12 col-lg-6" );
                hgcRow.Controls.Add( hgcPadding );


                if ( GetCheckinSchedules( person.Person ).Count() > 0 )
                { //Display check-in information
                    Panel hgcCell = new Panel();
                    hgcCell.ID = person.Person.Id.ToString() + "hgcCell";
                    hgcCell.AddCssClass( "personContainer col-xs-12" );
                    hgcPadding.Controls.Add( hgcCell );

                    DisplayPersonButton( person, hgcCell );
                    DisplayPersonCheckinAreas( person.Person, hgcCell );
                }
                else
                {   //Display can't check in information
                    Panel hgcCell = new Panel();
                    hgcCell.ID = person.Person.Id.ToString() + "hgcCell_noOption";
                    hgcCell.AddCssClass( "personContainer col-xs-12" );
                    hgcPadding.Controls.Add( hgcCell );
                    DisplayPersonNoOptions( person, hgcCell );
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

        private void DisplayPersonNoOptions( CheckInPerson person, Panel hgcCell )
        {
            //Padding div to make it look nice.
            Panel hgcPadding = new Panel();
            hgcPadding.AddCssClass( "col-sm-4 col-xs-12" );
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
            hgcCell.AddCssClass( "row col-xs-12" );
            hgcCell.Controls.Add( hgcAreaRow );
            var btnMessage = new Label();
            btnMessage.ID = person.Person.Id.ToString() + "_noOptionButton";
            btnMessage.AddCssClass( "btn btn-default col-xs-8 disabled" );
            hgcCell.Controls.Add( btnMessage );

            btnMessage.Text = "There are no classes available for " + person.Person.NickName + "<br> to check-in, or all rooms are currently full.";
            foreach ( var locationId in person.GroupTypes.SelectMany( gt => gt.Groups ).SelectMany( g => g.Locations ).Select( l => l.Location.Id ).ToList() )
            {
                var kla = CheckInCountCache.GetByLocation( locationId );
                if ( kla.SelectMany( k => k.PersonIds ).Contains( person.Person.Id ) )
                {
                    btnMessage.Text = person.Person.NickName + " has already been checked-in.";
                    btnPerson.Text = "<i class='fa fa-check-square-o fa-5x'></i><br/><span>" + person.Person.NickName + "</span>";
                    btnPerson.CssClass = "btn btn-default btn-lg col-xs-12 disabled checkinPerson";
                    break;
                }
            }
        }

        private void DisplayPersonCheckinAreas( Person person, Panel hgcRow )
        {
            List<CheckInSchedule> personSchedules = GetCheckinSchedules( person );

            foreach ( var schedule in personSchedules )
            {
                Panel hgcAreaRow = new Panel();
                hgcRow.Controls.Add( hgcAreaRow );
                hgcAreaRow.ID = person.Id.ToString() + schedule.Schedule.Id.ToString();

                hgcRow.AddCssClass( "row col-xs-12" );
                DisplayPersonSchedule( person, schedule, hgcAreaRow );
            }
        }

        private List<CheckInSchedule> GetCheckinSchedules( Person person )
        {
            var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];

            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
            .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
            .SelectMany( p => p.GroupTypes )
            .SelectMany( gt => gt.Groups )
            .SelectMany( g => g.Locations )
            .SelectMany( l => l.Schedules.Where( s => selectedSchedules.Contains( s.Schedule.Id ) ) )
            .OrderBy( s => s.Schedule.StartTimeOfDay )
            .DistinctBy( s => s.Schedule.Id ).ToList();
        }

        private void DisplayPersonSchedule( Person person, CheckInSchedule schedule, Panel hgcAreaRow )
        {
            BootstrapButton btnSchedule = new BootstrapButton();

            btnSchedule.Text = schedule.Schedule.Name + "<br>(Select Room To Checkin)";
            btnSchedule.CssClass = "btn btn-default col-sm-8 col-xs-12 scheduleNotSelected";
            btnSchedule.ID = person.Guid.ToString() + schedule.Schedule.Guid.ToString();

            btnSchedule.Click += ( s, e ) => { ShowRoomChangeModal( person, schedule ); };
            btnSchedule.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Loading Rooms...";

            CheckInGroupType groupType = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                  .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                  .SelectMany( p => p.GroupTypes )
                  .FirstOrDefault( gt => gt.Selected && gt.Groups.SelectMany( g => g.Locations ).SelectMany( l => l.Schedules.Where( s => s.Selected ) ).Select( s => s.Schedule.Guid ).Contains( schedule.Schedule.Guid ) == true );

            if ( groupType != null )
            {
                var group = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups ).Where( g => g.Selected && g.Locations.Where( l => l.Schedules.Where( s => s.Selected ).Select( s => s.Schedule.Id ).Contains( schedule.Schedule.Id ) && l.Selected ).Any() )
                .FirstOrDefault();

                if ( group != null )
                {
                    var rooms = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                        .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                        .SelectMany( p => p.GroupTypes )
                        .SelectMany( gt => gt.Groups.Where( g => g.Selected && g.Group.Guid == group.Group.Guid ) )
                        .SelectMany( g => g.Locations.Where( l => l.Schedules.Where( s => s.Selected ).Select( s => s.Schedule.Id ).Contains( schedule.Schedule.Id ) && l.Selected ) );

                    CheckInLocation room;
                    room = rooms.FirstOrDefault();

                    //If a room is selected
                    if ( room != null )
                    {
                        if ( LocationScheduleOkay( room, schedule ) )
                        {

                            if ( room.Selected && group.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
                            {
                                LinkLocations( person, group, room );
                            }
                            btnSchedule.CssClass = "btn btn-primary col-xs-8 scheduleSelected";
                            btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + room;
                        }
                        else
                        {
                            room.Selected = false;
                            SaveState();
                        }
                    }
                }
            }
            hgcAreaRow.Controls.Add( btnSchedule );
        }

        private void LinkLocations( Person person, CheckInGroup group, CheckInLocation room )
        {
            var groupTypes = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault().People.Where( p => p.Person.Id == person.Id ).FirstOrDefault().GroupTypes;
            foreach ( var groupType in groupTypes.ToList() )
            {
                if ( !groupType.Groups.Contains( group ) )
                {
                    groupType.Selected = false;
                    groupType.PreSelected = false;
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
                                location.Selected = false;
                                location.PreSelected = false;
                            }
                        }
                    }
                    else
                    {
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

        private void ShowRoomChangeModal( Person person, CheckInSchedule schedule )
        {
            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes );

            List<CheckInGroupType> groupTypes = GetGroupTypes( person, schedule );

            foreach ( var groupType in groupTypes )
            {
                List<CheckInGroup> groups = GetGroups( person, schedule, groupType );

                foreach ( var group in groups )
                {
                    List<CheckInLocation> locations = GetLocations( person, schedule, groupType, group );
                    foreach ( var location in locations )
                    {
                        if ( !LocationScheduleOkay( location, schedule ) )
                        {
                            continue;
                        }

                        Panel hgcPadding = new Panel();
                        hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                        hgcPadding.Style.Add( "padding", "5px" );
                        phModal.Controls.Add( hgcPadding );

                        //Change room button
                        BootstrapButton btnRoom = new BootstrapButton();
                        btnRoom.ID = "btn" + person.Guid.ToString() + group.Group.Guid.ToString() + schedule.Schedule.Guid.ToString() + location.Location.Guid.ToString();
                        btnRoom.Text = groupType.GroupType.Name + ": " + group.Group.Name + "<br>" + location.Location.Name;

                        //Add location count
                        if ( CurrentCheckInType.DisplayLocationCount )
                        {
                            btnRoom.Text += " (Count:" + kioskCountUtility.GetLocationScheduleCount( location.Location.Id, schedule.Schedule.Id ).ChildCount.ToString() + ")";
                        }

                        btnRoom.CssClass = "btn btn-success btn-block btn-lg";
                        btnRoom.Click += ( s, e ) =>
                        {
                            ChangeRoomSelection( person, schedule, groupType, group, location );
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
            btnCancel.ID = "c" + person.Guid.ToString() + schedule.Schedule.Guid.ToString();
            btnCancel.Text = "(Do not check in at " + schedule.Schedule.Name + ")";
            btnCancel.CssClass = "btn btn-danger btn-lg col-md-8 col-xs-12 btn-block";
            btnCancel.Click += ( s, e ) =>
            {
                ClearRoomSelection( person, schedule );
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
            Session["modalPerson"] = person;
            Session["modalSchedule"] = schedule;
        }

        protected void CancelModal( object sender, EventArgs e )
        {
            Session["modalActive"] = false;
            mdChoose.Hide();
        }

        private void ChangeRoomSelection( Person person, CheckInSchedule schedule,
            CheckInGroupType groupType, CheckInGroup group, CheckInLocation room )
        {
            ClearRoomSelection( person, schedule );
            CurrentCheckInState.CheckIn.Families.SelectMany( f => f.People ).Where( p => p.Person.Id == person.Id ).First().Selected = true;
            if ( group.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
            {
                LinkLocations( person, group, room );
            }

            RemoveOverlappingSchedules( person, schedule );

            room.Selected = true;
            group.Selected = true;
            groupType.Selected = true;
            room.Schedules.Where( s => s.Schedule.Guid == schedule.Schedule.Guid ).FirstOrDefault().Selected = true;
            SaveState();
        }

        private void RemoveOverlappingSchedules( Person person, CheckInSchedule schedule )
        {
            var otherSchedules = GetCheckinSchedules( person ).Where( s => s.Schedule.Id != schedule.Schedule.Id );
            var start = schedule.Schedule.GetCalenderEvent().DTStart;
            var end = schedule.Schedule.GetCalenderEvent().DTEnd;

            foreach ( var otherSchedule in otherSchedules )
            {
                if ( start.LessThan( otherSchedule.Schedule.GetCalenderEvent().DTEnd )
                    && otherSchedule.Schedule.GetCalenderEvent().DTStart.LessThan( end ) )
                {
                    ClearRoomSelection( person, otherSchedule );
                }
            }
        }

        /// <summary>
        /// Clears all room selections from room without clearing pre-selections
        /// </summary>
        /// <param name="person"></param>
        /// <param name="schedule"></param>
        private void ClearRoomSelection( Person person, CheckInSchedule schedule )
        {
            List<CheckInGroupType> groupTypes = GetGroupTypes( person, schedule );

            foreach ( var groupType in groupTypes )
            {
                List<CheckInGroup> groups = GetGroups( person, schedule, groupType );

                foreach ( var group in groups )
                {
                    List<CheckInLocation> rooms = GetLocations( person, schedule, groupType, group );

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

        private List<CheckInLocation> GetLocations( Person person, CheckInSchedule schedule, CheckInGroupType groupType, CheckInGroup group )
        {
            var locations = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                        .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                        .SelectMany( p => p.GroupTypes )
                        .Where( gt => gt.GroupType.Guid == groupType.GroupType.Guid )
                        .SelectMany( gt => gt.Groups )
                        .Where( g => g.Group.Guid == group.Group.Guid )
                        .SelectMany( g => g.Locations.Where(
                             l => l.Schedules.Where(
                                 s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ) );

            return locations.ToList();

        }

        private List<CheckInGroup> GetGroups( Person person, CheckInSchedule schedule, CheckInGroupType groupType )
        {
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes )
                .Where( gt => gt.GroupType.Guid == groupType.GroupType.Guid )
                .SelectMany( gt => gt.Groups )
                .Where( g => g.Locations.Where(
                     l => l.Schedules.Where(
                         s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ).Count() != 0 ).ToList();
        }

        private List<CheckInGroupType> GetGroupTypes( Person person, CheckInSchedule schedule )
        {
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes )
                .Where( gt => gt.Groups.Where( g => g.Locations.Where(
                      l => l.Schedules.Where(
                          s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ).Count() != 0 ).Count() != 0 ).ToList();
        }

        private void DisplayPersonButton( CheckInPerson person, Panel hgcRow )
        {
            //Padding div to make it look nice.
            Panel hgcPadding = new Panel();
            hgcPadding.AddCssClass( "col-sm-4 col-xs-12" );
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

        private bool PersonHasGroupSelected( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups.Where( g => g.Selected ) )
                .Any();
        }

        private bool PersonHasSelectedOption( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups.Where( g => g.Selected ) )
                .SelectMany( g => g.Locations.Where( l => l.Selected ) )
                .SelectMany( l => l.Schedules.Where( s => s.Selected ) )
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
            if ( PersonHasGroupSelected( checkinPerson ) )
            {
                return;
            }
            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes );

            var checkinSchedules = GetCheckinSchedules( checkinPerson.Person );
            foreach ( var checkinSchedule in checkinSchedules )
            {
                var checkinGroupTypes = GetGroupTypes( checkinPerson.Person, checkinSchedule );
                var checkinGroupType = checkinGroupTypes.FirstOrDefault();
                if ( checkinGroupTypes.Where( gt => gt.PreSelected ).Any() )
                {
                    checkinGroupType = checkinGroupTypes.Where( gt => gt.PreSelected ).FirstOrDefault();
                }
                if ( checkinGroupType != null )
                {
                    var checkinGroups = GetGroups( checkinPerson.Person, checkinSchedule, checkinGroupType );

                    var checkinGroupIds = checkinGroups.Select( cg => cg.Group.Id ).ToList();
                    bool isVounteer = false;
                    foreach ( var checkinGroupId in checkinGroupIds )
                    {
                        if ( kioskCountUtility.VolunteerGroupIds.Contains( checkinGroupId ) )
                        {
                            isVounteer = true;
                            break;
                        }
                    }

                    if ( isVounteer )
                    { //volunteers need to select their position
                        ShowRoomChangeModal( checkinPerson.Person, checkinSchedule );
                        break;
                    }
                    else
                    { //Children get automatically selected with the emptiest class
                        var checkinGroup = checkinGroups.FirstOrDefault();
                        if ( checkinGroups.Where( g => g.PreSelected ).Any() )
                        {
                            checkinGroup = checkinGroups.Where( g => g.PreSelected ).FirstOrDefault();
                        }
                        if ( checkinGroup != null )
                        {
                            var checkinLocations = GetLocations( checkinPerson.Person, checkinSchedule, checkinGroupType, checkinGroup );
                            var checkinLocation = checkinLocations.OrderBy( l => kioskCountUtility.GetLocationScheduleCount( l.Location.Id, checkinSchedule.Schedule.Id ).ChildCount ).FirstOrDefault();
                            if ( checkinLocation != null )
                            {
                                var locationSchedule = checkinLocation.Schedules.Where( s => s.Schedule.Id == checkinSchedule.Schedule.Id ).FirstOrDefault();
                                if ( locationSchedule != null )
                                {
                                    checkinGroupType.Selected = true;
                                    checkinGroupType.PreSelected = true;
                                    locationSchedule.Selected = true;
                                    checkinGroup.Selected = true;
                                    checkinGroup.PreSelected = true;
                                    checkinLocation.Selected = true;
                                    checkinLocation.PreSelected = true;
                                    if ( checkinGroup.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
                                    {
                                        LinkLocations( checkinPerson.Person, checkinGroup, checkinLocation );
                                    }
                                    RemoveOverlappingSchedules( checkinPerson.Person, locationSchedule );
                                }
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
                    return false;
                }

                //Check to see if the second schedule is in the selected schedules
                var selectedSchedules = ( List<int> ) Session["SelectedSchedules"];
                if ( !selectedSchedules.Contains( secondScheduleId ) )
                {
                    return false;
                }
            }
            return true;
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
                bool test = ProcessActivity( "Save Attendance", out errors );
            }
            catch ( Exception ex )
            {
                LogException( ex );
                NavigateToHomePage();
                return;
            }
            ProcessLabels();
            pnlMain.Visible = false;
        }

        private void ProcessLabels()
        {
            LabelPrinter labelPrinter = new LabelPrinter( CurrentCheckInState, Request );
            labelPrinter.PrintNetworkLabels();
            var script = labelPrinter.GetClientScript();
            script += "setTimeout( function(){ __doPostBack( '" + btnCancel.UniqueID + "', 'OnClick' ); },4000)";
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );
        }

        protected void btnNoCheckin_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnInterfaceCheckin_Click( object sender, EventArgs e )
        {
            if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).SelectMany( f => f.People ).Where( p => p.Selected ).Any() )
            {
                NavigateToNextPage();
                return;
            }

            var rockContext = new RockContext();

            var volAttribute = AttributeCache.Read( Constants.VOLUNTEER_ATTRIBUTE_GUID.AsGuid() );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            List<int> volunteerGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();

            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes );

            //Test for overloaded rooms
            var overload = false;
            var locationService = new LocationService( rockContext );

            var attendanceService = new AttendanceService( rockContext ).Queryable().AsNoTracking();
            foreach ( var person in CurrentCheckInState.CheckIn.CurrentFamily.People.Where( p => p.Selected ) )
            {
                foreach ( var groupType in person.GroupTypes.Where( gt => gt.Selected ) )
                {
                    foreach ( var group in groupType.Groups.Where( g => g.Selected ) )
                    {
                        foreach ( var location in group.Locations.Where( l => l.Selected ) )
                        {
                            var locationEntity = locationService.Get( location.Location.Id );
                            if ( locationEntity == null )
                            {
                                continue;
                            }
                            foreach ( var schedule in location.Schedules.Where( s => s.Selected ).ToList() )
                            {
                                //Check to see if all the schedules have their pairs
                                if ( !LocationScheduleOkay( location, schedule ) )
                                {
                                    schedule.Selected = false;
                                    continue;
                                }

                                var threshold = locationEntity.FirmRoomThreshold ?? 0;
                                var attendanceQry = attendanceService.Where( a =>
                                     a.EndDateTime == null
                                     && a.ScheduleId == schedule.Schedule.Id
                                     && a.StartDateTime >= Rock.RockDateTime.Today );

                                //Filter out if person is already checked in
                                if ( attendanceQry.Where( a => a.PersonAlias.PersonId == person.Person.Id ).Any() )
                                {
                                    location.Schedules.Remove( schedule );
                                    overload = true;
                                }

                                LocationScheduleCount locationScheduleCount = kioskCountUtility.GetLocationScheduleCount( location.Location.Id, schedule.Schedule.Id );
                                if ( locationScheduleCount.TotalCount >= threshold )
                                {
                                    person.Selected = false;
                                    location.Schedules.Remove( schedule );
                                    overload = true;
                                }

                                if ( !volunteerGroupIds.Contains( group.Group.Id ) )
                                {
                                    threshold = Math.Min( locationEntity.FirmRoomThreshold ?? 0, locationEntity.SoftRoomThreshold ?? 0 );

                                    if ( locationScheduleCount.ChildCount >= threshold )
                                    {
                                        person.Selected = false;
                                        location.Schedules.Remove( schedule );
                                        overload = true;
                                    }
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

        protected void addPerson_Click( object sender, EventArgs e )
        {
            showAddPersonModal();
        }

        private void showAddPersonModal()
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
                btnPerson.Text = person.Person.FullName;

                btnPerson.CssClass = "btn btn-success btn-block btn-lg";
                btnPerson.Click += ( s, e ) =>
                {
                    person.FamilyMember = true;
                    SaveState();
                    showAddPersonModal();
                };
                btnPerson.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Adding: " + person.Person.FullName + "to check-in...";
                hgcPadding.Controls.Add( btnPerson );
            }
            Panel hgcCancelPadding = new Panel();
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add( "padding", "5px" );
            phAddPerson.Controls.Add( hgcCancelPadding );

            BootstrapButton btnDone = new BootstrapButton();
            btnDone.ID = "btnDone";
            btnDone.Text = "Done";
            btnDone.CssClass = "btn btn-danger btn-lg col-md-8 col-xs-12 btn-block";
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
        enum QuickCheckinState
        {
            Schedule,
            People,
            Checkin
        }

        protected void btnCheckinHeader_Click( object sender, EventArgs e )
        {
            Session["QuickCheckinState"] = QuickCheckinState.Schedule;
            lStyle.Text = "";
            DisplayServiceOptions();
        }
    }
}