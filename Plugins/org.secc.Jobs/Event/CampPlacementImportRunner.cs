// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs.Event
{
    /// <summary>
    /// Processes a camp placement import run record.
    /// Loaded by <see cref="CampPlacementImportBackgroundJob"/>.
    /// All state comes from the run record row so this class has no
    /// dependency on the web request or ViewState.
    /// </summary>
    public static class CampPlacementImportRunner
    {
        // ─── Outcome enum ────────────────────────────────────────────────

        private enum PlacementOutcome { Empty = 0, Success = 1, Skipped = 2, Error = 3 }

        // ─── Internal result types ────────────────────────────────────────

        private class PlacementResult
        {
            public string ColumnName { get; set; }
            public string CsvValue { get; set; }
            public PlacementOutcome Outcome { get; set; }
            public string Message { get; set; }
        }

        private class ResultRow
        {
            public int CsvRowNumber { get; set; }
            public string CamperName { get; set; }
            public string MatchedPersonName { get; set; }
            public string PersonError { get; set; }
            public List<PlacementResult> Placements { get; set; }
        }

        // ─── Entry point ─────────────────────────────────────────────────

        /// <summary>
        /// Loads the run record identified by <paramref name="runId"/>,
        /// processes the import, writes progress updates back to the database
        /// every batch, and sets the final status when done.
        /// </summary>
        public static void Run( int runId )
        {
            // Mark the run as Running immediately so the UI can reflect that.
            UpdateRunStatus( runId, ImportRunStatus.Running, "Starting import…", 0, 0, 0 );

            // ── Load request ─────────────────────────────────────────────
            CampPlacementImportRequest request = LoadRequest( runId );

            if ( request == null )
            {
                MarkFailed( runId, "Could not load import request data from run record." );
                return;
            }

            if ( !request.RegistrationInstanceId.HasValue )
            {
                MarkFailed( runId, "No registration instance ID in request." );
                return;
            }

            if ( !request.BinaryFileId.HasValue )
            {
                MarkFailed( runId, "No uploaded file ID in request." );
                return;
            }

            // ── Parse CSV from the stored binary file ─────────────────────
            List<string> headers;
            List<List<string>> rows;

            try
            {
                ParseCsvFromBinaryFile( request.BinaryFileId.Value, out headers, out rows );
            }
            catch ( Exception ex )
            {
                MarkFailed( runId, "Failed to read CSV file: " + ex.Message );
                return;
            }

            if ( !headers.Any() || !rows.Any() )
            {
                MarkFailed( runId, "CSV file had no data rows." );
                return;
            }

            int totalRows = rows.Count;
            UpdateRunStatus( runId, ImportRunStatus.Running,
                string.Format( "Loaded {0} rows. Matching registrants…", totalRows ), 0, 0, totalRows );

            // ── Resolve column indexes ─────────────────────────────────────
            int firstNameIdx = headers.IndexOf( request.FirstNameCol );
            int lastNameIdx = headers.IndexOf( request.LastNameCol );

            if ( firstNameIdx < 0 || lastNameIdx < 0 )
            {
                MarkFailed( runId, "First Name or Last Name column not found in CSV headers." );
                return;
            }

            var status = ( GroupMemberStatus ) request.DefaultGroupMemberStatusValue;
            int batchSize = request.BatchSize > 0 ? request.BatchSize : 50;

            // ── Pre-scan for duplicate names ───────────────────────────────
            var nameCount = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
            foreach ( var row in rows )
            {
                string key = BuildFullName( GetCell( row, firstNameIdx ), GetCell( row, lastNameIdx ) );
                if ( nameCount.ContainsKey( key ) )
                    nameCount[key]++;
                else
                    nameCount[key] = 1;
            }

            var duplicateNames = new HashSet<string>(
                nameCount.Where( kv => kv.Value > 1 ).Select( kv => kv.Key ),
                StringComparer.OrdinalIgnoreCase );

            // ── Main processing ────────────────────────────────────────────
            int successCount = 0;
            int skippedCount = 0;
            int errorCount = 0;
            var resultRows = new List<ResultRow>();

            using ( var rockContext = new RockContext() )
            {
                // Load every registrant for the instance once.
                var registrants = LoadRegistrants( rockContext, request.RegistrationInstanceId.Value );

                var groupService = new GroupService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );

                // Pre-load child groups for every mapping.
                var childGroupsByParent = new Dictionary<int, List<Group>>();
                foreach ( var mapping in request.Mappings )
                {
                    if ( !childGroupsByParent.ContainsKey( mapping.ParentGroupId ) )
                    {
                        childGroupsByParent[mapping.ParentGroupId] = groupService.Queryable()
                            .Where( g => g.ParentGroupId == mapping.ParentGroupId
                                      && g.IsActive
                                      && !g.IsArchived )
                            .ToList();
                    }
                }

                // Bulk-load all existing memberships we might touch in one query.
                var allTargetGroupIds = childGroupsByParent.Values
                    .SelectMany( g => g )
                    .Select( g => g.Id )
                    .Distinct()
                    .ToList();

                var allPersonIds = registrants
                    .Where( r => r.PersonAlias != null )
                    .Select( r => r.PersonAlias.PersonId )
                    .Distinct()
                    .ToList();

                var existingMembershipLookup = new Dictionary<string, GroupMember>();
                if ( allTargetGroupIds.Any() && allPersonIds.Any() )
                {
                    var existingMembers = groupMemberService.Queryable()
                        .Where( gm => allTargetGroupIds.Contains( gm.GroupId )
                                   && allPersonIds.Contains( gm.PersonId ) )
                        .ToList();

                    foreach ( var gm in existingMembers )
                    {
                        string key = MembershipKey( gm.PersonId, gm.GroupId );
                        if ( !existingMembershipLookup.ContainsKey( key ) )
                            existingMembershipLookup[key] = gm;
                    }
                }

                int pendingSaves = 0;
                int processedRows = 0;

                for ( int rowIdx = 0; rowIdx < rows.Count; rowIdx++ )
                {
                    var row = rows[rowIdx];
                    string firstName = GetCell( row, firstNameIdx );
                    string lastName = GetCell( row, lastNameIdx );
                    string csvFullName = BuildFullName( firstName, lastName );

                    var resultRow = new ResultRow
                    {
                        CsvRowNumber = rowIdx + 2, // +2: 1-based + header row
                        CamperName = csvFullName,
                        Placements = new List<PlacementResult>()
                    };

                    // ── Duplicate name — skip all rows for this name ──────
                    if ( duplicateNames.Contains( csvFullName ) )
                    {
                        resultRow.PersonError = string.Format(
                            "'{0}' appears {1} times in the CSV. All rows for this name are skipped.",
                            csvFullName, nameCount[csvFullName] );

                        foreach ( var mapping in request.Mappings )
                        {
                            resultRow.Placements.Add( new PlacementResult
                            {
                                ColumnName = mapping.CsvColumnName,
                                CsvValue = GetCell( row, headers.IndexOf( mapping.CsvColumnName ) ),
                                Outcome = PlacementOutcome.Error,
                                Message = "Skipped — duplicate name in CSV"
                            } );
                            errorCount++;
                        }

                        resultRows.Add( resultRow );
                        processedRows++;
                        continue;
                    }

                    // ── Match person ──────────────────────────────────────
                    var person = FindRegistrant( firstName, lastName, registrants );

                    if ( person == null )
                    {
                        resultRow.PersonError = string.Format(
                            "Could not match '{0}' to a registrant.", csvFullName );

                        foreach ( var mapping in request.Mappings )
                        {
                            resultRow.Placements.Add( new PlacementResult
                            {
                                ColumnName = mapping.CsvColumnName,
                                CsvValue = GetCell( row, headers.IndexOf( mapping.CsvColumnName ) ),
                                Outcome = PlacementOutcome.Error,
                                Message = "Person not found"
                            } );
                            errorCount++;
                        }

                        resultRows.Add( resultRow );
                        processedRows++;
                        continue;
                    }

                    resultRow.MatchedPersonName = person.FullName;

                    // ── Process each mapping column for this row ──────────
                    foreach ( var mapping in request.Mappings )
                    {
                        int colIdx = headers.IndexOf( mapping.CsvColumnName );
                        string cellValue = GetCell( row, colIdx );

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
                            placementResult.Message = string.Format(
                                "Group '{0}' not found under parent group ID {1}",
                                cellValue, mapping.ParentGroupId );
                            errorCount++;
                            resultRow.Placements.Add( placementResult );
                            continue;
                        }

                        string membershipKey = MembershipKey( person.Id, targetGroup.Id );
                        GroupMember existingMember;
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
                        int? defaultRoleId = groupTypeCache != null ? groupTypeCache.DefaultGroupRoleId : ( int? ) null;

                        if ( !defaultRoleId.HasValue )
                        {
                            placementResult.Outcome = PlacementOutcome.Error;
                            placementResult.Message = string.Format(
                                "Group '{0}' has no default group role configured", targetGroup.Name );
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
                    }

                    resultRows.Add( resultRow );
                    processedRows++;

                    // ── Flush batch to database ──────────────────────────
                    if ( pendingSaves >= batchSize )
                    {
                        rockContext.SaveChanges();
                        pendingSaves = 0;
                    }

                    // ── Write progress every 10 rows ─────────────────────
                    if ( processedRows % 10 == 0 || processedRows == totalRows )
                    {
                        int pct = totalRows > 0
                            ? ( int ) Math.Round( ( processedRows / ( double ) totalRows ) * 100 )
                            : 100;

                        UpdateRunStatus( runId, ImportRunStatus.Running,
                            string.Format( "Processing row {0} of {1}…", processedRows, totalRows ),
                            pct, processedRows, totalRows );
                    }
                }

                // Final save for anything left in the batch.
                if ( pendingSaves > 0 )
                {
                    rockContext.SaveChanges();
                }
            }

            // ── Build result HTML and mark complete ────────────────────────
            string resultHtml = RenderResultsTable( resultRows, request.Mappings );
            MarkCompleted( runId, successCount, skippedCount, errorCount, totalRows, resultHtml );
        }

        // ─── Database helpers ─────────────────────────────────────────────

        private static CampPlacementImportRequest LoadRequest( int runId )
        {
            using ( var rockContext = new RockContext() )
            {
                var rows = rockContext.Database.SqlQuery<string>(
                    "SELECT [RequestJson] FROM [_org_secc_CampPlacementImportRun] WHERE [Id] = @runId",
                    new SqlParameter( "@runId", runId ) ).ToList();

                if ( !rows.Any() || string.IsNullOrWhiteSpace( rows[0] ) )
                    return null;

                return rows[0].FromJsonOrNull<CampPlacementImportRequest>();
            }
        }

        private static void UpdateRunStatus(
            int runId,
            ImportRunStatus status,
            string message,
            int percentComplete,
            int processedRows,
            int totalRows )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.ExecuteSqlCommand( @"
UPDATE [_org_secc_CampPlacementImportRun]
SET  [Status]          = @status
    ,[StatusMessage]   = @message
    ,[PercentComplete] = @pct
    ,[ProcessedRows]   = @processed
    ,[TotalRows]       = @total
WHERE [Id] = @runId",
                    new SqlParameter( "@status", ( int ) status ),
                    new SqlParameter( "@message", ( object ) message ?? DBNull.Value ),
                    new SqlParameter( "@pct", percentComplete ),
                    new SqlParameter( "@processed", processedRows ),
                    new SqlParameter( "@total", totalRows ),
                    new SqlParameter( "@runId", runId ) );
            }
        }

        private static void MarkCompleted(
            int runId,
            int successCount,
            int skippedCount,
            int errorCount,
            int totalRows,
            string resultHtml )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.ExecuteSqlCommand( @"
UPDATE [_org_secc_CampPlacementImportRun]
SET  [Status]            = @status
    ,[StatusMessage]     = @message
    ,[PercentComplete]   = 100
    ,[ProcessedRows]     = @total
    ,[TotalRows]         = @total
    ,[SuccessCount]      = @success
    ,[SkippedCount]      = @skipped
    ,[ErrorCount]        = @error
    ,[ResultHtml]        = @resultHtml
    ,[CompletedDateTime] = GETDATE()
WHERE [Id] = @runId",
                    new SqlParameter( "@status", ( int ) ImportRunStatus.Completed ),
                    new SqlParameter( "@message", string.Format( "Completed. {0} added, {1} skipped, {2} errors.", successCount, skippedCount, errorCount ) ),
                    new SqlParameter( "@total", totalRows ),
                    new SqlParameter( "@success", successCount ),
                    new SqlParameter( "@skipped", skippedCount ),
                    new SqlParameter( "@error", errorCount ),
                    new SqlParameter( "@resultHtml", ( object ) resultHtml ?? DBNull.Value ),
                    new SqlParameter( "@runId", runId ) );
            }
        }

        private static void MarkFailed( int runId, string message )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.ExecuteSqlCommand( @"
UPDATE [_org_secc_CampPlacementImportRun]
SET  [Status]            = @status
    ,[StatusMessage]     = @message
    ,[CompletedDateTime] = GETDATE()
WHERE [Id] = @runId",
                    new SqlParameter( "@status", ( int ) ImportRunStatus.Failed ),
                    new SqlParameter( "@message", ( object ) message ?? DBNull.Value ),
                    new SqlParameter( "@runId", runId ) );
            }
        }

        // ─── CSV helpers ──────────────────────────────────────────────────

        private static void ParseCsvFromBinaryFile(
            int binaryFileId,
            out List<string> headers,
            out List<List<string>> dataRows )
        {
            headers = new List<string>();
            dataRows = new List<List<string>>();

            using ( var rockContext = new RockContext() )
            {
                var binaryFile = new BinaryFileService( rockContext ).Get( binaryFileId );
                if ( binaryFile == null )
                    throw new Exception( "Binary file not found (ID " + binaryFileId + ")." );

                var csvContent = binaryFile.ContentsToString();
                if ( string.IsNullOrWhiteSpace( csvContent ) )
                    throw new Exception( "Binary file content is empty." );

                using ( var reader = new StringReader( csvContent ) )
                {
                    var allRows = ReadCsvRows( reader );
                    if ( allRows.Count == 0 )
                        return;

                    headers = allRows[0];

                    for ( int i = 1; i < allRows.Count; i++ )
                    {
                        // Skip blank rows
                        if ( allRows[i].Any( cell => !string.IsNullOrWhiteSpace( cell ) ) )
                            dataRows.Add( allRows[i] );
                    }
                }
            }
        }

        private static List<List<string>> ReadCsvRows( StringReader reader )
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
                        i++;
                        var field = new StringBuilder();
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
                                field.Append( line[i++] );
                            }
                        }
                        fields.Add( field.ToString().Trim() );
                        if ( i < line.Length && line[i] == ',' )
                            i++;
                    }
                    else
                    {
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

        // ─── Rock data helpers ────────────────────────────────────────────

        private static List<RegistrationRegistrant> LoadRegistrants( RockContext rockContext, int registrationInstanceId )
        {
            return new RegistrationRegistrantService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( r => r.PersonAlias.Person )
                .Where( r => r.Registration.RegistrationInstanceId == registrationInstanceId )
                .ToList();
        }

        private static Person FindRegistrant( string firstName, string lastName, List<RegistrationRegistrant> registrants )
        {
            if ( string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName ) )
                return null;

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

            return people.Count == 1 ? people[0] : null;
        }

        // ─── General helpers ──────────────────────────────────────────────

        private static string GetCell( List<string> row, int index )
        {
            return ( index >= 0 && index < row.Count ) ? row[index].Trim() : string.Empty;
        }

        private static string BuildFullName( string firstName, string lastName )
        {
            return string.Format( "{0} {1}", firstName, lastName ).Trim();
        }

        private static string MembershipKey( int personId, int groupId )
        {
            return string.Format( "{0}_{1}", personId, groupId );
        }

        // ─── HTML rendering ───────────────────────────────────────────────

        private static string RenderResultsTable(
            List<ResultRow> resultRows,
            List<CampPlacementMappingData> mappings )
        {
            var sb = new StringBuilder();
            sb.Append( "<div class='table-responsive'>" );
            sb.Append( "<table class='table table-bordered table-striped table-condensed'>" );

            // Header
            sb.Append( "<thead><tr>" );
            sb.Append( "<th>Row</th><th>Camper</th><th>Matched Person</th>" );
            foreach ( var m in mappings )
                sb.AppendFormat( "<th>{0}</th>", System.Web.HttpUtility.HtmlEncode( m.CsvColumnName ) );
            sb.Append( "</tr></thead><tbody>" );

            // Data
            foreach ( var row in resultRows )
            {
                sb.Append( "<tr>" );
                sb.AppendFormat( "<td>{0}</td>", row.CsvRowNumber );
                sb.AppendFormat( "<td>{0}</td>", System.Web.HttpUtility.HtmlEncode( row.CamperName ) );

                if ( !string.IsNullOrWhiteSpace( row.PersonError ) )
                    sb.AppendFormat( "<td><span class='label label-danger'>NOT FOUND</span><br/><small class='text-danger'>{0}</small></td>",
                        System.Web.HttpUtility.HtmlEncode( row.PersonError ) );
                else
                    sb.AppendFormat( "<td>{0}</td>", System.Web.HttpUtility.HtmlEncode( row.MatchedPersonName ) );

                foreach ( var m in mappings )
                {
                    var p = row.Placements.FirstOrDefault( x => x.ColumnName == m.CsvColumnName );
                    if ( p == null )
                    { sb.Append( "<td></td>" ); continue; }

                    string cls, lbl;
                    switch ( p.Outcome )
                    {
                        case PlacementOutcome.Success:
                            cls = "label-success";
                            lbl = "Added";
                            break;
                        case PlacementOutcome.Skipped:
                            cls = "label-info";
                            lbl = "Skipped";
                            break;
                        case PlacementOutcome.Error:
                            cls = "label-danger";
                            lbl = "Error";
                            break;
                        default:
                            cls = "label-default";
                            lbl = "Empty";
                            break;
                    }

                    sb.AppendFormat( "<td>{0} <span class='label {1}'>{2}</span><br/><small class='text-muted'>{3}</small></td>",
                        System.Web.HttpUtility.HtmlEncode( p.CsvValue ),
                        cls, lbl,
                        System.Web.HttpUtility.HtmlEncode( p.Message ) );
                }

                sb.Append( "</tr>" );
            }

            sb.Append( "</tbody></table></div>" );
            return sb.ToString();
        }
    }
}