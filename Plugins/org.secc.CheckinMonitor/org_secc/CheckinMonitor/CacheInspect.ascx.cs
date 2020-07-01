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
using org.secc.FamilyCheckin.Cache;
using OpenXmlPowerTools;
using org.secc.FamilyCheckin.Model;
using Rock.ServiceObjects.GeoCoder;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Cache Inspect" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays custom checkin cache values in a lovely grid." )]

    public partial class CacheInspect : RockBlock
    {
        private const string defaultCss = "btn btn-default";
        private const string activeCss = "btn btn-primary";

        protected override void OnLoad( EventArgs e )
        {


        }


        protected void btnOccurrences_Click( object sender, EventArgs e )
        {
            btnOccurrences.CssClass = activeCss;
            btnAttendances.CssClass = defaultCss;
            btnMobileRecords.CssClass = defaultCss;
            pnlOccurrences.Visible = true;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = false;
            pnlVerify.Visible = false;
            gOccurrences.DataSource = OccurrenceCache.All();
            gOccurrences.DataBind();
        }

        protected void btnAttendances_Click( object sender, EventArgs e )
        {
            ShowAttendances( null );

        }

        protected void btnMobileRecords_Click( object sender, EventArgs e )
        {
            btnOccurrences.CssClass = defaultCss;
            btnAttendances.CssClass = defaultCss;
            btnMobileRecords.CssClass = activeCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = true;
            pnlVerify.Visible = false;
            gMobileRecords.DataSource = MobileCheckinRecordCache.All();
            gMobileRecords.DataBind();
        }

        protected void gMobileRecords_RowSelected( object sender, RowEventArgs e )
        {
            var recordId = e.RowKeyId;
            var record = MobileCheckinRecordCache.Get( recordId );
            ShowAttendances( record.AttendanceIds );
        }

        protected void gOccurrences_RowSelected( object sender, RowEventArgs e )
        {
            var accessKey = ( string ) e.RowKeyValue;
            var occurrence = OccurrenceCache.Get( accessKey );
            ShowAttendances( occurrence.Attendances.Select( a => a.Id ).ToList() );
        }

        private void ShowAttendances( List<int> attendanceIds )
        {
            btnOccurrences.CssClass = defaultCss;
            btnAttendances.CssClass = activeCss;
            btnMobileRecords.CssClass = defaultCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = true;
            pnlMobileRecords.Visible = false;
            pnlVerify.Visible = false;
            var attendances = AttendanceCache.All();

            if ( attendanceIds != null )
            {
                attendances = attendances.Where( a => attendanceIds.Contains( a.Id ) ).ToList();
            }

            gAttendances.DataSource = attendances;
            gAttendances.DataBind();
        }


        protected void btnVerify_Click( object sender, EventArgs e )
        {
            btnOccurrences.CssClass = defaultCss;
            btnAttendances.CssClass = defaultCss;
            btnMobileRecords.CssClass = defaultCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = true;
            pnlMobileRecords.Visible = false;
            pnlVerify.Visible = true;

            VerifyCache();
        }

        private void VerifyCache()
        {
            List<string> errors = new List<string>();
            KioskTypeCache.Verify( ref errors );
            AttendanceCache.Verify( ref errors );
            MobileCheckinRecordCache.Verify( ref errors );

            if ( errors.Any() )
            {
                ltVerify.Text = string.Join( "<br>", errors );
            }
            else
            {
                ltVerify.Text = "<b>No Errors</b>";
            }
        }
    }
}