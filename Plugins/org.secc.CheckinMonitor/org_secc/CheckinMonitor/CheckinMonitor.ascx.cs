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

            if ( ViewState["ModalLocation"] != null )
            {
                LocationModal( ( int ) ViewState["ModalLocation"] );
            }

            if ( !Page.IsPostBack )
            {
                BindDropDown();
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            }
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

            schedules.Insert( 0, new { Id = 0, Name = "All Schedules" } );

            ddlSchedules.DataSource = schedules;
            ddlSchedules.DataBind();
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            _rockContext = new RockContext();
            AttendanceService attendanceSerivce = new AttendanceService( _rockContext );
            var attendanceData = attendanceSerivce.Queryable()
                                    .Where( a => a.StartDateTime > Rock.RockDateTime.Today );

            var definedTypeGuid = GetAttributeValue( "DeactivatedDefinedType" ).AsGuidOrNull();
            if ( definedTypeGuid == null )
            {
                //We need to change this to show an error.
                return;
            }
                var scheduleService = new ScheduleService( _rockContext );
                var groupLocationService = new GroupLocationService( _rockContext );
                var dtDeactivated = DefinedTypeCache.Read( definedTypeGuid ?? new Guid() );
                var dvDeactivated = dtDeactivated.DefinedValues;
                var deactivatedGroupLocationSchedules = dvDeactivated.Select( dv => dv.Value.Split( '|' ) )
                    .Select( s => new {
                        GroupLocation = groupLocationService.Get( s[0].AsInteger() ),
                        Schedule = scheduleService.Get( s[1].AsInteger() ),
                        Active = false
                    } ).ToList();

            var groupTypes = new GroupTypeService( _rockContext )
                .GetByIds( CurrentCheckInState.ConfiguredGroupTypes );
            foreach ( var groupType in groupTypes )
            {
                Literal ltGt = new Literal();
                ltGt.Text = "<br><b>" + groupType.Name + "</b>";
                phContent.Controls.Add( ltGt );
                Table table = new Table();
                table.CssClass = "table";
                table.Style.Add( "margin-bottom", "10px" );
                phContent.Controls.Add( table );
                foreach ( var group in groupType.Groups )
                {
                    var source = group.GroupLocations
                        .SelectMany( gl => gl.Schedules, ( gl, s ) => new { GroupLocation = gl, Schedule = s, Active = true } )
                        .Concat( deactivatedGroupLocationSchedules.Where( dGLS => dGLS.GroupLocation.GroupId == group.Id) )
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
                    thcRatio.Text = "Kid/Adult Ratio";
                    thcRatio.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcRatio );

                    TableHeaderCell thcCount = new TableHeaderCell();
                    thcCount.Text = "Count";
                    thcCount.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcCount );

                    TableHeaderCell thcToggle = new TableHeaderCell();
                    thcToggle.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcToggle );

                    TableHeaderCell thcRoom = new TableHeaderCell();
                    thcRoom.Style.Add( "width", "5%" );
                    thr.Controls.Add( thcRoom );

                    foreach ( var occurrence in source )
                    {

                        occurrence.GroupLocationSchedule.GroupLocation.Location.LoadAttributes();
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        name.Text = occurrence.GroupLocationSchedule.GroupLocation.Location.Name + " @ " + occurrence.GroupLocationSchedule.Schedule.Name;
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        var ratio = occurrence.GroupLocationSchedule.GroupLocation.Location.GetAttributeValue( "RoomRatio" ).AsInteger();
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
                            if ( occurrence.GroupLocationSchedule.GroupLocation.Location.IsActive && occurrence.Total != 0 && occurrence.Total >= ( occurrence.GroupLocationSchedule.GroupLocation.Location.FirmRoomThreshold ?? 0 ) )
                            {
                                CloseLocation( occurrence.GroupLocationSchedule.GroupLocation.Location.Id );
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
                        BootstrapButton btnActive = new BootstrapButton();
                        if ( occurrence.Active )
                        {
                            btnActive.CssClass = "btn btn-success btn-block";
                            btnActive.Text = "Open";
                        }
                        else
                        {
                            btnActive.CssClass = "btn btn-danger btn-block";
                            btnActive.Text = "Closed";
                        }
                        btnActive.ID = "btnL" + group.Id.ToString() + occurrence.GroupLocationSchedule.GroupLocation.Id + occurrence.GroupLocationSchedule.Schedule.Id;
                        btnActive.Click += ( s, ee ) => { ToggleLocation( occurrence.GroupLocationSchedule.GroupLocation.Id, occurrence.GroupLocationSchedule.Schedule ); };
                        tcToggle.Controls.Add( btnActive );

                        TableCell tcLocation = new TableCell();
                        tr.Controls.Add( tcLocation );
                        BootstrapButton btnLocation = new BootstrapButton();
                        btnLocation.ID = "btn" + group.Id.ToString() + occurrence.GroupLocationSchedule.GroupLocation.Location.Id + occurrence.GroupLocationSchedule.Schedule.Id;
                        btnLocation.Text = "<i class='fa fa-users'></i>";
                        btnLocation.CssClass = "btn btn-default";
                        btnLocation.Click += ( s, e ) => { LocationModal( occurrence.GroupLocationSchedule.GroupLocation.Location.Id + occurrence.GroupLocationSchedule.Schedule.Id ); };
                        tcLocation.Controls.Add( btnLocation );
                    }
                }
            }
        }

        private void LocationModal( int id )
        {
            phLocation.Controls.Clear();
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "clearInterval(timer);", true );
            mdLocation.Show();
            AttendanceService attendanceService = new AttendanceService( _rockContext );
            //Get all attendance data for location for today
            var data = attendanceService.Queryable( "PersonAlias.Person,Location,Group,Schedule" )
                .Where( a => a.LocationId == id && a.CreatedDateTime >= Rock.RockDateTime.Today )
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
                    foreach ( var record in reserved )
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
            ViewState.Add( "ModalLocation", id );
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
                LocationModal( locationId );
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
                LocationModal( record.LocationId ?? 0 );
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
                LocationModal( record.LocationId ?? 0 );
            }
        }

        private void MoveModal( int id )
        {
            ViewState["ModalLocation"] = null;
            ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "stopTimer", "clearInterval(timer);", true );
            mdLocation.Hide();
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

        private void ToggleLocation( int GroupLocationId, Schedule schedule )
        {
            var groupLocation = new GroupLocationService( _rockContext ).Get( GroupLocationId );
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
            if (definedValue == null )
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
            var definedValue = new DefinedValue() { Id = 0};
            definedValue.Value = groupLocation.Id.ToString() + "|" + schedule.Id.ToString();
            definedValue.Description = string.Format( "Deactivated {0} for schedule {1} at {2}", groupLocation.ToString(), schedule.Name, Rock.RockDateTime.Now.ToString() );
            definedValue.DefinedTypeId = definedType.Id;
            definedValue.IsSystem = false;
            var definedValueService = new DefinedValueService( _rockContext );
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

        private void CloseLocation( int id )
        {
            var location = new LocationService( _rockContext ).Get( id );
            if ( location != null )
            {
                location.IsActive = false;
                _rockContext.SaveChanges();
                Rock.CheckIn.KioskDevice.FlushAll();
                BindTable();
            }
        }

        protected void mdLocation_SaveClick( object sender, EventArgs e )
        {
            mdLocation.Hide();
            ViewState["ModalLocation"] = null;
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
            var currentRecords = attendanceService.Queryable()
                 .Where( a =>
                 a.CreatedDateTime >= Rock.RockDateTime.Today
                && a.DidAttend == true
                && a.PersonAliasId == a.PersonAliasId
                && a.EndDateTime == null ).ToList();

            foreach ( var record in currentRecords )
            {
                record.EndDateTime = Rock.RockDateTime.Now;
            }
            Attendance newRecord = new Attendance();
            newRecord.CopyPropertiesFrom( attendanceRecord );
            newRecord.StartDateTime = Rock.RockDateTime.Now;
            newRecord.EndDateTime = null;
            newRecord.DeviceId = null;
            newRecord.DidAttend = true;
            newRecord.LocationId = ddlMove.SelectedValue.AsInteger();
            attendanceService.Add( newRecord );
            _rockContext.SaveChanges();
            BindTable();
        }

        protected void cbAll_CheckedChanged( object sender, EventArgs e )
        {
            //This is an empty function because we just need to reload the page
        }

        protected void ddlSchedules_SelectedIndexChanged( object sender, EventArgs e )
        {

        }
    }
}