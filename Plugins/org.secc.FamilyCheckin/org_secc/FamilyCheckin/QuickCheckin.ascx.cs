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

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "QuickCheckin" )]
    [Category( "SECC > Check-in" )]
    [Description( "QuickCheckin block for helping parents check in their family quickly." )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Location Link Attribute", "Group attribute which determines if group is location linking." )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]

    public partial class QuickCheckin : CheckInBlock
    {
        private string locationLinkAttributeKey = string.Empty;
        private CullStatus _cullStatus;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/CheckinClient/cordova-2.4.0.js", false );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            mdChoose.Header.Visible = false;
            mdChoose.Footer.Visible = false;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

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

                if ( !CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
                {
                    NavigateToPreviousPage();
                    return;
                }

                var schedules = CurrentCheckInState.CheckIn.CurrentFamily.People
                    .SelectMany( p => p.GroupTypes )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.Locations )
                    .SelectMany( l => l.Schedules )
                    .DistinctBy( s => s.Schedule.Id );

                if ( !schedules.Any() )
                {
                    pnlMain.Visible = false;
                    pnlNoCheckin.Visible = true;
                    var script = "setTimeout( function(){ __doPostBack( '" + btnCancel.UniqueID + "', 'OnClick' ); },15000);";
                    ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "goBack", script, true );
                    return;
                }

                var culledSchedules = CurrentCheckInState.CheckIn.CurrentFamily.People
                    .SelectMany( p => p.GroupTypes )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.Locations )
                    .SelectMany( l => l.Schedules )
                    .DistinctBy( s => s.Schedule.Id )
                    .Where( s => s.Schedule.Description.ToLower().Contains( "cull" ) );

                var ncs = schedules.ToList();
                var cs = culledSchedules.ToList();

                //Counts aren't the same and there are available non-culled schedules give options
                if ( schedules.Count() != culledSchedules.Count()
                    && culledSchedules.Any() )
                {
                    Session["CullStatus"] = CullStatus.Select;
                }
                else
                {
                    Session["CullStatus"] = CullStatus.None;
                }

                foreach ( var person in CurrentCheckInState.CheckIn.CurrentFamily.People )
                {
                    person.Selected = false;
                    SaveState();
                }

            }

            if ( Session["CullStatus"] == null )
            {
                NavigateToPreviousPage();
                return;
            }

            _cullStatus = ( CullStatus ) Session["CullStatus"];

            switch ( _cullStatus )
            {
                case CullStatus.None:
                    btnParentGroupTypeHeader.Text = "Check-In";
                    btnParentGroupTypeHeader.DataLoadingText = "Check-In";
                    ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                    DisplayPeople();
                    break;
                case CullStatus.Active:
                    btnParentGroupTypeHeader.Text = "Worship Only";
                    btnParentGroupTypeHeader.DataLoadingText = "Worship Only <i class='fa fa-refresh fa-spin'>";
                    ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                    DisplayPeople();
                    break;
                case CullStatus.Inactive:
                    btnParentGroupTypeHeader.Text = "Worship + Second Hour";
                    btnParentGroupTypeHeader.DataLoadingText = "Worship + Second Hour <i class='fa fa-refresh fa-spin'>";
                    ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                    DisplayPeople();
                    break;
                case CullStatus.Select:
                    DisplayCullSelection();
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
            }
        }

        protected void btnParentGroupTypeHeader_Click( object sender, EventArgs e )
        {
            if ( _cullStatus == CullStatus.None )
            {
                return;
            }
            Session["CullStatus"] = CullStatus.Select;
            _cullStatus = CullStatus.Select;
            DisplayCullSelection();
        }

        private void DisplayCullSelection()
        {
            if ( _cullStatus == CullStatus.Select )
            {
                ltMessage.Text = "Where would you like to check-in to today?";
            }
            else
            {
                ltMessage.Text = "<style>#pgtSelect{display:none} #quickCheckinContent{left:0px;}</style>";
                return;
            }

            BootstrapButton btnActive = new BootstrapButton();
            btnActive.CssClass = "btn btn-default btn-block pgtSelectButton";
            btnActive.Text = "Worship Only";
            btnActive.Click += ( s, e ) => ChangeCullStatus( CullStatus.Active );
            btnActive.ID = "btnActive";
            btnActive.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Loading checkin...";
            phPgtSelect.Controls.Add( btnActive );

            BootstrapButton btnInactive = new BootstrapButton();
            btnInactive.CssClass = "btn btn-default btn-block pgtSelectButton";
            btnInactive.Text = "Worship + Second Hour";
            btnInactive.Click += ( s, e ) => ChangeCullStatus( CullStatus.Inactive );
            btnInactive.ID = "btnInactive";
            btnInactive.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Loading checkin...";
            phPgtSelect.Controls.Add( btnInactive );
        }

        private void ChangeCullStatus( CullStatus cullStatus )
        {
            Session["cullStatus"] = cullStatus;
            _cullStatus = cullStatus;

            switch ( cullStatus )
            {
                case CullStatus.Active:
                    DeselectCulled();
                    btnParentGroupTypeHeader.Text = "Worship Only";
                    btnParentGroupTypeHeader.DataLoadingText = "Worship Only <i class='fa fa-refresh fa-spin'>";
                    break;
                case CullStatus.Inactive:
                    btnParentGroupTypeHeader.Text = "Worship + Second Hour";
                    btnParentGroupTypeHeader.DataLoadingText = "Worship + Second Hour <i class='fa fa-refresh fa-spin'>";
                    break;
                default:
                    break;
            }

            //Show updated people info
            phPeople.Controls.Clear();
            DisplayPeople();

            //add sweet animation
            ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "selectPGT", "setTimeout(function(){showContent()},50);", true );
        }

        private void DeselectCulled()
        {
            foreach ( var family in CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ) )
            {
                foreach ( var person in family.People )
                {
                    foreach ( var groupType in person.GroupTypes )
                    {
                        foreach ( var group in groupType.Groups )
                        {
                            foreach ( var location in group.Locations )
                            {
                                if ( location.Location.Attributes == null || !location.Location.Attributes.Any() )
                                {
                                    location.Location.LoadAttributes();
                                }

                                if ( location.Location.GetAttributeValue( "Cull" ).AsBoolean() )
                                {
                                    location.Selected = false;
                                }

                                foreach ( var schedule in location.Schedules.Where( s => s.Schedule.Description.ToLower().Contains( "cull" ) ) )
                                {
                                    schedule.Selected = false;
                                }
                                if ( !location.Schedules.Where( s => s.Selected ).Any() )
                                {
                                    location.Selected = false;
                                }
                            }
                            if ( !group.Locations.Where( l => l.Selected ).Any() )
                            {
                                group.Selected = false;
                            }
                        }
                        if ( !groupType.Groups.Where( g => g.Selected ).Any() )
                        {
                            groupType.Selected = false;
                        }
                    }
                    if ( !person.GroupTypes.Where( gt => gt.Selected ).Any() )
                    {
                        person.Selected = false;
                    }
                }
            }
        }

        private void DisplayPeople()
        {
            if ( CurrentCheckInState == null )
            {
                LogException( new CheckInStateLost( "Lost check-in state on displaying people" ) );
                NavigateToPreviousPage();
                return;
            }

            var people = CurrentCheckInState.CheckIn.Families.SelectMany( f => f.People ).OrderBy( p => p.Person.BirthDate );

            int i = 0;
            Panel hgcRow = new Panel();

            foreach ( var person in people )
            {
                //Unselect person if no groups selected
                if ( person.Selected && !PersonHasSelectedGroup( person ) )
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
                var kla = KioskLocationAttendance.Read( locationId );
                if ( kla.Groups.SelectMany( g => g.Schedules ).SelectMany( s => s.DistinctPersonIds ).Contains( person.Person.Id ) )
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
                hgcAreaRow.ID = person.Id.ToString() + schedule.Schedule.Id.ToString() + ( _cullStatus == CullStatus.Inactive ? "cullingoff" : "" );

                hgcRow.AddCssClass( "row col-xs-12" );
                DisplayPersonSchedule( person, schedule, hgcAreaRow );
            }
        }

        private List<CheckInSchedule> GetCheckinSchedules( Person person )
        {
            if ( _cullStatus == CullStatus.Active )
            {
                var locations = CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                    .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                    .SelectMany( p => p.GroupTypes )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.Locations );

                foreach ( var location in locations )
                {
                    if ( location.Location.Attributes == null || !location.Location.Attributes.Any() )
                    {
                        location.Location.LoadAttributes();
                    }
                }

                return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                    .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                    .SelectMany( p => p.GroupTypes )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.Locations )
                    .Where( l => !l.Location.GetAttributeValue( "Cull" ).AsBoolean() )
                    .SelectMany( l => l.Schedules )
                    .DistinctBy( s => s.Schedule.Id )
                    .Where( s => !s.Schedule.Description.ToLower().Contains( "cull" ) )
                    .OrderBy( s => s.Schedule.StartTimeOfDay )
                    .ToList();
            }
            else
            {
                return CurrentCheckInState.CheckIn.Families.Where( f => f.Selected )
                .SelectMany( f => f.People.Where( p => p.Person.Guid == person.Guid ) )
                .SelectMany( p => p.GroupTypes )
                .SelectMany( gt => gt.Groups )
                .SelectMany( g => g.Locations )
                .SelectMany( l => l.Schedules )
                .OrderBy( s => s.Schedule.StartTimeOfDay )
                .DistinctBy( s => s.Schedule.Id ).ToList();
            }
        }

        private void DisplayPersonSchedule( Person person, CheckInSchedule schedule, Panel hgcAreaRow )
        {
            BootstrapButton btnSchedule = new BootstrapButton();

            btnSchedule.Text = schedule.Schedule.Name + "<br>(Select Room To Checkin)";
            btnSchedule.CssClass = "btn btn-default col-sm-8 col-xs-12 scheduleNotSelected";
            btnSchedule.ID = person.Guid.ToString() + schedule.Schedule.Guid.ToString();
            if ( _cullStatus == CullStatus.Inactive )
            {
                btnSchedule.ID = person.Guid.ToString() + schedule.Schedule.Guid.ToString() + "worshipPlus";
            }
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

                    if ( _cullStatus == CullStatus.Active )
                    {
                        room = rooms.Where( l => !l.Location.GetAttributeValue( "Cull" ).AsBoolean() ).FirstOrDefault();
                    }
                    else
                    {
                        room = rooms.FirstOrDefault();
                    }

                    //If a room is selected
                    if ( room != null )
                    {
                        if ( room.Selected && group.Group.GetAttributeValue( locationLinkAttributeKey ).AsBoolean() )
                        {
                            LinkLocations( person, group, room );
                        }
                        btnSchedule.CssClass = "btn btn-primary col-xs-8 scheduleSelected";
                        btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + room;
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
            phModal.Controls.Clear();

            var volAttributeGuid = GetAttributeValue( "VolunteerGroupAttribute" ).AsGuid();
            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes, volAttributeGuid );

            List<CheckInGroupType> groupTypes = GetGroupTypes( person, schedule );

            foreach ( var groupType in groupTypes )
            {
                List<CheckInGroup> groups = GetGroups( person, schedule, groupType );

                foreach ( var group in groups )
                {
                    List<CheckInLocation> locations = GetLocations( person, schedule, groupType, group );
                    foreach ( var location in locations )
                    {
                        Panel hgcPadding = new Panel();
                        hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                        hgcPadding.Style.Add( "padding", "5px" );
                        phModal.Controls.Add( hgcPadding );

                        //Change room button
                        BootstrapButton btnRoom = new BootstrapButton();
                        btnRoom.ID = "c" + person.Guid.ToString() + group.Group.Guid.ToString() + schedule.Schedule.Guid.ToString() + location.Location.Guid.ToString();
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

            if ( _cullStatus == CullStatus.Active )
            {
                foreach ( var location in locations )
                {
                    if ( location.Location.Attributes == null || !location.Location.Attributes.Any() )
                    {
                        location.Location.LoadAttributes();
                    }
                }
                return locations.Where( l => !l.Location.GetAttributeValue( "Cull" ).AsBoolean() ).ToList();
            }
            else
            {
                return locations.ToList();
            }
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

        private bool PersonHasSelectedGroup( CheckInPerson checkinPerson )
        {
            return checkinPerson.GroupTypes
                .SelectMany( gt => gt.Groups )
                .Where( g => g.Selected )
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
            if ( PersonHasSelectedGroup( checkinPerson ) )
            {
                return;
            }
            var volAttributeGuid = GetAttributeValue( "VolunteerGroupAttribute" ).AsGuid();
            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes, volAttributeGuid );

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

            //Unselect all schedules if culled
            if ( _cullStatus == CullStatus.Active )
            {
                DeselectCulled();
            }

            var rockContext = new RockContext();

            var volAttributeGuid = GetAttributeValue( "VolunteerGroupAttribute" ).AsGuid();
            var volAttribute = AttributeCache.Read( volAttributeGuid );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            List<int> volunteerGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();

            KioskCountUtility kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes, volAttributeGuid );

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
                                var threshold = locationEntity.FirmRoomThreshold ?? 0;
                                var attendanceQry = attendanceService.Where( a =>
                                     a.DidAttend == true
                                     && a.EndDateTime == null
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
    }

    enum CullStatus
    {
        None,
        Active,
        Inactive,
        Select
    }
}