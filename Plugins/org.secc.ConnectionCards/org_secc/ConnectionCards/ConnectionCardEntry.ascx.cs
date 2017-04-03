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
using Rock.Attribute;
using Rock.Web.Cache;
using org.secc.ConnectionCards.Utilities;
using Ghostscript.NET.Rasterizer;
using System.IO;
using System.Drawing.Imaging;

namespace RockWeb.Plugins.org_secc.ConnectionCards
{
    [DisplayName( "Connection Card Entry" )]
    [Category( "SECC > Connection Cards" )]
    [Description( "Entry form for adding connection cards in a sheet." )]

    //Settings
    [WorkflowTypeField( "Workflow Type", "The workflow type to activate to merge pdf" )]
    [BinaryFileTypeField( "Binary File Type", "The binary file type to set images as." )]
    public partial class ConnectionCardEntry : RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            nbSuccess.Visible = false;
            if ( string.IsNullOrWhiteSpace( hfImageGuid.Value ) )
            {
                pnlEdit.Visible = false;
                pnlUpload.Visible = true;
            }
            else
            {
                ShowImage();
            }
        }

        private void ShowImage()
        {
            pnlEdit.Visible = true;
            pnlUpload.Visible = false;
            var imageGuid = hfImageGuid.Value.AsGuid();
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( imageGuid );
            if ( binaryFile != null )
            {
                iPicture.ImageUrl = string.Format( "{0}&cache={1}", binaryFile.Url, Guid.NewGuid() );
                iPicture.Visible = true;
            }
        }

        protected void fMainSheet_FileUploaded( object sender, EventArgs e )
        {
            int? mainSheetId = fMainSheet.BinaryFileId;
            if ( mainSheetId != null || mainSheetId != 0 )
            {
                RockContext rockContext = new RockContext();
                BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                var mainSheet = binaryFileService.Get( mainSheetId ?? 0 );
                if ( mainSheet != null )
                {
                    BinaryFile binaryFile = ConnectionCardsUtilties.ConvertPDFToImage( mainSheet );
                    if ( binaryFile != null )
                    {
                        BinaryFileTypeService binaryFileTypeService = new BinaryFileTypeService( rockContext );
                        var binaryFileTypeGuid = GetAttributeValue( "BinaryFileType" ).AsGuid();
                        var binaryFileType = binaryFileTypeService.Get( binaryFileTypeGuid );
                        binaryFile.BinaryFileTypeId = binaryFileType.Id;
                        binaryFile.IsTemporary = true;
                        binaryFileService.Add( binaryFile );
                        binaryFileService.Delete( mainSheet );
                        rockContext.SaveChanges();
                        hfImageGuid.Value = binaryFile.Guid.ToString();
                        ShowImage();
                    }
                }
            }
        }

        protected void btnRotLeft_Click( object sender, EventArgs e )
        {
            var imageGuid = hfImageGuid.Value.AsGuid();
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( imageGuid );
            binaryFile = ConnectionCardsUtilties.RotateImage( binaryFile, System.Drawing.RotateFlipType.Rotate270FlipNone, rockContext );
            ShowImage();
        }

        protected void btnRotRight_Click( object sender, EventArgs e )
        {
            var imageGuid = hfImageGuid.Value.AsGuid();
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( imageGuid );
            binaryFile = ConnectionCardsUtilties.RotateImage( binaryFile, System.Drawing.RotateFlipType.Rotate90FlipNone, rockContext );
            ShowImage();
        }


        protected void btnCrop_Click( object sender, EventArgs e )
        {
            var imageGuid = hfImageGuid.Value.AsGuid();
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( imageGuid );
            var binaryFiles = ConnectionCardsUtilties.ChopImage( binaryFile, 3, 2, rockContext );
            binaryFileService.Delete( binaryFile );
            rockContext.SaveChanges();
            foreach (var connectionCard in binaryFiles )
            {
                StartWorkflow( connectionCard );
            }
            pnlEdit.Visible = false;
            pnlUpload.Visible = true;
            hfImageGuid.Value = "";
            fMainSheet.BinaryFileId = 0;
            nbSuccess.Visible = true;
        }

        protected void StartWorkflow( BinaryFile binaryFile)
        {
            var errorMessages = new List<string>();

            Guid? guid = GetAttributeValue( "WorkflowType" ).AsGuidOrNull();
            if ( guid.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var workflowTypeService = new WorkflowTypeService( rockContext );
                    var workflowService = new WorkflowService( rockContext );

                    var workflowType = workflowTypeService.Queryable( "ActivityTypes" )
                        .Where( w => w.Guid.Equals( guid.Value ) )
                        .FirstOrDefault();

                    if ( workflowType != null )
                    {
                        var workflow = Rock.Model.Workflow.Activate( workflowType, "New Connection Card Workflow", rockContext );


                        var activityTypes = workflowType.ActivityTypes.Where( a => a.IsActivatedWithWorkflow == true ).ToList();
                        foreach ( var activityType in activityTypes )
                        {

                            WorkflowActivity.Activate( activityType, workflow, rockContext );
                            if ( workflowService.Process( workflow, binaryFile, out errorMessages ) )
                            {
                                // Keep workflow active for continued processing
                                workflow.CompletedDateTime = null;
                            }
                        }
                    }
                }
            }
        }

        protected void btnBack_Click( object sender, EventArgs e )
        {
            var imageGuid = hfImageGuid.Value.AsGuid();
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = binaryFileService.Get( imageGuid );
            binaryFileService.Delete( binaryFile );
            rockContext.SaveChanges();

            pnlEdit.Visible = false;
            pnlUpload.Visible = true;
            hfImageGuid.Value = "";
            fMainSheet.BinaryFileId = 0;
        }
    }
}