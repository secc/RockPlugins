﻿// <copyright>
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
using System.Web.UI;
using org.secc.ConnectionCards.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.ConnectionCards
{
    [DisplayName( "Connection Card Entry" )]
    [Category( "SECC > Connection Cards" )]
    [Description( "Entry form for adding connection cards in a sheet." )]

    //Settings
    [WorkflowTypeField( "Workflow Type", "The workflow type to activate to merge pdf" )]
    [BinaryFileTypeField( "Binary File Type", "The binary file type to set images as." )]
    [IntegerField( "Number of Columns", "The number of different columns to divide the document into.", key: "cols" )]
    [IntegerField( "Number of Rows", "The number of different columns to divide the document into.", key: "rows" )]
    public partial class ConnectionCardEntry : RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                nbCols.Value = GetAttributeValue( "cols" ).AsInteger();
                nbRows.Value = GetAttributeValue( "rows" ).AsInteger();
            }

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
            int cols = nbCols.Value;
            int rows = nbRows.Value;
            var binaryFiles = ConnectionCardsUtilties.ChopImage( binaryFile, cols, rows, rockContext );
            binaryFileService.Delete( binaryFile );
            rockContext.SaveChanges();
            foreach ( var connectionCard in binaryFiles )
            {
                connectionCard.LaunchWorkflow( GetAttributeValue( "WorkflowType" ).AsGuidOrNull(), "New Connection Card Workflow", new Dictionary<string, string> { { "Initiator", CurrentPersonAlias.Guid.ToString() } } );
            }
            pnlEdit.Visible = false;
            pnlUpload.Visible = true;
            hfImageGuid.Value = "";
            fMainSheet.BinaryFileId = 0;
            nbSuccess.Visible = true;
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