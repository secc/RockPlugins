using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace org.secc.PDF.Workflows
{
    [ActionCategory("PDF")]
    [Description("Combines two PDF files.")]
    [Export(typeof(Rock.Workflow.ActionComponent))]
    [ExportMetadata("ComponentName", "PDF Combine")]
    [WorkflowAttribute("First PDF File", "Workflow Attribute that contains the first PDF that you want to combine.", true, FieldTypeClassNames = new string[] { "Rock.Field.Types.FileFieldType" }, Key ="PDFFirstFile")]
    [WorkflowAttribute("Second PDF File", "Workflow Attribute that contains the first PDF that you want to combine.", true, FieldTypeClassNames = new string[] { "Rock.Field.Types.FileFieldType" }, Key = "PDFSecondFile")]
    [WorkflowAttribute("Output PDF File", "Workflow Attribute that contains the first PDF that you want to combine.", true, FieldTypeClassNames = new string[] { "Rock.Field.Types.FileFieldType" }, Key = "PDFOutputFile")]
    public class PDFCombine : ActionComponent
    {
        
        public override bool Execute(RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages)
        {
            errorMessages = new List<string>();
            //get pdf1 
            var pdf1FileAttribute = AttributeCache.Get(GetAttributeValue(action, "PDFFirstFile").AsGuid(), rockContext);
            
            if(pdf1FileAttribute == null)
            {
                errorMessages.Add("First PDF File selection is required.");
                return false;
            }
            var pdf1FileGuid = action.GetWorkflowAttributeValue(pdf1FileAttribute.Guid);


            //get pdf2
            var pdf2FileAttribute = AttributeCache.Get(GetAttributeValue(action, "PDFSecondFile").AsGuid(), rockContext);
            
            if(pdf2FileAttribute == null)
            {
                errorMessages.Add("Second PDF File selection is required.");
                return false;
            }

            var pdf2FileGuid = action.GetWorkflowAttributeValue(pdf2FileAttribute.Guid);


            var binaryFileService = new BinaryFileService(rockContext);

            var pdf1 = binaryFileService.Get(pdf1FileGuid);
            var pdf2 = binaryFileService.Get(pdf2FileGuid);

            if(pdf1 == null)
            {
                errorMessages.Add("PDF 1 Not Found");
            }    
            if(pdf2 == null)
            {
                errorMessages.Add("PDF 2 Not Found");
            }

            if(errorMessages.Any())
            {
                return false;
            }

            if(!pdf1.MimeType.Equals("application/pdf") || !pdf2.MimeType.Equals("application/pdf"))
            {
                errorMessages.Add("All files must be in PDF Format.");
                return false;
            }

            var outputStream = new MemoryStream();
            var pdfWriter = new PdfWriter(outputStream);
            var pdfDocument = new PdfDocument(pdfWriter);

            //pdfDocument.AddNewPage();
            //Regex regex = new Regex(@"/Type\s*/Page[^s]");


            PdfMerger merger = new PdfMerger(pdfDocument);

            PdfDocument pdfDoc1 = new PdfDocument(new PdfReader(pdf1.ContentStream));
            PdfDocument pdfDoc2 = new PdfDocument(new PdfReader(pdf2.ContentStream));

            merger.Merge(pdfDoc1, 1, pdfDoc1.GetNumberOfPages());
            merger.Merge(pdfDoc2, 1, pdfDoc2.GetNumberOfPages());

            merger.Close();

            //pdfDocument.Close();
            pdfDoc1.Close();
            pdfDoc2.Close();

            pdfDocument.Close();
            pdfWriter.Close();
            var bytes = outputStream.ToArray();

            var pdfStream = new MemoryStream(bytes);

            BinaryFile outputFile = new BinaryFile();
            outputFile.MimeType = "application/pdf";
            outputFile.BinaryFileTypeId = pdf1.BinaryFileTypeId;
            outputFile.FileName = pdf1.FileName;
            outputFile.Guid = Guid.NewGuid();
            outputFile.ContentStream = pdfStream;

            binaryFileService.Add(outputFile);
            rockContext.SaveChanges();


            var outputAttribute = AttributeCache.Get(GetAttributeValue(action, "PDFOutputFile").AsGuid(), rockContext);
            SetWorkflowAttributeValue(action, outputAttribute.Guid, outputFile.Guid.ToString());
            //action.SaveAttributeValue(outputAttribute.Key, rockContext);
            rockContext.SaveChanges();


            return true;
        }
    }
}
