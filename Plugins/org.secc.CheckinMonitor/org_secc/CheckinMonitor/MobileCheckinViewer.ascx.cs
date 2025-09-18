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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using Newtonsoft.Json;
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
    [DisplayName( "Mobile Check-in Viewer" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays active mobile check-in records for printing." )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Aggregated Label", "Binary file that is the parent pickup label", false )]
    public partial class MobileCheckinViewer : CheckInBlock
    {
        protected CheckinKioskTypeCache KioskType;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/ZebraPrint.js" );

            var kioskTypeCookie = this.Page.Request.Cookies["KioskTypeId"];
            if ( kioskTypeCookie != null )
            {
                KioskType = CheckinKioskTypeCache.Get( kioskTypeCookie.Value.AsInteger() );
            }

            if ( KioskType == null )
            {
                NavigateToPreviousPage();
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            if ( CurrentCheckInState == null )
            {
                NavigateToPreviousPage();
                return;
            }

            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                BindRepeater();
            }

        }

        private void BindRepeater()
        {
            var campus = KioskType.Campus;

            if ( campus == null )
            {
                nbError.Text = "Kiosk Type is not configured with a campus.";
                nbError.Visible = true;
                return;
            }

            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var groupQry = groupService.Queryable();

            var records = mobileCheckinRecordService.Queryable().AsNoTracking()
                .Where( r => r.CampusId == campus.Id && r.Status == MobileCheckinStatus.Active && r.CreatedDateTime >= Rock.RockDateTime.Today )
                .Join( groupQry,
                r => r.FamilyGroupId,
                g => g.Id,
                ( r, g ) => new
                {
                    Record = r,
                    Caption = g.Name,
                    Attendances = r.Attendances.Where( a => a.EndDateTime == null )
                } )
                .ToList()
                .Select( r => new MCRPoco
                {
                    Record = r.Record,
                    Caption = r.Caption,
                    SubCaption = string.Join( "<br>", r.Attendances.Select( a => string.Format( "{0}: {1} in {2} at {3}", a.PersonAlias.Person.NickName, a.Occurrence.Group.Name, a.Occurrence.Location.Name, a.Occurrence.Schedule.Name ) ) )
                } )
                .OrderBy( r => r.Caption )
                .ToList();

            rMCR.DataSource = records;
            rMCR.DataBind();

            pnlNoRecords.Visible = !records.Any();
        }

        protected void rMCR_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
        {
            if ( e.CommandName == "Checkin" )
            {
                var accessKey = e.CommandArgument.ToString();
                MobileCheckin( accessKey );
            }
        }

        private void MobileCheckin( string accessKey )
        {
            var mobileDidAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_DID_ATTEND ).Id;
            var mobileNotAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND ).Id;

            RockContext rockContext = new RockContext();
            MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );

            var mobileCheckinRecord = mobileCheckinRecordService.Queryable().Where( r => r.AccessKey == accessKey ).FirstOrDefault();

            if ( mobileCheckinRecord == null )
            {
                mdAlert.Show( "Mobile check-in record not found", ModalAlertType.Alert );
                BindRepeater();
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Canceled )
            {
                mdAlert.Show( "Mobile check-in record is expired.", ModalAlertType.Alert );
                BindRepeater();
                return;
            }
            else if ( mobileCheckinRecord.Status == MobileCheckinStatus.Complete )
            {
                mdAlert.Show( "Mobile check-in record has already been completed.", ModalAlertType.Alert );
                BindRepeater();
                return;
            }

            try
            {
                if ( mobileCheckinRecord == null )
                {
                    return;
                }

                List<CheckInLabel> labels = null;

                if ( mobileCheckinRecord.Attendances.Any( a => a.EndDateTime != null ) )
                {
                    var people = mobileCheckinRecord.Attendances.Select( a => a.PersonAlias.Person ).DistinctBy( p => p.Id ).ToList();
                    labels = CheckinLabelGen.GenerateLabels( people, CurrentCheckInState.Kiosk.Device, GetAttributeValue( "AggregatedLabel" ).AsGuidOrNull() );
                }
                else
                {
                    labels = JsonConvert.DeserializeObject<List<CheckInLabel>>( mobileCheckinRecord.SerializedCheckInState );
                }

                LabelPrinter labelPrinter = new LabelPrinter()
                {
                    Request = Request,
                    Labels = labels
                };

                labelPrinter.PrintNetworkLabels();
                var script = labelPrinter.GetClientScript();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "addLabelScript", script, true );

                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    if ( attendance.QualifierValueId == mobileDidAttendId )
                    {
                        attendance.DidAttend = true;
                        attendance.QualifierValueId = null;
                        attendance.StartDateTime = Rock.RockDateTime.Now;
                    }
                    else if ( attendance.QualifierValueId == mobileNotAttendId )
                    {
                        attendance.DidAttend = false;
                        attendance.QualifierValueId = null;
                    }
                    attendance.Note = "Completed mobile check-in at: " + CurrentCheckInState.Kiosk.Device.Name;
                }

                mobileCheckinRecord.Status = MobileCheckinStatus.Complete;

                rockContext.SaveChanges();

                //wait until we successfully save to update cache
                foreach ( var attendance in mobileCheckinRecord.Attendances )
                {
                    AttendanceCache.AddOrUpdate( attendance );

                }
                MobileCheckinRecordCache.Update( mobileCheckinRecord.Id );
                BindRepeater();
            }
            catch ( Exception e )
            {
                LogException( e );
                mdAlert.Show( "An unexpected issue occurred.", ModalAlertType.Alert );
                BindRepeater();
            }
        }


        protected void btnRefresh_Click( object sender, EventArgs e )
        {
            BindRepeater();
        }
        private class MCRPoco
        {
            public MobileCheckinRecord Record { get; set; }
            public string Caption { get; set; }
            public string SubCaption { get; set; }
        }

    }
}