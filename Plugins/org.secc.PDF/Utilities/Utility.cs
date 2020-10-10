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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NReco.PdfGenerator;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.PDF
{
    public class Utility
    {
        public static BinaryFile HtmlToPdf( string html, string pageHeaderHtml = null, string pageFooterHtml = null, RockContext rockContext = null, string pdfFileName = "GeneratedPDF.pdf", PageMargins margins = null, PageOrientation orientation = PageOrientation.Default )
        {
            if (rockContext == null)
            {
                rockContext = new RockContext();
            }
            var htmlToPdf = new HtmlToPdfConverter();
            if (margins != null)
            {
                htmlToPdf.Margins = margins;
            }
            htmlToPdf.Orientation = orientation;

            // Add the Header
            if ( pageHeaderHtml != null && !string.IsNullOrWhiteSpace( pageHeaderHtml ) )
            {
                htmlToPdf.PageHeaderHtml = pageHeaderHtml;
            }
            // Add the Footer
            if ( pageFooterHtml != null && !string.IsNullOrWhiteSpace( pageFooterHtml ) )
            {
                htmlToPdf.PageFooterHtml = pageFooterHtml;
            }
            htmlToPdf.Size = PageSize.Letter;


            using ( MemoryStream msPDF = new MemoryStream( htmlToPdf.GeneratePdf( html ) ) )
            {
                BinaryFile pdfBinary = new BinaryFile();
                pdfBinary.Guid = Guid.NewGuid();
                pdfBinary.FileName = pdfFileName;
                pdfBinary.MimeType = "application/pdf";
                pdfBinary.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = msPDF.ToArray();

                pdfBinary.DatabaseData = pdfData;

                return pdfBinary;
            }
        }
        public static PDFWorkflowObject GetPDFFormMergeFromEntity( object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is PDFWorkflowObject )
            {
                return ( PDFWorkflowObject ) entity;
            }

            errorMessages.Add( "Could not get PDFFormMergeEntity object" );
            return null;
        }

        public static void EnsureAttributes( WorkflowAction action, RockContext rockContext )
        {

            if ( action.Activity.Workflow.Attributes == null || action.Activity.Workflow.AttributeValues.Count == 0 )
            {
                action.Activity.Workflow.LoadAttributes();
            }

            var workflowAttributes = action.Activity.Workflow.Attributes;

            if ( !workflowAttributes.ContainsKey( "PersonId" ) )
            {
                CreateAttribute( "PersonId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "RegistrationRegistrantId" ) )
            {
                CreateAttribute( "RegistrationRegistrantId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "GroupMemberId" ) )
            {
                CreateAttribute( "GroupMemberId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "PDFGuid" ) )
            {
                CreateAttribute( "PDFGuid", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "XHTML" ) )
            {
                CreateAttribute( "XHTML", action, rockContext );
            }
        }

        private static void CreateAttribute( string name, WorkflowAction action, RockContext rockContext )
        {
            Rock.Model.Attribute newAttribute = new Rock.Model.Attribute();
            newAttribute.Key = name;
            newAttribute.Name = name;
            newAttribute.FieldTypeId = FieldTypeCache.Get( new Guid( Rock.SystemGuid.FieldType.TEXT ) ).Id;
            newAttribute.Order = 0;
            newAttribute.AttributeQualifiers.Add( new AttributeQualifier() { Key = "ispassword", Value = "False" } );
            newAttribute.EntityTypeId = EntityTypeCache.Get( action.Activity.Workflow.GetType() ).Id;
            newAttribute.EntityTypeQualifierColumn = "WorkflowTypeId";
            newAttribute.EntityTypeQualifierValue = action.Activity.Workflow.WorkflowType.Id.ToString();
            AttributeService attributeService = new AttributeService( rockContext );
            attributeService.Add( newAttribute );
            rockContext.SaveChanges();
            AttributeCache.RemoveEntityAttributes();

            action.Activity.Workflow.LoadAttributes();
        }
    }
}
