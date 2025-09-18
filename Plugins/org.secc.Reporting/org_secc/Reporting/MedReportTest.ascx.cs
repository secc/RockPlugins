using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using OfficeOpenXml;
using org.secc.Reporting;
using Rock;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Reporting
{
    [DisplayName( "Med Test" )]
    [Category( "SECC > Report" )]
    [Description( "Test Application" )]
    public partial class MedReportTest : RockBlock
    {

        #region Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gMedications.GridRebind += gMedications_GridRebind;
            gMedications.Actions.ShowAdd = false;
            gMedications.Actions.ShowBulkUpdate = false;
            gMedications.Actions.ShowCommunicate = false;
            gMedications.Actions.ShowMergePerson = false;
            gMedications.Actions.ShowMergeTemplate = false;
            gMedications.ShowWorkflowOrCustomActionButtons = false;
            gMedications.ShowHeaderWhenEmpty = false;
            gMedications.EnableStickyHeaders = true;
            gMedications.Actions.ShowExcelExport = false;

            if(this.ContextEntity() == null)
            {
                LinkButton excel = new LinkButton()
                {
                    ID = "btnExcel",
                    Text = "<i class='fas fa-table'></i>",
                    CssClass = "btn btn-default btn-sm"
                };

                gMedications.Actions.Controls.Add( excel );
                excel.Click += Actions_ExcelExportClick;
                ScriptManager.GetCurrent( this.Page ).RegisterPostBackControl( excel );


            }
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            BindMedGrid();
        }
        #endregion


        #region Events
        private void gMedications_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindMedGrid();
        }

        private void Actions_ExcelExportClick( object sender, EventArgs e )
        {
            GenerateExcel();
        }

        #endregion

        #region Private Methods
        private void BindMedGrid()
        {

            gMedications.DataSource = GetMedicationData();
            gMedications.DataBind();
        }

        private List<CampMedicationReportItem> GetMedicationData()
        {
            var filter = new CampMedicationReportFilter();
            filter.RegistrationTemplateId = 1371;
            //filter.RegistrationInstanceId = 5251;

           return new CampMedicationReport( filter ).GenerateMedicationReportData();
        }


        private void GenerateExcel()
        {
            var medList = GetMedicationData();

            if(medList == null || medList.Count == 0 )
            {
                return;
            }
            var title = $"{medList.First().RegistrationTemplateName} - Medication List";
            string fileName = gMedications.ExportFilename;
            string worksheetName = "Medications";

            ExcelPackage excel = new ExcelPackage();
            excel.Workbook.Properties.Title = title;

            if(CurrentPerson != null)
            {
                excel.Workbook.Properties.Author = CurrentPerson.FullName;
            }
            else
            {
                excel.Workbook.Properties.Author = "Rock";
            }

            excel.Workbook.Properties.SetCustomPropertyValue( "Source", this.Page.Request.Url.OriginalString );
            excel.Workbook.Properties.Created = RockDateTime.Now;

            ExcelWorksheet worksheet = excel.Workbook.Worksheets.Add( worksheetName );

            var rowNumber = 1;

            using (ExcelRange r = worksheet.Cells[rowNumber, 1, rowNumber, 12])
            {
                r.Style.Font.Bold = true;
                r.Style.Font.Color.SetColor( System.Drawing.Color.Black );
                r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                r.Style.Fill.BackgroundColor.SetColor( System.Drawing.Color.LightGray );
            }

            worksheet.Cells[rowNumber, 1].Value = "Name";
            worksheet.Cells[rowNumber, 2].Value = "Birthdate";
            worksheet.Cells[rowNumber, 3].Value = "Campus";
            worksheet.Cells[rowNumber, 4].Value = "Group";
            worksheet.Cells[rowNumber, 5].Value = "Leader";
            worksheet.Cells[rowNumber, 6].Value = "Medicaton";
            worksheet.Cells[rowNumber, 7].Value = "Instructions";
            worksheet.Cells[rowNumber, 8].Value = "Breakfast";
            worksheet.Cells[rowNumber, 9].Value = "Lunch";
            worksheet.Cells[rowNumber, 10].Value = "Dinner";
            worksheet.Cells[rowNumber, 11].Value = "Bedtime";
            worksheet.Cells[rowNumber, 12].Value = "As Needed";

            rowNumber++;

            foreach (var med in medList)
            {
                worksheet.Cells[rowNumber, 1].Value = med.LastName + ", " + med.NickName;
                worksheet.Cells[rowNumber, 2].Value = med.BirthDate.HasValue ? med.BirthDate.ToShortDateString() : "";
                worksheet.Cells[rowNumber, 3].Value = med.CampusName;
                worksheet.Cells[rowNumber, 4].Value = med.GroupName;
                worksheet.Cells[rowNumber, 5].Value = med.LeaderName;
                worksheet.Cells[rowNumber, 6].Value = med.Medication;
                worksheet.Cells[rowNumber, 7].Value = med.Instructions;
                worksheet.Cells[rowNumber, 8].Value = med.Breakfast ? "X" : "";
                worksheet.Cells[rowNumber, 9].Value = med.Lunch ? "X" : "";
                worksheet.Cells[rowNumber, 10].Value = med.Dinner ? "X" : "";
                worksheet.Cells[rowNumber, 11].Value = med.Bedtime ? "X" : "";
                worksheet.Cells[rowNumber, 12].Value = med.AsNeeded ? "X" : "";

                rowNumber++;
            }

            using(ExcelRange r = worksheet.Cells[1, 6, rowNumber, 7])
            {
                r.Style.WrapText = true;
            }

            worksheet.Cells.AutoFitColumns( 0 );
            worksheet.Column( 6 ).Width = 55;
            worksheet.Column( 7 ).Width = 55;

            using(ExcelRange r = worksheet.Cells[2,8,rowNumber,12])
            {
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            byte[] byteArray;
            using (MemoryStream ms = new MemoryStream())
            {
                excel.SaveAs( ms );
                byteArray = ms.ToArray();
            }
            // send the spreadsheet to the browser
            this.Page.EnableViewState = false;
            this.Page.Response.Clear();
            this.Page.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            this.Page.Response.AppendHeader( "Content-Disposition", "attachment; filename=" + fileName );

            this.Page.Response.Charset = string.Empty;
            this.Page.Response.BinaryWrite( byteArray );
            this.Page.Response.Flush();
            this.Page.Response.End();


        }

        #endregion
    }
}