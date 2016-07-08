// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Check-In Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Helps manage rooms and room ratios." )]
    [DefinedTypeField( "Deactivated Defined Type", "Check-in monitor needs a place to save deactivated checkin configurations." )]
    [TextField("Room Ratio Attribute Key", "Attribute key for room ratios", true, "RoomRatio")]


    public partial class CheckinMonitor : CheckInBlock
    {

        private RockContext _rockContext;

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
            OccurrenceModal();
        }

        private void BindDropDown()
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToHomePage();
                return;
            }

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

        private void BindTable()
        {
            phContent.Controls.Clear();
            _rockContext = new RockContext();
            AttendanceService attendanceSerivce = new AttendanceService( _rockContext );
            var attendanceData = attendanceSerivce.Queryable()
                                    .Where( a =>
                                            a.StartDateTime > Rock.RockDateTime.Today
                                            && a.PersonAliasId != null
                                    );

            var definedTypeGuid = GetAttributeValue( "DeactivatedDefinedType" ).AsGuidOrNull();
            if ( definedTypeGuid == null )
            {
                maError.Show( "This block is not configured", ModalAlertType.Warning );
                return;
            }
            var scheduleService = new ScheduleService( _rockContext );
            var groupLocationService = new GroupLocationService( _rockContext );
            var dtDeactivated = DefinedTypeCache.Read( definedTypeGuid ?? new Guid() );
            var dvDeactivated = dtDeactivated.DefinedValues;
            var deactivatedGroupLocationSchedules = dvDeactivated.Select( dv => dv.Value.Split( '|' ) )
                .Select( s => new
                {
                    GroupLocation = groupLocationService.Get( s[0].AsInteger() ),
                    Schedule = scheduleService.Get( s[1].AsInteger() ),
                    Active = false
                } ).ToList();

            var groupTypes = new GroupTypeService( _rockContext )
                .GetByIds( CurrentCheckInState.ConfiguredGroupTypes );
            foreach ( var groupType in groupTypes.ToList() )
            {
                Literal ltGt = new Literal();
                ltGt.Text = "<br><b>" + groupType.Name + "</b>";
                phContent.Controls.Add( ltGt );
                Table table = new Table();
                table.CssClass = "table";
                table.Style.Add( "margin-bottom", "10px" );
                phContent.Controls.Add( table );

                var sources = groupType.Groups.SelectMany( g => g.GroupLocations )
                        .SelectMany( gl => gl.Schedules, ( gl, s ) => new { GroupLocation = gl, Schedule = s, Active = true } )
                        .Concat( deactivatedGroupLocationSchedules.Where( dGLS => groupType.Groups.Contains( dGLS.GroupLocation.Group ) ) )
                        .GroupJoin( attendanceData,
                            gls => new { LocationId = gls.GroupLocation.LocationId, ScheduleId = gls.Schedule.Id },
                            a => new { LocationId = a.LocationId.Value, ScheduleId = a.ScheduleId.Value },
                            ( gls, a ) =>
                            new
                            {
                                GroupLocationSchedule = gls,
                                KidCount = a.Where( at => at.PersonAlias.Person.Age <= 12 && at.DidAttend == true && at.EndDateTime == null ).Count(),
                                AdultCount = a.Where( at => at.PersonAlias.Person.Age > 12 && at.DidAttend == true && at.EndDateTime == null ).Count(),
                                Reserved = a.Where( at => at.DidAttend != true ).Count(),
                                Total = a.Where( at => at.DidAttend == true && at.DidAttend == true && at.EndDateTime == null ).Count(),
                                Active = gls.Active
                            }
                        )
                        .DistinctBy( o => new { GroupLocationId = o.GroupLocationSchedule.GroupLocation.Id, ScheduleId = o.GroupLocationSchedule.Schedule.Id } ) //this is here because bad cache can cause problems
                        .OrderBy( o => o.GroupLocationSchedule.GroupLocation.Id )
                        .ThenBy( o => o.GroupLocationSchedule.Schedule.Id )
                        .ToList();
                var selectedScheduleId = ddlSchedules.SelectedValue.AsInteger();
                if ( selectedScheduleId > 0 )
                {
                    sources = sources.Where( o => o.GroupLocationSchedule.Schedule.Id == selectedScheduleId ).ToList();
                }
                else if ( selectedScheduleId == 0 )
                {
                    sources = sources.Where( o => o.GroupLocationSchedule.Schedule.IsScheduleOrCheckInActive ).ToList();
                }

                foreach ( var group in groupType.Groups.ToList() )
                {
                    var source = sources.Where( s => s.GroupLocationSchedule.GroupLocation.GroupId == group.Id ).ToList();

                    Literal ltG = new Literal();
                    phContent.Controls.Add( ltG );

                    TableHeaderRow thr = new TableHeaderRow();
                    table.Controls.Add( thr );

                    TableHeaderCell thcName = new TableHeaderCell();
                    thcName.Text = group.Name;
                    thcName.Style.Add( "width", "35%" );
                    thr.Controls.Add( thcName );


                    TableHeaderCell thcRatio = new TableHeaderCell();
                    if ( source.Any() )
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
                    if ( source.Any() )
                    {
                        thcCount.Text = "Count";
                    }
                    thcCount.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcCount );

                    TableHeaderCell thcToggle = new TableHeaderCell();
                    thcToggle.Style.Add( "width", "15%" );
                    thr.Controls.Add( thcToggle );

                    TableHeaderCell thcRoom = new TableHeaderCell();
                    thcRoom.Style.Add( "width", "10%" );
                    thr.Controls.Add( thcRoom );

                    foreach ( var occurrence in source )
                    {

                        occurrence.GroupLocationSchedule.GroupLocation.Location.LoadAttributes();
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        if ( selectedScheduleId > 0 )
                        {
                            name.Text = occurrence.GroupLocationSchedule.GroupLocation.Location.Name;
                        }
                        else
                        {
                            name.Text = occurrence.GroupLocationSchedule.GroupLocation.Location.Name + " @ " + occurrence.GroupLocationSchedule.Schedule.Name;
                        }
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        occurrence.GroupLocationSchedule.GroupLocation.Location.LoadAttributes();
                        var ratio = occurrence.GroupLocationSchedule.GroupLocation.Location.GetAttributeValue( GetAttributeValue( "RoomRatioAttributeKey" ) ).AsInteger();
                        var ratioDistance = ( occurrence.AdultCount * ratio ) - occurrence.KidCount;
                        tcRatio.Text = occurrence.KidCount.ToString() + "/" + occurrence.AdultCount.ToString();
                        if ( occurrence.Total != 0 && ratioDistance == 0 )
                        {
                            tcRatio.Text += " [Room at ratio]";
                            tcRatio.CssClass = "warning";
                        }
                        else if ( occurrence.Total != 0 && ratioDistance < 0 )
                        {
                            tcRatio.Text += " [" + ( ratioDistance * -1 ).ToString() + " over ratio]";
                            tcRatio.CssClass = "danger";
                        }
                        else
                        {
                            if ( ratioDistance < 4 && occurrence.Total != 0 )
                            {
                                tcRatio.CssClass = "info";
                            }
                            tcRatio.Text += " [" + ratioDistance.ToString() + " remaining]";
                        }

                        tr.Controls.Add( tcRatio );

                        TableCell tcCapacity = new TableCell();
                        if ( ( occurrence.Total + occurrence.Reserved ) != 0
                            && ( occurrence.Total + occurrence.Reserved ) >= ( occurrence.GroupLocationSchedule.GroupLocation.Location.FirmRoomThreshold ?? 0 ) )
                        {
                            if ( occurrence.Active && occurrence.Total != 0 && occurrence.Total >= ( occurrence.GroupLocationSchedule.GroupLocation.Location.FirmRoomThreshold ?? 0 ) )
                            {
                                CloseOccurrence( occurrence.GroupLocationSchedule.GroupLocation, occurrence.GroupLocationSchedule.Schedule );
                                return;
                            }
                            tcCapacity.CssClass = "danger";
                        }
                        else if ( ( occurrence.Total + occurrence.Reserved ) != 0 && ( occurrence.Total + occurrence.Reserved ) >= ( occurrence.GroupLocationSchedule.GroupLocation.Location.FirmRoomThreshold ?? 0 ) + 3 )
                        {
                            tcCapacity.CssClass = "warning";
                        }
                        tcCapacity.Text = occurrence.Total.ToString() + " of " + ( occurrence.GroupLocationSchedule.GroupLocation.Location.FirmRoomThreshold ?? 0 ).ToString() + ( occurrence.Reserved > 0 ? " (+" + occurrence.Reserved + " reserved)" : "" );

                        tr.Controls.Add( tcCapacity );

                        TableCell tcToggle = new TableCell();
                        tr.Controls.Add( tcToggle );

                        BootstrapButton btnToggle = new BootstrapButton();
                        btnToggle.ID = "btnL" + group.Id.ToString() + occurrence.GroupLocationSchedule.GroupLocation.Id + occurrence.GroupLocationSchedule.Schedule.Id;
                        btnToggle.Click += ( s, ee ) => { ToggleLocation( occurrence.GroupLocationSchedule.GroupLocation, occurrence.GroupLocationSchedule.Schedule ); };
                        if ( occurrence.Active )
                        {
                            btnToggle.Text = "Active";
                            btnToggle.ToolTip = "Click to deactivate.";
                            btnToggle.CssClass = "btn btn-success btn-block";
                        }
                        else
                        {
                            btnToggle.Text = "Inactive";
                            btnToggle.ToolTip = "Click to activate.";
                            btnToggle.CssClass = "btn btn-danger btn-block";
                        }
                        tcToggle.Controls.Add( btnToggle );

                        TableCell tcLocation = new TableCell();
                        tr.Controls.Add( tcLocation );

                        BootstrapButton btnOccurrence = new BootstrapButton();
                        btnOccurrence.ID = "btn" + occurrence.GroupLocationSchedule.GroupLocation.Id.ToString() + occurrence.GroupLocationSchedule.Schedule.Id;
                        btnOccurrence.Text = "<i class='fa fa-users'></i>";
                        btnOccurrence.CssClass = "btn btn-default";
                        btnOccurrence.Click += ( s, e ) => { OccurrenceModal( occurrence.GroupLocationSchedule.GroupLocation, occurrence.GroupLocationSchedule.Schedule ); };
                        tcLocation.Controls.Add( btnOccurrence );

                        BootstrapButton btnLocation = new BootstrapButton();
                        btnLocation.ID = "btnLocation" + occurrence.GroupLocationSchedule.GroupLocation.Id.ToString() + occurrence.GroupLocationSchedule.Schedule.Id;
                        btnLocation.Text = "<i class='fa fa-map-marker'></i>";
                        btnLocation.CssClass = "btn btn-default";
                        btnLocation.Click += ( s, e ) => { ShowLocationModal( occurrence.GroupLocationSchedule.GroupLocation.Location ); };

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
            tbThreshold.Text = location.FirmRoomThreshold.ToString();
            mdLocation.Show();
        }

        private void OccurrenceModal()
        {
            if ( ViewState["ModalGroupLocation"] != null && ViewState["ModalGroupLocationSchedule"] != null )
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
            //mdOccurrence.Hide();
        }

        private void OccurrenceModal( GroupLocation groupLocation, Schedule schedule )
        {
            phLocation.Controls.Clear();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "stopTimer();", true );
            mdOccurrence.Show();
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            //Get all attendance data for grouplocationschedule for today
            var data = attendanceService.Queryable( "PersonAlias.Person,Location,Group,Schedule" )
                .Where( a =>
                        a.PersonAliasId != null
                        && a.LocationId == groupLocation.LocationId
                        && a.ScheduleId == schedule.Id
                        && a.CreatedDateTime >= Rock.RockDateTime.Today
                        )
                .ToList();
            var current = data.Where( a => a.DidAttend == true && a.EndDateTime == null ).ToList();
            var reserved = data.Where( a => a.DidAttend != true ).ToList();
            var history = data.Where( a => a.DidAttend == true && a.EndDateTime != null ).ToList();

            if ( !data.Any() )
            {
                ltLocation.Text = "There are currently no attendance records for this location today.";
            }
            else
            {
                ltLocation.Text = data.FirstOrDefault().Location.Name;
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

                        BootstrapButton btnMove = new BootstrapButton();
                        btnMove.ID = "move" + record.Id.ToString();
                        btnMove.CssClass = "btn btn-warning btn-xs";
                        btnMove.Text = "Move";
                        btnMove.Click += ( s, e ) => { MoveModal( record.Id ); };
                        tcButtons.Controls.Add( btnMove );

                        BootstrapButton btnCheckout = new BootstrapButton();
                        btnCheckout.ID = "checkout" + record.Id.ToString();
                        btnCheckout.CssClass = "btn btn-danger btn-xs";
                        btnCheckout.Text = "Check-Out";
                        btnCheckout.Click += ( s, e ) => { Checkout( record.Id ); };
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

        private void CancelReservation( int id )
        {
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            var record = attendanceService.Get( id );
            if ( record != null )
            {
                var locationId = record.LocationId ?? 0;
                attendanceService.Delete( record );
                _rockContext.SaveChanges();
                OccurrenceModal();
            }
        }

        private void Checkin( int id )
        {
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            var record = attendanceService.Get( id );
            if ( record != null )
            {
                record.StartDateTime = Rock.RockDateTime.Now;
                record.DidAttend = true;
                _rockContext.SaveChanges();
                OccurrenceModal();
            }
        }

        private void Checkout( int id )
        {
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            var record = attendanceService.Get( id );
            if ( record != null )
            {
                record.EndDateTime = Rock.RockDateTime.Now;
                _rockContext.SaveChanges();
                OccurrenceModal();
            }
        }

        private void MoveModal( int id )
        {
            ViewState["ModalGroupLocation"] = null;
            ViewState["ModalGroupLocationSchedule"] = null;
            ViewState["ModalLocation"] = null;
            mdOccurrence.Hide();
            mdMove.Show();
            ViewState.Add( "ModalMove", id );
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            var attendanceRecord = attendanceService.Get( id );
            if ( attendanceRecord == null )
            {
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
                ViewState["ModalMove"] = null;
                mdMove.Hide();
                return;
            }

            ltMove.Text = attendanceRecord.PersonAlias.Person.FullName;

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

        private void ToggleLocation( GroupLocation groupLocation, Schedule schedule )
        {
            if ( groupLocation != null )
            {
                if ( groupLocation.Schedules.Contains( schedule ) )
                {
                    groupLocation.Schedules.Remove( schedule );
                    RecordGroupLocationSchedule( groupLocation, schedule );
                }
                else
                {
                    groupLocation.Schedules.Add( schedule );
                    RemoveDisabledGroupLocationSchedule( groupLocation, schedule );
                }
                _rockContext.SaveChanges();
                Rock.CheckIn.KioskDevice.FlushAll();
                BindTable();
            }
        }

        private void RemoveDisabledGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
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

        private void RecordGroupLocationSchedule( GroupLocation groupLocation, Schedule schedule )
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

        private void CloseOccurrence( GroupLocation groupLocation, Schedule schedule )
        {
            if ( groupLocation.Schedules.Contains( schedule ) )
            {
                RecordGroupLocationSchedule( groupLocation, schedule );
                groupLocation.Schedules.Remove( schedule );
                _rockContext.SaveChanges();
                Rock.CheckIn.KioskDevice.FlushAll();
                BindTable();
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

            //Remove all other attendance records for this person today at this schedule
            var currentRecords = attendanceService.Queryable()
                 .Where( a =>
                 a.CreatedDateTime >= Rock.RockDateTime.Today
                && a.DidAttend == true
                && a.PersonAliasId == attendanceRecord.PersonAliasId
                && a.ScheduleId == attendanceRecord.ScheduleId
                && a.EndDateTime == null ).ToList();
            foreach ( var record in currentRecords )
            {
                record.EndDateTime = Rock.RockDateTime.Now;
            }

            //Create a new attendance record
            Attendance newRecord = new Attendance();
            newRecord.ScheduleId = attendanceRecord.ScheduleId;
            newRecord.PersonAliasId = attendanceRecord.PersonAliasId;
            newRecord.GroupId = attendanceRecord.GroupId;
            newRecord.StartDateTime = Rock.RockDateTime.Now;
            newRecord.EndDateTime = null;
            newRecord.DeviceId = null;
            newRecord.DidAttend = true;
            newRecord.LocationId = ddlMove.SelectedValue.AsInteger();
            attendanceService.Add( newRecord );
            _rockContext.SaveChanges();
            BindTable();
        }

        protected void ddlSchedules_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindTable();
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
            LocationService locationService = new LocationService( _rockContext );
            var location = locationService.Get( hfLocationId.ValueAsInt() );
            if (location == null )
            {
                return;
            }
            location.LoadAttributes();
            location.SetAttributeValue( GetAttributeValue( "RoomRatioAttributeKey" ), tbRatio.Text.AsInteger() );
            location.SaveAttributeValues();
            location.FirmRoomThreshold = tbThreshold.Text.AsInteger();
            _rockContext.SaveChanges();
            mdLocation.Hide();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            BindTable();
        }
    }
}