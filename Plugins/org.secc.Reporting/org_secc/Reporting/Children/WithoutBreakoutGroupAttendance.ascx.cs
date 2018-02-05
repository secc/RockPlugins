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
    [DisplayName( "Without Breakout Group Attendance" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "List of children without a breakout group with attendance." )]

    [GroupField( "Parent Breakout Group", "Parent Group of the Breakout Groups" )]
    [TextField( "Schedule IDs", "Coma separated list of the Ids of schedules." )]
    public partial class WithoutBreakoutGroupAttendance : RockBlock
    {

        List<Schedule> schedules;
        List<string> selectedSchedules;
        RockContext rockContext;
        List<Dictionary<int, int>> scheduleValueDicts;
        Dictionary<int, DateTime> firstAttendanceDate;
        Dictionary<int, DateTime> lastAttendanceDate;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
        }

        private void BindGrid()
        {
            var scheduleIds = GetAttributeValue( "ScheduleIDs" );
            rockContext = new RockContext();
            schedules = new ScheduleService( rockContext )
                .GetByIds( GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList()
            ).ToList();

            var parentGroupGuidString = GetAttributeValue( "ParentBreakoutGroup" );
            if ( string.IsNullOrWhiteSpace( parentGroupGuidString ) )
            {
                return;
            }

            var parentGroupGuid = parentGroupGuidString.AsGuid();

            var groupService = new GroupService( rockContext );

            var parentGroup = groupService.Get( parentGroupGuid );

            var breakoutGroupMembers = groupService.Queryable()
                .SelectMany( g => g.Groups.Where( g2 => g2.IsActive && g2.ParentGroupId == parentGroup.Id) )
                .SelectMany( g => g.Members.Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active ) )
                .Select( m => m.Person )
                .SelectMany( p => p.Aliases )
                .Select( a => a.Id );

            var selectedScheduleIds = schedules.Select( s => s.Id ).ToList();

            var attendanceService = new AttendanceService( rockContext );
            var attendanceQry = attendanceService.Queryable()
                .Where( a => !breakoutGroupMembers.Contains( a.PersonAliasId ?? 0 )
                    && a.DidAttend == true
                    && a.ScheduleId != null
                    && selectedScheduleIds.Contains( a.ScheduleId ?? 0 ) );

            if ( gpGrade.SelectedGradeValue != null )
            {
                var graduationYear = Person.GraduationYearFromGradeOffset( gpGrade.SelectedGradeValue.Value.AsInteger() );
                attendanceQry = attendanceQry.Where( a => a.PersonAlias.Person.GraduationYear == graduationYear );
            }

            var lower = drRange.LowerValue;
            if ( lower != null )
            {
                attendanceQry = attendanceQry.Where( a => a.StartDateTime >= lower );
            }
            var upper = drRange.UpperValue;
            if ( upper != null )
            {
                upper = upper.Value.AddDays( 1 );
                attendanceQry = attendanceQry.Where( a => a.StartDateTime <= upper );
            }

            var attendance = attendanceQry.Select( a => new
            {
                Person = a.PersonAlias.Person,
                Attendance = a
            }
            ).ToList();

            var children = attendance
                .DistinctBy( a => a.Person.Id )
                .Select( a => a.Person )
                .ToList();

            // create default settings
            string filename = "WithoutBreakoutGroupAttendance.xlsx";
            string workSheetName = "List";
            string title = "Without Breakout Group Attendance";

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

            List<string> columns = new List<string>() { "Name", "Gender", "Birthdate", "Grade", "First Time", "Last Time", "Total" };
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
            List<int> scheduleTotals = new List<int>();

            var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .ToList();

            foreach ( var schedulePref in schedulePreference )
            {
                scheduleTotals.Add( 0 );
            }
            //print data
            foreach ( Person person in children )
            {
                var currentAttendances = attendance
                    .Where( a => a.Person.Id == person.Id )
                    .OrderBy( a => a.Attendance.StartDateTime ).ToList();

                SetExcelValue( worksheet.Cells[rowCounter, 1], person.FullName );
                SetExcelValue( worksheet.Cells[rowCounter, 2], person.Gender.ToString() );
                SetExcelValue( worksheet.Cells[rowCounter, 3], ( person.BirthDate ?? new DateTime() ).ToString( "MM/dd/yyyy" ) );
                SetExcelValue( worksheet.Cells[rowCounter, 4], person.GradeFormatted );
                var firstAttendance = currentAttendances.FirstOrDefault().Attendance.StartDateTime.ToString( "MM/dd/yyyy" );
                SetExcelValue( worksheet.Cells[rowCounter, 5], firstAttendance );
                var lastAttendance = currentAttendances.LastOrDefault().Attendance.StartDateTime.ToString( "MM/dd/yyyy" );
                SetExcelValue( worksheet.Cells[rowCounter, 6], lastAttendance );
                SetExcelValue( worksheet.Cells[rowCounter, 7], currentAttendances.Count );

                var i = 0;
                foreach ( var schedule in schedules )
                {
                    var count = currentAttendances.Where( a => a.Attendance.ScheduleId == schedule.Id ).Count();

                    SetExcelValue( worksheet.Cells[rowCounter, 8 + i], count );
                    i++;

                }
                worksheet.Cells[rowCounter, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[rowCounter, 7].Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );

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
        /// Handles the ExcelExportClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void Actions_ExcelExportClick( object sender, EventArgs e )
        {


            BindGrid();


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
}