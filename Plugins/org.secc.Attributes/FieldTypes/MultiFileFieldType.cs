// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Attributes.Controls;
using Rock;
using Rock.Data;
using Rock.Field;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Attributes.FieldTypes
{
    /// <summary>
    /// Field type that accepts multiple file uploads against a single attribute.
    /// Files are stored as BinaryFiles; the attribute value is an AttributeMatrix Guid
    /// whose rows each reference one BinaryFile.
    /// </summary>
    [Serializable]
    public class MultiFileFieldType : Rock.Field.FieldType
    {
        #region Constants

        // Well-known Guid for the system-managed AttributeMatrixTemplate that backs every
        // MultiFile attribute. Auto-provisioned on first use. Do not change.
        // Generated via PowerShell [guid]::NewGuid() (CSPRNG-backed); not LLM-invented.
        public const string SHARED_TEMPLATE_GUID = "D95683B6-3587-43A6-8317-F48A79340175";
        public const string TEMPLATE_FILE_ATTRIBUTE_KEY = "File";

        private const string CONFIG_BINARY_FILE_TYPE = "binaryfiletype";
        private const string CONFIG_MAX_FILES = "maxfiles";
        private const string CONFIG_ALLOWED_EXTENSIONS = "allowedextensions";

        private static int? _sharedTemplateIdCache;

        #endregion

        #region Configuration

        public override List<string> ConfigurationKeys()
        {
            var keys = base.ConfigurationKeys();
            keys.Add( CONFIG_BINARY_FILE_TYPE );
            keys.Add( CONFIG_MAX_FILES );
            keys.Add( CONFIG_ALLOWED_EXTENSIONS );
            return keys;
        }

        public override List<Control> ConfigurationControls()
        {
            var controls = new List<Control>();

            var ddlBinaryFileType = new RockDropDownList
            {
                Label = "Binary File Type",
                Help = "The binary file type that uploads will be stored under.",
                AutoPostBack = true,
                Required = true
            };
            using ( var rockContext = new RockContext() )
            {
                foreach ( var bft in new BinaryFileTypeService( rockContext ).Queryable().OrderBy( b => b.Name ) )
                {
                    ddlBinaryFileType.Items.Add( new System.Web.UI.WebControls.ListItem( bft.Name, bft.Guid.ToString() ) );
                }
            }
            ddlBinaryFileType.SelectedIndexChanged += OnQualifierUpdated;
            controls.Add( ddlBinaryFileType );

            var nbMaxFiles = new NumberBox
            {
                Label = "Max Files",
                Help = "Maximum number of files allowed. Set to 0 for unlimited.",
                NumberType = ValidationDataType.Integer,
                MinimumValue = "0",
                AutoPostBack = true
            };
            nbMaxFiles.TextChanged += OnQualifierUpdated;
            controls.Add( nbMaxFiles );

            var tbAllowedExtensions = new RockTextBox
            {
                Label = "Allowed Extensions",
                Help = "Comma-separated list of allowed file extensions (e.g. \"pdf,docx,jpg\"). Leave blank to allow any.",
                AutoPostBack = true
            };
            tbAllowedExtensions.TextChanged += OnQualifierUpdated;
            controls.Add( tbAllowedExtensions );

            return controls;
        }

        public override Dictionary<string, ConfigurationValue> ConfigurationValues( List<Control> controls )
        {
            var values = new Dictionary<string, ConfigurationValue>
            {
                { CONFIG_BINARY_FILE_TYPE, new ConfigurationValue( "Binary File Type", "The binary file type that uploads will be stored under.", string.Empty ) },
                { CONFIG_MAX_FILES, new ConfigurationValue( "Max Files", "Maximum number of files allowed. 0 = unlimited.", "0" ) },
                { CONFIG_ALLOWED_EXTENSIONS, new ConfigurationValue( "Allowed Extensions", "Comma-separated allowed extensions.", string.Empty ) }
            };

            if ( controls != null )
            {
                if ( controls.Count > 0 && controls[0] is RockDropDownList ddl )
                {
                    values[CONFIG_BINARY_FILE_TYPE].Value = ddl.SelectedValue;
                }
                if ( controls.Count > 1 && controls[1] is NumberBox nb )
                {
                    values[CONFIG_MAX_FILES].Value = nb.Text;
                }
                if ( controls.Count > 2 && controls[2] is RockTextBox tb )
                {
                    values[CONFIG_ALLOWED_EXTENSIONS].Value = tb.Text;
                }
            }

            return values;
        }

        public override void SetConfigurationValues( List<Control> controls, Dictionary<string, ConfigurationValue> configurationValues )
        {
            if ( controls == null || configurationValues == null )
            {
                return;
            }

            if ( controls.Count > 0 && controls[0] is RockDropDownList ddl && configurationValues.ContainsKey( CONFIG_BINARY_FILE_TYPE ) )
            {
                ddl.SetValue( configurationValues[CONFIG_BINARY_FILE_TYPE].Value );
            }
            if ( controls.Count > 1 && controls[1] is NumberBox nb && configurationValues.ContainsKey( CONFIG_MAX_FILES ) )
            {
                nb.Text = configurationValues[CONFIG_MAX_FILES].Value;
            }
            if ( controls.Count > 2 && controls[2] is RockTextBox tb && configurationValues.ContainsKey( CONFIG_ALLOWED_EXTENSIONS ) )
            {
                tb.Text = configurationValues[CONFIG_ALLOWED_EXTENSIONS].Value;
            }
        }

        #endregion

        #region Edit Control

        public override Control EditControl( Dictionary<string, ConfigurationValue> configurationValues, string id )
        {
            Guid? binaryFileTypeGuid = null;
            int maxFiles = 0;
            string allowedExtensions = string.Empty;

            if ( configurationValues != null )
            {
                if ( configurationValues.ContainsKey( CONFIG_BINARY_FILE_TYPE ) )
                {
                    binaryFileTypeGuid = configurationValues[CONFIG_BINARY_FILE_TYPE].Value.AsGuidOrNull();
                }
                if ( configurationValues.ContainsKey( CONFIG_MAX_FILES ) )
                {
                    maxFiles = configurationValues[CONFIG_MAX_FILES].Value.AsIntegerOrNull() ?? 0;
                }
                if ( configurationValues.ContainsKey( CONFIG_ALLOWED_EXTENSIONS ) )
                {
                    allowedExtensions = configurationValues[CONFIG_ALLOWED_EXTENSIONS].Value ?? string.Empty;
                }
            }

            return new MultiFileUpload
            {
                ID = id,
                BinaryFileTypeGuid = binaryFileTypeGuid,
                MaxFiles = maxFiles,
                AllowedExtensions = allowedExtensions
            };
        }

        public override string GetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues )
        {
            var ctrl = control as MultiFileUpload;
            if ( ctrl == null )
            {
                return string.Empty;
            }

            var pendingFileIds = ctrl.GetCurrentFileIds();

            using ( var rockContext = new RockContext() )
            {
                // Resolve BinaryFile Ids to Guids
                var binaryFileService = new BinaryFileService( rockContext );
                var idToGuid = pendingFileIds
                    .Select( i => binaryFileService.Get( i ) )
                    .Where( bf => bf != null )
                    .ToDictionary( bf => bf.Id, bf => bf.Guid );

                var pendingFileGuids = idToGuid.Values.ToList();

                var matrixService = new AttributeMatrixService( rockContext );
                AttributeMatrix matrix = null;

                var existingMatrixGuid = ctrl.Value.AsGuidOrNull();
                if ( existingMatrixGuid.HasValue )
                {
                    matrix = matrixService.Get( existingMatrixGuid.Value );
                }

                // No existing matrix and no files to store - just return empty
                if ( matrix == null && !pendingFileGuids.Any() )
                {
                    return string.Empty;
                }

                if ( matrix == null )
                {
                    matrix = new AttributeMatrix
                    {
                        AttributeMatrixTemplateId = EnsureSharedTemplate( rockContext ),
                        AttributeMatrixItems = new List<AttributeMatrixItem>()
                    };
                    matrixService.Add( matrix );
                    rockContext.SaveChanges();
                }

                // Reconcile rows: index existing items by their File attribute Guid
                var itemService = new AttributeMatrixItemService( rockContext );
                var existingItems = matrix.AttributeMatrixItems.ToList();
                var existingByFileGuid = new Dictionary<Guid, AttributeMatrixItem>();
                foreach ( var item in existingItems )
                {
                    item.LoadAttributes();
                    var fileGuid = item.GetAttributeValue( TEMPLATE_FILE_ATTRIBUTE_KEY ).AsGuidOrNull();
                    if ( fileGuid.HasValue )
                    {
                        existingByFileGuid[fileGuid.Value] = item;
                    }
                }

                // Remove rows whose file is no longer in the pending list
                foreach ( var kv in existingByFileGuid.Where( x => !pendingFileGuids.Contains( x.Key ) ).ToList() )
                {
                    matrix.AttributeMatrixItems.Remove( kv.Value );
                    itemService.Delete( kv.Value );
                }

                // Add rows for newly uploaded files
                foreach ( var newGuid in pendingFileGuids.Where( g => !existingByFileGuid.ContainsKey( g ) ) )
                {
                    var newItem = new AttributeMatrixItem { AttributeMatrix = matrix };
                    matrix.AttributeMatrixItems.Add( newItem );
                    rockContext.SaveChanges();
                    newItem.LoadAttributes();
                    newItem.SetAttributeValue( TEMPLATE_FILE_ATTRIBUTE_KEY, newGuid.ToString() );
                    newItem.SaveAttributeValues( rockContext );
                }

                rockContext.SaveChanges();
                return matrix.Guid.ToString();
            }
        }

        public override void SetEditValue( Control control, Dictionary<string, ConfigurationValue> configurationValues, string value )
        {
            var ctrl = control as MultiFileUpload;
            if ( ctrl == null )
            {
                return;
            }

            ctrl.Value = value ?? string.Empty;

            var matrixGuid = value.AsGuidOrNull();
            if ( !matrixGuid.HasValue )
            {
                ctrl.SetCurrentFileIds( new List<int>() );
                return;
            }

            using ( var rockContext = new RockContext() )
            {
                var matrix = new AttributeMatrixService( rockContext ).Get( matrixGuid.Value );
                if ( matrix == null )
                {
                    ctrl.SetCurrentFileIds( new List<int>() );
                    return;
                }

                var fileGuids = new List<Guid>();
                foreach ( var item in matrix.AttributeMatrixItems )
                {
                    item.LoadAttributes();
                    var fg = item.GetAttributeValue( TEMPLATE_FILE_ATTRIBUTE_KEY ).AsGuidOrNull();
                    if ( fg.HasValue )
                    {
                        fileGuids.Add( fg.Value );
                    }
                }

                var ids = new BinaryFileService( rockContext ).Queryable()
                    .Where( bf => fileGuids.Contains( bf.Guid ) )
                    .Select( bf => bf.Id )
                    .ToList();

                ctrl.SetCurrentFileIds( ids );
            }
        }

        #endregion

        #region Formatting

        public override string FormatValue( Control parentControl, string value, Dictionary<string, ConfigurationValue> configurationValues, bool condensed )
        {
            var matrixGuid = value.AsGuidOrNull();
            if ( !matrixGuid.HasValue )
            {
                return string.Empty;
            }

            using ( var rockContext = new RockContext() )
            {
                var matrix = new AttributeMatrixService( rockContext ).Get( matrixGuid.Value );
                if ( matrix == null || matrix.AttributeMatrixItems == null || !matrix.AttributeMatrixItems.Any() )
                {
                    return string.Empty;
                }

                var fileGuids = new List<Guid>();
                foreach ( var item in matrix.AttributeMatrixItems )
                {
                    item.LoadAttributes();
                    var fg = item.GetAttributeValue( TEMPLATE_FILE_ATTRIBUTE_KEY ).AsGuidOrNull();
                    if ( fg.HasValue )
                    {
                        fileGuids.Add( fg.Value );
                    }
                }

                if ( !fileGuids.Any() )
                {
                    return string.Empty;
                }

                if ( condensed )
                {
                    return fileGuids.Count == 1 ? "1 file" : ( fileGuids.Count + " files" );
                }

                // The GUID-IN query doesn't guarantee order; key by Guid so we can emit in matrix order.
                var filesByGuid = new BinaryFileService( rockContext ).Queryable()
                    .Where( bf => fileGuids.Contains( bf.Guid ) )
                    .Select( bf => new { bf.Guid, bf.FileName, bf.MimeType } )
                    .ToList()
                    .ToDictionary( bf => bf.Guid );

                // fileGuids is already in matrix/display order; iterate it to preserve that order (O(n)).
                var blocks = fileGuids
                    .Where( filesByGuid.ContainsKey )
                    .Select( g =>
                    {
                        var f = filesByGuid[g];
                        return BuildFileHtml( f.Guid, f.FileName, f.MimeType );
                    } );

                return string.Join( string.Empty, blocks );
            }
        }

        private static readonly System.Text.RegularExpressions.Regex _videoExt =
            new System.Text.RegularExpressions.Regex( @"\.(mp4|webm|ogg|ogv|mov|m4v)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled );

        private static readonly System.Text.RegularExpressions.Regex _imageExt =
            new System.Text.RegularExpressions.Regex( @"\.(png|jpe?g|gif|webp|svg|bmp)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled );

        private static bool IsVideo( string fileName, string mimeType ) =>
            ( !string.IsNullOrWhiteSpace( mimeType ) && mimeType.StartsWith( "video/", StringComparison.OrdinalIgnoreCase ) )
            || ( !string.IsNullOrWhiteSpace( fileName ) && _videoExt.IsMatch( fileName ) );

        private static bool IsImage( string fileName, string mimeType ) =>
            ( !string.IsNullOrWhiteSpace( mimeType ) && mimeType.StartsWith( "image/", StringComparison.OrdinalIgnoreCase ) )
            || ( !string.IsNullOrWhiteSpace( fileName ) && _imageExt.IsMatch( fileName ) );

        /// <summary>
        /// Builds the display markup for one stored file: an inline &lt;video&gt; or &lt;img&gt;
        /// for media types (with the download link beneath), or a plain link otherwise.
        /// </summary>
        private static string BuildFileHtml( Guid guid, string fileName, string mimeType )
        {
            var url = "/GetFile.ashx?guid=" + guid;
            var encodedName = HttpUtility.HtmlEncode( fileName ?? string.Empty );
            var link = string.Format( "<a href=\"{0}\">{1}</a>", url, encodedName );

            if ( IsVideo( fileName, mimeType ) )
            {
                return string.Format(
                    "<div class=\"multifile-item multifile-video\">" +
                    "<video controls playsinline preload=\"metadata\" src=\"{0}\" aria-label=\"{1}\" " +
                    "style=\"display:block;width:100%;max-width:560px;margin:.5rem 0;background:#000;border-radius:4px;\">" +
                    "Your browser does not support the video tag.</video>" +
                    "{2}</div>",
                    url, encodedName, link );
            }

            if ( IsImage( fileName, mimeType ) )
            {
                return string.Format(
                    "<div class=\"multifile-item multifile-image\">" +
                    "<img src=\"{0}\" alt=\"{1}\" loading=\"lazy\" decoding=\"async\" " +
                    "style=\"display:block;max-width:240px;max-height:240px;margin:.5rem 0;border:1px solid #ddd;border-radius:4px;\" />" +
                    "{2}</div>",
                    url, encodedName, link );
            }

            return string.Format( "<div class=\"multifile-item\">{0}</div>", link );
        }

        #endregion

        #region Template Provisioning

        /// <summary>
        /// Ensures the shared AttributeMatrixTemplate that backs all MultiFile attributes
        /// exists, creating it with a single "File" column if needed. Returns the template's Id.
        /// </summary>
        private static int EnsureSharedTemplate( RockContext rockContext )
        {
            if ( _sharedTemplateIdCache.HasValue )
            {
                return _sharedTemplateIdCache.Value;
            }

            var templateGuid = SHARED_TEMPLATE_GUID.AsGuid();
            var templateService = new AttributeMatrixTemplateService( rockContext );
            var template = templateService.Get( templateGuid );

            if ( template == null )
            {
                template = new AttributeMatrixTemplate
                {
                    Guid = templateGuid,
                    Name = "Multi-File Upload (system)",
                    Description = "System-managed template used by org.secc.Attributes MultiFileFieldType. Do not edit.",
                    IsActive = true
                };
                templateService.Add( template );
                rockContext.SaveChanges();

                var fileFieldTypeId = FieldTypeCache.All()
                    .FirstOrDefault( ft => ft.Class == "Rock.Field.Types.FileFieldType" )?.Id;

                if ( !fileFieldTypeId.HasValue )
                {
                    throw new Exception( "Rock.Field.Types.FileFieldType not found in FieldTypeCache." );
                }

                var attributeMatrixItemEntityTypeId = EntityTypeCache.Get( typeof( AttributeMatrixItem ) ).Id;

                var fileAttribute = new Rock.Model.Attribute
                {
                    Key = TEMPLATE_FILE_ATTRIBUTE_KEY,
                    Name = "File",
                    EntityTypeId = attributeMatrixItemEntityTypeId,
                    EntityTypeQualifierColumn = "AttributeMatrixTemplateId",
                    EntityTypeQualifierValue = template.Id.ToString(),
                    FieldTypeId = fileFieldTypeId.Value,
                    Order = 0,
                    IsRequired = false,
                    IsGridColumn = true,
                    AllowSearch = false
                };
                new AttributeService( rockContext ).Add( fileAttribute );
                rockContext.SaveChanges();
            }

            _sharedTemplateIdCache = template.Id;
            return template.Id;
        }

        #endregion
    }
}
