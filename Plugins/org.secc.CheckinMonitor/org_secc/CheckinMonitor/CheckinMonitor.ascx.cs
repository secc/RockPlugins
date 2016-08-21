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
using Rock.Web.Cache;
using org.secc.FamilyCheckin.Utilities;
using System.Data.Entity;
using Rock.Security;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Check-In Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Helps manage rooms and room ratios." )]
    [DefinedTypeField( "Deactivated Defined Type", "Check-in monitor needs a place to save deactivated checkin configurations." )]
    [TextField( "Room Ratio Attribute Key", "Attribute key for room ratios", true, "RoomRatio" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]

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

            if ( IsUserAuthorized( Authorization.ADMINISTRATE ) )
            {
                btnFlush.Visible = true;
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
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            try
            {
                kioskCountUtility = new KioskCountUtility( CurrentCheckInState.ConfiguredGroupTypes,
                                                            GetAttributeValue( "VolunteerGroupAttribute" ).AsGuid(),
                                                            GetAttributeValue( "DeactivatedDefinedType" ).AsGuid() );
            }
            catch
            {
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
                groupLocationSchedules = groupLocationSchedules.Where( gls => gls.Schedule.IsScheduleOrCheckInActive ).ToList();
            }

            foreach ( var groupType in kioskCountUtility.GroupTypes.OrderBy( gt => gt.Order ).ToList() )
            {
                Literal ltGt = new Literal();
                ltGt.Text = "<br><b>" + groupType.Name + "</b>";
                phContent.Controls.Add( ltGt );
                Table table = new Table();
                table.CssClass = "table";
                table.Style.Add( "margin-bottom", "10px" );
                phContent.Controls.Add( table );

                foreach ( var group in groupType.Groups.OrderBy( g => g.Order ) )
                {
                    var occurance = groupLocationSchedules
                        .Where( gls => gls.GroupLocation.GroupId == group.Id )
                        .OrderBy( gls => gls.GroupLocation.Location.Name )
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
                        thcRatio.Text = "[No selected schedues]";
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

                        BootstrapButton btnOccurrence = new BootstrapButton();
                        btnOccurrence.ID = "btn" + gls.GroupLocation.Id.ToString() + gls.Schedule.Id;
                        btnOccurrence.Text = "<i class='fa fa-users'></i>";
                        btnOccurrence.CssClass = "btn btn-sm btn-default";
                        btnOccurrence.Click += ( s, e ) => { OccurrenceModal( gls.GroupLocation, gls.Schedule ); };
                        tcLocation.Controls.Add( btnOccurrence );

                        BootstrapButton btnLocation = new BootstrapButton();
                        btnLocation.ID = "btnLocation" + gls.GroupLocation.Id.ToString() + gls.Schedule.Id;
                        btnLocation.Text = "<i class='fa fa-map-marker'></i>";
                        btnLocation.CssClass = "btn btn-sm btn-default";
                        btnLocation.Click += ( s, e ) => { ShowLocationModal( gls.GroupLocation.Location ); };

                        tcLocation.Controls.Add( btnLocation );
                    }
                }
            }
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
                var data = attendanceService.Queryable( "PersonAlias.Person,Location,Group,Schedule" )
                    .Where( a =>
                            a.PersonAliasId != null
                            && a.LocationId == groupLocation.LocationId
                            && a.ScheduleId == schedule.Id
                            && a.StartDateTime >= Rock.RockDateTime.Today
                            )
                    .ToList();
                var currentVolunteers = data.Where( a => a.DidAttend == true
                                                    && a.EndDateTime == null
                                                    && kioskCountUtility.VolunteerGroupIds.Contains( a.GroupId ?? 0 ) )
                                                    .OrderBy( a => a.PersonAlias.Person.LastName )
                                                    .ThenBy( a => a.PersonAlias.Person.NickName );
                var currentChildren = data.Where( a => a.DidAttend == true
                                                    && a.EndDateTime == null
                                                    && !kioskCountUtility.VolunteerGroupIds.Contains( a.GroupId ?? 0 ) )
                                                    .OrderBy( a => a.PersonAlias.Person.LastName )
                                                    .ThenBy( a => a.PersonAlias.Person.NickName );

                var current = new List<Attendance>();
                current.AddRange( currentVolunteers );
                current.AddRange( currentChildren );
                var reserved = data.Where( a => a.DidAttend != true ).ToList();
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
                            if ( record.Group != null )
                            {
                                tcGroup.Text = record.Group.Name;
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
                            if ( record.Group != null )
                            {
                                tcGroup.Text = record.Group.Name;
                            }

                            TableCell tcSchedule = new TableCell();
                            trRecord.Controls.Add( tcSchedule );
                            tcSchedule.Style.Add( "width", "25%" );
                            if ( record.Schedule != null )
                            {
                                tcSchedule.Text = record.Schedule.Name;
                            }

                            TableCell tcButtons = new TableCell();
                            trRecord.Controls.Add( tcButtons );
                            tcButtons.Style.Add( "width", "15%" );

                            BootstrapButton btnCheckin = new BootstrapButton();
                            btnCheckin.ID = "checkin" + record.Id.ToString();
                            btnCheckin.CssClass = "btn btn-success btn-xs";
                            btnCheckin.Text = "Check-In";
                            btnCheckin.Click += ( s, e ) => { Checkin( record.Id ); };
                            tcButtons.Controls.Add( btnCheckin );

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
                    var locationId = record.LocationId ?? 0;
                    attendanceService.Delete( record );
                    _rockContext.SaveChanges();
                    RebuildModal();
                }
            }
        }

        private void Checkin( int id )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                AttendanceService attendanceService = new AttendanceService( _rockContext );
                var record = attendanceService.Get( id );
                if ( record != null )
                {
                    record.StartDateTime = Rock.RockDateTime.Now;
                    record.DidAttend = true;
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
                    KioskLocationAttendance.Flush( record.LocationId ?? 0 );
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

                ltMove.Text = string.Format( "{0} @ {1}", attendanceRecord.PersonAlias.Person.FullName, attendanceRecord.Schedule.Name );
                ltMoveInfo.Text = string.Format(
                    "{0} is currently in: {1} for the schedule {2}",
                    attendanceRecord.PersonAlias.Person.NickName,
                    attendanceRecord.Location.Name,
                    attendanceRecord.Schedule.Name );

                var locations = CurrentCheckInState.Kiosk.KioskGroupTypes
                    .SelectMany( gt => gt.KioskGroups )
                    .SelectMany( g => g.KioskLocations )
                    .Select( l => l.Location )
                    .DistinctBy( l => l.Id )
                    .ToList();

                ddlMove.DataSource = locations;
                ddlMove.DataValueField = "Id";
                ddlMove.DataTextField = "Name";
                ddlMove.DataBind();
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
                    Rock.CheckIn.KioskDevice.Flush( groupLocation.LocationId );
                    KioskLocationAttendance.Flush( groupLocation.LocationId );
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

                Rock.Web.Cache.DefinedTypeCache.Flush( definedValue.DefinedTypeId );
                Rock.Web.Cache.DefinedValueCache.Flush( definedValue.Id );

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

                Rock.Web.Cache.DefinedTypeCache.Flush( definedValue.DefinedTypeId );
                Rock.Web.Cache.DefinedValueCache.Flush( definedValue.Id );

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
                            Rock.CheckIn.KioskDevice.Flush( groupLocation.Id );
                            KioskLocationAttendance.Flush( groupLocation.Id );
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

                //Close all other attendance records for this person today at this schedule
                var currentRecords = attendanceService.Queryable()
                     .Where( a =>
                     a.StartDateTime >= Rock.RockDateTime.Today
                    && a.DidAttend == true
                    && a.PersonAliasId == attendanceRecord.PersonAliasId
                    && a.ScheduleId == attendanceRecord.ScheduleId
                    && a.EndDateTime == null ).ToList();
                foreach ( var record in currentRecords )
                {
                    record.EndDateTime = Rock.RockDateTime.Now;
                }

                //Create a new attendance record
                Attendance newRecord = ( Attendance ) attendanceRecord.Clone();
                newRecord.Id = 0;
                newRecord.Guid = new Guid();
                newRecord.AttendanceCode = null;
                newRecord.StartDateTime = Rock.RockDateTime.Now;
                newRecord.EndDateTime = null;
                newRecord.DidAttend = true;
                newRecord.Device = null;
                newRecord.SearchTypeValue = null;
                newRecord.LocationId = ddlMove.SelectedValue.AsInteger();
                attendanceService.Add( newRecord );
                _rockContext.SaveChanges();
                KioskLocationAttendance.AddAttendance( newRecord );
                KioskLocationAttendance.Flush( attendanceRecord.LocationId ?? 0 );
            }
            BindTable();
            RebuildModal();
        }

        protected void ddlSchedules_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ddlSchedules.SelectedValue.AsInteger() > 0 )
            {
                btnCloseAll.Visible = true;
                btnCloseAll.Text = string.Format( "Close all at {0}", ddlSchedules.SelectedItem.Text );
            }
            else
            {
                btnCloseAll.Visible = false;
            }
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            NavigateToPreviousPage();
        }

        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            //This is an empty function because we just need to reload the page
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
                Rock.CheckIn.KioskDevice.Flush( location.Id );
                KioskLocationAttendance.Flush( location.Id );
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
                var attendanceCode = attendanceCodeService.Queryable()
                    .Where( ac => ac.Code == code && ac.IssueDateTime >= Rock.RockDateTime.Today )
                    .FirstOrDefault();
                if ( attendanceCode == null )
                {
                    ltSearch.Text = "Attendance matching code not found";
                    return;
                }
                DisplaySearchRecords( attendanceCode.Attendances.Where( a => a.EndDateTime == null ).ToList() );
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
                var attendanceRecords = new AttendanceService( _rockContext ).Queryable( "AttendanceCode" )
                    .Where( a => a.StartDateTime >= Rock.RockDateTime.Today && a.EndDateTime == null && aliasIds.Contains( a.PersonAliasId ) )
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
                tcCode.Text = attendance.AttendanceCode.Code;
                trRow.Controls.Add( tcCode );

                TableCell tcDevice = new TableCell();
                tcDevice.Text = attendance.Device.Name;
                trRow.Controls.Add( tcDevice );

                TableCell tcLocation = new TableCell();
                tcLocation.Text = attendance.Location.Name;
                trRow.Controls.Add( tcLocation );

                TableCell tcSchedule = new TableCell();
                tcSchedule.Text = attendance.Schedule.Name;
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

        protected void btnCloseAll_Click( object sender, EventArgs e )
        {
            var scheduleName = ddlSchedules.SelectedItem.Text;
            ltConfirmClose.Text = string.Format( "Are you sure you want to close all locations for {0}", scheduleName );
            mdConfirmClose.Show();
        }

        protected void mdConfirmClose_SaveClick( object sender, EventArgs e )
        {
            var scheduleId = ddlSchedules.SelectedValue.AsInteger();
            foreach ( var gls in kioskCountUtility.GroupLocationSchedules.Where( gls => gls.Schedule.Id == scheduleId ) )
            {
                CloseOccurrence( gls.GroupLocation.Id, scheduleId, false );
            }
            mdConfirmClose.Hide();
            BindTable();
        }

        protected void btnFlush_Click( object sender, EventArgs e )
        {
            ViewState["LocationRatios"] = null;
            KioskDevice.FlushAll();
            foreach ( var locationId in kioskCountUtility.GroupLocationSchedules.Select( gls => gls.GroupLocation.LocationId ).Distinct() )
            {
                KioskLocationAttendance.Flush( locationId );
            }
            BindTable();
        }
    }
}