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
    public static class CampPlacementImportRunner
    {
        private enum PlacementOutcome { Empty = 0, Success = 1, Skipped = 2, Error = 3 }

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

        private class MappingMeta
        {
            public CampPlacementMappingData Mapping { get; set; }
            public int ColumnIndex { get; set; }
            public Dictionary<string, Group> GroupByName { get; set; }
        }

        public static void Run( int runId )
        {
            CampPlacementImportRequest request = null;

            try
            {
                UpdateRunStatus( runId, ImportRunStatus.Running, "Starting import…", 0, 0, 0 );

                request = LoadRequest( runId );

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

                int firstNameIdx = headers.IndexOf( request.FirstNameCol );
                int lastNameIdx = headers.IndexOf( request.LastNameCol );

                if ( firstNameIdx < 0 || lastNameIdx < 0 )
                {
                    MarkFailed( runId, "First Name or Last Name column not found in CSV headers." );
                    return;
                }

                if ( request.Mappings == null || !request.Mappings.Any() )
                {
                    MarkFailed( runId, "No placement mappings were provided." );
                    return;
                }

                var duplicateMappingColumns = request.Mappings
                    .Where( m => !string.IsNullOrWhiteSpace( m.CsvColumnName ) )
                    .GroupBy( m => m.CsvColumnName, StringComparer.OrdinalIgnoreCase )
                    .Where( g => g.Count() > 1 )
                    .Select( g => g.Key )
                    .OrderBy( n => n )
                    .ToList();

                if ( duplicateMappingColumns.Any() )
                {
                    MarkFailed( runId, "Duplicate mapping columns are not allowed: " + string.Join( ", ", duplicateMappingColumns ) );
                    return;
                }

                var invalidMappingRows = request.Mappings
                    .Select( ( m, i ) => new { Mapping = m, RowNumber = i + 1 } )
                    .Where( x => x.Mapping == null || string.IsNullOrWhiteSpace( x.Mapping.CsvColumnName ) )
                    .Select( x => x.RowNumber )
                    .ToList();

                if ( invalidMappingRows.Any() )
                {
                    MarkFailed( runId, "Each mapping must include a CSV column name. Invalid mapping row(s): " + string.Join( ", ", invalidMappingRows ) );
                    return;
                }

                var headerIndexByName = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
                for ( int i = 0; i < headers.Count; i++ )
                {
                    if ( !headerIndexByName.ContainsKey( headers[i] ) )
                    {
                        headerIndexByName[headers[i]] = i;
                    }
                }

                var status = ( GroupMemberStatus ) request.DefaultGroupMemberStatusValue;
                int batchSize = request.BatchSize > 0 ? request.BatchSize : 50;

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

                int successCount = 0;
                int skippedCount = 0;
                int errorCount = 0;
                var resultRows = new List<ResultRow>();
                
                DateTime lastProgressUpdate = DateTime.UtcNow;

                using ( var rockContext = new RockContext() )
                {
                    var registrants = LoadRegistrants( rockContext, request.RegistrationInstanceId.Value );

                    var groupService = new GroupService( rockContext );
                    var groupMemberService = new GroupMemberService( rockContext );

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

                        existingMembershipLookup = existingMembers
                            .GroupBy( gm => MembershipKey( gm.PersonId, gm.GroupId ) )
                            .ToDictionary(
                                g => g.Key,
                                g => g
                                    .OrderBy( gm => gm.IsArchived )
                                    .ThenBy( gm => gm.Id )
                                    .First() );
                    }

                    var registrantLookup = BuildRegistrantLookup( registrants );

                    var mappingMeta = new List<MappingMeta>();
                    foreach ( var mapping in request.Mappings )
                    {
                        int colIdx;
                        if ( !headerIndexByName.TryGetValue( mapping.CsvColumnName, out colIdx ) )
                        {
                            MarkFailed(
                                runId,
                                string.Format(
                                    "Configured CSV mapping column '{0}' was not found in the import file headers.",
                                    mapping.CsvColumnName ) );
                            return;
                        }

                        var childGroups = childGroupsByParent.ContainsKey( mapping.ParentGroupId )
                            ? childGroupsByParent[mapping.ParentGroupId]
                            : new List<Group>();

                        var groupByName = new Dictionary<string, Group>( StringComparer.OrdinalIgnoreCase );
                        foreach ( var g in childGroups )
                        {
                            if ( !groupByName.ContainsKey( g.Name ) )
                            {
                                groupByName[g.Name] = g;
                            }
                        }

                        mappingMeta.Add( new MappingMeta
                        {
                            Mapping = mapping,
                            ColumnIndex = colIdx,
                            GroupByName = groupByName
                        } );
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
                            CsvRowNumber = rowIdx + 2, 
                            CamperName = csvFullName,
                            Placements = new List<PlacementResult>()
                        };

                        if ( duplicateNames.Contains( csvFullName ) )
                        {
                            resultRow.PersonError = string.Format(
                                "'{0}' appears {1} times in the CSV. All rows for this name are skipped.",
                                csvFullName, nameCount[csvFullName] );

                            foreach ( var meta in mappingMeta )
                            {
                                resultRow.Placements.Add( new PlacementResult
                                {
                                    ColumnName = meta.Mapping.CsvColumnName,
                                    CsvValue = GetCell( row, meta.ColumnIndex ),
                                    Outcome = PlacementOutcome.Error,
                                    Message = "Skipped — duplicate name in CSV"
                                } );
                                errorCount++;
                            }

                            if ( resultRows.Count < 200 ) resultRows.Add( resultRow );
                            processedRows++;
                            continue;
                        }

                        var person = FindRegistrant( firstName, lastName, registrantLookup );

                        if ( person == null )
                        {
                            resultRow.PersonError = string.Format(
                                "Could not match '{0}' to a registrant.", csvFullName );

                            foreach ( var meta in mappingMeta )
                            {
                                resultRow.Placements.Add( new PlacementResult
                                {
                                    ColumnName = meta.Mapping.CsvColumnName,
                                    CsvValue = GetCell( row, meta.ColumnIndex ),
                                    Outcome = PlacementOutcome.Error,
                                    Message = "Person not found"
                                } );
                                errorCount++;
                            }

                            if ( resultRows.Count < 200 ) resultRows.Add( resultRow );
                            processedRows++;
                            continue;
                        }

                        resultRow.MatchedPersonName = person.FullName;

                        foreach ( var meta in mappingMeta )
                        {
                            var mapping = meta.Mapping;
                            string cellValue = GetCell( row, meta.ColumnIndex );

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

                            Group targetGroup;
                            if ( !meta.GroupByName.TryGetValue( cellValue, out targetGroup ) )
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
                                    existingMember.ArchivedDateTime = null;
                                    existingMember.ArchivedByPersonAliasId = null;
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
                                placementResult.Message = string.Format(
                                    "Validation failed for '{0}': {1}",
                                    targetGroup.Name,
                                    string.Join( "; ", groupMember.ValidationResults.Select( v => v.ErrorMessage ) ) );
                                errorCount++;
                            }

                            resultRow.Placements.Add( placementResult );
                        }

                        if ( resultRows.Count < 200 ) resultRows.Add( resultRow );
                        processedRows++;

                        if ( pendingSaves >= batchSize )
                        {
                            rockContext.SaveChanges();
                            pendingSaves = 0;
                        }

                        if ( processedRows == totalRows || (DateTime.UtcNow - lastProgressUpdate).TotalSeconds >= 2 )
                        {
                            int pct = totalRows > 0
                                ? ( int ) Math.Round( ( processedRows / ( double ) totalRows ) * 100 )
                                : 100;

                            UpdateRunStatus( runId, ImportRunStatus.Running,
                                string.Format( "Processing row {0} of {1}…", processedRows, totalRows ),
                                pct, processedRows, totalRows );
                            
                            lastProgressUpdate = DateTime.UtcNow;
                        }
                    }

                    if ( pendingSaves > 0 )
                    {
                        rockContext.SaveChanges();
                    }
                }

                string resultHtml = RenderResultsTable( resultRows, request.Mappings, totalRows );
                MarkCompleted( runId, successCount, skippedCount, errorCount, totalRows, resultHtml );
            }
            finally
            {
                if ( request != null && request.BinaryFileId.HasValue )
                {
                    try
                    {
                        using ( var ctx = new RockContext() )
                        {
                            var fileService = new BinaryFileService( ctx );
                            var file = fileService.Get( request.BinaryFileId.Value );
                            if ( file != null )
                            {
                                fileService.Delete( file );
                                ctx.SaveChanges();
                            }
                        }
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, null );
                    }
                }
            }
        }

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
                        if ( allRows[i].Any( cell => !string.IsNullOrWhiteSpace( cell ) ) )
                            dataRows.Add( allRows[i] );
                    }
                }
            }
        }

        private static List<List<string>> ReadCsvRows( StringReader reader )
        {
            var rows = new List<List<string>>();
            var csvContent = reader.ReadToEnd();

            if ( string.IsNullOrEmpty( csvContent ) )
            {
                return rows;
            }

            var fields = new List<string>();
            var field = new StringBuilder();
            bool isInQuotes = false;

            for ( int i = 0; i < csvContent.Length; i++ )
            {
                char currentChar = csvContent[i];

                if ( currentChar == '"' )
                {
                    if ( isInQuotes && i + 1 < csvContent.Length && csvContent[i + 1] == '"' )
                    {
                        field.Append( '"' );
                        i++;
                    }
                    else
                    {
                        isInQuotes = !isInQuotes;
                    }
                }
                else if ( currentChar == ',' && !isInQuotes )
                {
                    fields.Add( field.ToString().Trim() );
                    field.Clear();
                }
                else if ( ( currentChar == '\r' || currentChar == '\n' ) && !isInQuotes )
                {
                    fields.Add( field.ToString().Trim() );
                    field.Clear();
                    rows.Add( fields );
                    fields = new List<string>();

                    if ( currentChar == '\r' && i + 1 < csvContent.Length && csvContent[i + 1] == '\n' )
                    {
                        i++;
                    }
                }
                else
                {
                    field.Append( currentChar );
                }
            }

            if ( field.Length > 0 || fields.Count > 0 )
            {
                fields.Add( field.ToString().Trim() );
                rows.Add( fields );
            }

            return rows;
        }

        private static List<RegistrationRegistrant> LoadRegistrants( RockContext rockContext, int registrationInstanceId )
        {
            return new RegistrationRegistrantService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Include( r => r.PersonAlias.Person )
                .Where( r => r.Registration.RegistrationInstanceId == registrationInstanceId )
                .ToList();
        }

        private static Dictionary<string, List<Person>> BuildRegistrantLookup( List<RegistrationRegistrant> registrants )
        {
            var lookup = new Dictionary<string, List<Person>>( StringComparer.OrdinalIgnoreCase );

            foreach ( var registrant in registrants )
            {
                var person = registrant.PersonAlias != null ? registrant.PersonAlias.Person : null;
                if ( person == null || string.IsNullOrWhiteSpace( person.LastName ) )
                {
                    continue;
                }

                AddRegistrantLookupKey( lookup, person.FirstName, person.LastName, person );

                if ( !string.IsNullOrWhiteSpace( person.NickName )
                    && !person.NickName.Equals( person.FirstName, StringComparison.OrdinalIgnoreCase ) )
                {
                    AddRegistrantLookupKey( lookup, person.NickName, person.LastName, person );
                }
            }

            return lookup;
        }

        private static void AddRegistrantLookupKey(
            Dictionary<string, List<Person>> lookup,
            string firstName,
            string lastName,
            Person person )
        {
            if ( person == null || string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName ) )
            {
                return;
            }

            var key = BuildNameLookupKey( firstName, lastName );
            List<Person> matches;
            if ( !lookup.TryGetValue( key, out matches ) )
            {
                matches = new List<Person>();
                lookup[key] = matches;
            }

            if ( !matches.Any( p => p.Id == person.Id ) )
            {
                matches.Add( person );
            }
        }

        private static string BuildNameLookupKey( string firstName, string lastName )
        {
            return string.Format( "{0}|{1}", firstName.Trim(), lastName.Trim() );
        }

        private static Person FindRegistrant(
            string firstName,
            string lastName,
            Dictionary<string, List<Person>> registrantLookup )
        {
            if ( string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName ) )
            {
                return null;
            }

            List<Person> matches;
            if ( registrantLookup.TryGetValue( BuildNameLookupKey( firstName, lastName ), out matches )
                && matches.Count == 1 )
            {
                return matches[0];
            }

            return null;
        }

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

        private static string RenderResultsTable(
            List<ResultRow> resultRows,
            List<CampPlacementMappingData> mappings,
            int totalRows )
        {
            var sb = new StringBuilder();
            sb.Append( "<div class='table-responsive'>" );
            sb.Append( "<table class='table table-bordered table-striped table-condensed'>" );

            sb.Append( "<thead><tr>" );
            sb.Append( "<th>Row</th><th>Camper</th><th>Matched Person</th>" );
            foreach ( var m in mappings )
                sb.AppendFormat( "<th>{0}</th>", System.Web.HttpUtility.HtmlEncode( m.CsvColumnName ) );
            sb.Append( "</tr></thead><tbody>" );

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

            if ( totalRows > 2000 ) 
            {
                sb.AppendFormat( "<tr><td colspan='100%' class='text-center text-muted'><i>Showing first 2000 results... Please consult overall counts for final summaries.</i></td></tr>" );
            }

            sb.Append( "</tbody></table></div>" );
            return sb.ToString();
        }
    }
}