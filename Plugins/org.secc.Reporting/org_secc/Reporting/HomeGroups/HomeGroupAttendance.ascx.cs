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
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting.HomeGroups
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Home Group Attendance" )]
    [Category( "SECC > Reporting > Home Groups" )]
    [Description( "Tool to make reporting simpler." )]
    public partial class HomeGroupAttendance : RockBlock
    {


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );


        }

        protected void btnGo_Click( object sender, EventArgs e )
        {
            var groupId = PageParameter( "GroupId" ).AsInteger();
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var group = groupService.Get( groupId );
            var childGroups = group.Groups;
            var childGroupIds = childGroups.Select( g => g.Id ).ToList();
            var memberCount = childGroups.SelectMany( g => g.Members ).Count();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable()
                .Where( a => a.StartDateTime >= dpStart.DateRange.Start
                && a.StartDateTime <= dpStart.DateRange.End
                && childGroupIds.Contains( a.GroupId ?? 0 )
                );
            var groupsAttendance = attendances.DistinctBy( a => a.GroupId ).Count();
            var didAttend = attendances.Where( a => a.DidAttend == true ).Count();
            var total = attendances.Count();
            var percent = Decimal.Divide( didAttend, total );
            var extrapolated = memberCount * percent;

            pnlResult.Visible = true;
            lGroups.Text = childGroups.Count().ToString();
            lMember.Text = memberCount.ToString();
            ltGroupsAttendance.Text = groupsAttendance.ToString();
            lActual.Text = didAttend.ToString();
            lAttendance.Text = ( percent * 100 ).ToString( "#.##" );
            lAccounted.Text = total.ToString();
            lExtrapolated.Text = extrapolated.ToString( "#.##" );
        }
    }
}