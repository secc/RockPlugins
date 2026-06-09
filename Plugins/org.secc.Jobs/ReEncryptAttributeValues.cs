// <copyright>
// Custom maintenance job for Southeast Christian Church.
//
// Re-encrypts all encrypted AttributeValue data so that every value is stored
// under the CURRENT DataEncryptionKey. This is Phase 2 of a DataEncryptionKey
// rotation (see README.md). Run it ONLY after web.config has been updated so
// that:
//
//     DataEncryptionKey      = NEW key
//     OldDataEncryptionKey   = OLD (leaked) key   (and OldDataEncryptionKey1, 2, ... if needed)
//
// Rock's Encryption.DecryptString automatically falls back through the
// OldDataEncryptionKey* values, so this job can read old-key data and rewrite
// it with the new key. After the job reports 0 remaining old-key values you can
// remove the OldDataEncryptionKey settings.
//
// Target: Rock RMS 13.7 (Quartz IJob pattern).
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    /// <summary>
    /// Decrypts every encrypted AttributeValue (using the current key, falling back
    /// to OldDataEncryptionKey* for old data) and re-encrypts it with the current
    /// DataEncryptionKey. Idempotent and safe to re-run.
    /// </summary>
    [DisplayName( "Re-encrypt Attribute Values (Key Rotation)" )]
    [Description( "Re-encrypts all encrypted attribute values under the current DataEncryptionKey. Used during DataEncryptionKey rotation. Start with Dry Run = true." )]

    [BooleanField(
        "Dry Run",
        "When true (default), the job reports what it WOULD do without writing any changes. Always run a dry run first.",
        true,
        "General", 0, "DryRun" )]

    [IntegerField(
        "Batch Size",
        "Number of attribute values to process per database round trip / save. 500 is a safe default.",
        false, 500, "General", 1, "BatchSize" )]

    [TextField(
        "Field Type Classes",
        "Comma-delimited list of FieldType class names whose attribute values are encrypted. Defaults cover Rock core. Add any custom encrypting field types from your plugins.",
        false,
        "Rock.Field.Types.EncryptedTextFieldType,Rock.Field.Types.SSNFieldType",
        "General", 2, "FieldTypeClasses" )]

    [TextField(
        "Additional Attribute Keys",
        "Comma-delimited list of attribute KEYS to re-encrypt regardless of their field type. Use for plain (e.g. Text) attributes that hold manually-encrypted values. Matches entity attributes (not global) by key across all entity types. Only add keys you KNOW hold Encryption.EncryptString output.",
        false, "", "Gaps", 3, "AdditionalAttributeKeys" )]

    [TextField(
        "Global Attribute Keys",
        "Comma-delimited list of GLOBAL attribute keys to re-encrypt via a dedicated one-shot (decrypt -> re-encrypt -> save -> flush GlobalAttributesCache). Defaults to the known Intacct settings blob.",
        false, "IntacctAPISettings", "Gaps", 4, "GlobalAttributeKeys" )]

    [IntegerField(
        "Start At AttributeValue Id",
        "Resume helper. Only process AttributeValue rows with Id greater than this. Leave 0 to start from the beginning.",
        false, 0, "General", 5, "StartAtId" )]

    [IntegerField(
        "Max Values To Process",
        "Safety/test limit. 0 = no limit (process everything).",
        false, 0, "General", 6, "MaxValues" )]

    [BooleanField(
        "Require Old Key Present",
        "When true (default), a live run aborts if no OldDataEncryptionKey app setting is found. This prevents accidentally destroying old-key data that cannot be decrypted.",
        true,
        "General", 7, "RequireOldKey" )]

    [IntegerField(
        "Command Timeout",
        "SQL command timeout in seconds for each database operation. The default 30s is too low for a bulk pass over a large AttributeValue table. 0 = no timeout.",
        false, 300, "General", 8, "CommandTimeout" )]

    [DisallowConcurrentExecution]
    public class ReEncryptAttributeValues : IJob
    {
        /// <summary>
        /// Empty constructor required by the Quartz scheduler.
        /// </summary>
        public ReEncryptAttributeValues()
        {
        }

        /// <summary>
        /// Executes the re-encryption pass.
        /// </summary>
        public virtual void Execute( IJobExecutionContext context )
        {
            var dataMap = context.JobDetail.JobDataMap;

            bool dryRun = dataMap.GetString( "DryRun" ).AsBoolean( true );
            int batchSize = dataMap.GetString( "BatchSize" ).AsIntegerOrNull() ?? 500;
            if ( batchSize < 1 )
            { batchSize = 500; }
            int startAtId = dataMap.GetString( "StartAtId" ).AsIntegerOrNull() ?? 0;
            int maxValues = dataMap.GetString( "MaxValues" ).AsIntegerOrNull() ?? 0;
            bool requireOldKey = dataMap.GetString( "RequireOldKey" ).AsBoolean( true );
            int commandTimeout = dataMap.GetString( "CommandTimeout" ).AsIntegerOrNull() ?? 300;
            if ( commandTimeout < 0 )
            { commandTimeout = 300; }

            var fieldTypeClasses = dataMap.GetString( "FieldTypeClasses" )
                .SplitDelimitedValues()
                .Select( s => s.Trim() )
                .Where( s => s.IsNotNullOrWhiteSpace() )
                .ToList();

            var additionalAttributeKeys = dataMap.GetString( "AdditionalAttributeKeys" )
                .SplitDelimitedValues()
                .Select( s => s.Trim() )
                .Where( s => s.IsNotNullOrWhiteSpace() )
                .Distinct()
                .ToList();

            var globalAttributeKeys = dataMap.GetString( "GlobalAttributeKeys" )
                .SplitDelimitedValues()
                .Select( s => s.Trim() )
                .Where( s => s.IsNotNullOrWhiteSpace() )
                .Distinct()
                .ToList();

            // ---- Preconditions -------------------------------------------------
            if ( !fieldTypeClasses.Any() && !additionalAttributeKeys.Any() && !globalAttributeKeys.Any() )
            {
                throw new Exception( "Nothing configured: set Field Type Classes, Additional Attribute Keys, or Global Attribute Keys." );
            }

            bool oldKeyPresent = WebConfigurationManager.AppSettings["OldDataEncryptionKey"].IsNotNullOrWhiteSpace();
            if ( !dryRun && requireOldKey && !oldKeyPresent )
            {
                throw new Exception(
                    "Aborting: 'Require Old Key Present' is true but no 'OldDataEncryptionKey' app setting was found. " +
                    "Add the previous (old) key as OldDataEncryptionKey in web.config before running live, " +
                    "or set 'Require Old Key Present' to false if you are certain all data is already on the current key." );
            }

            // ---- Resolve which attributes are encrypted ------------------------
            var attributeIds = new List<int>();
            long includeListAttrCount = 0;     // attributes added purely via the include-list
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.CommandTimeout = commandTimeout;
                if ( fieldTypeClasses.Any() )
                {
                    var fieldTypeIds = new FieldTypeService( rockContext ).Queryable().AsNoTracking()
                        .Where( ft => fieldTypeClasses.Contains( ft.Class ) )
                        .Select( ft => ft.Id )
                        .ToList();

                    if ( fieldTypeIds.Any() )
                    {
                        attributeIds = new AttributeService( rockContext ).Queryable().AsNoTracking()
                            .Where( a => fieldTypeIds.Contains( a.FieldTypeId ) )
                            .Select( a => a.Id )
                            .ToList();
                    }
                }

                // Include-list: entity attributes (EntityTypeId set) matched by Key, regardless of
                // field type. Globals (EntityTypeId null) are intentionally excluded here -- they go
                // through the dedicated global one-shot so the GlobalAttributesCache gets flushed.
                if ( additionalAttributeKeys.Any() )
                {
                    var includeIds = new AttributeService( rockContext ).Queryable().AsNoTracking()
                        .Where( a => additionalAttributeKeys.Contains( a.Key ) && a.EntityTypeId.HasValue )
                        .Select( a => a.Id )
                        .ToList();

                    includeListAttrCount = includeIds.Where( id => !attributeIds.Contains( id ) ).Count();
                    attributeIds = attributeIds.Union( includeIds ).ToList();
                }
            }

            if ( !attributeIds.Any() && !globalAttributeKeys.Any() )
            {
                context.Result = "No matching attributes found (field types or include-list) and no global keys configured. Nothing to do.";
                return;
            }

            // ---- Counters ------------------------------------------------------
            long scanned = 0;
            long reEncrypted = 0;   // in dry run: would re-encrypt
            long skippedEmpty = 0;
            long skippedNotBase64 = 0;
            long failedDecrypt = 0;
            long roundTripFailed = 0;

            var failedDecryptSampleIds = new List<int>();
            var notBase64SampleIds = new List<int>();
            var roundTripSampleIds = new List<int>();

            int lastId = startAtId;

            // ---- Pre-fetch candidate AttributeValue Ids (one indexed pass) -----
            // Do NOT keyset-page with "Id > lastId AND AttributeId IN (...)": on a large
            // AttributeValue table that degrades into clustered-index scans once lastId
            // passes the bulk of matches, and each batch times out. Instead pull all
            // matching Ids once (the AttributeId index makes this a seek), then fetch and
            // update in batches BY PRIMARY KEY, which are fast seeks regardless of size.
            var candidateIds = new List<int>();
            if ( attributeIds.Any() )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.CommandTimeout = commandTimeout;
                    // Only filter on Id + AttributeId here so this is a covered seek on
                    // IX_AttributeId (the clustered Id is implicitly included). Empty values
                    // are cheaply skipped per-row below; filtering Value here would force a
                    // key lookup for every row and slow the pass dramatically.
                    candidateIds = new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                        .Where( av =>
                            av.Id > startAtId &&
                            attributeIds.Contains( av.AttributeId ) )
                        .OrderBy( av => av.Id )
                        .Select( av => av.Id )
                        .ToList();
                }

                if ( maxValues > 0 && candidateIds.Count > maxValues )
                {
                    candidateIds = candidateIds.Take( maxValues ).ToList();
                }
            }

            long candidateCount = candidateIds.Count;

            // ---- Process in batches by primary key -----------------------------
            for ( int batchOffset = 0; batchOffset < candidateIds.Count; batchOffset += batchSize )
            {
                var idBatch = candidateIds.Skip( batchOffset ).Take( batchSize ).ToList();

                // Fresh context per batch keeps the change tracker small.
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.CommandTimeout = commandTimeout;
                    var attributeValueService = new AttributeValueService( rockContext );

                    // Tracked fetch by primary key so we can update Value in place.
                    var batch = attributeValueService.Queryable()
                        .Where( av => idBatch.Contains( av.Id ) )
                        .ToList();

                    bool batchHasChanges = false;

                    foreach ( var av in batch )
                    {
                        scanned++;

                        string current = av.Value;
                        if ( current.IsNullOrWhiteSpace() )
                        {
                            skippedEmpty++;
                            continue;
                        }

                        // Encrypted values are always Base64. If it isn't, it was never
                        // encrypted (or is corrupt) -- never touch it.
                        if ( !IsBase64( current ) )
                        {
                            skippedNotBase64++;
                            if ( notBase64SampleIds.Count < 25 )
                            { notBase64SampleIds.Add( av.Id ); }
                            continue;
                        }

                        // Decrypt using current key, falling back to OldDataEncryptionKey*.
                        string plain;
                        try
                        {
                            plain = Encryption.DecryptString( current );
                        }
                        catch
                        {
                            plain = null;
                        }

                        if ( plain == null )
                        {
                            // Could not decrypt with the current key OR any old key.
                            // Do NOT overwrite -- flag it for investigation.
                            failedDecrypt++;
                            if ( failedDecryptSampleIds.Count < 25 )
                            { failedDecryptSampleIds.Add( av.Id ); }
                            continue;
                        }

                        // Re-encrypt with the current key.
                        string newCipher = Encryption.EncryptString( plain );

                        // Round-trip safety check: confirm the new ciphertext decrypts back
                        // to exactly the same plaintext BEFORE we commit to overwriting.
                        string verify;
                        try
                        {
                            verify = Encryption.DecryptString( newCipher );
                        }
                        catch
                        {
                            verify = null;
                        }

                        if ( verify == null || verify != plain )
                        {
                            roundTripFailed++;
                            if ( roundTripSampleIds.Count < 25 )
                            { roundTripSampleIds.Add( av.Id ); }
                            continue;
                        }

                        reEncrypted++;

                        if ( !dryRun )
                        {
                            av.Value = newCipher;
                            batchHasChanges = true;
                        }
                    }

                    if ( !dryRun && batchHasChanges )
                    {
                        // disablePrePostProcessing: true -> skip audits, save hooks, ModifiedDateTime
                        // bumps and any attribute-change side effects. The decrypted value is unchanged,
                        // so there is nothing downstream that needs to react.
                        rockContext.SaveChanges( true );
                    }
                }

                // candidateIds is ascending, so the slice's last Id is the high-water mark.
                lastId = idBatch[idBatch.Count - 1];

                context.UpdateLastStatusMessage(
                    $"{( dryRun ? "[DRY RUN] " : "" )}Scanned {scanned:N0} / {candidateCount:N0}; " +
                    $"{( dryRun ? "would re-encrypt" : "re-encrypted" )} {reEncrypted:N0}; " +
                    $"failedDecrypt {failedDecrypt:N0}; lastId {lastId}." );
            }

            // ---- Global attribute one-shot ------------------------------------
            var globalReport = new StringBuilder();
            ReEncryptGlobalAttributes( globalAttributeKeys, dryRun, commandTimeout, globalReport );

            // ---- Final summary -------------------------------------------------
            var sb = new StringBuilder();
            sb.AppendLine( dryRun ? "DRY RUN (no changes written)." : "LIVE RUN complete." );
            sb.AppendLine( $"Attributes targeted (field types + include-list): {attributeIds.Count:N0}" );
            sb.AppendLine( $"  ...of which added only via include-list keys: {includeListAttrCount:N0}" );
            sb.AppendLine( $"Values scanned: {scanned:N0}" );
            sb.AppendLine( $"{( dryRun ? "Values that WOULD be re-encrypted" : "Values re-encrypted" )}: {reEncrypted:N0}" );
            sb.AppendLine( $"Skipped (empty/whitespace): {skippedEmpty:N0}" );
            sb.AppendLine( $"Skipped (not Base64 / not encrypted): {skippedNotBase64:N0}" );
            sb.AppendLine( $"FAILED to decrypt (left untouched - investigate): {failedDecrypt:N0}" );
            sb.AppendLine( $"Round-trip verification failures (left untouched): {roundTripFailed:N0}" );
            sb.AppendLine( $"Last AttributeValue Id processed: {lastId}" );

            if ( failedDecryptSampleIds.Any() )
            {
                sb.AppendLine( "Sample failed-decrypt AttributeValue Ids: " + failedDecryptSampleIds.AsDelimited( ", " ) );
            }
            if ( notBase64SampleIds.Any() )
            {
                sb.AppendLine( "Sample not-Base64 AttributeValue Ids: " + notBase64SampleIds.AsDelimited( ", " ) );
            }
            if ( roundTripSampleIds.Any() )
            {
                sb.AppendLine( "Sample round-trip-failure AttributeValue Ids: " + roundTripSampleIds.AsDelimited( ", " ) );
            }

            if ( failedDecrypt > 0 )
            {
                sb.AppendLine();
                sb.AppendLine( "WARNING: Some values could not be decrypted with the current key or any OldDataEncryptionKey. " +
                    "Do NOT remove OldDataEncryptionKey settings until this count is 0. Investigate the sample Ids above." );
            }

            sb.Append( globalReport.ToString() );

            context.Result = sb.ToString();
        }

        /// <summary>
        /// Re-encrypts named GLOBAL attribute values (EntityId null) under the current key.
        /// Global attribute values are cached in GlobalAttributesCache, and the save hook that
        /// would normally clear that cache is skipped by SaveChanges(disablePrePostProcessing: true),
        /// so this method flushes the cache itself after any live change.
        /// Same safety guarantees as the main loop: only overwrites on successful decrypt + round trip.
        /// </summary>
        private void ReEncryptGlobalAttributes( List<string> globalKeys, bool dryRun, int commandTimeout, StringBuilder report )
        {
            if ( globalKeys == null || !globalKeys.Any() )
            {
                return;
            }

            report.AppendLine();
            report.AppendLine( "--- Global attributes ---" );

            bool wroteAny = false;

            foreach ( var key in globalKeys )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.CommandTimeout = commandTimeout;
                    var av = new AttributeValueService( rockContext ).GetGlobalAttributeValue( key );

                    if ( av == null )
                    {
                        report.AppendLine( $"[{key}] global attribute/value not found - skipped." );
                        continue;
                    }

                    string current = av.Value;
                    if ( current.IsNullOrWhiteSpace() )
                    {
                        report.AppendLine( $"[{key}] empty value - skipped." );
                        continue;
                    }

                    if ( !IsBase64( current ) )
                    {
                        report.AppendLine( $"[{key}] value is not Base64 (not encrypted?) - left untouched." );
                        continue;
                    }

                    string plain;
                    try
                    { plain = Encryption.DecryptString( current ); }
                    catch { plain = null; }

                    if ( plain == null )
                    {
                        report.AppendLine( $"[{key}] FAILED to decrypt with current or any old key - left untouched. Investigate before removing OldDataEncryptionKey." );
                        continue;
                    }

                    string newCipher = Encryption.EncryptString( plain );

                    string verify;
                    try
                    { verify = Encryption.DecryptString( newCipher ); }
                    catch { verify = null; }

                    if ( verify == null || verify != plain )
                    {
                        report.AppendLine( $"[{key}] round-trip verification failed - left untouched." );
                        continue;
                    }

                    if ( dryRun )
                    {
                        report.AppendLine( $"[{key}] WOULD be re-encrypted." );
                    }
                    else
                    {
                        av.Value = newCipher;
                        rockContext.SaveChanges( true );
                        wroteAny = true;
                        report.AppendLine( $"[{key}] re-encrypted." );
                    }
                }
            }

            if ( wroteAny )
            {
                // The save hook that normally clears this cache is skipped by disablePrePostProcessing.
                GlobalAttributesCache.Remove();
                report.AppendLine( "GlobalAttributesCache flushed." );
            }
        }

        /// <summary>
        /// True if the string is valid Base64 (the format every Rock-encrypted value uses).
        /// </summary>
        private static bool IsBase64( string value )
        {
            if ( value.IsNullOrWhiteSpace() )
            {
                return false;
            }

            value = value.Trim();

            // Base64 length is always a multiple of 4.
            if ( value.Length % 4 != 0 )
            {
                return false;
            }

            try
            {
                Convert.FromBase64String( value );
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
