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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Rock.CheckIn;
using Rock.Attribute;
using org.secc.FamilyCheckin.Utilities;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Check-In Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Helps manage rooms and room ratios." )]
    [DefinedTypeField( "Deactivated Defined Type", "Check-in monitor needs a place to save deactivated checkin configurations." )]
    [TextField( "Room Ratio Attribute Key", "Attribute key for room ratios", true, "RoomRatio" )]
    [DataViewField( "Approved People", "Data view which contains the members who may check-in.", entityTypeName: "Rock.Model.Person" )]
    public partial class CheckinMonitor : CheckInBlock
    {
        KioskCountUtility kioskCountUtility;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }
            BindTable();

            if ( !Page.IsPostBack )
            {
                ViewState["MinimizedGroupTypes"] = new List<int>();
                BindDropDown();
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            }

            //Open modal if it is active
            RebuildModal();
        }

        private void BindDropDown()
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

            using ( RockContext _rockContext = new RockContext() )
            {

                var schedules = new GroupTypeService( _rockContext )
                    .GetByIds( CurrentCheckInState.ConfiguredGroupTypes )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.GroupLocations )
                    .SelectMany( gl => gl.Schedules )
                    .DistinctBy( s => s.Id )
                    .Select( s =>
                    new { Id = s.Id, Name = s.Name } )
                    .ToList();

                schedules.Insert( 0, new { Id = -1, Name = "[ All Schedules ]" } );
                schedules.Insert( 0, new { Id = 0, Name = "[ Active Schedules ]" } );

                ddlSchedules.DataSource = schedules;
                ddlSchedules.DataBind();
            }

            ddlClose.BindToEnum<CloseScope>();
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            try
            {
                kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes,
                    GetAttributeValue( "DeactivatedDefinedType" ).AsGuid() );
            }
            catch ( Exception ex )
            {
                LogException( ex );
                maError.Show( "Block Not Configured", ModalAlertType.Alert );
                return;
            }


            //preload location attributes

            Dictionary<int, int> locationRatios = new Dictionary<int, int>();

            if ( ViewState["LocationRatios"] != null && ( ( Dictionary<int, int> ) ViewState["LocationRatios"] ).Any() )
            {
                locationRatios = ( ( Dictionary<int, int> ) ViewState["LocationRatios"] );
            }
            else
            {
                using ( var _rockContext = new RockContext() )
                {
                    var ratioKey = GetAttributeValue( "RoomRatioAttributeKey" );
                    var ratioAttribute = new AttributeService( _rockContext ).Queryable()
                        .Where( a => a.Key == ratioKey )
                        .FirstOrDefault();
                    if ( ratioAttribute != null )
                    {
                        var attributeValueService = new AttributeValueService( _rockContext ).Queryable();
                        attributeValueService
                            .Where( av => av.AttributeId == ratioAttribute.Id )
                            .Select( av => new
                            {
                                LocationId = av.EntityId.Value,
                                Ratio = av.Value
                            } ).ToList()
                            .ForEach( anon => locationRatios[anon.LocationId] = anon.Ratio.AsInteger() );
                    }
                    ViewState["LocationRatios"] = locationRatios;
                }
            }

            var groupLocationSchedules = kioskCountUtility.GroupLocationSchedules;

            //Remove duplicates to prevent collisions
            groupLocationSchedules = groupLocationSchedules
                .DistinctBy( gls => new { GroupLocationId = gls.GroupLocation.Id, ScheduleId = gls.Schedule.Id } )
                .ToList();

            var selectedScheduleId = ddlSchedules.SelectedValue.AsInteger();
            if ( selectedScheduleId > 0 )
            {
                groupLocationSchedules = groupLocationSchedules.Where( gls => gls.Schedule.Id == selectedScheduleId ).ToList();
            }
            else if ( selectedScheduleId == 0 )
            {
                groupLocationSchedules = groupLocationSchedules.Where( gls => gls.Schedule.WasScheduleOrCheckInActive( RockDateTime.Now ) ).ToList();
            }

            foreach ( var groupType in kioskCountUtility.GroupTypes.OrderBy( gt => gt.Order ).ToList() )
            {
                if ( ViewState["MinimizedGroupTypes"] != null
                    && ( ( List<int> ) ViewState["MinimizedGroupTypes"] ).Contains( groupType.Id ) )
                {
                    LinkButton lbMaximize = new LinkButton();
                    lbMaximize.ID = "max" + groupType.Id.ToString();
                    lbMaximize.Text = string.Format( "<br><i class='fa fa-plus-square'></i> <b>{0}</b>", groupType.Name );
                    lbMaximize.Click += ( s, e ) => MaximizeGroupType( groupType.Id );
                    phContent.Controls.Add( lbMaximize );
                    continue;
                }

                LinkButton lbMinimize = new LinkButton();
                lbMinimize.ID = "min" + groupType.Id.ToString();
                lbMinimize.Text = string.Format( "<br> <i class='fa fa-minus-square'></i> <b>{0}</b>", groupType.Name );
                lbMinimize.Click += ( s, e ) => MinimizeGroupType( groupType.Id );
                phContent.Controls.Add( lbMinimize );
                Table table = new Table();
                table.CssClass = "table";
                table.Style.Add( "margin-bottom", "10px" );
                phContent.Controls.Add( table );

                foreach ( var group in groupType.Groups.Where( g => g.IsActive ).OrderBy( g => g.Order ) )
                {
                    var occurance = groupLocationSchedules
                        .Where( gls => gls.GroupLocation.GroupId == group.Id )
                        .OrderBy( gls => gls.GroupLocation.Order )
                        .ThenBy( gls => gls.GroupLocation.Location.Name )
                        .ThenBy( gls => gls.Schedule.StartTimeOfDay )
                        .ToList();

                    Literal ltG = new Literal();
                    phContent.Controls.Add( ltG );

                    TableHeaderRow thr = new TableHeaderRow();
                    table.Controls.Add( thr );

                    TableHeaderCell thcName = new TableHeaderCell();
                    thcName.Text = group.Name;
                    thcName.Style.Add( "width", "35%" );
                    thr.Controls.Add( thcName );


                    TableHeaderCell thcRatio = new TableHeaderCell();
                    if ( occurance.Any() )
                    {
                        thcRatio.Text = "Kid/Adult Ratio";
                    }
                    else
                    {
                        thcRatio.Text = "[No selected schedules]";
                    }
                    thcRatio.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcRatio );

                    TableHeaderCell thcCount = new TableHeaderCell();
                    if ( occurance.Any() )
                    {
                        thcCount.Text = "Kid / Total Count";
                    }
                    thcCount.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcCount );

                    TableHeaderCell thcToggle = new TableHeaderCell();
                    thcToggle.Style.Add( "width", "15%" );
                    thr.Controls.Add( thcToggle );

                    TableHeaderCell thcRoom = new TableHeaderCell();
                    thcRoom.Style.Add( "width", "10%" );
                    thr.Controls.Add( thcRoom );

                    foreach ( var gls in occurance )
                    {
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        if ( selectedScheduleId > 0 )
                        {
                            name.Text = gls.GroupLocation.Location.Name;
                        }
                        else
                        {
                            name.Text = gls.GroupLocation.Location.Name + " @ " + gls.Schedule.Name;
                        }
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        int ratio = 0;
                        if ( locationRatios.ContainsKey( gls.GroupLocation.LocationId ) )
                        {
                            ratio = locationRatios[gls.GroupLocation.LocationId];
                        }

                        var lsCount = kioskCountUtility.GetLocationScheduleCount( gls.GroupLocation.Location.Id, gls.Schedule.Id );
                        var ratioDistance = ( lsCount.VolunteerCount * ratio ) - lsCount.ChildCount;
                        tcRatio.Text = lsCount.ChildCount.ToString() + "/" + lsCount.VolunteerCount.ToString();
                        if ( lsCount.TotalCount != 0 && ratioDistance == 0 )
                        {
                            tcRatio.Text += " [Room at ratio]";
                            tcRatio.CssClass = "warning";
                        }
                        else if ( lsCount.TotalCount != 0 && ratioDistance < 0 )
                        {
                            tcRatio.Text += " [" + ( ratioDistance * -1 ).ToString() + " over ratio]";
                            tcRatio.CssClass = "danger";
                        }
                        else if ( ratioDistance < 4 && lsCount.TotalCount != 0 )
                        {

                            tcRatio.CssClass = "info";
                            tcRatio.Text += " [" + ratioDistance.ToString() + " remaining]";
                        }
                        else if ( lsCount.TotalCount != 0 )
                        {
                            tcRatio.CssClass = "success";
                            tcRatio.Text += " [" + ratioDistance.ToString() + " remaining]";
                        }

                        tr.Controls.Add( tcRatio );

                        TableCell tcCapacity = new TableCell();
                        if ( ( lsCount.TotalCount + lsCount.ReservedCount ) != 0
                            && ( lsCount.TotalCount + lsCount.ReservedCount ) >= ( gls.GroupLocation.Location.FirmRoomThreshold ?? 0 ) )
                        {
                            if ( gls.Active && lsCount.TotalCount != 0
                                && ( lsCount.TotalCount >= ( gls.GroupLocation.Location.FirmRoomThreshold ?? 0 )
                                || lsCount.ChildCount >= ( gls.GroupLocation.Location.SoftRoomThreshold ?? 0 ) ) )
                            {
                                CloseOccurrence( gls.GroupLocation.Id, gls.Schedule.Id );
                                return;
                            }
                            tcCapacity.CssClass = "danger";
                        }
                        else if ( ( lsCount.TotalCount + lsCount.ReservedCount ) != 0
                            && (
                                  ( ( lsCount.TotalCount + lsCount.ReservedCount ) + 3 ) >= ( gls.GroupLocation.Location.FirmRoomThreshold ?? 0 )
                                    || ( lsCount.ChildCount + 3 ) >= ( gls.GroupLocation.Location.SoftRoomThreshold ?? 0 ) + 3 ) )
                        {
                            tcCapacity.CssClass = "warning";
                        }
                        else
                        {
                            tcCapacity.CssClass = "success";
                        }
                        tcCapacity.Text = lsCount.ChildCount.ToString() + " of " + ( gls.GroupLocation.Location.SoftRoomThreshold ?? 0 ) + " / " + lsCount.TotalCount.ToString() + " of " + ( gls.GroupLocation.Location.FirmRoomThreshold ?? 0 ).ToString() + ( lsCount.ReservedCount > 0 ? " (+" + lsCount.ReservedCount + " reserved)" : "" );

                        tr.Controls.Add( tcCapacity );

                        TableCell tcToggle = new TableCell();
                        tr.Controls.Add( tcToggle );

                        BootstrapButton btnToggle = new BootstrapButton();
                        btnToggle.ID = "btnL" + group.Id.ToString() + gls.GroupLocation.Id + gls.Schedule.Id;
                        btnToggle.Click += ( s, ee ) => { ToggleLocation( gls.GroupLocation.Id, gls.Schedule.Id ); };
                        if ( gls.Active )
                        {
                            btnToggle.Text = "Active";
                            btnToggle.ToolTip = "Click to deactivate.";
                            btnToggle.CssClass = "btn btn-sm btn-success btn-block";
                        }
                        else
                        {
                            btnToggle.Text = "Inactive";
                            btnToggle.ToolTip = "Click to activate.";
                            btnToggle.CssClass = "btn btn-sm btn-danger btn-block";
                        }
                        tcToggle.Controls.Add( btnToggle );

                        TableCell tcLocation = new TableCell();
                        tr.Controls.Add( tcLocation );

                        LinkButton btnOccurrence = new LinkButton();
                        btnOccurrence.ID = "btn" + gls.GroupLocation.Id.ToString() + gls.Schedule.Id;
                        btnOccurrence.Text = "<i class='fa fa-users'></i>";
                        btnOccurrence.CssClass = "btn btn-sm btn-default";
                        btnOccurrence.Click += ( s, e ) => { OccurrenceModal( gls.GroupLocation, gls.Schedule ); };
                        btnOccurrence.Attributes.Add( "data-loading-text", "<i class='fa fa-refresh fa-spin working'></i>" );
                        btnOccurrence.OnClientClick = "Rock.controls.bootstrapButton.showLoading(this);stopTimer();";
                        tcLocation.Controls.Add( btnOccurrence );

                        LinkButton btnLocation = new LinkButton();
                        btnLocation.ID = "btnLocation" + gls.GroupLocation.Id.ToString() + gls.Schedule.Id;
                        btnLocation.Text = "<i class='fa fa-map-marker'></i>";
                        btnLocation.CssClass = "btn btn-sm btn-default";
                        btnLocation.Click += ( s, e ) => { ShowLocationModal( gls.GroupLocation.Location ); };
                        btnLocation.Attributes.Add( "data-loading-text", "<i class='fa fa-refresh fa-spin working'></i>" );
                        btnLocation.OnClientClick = "Rock.controls.bootstrapButton.showLoading(this);stopTimer();";
                        tcLocation.Controls.Add( btnLocation );
                    }
                }
            }
        }

        private void MinimizeGroupType( int groupTypeId )
        {
            if ( ViewState["MinimizedGroupTypes"] == null )
            {
                ViewState["MinimizedGroupTypes"] = new List<int>();
            }
            var minimizedGroupTypes = ( List<int> ) ViewState["MinimizedGroupTypes"];
            if ( !minimizedGroupTypes.Contains( groupTypeId ) )
            {
                minimizedGroupTypes.Add( groupTypeId );
            }
            ViewState["MinimizedGroupTypes"] = minimizedGroupTypes;
            BindTable();
        }

        private void MaximizeGroupType( int groupTypeId )
        {
            if ( ViewState["MinimizedGroupTypes"] == null )
            {
                ViewState["MinimizedGroupTypes"] = new List<int>();
            }
            var minimizedGroupTypes = ( List<int> ) ViewState["MinimizedGroupTypes"];
            while ( minimizedGroupTypes.Contains( groupTypeId ) )
            {
                minimizedGroupTypes.Remove( groupTypeId );
            }
            ViewState["MinimizedGroupTypes"] = minimizedGroupTypes;
            BindTable();
        }

        private void ShowLocationModal( Location location )
        {
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            ltLocationName.Text = location.Name;
            hfLocationId.Value = location.Id.ToString();
            location.LoadAttributes();
            var roomRatioKey = GetAttributeValue( "RoomRatioAttributeKey" );
            var roomRatio = location.GetAttributeValue( roomRatioKey );
            tbRatio.Text = roomRatio;
            tbFirmThreshold.Text = location.FirmRoomThreshold.ToString();
            tbSoftThreshold.Text = location.SoftRoomThreshold.ToString();
            mdLocation.Show();
        }

        private void RebuildModal()
        {
            if ( ViewState["ModalGroupLocation"] != null && ViewState["ModalGroupLocationSchedule"] != null )
            {
                using ( RockContext _rockContext = new RockContext() )
                {
                    var groupLocationId = ( int ) ViewState["ModalGroupLocation"];
                    var scheduleId = ( int ) ViewState["ModalGroupLocationSchedule"];
                    var groupLocation = new GroupLocationService( _rockContext ).Get( groupLocationId );
                    var schedule = new ScheduleService( _rockContext ).Get( scheduleId );
                    if ( groupLocation != null && schedule != null )
                    {
                        OccurrenceModal( groupLocation, schedule );
                        return;
                    }
                }
            }
            else if ( ViewState["SearchType"] != null )
            {
                var searchBy = ( SearchType ) ViewState["SearchType"];
                switch ( searchBy )
                {
                    case SearchType.Code:
                        SearchByCode();
                        break;
                    case SearchType.Name:
                        SearchByName();
                        break;
                    default:
                        break;
                }
            }
        }

        private void OccurrenceModal( GroupLocation groupLocation, Schedule schedule )
        {
            phLocation.Controls.Clear();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            mdOccurrence.Show();
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                //Get all attendance data for grouplocationschedule for today
                var data = attendanceService.Queryable( "PersonAlias.Person,Occurrence,Occurrence.Location,Occurrence.Group,Occurrence.Schedule" )
                    .Where( a =>
                            a.PersonAliasId != null
                            && a.Occurrence.LocationId == groupLocation.LocationId
                            && a.Occurrence.ScheduleId == schedule.Id
                            && a.StartDateTime >= Rock.RockDateTime.Today
                            )
                    .ToList();
                var currentVolunteers = data.Where( a => a.DidAttend == true
                                                    && a.EndDateTime == null
                                                    && kioskCountUtility.VolunteerGroupIds.Contains( a.Occurrence.GroupId ?? 0 ) )
                                                    .OrderBy( a => a.PersonAlias.Person.LastName )
                                                    .ThenBy( a => a.PersonAlias.Person.NickName );
                var currentChildren = data.Where( a => a.DidAttend == true
                                                    && a.EndDateTime == null
                                                    && !kioskCountUtility.VolunteerGroupIds.Contains( a.Occurrence.GroupId ?? 0 ) )
                                                    .OrderBy( a => a.PersonAlias.Person.LastName )
                                                    .ThenBy( a => a.PersonAlias.Person.NickName );

                var current = new List<Attendance>();
                current.AddRange( currentVolunteers );
                current.AddRange( currentChildren );
                var reserved = data.Where( a => a.DidAttend != true && a.EndDateTime == null ).ToList();
                var history = data.Where( a => a.DidAttend == true && a.EndDateTime != null ).ToList();

                if ( !data.Any() )
                {
                    ltLocation.Text = "There are currently no attendance records for this location today.";
                }
                else
                {
                    ltLocation.Text = string.Format( "{0} @ {1}", groupLocation.Location.Name, schedule.Name );
                    if ( current.Any() )
                    {
                        Literal ltCurrent = new Literal();
                        ltCurrent.Text = "<b>Current</b>";
                        phLocation.Controls.Add( ltCurrent );

                        Table tblCurrent = new Table();
                        tblCurrent.CssClass = "table";
                        phLocation.Controls.Add( tblCurrent );

                        foreach ( var record in current )
                        {
                            TableRow trRecord = new TableRow();
                            tblCurrent.Controls.Add( trRecord );

                            TableCell tcName = new TableCell();
                            tcName.Text = record.PersonAlias.Person.FullName;
                            trRecord.Controls.Add( tcName );
                            tcName.Style.Add( "width", "30%" );

                            TableCell tcGroup = new TableCell();
                            trRecord.Controls.Add( tcGroup );
                            tcGroup.Style.Add( "width", "30%" );
                            if ( record.Occurrence.Group != null )
                            {
                                tcGroup.Text = record.Occurrence.Group.Name;
                            }

                            TableCell tcSchedule = new TableCell();
                            trRecord.Controls.Add( tcSchedule );
                            tcSchedule.Style.Add( "width", "25%" );
                            tcSchedule.Text = "Arrived: " + record.StartDateTime.ToString();

                            TableCell tcButtons = new TableCell();
                            trRecord.Controls.Add( tcButtons );
                            tcButtons.Style.Add( "width", "15%" );

                            BootstrapButton btnTableMove = new BootstrapButton();
                            btnTableMove.ID = "move" + record.Id.ToString();
                            btnTableMove.CssClass = "btn btn-warning btn-xs";
                            btnTableMove.Text = "Move";
                            btnTableMove.Click += ( s, e ) =>
                            {
                                mdOccurrence.Hide();
                                MoveModal( record.Id );
                            };
                            tcButtons.Controls.Add( btnTableMove );

                            BootstrapButton btnCheckout = new BootstrapButton();
                            btnCheckout.ID = "checkout" + record.Id.ToString();
                            btnCheckout.CssClass = "btn btn-danger btn-xs";
                            btnCheckout.Text = "Check-Out";
                            btnCheckout.Click += ( s, e ) =>
                            {
                                Checkout( record.Id );
                            };
                            tcButtons.Controls.Add( btnCheckout );
                        }
                    }
                    if ( reserved.Any() )
                    {

                        Literal ltReserved = new Literal();
                        ltReserved.Text = "<b>Reserved</b>";
                        phLocation.Controls.Add( ltReserved );

                        Table tblReserved = new Table();
                        tblReserved.CssClass = "table";
                        phLocation.Controls.Add( tblReserved );
                        foreach ( var record in reserved.ToList() )
                        {
                            TableRow trRecord = new TableRow();
                            tblReserved.Controls.Add( trRecord );

                            TableCell tcName = new TableCell();
                            tcName.Text = record.PersonAlias.Person.FullName;
                            trRecord.Controls.Add( tcName );
                            tcName.Style.Add( "width", "30%" );

                            TableCell tcGroup = new TableCell();
                            trRecord.Controls.Add( tcGroup );
                            tcGroup.Style.Add( "width", "30%" );
                            if ( record.Occurrence.Group != null )
                            {
                                tcGroup.Text = record.Occurrence.Group.Name;
                            }

                            TableCell tcSchedule = new TableCell();
                            trRecord.Controls.Add( tcSchedule );
                            tcSchedule.Style.Add( "width", "25%" );
                            if ( record.Occurrence.Schedule != null )
                            {
                                tcSchedule.Text = record.Occurrence.Schedule.Name;
                            }

                            TableCell tcButtons = new TableCell();
                            trRecord.Controls.Add( tcButtons );
                            tcButtons.Style.Add( "width", "15%" );

                            BootstrapButton btnTableMove = new BootstrapButton();
                            btnTableMove.ID = "move" + record.Id.ToString();
                            btnTableMove.CssClass = "btn btn-warning btn-xs";
                            btnTableMove.Text = "Move";
                            btnTableMove.Click += ( s, e ) =>
                            {
                                mdOccurrence.Hide();
                                MoveModal( record.Id );
                            };
                            tcButtons.Controls.Add( btnTableMove );

                            BootstrapButton btnCancel = new BootstrapButton();
                            btnCancel.ID = "cancel" + record.Id.ToString();
                            btnCancel.CssClass = "btn btn-danger btn-xs";
                            btnCancel.Text = "Cancel";
                            btnCancel.Click += ( s, e ) => { CancelReservation( record.Id ); };
                            tcButtons.Controls.Add( btnCancel );
                        }
                    }
                }
                ViewState.Add( "ModalGroupLocation", groupLocation.Id );
                ViewState.Add( "ModalGroupLocationSchedule", schedule.Id );
            }
        }

        private void CancelReservation( int id )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var record = attendanceService.Get( id );
                if ( record != null )
                {
                    record.EndDateTime = Rock.RockDateTime.Now;
                    CheckInCountCache.RemoveAttendance( record );
                    _rockContext.SaveChanges();
                    RebuildModal();
                }
            }
        }

        private void Checkout( int id )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var record = attendanceService.Get( id );
                if ( record != null )
                {
                    record.EndDateTime = Rock.RockDateTime.Now;
                    _rockContext.SaveChanges();
                    CheckInCountCache.RemoveAttendance( record );
                    RebuildModal();
                }
            }
        }

        private void MoveModal( int attendanceId )
        {
            ViewState["ModalGroupLocation"] = null;
            ViewState["ModalGroupLocationSchedule"] = null;
            ViewState["ModalLocation"] = null;
            mdMove.Show();
            ViewState.Add( "ModalMove", attendanceId );

            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var attendanceRecord = attendanceService.Get( attendanceId );
                if ( attendanceRecord == null )
                {
                    ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                    ViewState["ModalMove"] = null;
                    mdMove.Hide();
                    return;
                }

                ltMove.Text = string.Format( "{0} @ {1}", attendanceRecord.PersonAlias.Person.FullName, attendanceRecord.Occurrence.Schedule.Name );
                ltMoveInfo.Text = string.Format(
                    "{0} is currently in {1} > {2} at {3} for the schedule {4}",
                    attendanceRecord.PersonAlias.Person.NickName,
                    attendanceRecord.Occurrence.Group.GroupType.Name,
                    attendanceRecord.Occurrence.Group.Name,
                    attendanceRecord.Occurrence.Location.Name,
                    attendanceRecord.Occurrence.Schedule.Name );

                // If this person is not an approved volunteer generate a list of groups they cannot join
                List<int> forbiddenGroupIds = new List<int>();
                var approvedPeopleGuid = GetAttributeValue( "ApprovedPeople" ).AsGuid();
                var approvedPeople = new DataViewService( _rockContext ).Get( approvedPeopleGuid );
                var errorMessages = new List<string>();
                if ( approvedPeople != null )
                {
                    var approvedPeopleQry = approvedPeople.GetQuery( null, 30, out errorMessages );
                    if ( !approvedPeopleQry.Where( dv => dv.Id == attendanceRecord.PersonAlias.PersonId ).Any() )
                    {
                        forbiddenGroupIds = kioskCountUtility.VolunteerGroupIds;
                        ltMoveInfo.Text += "<br><i>" + attendanceRecord.PersonAlias.Person.NickName + " is not cleared to work with minors.</i>";
                    }
                }

                var groups = new GroupTypeService( _rockContext ).Queryable()
                     .Where( gt => CurrentCheckInState.ConfiguredGroupTypes.Contains( gt.Id ) )
                     .SelectMany( gt => gt.Groups )
                     .Where( g => g.IsActive )
                     .Where( g => !forbiddenGroupIds.Contains( g.Id ) )
                     .OrderBy( g => g.GroupType.Order )
                     .ThenBy( g => g.Order )
                     .Select( g => new
                     {
                         Id = g.Id,
                         Name = g.GroupType.Name + " > " + g.Name
                     } )
                     .ToList();

                ddlGroup.DataSource = groups;
                ddlGroup.DataValueField = "Id";
                ddlGroup.DataTextField = "Name";
                ddlGroup.DataBind();
                ddlGroup.Items.Insert( 0, new ListItem( "[DO NOT CHANGE CLASS]", "0" ) );

                BindLocationDropDown();
            }
        }

        private void ToggleLocation( int groupLocationId, int scheduleId )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var groupLocation = new GroupLocationService( _rockContext ).Get( groupLocationId );
                if ( groupLocation != null )
                {
                    var schedule = groupLocation.Schedules.Where( s => s.Id == scheduleId ).FirstOrDefault();
                    if ( schedule != null )
                    {
                        groupLocation.Schedules.Remove( schedule );
                        RecordGroupLocationSchedule( groupLocation, schedule );
                    }
                    else
                    {
                        schedule = new ScheduleService( _rockContext ).Get( scheduleId );
                        if ( schedule != null )
                        {
                            groupLocation.Schedules.Add( schedule );
                            RemoveDisabledGroupLocationSchedule( groupLocation, schedule );
                        }
                    }
                    _rockContext.SaveChanges();
                    Rock.CheckIn.KioskDevice.Clear();
                    CheckInCountCache.Flush();
                }
                BindTable();
            }
        }

        private void RemoveDisabledGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var definedTypeGuid = GetAttributeValue( "DeactivatedDefinedType" ).AsGuidOrNull();
                if ( definedTypeGuid == null )
                {
                    return;
                }
                var definedType = new DefinedTypeService( _rockContext ).Get( definedTypeGuid ?? new Guid() );
                var value = string.Format( "{0}|{1}", groupLocation.Id, schedule.Id );
                var definedValueService = new DefinedValueService( _rockContext );
                var definedValue = definedValueService.Queryable().Where( dv => dv.DefinedTypeId == definedType.Id && dv.Value == value ).FirstOrDefault();
                if ( definedValue == null )
                {
                    return;
                }

                Rock.Web.Cache.DefinedTypeCache.Remove( definedValue.DefinedTypeId );
                Rock.Web.Cache.DefinedValueCache.Remove( definedValue.Id );

                definedValueService.Delete( definedValue );
                _rockContext.SaveChanges();
            }
        }

        private void RecordGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var definedTypeGuid = GetAttributeValue( "DeactivatedDefinedType" ).AsGuidOrNull();
                if ( definedTypeGuid == null )
                {
                    return;
                }
                var definedType = new DefinedTypeService( _rockContext ).Get( definedTypeGuid ?? new Guid() );
                var definedValueService = new DefinedValueService( _rockContext );
                var value = groupLocation.Id.ToString() + "|" + schedule.Id.ToString();
                var definedValue = definedValueService.Queryable().Where( dv => dv.DefinedTypeId == definedType.Id && dv.Value == value ).FirstOrDefault();
                if ( definedValue != null )
                {
                    return;
                }

                definedValue = new DefinedValue() { Id = 0 };
                definedValue.Value = value;
                definedValue.Description = string.Format( "Deactivated {0} for schedule {1} at {2}", groupLocation.ToString(), schedule.Name, Rock.RockDateTime.Now.ToString() );
                definedValue.DefinedTypeId = definedType.Id;
                definedValue.IsSystem = false;
                var orders = definedValueService.Queryable()
                       .Where( d => d.DefinedTypeId == definedType.Id )
                       .Select( d => d.Order )
                       .ToList();

                definedValue.Order = orders.Any() ? orders.Max() + 1 : 0;

                definedValueService.Add( definedValue );

                Rock.Web.Cache.DefinedTypeCache.Remove( definedValue.DefinedTypeId );
                Rock.Web.Cache.DefinedValueCache.Remove( definedValue.Id );

                _rockContext.SaveChanges();
            }
        }

        private void CloseOccurrence( int groupLocationId, int scheduleId, bool bindTable = true )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var groupLocation = new GroupLocationService( _rockContext ).Get( groupLocationId );
                if ( groupLocation != null )
                {
                    var schedule = groupLocation.Schedules.Where( s => s.Id == scheduleId ).FirstOrDefault();
                    if ( schedule != null )
                    {
                        if ( groupLocation.Schedules.Contains( schedule ) )
                        {
                            RecordGroupLocationSchedule( groupLocation, schedule );
                            groupLocation.Schedules.Remove( schedule );
                            _rockContext.SaveChanges();
                            Rock.CheckIn.KioskDevice.Clear();
                            CheckInCountCache.Flush();
                            if ( bindTable )
                            {
                                BindTable();
                            }
                        }
                    }
                }
            }
        }

        protected void mdOccurrence_SaveClick( object sender, EventArgs e )
        {
            mdOccurrence.Hide();
            ViewState["ModalGroupLocation"] = null;
            ViewState["ModalGroupLocationSchedule"] = null;
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
        }

        protected void mdMove_CancelClick( object sender, EventArgs e )
        {
            mdMove.Hide();
            ViewState["ModalMove"] = null;
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
        }

        protected void btnMove_Click( object sender, EventArgs e )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                int id = ( int ) ViewState["ModalMove"];
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                ViewState["ModalMove"] = null;
                mdMove.Hide();
                var attendanceRecord = attendanceService.Get( id );
                if ( attendanceRecord == null )
                {
                    return;
                }

                var newGroupId = ddlGroup.SelectedValue.AsInteger();
                var newLocationId = ddlLocation.SelectedValue.AsInteger();

                if ( newGroupId != 0 || newLocationId != 0 )
                {
                    //Create a new attendance record
                    Attendance newRecord = ( Attendance ) attendanceRecord.Clone();
                    newRecord.Id = 0;
                    newRecord.Guid = new Guid();
                    newRecord.StartDateTime = Rock.RockDateTime.Now;
                    newRecord.EndDateTime = null;
                    newRecord.Device = null;
                    newRecord.AttendanceCodeId = null;
                    newRecord.AttendanceCode = null;
                    newRecord.SearchTypeValue = null;

                    if ( newGroupId != 0 )
                    {
                        newRecord.Occurrence.GroupId = newGroupId;
                    }
                    if ( newLocationId != 0 )
                    {
                        newRecord.Occurrence.LocationId = newLocationId;
                    }

                    //Close all other attendance records for this person today at this schedule
                    var currentRecords = attendanceService.Queryable()
                         .Where( a =>
                         a.StartDateTime >= Rock.RockDateTime.Today
                        && a.PersonAliasId == attendanceRecord.PersonAliasId
                        && a.Occurrence.ScheduleId == attendanceRecord.Occurrence.ScheduleId
                        && a.EndDateTime == null ).ToList();

                    foreach ( var record in currentRecords )
                    {
                        record.EndDateTime = Rock.RockDateTime.Now;
                        record.DidAttend = false;
                    }

                    attendanceService.Add( newRecord );
                    attendanceRecord.DidAttend = false;
                    _rockContext.SaveChanges();
                }
            }
            CheckInCountCache.Flush();
            BindTable();
            RebuildModal();
        }

        protected void ddlSchedules_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlSchedules.SelectedValue.AsInteger() > 0 )
            {
                ddlClose.Visible = true;
                ddlClose.Title = string.Format( "Close locations for {0}", ddlSchedules.SelectedItem.Text );
            }
            else
            {
                ddlClose.Visible = false;
            }
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            ViewState["LocationRatios"] = null;
            KioskDevice.Clear();
            CheckInCountCache.Flush();
            BindTable();
        }

        protected void mdLocation_SaveClick( object sender, EventArgs e )
        {
            using ( RockContext _rockContext = new RockContext() )
            {

                LocationService locationService = new LocationService( _rockContext );
                var location = locationService.Get( hfLocationId.ValueAsInt() );
                if ( location == null )
                {
                    return;
                }
                location.LoadAttributes();
                location.SetAttributeValue( GetAttributeValue( "RoomRatioAttributeKey" ), tbRatio.Text.AsInteger() );
                location.SaveAttributeValues();
                location.FirmRoomThreshold = tbFirmThreshold.Text.AsInteger();
                location.SoftRoomThreshold = tbSoftThreshold.Text.AsInteger();
                _rockContext.SaveChanges();
                mdLocation.Hide();
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                Rock.CheckIn.KioskDevice.Clear();
                CheckInCountCache.Flush();
            }
            ViewState["LocationRatios"] = null;
            BindTable();
        }

        protected void mdSearch_SaveClick( object sender, EventArgs e )
        {
            ViewState["SearchType"] = null;
            ltLocation.Text = "";
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            mdSearch.Hide();
        }

        protected void btnCodeSearch_Click( object sender, EventArgs e )
        {
            ViewState["SearchType"] = SearchType.Code;
            SearchByCode();
        }

        private void SearchByCode()
        {
            var code = tbSearch.Text.ToUpper();
            if ( string.IsNullOrWhiteSpace( code ) )
            {
                return;
            }
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceCodeService attendanceCodeService = new AttendanceCodeService( _rockContext );
                var attendanceCode = attendanceCodeService.Queryable( "Attendance,Attendance.Occurrence" )
                    .Where( ac =>
                        ac.Code == code && ac.IssueDateTime >= Rock.RockDateTime.Today
                    )
                    .FirstOrDefault();
                if ( attendanceCode == null )
                {
                    ltSearch.Text = "Attendance matching code not found";
                    return;
                }
                var attendanceRecords = attendanceCode.Attendances
                    .Where( a =>
                        a.EndDateTime == null
                        && a.Occurrence.ScheduleId != null
                        && a.AttendanceCodeId != null
                        && a.Occurrence.LocationId != null
                    )
                    .ToList();
                DisplaySearchRecords( attendanceRecords );
            }
        }

        protected void btnNameSearch_Click( object sender, EventArgs e )
        {
            ViewState["SearchType"] = SearchType.Name;
            SearchByName();
        }
        private void SearchByName()
        {
            var name = tbSearch.Text;
            if ( string.IsNullOrWhiteSpace( name ) )
            {
                return;
            }
            using ( RockContext _rockContext = new RockContext() )
            {
                var people = new PersonService( _rockContext ).GetByFullName( name, true );
                if ( !people.Any() )
                {
                    ltSearch.Text = "Could not find person's attendance record.";
                    return;
                }
                var aliasIds = people.ToList().Select( p => p.PrimaryAliasId );
                var attendanceRecords = new AttendanceService( _rockContext ).Queryable( "AttendanceCode,Occurrence" )
                    .Where( a =>
                        a.StartDateTime >= Rock.RockDateTime.Today
                        && a.EndDateTime == null
                        && aliasIds.Contains( a.PersonAliasId )
                        && a.Occurrence.ScheduleId != null
                        && a.AttendanceCodeId != null
                        && a.Occurrence.LocationId != null
                    )
                    .ToList();
                if ( !attendanceRecords.Any() )
                {
                    ltSearch.Text = "Could not find person's attendance record.";
                    return;
                }
                DisplaySearchRecords( attendanceRecords );
            }
        }

        private void DisplaySearchRecords( List<Attendance> attendanceRecords )
        {
            ltSearch.Text = "";
            phSearchResults.Controls.Clear();

            Table table = new Table();
            table.CssClass = "table";
            phSearchResults.Controls.Add( table );

            TableHeaderRow trHeader = new TableHeaderRow();
            table.Controls.Add( trHeader );

            TableHeaderCell thcName = new TableHeaderCell();
            thcName.Text = "Name";
            trHeader.Controls.Add( thcName );

            TableHeaderCell thcCode = new TableHeaderCell();
            thcCode.Text = "Code";
            trHeader.Controls.Add( thcCode );

            TableHeaderCell thcDevice = new TableHeaderCell();
            thcDevice.Text = "Kiosk";
            trHeader.Controls.Add( thcDevice );

            TableHeaderCell thcLocation = new TableHeaderCell();
            thcLocation.Text = "Location";
            trHeader.Controls.Add( thcLocation );

            TableHeaderCell thcSchedule = new TableHeaderCell();
            thcSchedule.Text = "Schedule";
            trHeader.Controls.Add( thcSchedule );

            TableHeaderCell thcButtons = new TableHeaderCell();
            trHeader.Controls.Add( thcButtons );

            foreach ( var attendance in attendanceRecords )
            {
                TableRow trRow = new TableRow();
                table.Controls.Add( trRow );

                TableCell tcName = new TableCell();
                tcName.Text = attendance.PersonAlias.Person.FullName;
                trRow.Controls.Add( tcName );

                TableCell tcCode = new TableCell();
                if ( attendance.AttendanceCode != null && attendance.AttendanceCode.Code != null )
                {
                    tcCode.Text = attendance.AttendanceCode.Code;
                }
                trRow.Controls.Add( tcCode );

                TableCell tcDevice = new TableCell();
                if ( attendance.Device != null )
                {
                    tcDevice.Text = attendance.Device.Name;
                }
                trRow.Controls.Add( tcDevice );

                TableCell tcLocation = new TableCell();
                if ( attendance.Occurrence.Location != null )
                {
                    tcLocation.Text = attendance.Occurrence.Location.Name;
                }
                trRow.Controls.Add( tcLocation );

                TableCell tcSchedule = new TableCell();
                if ( attendance.Occurrence.Schedule != null )
                {
                    tcSchedule.Text = attendance.Occurrence.Schedule.Name;
                }
                trRow.Controls.Add( tcSchedule );

                TableCell tcButtons = new TableCell();
                trRow.Controls.Add( tcButtons );

                if ( attendance.EndDateTime == null )
                {
                    BootstrapButton btnSearchMove = new BootstrapButton();
                    btnSearchMove.ID = string.Format( "btnSearchMove{0}", attendance.Id );
                    btnSearchMove.Text = "Move";
                    btnSearchMove.CssClass = "btn btn-xs btn-warning";
                    btnSearchMove.Click += ( s, e ) => { MoveModal( attendance.Id ); };
                    tcButtons.Controls.Add( btnSearchMove );
                }

                if ( attendance.DidAttend == false && attendance.EndDateTime == null )
                {
                    BootstrapButton btnCancel = new BootstrapButton();
                    btnCancel.ID = string.Format( "btnSearchCancel{0}", attendance.Id );
                    btnCancel.Text = "Cancel";
                    btnCancel.CssClass = "btn btn-xs btn-danger";
                    btnCancel.Click += ( s, e ) => { CancelReservation( attendance.Id ); };
                    tcButtons.Controls.Add( btnCancel );
                }

                if ( attendance.DidAttend == true && attendance.EndDateTime == null )
                {
                    BootstrapButton btnCheckout = new BootstrapButton();
                    btnCheckout.ID = string.Format( "btnSearchCheckout{0}", attendance.Id );
                    btnCheckout.Text = "Checkout";
                    btnCheckout.CssClass = "btn btn-xs btn-danger";
                    btnCheckout.Click += ( s, e ) => { Checkout( attendance.Id ); };
                    tcButtons.Controls.Add( btnCheckout );
                }
            }
        }

        protected void btnSearch_Click( object sender, EventArgs e )
        {
            tbSearch.Text = "";
            ltSearch.Text = "";
            phSearchResults.Controls.Clear();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            mdSearch.Show();
        }

        enum SearchType
        {
            Code,
            Name
        }

        protected void mdConfirmClose_SaveClick( object sender, EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            var scheduleId = ddlSchedules.SelectedValue.AsInteger();

            var groupIds = new GroupTypeService( new RockContext() ).Queryable()
                     .Where( gt => CurrentCheckInState.ConfiguredGroupTypes.Contains( gt.Id ) )
                     .SelectMany( gt => gt.Groups )
                     .Where( g => g.IsActive )
                     .Select( g => g.Id )
                     .ToList();

            List<int> closeGroupIds;

            switch ( ddlClose.SelectedValueAsEnum<CloseScope>() )
            {
                case CloseScope.Children:
                    closeGroupIds = groupIds.Where( i => !kioskCountUtility.VolunteerGroupIds.Contains( i ) ).ToList();
                    break;
                case CloseScope.Volunteer:
                    closeGroupIds = kioskCountUtility.VolunteerGroupIds;
                    break;
                case CloseScope.All:
                    closeGroupIds = groupIds;
                    break;
                default:
                    closeGroupIds = groupIds;
                    break;
            }

            foreach ( var gls in kioskCountUtility.GroupLocationSchedules.Where( gls => gls.Schedule.Id == scheduleId ) )
            {
                if ( closeGroupIds.Contains( gls.GroupLocation.GroupId ) )
                {
                    CloseOccurrence( gls.GroupLocation.Id, scheduleId, false );
                }
            }
            mdConfirmClose.Hide();
            BindTable();
            ddlClose.SelectedValue = null;
        }

        protected void ddlGroup_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindLocationDropDown();
        }

        private void BindLocationDropDown()
        {
            var selectedGroupId = ddlGroup.SelectedValue.AsInteger();

            if ( selectedGroupId == 0 )
            {
                var locations = new GroupTypeService( new RockContext() ).Queryable()
                     .Where( gt => CurrentCheckInState.ConfiguredGroupTypes.Contains( gt.Id ) )
                    .SelectMany( gt => gt.Groups )
                    .SelectMany( g => g.GroupLocations )
                    .Select( gl => gl.Location )
                    .DistinctBy( l => l.Id )
                    .OrderBy( l => l.Name )
                    .ToList();

                ddlLocation.DataSource = locations;
                ddlLocation.DataValueField = "Id";
                ddlLocation.DataTextField = "Name";
                ddlLocation.DataBind();
                ddlLocation.Items.Insert( 0, new ListItem( "[DO NOT CHANGE LOCATION]", "0" ) );

            }
            else
            {
                var locations = new GroupService( new RockContext() )
                    .Get( selectedGroupId )
                    .GroupLocations
                    .Select( gl => gl.Location )
                    .OrderBy( l => l.Name )
                    .ToList();

                ddlLocation.DataSource = locations;
                ddlLocation.DataValueField = "Id";
                ddlLocation.DataTextField = "Name";
                ddlLocation.DataBind();
                ddlLocation.Items.Insert( 0, new ListItem( "[DO NOT CHANGE LOCATION]", "0" ) );
            }
        }

        protected void ddlClose_SelectionChanged( object sender, EventArgs e )
        {
            ltConfirmClose.Text = "Are you sure you want to close these locations?";
            mdConfirmClose.Show();
        }

        enum CloseScope
        {
            Children,
            Volunteer,
            All
        }

    }
}