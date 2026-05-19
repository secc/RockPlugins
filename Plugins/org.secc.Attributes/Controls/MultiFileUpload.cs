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
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace org.secc.Attributes.Controls
{
    /// <summary>
    /// Edit control for MultiFileFieldType. Renders an HTML5 multi-file picker that
    /// uploads each selected file via Rock's FileUploader handler, tracks the resulting
    /// BinaryFile Ids in a hidden field, and renders a removable list of currently
    /// attached files.
    /// </summary>
    public class MultiFileUpload : CompositeControl, IRockControl
    {
        #region IRockControl Properties

        [Bindable( true ), Category( "Appearance" ), DefaultValue( "" ), Description( "The text for the label." )]
        public string Label
        {
            get { return ViewState["Label"] as string ?? string.Empty; }
            set { ViewState["Label"] = value; }
        }

        [Bindable( true ), Category( "Appearance" ), Description( "The CSS class to add to the form-group div." )]
        public string FormGroupCssClass
        {
            get { return ViewState["FormGroupCssClass"] as string ?? string.Empty; }
            set { ViewState["FormGroupCssClass"] = value; }
        }

        [Bindable( true ), Category( "Appearance" ), DefaultValue( "" ), Description( "The help block." )]
        public string Help
        {
            get { return HelpBlock != null ? HelpBlock.Text : string.Empty; }
            set { if ( HelpBlock != null ) { HelpBlock.Text = value; } }
        }

        [Bindable( true ), Category( "Appearance" ), DefaultValue( "" ), Description( "The warning block." )]
        public string Warning
        {
            get { return WarningBlock != null ? WarningBlock.Text : string.Empty; }
            set { if ( WarningBlock != null ) { WarningBlock.Text = value; } }
        }

        [Bindable( true ), Category( "Behavior" ), DefaultValue( "false" ), Description( "Is the value required?" )]
        public bool Required
        {
            get { return ViewState["Required"] as bool? ?? false; }
            set { ViewState["Required"] = value; }
        }

        public string RequiredErrorMessage
        {
            get { return RequiredFieldValidator != null ? RequiredFieldValidator.ErrorMessage : string.Empty; }
            set { if ( RequiredFieldValidator != null ) { RequiredFieldValidator.ErrorMessage = value; } }
        }

        public string ValidationGroup
        {
            get { return ViewState["ValidationGroup"] as string; }
            set
            {
                ViewState["ValidationGroup"] = value;
                if ( RequiredFieldValidator != null ) { RequiredFieldValidator.ValidationGroup = value; }
            }
        }

        public virtual bool IsValid
        {
            get
            {
                if ( !Required )
                {
                    return true;
                }
                return GetCurrentFileIds().Any();
            }
        }

        public HelpBlock HelpBlock { get; set; }
        public WarningBlock WarningBlock { get; set; }
        public RequiredFieldValidator RequiredFieldValidator { get; set; }

        public bool DisplayRequiredIndicator
        {
            get { return ViewState["Required"] as bool? ?? false; }
            set { }
        }

        #endregion

        #region Public Properties

        public Guid? BinaryFileTypeGuid
        {
            get { return ViewState["BinaryFileTypeGuid"] as Guid?; }
            set { ViewState["BinaryFileTypeGuid"] = value; }
        }

        public int MaxFiles
        {
            get { return ViewState["MaxFiles"] as int? ?? 0; }
            set { ViewState["MaxFiles"] = value; }
        }

        public string AllowedExtensions
        {
            get { return ViewState["AllowedExtensions"] as string ?? string.Empty; }
            set { ViewState["AllowedExtensions"] = value ?? string.Empty; }
        }

        /// <summary>
        /// The persisted AttributeMatrix Guid. Set by the field type before render;
        /// returned (potentially updated) after postback by the field type's GetEditValue.
        /// </summary>
        public string Value
        {
            get { return ViewState["Value"] as string ?? string.Empty; }
            set { ViewState["Value"] = value ?? string.Empty; }
        }

        #endregion

        #region Private State

        private HiddenField _hfFileIds;

        #endregion

        public MultiFileUpload()
            : base()
        {
            HelpBlock = new HelpBlock();
            WarningBlock = new WarningBlock();
            RequiredFieldValidator = new HiddenFieldValidator();
        }

        /// <summary>
        /// Seeds the hidden field with the BinaryFile Ids currently attached to this attribute.
        /// Called by the field type's SetEditValue on initial page load.
        /// </summary>
        public void SetCurrentFileIds( IEnumerable<int> ids )
        {
            EnsureChildControls();
            _hfFileIds.Value = ids == null ? string.Empty : string.Join( ",", ids );
        }

        /// <summary>
        /// Reads the BinaryFile Ids that the user has chosen (initial set plus any uploaded,
        /// minus any removed). The field type calls this from GetEditValue.
        /// </summary>
        public List<int> GetCurrentFileIds()
        {
            EnsureChildControls();
            return ( _hfFileIds.Value ?? string.Empty )
                .Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.Trim().AsIntegerOrNull() )
                .Where( i => i.HasValue && i.Value > 0 )
                .Select( i => i.Value )
                .Distinct()
                .ToList();
        }

        protected override void OnLoad( EventArgs e )
        {
            EnsureChildControls();
            base.OnLoad( e );
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Controls.Clear();
            RockControlHelper.CreateChildControls( this, Controls );

            _hfFileIds = new HiddenField { ID = "hfFileIds" };
            Controls.Add( _hfFileIds );

            if ( RequiredFieldValidator != null )
            {
                RequiredFieldValidator.ControlToValidate = _hfFileIds.ID;
                RequiredFieldValidator.InitialValue = string.Empty;
                RequiredFieldValidator.Enabled = Required;
                RequiredFieldValidator.ValidationGroup = ValidationGroup;
            }
        }

        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        public void RenderBaseControl( HtmlTextWriter writer )
        {
            EnsureChildControls();

            // Look up the file names for currently-attached ids so we can render the list server-side.
            var currentIds = GetCurrentFileIds();
            var files = new List<FileDisplay>();
            if ( currentIds.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    files = new BinaryFileService( rockContext ).Queryable()
                        .Where( bf => currentIds.Contains( bf.Id ) )
                        .Select( bf => new FileDisplay { Id = bf.Id, Guid = bf.Guid, FileName = bf.FileName } )
                        .ToList();
                }
            }

            writer.AddAttribute( HtmlTextWriterAttribute.Class, "multi-file-upload" );
            writer.AddAttribute( HtmlTextWriterAttribute.Id, ClientID + "_wrap" );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );

            _hfFileIds.RenderControl( writer );

            // File list
            writer.AddAttribute( HtmlTextWriterAttribute.Id, ClientID + "_list" );
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "multi-file-upload-list list-unstyled" );
            writer.RenderBeginTag( HtmlTextWriterTag.Ul );
            foreach ( var f in files )
            {
                RenderListItem( writer, f.Id, f.Guid, f.FileName );
            }
            writer.RenderEndTag(); // ul

            // File picker
            writer.AddAttribute( HtmlTextWriterAttribute.Type, "file" );
            writer.AddAttribute( HtmlTextWriterAttribute.Id, ClientID + "_picker" );
            writer.AddAttribute( "multiple", "multiple" );
            if ( !string.IsNullOrWhiteSpace( AllowedExtensions ) )
            {
                var accept = string.Join( ",", AllowedExtensions
                    .Split( ',' )
                    .Select( x => x.Trim().TrimStart( '.' ) )
                    .Where( x => x.Length > 0 )
                    .Select( x => "." + x ) );
                writer.AddAttribute( "accept", accept );
            }
            writer.RenderBeginTag( HtmlTextWriterTag.Input );
            writer.RenderEndTag();

            writer.RenderEndTag(); // div

            RegisterClientScript();
        }

        private void RenderListItem( HtmlTextWriter writer, int id, Guid guid, string fileName )
        {
            writer.AddAttribute( "data-file-id", id.ToString() );
            writer.RenderBeginTag( HtmlTextWriterTag.Li );

            writer.AddAttribute( HtmlTextWriterAttribute.Href, "/GetFile.ashx?guid=" + guid );
            writer.AddAttribute( HtmlTextWriterAttribute.Target, "_blank" );
            writer.RenderBeginTag( HtmlTextWriterTag.A );
            writer.WriteEncodedText( string.IsNullOrWhiteSpace( fileName ) ? "(file)" : fileName );
            writer.RenderEndTag(); // a

            writer.Write( " " );

            writer.AddAttribute( HtmlTextWriterAttribute.Type, "button" );
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "btn btn-xs btn-link js-mfu-remove" );
            writer.AddAttribute( "title", "Remove" );
            writer.RenderBeginTag( HtmlTextWriterTag.Button );
            writer.Write( "&times;" );
            writer.RenderEndTag(); // button

            writer.RenderEndTag(); // li
        }

        private void RegisterClientScript()
        {
            var uploadQs = "?isBinaryFile=T";
            if ( BinaryFileTypeGuid.HasValue )
            {
                uploadQs += "&fileTypeGuid=" + BinaryFileTypeGuid.Value;
            }

            var allowedExtsJs = ( AllowedExtensions ?? string.Empty ).Replace( "\\", "\\\\" ).Replace( "'", "\\'" );

            var script = @"
(function() {
    var clientId = '" + ClientID + @"';
    var hf = document.getElementById('" + _hfFileIds.ClientID + @"');
    var list = document.getElementById(clientId + '_list');
    var picker = document.getElementById(clientId + '_picker');
    if (!hf || !list || !picker) { return; }
    var maxFiles = " + MaxFiles + @";
    var allowed = '" + allowedExtsJs + @"'.split(',')
        .map(function(s){ return s.trim().toLowerCase().replace(/^\./,''); })
        .filter(function(s){ return s.length > 0; });
    var uploadUrl = '/FileUploader.ashx" + uploadQs + @"';

    function getIds() {
        return (hf.value || '').split(',').filter(function(s){ return s.length > 0; });
    }
    function setIds(arr) { hf.value = arr.join(','); }
    function makeRemoveBtn() {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-xs btn-link js-mfu-remove';
        btn.title = 'Remove';
        btn.innerHTML = '&times;';
        return btn;
    }

    function showWarning(msg) {
        var prior = document.querySelector('.js-mfu-alert[data-mfu=""' + clientId + '""]');
        if (prior && prior.parentNode) { prior.parentNode.removeChild(prior); }

        var wrap = document.getElementById(clientId + '_wrap');
        if (!wrap || !wrap.parentNode) { return; }

        var alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-warning alert-dismissable js-mfu-alert';
        alertDiv.setAttribute('data-mfu', clientId);

        var closeBtn = document.createElement('button');
        closeBtn.type = 'button';
        closeBtn.className = 'close';
        closeBtn.setAttribute('data-dismiss', 'alert');
        closeBtn.setAttribute('aria-hidden', 'true');
        closeBtn.innerHTML = '&times;';
        alertDiv.appendChild(closeBtn);

        var label = document.createElement('strong');
        label.innerHTML = '<i class=""fa fa-exclamation-triangle""></i> Warning ';
        alertDiv.appendChild(label);

        alertDiv.appendChild(document.createTextNode(msg));

        wrap.parentNode.insertBefore(alertDiv, wrap);
    }

    list.addEventListener('click', function(e) {
        var btn = e.target;
        while (btn && btn !== list && !(btn.classList && btn.classList.contains('js-mfu-remove'))) {
            btn = btn.parentNode;
        }
        if (!btn || btn === list) { return; }
        e.preventDefault();
        var li = btn.parentNode;
        var id = li.getAttribute('data-file-id');
        setIds(getIds().filter(function(x){ return x !== id; }));
        li.parentNode.removeChild(li);
    });

    picker.addEventListener('change', function() {
        // Clear any prior warning — user is taking a new upload action.
        var priorAlert = document.querySelector('.js-mfu-alert[data-mfu=""' + clientId + '""]');
        if (priorAlert && priorAlert.parentNode) { priorAlert.parentNode.removeChild(priorAlert); }

        var files = picker.files ? Array.prototype.slice.call(picker.files) : [];
        if (files.length === 0) { return; }

        // ---- Pre-check 1: extensions (if a whitelist is configured) ----
        if (allowed.length > 0) {
            var invalid = files.filter(function(f) {
                var ext = (f.name.split('.').pop() || '').toLowerCase();
                return allowed.indexOf(ext) === -1;
            }).map(function(f) { return f.name; });

            if (invalid.length > 0) {
                showWarning(
                    'Cannot upload — invalid file extension' +
                    (invalid.length === 1 ? '' : 's') + ': ' + invalid.join(', ') +
                    '. Allowed: ' + allowed.join(', ') + '. No files were uploaded.'
                );
                picker.value = '';
                return;
            }
        }

        // ---- Pre-check 2: max files ----
        if (maxFiles > 0) {
            var existingCount = getIds().length;
            var projected = existingCount + files.length;
            if (projected > maxFiles) {
                showWarning(
                    'Cannot upload — would exceed the maximum of ' + maxFiles +
                    ' file' + (maxFiles === 1 ? '' : 's') +
                    ' (you have ' + existingCount + ' attached and selected ' +
                    files.length + ' more). No files were uploaded.'
                );
                picker.value = '';
                return;
            }
        }

        // ---- All pre-checks passed; upload each file ----
        files.forEach(function(file) {
            var fd = new FormData();
            fd.append('file', file);
            var xhr = new XMLHttpRequest();
            xhr.open('POST', uploadUrl, true);
            xhr.onload = function() {
                if (xhr.status !== 200) {
                    var msg = (xhr.status === 406)
                        ? 'File type not allowed.'
                        : (xhr.responseText || ('Unable to upload (HTTP ' + xhr.status + ').'));
                    showWarning(msg);
                    return;
                }
                var resp;
                try { resp = JSON.parse(xhr.responseText); }
                catch (err) { showWarning('Upload response parse error: ' + err); return; }
                var id = (resp.Id || resp.id || '').toString();
                var guid = resp.Guid || resp.guid || '';
                var fileName = resp.FileName || resp.fileName || file.name;
                if (!id) { showWarning('Upload response missing Id.'); return; }
                var ids = getIds();
                if (ids.indexOf(id) !== -1) { return; }
                ids.push(id);
                setIds(ids);
                var li = document.createElement('li');
                li.setAttribute('data-file-id', id);
                var a = document.createElement('a');
                a.href = guid ? ('/GetFile.ashx?guid=' + guid) : ('/GetFile.ashx?id=' + id);
                a.target = '_blank';
                a.textContent = fileName;
                li.appendChild(a);
                li.appendChild(document.createTextNode(' '));
                li.appendChild(makeRemoveBtn());
                list.appendChild(li);
            };
            xhr.onerror = function() {
                showWarning('Unable to upload ""' + file.name + '"" — network error.');
            };
            xhr.send(fd);
        });
        picker.value = '';
    });
})();
";
            ScriptManager.RegisterStartupScript( this, GetType(), "mfu_" + ClientID, script, true );
        }

        private class FileDisplay
        {
            public int Id { get; set; }
            public Guid Guid { get; set; }
            public string FileName { get; set; }
        }
    }
}
