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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using WkHtmlToXSharp;

namespace org.secc.PDF
{
    public class Utility
    {
        public static BinaryFile HtmlToPdf( string html, RockContext rockContext = null, string pdfFileName = "GeneratedPDF.pdf", PdfGlobalSettings settings = null)
        {
            if (rockContext == null)
            {
                rockContext = new RockContext();
            }

            WkHtmlToXLibrariesManager.Register( new Win64NativeBundle() );

            IHtmlToPdfConverter htmlToPdfConverter = new MultiplexingConverter();
            
            if ( settings != null )
            {
                htmlToPdfConverter.GlobalSettings.DocumentTitle = settings.DocumentTitle;
                htmlToPdfConverter.GlobalSettings.Dpi = settings.Dpi;
                htmlToPdfConverter.GlobalSettings.ImageDpi = settings.ImageDpi;
                htmlToPdfConverter.GlobalSettings.ImageQuality = settings.ImageQuality;
                htmlToPdfConverter.GlobalSettings.Margin.Bottom = settings.Margin.Bottom;
                htmlToPdfConverter.GlobalSettings.Margin.Left = settings.Margin.Left;
                htmlToPdfConverter.GlobalSettings.Margin.Right = settings.Margin.Right;
                htmlToPdfConverter.GlobalSettings.Margin.Top = settings.Margin.Top;
                htmlToPdfConverter.GlobalSettings.Orientation = settings.Orientation;
                htmlToPdfConverter.GlobalSettings.Out = settings.Out;
                htmlToPdfConverter.GlobalSettings.Outline = htmlToPdfConverter.GlobalSettings.Outline;
                htmlToPdfConverter.GlobalSettings.Size.Height = htmlToPdfConverter.GlobalSettings.Size.Height;
                htmlToPdfConverter.GlobalSettings.Size.PageSize = htmlToPdfConverter.GlobalSettings.Size.PageSize;
                htmlToPdfConverter.GlobalSettings.Size.Width = htmlToPdfConverter.GlobalSettings.Size.Width;
            }

            htmlToPdfConverter.ObjectSettings.Load.LoadErrorHandling = LoadErrorHandlingType.ignore;

            using ( MemoryStream msPDF = new MemoryStream( htmlToPdfConverter.Convert( html ) ) )
            {
                BinaryFile pdfBinary = new BinaryFile();
                pdfBinary.Guid = Guid.NewGuid();
                pdfBinary.FileName = pdfFileName;
                pdfBinary.MimeType = "application/pdf";
                pdfBinary.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = msPDF.ToArray();

                pdfBinary.DatabaseData = pdfData;

                htmlToPdfConverter.Dispose();

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
            newAttribute.FieldTypeId = FieldTypeCache.Read( new Guid( Rock.SystemGuid.FieldType.TEXT ) ).Id;
            newAttribute.Order = 0;
            newAttribute.AttributeQualifiers.Add( new AttributeQualifier() { Key = "ispassword", Value = "False" } );
            newAttribute.EntityTypeId = EntityTypeCache.Read( action.Activity.Workflow.GetType() ).Id;
            newAttribute.EntityTypeQualifierColumn = "WorkflowTypeId";
            newAttribute.EntityTypeQualifierValue = action.Activity.Workflow.WorkflowType.Id.ToString();
            AttributeService attributeService = new AttributeService( rockContext );
            attributeService.Add( newAttribute );
            rockContext.SaveChanges();
            AttributeCache.FlushEntityAttributes();

            action.Activity.Workflow.LoadAttributes();
        }
    }
}
