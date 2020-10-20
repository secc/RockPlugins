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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;

using Rock;
using Rock.Web.UI;
using System.Web.UI.HtmlControls;
using Rock.Web.UI.Controls;
using Rock.Data;
using Rock.Model;
using org.secc.PDF;
using Rock.Attribute;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.PDFExamples
{
    [DisplayName( "PDF Form Example" )]
    [Category( "CMS" )]
    [Description( "Example " )]

    //Settings
    [WorkflowTypeField( "Workflow Type", "The workflow type to activate to merge pdf" )]
    [TextField( "Workflow Activity", "The name of the workflow activity to run on selection.", false, "" )]
    public partial class PDFFormExample : RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                fpSelectedFile.BinaryFileTypeGuid = new Guid(Rock.SystemGuid.BinaryFiletype.MERGE_TEMPLATE);
            }
        }

        protected void fsPDF_FileUploaded( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var binaryFileService = new BinaryFileService( rockContext );

            //Get the binary file from the control
            var binaryFile = binaryFileService.Get( fsFile.BinaryFileId.Value );

            //Set binary file type as merge template
            binaryFile.BinaryFileType = new BinaryFileTypeService(rockContext)
                .Get( new Guid( Rock.SystemGuid.BinaryFiletype.MERGE_TEMPLATE ) );
            binaryFile.BinaryFileTypeId = binaryFile.BinaryFileType.Id;

            //set our file to not temporary
            binaryFile.IsTemporary = false;

            //Save our baby!
            rockContext.SaveChanges();

            ReloadSelectMergeDocument();
        }

        protected void bntDelete_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile binaryFile = binaryFileService.Get( int.Parse( fpSelectedFile.SelectedValue ) );
            binaryFileService.Delete( binaryFile );
            rockContext.SaveChanges();

            ReloadSelectMergeDocument();
        }
        protected void bntMerge_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            //Get the mergefields and pdf
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            Dictionary<string, object> mergeFields = GetMergeFields();
            BinaryFile pdf = binaryFileService.Get( int.Parse( fpSelectedFile.SelectedValue ) );

            //Create the object we will need to pass to the workflow
            if ( pdf != null && mergeFields.Count > 0 )
            {
                var pdfEntity = new PDFWorkflowObject();
                pdfEntity.PDF = pdf;
                pdfEntity.MergeObjects = mergeFields;

                Guid workflowTypeGuid = Guid.NewGuid();
                if ( Guid.TryParse( GetAttributeValue( "WorkflowType" ), out workflowTypeGuid ) )
                {
                    var workflowRockContext = new RockContext();
                    var workflowTypeService = new WorkflowTypeService( workflowRockContext );
                    var workflowType = workflowTypeService.Get( workflowTypeGuid );
                    if ( workflowType != null )
                    {
                        var workflow = Workflow.Activate( WorkflowTypeCache.Get(workflowType.Id), pdf.FileName ); 

                        List<string> workflowErrors;
                        var workflowService = new WorkflowService( workflowRockContext );
                        var workflowActivity = GetAttributeValue( "WorkflowActivity" );
                        var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
                        if ( activityType != null )
                        {
                            WorkflowActivity.Activate( WorkflowActivityTypeCache.Get( activityType.Id ), workflow, workflowRockContext );
                            if ( workflowService.Process( workflow, pdfEntity, out workflowErrors ) )
                            {
                                //success
                            }
                        }
                    }
                }

                var mergedPDF = pdfEntity.PDF;
                //mergedPDF.Guid = Guid.NewGuid();
                binaryFileService.Add( mergedPDF );
                rockContext.SaveChanges();

                Response.Redirect( mergedPDF.Path );
            }
        }

        private Dictionary<string, object> GetMergeFields()
        {
            var rawMergeFields = kvlMerge.Value;
            var splitMergeFields = rawMergeFields.Split( '|' );
            Dictionary<string, object> mergeFields = new Dictionary<string, object>();
            foreach ( string split in splitMergeFields )
            {
                if ( !string.IsNullOrWhiteSpace( split ) )
                {
                    var keyValue = split.Split( '^' );
                    mergeFields.Add( keyValue[0], keyValue[1] );
                }
            }
            return mergeFields;
        }

        private void ReloadSelectMergeDocument()
        {
            //Reload the page to lazy reset fpSelectedFile
            Response.Redirect( Request.Url.ToString() );
        }


    }
}