using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;


namespace org.secc.Reporting
{
    public class CampMedicationReport
    {
        private readonly CampMedicationReportFilter _filter;
        private Dictionary<int, CampusCache> _instanceCampuses = new Dictionary<int, CampusCache>();


        public CampMedicationReport( CampMedicationReportFilter filter )
        {
            _filter = filter;
        }

        public List<CampMedicationReportItem> GenerateMedicationReportData()
        {
            if (_filter.RegistrationTemplateId <= 0)
            {
                throw new ArgumentException( "Registration Template Id must be greater than 0." );
            }
            using (var context = new RockContext())
            {
                var campuses = CampusCache.All( context )
                    .Select( c => new { c.Id, c.Name } )
                    .ToList();

                var sqlParam = new List<SqlParameter>();
                sqlParam.Add( new SqlParameter( "@RegistrationTemplateId", _filter.RegistrationTemplateId ) );
                sqlParam.Add( new SqlParameter( "@RegistrationInstanceId", _filter.RegistrationInstanceId ) );


                var camperMeds = context.Database.SqlQuery<CampMedicationReportItem>( "dbo._org_secc_CampManager_GetMedicationReport @RegistrationTemplateId, @RegistrationInstanceId ",
                    sqlParam.ToArray() ).ToList();

                foreach (var c in camperMeds)
                {
                    var campus = GetCamperCampus( c );
                    if (campus != null)
                    {
                        c.CampusId = campus.Id;
                        c.CampusName = campus.Name;
                    }
                }

                var camperMedsQry = camperMeds.AsQueryable();

                if (_filter.CampusId.HasValue)
                {
                    camperMedsQry = camperMedsQry.Where( c => c.CampusId == _filter.CampusId.Value );
                }


                if (_filter.ScheduleGuid.HasValue)
                {
                    camperMedsQry = camperMedsQry.Where( c => c.DelimitedScheduleGuids.Contains( _filter.ScheduleGuid.Value ) );
                }


                return camperMedsQry.ToList();
            }


        }

        public int? GenerateExcelExport( int binaryFileTypeId, string filename, Rock.Model.Person currentPerson = null, string source = null )
        {
            var fileTypeCache = BinaryFileTypeCache.Get( binaryFileTypeId );

            if (fileTypeCache == default)
            {
                throw new Exception( "Binary File Type not found." );
            }

            var fileRegex = new Regex( @"(\.xls)|(\.xlsx)$", RegexOptions.IgnoreCase );
            if (!fileRegex.Match( filename ).Success)
            {
                filename = filename + ".xlsx";
            }

            var medList = GenerateMedicationReportData();

            if (medList == null || medList.Count == 0)
            {
                return null;
            }
            var title = $"{medList.First().RegistrationTemplateName} - Medication List";
            string worksheetName = "Medications";

            ExcelPackage excel = new ExcelPackage();
            excel.Workbook.Properties.Title = title;

            if (currentPerson != null)
            {
                excel.Workbook.Properties.Author = currentPerson.FullName;
            }
            else
            {
                excel.Workbook.Properties.Author = "Rock";
            }

            if (!source.IsNullOrWhiteSpace())
            {
                excel.Workbook.Properties.SetCustomPropertyValue( "Source", source );
            }

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

            using (ExcelRange r = worksheet.Cells[1, 6, rowNumber, 7])
            {
                r.Style.WrapText = true;
            }

            worksheet.Cells.AutoFitColumns( 0 );
            worksheet.Column( 6 ).Width = 55;
            worksheet.Column( 7 ).Width = 55;

            using (ExcelRange r = worksheet.Cells[2, 8, rowNumber, 12])
            {
                r.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }


            var medXLS = new BinaryFile();
            using (MemoryStream ms = new MemoryStream())
            {
                var rockContext = new RockContext();
                


                excel.SaveAs( ms );
                medXLS.MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                medXLS.FileName = filename;
                medXLS.IsTemporary = false;
                medXLS.Guid = Guid.NewGuid();
                medXLS.BinaryFileTypeId = binaryFileTypeId;
                medXLS.ContentStream = new MemoryStream( ms.ToArray() );


                var binaryFileService = new BinaryFileService( rockContext );
                binaryFileService.Add( medXLS );
                rockContext.SaveChanges();
            }
            return medXLS.Id;

        }

        private CampusCache GetCamperCampus( CampMedicationReportItem c )
        {
            if (!_instanceCampuses.ContainsKey( c.RegistrationInstanceId ))
            {
                var instanceCampus = CampusCache.All()
                    .Where( c1 => c.RegistrationInstanceName.EndsWith( c1.Name, StringComparison.InvariantCultureIgnoreCase ) )
                    .FirstOrDefault();

                if (instanceCampus != null)
                {
                    _instanceCampuses.Add( c.RegistrationInstanceId, instanceCampus );
                }
                else
                {
                    return null;
                }

            }
            return _instanceCampuses[c.RegistrationInstanceId];

        }

    }

    [ActionCategory("SECC > Events")]
    [Description("Produces an Excel File of Event Participants who have medications.")]
    [Export(typeof(Rock.Workflow.ActionComponent))]
    [ExportMetadata("ComponentName", "Event Medication Export")]

    [WorkflowAttribute("Excel File Export", "Workflow attribute for Excel File.", true, Key ="ExcelFile", FieldTypeClassNames = new string[] { "Rock.Field.Types.FileFieldType" } )]
    [WorkflowAttribute("Registration Template", "The Registratiokn Template to pull meds for", true, Key="RegistrationTemplate", FieldTypeClassNames = new string[] { "Rock.Field.Types.RegistrationTemplateFieldType" } )]

    public class CampMedicationReportExportAction : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            var binaryFileType = BinaryFileTypeCache.Get( Guid.Parse( "5C1005F6-710D-43E4-B70B-BAA46682430D" ) );
            var templateAttributeGuid = GetAttributeValue( action, "RegistrationTemplate" ).AsGuid();
            AttributeCache attribute = AttributeCache.Get( templateAttributeGuid, rockContext );
            if(attribute == null)
            {
                errorMessages.Add( "Registration Template not set" );
                return false;
            }
            var templateGuid = action.GetWorkflowAttributeValue( attribute.Guid ).AsGuid();

            var registrationTemplateId = new RegistrationTemplateService( rockContext ).Get( templateGuid ).Id;

            CampMedicationReportFilter filter = new CampMedicationReportFilter();
            filter.RegistrationTemplateId = registrationTemplateId;

            var medReport = new CampMedicationReport( filter );
            var timestamp = string.Format( "{0:yyyyMMddHHmm}", RockDateTime.Now );
            var fileId = medReport.GenerateExcelExport( binaryFileType.Id, $"MedicationReport{timestamp}.xlsx" );

            var binaryFile = new BinaryFileService( rockContext ).Get( fileId.Value );

            var fileAttributeGuid = GetAttributeValue( action, "ExcelFile" ).AsGuid();
            var fileAttribute = AttributeCache.Get( fileAttributeGuid, rockContext );
            if(attribute != null)
            {
                SetWorkflowAttributeValue( action, fileAttributeGuid, binaryFile.Guid.ToString() );
            }

            return true;

        }
    }



}