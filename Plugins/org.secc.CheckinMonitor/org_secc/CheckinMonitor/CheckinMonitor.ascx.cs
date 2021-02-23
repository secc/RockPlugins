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
using System.Web.UI;
using System.Web.UI.WebControls;
using Humanizer;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Check-In Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Helps manage rooms and room ratios." )]
    [TextField( "Room Ratio Attribute Key", "Attribute key for room ratios", true, "RoomRatio" )]
    [DataViewField( "Approved People", "Data view which contains the members who may check-in.", entityTypeName: "Rock.Model.Person" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Location Label", "Label to use to for printing a location label." )]

    public partial class CheckinMonitor : CheckInBlock
    {

        KioskTypeCache KioskType = null;
        private static class ViewStateKeys
        {
            internal const string ModalLocationId = "ModalLocationId";
            internal const string ModalScheduleId = "ModalScheduleId";
            internal const string ModalLocation = "ModalLocation";
            internal const string MinimizedGroupTypes = "MinimizedGroupTypes";
            internal const string SearchType = "SearchType";
            internal const string ModalMove = "ModalMove";
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );
            var kioskTypeCookie = this.Page.Request.Cookies["KioskTypeId"];
            if ( kioskTypeCookie != null )
            {
                KioskType = KioskTypeCache.Get( kioskTypeCookie.Value.AsInteger() );
            }

            if ( KioskType == null )
            {
                NavigateToHomePage();
            }
        }

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
                ViewState[ViewStateKeys.MinimizedGroupTypes] = new List<int>();
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

            var schedules = OccurrenceCache.All()
                .Where( o => CurrentCheckInState.ConfiguredGroupTypes.Contains( o.GroupTypeId ) )
                .DistinctBy( o => o.ScheduleId )
                .OrderBy( o => o.ScheduleStartTime )
                .Select( o => new { Id = o.ScheduleId, Name = o.ScheduleName } )
                .ToList();

            schedules.Insert( 0, new { Id = -1, Name = "[ All Schedules ]" } );
            schedules.Insert( 0, new { Id = 0, Name = "[ Active Schedules ]" } );

            ddlSchedules.DataSource = schedules;
            ddlSchedules.DataBind();

            ddlClose.BindToEnum<CloseScope>();
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            var selectedScheduleId = ddlSchedules.SelectedValue.AsInteger();
            List<OccurrenceCache> occurrences = OccurrenceCache.All()
                .Where( o => CurrentCheckInState.ConfiguredGroupTypes.Contains( o.GroupTypeId ) )
                .ToList();

            if ( selectedScheduleId > 0 )
            {
                occurrences = occurrences.Where( o => o.ScheduleId == selectedScheduleId ).ToList();
            }
            else if ( selectedScheduleId == 0 )
            {
                //Use the kiosk type to get the active schedules
                var activeScheduleIds = KioskType.CheckInSchedules
                    .Where( s => s.WasScheduleOrCheckInActive( Rock.RockDateTime.Now ) )
                    .Select( s => s.Id )
                    .ToList();

                occurrences = occurrences.Where( o => activeScheduleIds.Contains( o.ScheduleId ) ).ToList();
            }

            List<GroupTypeCache> groupTypes = new List<GroupTypeCache>();

            foreach ( var id in CurrentCheckInState.ConfiguredGroupTypes )
            {
                var gtc = GroupTypeCache.Get( id );
                if ( gtc != null )
                {
                    groupTypes.Add( gtc );
                }
            }

            groupTypes = groupTypes.OrderBy( gt => gt.Order ).ToList();


            foreach ( var groupType in groupTypes )
            {
                if ( ViewState[ViewStateKeys.MinimizedGroupTypes] != null
                    && ( ( List<int> ) ViewState[ViewStateKeys.MinimizedGroupTypes] ).Contains( groupType.Id ) )
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

                var groupOccurrences = occurrences.Where( o => o.GroupTypeId == groupType.Id )
                    .DistinctBy( o => o.GroupId )
                    .OrderBy( o => o.GroupOrder )
                    .ToList();


                foreach ( var groupOccurrence in groupOccurrences )
                {

                    Literal ltG = new Literal();
                    phContent.Controls.Add( ltG );

                    TableHeaderRow thr = new TableHeaderRow();
                    table.Controls.Add( thr );

                    TableHeaderCell thcName = new TableHeaderCell();
                    thcName.Text = groupOccurrence.GroupName;
                    thcName.Style.Add( "width", "30%" );
                    thr.Controls.Add( thcName );


                    TableHeaderCell thcRatio = new TableHeaderCell();
                    thcRatio.Text = "Kid/Adult Ratio";
                    thcRatio.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcRatio );

                    TableHeaderCell thcCount = new TableHeaderCell();
                    thcCount.Text = " Kid / Total Count";
                    thcCount.Style.Add( "width", "25%" );
                    thr.Controls.Add( thcCount );

                    TableHeaderCell thcToggle = new TableHeaderCell();
                    thcToggle.Style.Add( "width", "15%" );
                    thr.Controls.Add( thcToggle );

                    TableHeaderCell thcRoom = new TableHeaderCell();
                    thcRoom.Style.Add( "width", "10%" );
                    thr.Controls.Add( thcRoom );

                    var locationOccurrences = occurrences.Where( o => o.GroupId == groupOccurrence.GroupId )
                    .OrderBy( o => o.GroupLocationOrder )
                    .ThenBy( o => o.ScheduleStartTime )
                    .ToList();

                    foreach ( var locationOccurrence in locationOccurrences )
                    {
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        if ( selectedScheduleId > 0 )
                        {
                            name.Text = locationOccurrence.LocationName;
                        }
                        else
                        {
                            name.Text = locationOccurrence.LocationName + " @ " + locationOccurrence.ScheduleName;
                        }
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        int ratio = locationOccurrence.RoomRatio ?? 0;

                        var attendances = AttendanceCache.GetByLocationIdAndScheduleId( locationOccurrence.LocationId, locationOccurrence.ScheduleId );
                        var volunteerCount = attendances.Count( a => a.IsVolunteer );
                        var childCount = attendances.Count( a => !a.IsVolunteer );
                        var totalCount = attendances.Count;
                        var reservedCount = attendances.Count( a => a.AttendanceState == AttendanceState.EnRoute || a.AttendanceState == AttendanceState.MobileReserve );

                        var ratioDistance = ( volunteerCount * ratio ) - childCount;
                        tcRatio.Text = string.Format( "{0}/{1}", childCount, volunteerCount );
                        if ( totalCount != 0 && ratioDistance == 0 )
                        {
                            tcRatio.Text += " [Room at ratio]";
                            tcRatio.CssClass = "warning";
                        }
                        else if ( totalCount != 0 && ratioDistance < 0 )
                        {
                            tcRatio.Text += " [" + ( ratioDistance * -1 ).ToString() + " over ratio]";
                            tcRatio.CssClass = "danger";
                        }
                        else if ( ratioDistance < 4 && totalCount != 0 )
                        {

                            tcRatio.CssClass = "info";
                            tcRatio.Text += " [" + ratioDistance.ToString() + " to ratio]";
                        }
                        else if ( attendances.Count != 0 )
                        {
                            tcRatio.CssClass = "success";
                            tcRatio.Text += " [" + ratioDistance.ToString() + " to ratio]";
                        }

                        tr.Controls.Add( tcRatio );

                        TableCell tcCapacity = new TableCell();
                        if ( ( totalCount + reservedCount ) != 0
                            && ( totalCount + reservedCount ) >= ( locationOccurrence.FirmRoomThreshold ?? int.MaxValue ) )
                        {
                            if ( locationOccurrence.IsActive && totalCount != 0
                                && totalCount >= ( locationOccurrence.FirmRoomThreshold ?? int.MaxValue ) )
                            {
                                CloseOccurrence( locationOccurrence.GroupLocationId, locationOccurrence.ScheduleId );
                                KioskTypeCache.ClearForTemplateId( LocalDeviceConfig.CurrentCheckinTypeId ?? 0 );
                                KioskDeviceHelpers.Clear( CurrentCheckInState.ConfiguredGroupTypes );
                                return;
                            }
                            tcCapacity.CssClass = "danger";
                        }
                        else if ( ( totalCount + reservedCount ) != 0
                            && (
                                  ( ( totalCount + reservedCount ) + 3 ) >= ( locationOccurrence.FirmRoomThreshold ?? int.MaxValue )
                                    || ( childCount + 3 ) >= ( locationOccurrence.SoftRoomThreshold ?? int.MaxValue - 3 ) + 3 ) )
                        {
                            tcCapacity.CssClass = "warning";
                        }
                        else
                        {
                            tcCapacity.CssClass = "success";
                        }
                        tcCapacity.Text = string.Format( "{0} of {1} / {2} of {3} {4}",
                                            childCount,
                                            locationOccurrence.SoftRoomThreshold ?? int.MaxValue,
                                            totalCount,
                                            locationOccurrence.FirmRoomThreshold ?? int.MaxValue,
                                            reservedCount > 0 ? "/ (including " + reservedCount + " en route)" : "" );

                        tr.Controls.Add( tcCapacity );

                        TableCell tcToggle = new TableCell();
                        tr.Controls.Add( tcToggle );

                        BootstrapButton btnToggle = new BootstrapButton();
                        btnToggle.ID = string.Format( "btnL{0}_{1}_{2}", locationOccurrence.GroupId, locationOccurrence.GroupLocationId, locationOccurrence.ScheduleId );
                        btnToggle.Click += ( s, ee ) => { ToggleLocation( locationOccurrence.GroupLocationId, locationOccurrence.ScheduleId ); };
                        if ( locationOccurrence.IsActive )
                        {
                            btnToggle.Text = "Active";
                            btnToggle.ToolTip = "Click to deactivate.";
                            btnToggle.CssClass = "btn btn-sm btn-success btn-block";
                            btnToggle.DataLoadingText = "Updating...";
                        }
                        else
                        {
                            btnToggle.Text = "Inactive";
                            btnToggle.ToolTip = "Click to activate.";
                            btnToggle.CssClass = "btn btn-sm btn-danger btn-block";
                            btnToggle.DataLoadingText = "Updating...";
                        }
                        tcToggle.Controls.Add( btnToggle );

                        TableCell tcLocation = new TableCell();
                        tr.Controls.Add( tcLocation );

                        LinkButton btnOccurrence = new LinkButton();
                        btnOccurrence.ID = string.Format( "btn_{0}_{1}", locationOccurrence.GroupLocationId, locationOccurrence.ScheduleId );
                        btnOccurrence.Text = "<i class='fa fa-users'></i>";
                        btnOccurrence.CssClass = "btn btn-sm btn-default";
                        btnOccurrence.Click += ( s, e ) => { OccurrenceModal( locationOccurrence.LocationId, locationOccurrence.ScheduleId ); };
                        btnOccurrence.Attributes.Add( "data-loading-text", "<i class='fa fa-refresh fa-spin working'></i>" );
                        btnOccurrence.OnClientClick = "Rock.controls.bootstrapButton.showLoading(this);stopTimer();";
                        tcLocation.Controls.Add( btnOccurrence );

                        LinkButton btnLocation = new LinkButton();
                        btnLocation.ID = string.Format( "btnLocation_{0}_{1}", locationOccurrence.GroupLocationId, locationOccurrence.ScheduleId );
                        btnLocation.Text = "<i class='fa fa-map-marker'></i>";
                        btnLocation.CssClass = "btn btn-sm btn-default";
                        btnLocation.Click += ( s, e ) => { ShowLocationModal( locationOccurrence.LocationId ); };
                        btnLocation.Attributes.Add( "data-loading-text", "<i class='fa fa-refresh fa-spin working'></i>" );
                        btnLocation.OnClientClick = "Rock.controls.bootstrapButton.showLoading(this);stopTimer();";
                        tcLocation.Controls.Add( btnLocation );
                    }
                }
            }
        }

        private void MinimizeGroupType( int groupTypeId )
        {
            if ( ViewState[ViewStateKeys.MinimizedGroupTypes] == null )
            {
                ViewState["MinimizedGroupTypes"] = new List<int>();
            }
            var minimizedGroupTypes = ( List<int> ) ViewState[ViewStateKeys.MinimizedGroupTypes];
            if ( !minimizedGroupTypes.Contains( groupTypeId ) )
            {
                minimizedGroupTypes.Add( groupTypeId );
            }
            ViewState[ViewStateKeys.MinimizedGroupTypes] = minimizedGroupTypes;
            BindTable();
        }

        private void MaximizeGroupType( int groupTypeId )
        {
            if ( ViewState[ViewStateKeys.MinimizedGroupTypes] == null )
            {
                ViewState["MinimizedGroupTypes"] = new List<int>();
            }
            var minimizedGroupTypes = ( List<int> ) ViewState[ViewStateKeys.MinimizedGroupTypes];
            while ( minimizedGroupTypes.Contains( groupTypeId ) )
            {
                minimizedGroupTypes.Remove( groupTypeId );
            }
            ViewState[ViewStateKeys.MinimizedGroupTypes] = minimizedGroupTypes;
            BindTable();
        }

        private void ShowLocationModal( int locationId )
        {
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            RockContext rockContext = new RockContext();
            LocationService locationService = new LocationService( rockContext );

            var location = locationService.Get( locationId );

            ltLocationName.Text = location.Name;
            hfLocationId.Value = location.Id.ToString();
            location.LoadAttributes();

            var roomRatio = location.GetAttributeValue( Constants.LOCATION_ATTRIBUTE_ROOM_RATIO );
            tbRatio.Text = roomRatio;
            tbFirmThreshold.Text = location.FirmRoomThreshold.ToString();
            tbSoftThreshold.Text = location.SoftRoomThreshold.ToString();
            mdLocation.Show();
        }

        private void RebuildModal()
        {
            if ( ViewState[ViewStateKeys.ModalLocationId] != null && ViewState[ViewStateKeys.ModalScheduleId] != null )
            {
                using ( RockContext _rockContext = new RockContext() )
                {
                    var locationId = ( int ) ViewState[ViewStateKeys.ModalLocationId];
                    var scheduleId = ( int ) ViewState[ViewStateKeys.ModalScheduleId];
                    var location = new LocationService( _rockContext ).Get( locationId );
                    var schedule = new ScheduleService( _rockContext ).Get( scheduleId );
                    if ( location != null && schedule != null )
                    {
                        OccurrenceModal( locationId, scheduleId );
                        return;
                    }
                }
            }
            else if ( ViewState[ViewStateKeys.SearchType] != null )
            {
                var searchBy = ( SearchType ) ViewState[ViewStateKeys.SearchType];
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

        private void OccurrenceModal( int locationId, int scheduleId )
        {
            phLocation.Controls.Clear();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            mdOccurrence.Show();

            var attendances = AttendanceCache.GetByLocationIdAndScheduleId( locationId, scheduleId );

            var current = attendances.Where( a => a.AttendanceState == AttendanceState.InRoom ).OrderByDescending( a => a.IsVolunteer ).ToList();
            var reserved = attendances.Where( a => a.AttendanceState == AttendanceState.EnRoute || a.AttendanceState == AttendanceState.MobileReserve ).ToList();

            if ( !attendances.Any() )
            {
                ltLocation.Text = "There are currently no attendance records for this location today.";
            }
            else
            {
                var occurrance = OccurrenceCache.Get( attendances.FirstOrDefault().OccurrenceAccessKey );
                if ( occurrance == null )
                {
                    //There is no group location schedule combination that matches this first attendance,
                    //we'll load the title manually
                    var attendance = attendances.FirstOrDefault();
                    RockContext rockContext = new RockContext();
                    var location = new LocationService( rockContext ).Get( attendance.LocationId ?? 0 );
                    var schedule = new ScheduleService( rockContext ).Get( attendance.ScheduleId ?? 0 );

                    if ( location != null && schedule != null )
                    {
                        ltLocation.Text = string.Format( "{0} @ {1}", location.Name, schedule.Name );
                    }
                }
                else
                {
                    ltLocation.Text = string.Format( "{0} @ {1}", occurrance.LocationName, occurrance.ScheduleName );
                }
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
                        tcName.Text = record.PersonName;
                        trRecord.Controls.Add( tcName );
                        tcName.Style.Add( "width", "30%" );

                        TableCell tcGroup = new TableCell();
                        trRecord.Controls.Add( tcGroup );
                        tcGroup.Style.Add( "width", "30%" );
                        var occurrence = OccurrenceCache.Get( record.OccurrenceAccessKey );
                        if ( occurrence != null )
                        {
                            tcGroup.Text = occurrence.GroupName;
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
                        if ( record.AttendanceState == AttendanceState.MobileReserve )
                        {
                            tcName.Text = "<i class='fa fa-mobile'></i> " + record.PersonName + " (" + record.CreatedDateTime.Humanize( true, Rock.RockDateTime.Now ) + ")";
                        }
                        else
                        {
                            tcName.Text = record.PersonName;
                        }
                        trRecord.Controls.Add( tcName );
                        tcName.Style.Add( "width", "30%" );

                        TableCell tcGroup = new TableCell();
                        trRecord.Controls.Add( tcGroup );
                        tcGroup.Style.Add( "width", "30%" );
                        var occurrence = OccurrenceCache.Get( record.OccurrenceAccessKey );
                        if ( occurrence != null )
                        {
                            tcGroup.Text = occurrence.GroupName;
                        }

                        TableCell tcSchedule = new TableCell();
                        trRecord.Controls.Add( tcSchedule );
                        tcSchedule.Style.Add( "width", "25%" );
                        if ( occurrance != null )
                        {
                            tcSchedule.Text = occurrance.ScheduleName;
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

                        if ( record.CanClose )
                        {
                            LinkButton btnCancel = new LinkButton();
                            btnCancel.ID = "cancel" + record.Id.ToString();
                            btnCancel.CssClass = "btn btn-danger btn-xs";
                            btnCancel.Text = "Cancel";
                            if ( record.AttendanceState == AttendanceState.MobileReserve )
                            {
                                btnCancel.OnClientClick = "Rock.dialogs.confirmPreventOnCancel(event, 'Are you sure you want to delete this attendance? It is an active mobile checkin and will remove all connected attendances.')";
                            }
                            else
                            {
                                btnCancel.OnClientClick = "Rock.dialogs.confirmPreventOnCancel(event, 'Are you sure you want to delete this attendance?')";
                            }
                            btnCancel.Click += ( s, e ) => { CancelReservation( record.Id ); };
                            tcButtons.Controls.Add( btnCancel );
                        }
                    }
                }
            }
            ViewState[ViewStateKeys.ModalLocationId] = locationId;
            ViewState[ViewStateKeys.ModalScheduleId] = scheduleId;
        }

        private void CancelReservation( int id )
        {
            var attendanceCache = AttendanceCache.Get( id );

            if ( attendanceCache.AttendanceState == AttendanceState.MobileReserve )
            {
                var record = MobileCheckinRecordCache.GetByAttendanceId( id );
                if ( record != null ) //Unlikely but happened in testing
                {
                    MobileCheckinRecordCache.CancelReservation( record );
                    RebuildModal();
                    return;
                }
            }

            using ( RockContext rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( rockContext );
                var attendance = attendanceService.Get( id );
                if ( attendance != null )
                {
                    attendance.EndDateTime = Rock.RockDateTime.Now;
                    rockContext.SaveChanges();
                    AttendanceCache.AddOrUpdate( attendance );
                    RebuildModal();
                }
            }
        }

        private void Checkout( int id )
        {
            using ( RockContext rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( rockContext );
                var attendance = attendanceService.Get( id );
                if ( attendance != null )
                {
                    attendance.EndDateTime = Rock.RockDateTime.Now;
                    rockContext.SaveChanges();
                    AttendanceCache.AddOrUpdate( attendance );
                    RebuildModal();
                }
            }
        }

        private void MoveModal( int attendanceId )
        {
            ViewState[ViewStateKeys.ModalLocationId] = null;
            ViewState[ViewStateKeys.ModalScheduleId] = null;
            ViewState[ViewStateKeys.ModalLocation] = null;
            mdMove.Show();
            ViewState[ViewStateKeys.ModalMove] = attendanceId;

            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var attendanceRecord = attendanceService.Get( attendanceId );
                if ( attendanceRecord == null )
                {
                    ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                    ViewState[ViewStateKeys.ModalMove] = null;
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
                        forbiddenGroupIds = OccurrenceCache.GetVolunteerOccurrences().Select( o => o.GroupId ).ToList();
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
                    var occurrence = OccurrenceCache.All()
                        .Where( o => o.GroupLocationId == groupLocationId && o.ScheduleId == scheduleId )
                        .FirstOrDefault();
                    var schedule = groupLocation.Schedules.Where( s => s.Id == scheduleId ).FirstOrDefault();
                    if ( schedule != null )
                    {
                        groupLocation.Schedules.Remove( schedule );
                        RecordGroupLocationSchedule( groupLocation, schedule );
                        occurrence.IsActive = false;
                    }
                    else
                    {
                        schedule = new ScheduleService( _rockContext ).Get( scheduleId );
                        if ( schedule != null )
                        {
                            groupLocation.Schedules.Add( schedule );
                            RemoveDisabledGroupLocationSchedule( groupLocation, schedule );
                            occurrence.IsActive = true;
                        }
                    }
                    _rockContext.SaveChanges();

                    KioskTypeCache.ClearForTemplateId( LocalDeviceConfig.CurrentCheckinTypeId ?? 0 );
                    KioskDeviceHelpers.Clear( CurrentCheckInState.ConfiguredGroupTypes );
                    OccurrenceCache.AddOrUpdate( occurrence );
                }
                BindTable();
            }
        }

        private void RemoveDisabledGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
        {
            using ( RockContext _rockContext = new RockContext() )
            {

                var definedType = new DefinedTypeService( _rockContext ).Get( Constants.DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES.AsGuid() );
                var value = string.Format( "{0}|{1}", groupLocation.Id, schedule.Id );
                var definedValueService = new DefinedValueService( _rockContext );
                var definedValue = definedValueService.Queryable().Where( dv => dv.DefinedTypeId == definedType.Id && dv.Value == value ).FirstOrDefault();
                if ( definedValue == null )
                {
                    return;
                }

                definedValueService.Delete( definedValue );
                _rockContext.SaveChanges();
            }
        }

        private void RecordGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var definedType = new DefinedTypeService( _rockContext ).Get( Constants.DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES.AsGuid() );
                var definedValueService = new DefinedValueService( _rockContext );
                var value = groupLocation.Id.ToString() + "|" + schedule.Id.ToString();

                var definedValue = new DefinedValue() { Id = 0 };
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
                            var occurrence = OccurrenceCache.All().Where( o => o.GroupLocationId == groupLocationId && o.ScheduleId == scheduleId ).FirstOrDefault();
                            occurrence.IsActive = false;
                            OccurrenceCache.AddOrUpdate( occurrence );

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
            ViewState[ViewStateKeys.ModalLocationId] = null;
            ViewState[ViewStateKeys.ModalScheduleId] = null;
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
        }

        protected void mdMove_CancelClick( object sender, EventArgs e )
        {
            mdMove.Hide();
            ViewState[ViewStateKeys.ModalMove] = null;
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
        }

        protected void btnMove_Click( object sender, EventArgs e )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                int id = ( int ) ViewState[ViewStateKeys.ModalMove];
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                ViewState[ViewStateKeys.ModalMove] = null;
                mdMove.Hide();
                var attendanceRecord = attendanceService.Get( id );
                if ( attendanceRecord == null )
                {
                    return;
                }

                var newGroupId = ddlGroup.SelectedValue.AsInteger();
                var newLocationId = ddlLocation.SelectedValue.AsInteger();
                var groupId = attendanceRecord.Occurrence.GroupId;
                var locationId = attendanceRecord.Occurrence.LocationId;

                if ( newGroupId != 0 )
                {
                    groupId = newGroupId;
                }
                if ( newLocationId != 0 )
                {
                    locationId = newLocationId;
                }

                int? mobileCheckinRecordId = null;

                if ( groupId != 0 && locationId != 0 )
                {
                    if ( attendanceRecord.Qualifier != null )
                    {
                        var mobileRecord = MobileCheckinRecordCache.All()
                            .Where( r => r.Status == org.secc.FamilyCheckin.Model.MobileCheckinStatus.Active )
                            .Where( r => r.AttendanceIds.Contains( attendanceRecord.Id ) ).FirstOrDefault();
                        if ( mobileRecord != null )
                        {
                            mobileCheckinRecordId = mobileRecord.Id;
                        }
                    }

                    AttendanceOccurrence occurrence = GetOccurrence( _rockContext, attendanceRecord.StartDateTime, groupId, locationId, attendanceRecord.Occurrence.ScheduleId );

                    var newRecord = new Attendance
                    {
                        Occurrence = occurrence,
                        OccurrenceId = occurrence.Id,
                        PersonAliasId = attendanceRecord.PersonAliasId,
                        StartDateTime = RockDateTime.Now,
                        DidAttend = attendanceRecord.DidAttend,
                        CampusId = attendanceRecord.CampusId,
                        DeviceId = LocalDeviceConfig.CurrentKioskId,
                        SearchValue = "MOVED IN OZ",
                        AttendanceCodeId = attendanceRecord.AttendanceCodeId,
                        QualifierValueId = attendanceRecord.QualifierValueId
                    };

                    //Close all other attendance records for this person today at this schedule
                    var currentRecords = attendanceService.Queryable()
                         .Where( a =>
                         a.StartDateTime >= Rock.RockDateTime.Today
                        && a.PersonAliasId == attendanceRecord.PersonAliasId
                        && a.Occurrence.ScheduleId == attendanceRecord.Occurrence.ScheduleId
                        && a.EndDateTime == null
                        && a.Id != newRecord.Id )
                        .ToList();

                    foreach ( var record in currentRecords )
                    {
                        record.EndDateTime = Rock.RockDateTime.Now;
                        record.DidAttend = false;
                    }

                    attendanceService.Add( newRecord );
                    _rockContext.SaveChanges();

                    if ( mobileCheckinRecordId.HasValue )
                    {
                        MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( _rockContext );
                        var record = mobileCheckinRecordService.Get( mobileCheckinRecordId.Value );
                        if ( record != null )
                        {
                            foreach ( var pastAttendanceRecord  in currentRecords )
                            {
                                record.Attendances.Remove( pastAttendanceRecord );
                            }
                            record.IsDirty = true;
                            record.Attendances.Add( newRecord );
                            _rockContext.SaveChanges();
                            MobileCheckinRecordCache.Update( record.Id );
                        }
                    }

                    foreach ( var record in currentRecords )
                    {
                        AttendanceCache.AddOrUpdate( record );
                    }
                    AttendanceCache.AddOrUpdate( newRecord );
                }
            }
            BindTable();
            RebuildModal();
        }

        private AttendanceOccurrence GetOccurrence( RockContext rockContext, DateTime startDateTime, int? groupId, int? locationId, int? scheduleId )
        {
            var occurrenceService = new AttendanceOccurrenceService( rockContext );
            var occurrence = occurrenceService.Get( startDateTime.Date, groupId, locationId, scheduleId );

            if ( occurrence == null )
            {
                // If occurrence does not yet exists, use a new context and create it
                using ( var newContext = new RockContext() )
                {
                    occurrence = new AttendanceOccurrence
                    {
                        OccurrenceDate = startDateTime.Date,
                        GroupId = groupId,
                        LocationId = locationId,
                        ScheduleId = scheduleId,
                    };

                    var newOccurrenceService = new AttendanceOccurrenceService( newContext );
                    newOccurrenceService.Add( occurrence );
                    newContext.SaveChanges();

                    // Query for the new occurrence using original context.
                    occurrence = occurrenceService.Get( occurrence.Id );
                }
            }
            return occurrence;
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
                location.SetAttributeValue( Constants.LOCATION_ATTRIBUTE_ROOM_RATIO, tbRatio.Text.AsInteger() );
                location.SaveAttributeValues();
                location.FirmRoomThreshold = tbFirmThreshold.Text.AsInteger();
                location.SoftRoomThreshold = tbSoftThreshold.Text.AsInteger();
                _rockContext.SaveChanges();
                mdLocation.Hide();
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                KioskTypeCache.ClearForTemplateId( LocalDeviceConfig.CurrentCheckinTypeId ?? 0 );
                KioskDeviceHelpers.Clear( CurrentCheckInState.ConfiguredGroupTypes );

                //Update the occurrence caches with the new numbers
                var occurrences = OccurrenceCache.All().Where( a => a.LocationId == location.Id ).ToList();
                foreach ( var occurrence in occurrences )
                {
                    occurrence.FirmRoomThreshold = location.FirmRoomThreshold;
                    occurrence.SoftRoomThreshold = location.SoftRoomThreshold;
                    occurrence.RoomRatio = tbRatio.Text.AsInteger();
                    OccurrenceCache.AddOrUpdate( occurrence );
                }
            }
            BindTable();
        }

        protected void mdSearch_SaveClick( object sender, EventArgs e )
        {
            ViewState[ViewStateKeys.SearchType] = null;
            ltLocation.Text = "";
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            mdSearch.Hide();
        }

        protected void btnCodeSearch_Click( object sender, EventArgs e )
        {
            ViewState[ViewStateKeys.SearchType] = SearchType.Code;
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
                var attendanceCode = attendanceCodeService.Queryable()
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
                        a.Occurrence.ScheduleId != null
                        && a.AttendanceCodeId != null
                        && a.Occurrence.LocationId != null
                    )
                    .ToList();
                DisplaySearchRecords( attendanceRecords );
            }
        }

        protected void btnNameSearch_Click( object sender, EventArgs e )
        {
            ViewState[ViewStateKeys.SearchType] = SearchType.Name;
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

                if ( attendance.DidAttend == false && attendance.EndDateTime == null && attendance.QualifierValueId == null )
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

            var occurences = OccurrenceCache.All()
                .Where( o => CurrentCheckInState.ConfiguredGroupTypes.Contains( o.GroupTypeId ) )
                .Where( o => o.ScheduleId == scheduleId )
                .ToList();


            switch ( ddlClose.SelectedValueAsEnum<CloseScope>() )
            {
                case CloseScope.Children:
                    occurences = occurences.Where( o => !o.IsVolunteer ).ToList();
                    break;
                case CloseScope.Volunteer:
                    occurences = occurences.Where( o => o.IsVolunteer ).ToList();
                    break;
                case CloseScope.All:
                    occurences = occurences.ToList();
                    break;
                default:
                    occurences = occurences.ToList();
                    break;
            }

            foreach ( var occcurrence in occurences )
            {
                CloseOccurrence( occcurrence.GroupLocationId, occcurrence.ScheduleId, false );
            }

            KioskTypeCache.ClearForTemplateId( LocalDeviceConfig.CurrentCheckinTypeId ?? 0 );
            KioskDeviceHelpers.Clear( CurrentCheckInState.ConfiguredGroupTypes );

            BindTable();
            ddlClose.SelectedValue = null;
            mdConfirmClose.Hide();
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

        protected void btnLocationPrint_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var labelGuid = GetAttributeValue( "LocationLabel" ).AsGuid();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var label = binaryFileService.Get( labelGuid );
            if ( label == null )
            {
                return;
            }

            label.LoadAttributes( rockContext );

            var locationId = hfLocationId.ValueAsInt();
            LocationService locationService = new LocationService( rockContext );
            var location = locationService.Get( locationId );
            if ( location == null )
            {
                return;
            }
            var lavaMergeObjects = new Dictionary<string, object>() { { "Location", location } };
            var mergeFields = new Dictionary<string, string>();

            var labelMergeFields = label.GetAttributeValue( "MergeCodes" ).ToKeyValuePairList();
            foreach ( var field in labelMergeFields )
            {
                mergeFields[field.Key] = DefinedValueCache.Get( ( ( string ) field.Value ).AsInteger() ).GetAttributeValue( "MergeField" );
            }

            foreach ( var key in mergeFields.Keys.ToList() )
            {
                mergeFields[key] = mergeFields[key].ResolveMergeFields( lavaMergeObjects );
            }

            var printLabel = new CheckInLabel
            {
                FileGuid = label.Guid,
                LabelFile = string.Format( label.Url ),
                MergeFields = mergeFields
            };
            var script = AddLabelScript( new List<CheckInLabel> { printLabel }.ToJson() );
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "addLabelScript", script, true );
        }

        private string AddLabelScript( string jsonObject )
        {
            string script = string.Format( @"
            var labelData = {0};
		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
try{{
            printLabels();
}} catch(e){{}}
            ", jsonObject );
            return script;
        }

        enum CloseScope
        {
            Children,
            Volunteer,
            All
        }
    }
}