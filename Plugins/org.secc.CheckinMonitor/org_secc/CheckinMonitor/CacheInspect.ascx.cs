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
using System.Web.UI.WebControls;
using org.secc.FamilyCheckin.Cache;
using Rock;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

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
            btnKioskTypes.CssClass = defaultCss;
            pnlOccurrences.Visible = true;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = false;
            pnlKioskTypes.Visible = false;
            pnlVerify.Visible = false;

            var occurrences = OccurrenceCache.All().Select( o => new CacheContainer( o, o.AccessKey ) ).ToList();

            gOccurrences.DataSource = occurrences;
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
            btnKioskTypes.CssClass = defaultCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = true;
            pnlKioskTypes.Visible = false;
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
            btnKioskTypes.CssClass = defaultCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = true;
            pnlMobileRecords.Visible = false;
            pnlKioskTypes.Visible = false;
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
            btnKioskTypes.CssClass = defaultCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = false;
            pnlKioskTypes.Visible = false;
            pnlVerify.Visible = true;

            VerifyCache();
        }

        private void VerifyCache()
        {
            List<string> errors = new List<string>();
            KioskTypeCache.Verify( ref errors );
            AttendanceCache.Verify( ref errors );
            MobileCheckinRecordCache.Verify( ref errors );
            OccurrenceCache.Verify( ref errors );

            if ( errors.Any() )
            {
                ltVerify.Text = string.Join( "<br>", errors );
            }
            else
            {
                ltVerify.Text = "<b>No Errors</b>";
            }
        }

        protected void btnKioskTypes_Click( object sender, EventArgs e )
        {
            btnOccurrences.CssClass = defaultCss;
            btnAttendances.CssClass = defaultCss;
            btnMobileRecords.CssClass = defaultCss;
            btnKioskTypes.CssClass = activeCss;
            pnlOccurrences.Visible = false;
            pnlAttendances.Visible = false;
            pnlMobileRecords.Visible = false;
            pnlKioskTypes.Visible = true;
            pnlVerify.Visible = false;

            var kioskTypes = KioskTypeCache.All().Select( k => new CacheContainer( k, k.Id ) ).ToList();
            gKioskTypes.DataSource = kioskTypes;
            gKioskTypes.DataBind();
        }

        class CacheContainer
        {
            public string Id { get; set; }
            public CacheContainer( IItemCache item, object id )
            {
                Item = item;
                Id = id.ToString();
            }

            public IItemCache Item { get; set; }
            public int Size
            {
                get
                {
                    return Item.ToJson().Length;
                }
            }

            public string Json
            {
                get
                {
                    return Item.ToJson();
                }
            }
        }

        protected void gKioskTypes_RowSelected( object sender, RowEventArgs e )
        {
            var id = e.RowKeyValue.ToString().AsInteger();
            var item = KioskTypeCache.Get( id );
            KioskTypeCache.ClearForTemplateId( item.CheckinTemplateId ?? 0 );
        }

        protected void btnFlushAttendance_Click( object sender, EventArgs e )
        {
            AttendanceCache.Clear();
            AttendanceCache.All();
        }

        protected void btnFlushMCR_Click( object sender, EventArgs e )
        {
            MobileCheckinRecordCache.Clear();
            MobileCheckinRecordCache.All();
        }

        protected void btnFlushKioskTypes_Click( object sender, EventArgs e )
        {
            KioskTypeCache.Clear();
            KioskTypeCache.All();

        }
    }
}