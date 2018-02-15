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
using Rock.Web.UI;
using System.Data.Entity;
using System.Diagnostics;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Live Monitor" )]
    [Category( "SECC > Check-in" )]
    [Description( "Monitor Check-In live across the entire system." )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]

    public partial class LiveMonitor : RockBlock
    {
        KioskCountUtility kioskCountUtility;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable().AsNoTracking()
                .Where( a => a.StartDateTime >= Rock.RockDateTime.Today && a.StartDateTime <= Rock.RockDateTime.Now )
                .OrderByDescending( a => a.StartDateTime )
                .Take( 100 )
                .Select( a => new
                {
                    CampusShortCode = a.Campus.ShortCode,
                    Person = a.PersonAlias.Person,
                    GroupName = a.Group.Name,
                    LocationName = a.Location.Name,
                    ScheduleName = a.Schedule.Name,
                    KioskName = a.Device.Name,
                    StartDateTime = a.StartDateTime
                } )
                .ToList();
            foreach ( var attendance in attendances )
            {
                var shortCode = attendance.CampusShortCode.ToLower();
                Literal ltAttend = new Literal();
                if ( shortCode == "920" )
                {
                    shortCode = "c920";
                }
                ltAttend.Text = "<i class='se se-" + shortCode + "'></i> <b>" +
                    attendance.Person.FullName + "</b> checked-in to <b>" + attendance.GroupName +
                    "</b> in <b>" + attendance.LocationName + "</b> at <b>" + attendance.ScheduleName +
                    "</b> on <b>" + attendance.KioskName + "</b> (" + TimeAgo( attendance.StartDateTime ) + ")<br>";
                phAttendance.Controls.Add( ltAttend );
            }

            var minuteAgo = Rock.RockDateTime.Now.AddMinutes( -1 );
            var count = attendanceService.Queryable().AsNoTracking()
                .Where( a => a.StartDateTime >= minuteAgo && a.StartDateTime <= Rock.RockDateTime.Now ).Count();
            ltCount.Text = count.ToString();

            PerformanceCounter cpuCounter = new PerformanceCounter( "Processor", "% Processor Time", "_Total" );
            var speed = cpuCounter.NextValue();
            System.Threading.Thread.Sleep( 200 );
            ltCpu.Text = string.Format( "{0:N2}", cpuCounter.NextValue() );

            if ( !Page.IsPostBack )
            {
                ScriptManager.RegisterStartupScript( upDevice, upDevice.GetType(), "startTimer", "startTimer();", true );
            }

        }

        // Stolen: http://stackoverflow.com/questions/11/how-can-relative-time-be-calculated-in-c 
        private string TimeAgo( DateTime dt )
        {
            var ts = new TimeSpan( RockDateTime.Now.Ticks - dt.Ticks );
            double delta = Math.Abs( ts.TotalSeconds );

            if ( delta < 60 )
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }
            if ( delta < 120 )
            {
                return "a minute ago";
            }
            if ( delta < 2700 ) // 45 * 60
            {
                return ts.Minutes + " minutes ago";
            }
            if ( delta < 5400 ) // 90 * 60
            {
                return "an hour ago";
            }
            if ( delta < 86400 ) // 24 * 60 * 60
            {
                return ts.Hours + " hours ago";
            }
            if ( delta < 172800 ) // 48 * 60 * 60
            {
                return "yesterday";
            }
            if ( delta < 2592000 ) // 30 * 24 * 60 * 60
            {
                return ts.Days + " days ago";
            }
            if ( delta < 31104000 ) // 12 * 30 * 24 * 60 * 60
            {
                int months = Convert.ToInt32( Math.Floor( ( double ) ts.Days / 30 ) );
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            int years = Convert.ToInt32( Math.Floor( ( double ) ts.Days / 365 ) );
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }
}