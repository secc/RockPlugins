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
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Event
{
    [DisplayName( "Camp Placement Import" )]
    [Category( "SECC > Event" )]
    [Description( "Imports a CSV file to place camp registrants into placement groups." )]

    [CustomRadioListField( "Default Group Member Status",
        Description = "The group member status to use when adding a person to a placement group.",
        ListSource = "1^Active,2^Pending,0^Inactive",
        IsRequired = true,
        DefaultValue = "1",
        Order = 0,
        Key = AttributeKey.DefaultGroupMemberStatus )]

    [IntegerField( "Batch Size",
        Description = "The number of group member records to save per batch. Smaller batches are safer but slower.",
        IsRequired = true,
        DefaultIntegerValue = 50,
        Order = 1,
        Key = AttributeKey.BatchSize )]

    public partial class CampPlacementImport : RockBlock
    {
        #region Attribute Keys

        private static class AttributeKey
        {
            public const string DefaultGroupMemberStatus = "DefaultGroupMemberStatus";
            public const string BatchSize = "BatchSize";
        }

        #endregion

        #region ViewState Properties

        /// <summary>
        /// The CSV headers parsed from the uploaded file.
        /// </summary>
        private List<string> CsvHeaders
        {
            get { return ViewState["CsvHeaders"] as List<string> ?? new List<string>(); }
            set { ViewState["CsvHeaders"] = value; }
        }

        /// <summary>
        /// The CSV data rows parsed from the uploaded file.
        /// Each row is a list of cell values in the same order as the headers.
        /// </summary>
        private List<List<string>> CsvRows
        {
            get { return ViewState["CsvRows"] as List<List<string>> ?? new List<List<string>>(); }
            set { ViewState["CsvRows"] = value; }
        }

        /// <summary>
        /// The number of placement mapping rows to display in the repeater.
        /// </summary>
        private int MappingCount
        {
            get { return ViewState["MappingCount"] as int? ?? 1; }
            set { ViewState["MappingCount"] = value; }
        }

        /// <summary>
        /// Stores the selected registration instance id across postbacks.
        /// </summary>
        private int? SelectedRegistrationInstanceId
        {
            get { return ViewState["SelectedRegistrationInstanceId"] as int?; }
            set { ViewState["SelectedRegistrationInstanceId"] = value; }
        }

        /// <summary>
        /// Stores the binary file id of the uploaded CSV so it can be cleaned up.
        /// </summary>
        private int? UploadedBinaryFileId
        {
            get { return ViewState["UploadedBinaryFileId"] as int?; }
            set { ViewState["UploadedBinaryFileId"] = value; }
        }

        /// <summary>
        /// Stores the base parent group ID to filter child groups in mappings.
        /// </summary>
        private int? BaseParentGroupId
        {
            get { return ViewState["BaseParentGroupId"] as int?; }
            set { ViewState["BaseParentGroupId"] = value; }
        }

        #endregion

        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gPreview.GridRebind += gPreview_GridRebind;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbWarning.Visible = false;
            nbInfo.Visible = false;
            nbSuccess.Visible = false;
        }

        #endregion

        #region Step 1 – Select Registration Instance

        protected void btnInstanceNext_Click( object sender, EventArgs e )
        {
            if ( !ripInstance.RegistrationInstanceId.HasValue )
            {
                ShowWarning( "Please select a registration instance." );
                return;
            }

            SelectedRegistrationInstanceId = ripInstance.RegistrationInstanceId;

            SetActivePanel( pnlUpload );
        }

        #endregion

        #region Step 2 – Upload CSV

        protected void fuCsvFile_FileUploaded( object sender, FileUploaderEventArgs e )
        {
            if ( !e.BinaryFileId.HasValue )
            {
                ShowWarning( "File upload failed. Please try again." );
                return;
            }

            UploadedBinaryFileId = e.BinaryFileId.Value;

            using ( var rockContext = new RockContext() )
            {
                var binaryFile = new BinaryFileService( rockContext ).Get( e.BinaryFileId.Value );
                if ( binaryFile == null )
                {
                    ShowWarning( "Could not read the uploaded file." );
                    return;
                }

                var csvContent = binaryFile.ContentsToString();
                if ( string.IsNullOrWhiteSpace( csvContent ) )
                {
                    ShowWarning( "The uploaded file is empty." );
                    return;
                }

                ParseCsv( csvContent );
            }

            if ( !CsvHeaders.Any() )
            {
                ShowWarning( "Could not parse any columns from the CSV. Make sure the first row contains headers." );
                return;
            }

            if ( !CsvRows.Any() )
            {
                ShowWarning( "The CSV file has headers but no data rows." );
                return;
            }

            ShowInfo( string.Format( "Loaded {0} rows with {1} columns.", CsvRows.Count, CsvHeaders.Count ) );

            MappingCount = 1;
            SetActivePanel( pnlMapping );
            BindMappingControls();
        }

        protected void btnUploadBack_Click( object sender, EventArgs e )
        {
            SetActivePanel( pnlSelectInstance );
        }

        #endregion

        #region Step 3 – Column Mapping

        protected void gpBasePlacementGroup_SelectItem( object sender, EventArgs e )
        {
            BaseParentGroupId = gpBasePlacementGroup.SelectedValueAsInt();

            // Rebind mappings to refresh the parent group controls
            var savedColumns = ViewState["SavedMappingColumns"] as List<string> ?? new List<string>();
            var savedGroups = ViewState["SavedMappingGroups"] as List<int?> ?? new List<int?>();

            BindMappingControls( savedColumns, savedGroups );
        }

        protected void rptMappings_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {
                var ddlCsvColumn = e.Item.FindControl( "ddlCsvColumn" ) as RockDropDownList;
                if ( ddlCsvColumn != null )
                {
                    ddlCsvColumn.Items.Clear();
                    ddlCsvColumn.Items.Add( new ListItem( "", "" ) );
                    foreach ( var header in CsvHeaders )
                    {
                        ddlCsvColumn.Items.Add( new ListItem( header, header ) );
                    }
                }

                var gpParentGroup = e.Item.FindControl( "gpParentGroup" ) as GroupPicker;
                var ddlParentGroupChild = e.Item.FindControl( "ddlParentGroupChild" ) as RockDropDownList;

                if ( BaseParentGroupId.HasValue )
                {
                    // Base group is set — show filtered dropdown of children
                    gpParentGroup.Visible = false;
                    ddlParentGroupChild.Visible = true;

                    ddlParentGroupChild.Items.Clear();
                    ddlParentGroupChild.Items.Add( new ListItem( "", "" ) );

                    using ( var rockContext = new RockContext() )
                    {
                        var childGroups = new GroupService( rockContext )
                            .Queryable()
                            .Where( g => g.ParentGroupId == BaseParentGroupId.Value
                                      && g.IsActive
                                      && !g.IsArchived )
                            .OrderBy( g => g.Name )
                            .Select( g => new { g.Id, g.Name } )
                            .ToList();

                        foreach ( var group in childGroups )
                        {
                            ddlParentGroupChild.Items.Add( new ListItem( group.Name, group.Id.ToString() ) );
                        }
                    }
                }
                else
                {
                    // No base group — show full GroupPicker
                    gpParentGroup.Visible = true;
                    ddlParentGroupChild.Visible = false;
                }
            }
        }

        protected void btnAddMapping_Click( object sender, EventArgs e )
        {
            SaveMappingSelections();
            MappingCount++;

            var savedColumns = ViewState["SavedMappingColumns"] as List<string> ?? new List<string>();
            var savedGroups = ViewState["SavedMappingGroups"] as List<int?> ?? new List<int?>();

            BindMappingControls( savedColumns, savedGroups );
        }

        protected void btnRemoveMapping_Click( object sender, EventArgs e )
        {
            if ( MappingCount > 1 )
            {
                // Determine which row to remove
                var btn = sender as LinkButton;
                var item = btn.NamingContainer as RepeaterItem;
                int removeIndex = item.ItemIndex;

                SaveMappingSelections();

                var savedColumns = ViewState["SavedMappingColumns"] as List<string> ?? new List<string>();
                var savedGroups = ViewState["SavedMappingGroups"] as List<int?> ?? new List<int?>();

                if ( removeIndex < savedColumns.Count )
                {
                    savedColumns.RemoveAt( removeIndex );
                }
                if ( removeIndex < savedGroups.Count )
                {
                    savedGroups.RemoveAt( removeIndex );
                }

                ViewState["SavedMappingColumns"] = savedColumns;
                ViewState["SavedMappingGroups"] = savedGroups;

                MappingCount--;
                BindMappingControls( savedColumns, savedGroups );
            }
        }

        protected void btnMappingBack_Click( object sender, EventArgs e )
        {
            SetActivePanel( pnlUpload );
        }

        protected void btnPreview_Click( object sender, EventArgs e )
        {
            SaveMappingSelections();

            var mappings = GetMappingsFromUI();
            if ( !mappings.Any() )
            {
                ShowWarning( "Please configure at least one placement mapping." );
                return;
            }

            var firstNameCol = ddlFirstNameCol.SelectedValue;
            var lastNameCol = ddlLastNameCol.SelectedValue;
            if ( string.IsNullOrWhiteSpace( firstNameCol ) || string.IsNullOrWhiteSpace( lastNameCol ) )
            {
                ShowWarning( "Please select both First Name and Last Name columns." );
                return;
            }

            var previewData = BuildPreviewData( firstNameCol, lastNameCol, mappings );
            BindPreviewGrid( previewData );
            SetActivePanel( pnlPreview );
        }

        #endregion

        #region Step 4 – Preview

        private void gPreview_GridRebind( object sender, GridRebindEventArgs e )
        {
            // Re-run preview with current mappings
            var mappings = GetMappingsFromUI();
            var firstNameCol = ddlFirstNameCol.SelectedValue;
            var lastNameCol = ddlLastNameCol.SelectedValue;
            if ( !string.IsNullOrWhiteSpace( firstNameCol ) && !string.IsNullOrWhiteSpace( lastNameCol ) && mappings.Any() )
            {
                var previewData = BuildPreviewData( firstNameCol, lastNameCol, mappings );
                BindPreviewGrid( previewData );
            }
        }

        protected void btnPreviewBack_Click( object sender, EventArgs e )
        {
            SetActivePanel( pnlMapping );

            var savedColumns = ViewState["SavedMappingColumns"] as List<string> ?? new List<string>();
            var savedGroups = ViewState["SavedMappingGroups"] as List<int?> ?? new List<int?>();

            BindMappingControls( savedColumns, savedGroups );
        }

        protected void btnProcess_Click( object sender, EventArgs e )
        {
            var mappings = GetMappingsFromUI();
            var firstNameCol = ddlFirstNameCol.SelectedValue;
            var lastNameCol = ddlLastNameCol.SelectedValue;

            ProcessPlacements( firstNameCol, lastNameCol, mappings );

            SetActivePanel( pnlResults );
        }

        #endregion

        #region Step 5 – Results

        protected void btnStartOver_Click( object sender, EventArgs e )
        {
            CsvHeaders = new List<string>();
            CsvRows = new List<List<string>>();
            MappingCount = 1;
            SelectedRegistrationInstanceId = null;
            UploadedBinaryFileId = null;
            BaseParentGroupId = null;
            fuCsvFile.BinaryFileId = null;
            gpBasePlacementGroup.SetValue( ( Group ) null );

            SetActivePanel( pnlSelectInstance );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses raw CSV text into headers and data rows.
        /// Handles quoted fields that may contain commas or newlines.
        /// </summary>
        private void ParseCsv( string csvContent )
        {
            var rows = new List<List<string>>();
            var headers = new List<string>();

            using ( var reader = new StringReader( csvContent ) )
            {
                var allRows = ReadCsvRows( reader );
                if ( allRows.Any() )
                {
                    headers = allRows[0];
                    for ( int i = 1; i < allRows.Count; i++ )
                    {
                        // Skip completely empty rows
                        if ( allRows[i].Any( cell => !string.IsNullOrWhiteSpace( cell ) ) )
                        {
                            rows.Add( allRows[i] );
                        }
                    }
                }
            }

            CsvHeaders = headers;
            CsvRows = rows;
        }

        /// <summary>
        /// Reads all rows from a CSV reader, handling quoted fields.
        /// </summary>
        private List<List<string>> ReadCsvRows( StringReader reader )
        {
            var rows = new List<List<string>>();
            string line;
            while ( ( line = reader.ReadLine() ) != null )
            {
                var fields = new List<string>();
                int i = 0;
                while ( i < line.Length )
                {
                    if ( line[i] == '"' )
                    {
                        // Quoted field
                        i++;
                        var field = new System.Text.StringBuilder();
                        while ( i < line.Length )
                        {
                            if ( line[i] == '"' )
                            {
                                if ( i + 1 < line.Length && line[i + 1] == '"' )
                                {
                                    field.Append( '"' );
                                    i += 2;
                                }
                                else
                                {
                                    i++;
                                    break;
                                }
                            }
                            else
                            {
                                field.Append( line[i] );
                                i++;
                            }
                        }

                        fields.Add( field.ToString().Trim() );

                        // skip comma after closing quote
                        if ( i < line.Length && line[i] == ',' )
                        {
                            i++;
                        }
                    }
                    else
                    {
                        // Unquoted field
                        int nextComma = line.IndexOf( ',', i );
                        if ( nextComma == -1 )
                        {
                            fields.Add( line.Substring( i ).Trim() );
                            i = line.Length;
                        }
                        else
                        {
                            fields.Add( line.Substring( i, nextComma - i ).Trim() );
                            i = nextComma + 1;
                        }
                    }
                }

                rows.Add( fields );
            }

            return rows;
        }

        /// <summary>
        /// Binds the mapping repeater and name-column dropdowns.
        /// </summary>
        private void BindMappingControls( List<string> savedColumns = null, List<int?> savedGroups = null )
        {
            // Restore base parent group selection
            if ( BaseParentGroupId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var baseGroup = new GroupService( rockContext ).Get( BaseParentGroupId.Value );
                    gpBasePlacementGroup.SetValue( baseGroup );
                }
            }

            // Populate identity column dropdowns
            var selectedFirst = ddlFirstNameCol.SelectedValue;
            var selectedLast = ddlLastNameCol.SelectedValue;

            ddlFirstNameCol.Items.Clear();
            ddlLastNameCol.Items.Clear();
            ddlFirstNameCol.Items.Add( new ListItem( "", "" ) );
            ddlLastNameCol.Items.Add( new ListItem( "", "" ) );

            foreach ( var header in CsvHeaders )
            {
                ddlFirstNameCol.Items.Add( new ListItem( header, header ) );
                ddlLastNameCol.Items.Add( new ListItem( header, header ) );
            }

            // Auto-select common column names if not yet selected
            if ( string.IsNullOrWhiteSpace( selectedFirst ) )
            {
                selectedFirst = CsvHeaders.FirstOrDefault( h =>
                    h.Equals( "FirstName", StringComparison.OrdinalIgnoreCase ) ||
                    h.Equals( "First Name", StringComparison.OrdinalIgnoreCase ) ||
                    h.Equals( "First", StringComparison.OrdinalIgnoreCase ) );
            }

            if ( string.IsNullOrWhiteSpace( selectedLast ) )
            {
                selectedLast = CsvHeaders.FirstOrDefault( h =>
                    h.Equals( "LastName", StringComparison.OrdinalIgnoreCase ) ||
                    h.Equals( "Last Name", StringComparison.OrdinalIgnoreCase ) ||
                    h.Equals( "Last", StringComparison.OrdinalIgnoreCase ) );
            }

            ddlFirstNameCol.SetValue( selectedFirst );
            ddlLastNameCol.SetValue( selectedLast );

            // Build mapping placeholder list for repeater binding
            var mappingData = new List<int>();
            for ( int i = 0; i < MappingCount; i++ )
            {
                mappingData.Add( i );
            }

            rptMappings.DataSource = mappingData;
            rptMappings.DataBind();

            // Restore saved selections after databind
            if ( savedColumns != null && savedGroups != null )
            {
                RestoreMappingSelections( savedColumns, savedGroups );
            }
        }

        /// <summary>
        /// Saves the current mapping dropdown selections to ViewState so they survive repeater rebinds.
        /// </summary>
        private void SaveMappingSelections()
        {
            var columns = new List<string>();
            var groups = new List<int?>();

            foreach ( RepeaterItem item in rptMappings.Items )
            {
                var ddl = item.FindControl( "ddlCsvColumn" ) as RockDropDownList;
                columns.Add( ddl != null ? ddl.SelectedValue : "" );

                if ( BaseParentGroupId.HasValue )
                {
                    // Using dropdown
                    var ddlGroup = item.FindControl( "ddlParentGroupChild" ) as RockDropDownList;
                    groups.Add( ddlGroup != null ? ddlGroup.SelectedValueAsInt() : null );
                }
                else
                {
                    // Using group picker
                    var gp = item.FindControl( "gpParentGroup" ) as GroupPicker;
                    groups.Add( gp != null ? gp.GroupId : null );
                }
            }

            ViewState["SavedMappingColumns"] = columns;
            ViewState["SavedMappingGroups"] = groups;
        }

        /// <summary>
        /// Restores mapping selections after a repeater rebind.
        /// </summary>
        private void RestoreMappingSelections( List<string> columns, List<int?> groups )
        {
            for ( int i = 0; i < rptMappings.Items.Count && i < columns.Count; i++ )
            {
                var ddl = rptMappings.Items[i].FindControl( "ddlCsvColumn" ) as RockDropDownList;
                if ( ddl != null )
                {
                    ddl.SetValue( columns[i] );
                }

                if ( i < groups.Count && groups[i].HasValue )
                {
                    if ( BaseParentGroupId.HasValue )
                    {
                        // Restore dropdown selection
                        var ddlGroup = rptMappings.Items[i].FindControl( "ddlParentGroupChild" ) as RockDropDownList;
                        if ( ddlGroup != null )
                        {
                            ddlGroup.SetValue( groups[i].Value );
                        }
                    }
                    else
                    {
                        // Restore group picker selection
                        var gp = rptMappings.Items[i].FindControl( "gpParentGroup" ) as GroupPicker;
                        if ( gp != null )
                        {
                            using ( var rockContext = new RockContext() )
                            {
                                var group = new GroupService( rockContext ).Get( groups[i].Value );
                                gp.SetValue( group );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads the current column-to-group mappings from the UI.
        /// </summary>
        private List<PlacementMapping> GetMappingsFromUI()
        {
            var mappings = new List<PlacementMapping>();

            foreach ( RepeaterItem item in rptMappings.Items )
            {
                var ddl = item.FindControl( "ddlCsvColumn" ) as RockDropDownList;

                int? parentGroupId = null;

                if ( BaseParentGroupId.HasValue )
                {
                    // Using dropdown for parent group selection
                    var ddlGroup = item.FindControl( "ddlParentGroupChild" ) as RockDropDownList;
                    parentGroupId = ddlGroup != null ? ddlGroup.SelectedValueAsInt() : null;
                }
                else
                {
                    // Using group picker for parent group selection
                    var gp = item.FindControl( "gpParentGroup" ) as GroupPicker;
                    parentGroupId = gp != null ? gp.GroupId : null;
                }

                if ( ddl != null &&
                     !string.IsNullOrWhiteSpace( ddl.SelectedValue ) &&
                     parentGroupId.HasValue )
                {
                    mappings.Add( new PlacementMapping
                    {
                        CsvColumnName = ddl.SelectedValue,
                        ParentGroupId = parentGroupId.Value
                    } );
                }
            }

            return mappings;
        }

        /// <summary>
        /// Builds the preview data by matching CSV rows to registrants and resolving placement groups.
        /// Rows belonging to names that appear more than once in the CSV are flagged as duplicates.
        /// </summary>
        private List<PreviewRow> BuildPreviewData( string firstNameCol, string lastNameCol, List<PlacementMapping> mappings )
        {
            var previewRows = new List<PreviewRow>();
            int firstNameIdx = CsvHeaders.IndexOf( firstNameCol );
            int lastNameIdx = CsvHeaders.IndexOf( lastNameCol );

            if ( firstNameIdx < 0 || lastNameIdx < 0 )
            {
                return previewRows;
            }

            // Pre-scan: find every full name that appears more than once in the CSV.
            // ALL rows for that name will be skipped, not just the duplicates.
            var nameCount = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
            foreach ( var row in CsvRows )
            {
                string fn = GetCellValue( row, firstNameIdx );
                string ln = GetCellValue( row, lastNameIdx );
                string fullName = string.Format( "{0} {1}", fn, ln ).Trim();
                if ( nameCount.ContainsKey( fullName ) )
                {
                    nameCount[fullName]++;
                }
                else
                {
                    nameCount[fullName] = 1;
                }
            }

            var duplicateNames = new HashSet<string>(
                nameCount.Where( kv => kv.Value > 1 ).Select( kv => kv.Key ),
                StringComparer.OrdinalIgnoreCase );

            using ( var rockContext = new RockContext() )
            {
                var registrants = LoadRegistrants( rockContext );

                var childGroupsByParent = new Dictionary<int, List<Group>>();
                var groupService = new GroupService( rockContext );

                foreach ( var mapping in mappings )
                {
                    if ( !childGroupsByParent.ContainsKey( mapping.ParentGroupId ) )
                    {
                        childGroupsByParent[mapping.ParentGroupId] = groupService.Queryable()
                            .AsNoTracking()
                            .Where( g => g.ParentGroupId == mapping.ParentGroupId && g.IsActive && !g.IsArchived )
                            .ToList();
                    }
                }

                for ( int rowIdx = 0; rowIdx < CsvRows.Count; rowIdx++ )
                {
                    var row = CsvRows[rowIdx];
                    string firstName = GetCellValue( row, firstNameIdx );
                    string lastName = GetCellValue( row, lastNameIdx );
                    string csvFullName = string.Format( "{0} {1}", firstName, lastName ).Trim();

                    var preview = new PreviewRow
                    {
                        RowIndex = rowIdx,
                        CsvName = csvFullName
                    };

                    // If the name appears more than once, flag all rows for it and move on.
                    if ( duplicateNames.Contains( csvFullName ) )
                    {
                        preview.MatchedPerson = "DUPLICATE";
                        preview.Status = "Duplicate - will be skipped";
                        preview.HasError = true;
                        preview.PlacementSummary = string.Format(
                            "'{0}' appears {1} times in the CSV. All rows for this name will be skipped.",
                            csvFullName, nameCount[csvFullName] );
                        previewRows.Add( preview );
                        continue;
                    }

                    var matchedPerson = FindRegistrant( firstName, lastName, registrants );
                    if ( matchedPerson == null )
                    {
                        preview.MatchedPerson = "NOT FOUND";
                        preview.Status = "Error";
                        preview.HasError = true;
                    }
                    else
                    {
                        preview.MatchedPerson = string.Format( "{0} (ID: {1})", matchedPerson.FullName, matchedPerson.Id );
                        preview.PersonId = matchedPerson.Id;
                    }

                    var placementParts = new List<string>();
                    foreach ( var mapping in mappings )
                    {
                        int colIdx = CsvHeaders.IndexOf( mapping.CsvColumnName );
                        string cellValue = GetCellValue( row, colIdx );

                        if ( string.IsNullOrWhiteSpace( cellValue ) )
                        {
                            continue;
                        }

                        var childGroups = childGroupsByParent.ContainsKey( mapping.ParentGroupId )
                            ? childGroupsByParent[mapping.ParentGroupId]
                            : new List<Group>();

                        var targetGroup = childGroups.FirstOrDefault( g =>
                            g.Name.Equals( cellValue, StringComparison.OrdinalIgnoreCase ) );

                        if ( targetGroup != null )
                        {
                            placementParts.Add( string.Format( "{0}: {1} ✓", mapping.CsvColumnName, cellValue ) );
                        }
                        else
                        {
                            placementParts.Add( string.Format( "{0}: {1} ✗ (group not found)", mapping.CsvColumnName, cellValue ) );
                            preview.HasError = true;
                            preview.Status = "Error";
                        }
                    }

                    preview.PlacementSummary = string.Join( " | ", placementParts );
                    if ( string.IsNullOrWhiteSpace( preview.Status ) )
                    {
                        preview.Status = placementParts.Any() ? "Ready" : "No placements";
                    }

                    previewRows.Add( preview );
                }
            }

            return previewRows;
        }

        /// <summary>
        /// Binds the preview grid and surfaces warnings for duplicates and other errors.
        /// </summary>
        private void BindPreviewGrid( List<PreviewRow> previewData )
        {
            gPreview.DataSource = previewData;
            gPreview.DataBind();

            // Collect unique duplicate names to call out explicitly.
            var duplicateNames = previewData
                .Where( r => r.Status != null && r.Status.StartsWith( "Duplicate" ) )
                .Select( r => r.CsvName )
                .Distinct( StringComparer.OrdinalIgnoreCase )
                .OrderBy( n => n )
                .ToList();

            if ( duplicateNames.Any() )
            {
                nbDuplicates.Text = string.Format(
                    "<strong>Duplicate names detected.</strong> The following name(s) appear more than once in the CSV. " +
                    "<strong>All rows for these names will be skipped</strong> when you process placements. " +
                    "Remove the duplicates from your CSV and re-upload if you want them processed.<br/><br/>{0}",
                    string.Join( "<br/>", duplicateNames.Select( n => "&bull; " + System.Web.HttpUtility.HtmlEncode( n ) ) ) );
                nbDuplicates.Visible = true;
            }
            else
            {
                nbDuplicates.Visible = false;
            }

            int errorCount = previewData.Count( r => r.HasError );
            int readyCount = previewData.Count( r => !r.HasError && r.Status == "Ready" );

            if ( errorCount > 0 )
            {
                ShowWarning( string.Format( "{0} row(s) have errors or are duplicates and will be skipped. {1} row(s) are ready to process.", errorCount, readyCount ) );
            }
            else
            {
                ShowInfo( string.Format( "{0} row(s) ready to process.", readyCount ) );
            }
        }

        /// <summary>
        /// Processes the actual group member placements and collects detailed per-cell results.
        /// All rows for any name that appears more than once in the CSV are skipped entirely.
        /// Existing memberships are pre-loaded in bulk to avoid per-row database queries.
        /// </summary>
        private void ProcessPlacements( string firstNameCol, string lastNameCol, List<PlacementMapping> mappings )
        {
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var resultRows = new List<ResultRow>();

            int firstNameIdx = CsvHeaders.IndexOf( firstNameCol );
            int lastNameIdx = CsvHeaders.IndexOf( lastNameCol );

            int batchSize = GetAttributeValue( AttributeKey.BatchSize ).AsIntegerOrNull() ?? 50;
            var status = ( GroupMemberStatus ) GetAttributeValue( AttributeKey.DefaultGroupMemberStatus ).AsInteger();

            // Pre-scan: identify every name that appears more than once so ALL their rows are skipped.
            var nameCount = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
            foreach ( var row in CsvRows )
            {
                string fn = GetCellValue( row, firstNameIdx );
                string ln = GetCellValue( row, lastNameIdx );
                string fullName = string.Format( "{0} {1}", fn, ln ).Trim();
                if ( nameCount.ContainsKey( fullName ) )
                {
                    nameCount[fullName]++;
                }
                else
                {
                    nameCount[fullName] = 1;
                }
            }

            var duplicateNames = new HashSet<string>(
                nameCount.Where( kv => kv.Value > 1 ).Select( kv => kv.Key ),
                StringComparer.OrdinalIgnoreCase );

            using ( var rockContext = new RockContext() )
            {
                var registrants = LoadRegistrants( rockContext );
                var groupService = new GroupService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );

                // Pre-load all child groups for every mapping.
                var childGroupsByParent = new Dictionary<int, List<Group>>();
                foreach ( var mapping in mappings )
                {
                    if ( !childGroupsByParent.ContainsKey( mapping.ParentGroupId ) )
                    {
                        childGroupsByParent[mapping.ParentGroupId] = groupService.Queryable()
                            .Where( g => g.ParentGroupId == mapping.ParentGroupId && g.IsActive && !g.IsArchived )
                            .ToList();
                    }
                }

                // Collect the full set of target group IDs and registrant person IDs so we can
                // load all potentially relevant GroupMember records in a single query.
                var allTargetGroupIds = childGroupsByParent.Values
                    .SelectMany( groups => groups )
                    .Select( g => g.Id )
                    .Distinct()
                    .ToList();

                var allPersonIds = registrants
                    .Where( r => r.PersonAlias != null )
                    .Select( r => r.PersonAlias.PersonId )
                    .Distinct()
                    .ToList();

                // Single bulk membership query instead of one query per person per group.
                // Keyed by "personId_groupId" for O(1) lookups inside the loop.
                var existingMembershipLookup = new Dictionary<string, GroupMember>();
                if ( allTargetGroupIds.Any() && allPersonIds.Any() )
                {
                    var existingMembers = groupMemberService.Queryable()
                        .Where( gm => allTargetGroupIds.Contains( gm.GroupId )
                                   && allPersonIds.Contains( gm.PersonId ) )
                        .ToList();

                    foreach ( var gm in existingMembers )
                    {
                        string key = string.Format( "{0}_{1}", gm.PersonId, gm.GroupId );
                        // Keep the first record found if duplicates exist (matches prior FirstOrDefault behaviour).
                        if ( !existingMembershipLookup.ContainsKey( key ) )
                        {
                            existingMembershipLookup[key] = gm;
                        }
                    }
                }

                int pendingSaves = 0;

                for ( int rowIdx = 0; rowIdx < CsvRows.Count; rowIdx++ )
                {
                    var row = CsvRows[rowIdx];
                    string firstName = GetCellValue( row, firstNameIdx );
                    string lastName = GetCellValue( row, lastNameIdx );
                    string csvFullName = string.Format( "{0} {1}", firstName, lastName ).Trim();

                    var resultRow = new ResultRow
                    {
                        CsvRowNumber = rowIdx + 2,
                        CamperName = csvFullName,
                        Placements = new List<PlacementResult>()
                    };

                    // Skip every row for a name that appeared more than once.
                    if ( duplicateNames.Contains( csvFullName ) )
                    {
                        resultRow.PersonError = string.Format(
                            "'{0}' appears {1} times in the CSV. All rows for this name are skipped.",
                            csvFullName, nameCount[csvFullName] );

                        foreach ( var mapping in mappings )
                        {
                            int colIdx = CsvHeaders.IndexOf( mapping.CsvColumnName );
                            resultRow.Placements.Add( new PlacementResult
                            {
                                ColumnName = mapping.CsvColumnName,
                                CsvValue = GetCellValue( row, colIdx ),
                                Outcome = PlacementOutcome.Error,
                                Message = "Skipped — duplicate name in CSV"
                            } );
                            errorCount++;
                        }

                        resultRows.Add( resultRow );
                        continue;
                    }

                    var person = FindRegistrant( firstName, lastName, registrants );
                    if ( person == null )
                    {
                        resultRow.PersonError = string.Format( "Could not match '{0} {1}' to a registrant.", firstName, lastName );

                        foreach ( var mapping in mappings )
                        {
                            int colIdx = CsvHeaders.IndexOf( mapping.CsvColumnName );
                            resultRow.Placements.Add( new PlacementResult
                            {
                                ColumnName = mapping.CsvColumnName,
                                CsvValue = GetCellValue( row, colIdx ),
                                Outcome = PlacementOutcome.Error,
                                Message = "Person not found"
                            } );
                            errorCount++;
                        }

                        resultRows.Add( resultRow );
                        continue;
                    }

                    resultRow.MatchedPersonName = person.FullName;

                    foreach ( var mapping in mappings )
                    {
                        int colIdx = CsvHeaders.IndexOf( mapping.CsvColumnName );
                        string cellValue = GetCellValue( row, colIdx );

                        var placementResult = new PlacementResult
                        {
                            ColumnName = mapping.CsvColumnName,
                            CsvValue = cellValue
                        };

                        if ( string.IsNullOrWhiteSpace( cellValue ) )
                        {
                            placementResult.Outcome = PlacementOutcome.Empty;
                            placementResult.Message = "No value in CSV";
                            resultRow.Placements.Add( placementResult );
                            continue;
                        }

                        var childGroups = childGroupsByParent.ContainsKey( mapping.ParentGroupId )
                            ? childGroupsByParent[mapping.ParentGroupId]
                            : new List<Group>();

                        var targetGroup = childGroups.FirstOrDefault( g =>
                            g.Name.Equals( cellValue, StringComparison.OrdinalIgnoreCase ) );

                        if ( targetGroup == null )
                        {
                            placementResult.Outcome = PlacementOutcome.Error;
                            placementResult.Message = string.Format( "Group '{0}' not found under parent group ID {1}", cellValue, mapping.ParentGroupId );
                            errorCount++;
                            resultRow.Placements.Add( placementResult );
                            continue;
                        }

                        // In-memory lookup — no database query.
                        string membershipKey = string.Format( "{0}_{1}", person.Id, targetGroup.Id );
                        GroupMember existingMember = null;
                        existingMembershipLookup.TryGetValue( membershipKey, out existingMember );

                        if ( existingMember != null )
                        {
                            if ( existingMember.IsArchived )
                            {
                                existingMember.IsArchived = false;
                                existingMember.GroupMemberStatus = status;
                                placementResult.Outcome = PlacementOutcome.Success;
                                placementResult.Message = string.Format( "Restored archived membership in '{0}'", targetGroup.Name );
                                successCount++;
                                pendingSaves++;
                            }
                            else
                            {
                                placementResult.Outcome = PlacementOutcome.Skipped;
                                placementResult.Message = string.Format( "Already a member of '{0}'", targetGroup.Name );
                                skippedCount++;
                            }

                            resultRow.Placements.Add( placementResult );
                            continue;
                        }

                        var groupTypeCache = GroupTypeCache.Get( targetGroup.GroupTypeId );
                        int? defaultRoleId = groupTypeCache?.DefaultGroupRoleId;

                        if ( !defaultRoleId.HasValue )
                        {
                            placementResult.Outcome = PlacementOutcome.Error;
                            placementResult.Message = string.Format( "Group '{0}' has no default group role configured", targetGroup.Name );
                            errorCount++;
                            resultRow.Placements.Add( placementResult );
                            continue;
                        }

                        var groupMember = new GroupMember
                        {
                            PersonId = person.Id,
                            GroupId = targetGroup.Id,
                            GroupRoleId = defaultRoleId.Value,
                            GroupMemberStatus = status
                        };

                        if ( groupMember.IsValidGroupMember( rockContext ) )
                        {
                            groupMemberService.Add( groupMember );

                            // Add to the in-memory lookup so subsequent rows in this same
                            // import don't try to add the same person to the same group again.
                            existingMembershipLookup[membershipKey] = groupMember;

                            placementResult.Outcome = PlacementOutcome.Success;
                            placementResult.Message = string.Format( "Added to '{0}'", targetGroup.Name );
                            successCount++;
                            pendingSaves++;
                        }
                        else
                        {
                            placementResult.Outcome = PlacementOutcome.Error;
                            placementResult.Message = string.Format( "Validation failed for '{0}': {1}",
                                targetGroup.Name,
                                string.Join( "; ", groupMember.ValidationResults.Select( v => v.ErrorMessage ) ) );
                            errorCount++;
                        }

                        resultRow.Placements.Add( placementResult );

                        if ( pendingSaves >= batchSize )
                        {
                            rockContext.SaveChanges();
                            pendingSaves = 0;
                        }
                    }

                    resultRows.Add( resultRow );
                }

                if ( pendingSaves > 0 )
                {
                    rockContext.SaveChanges();
                }
            }

            lSuccessCount.Text = successCount.ToString();
            lSkippedCount.Text = skippedCount.ToString();
            lErrorCount.Text = errorCount.ToString();

            lResultsTable.Text = RenderResultsTable( resultRows, mappings );
        }

        /// <summary>
        /// Renders an HTML table showing per-row, per-column placement outcomes with color-coded labels.
        /// </summary>
        private string RenderResultsTable( List<ResultRow> resultRows, List<PlacementMapping> mappings )
        {
            var sb = new StringBuilder();

            sb.Append( "<div class='table-responsive'>" );
            sb.Append( "<table class='table table-bordered table-striped table-condensed'>" );

            // Header row
            sb.Append( "<thead><tr>" );
            sb.Append( "<th>Row</th>" );
            sb.Append( "<th>Camper</th>" );
            sb.Append( "<th>Matched Person</th>" );
            foreach ( var mapping in mappings )
            {
                sb.AppendFormat( "<th>{0}</th>", System.Web.HttpUtility.HtmlEncode( mapping.CsvColumnName ) );
            }
            sb.Append( "</tr></thead>" );

            // Data rows
            sb.Append( "<tbody>" );
            foreach ( var row in resultRows )
            {
                sb.Append( "<tr>" );
                sb.AppendFormat( "<td>{0}</td>", row.CsvRowNumber );
                sb.AppendFormat( "<td>{0}</td>", System.Web.HttpUtility.HtmlEncode( row.CamperName ) );

                // Matched person column
                if ( !string.IsNullOrWhiteSpace( row.PersonError ) )
                {
                    sb.AppendFormat( "<td><span class='label label-danger'>NOT FOUND</span><br/><small class='text-danger'>{0}</small></td>",
                        System.Web.HttpUtility.HtmlEncode( row.PersonError ) );
                }
                else
                {
                    sb.AppendFormat( "<td>{0}</td>", System.Web.HttpUtility.HtmlEncode( row.MatchedPersonName ) );
                }

                // One cell per mapping column
                foreach ( var mapping in mappings )
                {
                    var placement = row.Placements.FirstOrDefault( p => p.ColumnName == mapping.CsvColumnName );
                    if ( placement == null )
                    {
                        sb.Append( "<td></td>" );
                        continue;
                    }

                    string labelClass;
                    string labelText;
                    switch ( placement.Outcome )
                    {
                        case PlacementOutcome.Success:
                            labelClass = "label-success";
                            labelText = "Added";
                            break;
                        case PlacementOutcome.Skipped:
                            labelClass = "label-info";
                            labelText = "Skipped";
                            break;
                        case PlacementOutcome.Error:
                            labelClass = "label-danger";
                            labelText = "Error";
                            break;
                        default:
                            labelClass = "label-default";
                            labelText = "Empty";
                            break;
                    }

                    string csvValueEncoded = System.Web.HttpUtility.HtmlEncode( placement.CsvValue );
                    string messageEncoded = System.Web.HttpUtility.HtmlEncode( placement.Message );

                    sb.AppendFormat( "<td>{0} <span class='label {1}'>{2}</span><br/><small class='text-muted'>{3}</small></td>",
                        csvValueEncoded, labelClass, labelText, messageEncoded );
                }

                sb.Append( "</tr>" );
            }

            sb.Append( "</tbody></table></div>" );

            return sb.ToString();
        }

        /// <summary>
        /// Loads all registrants for the selected registration instance with their Person records.
        /// </summary>
        private List<RegistrationRegistrant> LoadRegistrants( RockContext rockContext )
        {
            if ( !SelectedRegistrationInstanceId.HasValue )
            {
                return new List<RegistrationRegistrant>();
            }

            return new RegistrationRegistrantService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( r => r.PersonAlias.Person )
                .Where( r => r.Registration.RegistrationInstanceId == SelectedRegistrationInstanceId.Value )
                .ToList();
        }

        /// <summary>
        /// Finds a matching registrant person by first/last name (or nickname).
        /// Returns null if zero or multiple matches are found.
        /// </summary>
        private Person FindRegistrant( string firstName, string lastName, List<RegistrationRegistrant> registrants )
        {
            if ( string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName ) )
            {
                return null;
            }

            var people = registrants
                .Where( r => r.PersonAlias != null && r.PersonAlias.Person != null )
                .Select( r => r.PersonAlias.Person )
                .Where( p =>
                    ( p.FirstName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) ||
                      p.NickName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) ) &&
                    p.LastName.Equals( lastName, StringComparison.OrdinalIgnoreCase ) )
                .GroupBy( p => p.Id )
                .Select( g => g.First() )
                .ToList();

            if ( people.Count == 1 )
            {
                return people.First();
            }

            return null;
        }

        /// <summary>
        /// Safely retrieves a cell value from a CSV row by column index.
        /// </summary>
        private string GetCellValue( List<string> row, int columnIndex )
        {
            if ( columnIndex >= 0 && columnIndex < row.Count )
            {
                return row[columnIndex].Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the active wizard panel and hides all others.
        /// </summary>
        private void SetActivePanel( Panel activePanel )
        {
            pnlSelectInstance.Visible = activePanel == pnlSelectInstance;
            pnlUpload.Visible = activePanel == pnlUpload;
            pnlMapping.Visible = activePanel == pnlMapping;
            pnlPreview.Visible = activePanel == pnlPreview;
            pnlResults.Visible = activePanel == pnlResults;
        }

        private void ShowWarning( string message )
        {
            nbWarning.Text = message;
            nbWarning.Visible = true;
        }

        private void ShowInfo( string message )
        {
            nbInfo.Text = message;
            nbInfo.Visible = true;
        }

        private void ShowSuccess( string message )
        {
            nbSuccess.Text = message;
            nbSuccess.Visible = true;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Represents a mapping from a CSV column to a parent group whose children are valid placement targets.
        /// </summary>
        [Serializable]
        private class PlacementMapping
        {
            public string CsvColumnName { get; set; }
            public int ParentGroupId { get; set; }
        }

        /// <summary>
        /// Represents a single row of preview data for the validation grid.
        /// </summary>
        private class PreviewRow
        {
            public int RowIndex { get; set; }
            public string CsvName { get; set; }
            public string MatchedPerson { get; set; }
            public int? PersonId { get; set; }
            public string PlacementSummary { get; set; }
            public string Status { get; set; }
            public bool HasError { get; set; }
        }

        /// <summary>
        /// The outcome of a single placement cell.
        /// </summary>
        private enum PlacementOutcome
        {
            Empty = 0,
            Success = 1,
            Skipped = 2,
            Error = 3
        }

        /// <summary>
        /// Tracks the result of processing a single placement (one cell in one row).
        /// </summary>
        private class PlacementResult
        {
            public string ColumnName { get; set; }
            public string CsvValue { get; set; }
            public PlacementOutcome Outcome { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Tracks all placement results for a single CSV row.
        /// </summary>
        private class ResultRow
        {
            public int CsvRowNumber { get; set; }
            public string CamperName { get; set; }
            public string MatchedPersonName { get; set; }
            public string PersonError { get; set; }
            public List<PlacementResult> Placements { get; set; }
        }

        #endregion
    }
}