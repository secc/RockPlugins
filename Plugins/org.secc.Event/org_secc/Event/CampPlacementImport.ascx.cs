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
using System.Web.UI;
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
            }
        }

        protected void btnAddMapping_Click( object sender, EventArgs e )
        {
            SaveMappingSelections();
            MappingCount++;
            BindMappingControls();
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
            BindMappingControls();
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
            fuCsvFile.BinaryFileId = null;

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
                var gp = item.FindControl( "gpParentGroup" ) as GroupPicker;

                columns.Add( ddl != null ? ddl.SelectedValue : "" );
                groups.Add( gp != null ? gp.GroupId : null );
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
                var gp = rptMappings.Items[i].FindControl( "gpParentGroup" ) as GroupPicker;

                if ( ddl != null )
                {
                    ddl.SetValue( columns[i] );
                }

                if ( gp != null && i < groups.Count && groups[i].HasValue )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var group = new GroupService( rockContext ).Get( groups[i].Value );
                        gp.SetValue( group );
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
                var gp = item.FindControl( "gpParentGroup" ) as GroupPicker;

                if ( ddl != null && gp != null &&
                    !string.IsNullOrWhiteSpace( ddl.SelectedValue ) &&
                    gp.GroupId.HasValue )
                {
                    mappings.Add( new PlacementMapping
                    {
                        CsvColumnName = ddl.SelectedValue,
                        ParentGroupId = gp.GroupId.Value
                    } );
                }
            }

            return mappings;
        }

        /// <summary>
        /// Builds the preview data by matching CSV rows to registrants and resolving placement groups.
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

            using ( var rockContext = new RockContext() )
            {
                // Load all registrants for this instance
                var registrants = LoadRegistrants( rockContext );

                // Pre-load all child groups for each parent group in the mappings
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
                    string firstName = rowIdx < row.Count ? GetCellValue( row, firstNameIdx ) : "";
                    string lastName = rowIdx < row.Count ? GetCellValue( row, lastNameIdx ) : "";

                    var preview = new PreviewRow
                    {
                        RowIndex = rowIdx,
                        CsvName = string.Format( "{0} {1}", firstName, lastName ).Trim()
                    };

                    // Match person
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

                    // Resolve each placement
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
        /// Binds the preview grid.
        /// </summary>
        private void BindPreviewGrid( List<PreviewRow> previewData )
        {
            gPreview.DataSource = previewData;
            gPreview.DataBind();

            int errorCount = previewData.Count( r => r.HasError );
            int readyCount = previewData.Count( r => !r.HasError && r.Status == "Ready" );

            if ( errorCount > 0 )
            {
                ShowWarning( string.Format( "{0} row(s) have errors and will be skipped. {1} row(s) are ready to process.", errorCount, readyCount ) );
            }
            else
            {
                ShowInfo( string.Format( "{0} row(s) ready to process.", readyCount ) );
            }
        }

        /// <summary>
        /// Processes the actual group member placements.
        /// Uses GroupMemberService.Add() + SaveChanges() to ensure workflow triggers fire.
        /// </summary>
        private void ProcessPlacements( string firstNameCol, string lastNameCol, List<PlacementMapping> mappings )
        {
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var errorMessages = new List<string>();

            int firstNameIdx = CsvHeaders.IndexOf( firstNameCol );
            int lastNameIdx = CsvHeaders.IndexOf( lastNameCol );

            int batchSize = GetAttributeValue( AttributeKey.BatchSize ).AsIntegerOrNull() ?? 50;
            var status = ( GroupMemberStatus ) GetAttributeValue( AttributeKey.DefaultGroupMemberStatus ).AsInteger();

            using ( var rockContext = new RockContext() )
            {
                var registrants = LoadRegistrants( rockContext );
                var groupService = new GroupService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );

                // Pre-load child groups
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

                int pendingSaves = 0;

                for ( int rowIdx = 0; rowIdx < CsvRows.Count; rowIdx++ )
                {
                    var row = CsvRows[rowIdx];
                    string firstName = GetCellValue( row, firstNameIdx );
                    string lastName = GetCellValue( row, lastNameIdx );

                    var person = FindRegistrant( firstName, lastName, registrants );
                    if ( person == null )
                    {
                        errorCount++;
                        errorMessages.Add( string.Format( "Row {0}: Could not match '{1} {2}' to a registrant.", rowIdx + 2, firstName, lastName ) );
                        continue;
                    }

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

                        if ( targetGroup == null )
                        {
                            errorCount++;
                            errorMessages.Add( string.Format( "Row {0}: Group '{1}' not found under parent group ID {2}.", rowIdx + 2, cellValue, mapping.ParentGroupId ) );
                            continue;
                        }

                        // Check for existing membership
                        var existingMember = groupMemberService.GetByGroupIdAndPersonId( targetGroup.Id, person.Id ).FirstOrDefault();
                        if ( existingMember != null )
                        {
                            if ( existingMember.IsArchived )
                            {
                                existingMember.IsArchived = false;
                                existingMember.GroupMemberStatus = status;
                                successCount++;
                                pendingSaves++;
                            }
                            else
                            {
                                skippedCount++;
                            }

                            continue;
                        }

                        // Determine the default role for the group
                        var groupTypeCache = GroupTypeCache.Get( targetGroup.GroupTypeId );
                        int? defaultRoleId = groupTypeCache?.DefaultGroupRoleId;

                        if ( !defaultRoleId.HasValue )
                        {
                            errorCount++;
                            errorMessages.Add( string.Format( "Row {0}: Group '{1}' has no default group role configured.", rowIdx + 2, targetGroup.Name ) );
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
                            successCount++;
                            pendingSaves++;
                        }
                        else
                        {
                            errorCount++;
                            errorMessages.Add( string.Format( "Row {0}: Validation failed for '{1}' in group '{2}': {3}",
                                rowIdx + 2,
                                person.FullName,
                                targetGroup.Name,
                                string.Join( "; ", groupMember.ValidationResults.Select( v => v.ErrorMessage ) ) ) );
                        }

                        // Batch save to balance performance with trigger reliability
                        if ( pendingSaves >= batchSize )
                        {
                            rockContext.SaveChanges();
                            pendingSaves = 0;
                        }
                    }
                }

                // Final save for any remaining records
                if ( pendingSaves > 0 )
                {
                    rockContext.SaveChanges();
                }
            }

            // Display results
            lSuccessCount.Text = successCount.ToString();
            lSkippedCount.Text = skippedCount.ToString();
            lErrorCount.Text = errorCount.ToString();

            if ( errorMessages.Any() )
            {
                pnlErrorDetails.Visible = true;
                nbErrors.Text = string.Join( "<br/>", errorMessages );
            }
            else
            {
                pnlErrorDetails.Visible = false;
            }
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

        #endregion
    }
}