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

namespace RockWeb.Blocks.Reporting.Children
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Breakout Group Attendance" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "A filterable and sortable list of breakout groups." )]

    [GroupTypeField( "Breakout Group Type", "GroupType of the Breakout Groups" )]
    [TextField( "Schedule IDs", "Coma separated list of the Ids of schedules." )]
    public partial class BreakoutGroupAttendance : RockBlock
    {

        List<Schedule> schedules;
        List<string> selectedSchedules;
        RockContext rockContext;
        List<Dictionary<int, int>> scheduleValueDicts;
        Dictionary<int, DateTime> firstAttendanceDate;
        Dictionary<int, DateTime> lastAttendanceDate;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gBreakoutGroups.ShowActionRow = false;
            gBreakoutGroups.GridRebind += RebindGrid;
        }

        private void RebindGrid( object sender, GridRebindEventArgs e )
        {
            var scheduleIds = GetAttributeValue( "ScheduleIDs" );
            rockContext = new RockContext();
            schedules = new ScheduleService( rockContext )
                .GetByIds( GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList()
            ).ToList();

            BindGrid();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var scheduleIds = GetAttributeValue( "ScheduleIDs" );
            rockContext = new RockContext();
            schedules = new ScheduleService( rockContext )
                .GetByIds( GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList()
            ).ToList();

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            var groupTypeGuid = GetAttributeValue( "BreakoutGroupType" );
            if ( string.IsNullOrWhiteSpace( groupTypeGuid ) )
            {
                return;
            }

            var groupType = new GroupTypeService( rockContext ).Get( groupTypeGuid.AsGuid() );
            if ( groupType == null )
            {
                return;
            }

            List<BreakoutGroupMember> data = new List<BreakoutGroupMember>();

            BindFilter( groupType.Groups, schedules );

            List<BreakoutGroupMember> breakoutGroupMembers = GetBreakoutGroupMembers();

            //remove dynamically added columns if any exist
            while ( gBreakoutGroups.Columns.Count > 8 )
            {
                gBreakoutGroups.Columns.RemoveAt( 8 );
            }

            foreach ( var schedule in schedules )
            {
                if ( selectedSchedules.Contains( schedule.Id.ToString() ) )
                {
                    var field = new RockLiteralField();
                    field.HeaderText = schedule.Name;
                    gBreakoutGroups.Columns.Add( field );
                }
            }

            gBreakoutGroups.DataSource = breakoutGroupMembers;
            gBreakoutGroups.DataBind();
        }

        private List<BreakoutGroupMember> GetBreakoutGroupMembers()
        {
            var selectedGroups = new List<int>();
            foreach ( ListItem item in cblGroups.Items )
            {
                if ( item.Selected )
                {
                    selectedGroups.Add( item.Value.AsInteger() );
                }
            }

            var breakoutGroupMembers = new GroupService( rockContext )
                .GetByIds( selectedGroups )
                .SelectMany( g => g.Members.Where( m => !m.GroupRole.IsLeader && m.GroupMemberStatus == GroupMemberStatus.Active ) )
                .Select( gm => new BreakoutGroupMember
                {
                    Id = gm.PersonId,
                    group = gm.Group,
                    Name = gm.Person.LastName + ", " + gm.Person.NickName,
                    person = gm.Person,
                    Gender = gm.Person.Gender,
                    Birthdate = gm.Person.BirthDate,
                    Breakout = gm.Group.Name
                }
                )
                .OrderBy( a => a.Breakout )
                .ThenBy( a => a.Name )
                .ToList();

            selectedSchedules = new List<string>();
            scheduleValueDicts = new List<Dictionary<int, int>>();

            var personIds = breakoutGroupMembers.Select( bgm => bgm.person.Id ).ToList();
            var attendanceService = new AttendanceService( rockContext );

            var lower = drRange.LowerValue;
            var upper = drRange.UpperValue;
            if ( upper != null )
            {
                upper = upper.Value.AddDays( 1 );
            }

            //Build dictionaries containing the values for the attendance count to be added on row bind
            foreach ( ListItem item in cblSchedules.Items )
            {
                if ( item.Selected )
                {
                    selectedSchedules.Add( item.Value );
                    var scheduleId = item.Value.AsInteger();

                    var attendanceQry = attendanceService.Queryable()
                        .Where( a => personIds.Contains( a.PersonAlias.PersonId ) && a.ScheduleId == scheduleId );

                    if ( upper != null )
                    {
                        attendanceQry = attendanceQry.Where( a => a.StartDateTime <= upper );
                    }
                    if ( lower != null )
                    {
                        attendanceQry = attendanceQry.Where( a => a.StartDateTime >= lower );
                    }
                    var groupby = attendanceQry.GroupBy( a => a.PersonAlias.PersonId )
                        .Select( g => new { Key = g.Key, Value = g.Count() } ).ToDictionary( kvp => kvp.Key, kvp => kvp.Value );
                    scheduleValueDicts.Add( groupby );
                }
            }

            //Get first and last attendance dates
            List<int> selectedSchedulesInt = selectedSchedules.Select( ss => ss.AsInteger() ).ToList();

            var attendanceDateQry = attendanceService.Queryable()
                .Where( a => personIds.Contains( a.PersonAlias.PersonId ) && a.ScheduleId != null && selectedSchedulesInt.Contains( a.ScheduleId ?? 0 ) );

            if ( upper != null )
            {
                attendanceDateQry = attendanceDateQry.Where( a => a.StartDateTime <= upper );
            }
            if ( lower != null )
            {
                attendanceDateQry = attendanceDateQry.Where( a => a.StartDateTime >= lower );
            }

            var attendanceDateSelect = attendanceDateQry
                .Select( a => new { Key = a.PersonAlias.PersonId, Value = a.StartDateTime } );

            firstAttendanceDate = attendanceDateSelect
                .OrderBy( a => a.Value )
                .DistinctBy( a => a.Key )
                .ToDictionary( a => a.Key, a => a.Value );

            lastAttendanceDate = attendanceDateSelect
                .OrderByDescending( a => a.Value )
                .DistinctBy( a => a.Key )
                .ToDictionary( a => a.Key, a => a.Value );

            return breakoutGroupMembers;
        }

        private void BindFilter( ICollection<Group> groups, ICollection<Schedule> schedules )
        {

            //Group checkbox
            cblGroups.Items.Clear();
            var scheduleIds = schedules.Select( s => s.Id );
            foreach ( var group in groups.Where( g => scheduleIds.Contains( g.ScheduleId ?? 0 ) ).OrderBy( g => g.Name ) )
            {
                ListItem listItem = new ListItem()
                {
                    Value = group.Id.ToString(),
                    Text = group.Name
                };
                cblGroups.Items.Add( listItem );
            }

            var groupsWithoutSchedule = groups.Where( g => g.Schedule == null );
            if ( groupsWithoutSchedule.Any() )
            {
                nbMissingSchedules.Visible = true;
                nbMissingSchedules.Text = "The following breakout groups do not have a schedule:";
                foreach ( var group in groupsWithoutSchedule )
                {
                    nbMissingSchedules.Text += " " + group.Name;
                }
            }
            else
            {
                nbMissingSchedules.Visible = true;
            }

            var groupPreference = GetUserPreference( BlockCache.Guid.ToString() + "Group" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

            //Pre-select based on user preference
            if ( groupPreference.Any() )
            {
                foreach ( ListItem item in cblGroups.Items )
                {
                    if ( groupPreference.Contains( item.Value ) )
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                foreach ( ListItem item in cblGroups.Items )
                {
                    item.Selected = true;
                }
            }

            //Schedule checkbox
            cblSchedules.Items.Clear();
            foreach ( var schedule in schedules )
            {
                ListItem listItem = new ListItem()
                {
                    Value = schedule.Id.ToString(),
                    Text = schedule.Name
                };
                cblSchedules.Items.Add( listItem );
            }
            var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

            //Pre-select based on user preference
            if ( schedulePreference.Any() )
            {
                foreach ( ListItem item in cblSchedules.Items )
                {
                    if ( schedulePreference.Contains( item.Value ) )
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                foreach ( ListItem item in cblSchedules.Items )
                {
                    item.Selected = true;
                }
            }

            //DateRange
            var lower = GetUserPreference( BlockCache.Guid.ToString() + "Lower" );
            var upper = GetUserPreference( BlockCache.Guid.ToString() + "Upper" );

            if ( !string.IsNullOrWhiteSpace( lower ) && !string.IsNullOrWhiteSpace( upper ) )
            {
                drRange.UpperValue = DateTime.Parse( upper );
                drRange.LowerValue = DateTime.Parse( lower );
            }


        }

        protected void gBreakoutGroups_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var total = 0;
                var attendanceService = new AttendanceService( rockContext ).Queryable();
                BreakoutGroupMember bgm = ( BreakoutGroupMember ) e.Row.DataItem;
                e.Row.Cells[4].Text = bgm.person.GradeFormatted;
                e.Row.Cells[3].Text = ( bgm.person.BirthDate ?? new DateTime() ).ToString( "MM/dd/yyyy " );

                var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

                var i = 0;

                if ( firstAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    e.Row.Cells[5].Text = firstAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy " );
                }

                if ( lastAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    e.Row.Cells[6].Text = lastAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy " );
                }

                if ( schedulePreference.Any() )
                {
                    foreach ( var schedule in schedules )
                    {
                        if ( !schedulePreference.Contains( schedule.Id.ToString() ) )
                        {
                            continue;
                        }

                        var attendance = 0;

                        if ( scheduleValueDicts[i].ContainsKey( bgm.person.Id ) )
                        {
                            attendance = scheduleValueDicts[i][bgm.person.Id];
                        }


                        total += attendance;
                        e.Row.Cells[8 + i].Text = attendance.ToString();
                        i++;
                    }
                }
                e.Row.Cells[7].Text = total.ToString();
            }
        }

        protected void fBreakoutGroups_ApplyFilterClick( object sender, EventArgs e )
        {
            var groupPreferences = new List<string>();

            foreach ( ListItem item in cblGroups.Items )
            {
                if ( item.Selected )
                {
                    groupPreferences.Add( item.Value );
                }
            }
            SetUserPreference( BlockCache.Guid.ToString() + "Group", string.Join( ",", groupPreferences ) );

            var schedulePreferences = new List<string>();

            foreach ( ListItem item in cblSchedules.Items )
            {
                if ( item.Selected )
                {
                    schedulePreferences.Add( item.Value );
                }
            }
            SetUserPreference( BlockCache.Guid.ToString() + "Schedule", string.Join( ",", schedulePreferences ) );

            if ( drRange.LowerValue != null )
            {
                SetUserPreference( BlockCache.Guid.ToString() + "Lower", drRange.LowerValue.ToString() );
            }
            else
            {
                SetUserPreference( BlockCache.Guid.ToString() + "Lower", "" );
            }

            if ( drRange.UpperValue != null )
            {
                SetUserPreference( BlockCache.Guid.ToString() + "Upper", drRange.UpperValue.ToString() );
            }
            else
            {
                SetUserPreference( BlockCache.Guid.ToString() + "Upper", "" );
            }
            BindGrid();
        }


        /// <summary>
        /// Handles the ExcelExportClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void Actions_ExcelExportClick( object sender, EventArgs e )
        {
            // create default settings
            string filename = gBreakoutGroups.ExportFilename;
            string workSheetName = "List";
            string title = "Breakout Group Attendance";

            ExcelPackage excel = new ExcelPackage();
            excel.Workbook.Properties.Title = title;

            // add author info
            Rock.Model.UserLogin userLogin = Rock.Model.UserLoginService.GetCurrentUser();
            if ( userLogin != null )
            {
                excel.Workbook.Properties.Author = userLogin.Person.FullName;
            }
            else
            {
                excel.Workbook.Properties.Author = "Rock";
            }

            // add the page that created this
            excel.Workbook.Properties.SetCustomPropertyValue( "Source", this.Page.Request.Url.OriginalString );

            ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( workSheetName );
            worksheet.PrinterSettings.LeftMargin = .5m;
            worksheet.PrinterSettings.RightMargin = .5m;
            worksheet.PrinterSettings.TopMargin = .5m;
            worksheet.PrinterSettings.BottomMargin = .5m;

            //// write data to worksheet there are three supported data sources
            //// DataTables, DataViews and ILists

            int rowCounter = 4;
            int columnCounter = 0;

            List<string> columns = new List<string>() { "Breakout", "Name", "Gender", "Birthdate", "Grade", "First Time", "Last Time", "Total" };
            foreach ( var schedule in schedules )
            {
                columns.Add( schedule.Name );
            }

            // print headings
            foreach ( String column in columns )
            {
                columnCounter++;
                worksheet.Cells[3, columnCounter].Value = column.SplitCase();
            }

            int lastBreakoutGroupId = 0;
            int lastBreakoutGroupTotal = 0;
            List<int> scheduleTotals = new List<int>();

            var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .ToList();

            foreach ( var schedulePref in schedulePreference )
            {
                scheduleTotals.Add( 0 );
            }

            // print data
            foreach ( BreakoutGroupMember bgm in GetBreakoutGroupMembers() )
            {
                if ( lastBreakoutGroupId == 0 )
                {
                    lastBreakoutGroupId = bgm.group.Id;
                }

                if ( lastBreakoutGroupId != bgm.group.Id )
                {
                    SetExcelValue( worksheet.Cells[rowCounter, 1], worksheet.Cells[rowCounter - 1, 1].Text );
                    lastBreakoutGroupId = bgm.group.Id;
                    SetExcelValue( worksheet.Cells[rowCounter, 7], "Breakout Total" );
                    worksheet.Cells[rowCounter, 7].Style.Font.Bold = true;
                    SetExcelValue( worksheet.Cells[rowCounter, 8], lastBreakoutGroupTotal );
                    worksheet.Cells[rowCounter, 8].Style.Font.Bold = true;
                    for ( int j = 0; j < scheduleTotals.Count; j++ )
                    {
                        SetExcelValue( worksheet.Cells[rowCounter, 9 + j], scheduleTotals[j] );
                        worksheet.Cells[rowCounter, 9 + j].Style.Font.Bold = true;
                        scheduleTotals[j] = 0;
                    }
                    lastBreakoutGroupTotal = 0;
                    rowCounter++;
                }

                SetExcelValue( worksheet.Cells[rowCounter, 1], bgm.Breakout );
                SetExcelValue( worksheet.Cells[rowCounter, 2], bgm.Name );
                SetExcelValue( worksheet.Cells[rowCounter, 3], bgm.Gender );
                SetExcelValue( worksheet.Cells[rowCounter, 4], ( bgm.person.BirthDate ?? new DateTime() ).ToString( "MM/dd/yyyy" ) );
                SetExcelValue( worksheet.Cells[rowCounter, 5], bgm.person.GradeFormatted );

                if ( firstAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    SetExcelValue( worksheet.Cells[rowCounter, 6], firstAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy" ) );
                }
                if ( lastAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    SetExcelValue( worksheet.Cells[rowCounter, 7], lastAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy" ) );
                }

                var i = 0;
                var total = 0;
                if ( schedulePreference.Any() )
                {
                    foreach ( var schedule in schedules )
                    {
                        if ( !schedulePreference.Contains( schedule.Id.ToString() ) )
                        {
                            continue;
                        }

                        var attendance = 0;

                        if ( scheduleValueDicts[i].ContainsKey( bgm.person.Id ) )
                        {
                            attendance = scheduleValueDicts[i][bgm.person.Id];
                        }

                        //update row total
                        total += attendance;

                        //update attendance total
                        scheduleTotals[i] += attendance;

                        SetExcelValue( worksheet.Cells[rowCounter, 9 + i], attendance );
                        i++;
                    }
                }
                SetExcelValue( worksheet.Cells[rowCounter, 8], total );
                worksheet.Cells[rowCounter, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[rowCounter, 8].Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                lastBreakoutGroupTotal += total;

                rowCounter++;
            }
            var range = worksheet.Cells[3, 1, rowCounter, columnCounter];

            var table = worksheet.Tables.Add( range, "table1" );

            // ensure each column in the table has a unique name
            var columnNames = worksheet.Cells[3, 1, 3, columnCounter].Select( a => new { OrigColumnName = a.Text, Cell = a } ).ToList();
            columnNames.Reverse();
            foreach ( var col in columnNames )
            {
                int duplicateSuffix = 0;
                string uniqueName = col.OrigColumnName;

                // increment the suffix by 1 until there is only one column with that name
                while ( columnNames.Where( a => a.Cell.Text == uniqueName ).Count() > 1 )
                {
                    duplicateSuffix++;
                    uniqueName = col.OrigColumnName + duplicateSuffix.ToString();
                    col.Cell.Value = uniqueName;
                }
            }

            table.ShowFilter = true;
            table.TableStyle = OfficeOpenXml.Table.TableStyles.None;

            // format header range
            using ( ExcelRange r = worksheet.Cells[3, 1, 3, columnCounter] )
            {
                r.Style.Font.Bold = true;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                r.Style.Font.Color.SetColor( Color.Black );
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            }

            // format and set title
            worksheet.Cells[1, 1].Value = title;
            using ( ExcelRange r = worksheet.Cells[1, 1, 1, columnCounter] )
            {
                r.Merge = true;
                r.Style.Font.SetFromFont( new Font( "Calibri", 22, FontStyle.Regular ) );
                r.Style.Font.Color.SetColor( Color.White );
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 34, 41, 55 ) );

                // set border
                r.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                r.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // TODO: add image to worksheet

            // freeze panes
            worksheet.View.FreezePanes( 3, 1 );

            // autofit columns for all cells
            worksheet.Cells.AutoFitColumns( 0 );

            // Set all the column widths
            worksheet.Column( 2 ).Width = 20;
            worksheet.Column( 4 ).Width = 30;
            worksheet.Column( 6 ).Width = 45;

            // add alternating highlights

            // set some footer text
            worksheet.HeaderFooter.OddHeader.CenteredText = title;
            worksheet.HeaderFooter.OddFooter.RightAlignedText = string.Format( "Page {0} of {1}", ExcelHeaderFooter.PageNumber, ExcelHeaderFooter.NumberOfPages );
            byte[] byteArray;
            using ( MemoryStream ms = new MemoryStream() )
            {
                excel.SaveAs( ms );
                byteArray = ms.ToArray();
            }

            // send the spreadsheet to the browser
            this.Page.EnableViewState = false;
            this.Page.Response.Clear();
            this.Page.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            this.Page.Response.AppendHeader( "Content-Disposition", "attachment; filename=" + filename );

            this.Page.Response.Charset = string.Empty;
            this.Page.Response.BinaryWrite( byteArray );
            this.Page.Response.Flush();
            this.Page.Response.End();
        }

        /// <summary>
        /// Formats the export value.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="exportValue">The export value.</param>
        private void SetExcelValue( ExcelRange range, object exportValue )
        {
            if ( exportValue != null &&
                ( exportValue is decimal || exportValue is decimal? ||
                exportValue is int || exportValue is int? ||
                exportValue is double || exportValue is double? ||
                exportValue is DateTime || exportValue is DateTime? ) )
            {
                range.Value = exportValue;
            }
            else
            {
                string value = exportValue != null ? exportValue.ToString().ConvertBrToCrLf().Replace( "&nbsp;", " " ) : string.Empty;
                range.Value = value;
                if ( value.Contains( Environment.NewLine ) )
                {
                    range.Style.WrapText = true;
                }
            }
        }

        protected void gBreakoutGroups_RowSelected( object sender, RowEventArgs e )
        {
            var personId = ( int ) e.RowKeyValue;
            if ( personId != 0 )
            {
                Response.Redirect( string.Format( "/Person/{0}", personId ) );
            }
        }
    }
    class BreakoutGroupMember
    {
        public int Id { get; set; }
        public Rock.Model.Group group { get; set; }
        public Person person { get; set; }
        public string Breakout { get; set; }
        public string Name { get; set; }
        public DateTime? Birthdate { get; set; }
        public Gender Gender { get; set; }
        public string Grade { get; set; }
    }
}