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
        }

        private void BindTable()
        {
            phContent.Controls.Clear();
            _rockContext = new RockContext();
            AttendanceService attendanceSerivce = new AttendanceService( _rockContext );
            var attendanceData = attendanceSerivce.Queryable()
                                    .Where( a => a.DidAttend == true && a.StartDateTime > Rock.RockDateTime.Today );

            var GroupTypes = CurrentCheckInState.Kiosk.ActiveGroupTypes( CurrentCheckInState.ConfiguredGroupTypes );
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
                                KidCount = a.Where( at => at.PersonAlias.Person.Age <= 12 ).Count(),
                                AdultCount = a.Where( at => at.PersonAlias.Person.Age > 12 ).Count()
                            }
                        )
                        .DistinctBy( s => s.Location.Id )
                        .ToList();

                    source.ForEach( l => l.Location.LoadAttributes() );


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
                    thcToggle.Style.Add( "width", "25%" );
                    thr.Controls.Add( thcToggle );

                    foreach ( var room in source )
                    {
                        TableRow tr = new TableRow();
                        table.Controls.Add( tr );

                        TableCell name = new TableCell();
                        name.Text = room.Location.Name;
                        tr.Controls.Add( name );

                        TableCell tcRatio = new TableCell();
                        var ratio = room.Location.GetAttributeValue( "RoomRatio" ).AsInteger();
                        var ratioDistance = ( room.AdultCount * ratio ) - room.KidCount;
                        tcRatio.Text = room.KidCount.ToString() + "/" + room.AdultCount.ToString();
                        if ( ratioDistance == 0 )
                        {
                            tcRatio.Text += " [Room at ratio]";
                            tcRatio.CssClass = "warning";
                        }
                        else if ( ratioDistance < 0 )
                        {
                            tcRatio.Text += " [" + ( ratioDistance * -1 ).ToString() + " over ratio]";
                            tcRatio.CssClass = "danger";
                        }
                        else
                        {
                            if ( ratioDistance < 4 )
                            {
                                tcRatio.CssClass = "info";
                            }
                            tcRatio.Text += " [" + ratioDistance.ToString() + " remaining]";
                        }

                        tr.Controls.Add( tcRatio );

                        TableCell tcCapacity = new TableCell();
                        var total = room.AdultCount + room.KidCount;
                        if ( total >= (room.Location.FirmRoomThreshold??0) && room.Location.IsActive )
                        {
                            //CloseLocation( room.Location.Id );
                            tcCapacity.CssClass="danger";
                        }
                        else if( total >= ( room.Location.FirmRoomThreshold ?? 0 )+3 && room.Location.IsActive )
                        {
                            tcCapacity.CssClass = "warning";
                        }
                        tcCapacity.Text = total.ToString() + " of " + ( room.Location.FirmRoomThreshold ?? 0 ).ToString();

                        tr.Controls.Add( tcCapacity );

                        TableCell tcToggle = new TableCell();
                        tr.Controls.Add( tcToggle );
                        Toggle cbActive = new Toggle();
                        cbActive.OnCssClass = "btn-success";
                        cbActive.OffCssClass = "btn-danger";
                        cbActive.OnText = "Open";
                        cbActive.OffText = "Closed";
                        cbActive.ButtonSizeCssClass = "btn-xs";
                        cbActive.Checked = room.Location.IsActive;
                        cbActive.ID = g.Group.Id.ToString() + room.Location.Id;
                        cbActive.CheckedChanged += ( s, ee ) => { ToggleLocation( room.Location.Id ); };

                        tcToggle.Controls.Add( cbActive );
                    }
                }
            }
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
                Rock.CheckIn.KioskDevice.FlushAll();
                _rockContext.SaveChanges();
            }
        }

        protected void Timer1_Tick( object sender, EventArgs e )
        {

        }
    }
}