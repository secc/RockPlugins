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
    [DisplayName( "Breakout Group Attendance Summary" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "A filterable and sortable list of breakout groups." )]
    [GroupTypeField( "Breakout Group Type", "GroupType of the Breakout Groups" )]
    [TextField( "Schedule IDs", "Coma separated list of the Ids of schedules." )]
    public partial class BreakoutGroupAttendanceSummary : RockBlock
    {

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {

        }

        protected void btnGenerate_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var groupTypeGuid = GetAttributeValue( "BreakoutGroupType" ).AsGuid();
            var gQry = new GroupService( rockContext ).Queryable().Where( g => g.GroupType.Guid == groupTypeGuid );
            var gmQry = gQry.SelectMany( g => g.Members );

            var personQry = new PersonService( rockContext ).Queryable().Where( p => p.GraduationYear != null );

            if ( gpGrade.SelectedGradeValue != null )
            {
                var grade = Person.GraduationYearFromGradeOffset( gpGrade.SelectedGradeValue.Value.AsInteger() );
                personQry = personQry.Where( p => p.GraduationYear == grade );
            }

            var membersQry = gmQry.Join( personQry,
                gm => new { PersonId = gm.PersonId },
                p => new { PersonId = p.Id },
                ( gm, p ) => new { GroupId = gm.GroupId, PersonId = p.Id }
                );

            //get all the breakout groups where we have members
            var breakoutGroups = gQry.ToList()
                .Where( g => membersQry.Select( gm => gm.GroupId )
                 .Contains( g.Id ) )
                .OrderBy( g => g.Schedule.Id )
                .ThenBy( g => g.Name )
                .ToList();

            var schedules = new ScheduleService( rockContext )
                .GetByIds( GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList()
            ).ToList();

            var scheduleIds = GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList();

            var attendanceQry = new AttendanceService( rockContext ).Queryable();
            attendanceQry = attendanceQry.Where( a => a.SundayDate != null && scheduleIds.Contains( a.ScheduleId ?? 0 ) );
            var upper = drpRange.UpperValue;
            var lower = drpRange.LowerValue;
            if ( upper != null )
            {
                upper = upper.Value.AddDays( 1 );
                attendanceQry = attendanceQry.Where( a => a.StartDateTime <= upper );
            }
            if ( lower != null )
            {
                attendanceQry = attendanceQry.Where( a => a.StartDateTime >= lower );
            }

            //Begin creation of EXCEL file
            string filename = "RockExport.xls";

            ExcelPackage excel = new ExcelPackage();
            excel.Workbook.Properties.Title = "Breakout Group Attendance Summary";

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

            List<ExcelWorksheet> worksheets = new List<ExcelWorksheet>();

            worksheets.Add( excel.Workbook.Worksheets.Add( "Service Averages" ) );
            worksheets.Add( excel.Workbook.Worksheets.Add( "Attendance Summary" ) );
            foreach ( var worksheet in worksheets )
            {
                worksheet.PrinterSettings.LeftMargin = .5m;
                worksheet.PrinterSettings.RightMargin = .5m;
                worksheet.PrinterSettings.TopMargin = .5m;
                worksheet.PrinterSettings.BottomMargin = .5m;
            }
            //// write data to worksheet there are three supported data sources
            //// DataTables, DataViews and ILists

            int rowCounter0 = 4;
            int rowCounter1 = 4;
            int columnCounter0 = 0;
            int columnCounter1 = 0;

            List<string> columns0 = new List<string>() { "Service", "No Breakout Group" };
            List<string> columns1 = new List<string>() { "Week", "Service", "No Breakout Group" };
            foreach ( var group in breakoutGroups )
            {
                var name = string.Format( "{0} {1}", group.Schedule.GetCalenderEvent().DTStart.Value.TimeOfDay.ToTimeString(), group.Name[group.Name.Length - 1] );
                columns0.Add( name );
                columns1.Add( name );
            }
            foreach ( String column in columns0 )
            {
                columnCounter0++;
                worksheets[0].Cells[3, columnCounter0].Value = column.SplitCase();
            }
            foreach ( String column in columns1 )
            {
                columnCounter1++;
                worksheets[1].Cells[3, columnCounter1].Value = column.SplitCase();
            }

            var sundays = attendanceQry
                            .DistinctBy( a => a.SundayDate )
                            .Select( a => a.SundayDate )
                            .OrderBy( a => a )
                            .ToList();

            var attendance = attendanceQry
                .Join(
                    personQry,
                    a => new { PersonId = a.PersonAlias.PersonId },
                    p => new { PersonId = p.Id },
                    ( a, p ) => new
                    {
                        PersonId = p.Id,
                        ScheduleId = a.ScheduleId,
                        SundayDate = a.SundayDate
                    }
                    ).ToList();

            var allMemberIds = membersQry.Select( m => m.PersonId ).ToList();

            //Worksheet 0 / Service Averages
            foreach ( var schedule in schedules )
            {
                SetExcelValue( worksheets[0].Cells[rowCounter0, 1], schedule.Name );

                var nonMembers = attendance.Where( a =>
                      a.ScheduleId == schedule.Id
                     && !allMemberIds.Contains( a.PersonId )
                    ).Count() / sundays.Count();

                SetExcelValue( worksheets[0].Cells[rowCounter0, 2], nonMembers );
                var i = 0;
                foreach ( var group in breakoutGroups )
                {
                    var breakoutGroupMemberPersonIds = membersQry.Where( m => m.GroupId == group.Id ).Select( m => m.PersonId ).ToList();
                    var groupCount = attendance.Where( a =>
                      a.ScheduleId == schedule.Id
                     && breakoutGroupMemberPersonIds.Contains( a.PersonId )
                    ).Count() / sundays.Count();
                    SetExcelValue( worksheets[0].Cells[rowCounter0, 3 + i], groupCount );
                    i++;
                }
                rowCounter0++;
            }

            //Worsheet 1 / Attendance Summary
            foreach ( var sunday in sundays )
            {
                SetExcelValue( worksheets[1].Cells[rowCounter1, 1], sunday.ToString( "MM/dd/yyyy" ) );
                var nonMemberTotal = 0;
                var totals = new List<int>();
                foreach ( var group in breakoutGroups )
                {
                    totals.Add( 0 );
                }

                foreach ( var schedule in schedules )
                {
                    SetExcelValue( worksheets[1].Cells[rowCounter1, 2], schedule.Name );

                    var nonMembers = attendance.Where( a =>
                         a.SundayDate == sunday
                         && a.ScheduleId == schedule.Id
                         && !allMemberIds.Contains( a.PersonId )
                        ).Count();
                    nonMemberTotal += nonMembers;
                    SetExcelValue( worksheets[1].Cells[rowCounter1, 3], nonMembers );

                    var i = 0;

                    foreach ( var group in breakoutGroups )
                    {
                        var breakoutGroupMemberPersonIds = membersQry.Where( m => m.GroupId == group.Id ).Select( m => m.PersonId ).ToList();
                        var groupCount = attendance.Where( a =>
                            a.SundayDate == sunday
                            && a.ScheduleId == schedule.Id
                            && breakoutGroupMemberPersonIds.Contains( a.PersonId )
                        ).Count();
                        SetExcelValue( worksheets[1].Cells[rowCounter1, 4 + i], groupCount );
                        totals[i] += groupCount;
                        i++;
                    }
                    rowCounter1++;
                }
                SetExcelValue( worksheets[1].Cells[rowCounter1, 2], "Total" );
                worksheets[1].Cells[rowCounter1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheets[1].Cells[rowCounter1, 3].Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                worksheets[1].Cells[rowCounter1, 3].Style.Font.Color.SetColor( Color.Black );

                SetExcelValue( worksheets[1].Cells[rowCounter1, 3], nonMemberTotal );
                worksheets[1].Cells[rowCounter1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheets[1].Cells[rowCounter1, 4].Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                worksheets[1].Cells[rowCounter1, 4].Style.Font.Color.SetColor( Color.Black );

                for ( var index = 0; index < totals.Count(); index++ )
                {
                    SetExcelValue( worksheets[1].Cells[rowCounter1, 4 + index], totals[index] );
                    worksheets[1].Cells[rowCounter1, 5 + index].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheets[1].Cells[rowCounter1, 5 + index].Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                    worksheets[1].Cells[rowCounter1, 5 + index].Style.Font.Color.SetColor( Color.Black );
                }
                rowCounter1++;
            }


            //Finish EXCEL
            var range = worksheets[0].Cells[3, 1, rowCounter0, columnCounter0];

            var table = worksheets[0].Tables.Add( range, "table1" );

            // ensure each column in the table has a unique name
            var columnNames = worksheets[0].Cells[3, 1, 3, columnCounter0].Select( a => new { OrigColumnName = a.Text, Cell = a } ).ToList();
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

            worksheets[0].Cells[1, 1].Value = "Service Averages";
            worksheets[1].Cells[1, 1].Value = "Attendance Summary";
            foreach ( var worksheet in worksheets )
            {
                // format header range
                using ( ExcelRange r = worksheet.Cells[3, 1, 3, columnCounter1] )
                {
                    r.Style.Font.Bold = true;
                    r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor( Color.FromArgb( 223, 223, 223 ) );
                    r.Style.Font.Color.SetColor( Color.Black );
                    r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                }

                // format and set title
                using ( ExcelRange r = worksheet.Cells[1, 1, 1, columnCounter1] )
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
                worksheet.View.FreezePanes( 3, 1 );
                worksheet.Cells.AutoFitColumns( 0 );
            }

            // autofit columns for all cells


            // set some footer text

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
    }
}