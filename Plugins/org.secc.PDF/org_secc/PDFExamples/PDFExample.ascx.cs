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
using org.secc.PDFMerge;
using Rock.Attribute;

namespace RockWeb.Plugins.org_secc.PDFExamples
{
    [DisplayName( "PDF Example" )]
    [Category( "CMS" )]
    [Description( "Example " )]

    //Settings
    [WorkflowTypeField( "Workflow Type", "The workflow type to activate to merge pdf" )]
    [TextField( "Workflow Activity", "The name of the workflow activity to run on selection.", false, "" )]
    public partial class PDFExample : RockBlock
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
            Dictionary<string, string> mergeFields = GetMergeFields();
            BinaryFile pdf = binaryFileService.Get( int.Parse( fpSelectedFile.SelectedValue ) );

            //Create the object we will need to pass to the workflow
            if ( pdf != null && mergeFields.Count > 0 )
            {
                var pdfEntity = new PDFFormMergeEntity();
                pdfEntity.PDF = pdf;
                pdfEntity.MergeFields = mergeFields;

                Guid workflowTypeGuid = Guid.NewGuid();
                if ( Guid.TryParse( GetAttributeValue( "WorkflowType" ), out workflowTypeGuid ) )
                {
                    var workflowRockContext = new RockContext();
                    var workflowTypeService = new WorkflowTypeService( workflowRockContext );
                    var workflowType = workflowTypeService.Get( workflowTypeGuid );
                    if ( workflowType != null )
                    {
                        var workflow = Workflow.Activate( workflowType, pdf.FileName ); 

                        List<string> workflowErrors;
                        var workflowService = new WorkflowService( workflowRockContext );
                        var workflowActivity = GetAttributeValue( "WorkflowActivity" );
                        var activityType = workflowType.ActivityTypes.Where( a => a.Name == workflowActivity ).FirstOrDefault();
                        if ( activityType != null )
                        {
                            WorkflowActivity.Activate( activityType, workflow, workflowRockContext );
                            if ( workflowService.Process( workflow, pdfEntity, out workflowErrors ) )
                            {
                                //success
                            }
                        }
                    }


                }

                

                var mergedPDF = pdfEntity.MergedPDF;
                //mergedPDF.Guid = Guid.NewGuid();
                binaryFileService.Add( mergedPDF );
                rockContext.SaveChanges();

                Response.Redirect( mergedPDF.Path );
            }
        }

        private Dictionary<string, string> GetMergeFields()
        {
            var rawMergeFields = kvlMerge.Value;
            var splitMergeFields = rawMergeFields.Split( '|' );
            Dictionary<string, string> mergeFields = new Dictionary<string, string>();
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