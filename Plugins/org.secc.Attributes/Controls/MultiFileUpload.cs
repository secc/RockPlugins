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
using org.secc.Attributes.FieldTypes;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace org.secc.Attributes.Controls
{
    /// <summary>
    /// Edit control for MultiFileFieldType. Renders an HTML5 multi-file picker that
    /// uploads each selected file via Rock's FileUploader handler, tracks the resulting
    /// BinaryFile Guids in a hidden field, and renders a removable list of currently
    /// attached files. Guids, not Ids: Ids are sequential, so a hidden field of Ids lets a
    /// visitor claim someone else's pending upload by guessing a nearby number.
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
                if ( _cvFiles != null ) { _cvFiles.ValidationGroup = value; }
            }
        }

        public virtual bool IsValid
        {
            get
            {
                // Only once the validator has run and failed, so a postback that merely recorded a
                // rejection (e.g. a visibility-rule postback) doesn't show a red field with no message.
                if ( _cvFiles != null && !_cvFiles.IsValid )
                {
                    return false;
                }
                if ( !Required )
                {
                    return true;
                }
                return GetCurrentFileGuids().Any();
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
        /// The persisted attribute value (comma-separated BinaryFile Guids, or a legacy
        /// AttributeMatrix Guid). Set by the field type's SetEditValue before render.
        /// </summary>
        public string Value
        {
            get { return ViewState["Value"] as string ?? string.Empty; }
            set { ViewState["Value"] = value ?? string.Empty; }
        }

        /// <summary>
        /// The value as SetEditValue loaded it. Unlike <see cref="Value"/>, GetEditValue never
        /// overwrites it, so it still says how many files the saved value held before this edit.
        /// </summary>
        public string LoadedValue
        {
            get { return ViewState["LoadedValue"] as string ?? string.Empty; }
            set { ViewState["LoadedValue"] = value ?? string.Empty; }
        }

        /// <summary>True once SetEditValue has set <see cref="LoadedValue"/> for this control.</summary>
        public bool HasLoadedValue
        {
            get { return ViewState["LoadedValue"] != null; }
        }

        #endregion

        #region Private State

        private HiddenField _hfFileIds;
        private CustomValidator _cvFiles;

        /// <summary>
        /// Posted Guids that were rejected (and stripped from the hidden field) but not yet reported
        /// to the visitor. Kept in ViewState because WorkflowEntry calls GetEditValue on postbacks
        /// that don't validate (e.g. conditional-visibility postbacks): the strip persists, so the
        /// rejection must too, or the next submit would pass and save without the file. The
        /// validator reports it once and clears it.
        /// </summary>
        private List<Guid> PendingRejectedFileGuids
        {
            get
            {
                return ( ViewState["PendingRejectedFileGuids"] as string ?? string.Empty )
                    .Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( s => s.AsGuid() )
                    .ToList();
            }
            set { ViewState["PendingRejectedFileGuids"] = value == null ? string.Empty : string.Join( ",", value ); }
        }

        /// <summary>
        /// Files GetEditValue switched from temporary to permanent during this request. If this
        /// request's validation fails, OnPreRender switches them back, so an abandoned form doesn't
        /// leave permanent files nothing points at.
        /// </summary>
        private readonly List<Guid> _filesMadePermanent = new List<Guid>();

        /// <summary>True once this control's validator has run in this request.</summary>
        private bool _validatedThisRequest;

        #endregion

        public MultiFileUpload()
            : base()
        {
            HelpBlock = new HelpBlock();
            WarningBlock = new WarningBlock();
            RequiredFieldValidator = new HiddenFieldValidator();
        }

        /// <summary>
        /// Sets the hidden field to the BinaryFile Guids attached to this attribute, in order.
        /// Called by the field type's SetEditValue on initial load, and by GetEditValue to drop
        /// any posted entries it rejected so they are not re-rendered.
        /// </summary>
        public void SetCurrentFileGuids( IEnumerable<Guid> guids )
        {
            EnsureChildControls();
            _hfFileIds.Value = guids == null ? string.Empty : string.Join( ",", guids.Select( g => g.ToString() ) );
        }

        /// <summary>
        /// Reads the BinaryFile Guids that the user has chosen (initial set plus any uploaded,
        /// minus any removed), in order. The field type calls this from GetEditValue.
        /// </summary>
        public List<Guid> GetCurrentFileGuids()
        {
            EnsureChildControls();
            return ( _hfFileIds.Value ?? string.Empty )
                .Split( new[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .Select( s => s.Trim().AsGuidOrNull() )
                .Where( g => g.HasValue && g.Value != Guid.Empty )
                .Select( g => g.Value )
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Records posted Guids that were not accepted, so the next validating submit fails instead
        /// of the value silently saving without them. GetEditValue strips rejected entries from the
        /// hidden field (and WorkflowEntry calls it before it validates), so the validator can't
        /// rediscover them itself and relies on this record.
        /// </summary>
        internal void MarkFilesRejected( IEnumerable<Guid> guids )
        {
            var pending = PendingRejectedFileGuids;
            var added = false;
            foreach ( var guid in guids )
            {
                if ( !pending.Contains( guid ) )
                {
                    pending.Add( guid );
                    added = true;
                }
            }

            if ( added )
            {
                PendingRejectedFileGuids = pending;
            }
        }

        /// <summary>
        /// Records files GetEditValue just made permanent, so a failed submit can undo it.
        /// </summary>
        internal void MarkFilesMadePermanent( IEnumerable<Guid> guids )
        {
            _filesMadePermanent.AddRange( guids );
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

            // Server-only: the accept rule needs the database.
            _cvFiles = new CustomValidator
            {
                ID = "cvFiles",
                Display = ValidatorDisplay.Dynamic,
                CssClass = "validation-error help-inline",
                EnableClientScript = false,
                ValidationGroup = ValidationGroup
            };
            _cvFiles.ServerValidate += cvFiles_ServerValidate;
            Controls.Add( _cvFiles );
        }

        /// <summary>
        /// Fails when a posted file can't be attached: it no longer exists (e.g. Cleanup removed a
        /// stale temporary upload), is the wrong file type, or is someone else's saved file. The
        /// rejected entries are dropped from the hidden field, so the re-rendered list shows only
        /// the files that will save, and the visitor can upload the missing ones again.
        /// </summary>
        private void cvFiles_ServerValidate( object source, ServerValidateEventArgs args )
        {
            _validatedThisRequest = true;
            var postedGuids = GetCurrentFileGuids();
            if ( postedGuids.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    var acceptedGuids = MultiFileFieldType.GetAcceptableFiles( postedGuids, BinaryFileTypeGuid, Value, rockContext ).Keys;
                    var rejectedGuids = postedGuids.Where( g => !acceptedGuids.Contains( g ) ).ToList();
                    if ( rejectedGuids.Any() )
                    {
                        MarkFilesRejected( rejectedGuids );
                        SetCurrentFileGuids( postedGuids.Where( g => acceptedGuids.Contains( g ) ) );
                    }
                }
            }

            // ValidationSummary renders ErrorMessage as HTML.
            var name = HttpUtility.HtmlEncode( string.IsNullOrWhiteSpace( Label ) ? "Files" : Label );
            var messages = new List<string>();

            // Worded for a rejection recorded on an earlier postback, when the visitor may already
            // have noticed and re-uploaded. The list is cleared in OnPreRender, not here, so a
            // second Validate() of the same group in this request still fails.
            var rejectedCount = PendingRejectedFileGuids.Count;
            if ( rejectedCount > 0 )
            {
                messages.Add( string.Format(
                    "{0}: {1} could not be attached and {2} removed from the list. Check the list and upload any missing files again.",
                    name,
                    rejectedCount == 1 ? "one file" : rejectedCount + " files",
                    rejectedCount == 1 ? "was" : "were" ) );
            }

            // The browser enforces MaxFiles too, but the hidden field can be edited. A value saved
            // before an admin lowered MaxFiles may keep what it has, just not grow.
            var fileCount = postedGuids.Count == 0 ? 0 : GetCurrentFileGuids().Count;
            var limit = MaxFiles > 0 && fileCount > MaxFiles
                ? Math.Max( MaxFiles, MultiFileFieldType.CountFilesInValue( LoadedValue ) )
                : MaxFiles;
            if ( limit > 0 && fileCount > limit )
            {
                messages.Add( string.Format(
                    "{0}: no more than {1} {2} can be attached. Remove {3} and submit again.",
                    name,
                    limit,
                    limit == 1 ? "file" : "files",
                    fileCount - limit == 1 ? "one file" : ( fileCount - limit ) + " files" ) );
            }

            args.IsValid = !messages.Any();
            if ( !args.IsValid )
            {
                _cvFiles.ErrorMessage = string.Join( " ", messages );
            }
        }

        private bool ValidationFailedThisRequest()
        {
            return _validatedThisRequest
                && Page.GetValidators( ValidationGroup ).Cast<IValidator>().Any( v => !v.IsValid );
        }

        protected override void OnPreRender( EventArgs e )
        {
            // The visitor has now been told, so a resubmit goes through with the files still listed.
            if ( _cvFiles != null && !_cvFiles.IsValid )
            {
                PendingRejectedFileGuids = new List<Guid>();
            }

            // GetEditValue runs before validation (WorkflowEntry reads values first), so it may have
            // made new uploads permanent for a submit that then failed validation. Put them back to
            // temporary: if the visitor abandons the form, Rock Cleanup removes them as it does any
            // stale upload, and a resubmit still accepts them as fresh uploads.
            // Only when this request certainly saved nothing: this validator ran and its group
            // failed (WorkflowEntry and Workflow Detail both stop before saving then). Any other
            // request may have saved the value, e.g. a form button with Causes Validation off, and
            // undoing there would let Cleanup delete files a saved value points at. Leaking an
            // abandoned upload is the lesser failure.
            if ( _filesMadePermanent.Any() && Page != null && ValidationFailedThisRequest() )
            {
                using ( var rockContext = new RockContext() )
                {
                    var guids = _filesMadePermanent.Distinct().ToList();
                    var files = new BinaryFileService( rockContext ).Queryable()
                        .Where( bf => guids.Contains( bf.Guid ) && !bf.IsTemporary )
                        .ToList();
                    foreach ( var file in files )
                    {
                        file.IsTemporary = true;
                    }

                    rockContext.SaveChanges();
                }

                _filesMadePermanent.Clear();
            }

            base.OnPreRender( e );
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

            // Look up the file names for the attached Guids so we can render the list server-side,
            // in the stored order.
            var currentGuids = GetCurrentFileGuids();
            var files = new List<FileDisplay>();
            if ( currentGuids.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    var filesByGuid = new BinaryFileService( rockContext ).Queryable()
                        .Where( bf => currentGuids.Contains( bf.Guid ) )
                        .Select( bf => new FileDisplay { Guid = bf.Guid, FileName = bf.FileName } )
                        .ToList()
                        .ToDictionary( f => f.Guid );
                    files = currentGuids.Where( filesByGuid.ContainsKey ).Select( g => filesByGuid[g] ).ToList();
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
                RenderListItem( writer, f.Guid, f.FileName );
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

            _cvFiles.RenderControl( writer );

            writer.RenderEndTag(); // div

            RegisterClientScript();
        }

        private void RenderListItem( HtmlTextWriter writer, Guid guid, string fileName )
        {
            writer.AddAttribute( "data-file-guid", guid.ToString() );
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

            // Leading ";" because ASP.NET emits the previous startup script (e.g. a validator
            // declaration) without a trailing semicolon; without it "(function" parses as a call
            // on that expression and throws, so the uploader never attaches.
            var script = @"
;(function() {
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
        return (hf.value || '').toLowerCase().split(',').filter(function(s){ return s.length > 0; });
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
        var guid = (li.getAttribute('data-file-guid') || '').toLowerCase();
        setIds(getIds().filter(function(x){ return x !== guid; }));
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
                var guid = (resp.Guid || resp.guid || '').toString().toLowerCase();
                var fileName = resp.FileName || resp.fileName || file.name;
                if (!guid) { showWarning('Upload response missing Guid.'); return; }
                var ids = getIds();
                if (ids.indexOf(guid) !== -1) { return; }
                ids.push(guid);
                setIds(ids);
                var li = document.createElement('li');
                li.setAttribute('data-file-guid', guid);
                var a = document.createElement('a');
                a.href = '/GetFile.ashx?guid=' + guid;
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
            public Guid Guid { get; set; }
            public string FileName { get; set; }
        }
    }
}
