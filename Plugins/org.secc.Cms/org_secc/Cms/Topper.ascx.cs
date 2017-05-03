// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using Rock;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Attribute;
using Rock.Data;

namespace RockWeb.Plugins.org_secc.CMS
{
    [DisplayName( "Topper" )]
    [Category( "SECC > CMS" )]
    [Description( "Header Parallax Title" )]
    [TextField( "Background Image Id", "The parallax background image.", false )]
    [BooleanField("Info Panel Visible", "", false)]
    [TextField( "Title Text", "The title text to display over the parallax image (Defaults to the page title)." )]
    [CodeEditorField( "Info Panel Content", "The content to display on the right of the topper.", Rock.Web.UI.Controls.CodeEditorMode.Html, category: "Info Panel" )]
    [TextField( "Info Panel Background RGB Value", "The background color to use on the info panel", true, "rgba(255,255,255,0.8)", category: "Info Panel" )]
    public partial class Topper : Rock.Web.UI.RockBlockCustomSettings
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                imgupTitleBackground.BinaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.MEDIA_FILE.AsGuid();
            }

            var titleText = GetAttributeValue( "TitleText" );
            if ( string.IsNullOrWhiteSpace( titleText ) )
            {
                SetAttributeValue( "TitleText", Page.Title );
                SaveAttributeValues();
            }
            pnlInfoPanel.Visible = GetAttributeValue( "InfoPanelVisible" ).AsBoolean();

            ltHtmlContent.Text = GetAttributeValue( "InfoPanelContent" );
            ltTopperTitle.Text = GetAttributeValue( "TitleText" );
            
            nextInfo.Style.Add( "background", GetAttributeValue( "InfoPanelBackgroundRGBValue" ) );
        }

        public string getPageHeaderUrl()
        {
            return string.Format( "url('/GetFile.ashx?Id={0}')", GetAttributeValue( "BackgroundImageId" ) );
        }

        public string getH1Margin()
        {
            return GetAttributeValue( "InfoPanelVisible" ).AsBoolean() ? "" : "margin-left: 0%";
        }
        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            tbTopperTitle.Text = GetAttributeValue( "TitleText" );
            imgupTitleBackground.BinaryFileId = GetAttributeValue( "BackgroundImageId" ).AsInteger();
            bool isInfoPanelVisible = GetAttributeValue( "InfoPanelVisible" ).AsBoolean();
            cbInfoPanel.Checked = isInfoPanelVisible;
            htmlInfoPanelContent.Visible = isInfoPanelVisible;
            cpBackground.Visible = isInfoPanelVisible;
            htmlInfoPanelContent.Text = GetAttributeValue( "InfoPanelContent" );
            cpBackground.Text = GetAttributeValue( "InfoPanelBackgroundRGBValue" );

            mdEdit.Show();
        }
        #endregion

        
        protected void mdEdit_SaveClick( object sender, EventArgs e )
        {
            // Save the image
            // SetAttributeValue("BackgroundImage", imgupTitleBackground.BinaryFileId);
            SetAttributeValue( "TitleText", tbTopperTitle.Text );
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile backgroundImage = binaryFileService.Get( imgupTitleBackground.BinaryFileId??0 );
            if ( backgroundImage != null )
            {
                backgroundImage.IsTemporary = false;    
                rockContext.SaveChanges();
            }

            SetAttributeValue( "BackgroundImageId", imgupTitleBackground.BinaryFileId.ToString() );
            SetAttributeValue( "InfoPanelContent", htmlInfoPanelContent.Text );
            SetAttributeValue( "InfoPanelBackgroundRGBValue", cpBackground.Text );
            SetAttributeValue( "InfoPanelVisible", cbInfoPanel.Checked.ToString() );
            SaveAttributeValues();
            mdEdit.Hide();
        }

        protected void imgupTitleBackground_ImageUploaded( object sender, Rock.Web.UI.Controls.ImageUploaderEventArgs e )
        {
            DeletePreviousImage();
        }

        protected void imgupTitleBackground_ImageRemoved( object sender, Rock.Web.UI.Controls.ImageUploaderEventArgs e )
        {
            DeletePreviousImage();
        }

        private void DeletePreviousImage()
        {
            RockContext rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            var oldBackgroundImageID = GetAttributeValue( "BackgroundImageId" ).AsInteger();
            BinaryFile oldBackgroundImage = binaryFileService.Get( oldBackgroundImageID );
            if ( oldBackgroundImage != null )
            {
                binaryFileService.Delete( oldBackgroundImage );
                rockContext.SaveChanges();
            }
        }

        protected void cbInfoPanel_CheckedChanged( object sender, EventArgs e )
        {
            htmlInfoPanelContent.Visible = cbInfoPanel.Checked;
            cpBackground.Visible = cbInfoPanel.Checked;
        }
    }
}
