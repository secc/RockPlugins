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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using iTextSharp.text.pdf;
using System.IO;
using System.ComponentModel.Composition;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.tool.xml;
using Rock.Attribute;
using WkHtmlToXSharp;
using Rock.Web.Cache;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Creates pdf from lava" )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Lava PDF" )]


    //Settings
    [CodeEditorField("Lava", "The lava to convert to a PDF", Rock.Web.UI.Controls.CodeEditorMode.Lava)]
    [WorkflowAttribute( "PDF", "Binary File attribute to output PDF to.", fieldTypeClassNames: new string[] { "Rock.Field.Types.FileFieldType" } )]
    class LavaPDF : ActionComponent
    {
        
        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            string Lava = GetActionAttributeValue( action, "Lava" );
            string html = Lava.ResolveMergeFields( GetMergeFields( action ) );

            BinaryFile pdfBinary = Utility.HtmlToPdf( html, rockContext );


            Guid guid = GetAttributeValue( action, "PDF" ).AsGuid();
            if ( !guid.IsEmpty() )
            {
                var destinationAttribute = AttributeCache.Read( guid, rockContext );
                if ( destinationAttribute != null )
                {

                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                    binaryFileService.Add( pdfBinary );
                    rockContext.SaveChanges();

                    // Update the file type if necessary
                    Guid binaryFileTypeGuid = Guid.Empty;
                    var binaryFileTypeQualifier = destinationAttribute.QualifierValues["binaryFileType"];
                    if ( binaryFileTypeQualifier != null )
                    {
                        if ( binaryFileTypeQualifier.Value != null )
                        {
                            binaryFileTypeGuid = binaryFileTypeQualifier.Value.AsGuid();

                            pdfBinary.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( binaryFileTypeGuid ).Id;
                        }
                    }

                    // Now store the attribute
                    if ( destinationAttribute.EntityTypeId == new Workflow().TypeId )
                    {
                        action.Activity.Workflow.SetAttributeValue( destinationAttribute.Key, pdfBinary.Guid.ToString() );
                    }
                    else if ( destinationAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                    {
                        action.Activity.SetAttributeValue( destinationAttribute.Key, pdfBinary.Guid.ToString() );
                    }
                }
            }
            return true;
        }

    }
}
