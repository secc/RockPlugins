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
using System.Data.Entity.SqlServer;
using System.Linq;

using Newtonsoft.Json;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    /// <summary>
    /// Job that disables email and SMS communication preferences for people
    /// whose Record Status is Inactive. Stores original preferences in a
    /// DefinedType so they can be restored if the person becomes active again.
    /// </summary>
    [DisplayName( "SECC | Disable Communications for Inactive People" )]
    [Description( "Disables email and SMS preferences for people with an Inactive record status. Original preferences are stored in a DefinedType for later restoration." )]
    [DefinedTypeField( "Inactive Person Communication Overrides Defined Type",
        Description = "The Defined Type used to store original communication preferences for inactive people.",
        IsRequired = true,
        Key = "InactiveCommunicationOverridesDefinedType",
        Order = 0 )]
    [IntegerField( "Lookback Days",
        Description = "Only process people whose record status changed within this many days. Set to 0 to process all inactive people regardless of when their status changed. Use 0 for the initial run.",
        IsRequired = false,
        DefaultIntegerValue = 15,
        Key = "LookbackDays",
        Order = 1 )]
    [BooleanField( "Dry Run",
        Description = "When enabled, the job reports how many people would be affected without making any changes. Use this to validate the job before processing real data.",
        DefaultBooleanValue = true,
        Key = "DryRun",
        Order = 2 )]
    [DisallowConcurrentExecution]
    public class DisableCommunicationsForInactivePeople : IJob
    {
        private class PreferenceSnapshot
        {
            public int EmailPreference { get; set; }
            public List<PhoneSnapshot> Phones { get; set; }
        }

        private class PhoneSnapshot
        {
            public int PhoneNumberId { get; set; }
            public bool IsMessagingEnabled { get; set; }
        }

        /// <summary>
        /// Empty constructor required by the Quartz scheduler.
        /// </summary>
        public DisableCommunicationsForInactivePeople()
        {
        }

        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var definedTypeGuid = dataMap.GetString( "InactiveCommunicationOverridesDefinedType" ).AsGuid();
            int lookbackDays = dataMap.GetString( "LookbackDays" ).AsIntegerOrNull() ?? 15;
            bool dryRun = dataMap.GetString( "DryRun" ).AsBoolean();

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );
            var definedType = DefinedTypeCache.Get( definedTypeGuid );

            if ( definedType == null )
            {
                throw new RockJobWarningException( "Inactive Person Communication Overrides Defined Type not found. Verify the job attribute is configured." );
            }

            // Get the Inactive record status DefinedValue Id
            var inactiveStatusCache = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() );
            if ( inactiveStatusCache == null )
            {
                throw new RockJobWarningException( "Could not find the Inactive Record Status DefinedValue." );
            }
            var inactiveStatusId = inactiveStatusCache.Id;

            // Build a subquery of PersonIds already tracked (stays in SQL, avoids parameter limit)
            var trackedPersonIdQuery = definedValueService.Queryable()
                .Where( dv => dv.DefinedTypeId == definedType.Id )
                .Select( dv => dv.Value );

            // Query inactive people not yet tracked
            var query = personService.Queryable().AsNoTracking()
                .Where( p => p.RecordStatusValueId == inactiveStatusId )
                .Where( p => !trackedPersonIdQuery.Contains( SqlFunctions.StringConvert( ( double ) p.Id ).Trim() ) );

            // Apply lookback filter if configured (skip for initial catch-up run with 0)
            if ( lookbackDays > 0 )
            {
                var lookbackDate = RockDateTime.Now.AddDays( -lookbackDays );
                query = query.Where( p => p.RecordStatusLastModifiedDateTime.HasValue
                    && p.RecordStatusLastModifiedDateTime.Value >= lookbackDate );
            }

            var inactivePeople = query
                .OrderBy( p => p.Id )
                .Select( p => new
                {
                    p.Id,
                    p.EmailPreference
                } )
                .ToList();

            // Dry run: report what would happen without making changes
            if ( dryRun )
            {
                context.Result = $"[DRY RUN] Found {inactivePeople.Count} inactive people to process (lookback: {( lookbackDays > 0 ? lookbackDays + " days" : "all" )}). No changes were made.";
                return;
            }

            int disabledCount = 0;
            int errorCount = 0;
            int skippedCount = 0;

            foreach ( var person in inactivePeople )
            {
                try
                {
                    using ( var personContext = new RockContext() )
                    {
                    var pService = new PersonService( personContext );
                    var phService = new PhoneNumberService( personContext );
                    var dvService = new DefinedValueService( personContext );

                    var personEntity = pService.Get( person.Id );
                    if ( personEntity == null )
                    {
                        skippedCount++;
                        continue;
                    }

                    // Load all phone numbers once for snapshot and modification
                    var phoneEntities = phService.Queryable()
                        .Where( pn => pn.PersonId == person.Id )
                        .ToList();

                    // Build snapshot using typed DTOs
                    var snapshot = new PreferenceSnapshot
                    {
                        EmailPreference = ( int ) person.EmailPreference,
                        Phones = phoneEntities.Select( ph => new PhoneSnapshot
                        {
                            PhoneNumberId = ph.Id,
                            IsMessagingEnabled = ph.IsMessagingEnabled
                        } ).ToList()
                    };

                    // Store original preferences as a DefinedValue (Value = PersonId)
                    var definedValue = new DefinedValue
                    {
                        DefinedTypeId = definedType.Id,
                        Value = person.Id.ToString(),
                        Description = JsonConvert.SerializeObject( snapshot ),
                        IsSystem = false
                    };
                    dvService.Add( definedValue );

                    // Disable email (DoNotEmail blocks all email — bulk and regular)
                    personEntity.EmailPreference = EmailPreference.DoNotEmail;

                    // Disable SMS on all phone numbers
                    foreach ( var phone in phoneEntities.Where( pn => pn.IsMessagingEnabled ) )
                    {
                        phone.IsMessagingEnabled = false;
                    }

                    personContext.SaveChanges();
                    disabledCount++;
                    }
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex );
                    errorCount++;
                }
            }

            // Invalidate the DefinedType cache so other code sees the new entries
            DefinedTypeCache.Remove( definedType.Id );

            var result = $"Evaluated {inactivePeople.Count} inactive people (lookback: {( lookbackDays > 0 ? lookbackDays + " days" : "all" )}). Disabled communications for {disabledCount}. Skipped {skippedCount} (no longer found).";
            if ( errorCount > 0 )
            {
                result += $" {errorCount} errors occurred.";
                throw new RockJobWarningException( result );
            }

            context.Result = result;
        }
    }
}
