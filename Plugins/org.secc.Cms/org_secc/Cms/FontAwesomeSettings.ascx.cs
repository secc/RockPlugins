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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Hosting;
using System.Web.UI;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Utility;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Font Awesome Settings" )]
    [Category( "SECC > CMS" )]
    [Description( "Block that can be used to configure Font Awesome" )]
    public partial class FontAwesomeSettings : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                var fontAwesomeProKey = FontAwesomeHelper.GetFontAwesomeProKey();
                var hasFontAwesomeProKey = FontAwesomeHelper.HasFontAwesomeProKey();
                tbFontAwesomeProKey.Text = fontAwesomeProKey;
                pnlFontAwesomeFree.Visible = !hasFontAwesomeProKey;
                btnInstallUpdate.Text = hasFontAwesomeProKey ? "Update" : "Install";
                fupFontAwesomeProPackage.UploadAsTemporary = true;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnInstallUpdate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnInstallUpdate_Click( object sender, EventArgs e )
        {
            var fontAwesomeProKey = FontAwesomeHelper.GetFontAwesomeProKey();
            List<string> updateMessages = new List<string>();

            if ( fontAwesomeProKey != tbFontAwesomeProKey.Text )
            {
                FontAwesomeHelper.SetFontAwesomeProKey( tbFontAwesomeProKey.Text );
                if ( tbFontAwesomeProKey.Text.IsNullOrWhiteSpace() )
                {
                    updateMessages.Add( "Font Awesome Pro Key removed" );
                    pnlFontAwesomeFree.Visible = true;
                }
                else
                {
                    updateMessages.Add( "Font Awesome Pro Key updated" );
                    pnlFontAwesomeFree.Visible = false;
                }
            }

            if ( fupFontAwesomeProPackage.BinaryFileId.HasValue )
            {
                if ( ExtractFontAwesomePackage( fupFontAwesomeProPackage.BinaryFileId.Value ) )
                {
                    updateMessages.Add( "Font Awesome Pro Package updated. In order to use any new fonts you must manually re-compile the theme where they will be used" );
                    btnInstallUpdate.Text = "Update";
                }
            }

            if ( updateMessages.Any() )
            {
                nbInstallSuccess.Text = updateMessages.AsDelimited( ", ", " and " ) + ".";
                nbInstallSuccess.NotificationBoxType = NotificationBoxType.Success;
                nbInstallSuccess.Visible = true;
            }
            else
            {
                nbInstallSuccess.Text = "Please upload a Font Awesome Pro Package to update the Font Awesome fonts";
                nbInstallSuccess.NotificationBoxType = NotificationBoxType.Warning;
                nbInstallSuccess.Visible = true;
            }
        }

        public bool ExtractFontAwesomePackage( int binaryFileId )
        {
            var rockContext = new RockContext();
            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile fontawesomePackageBinaryFile = binaryFileService.Get( binaryFileId );

            if ( fontawesomePackageBinaryFile != null )
            {
                using ( ZipArchive fontawesomePackageZip = new ZipArchive( fontawesomePackageBinaryFile.ContentStream ) )
                {
                    string webFontsWithCssFolder = fontawesomePackageZip.Entries.Where( a => a.FullName.EndsWith( "/web-fonts-with-css/" ) ).Select( a => a.FullName ).FirstOrDefault();

                    var webFontsWithCssFiles = fontawesomePackageZip.Entries.Where( a => a.Name.IsNotNullOrWhiteSpace() ).ToList();
                    if ( !string.IsNullOrEmpty( webFontsWithCssFolder ) )
                    {
                        webFontsWithCssFiles = fontawesomePackageZip.Entries.Where( a => a.Name.IsNotNullOrWhiteSpace() && a.FullName.StartsWith( webFontsWithCssFolder ) ).ToList();
                    }


                    var lessFileEntries = webFontsWithCssFiles
                        .Where( a => new DirectoryInfo( Path.GetDirectoryName( a.FullName ) ).Name.Equals( "less" ) ).ToList();

                    var scssFileEntries = webFontsWithCssFiles
                        .Where( a => new DirectoryInfo( Path.GetDirectoryName( a.FullName ) ).Name.Equals( "scss" ) ).ToList();

                    var webFontsFileEntries = webFontsWithCssFiles
                        .Where( a => new DirectoryInfo( Path.GetDirectoryName( a.FullName ) ).Name.Equals( "webfonts" ) ).ToList();

                    var fontAwesomeFontsFolder = HostingEnvironment.MapPath( "~/Assets/Fonts/FontAwesome" );
                    var fontAwesomeStylesFolder = HostingEnvironment.MapPath( "~/Styles/FontAwesome" );

                    var fontAwesomeScssFolder = HostingEnvironment.MapPath( "~/Styles/FontAwesome/Scss" );
                    System.IO.Directory.CreateDirectory( fontAwesomeScssFolder );

                    foreach ( ZipArchiveEntry lessFileEntry in lessFileEntries )
                    {
                        var destLessFileName = Path.Combine( fontAwesomeStylesFolder, lessFileEntry.Name );
                        if ( File.Exists( destLessFileName ) )
                        {
                            File.Delete( destLessFileName );
                        }

                        lessFileEntry.ExtractToFile( destLessFileName );

                        // Fixup LineEndings
                        var updatedFileLines = File.ReadAllLines( destLessFileName );
                        File.WriteAllLines( destLessFileName, updatedFileLines );
                    }

                    foreach ( ZipArchiveEntry scssFileEntry in scssFileEntries )
                    {
                        var destLessFileName = Path.Combine( fontAwesomeScssFolder, scssFileEntry.Name );
                        if ( File.Exists( destLessFileName ) )
                        {
                            File.Delete( destLessFileName );
                        }

                        scssFileEntry.ExtractToFile( destLessFileName );

                        // Fixup LineEndings
                        var updatedFileLines = File.ReadAllLines( destLessFileName );
                        File.WriteAllLines( destLessFileName, updatedFileLines );
                    }

                    foreach ( var webFontsFileEntry in webFontsFileEntries )
                    {
                        var destFontFile = Path.Combine( fontAwesomeFontsFolder, webFontsFileEntry.Name );
                        if ( File.Exists( destFontFile ) )
                        {
                            File.Delete( destFontFile );
                        }

                        webFontsFileEntry.ExtractToFile( destFontFile );
                    }
                }

                binaryFileService.Delete( fontawesomePackageBinaryFile );
                return true;
            }

            return false;
        }

        #endregion
    }
}