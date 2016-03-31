// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using iTextSharp.text.pdf;
using System.IO;

namespace org.secc.PDFMerge
{
    /// <summary>
    /// Delays successful execution of action until a specified number of minutes have passed
    /// </summary>
    [ActionCategory( "PDF" )]
    [Description( "Merges" )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "PDF Form Merge" )]

    //Settings
    [BooleanField("Flatten", "Should the action flatten the PDF locking the form fields")]

    class PDFFormMerge : ActionComponent
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

            PDFFormMergeEntity pdfFormMergeEntity = GetPDFFormMergeFromEntity( entity, out errorMessages );

            var pdf = pdfFormMergeEntity.PDF;

            using (MemoryStream ms = new MemoryStream() )
            {
                var pdfBytes = pdf.ContentStream.ReadBytesToEnd();
                var pdfReader = new PdfReader( pdfBytes );

                var stamper = new PdfStamper( pdfReader, ms );

                var form = stamper.AcroFields;

                form.GenerateAppearances = true;

                var fieldKeys = form.Fields.Keys;

                foreach ( string fieldKey in fieldKeys )
                {
                    if ( pdfFormMergeEntity.MergeFields.ContainsKey( fieldKey ) )
                    {
                        form.SetField( fieldKey, pdfFormMergeEntity.MergeFields[fieldKey]);
                   }
                }

                //Should we flatten the form
                stamper.FormFlattening = GetActionAttributeValue( action, "Flatten" ).AsBoolean();

                stamper.Close();
                pdfReader.Close();

                BinaryFile mergedPDF = new BinaryFile();
                mergedPDF.CopyPropertiesFrom( pdf );
                mergedPDF.Guid = Guid.NewGuid();
                mergedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = ms.ToArray();

                mergedPDF.DatabaseData = pdfData;

                pdfFormMergeEntity.MergedPDF = mergedPDF;

                pdfReader.Close();
            }

            return true;
        }

        private PDFFormMergeEntity GetPDFFormMergeFromEntity( object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is PDFFormMergeEntity )
            {
                return ( PDFFormMergeEntity ) entity;
            }

            errorMessages.Add( "Could not get PDFFormMergeEntity object" );
            return null;
        }

        public static byte[] ReadFully( Stream input )
        {
            byte[] buffer = new byte[16 * 1024];
            using ( MemoryStream ms = new MemoryStream() )
            {
                int read;
                while ( ( read = input.Read( buffer, 0, buffer.Length ) ) > 0 )
                {
                    ms.Write( buffer, 0, read );
                }
                return ms.ToArray();
            }
        }
    }
}
