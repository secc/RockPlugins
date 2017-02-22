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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Blocks.Reporting.Children
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Breakout Group Handout" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "Report generator for breakout group handouts." )]

    [GroupField( "Parent Breakout Group", "Parent Group of the Breakout Groups", key: "ParentGroup" )]
    public partial class BreakoutGroupHandout : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                BindCheckBoxList();
            }
        }

        private void BindCheckBoxList()
        {
            RockContext rockContext = new RockContext();
            var parentGroup = new GroupService( rockContext ).Get( GetAttributeValue( "ParentGroup" ).AsGuid() );
            if ( parentGroup == null )
            {
                return;
            }
            var groups = parentGroup.Groups;

            //Group checkbox
            cblGroups.Items.Clear();
            foreach ( var group in groups )
            {
                ListItem listItem = new ListItem()
                {
                    Value = group.Id.ToString(),
                    Text = group.Name
                };
                cblGroups.Items.Add( listItem );
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
        }

        private void SavePreference()
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
        }


        /// <summary>
        /// Handles the ExcelExportClick event of the Actions control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void ExcelExportClick( object sender, EventArgs e )
        {
            SavePreference();
            RockContext rockContext = new RockContext();
            var groupIds = cblGroups.SelectedValuesAsInt;
            List<Group> groups = new GroupService( rockContext ).GetByIds( groupIds ).ToList();

            // create default settings
            string filename = "Breakout Group Handout";
            string title = "Hospital Report";

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

            int count = 0;

            foreach ( var group in groups )
            {
                count++;
                ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( count.ToString() + ": " + group.Name );
                worksheet.PrinterSettings.LeftMargin = .5m;
                worksheet.PrinterSettings.RightMargin = .5m;
                worksheet.PrinterSettings.TopMargin = .5m;
                worksheet.PrinterSettings.BottomMargin = .5m;

                //Print Title
                // format and set title
                worksheet.Cells[1, 1].Value = group.Name;
                using ( ExcelRange r = worksheet.Cells[1, 1, 1, 8] )
                {
                    r.Merge = true;
                    r.Style.Font.SetFromFont( new Font( "Calibri", 28, FontStyle.Regular ) );
                    r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    // set border
                    r.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                worksheet.Cells[2, 1].Value = Rock.RockDateTime.Today.ToString( "MMMM d, yyyy" );
                using ( ExcelRange r = worksheet.Cells[2, 1, 2, 8] )
                {
                    r.Merge = true;
                    r.Style.Font.SetFromFont( new Font( "Calibri", 20, FontStyle.Regular ) );
                    r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    // set border
                    r.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    r.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }
                using ( ExcelRange r = worksheet.Cells[3, 1, 3, 8] )
                {
                    r.Style.Font.Bold = true;
                }
                SetExcelValue( worksheet.Cells[3, 1], "Name" );
                SetExcelValue( worksheet.Cells[3, 2], "Age" );
                SetExcelValue( worksheet.Cells[3, 3], "Parents" );
                SetExcelValue( worksheet.Cells[3, 4], "Parent's Numbers" );
                SetExcelValue( worksheet.Cells[3, 5], "Parent's Email" );
                SetExcelValue( worksheet.Cells[3, 6], "Address" );
                SetExcelValue( worksheet.Cells[3, 7], "Baptism Date" );
                SetExcelValue( worksheet.Cells[3, 8], "Church Membership" );

                var rowCounter = 4;

                var groupMembers = group.Members;
                foreach ( var member in groupMembers.OrderBy( gm => gm.Person.NickName ).ToList() )
                {
                    var person = member.Person;
                    person.LoadAttributes();
                    SetExcelValue( worksheet.Cells[rowCounter, 1], person );
                    SetExcelValue( worksheet.Cells[rowCounter, 2], person.Age );
                    var familyMembers = person
                        .GetFamilyMembers()
                        .Where( m => m.GroupRoleId == 3 )
                        .Select( m => m.Person )
                        .ToList();

                    SetExcelValue( worksheet.Cells[rowCounter, 3], string.Join( ",", familyMembers.Select( p => p.FullName ) ) );

                    var phonenumbers = string.Join( ",", familyMembers.SelectMany( p => p.PhoneNumbers.Select( pn => pn.NumberFormatted ) ).ToList() );
                    SetExcelValue( worksheet.Cells[rowCounter, 4], string.Join( ",", phonenumbers ) );
                    SetExcelValue( worksheet.Cells[rowCounter, 5], string.Join( ",", familyMembers.Select( p => p.Email ) ) );
                    var homelocation = person.GetHomeLocation();
                    var address = "";
                    if ( homelocation == null )
                    {
                        address = "[No location on record]";
                    }
                    else
                    {
                        address = homelocation.ToString();
                    }
                    SetExcelValue( worksheet.Cells[rowCounter, 6], address );

                    var baptismDateString = person.GetAttributeValue( "Arena-34-404" );
                    if ( !string.IsNullOrEmpty( baptismDateString ) )
                    {
                        var baptismDateNullable = baptismDateString.AsDateTime();
                        if ( baptismDateNullable != null )
                        {
                            var baptismDate = baptismDateNullable ?? new DateTime(); //I have to turn a nullable datetime to non nullable date time to use .tostring
                            SetExcelValue( worksheet.Cells[rowCounter, 7], baptismDate.ToString( "MMMM d, yyyy" ) );
                        }
                    }

                    SetExcelValue( worksheet.Cells[rowCounter, 8], person.ConnectionStatusValue.Value );
                    rowCounter++;
                }
                // autofit columns for all cells
                worksheet.Cells.AutoFitColumns( 0 );

                for ( var i = 1; i < 9; i++ )
                {
                    worksheet.Column( i ).Width = 24;
                }
                worksheet.Column( 2 ).Width = 4;

            }
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