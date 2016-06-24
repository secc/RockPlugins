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
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using System.Web;
using Rock.CheckIn;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Check-In Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Helps manage rooms and room ratios." )]


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
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            }
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            _rockContext = new RockContext();
            AttendanceService attendanceSerivce = new AttendanceService( _rockContext );
            var attendanceData = attendanceSerivce.Queryable()
                                    .Where( a => a.StartDateTime > Rock.RockDateTime.Today );
            List<KioskGroupType> GroupTypes;
            if ( cbAll.Checked )
            {
                //All configured Kiosk group types
                GroupTypes = new List<KioskGroupType>();
                var groupService = new GroupService( _rockContext );
                var groupLocationService = new GroupLocationService( _rockContext );
                foreach (var id in CurrentCheckInState.ConfiguredGroupTypes )
                {
                    var kgt = new KioskGroupType( id );
                    GroupTypes.Add( kgt );
                    var a = kgt.KioskGroups;
                    var childGroups = groupService.Queryable().Where( g => g.GroupTypeId == id );
                    foreach(var childGroup in childGroups )
                    {
                        var kg = new KioskGroup( childGroup );
                        kgt.KioskGroups.Add( kg );
                        var childLocations = groupLocationService.Queryable()
                            .Where( gl => gl.GroupId == childGroup.Id )
                            .Select( gl => gl.Location ).ToList();
                        foreach(var childLocation in childLocations )
                        {
                            var kl = new KioskLocation( childLocation );
                            kg.KioskLocations.Add( kl );
                        }
                    }
                }
            }
            else
            {
                GroupTypes = CurrentCheckInState.Kiosk.ActiveGroupTypes( CurrentCheckInState.ConfiguredGroupTypes );
            }

            var Groups = GroupTypes.SelectMany( gt => gt.KioskGroups );
            var Locations = Groups.SelectMany( g => g.KioskLocations );

            foreach ( var gt in GroupTypes )
            {
                Literal ltGt = new Literal();
                ltGt.Text = "<br><b>" + gt.GroupType.Name + "</b>";
                phContent.Controls.Add( ltGt );
                Table table = new Table();
                table.CssClass = "table";
                table.Style.Add( "margin-bottom", "10px" );
                phContent.Controls.Add( table );
                foreach ( var g in gt.KioskGroups )
                {
                    Literal ltG = new Literal();
                    phContent.Controls.Add( ltG );

                    var source = g.KioskLocations.Select( kl => kl.Location )
                        .GroupJoin( attendanceData,
                            l => l.Id,
                            a => a.LocationId,
                            ( l, a ) =>
                            new
                            {
                                Location = l,
                                KidCount = a.Where( at => at.PersonAlias.Person.Age <= 12 && at.DidAttend == true && at.EndDateTime == null ).Count(),
                                AdultCount = a.Where( at => at.PersonAlias.Person.Age > 12 && at.DidAttend == true && at.EndDateTime == null ).Count(),
                                Reserved = a.Where( at => at.DidAttend != true ).Count(),
                                Total = a.Where( at => at.DidAttend == true && at.DidAttend == true && at.EndDateTime == null ).Count()
                            }
                        )
                        .DistinctBy( s => s.Location.Id )
                        .ToList();

                    source.DistinctBy( s => s.Location ).ToList().ForEach( s => s.Location.LoadAttributes() );


                    TableHeaderRow thr = new TableHeaderRow();
                    table.Controls.Add( thr );

                    TableHeaderCell thcName = new TableHeaderCell();
                    thcName.Text = g.Group.Name;
                    thcName.Style.Add( "width", "25%" );
                    thr.Controls.Add( thcName );

                    TableHeaderCell thcRatio = new TableHeaderCell();
                    thcRatio.Text = "Ratio";
                    thcRatio.Style.Add( "width", "25%" );
                    thr.Controls.Add( thcRatio );

                    TableHeaderCell thcCount = new TableHeaderCell();
                    thcCount.Text = "Count";
                    thcCount.Style.Add( "width", "25%" );
                    thr.Controls.Add( thcCount );

                    TableHeaderCell thcToggle = new TableHeaderCell();
                    thcToggle.Style.Add( "width", "20%" );
                    thr.Controls.Add( thcToggle );

                    TableHeaderCell thcRoom = new TableHeaderCell();
                    thcRoom.Style.Add( "width", "5%" );
                    thr.Controls.Add( thcRoom );

                    foreach ( var room in source )
                    {
                        room.Location.LoadAttributes();
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        name.Text = room.Location.Name;
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        var ratio = room.Location.GetAttributeValue( "RoomRatio" ).AsInteger();
                        var ratioDistance = ( room.AdultCount * ratio ) - room.KidCount;
                        tcRatio.Text = room.KidCount.ToString() + "/" + room.AdultCount.ToString();
                        if ( room.Total != 0 && ratioDistance == 0 )
                        {
                            tcRatio.Text += " [Room at ratio]";
                            tcRatio.CssClass = "warning";
                        }
                        else if ( room.Total != 0 && ratioDistance < 0 )
                        {
                            tcRatio.Text += " [" + ( ratioDistance * -1 ).ToString() + " over ratio]";
                            tcRatio.CssClass = "danger";
                        }
                        else
                        {
                            if ( ratioDistance < 4 && room.Total != 0 )
                            {
                                tcRatio.CssClass = "info";
                            }
                            tcRatio.Text += " [" + ratioDistance.ToString() + " remaining]";
                        }

                        tr.Controls.Add( tcRatio );

                        TableCell tcCapacity = new TableCell();
                        if ( ( room.Total + room.Reserved ) != 0
                            && ( room.Total + room.Reserved ) >= ( room.Location.FirmRoomThreshold ?? 0 ) )
                        {
                            if ( room.Location.IsActive && room.Total != 0 && room.Total >= ( room.Location.FirmRoomThreshold ?? 0 ) )
                            {
                                CloseLocation( room.Location.Id );
                            }
                            tcCapacity.CssClass = "danger";
                        }
                        else if ( ( room.Total + room.Reserved ) != 0 && ( room.Total + room.Reserved ) >= ( room.Location.FirmRoomThreshold ?? 0 ) + 3 )
                        {
                            tcCapacity.CssClass = "warning";
                        }
                        tcCapacity.Text = room.Total.ToString() + " of " + ( room.Location.FirmRoomThreshold ?? 0 ).ToString() + ( room.Reserved > 0 ? " (+" + room.Reserved + " reserved)" : "" );

                        tr.Controls.Add( tcCapacity );

                        TableCell tcToggle = new TableCell();
                        tr.Controls.Add( tcToggle );
                        BootstrapButton btnActive = new BootstrapButton();
                        if ( room.Location.IsActive )
                        {
                            btnActive.CssClass = "btn btn-success btn-block";
                            btnActive.Text = "Open";

                        }
                        else
                        {
                            btnActive.CssClass = "btn btn-danger btn-block";
                            btnActive.Text = "Closed";

                        }
                        btnActive.ID = "btnL" + g.Group.Id.ToString() + room.Location.Id;
                        btnActive.Click += ( s, ee ) => { ToggleLocation( room.Location.Id ); };
                        tcToggle.Controls.Add( btnActive );

                        TableCell tcLocation = new TableCell();
                        tr.Controls.Add( tcLocation );
                        BootstrapButton btnLocation = new BootstrapButton();
                        btnLocation.ID = "btn" + g.Group.Id.ToString() + room.Location.Id;
                        btnLocation.Text = "<i class='fa fa-users'></i>";
                        btnLocation.CssClass = "btn btn-default";
                        btnLocation.Click += ( s, e ) => { LocationModal( room.Location.Id ); };
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

        private void ToggleLocation( int id )
        {
            var location = new LocationService( _rockContext ).Get( id );
            if ( location != null )
            {
                location.IsActive = !location.IsActive;
                _rockContext.SaveChanges();
                Rock.CheckIn.KioskDevice.FlushAll();
                BindTable();
            }
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
    }
}