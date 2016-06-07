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

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "QuickCheckin" )]
    [Category( "SECC > Check-in" )]
    [Description( "QuickCheckin block for helping parents check in their family quickly." )]
    [TextField( "Preselect Activity", "Activity for preselecting classes.", false )]

    public partial class QuickCheckin : CheckInBlock
    {
        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
            }

            mdChoose.Header.Visible = false;
            mdChoose.Footer.Visible = false;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                List<string> errors = new List<string>();
                string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                try
                {
                    //Sometimes this blows up if the session state is lost
                    bool test = ProcessActivity( workflowActivity, out errors );
                }
                catch
                {
                    NavigateToPreviousPage();
                    Response.End();
                    return;
                }

                //Find the parent group types
                parentGroupTypesList = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault()
                    .People.SelectMany( p => p.GroupTypes ).SelectMany( gt => gt.GroupType.ParentGroupTypes )
                    .Where( pgt => pgt.ChildGroupTypes.Count() > 1 ).DistinctBy( pgt => pgt.Guid ).ToList();
                Session["parentGroupTypesList"] = parentGroupTypesList;

                currentParentGroupType = getCurrentParentGroupType();

                Session["currentParentGroupType"] = currentParentGroupType;
                Session["selectPgt"] = true;
                Session["modalActive"] = false;
                Session["modalPerson"] = null;
                Session["modalSchedule"] = null;
            }

            parentGroupTypesList = ( List<GroupTypeCache> ) Session["parentGroupTypesList"];
            currentParentGroupType = ( GroupTypeCache ) Session["currentParentGroupType"];

            if ( currentParentGroupType != null && parentGroupTypesList.Count > 1 )
            {
                btnParentGroupTypeHeader.Text = currentParentGroupType.Name;
                btnParentGroupTypeHeader.DataLoadingText = currentParentGroupType.Name + " <i class='fa fa-refresh fa-spin'>";
            }
            else
            {
                btnParentGroupTypeHeader.Text = "Check-In";
                btnParentGroupTypeHeader.DataLoadingText = "Check-In";
            }

            if ( Session["selectPgt"] != null && ( bool ) Session["selectPgt"] )
            {
                DisplayPgtSelection();
            }
            else
            {
                ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                DisplayPeople();
            }


            if ( ( bool ) Session["modalActive"] )
            {
                if ( Session["modalPerson"] != null && Session["modalSchedule"] != null )
                {
                    ShowRoomChangeModal( ( Person ) Session["modalPerson"], ( CheckInSchedule ) Session["modalSchedule"] );
                }
            }
        }

        protected void btnParentGroupTypeHeader_Click( object sender, EventArgs e )
        {
            Session["selectPgt"] = true;
            DisplayPgtSelection();
        }

        private void DisplayPgtSelection()
        {
            var parentGroupTypes = ( List<GroupTypeCache> ) Session["parentGroupTypesList"];
            if ( !( bool ) Session["selectPgt"] || parentGroupTypes.Count == 1 )
            {
                ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                return;
            }
            else
            {
                ltMessage.Text = "Where would you like to check-in to today?";
            }

            foreach ( var pgt in parentGroupTypes )
            {
                BootstrapButton link = new BootstrapButton();
                link.CssClass = "btn btn-default btn-block pgtSelectButton";
                link.Text = pgt.Name;
                link.Click += ( s, e ) => ChangeParentGroupType( pgt );
                link.ID = pgt.Id.ToString();
                link.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Loading checkin...";
                phPgtSelect.Controls.Add( link );
            }

        }

        private GroupTypeCache getCurrentParentGroupType()
        {
            var lastCheckin = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Selected ) ).Select( p => p.LastCheckIn ).Where( dt => dt != null ).Max();

            //Find the current parent group type
            foreach ( var parentGroupType in parentGroupTypesList )
            {
                if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault()
                       .People.Where( p => p.LastCheckIn == lastCheckin ).SelectMany( p => p.GroupTypes )
                       .Where( gt => gt.Selected == true && gt.GroupType.ParentGroupTypes.Contains( parentGroupType ) ).Count() > 0 )
                {
                    return parentGroupType;
                }
            }
            return null;
        }

        private void ChangeParentGroupType( GroupTypeCache parentGroupType )
        {
            //Save pgt
            currentParentGroupType = parentGroupType;
            Session["currentParentGroupType"] = currentParentGroupType;
            Session["selectPgt"] = false;

            //change name
            btnParentGroupTypeHeader.Text = currentParentGroupType.Name;
            btnParentGroupTypeHeader.DataLoadingText = currentParentGroupType.Name + " <i class='fa fa-refresh fa-spin'>";

            //Ensure all people are selected
            CurrentCheckInState.CheckIn.Families.SelectMany( f => f.People ).ToList().ForEach( p => p.Selected = true );

            //Show updated people info
            phPeople.Controls.Clear();
            DisplayPeople();
            SaveState();

            //add sweet animation
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "selectPGT", "setTimeout(function(){showContent()},50);", true );
        }

        private void DisplayPeople()
        {
            var people = CurrentCheckInState.CheckIn.Families.SelectMany( f => f.People );

            HtmlGenericControl hgcRow = new HtmlGenericControl( "div" );
            //hgcRow.AddCssClass( "row" );
            phPeople.Controls.Add( hgcRow );

            foreach ( var person in people )
            {
                //Unselect person if no groups selected
                if ( person.Selected && !PersonHasSelectedGroup( person ) )
                {
                    person.Selected = false;
                    SaveState();
                }
                //Display person checkin information
                if ( GetCheckinSchedules( person.Person ).Count() > 0 )
                {
                    HtmlGenericControl hgcPadding = new HtmlGenericControl( "div" );
                    hgcPadding.AddCssClass( "col-xs-12 col-lg-6" );
                    hgcRow.Controls.Add( hgcPadding );

                    HtmlGenericControl hgcCell = new HtmlGenericControl( "div" );
                    hgcCell.AddCssClass( "personContainer col-xs-12" );
                    hgcPadding.Controls.Add( hgcCell );
                    DisplayPersonButton( person, hgcCell );
                    DisplayPersonCheckinAreas( person.Person, hgcCell );
                }
            }
        }

        private void DisplayPersonCheckinAreas( Person person, HtmlGenericControl hgcRow )
        {
            List<CheckInSchedule> personSchedules = GetCheckinSchedules( person );

            foreach ( var schedule in personSchedules )
            {
                HtmlGenericControl hgcAreaRow = new HtmlGenericControl( "div" );
                hgcRow.AddCssClass( "row col-xs-12" );
                hgcRow.Controls.Add( hgcAreaRow );
                DisplayPersonSchedule( person, schedule, hgcAreaRow );
            }
        }

        private List<CheckInSchedule> GetCheckinSchedules( Person person )
        {
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true ) )
                .SelectMany( gt => gt.Groups ).SelectMany( g => g.Locations ).SelectMany( l => l.Schedules )
                .DistinctBy( s => s.Schedule.Guid ).ToList();
        }

        private void DisplayPersonSchedule( Person person, CheckInSchedule schedule, HtmlGenericControl hgcAreaRow )
        {
            BootstrapButton btnSchedule = new BootstrapButton();
            btnSchedule.Text = schedule.Schedule.Name + "<br>(Select Room To Checkin)";
            hgcAreaRow.Controls.Add( btnSchedule );
            btnSchedule.CssClass = "btn btn-default col-sm-8 col-xs-12 scheduleNotSelected";
            btnSchedule.ID = person.Guid.ToString() + currentParentGroupType.Guid.ToString() + schedule.Schedule.Guid.ToString();
            btnSchedule.Click += ( s, e ) => { ShowRoomChangeModal( person, schedule ); };
            btnSchedule.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Loading Rooms...";

            var groupType = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true ) )
                .FirstOrDefault( gt => gt.Selected && gt.Groups.SelectMany( g => g.Locations ).SelectMany( l => l.Schedules.Where( s => s.Selected ) ).Select( s => s.Schedule.Guid ).Contains( schedule.Schedule.Guid ) == true );

            if ( groupType != null )
            {
                var group = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true && gt == groupType ) )
                .SelectMany( gt => gt.Groups ).FirstOrDefault( g => g.Selected );

                if ( group != null )
                {
                    var room = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                        .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                        .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true && gt == groupType ) )
                        .SelectMany( gt => gt.Groups.Where( g => g.Selected && g.Group.Guid == group.Group.Guid ) )
                        .SelectMany( g => g.Locations ).Where( l => l.Selected && l.Schedules.FirstOrDefault( s => s.Schedule.Guid == schedule.Schedule.Guid ).Selected )
                        .FirstOrDefault();

                    //If a room is selected
                    if ( room != null )
                    {
                        btnSchedule.CssClass = "btn btn-primary col-xs-8 scheduleSelected";
                        btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + room;
                    }
                    else
                    {
                        //if group is selected by a room isn't selected we need to pick a room
                        //Get the rooms 
                        var availableRooms = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                        .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                        .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true && gt == groupType ) )
                        .SelectMany( gt => gt.Groups.Where( g => g.Selected && g.Group.Guid == group.Group.Guid ) )
                        .SelectMany( g => g.Locations ).Where( l => l.Schedules.FirstOrDefault( s => s.Schedule.Guid == schedule.Schedule.Guid ).Selected )
                        .OrderBy( l => l.Location );

                        //Find the room with the fewest number of people in it
                        if ( availableRooms.Count() > 0 )
                        {
                            var autoRoom = new AttendanceService( new RockContext() ).Queryable()
                                 .Where( a => a.StartDateTime.Date == RockDateTime.Today.Date
                                                 && a.EndDateTime == null
                                                 && availableRooms.Select( ar => ar.Location ).Contains( a.Location ) )
                                 .GroupBy( a => a )
                                 .OrderBy( g => g.Count() )
                                 .Select( g => g.Key )
                                 .FirstOrDefault().Location;

                            //set room as selected and show on page
                            if ( autoRoom != null )
                            {
                                CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                                    .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                                    .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true && gt == groupType ) )
                                    .SelectMany( gt => gt.Groups.Where( g => g.Selected && g.Group.Guid == group.Group.Guid ) )
                                    .SelectMany( g => g.Locations ).Where( l => l.Schedules.FirstOrDefault( s => s.Schedule.Guid == schedule.Schedule.Guid ).Selected )
                                    .Where( l => l.Location.Guid == autoRoom.Guid ).FirstOrDefault().Selected = true;

                                btnSchedule.CssClass = "btn btn-primary col-xs-8";
                                btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + autoRoom.Name;
                            }
                        }
                    }
                }
            }
        }

        private void ShowRoomChangeModal( Person person, CheckInSchedule schedule )
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
                        HtmlGenericContainer hgcPadding = new HtmlGenericContainer( "div" );
                        hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                        hgcPadding.Style.Add( "padding", "5px" );
                        phModal.Controls.Add( hgcPadding );


                        //Change room button
                        BootstrapButton btnRoom = new BootstrapButton();
                        btnRoom.ID = "c" + person.Guid.ToString() + schedule.Schedule.Guid.ToString() + room.Location.Guid.ToString();
                        btnRoom.Text = groupType.GroupType.Name + ": " + group.Group.Name + "<br>" +
                            room.Location.Name + " - Count: " + KioskLocationAttendance.Read( room.Location.Id ).CurrentCount;
                        btnRoom.CssClass = "btn btn-success btn-block btn-lg";
                        btnRoom.Click += ( s, e ) =>
                        {
                            ChangeRoomSelection( person, schedule, groupType, group, room );
                            Session["modalActive"] = false;
                            mdChoose.Hide();
                            phPeople.Controls.Clear();
                            DisplayPeople();
                        };
                        btnRoom.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Changing room to: " + room.Location.Name;
                        hgcPadding.Controls.Add( btnRoom );
                    }
                }
            }
            HtmlGenericContainer hgcCancelPadding = new HtmlGenericContainer( "div" );
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
            room.Selected = true;
            group.Selected = true;
            groupType.Selected = true;
            room.Schedules.Where( s => s.Schedule.Guid == schedule.Schedule.Guid ).FirstOrDefault().Selected = true;
            SaveState();
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
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                        .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                        .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true ) )
                        .Where( gt => gt.GroupType.Guid == groupType.GroupType.Guid )
                        .SelectMany( gt => gt.Groups )
                        .Where( g => g.Group.Guid == group.Group.Guid )
                        .SelectMany( g => g.Locations.Where(
                             l => l.Schedules.Where(
                                 s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ) ).ToList();
        }

        private List<CheckInGroup> GetGroups( Person person, CheckInSchedule schedule, CheckInGroupType groupType )
        {
            return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true ) )
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
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == true ) )
                .Where( gt => gt.Groups.Where( g => g.Locations.Where(
                      l => l.Schedules.Where(
                          s => s.Schedule.Guid == schedule.Schedule.Guid ).Count() != 0 ).Count() != 0 ).Count() != 0 ).ToList();
        }

        private void DisplayPersonButton( CheckInPerson person, HtmlGenericControl hgcRow )
        {
            //Padding div to make it look nice.
            HtmlGenericControl hgcPadding = new HtmlGenericControl( "div" );
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

        private bool PersonHasSelectedGroup( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) )
            .SelectMany( gt => gt.Groups ).Where( g => g.Selected ).Any();
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
            if ( PersonHasSelectedGroup( checkinPerson ) )
            {
                return;
            }
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
                    var checkinGroup = checkinGroups.FirstOrDefault();
                    if ( checkinGroups.Where( g => g.PreSelected ).Any() )
                    {
                        checkinGroup = checkinGroups.Where( g => g.PreSelected ).FirstOrDefault();
                    }
                    if ( checkinGroup != null )
                    {

                        var checkinLocations = GetLocations( checkinPerson.Person, checkinSchedule, checkinGroupType, checkinGroup );
                        var checkinLocation = checkinLocations.OrderBy( l => KioskLocationAttendance.Read( l.Location.Id ).CurrentCount ).FirstOrDefault();
                        if ( checkinLocations.Where( l => l.PreSelected ).Any() )
                        {
                            checkinLocation = checkinLocations.Where( l => l.PreSelected ).FirstOrDefault();
                        }
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
                            }
                        }
                    }
                }

            }
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
            //Unselect all groups not in parent group
            var groupTypes = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People )
                .SelectMany( p => p.GroupTypes.Where( gt => gt.GroupType.ParentGroupTypes.Select( pgt => pgt.Guid ).Contains( currentParentGroupType.Guid ) == false ) );
            foreach ( var groupType in groupTypes )
            {
                groupType.Selected = false;
            }
            //Check-in and print tags.
            List<string> errors = new List<string>();
            bool test = ProcessActivity( "Save Attendance", out errors );
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
    }
}