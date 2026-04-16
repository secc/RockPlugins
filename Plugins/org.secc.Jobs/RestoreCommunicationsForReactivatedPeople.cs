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
    /// Job that restores email and SMS communication preferences for people
    /// who were previously disabled by DisableCommunicationsForInactivePeople
    /// and have since become active again.
    /// </summary>
    [DisplayName( "SECC | Restore Communications for Reactivated People" )]
    [Description( "Restores original email and SMS preferences for people who have become active again, using snapshots stored in a DefinedType." )]
    [DefinedTypeField( "Inactive Person Communication Overrides Defined Type",
        Description = "The Defined Type used to store original communication preferences for inactive people.",
        IsRequired = true,
        Key = "InactiveCommunicationOverridesDefinedType",
        Order = 0 )]
    [BooleanField( "Dry Run",
        Description = "When enabled, the job reports how many people would be restored without making any changes.",
        DefaultBooleanValue = true,
        Key = "DryRun",
        Order = 1 )]
    [DisallowConcurrentExecution]
    public class RestoreCommunicationsForReactivatedPeople : IJob
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
        public RestoreCommunicationsForReactivatedPeople()
        {
        }

        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var definedTypeGuid = dataMap.GetString( "InactiveCommunicationOverridesDefinedType" ).AsGuid();
            bool dryRun = dataMap.GetString( "DryRun" ).AsBoolean();

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );
            var definedType = DefinedTypeCache.Get( definedTypeGuid );

            if ( definedType == null )
            {
                throw new RockJobWarningException( "Inactive Person Communication Overrides Defined Type not found. Verify the job attribute is configured." );
            }

            // Get the Active record status DefinedValue Id
            var activeStatusCache = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() );
            if ( activeStatusCache == null )
            {
                throw new RockJobWarningException( "Could not find the Active Record Status DefinedValue." );
            }
            var activeStatusId = activeStatusCache.Id;

            // Build a subquery of PersonIds that have tracking entries in the DefinedType
            var trackedPersonIdQuery = definedValueService.Queryable()
                .Where( dv => dv.DefinedTypeId == definedType.Id )
                .Select( dv => dv.Value );

            // Find everyone who is now Active AND has a tracking entry.
            // No lookback filter here — the tracking entry itself scopes the result set,
            // and this ensures nobody gets stranded if the job misses a run.
            var reactivatedPeople = personService.Queryable().AsNoTracking()
                .Where( p => p.RecordStatusValueId == activeStatusId )
                .Where( p => trackedPersonIdQuery.Contains( SqlFunctions.StringConvert( ( double ) p.Id ).Trim() ) )
                .OrderBy( p => p.Id )
                .Select( p => p.Id )
                .ToList();

            // Dry run: report what would happen without making changes
            if ( dryRun )
            {
                context.Result = $"[DRY RUN] Found {reactivatedPeople.Count} reactivated people with tracking entries. No changes were made.";
                return;
            }

            int restoredCount = 0;
            int errorCount = 0;
            int skippedCount = 0;

            foreach ( var personId in reactivatedPeople )
            {
                try
                {
                    var personContext = new RockContext();
                    var pService = new PersonService( personContext );
                    var phService = new PhoneNumberService( personContext );
                    var dvService = new DefinedValueService( personContext );

                    var personEntity = pService.Get( personId );
                    if ( personEntity == null )
                    {
                        skippedCount++;
                        continue;
                    }

                    // Find the tracking DefinedValue for this person
                    var personIdString = personId.ToString();
                    var trackingEntry = dvService.Queryable()
                        .Where( dv => dv.DefinedTypeId == definedType.Id && dv.Value == personIdString )
                        .FirstOrDefault();

                    if ( trackingEntry == null )
                    {
                        skippedCount++;
                        continue;
                    }

                    // Parse the snapshot
                    var snapshot = JsonConvert.DeserializeObject<PreferenceSnapshot>( trackingEntry.Description );
                    if ( snapshot == null )
                    {
                        skippedCount++;
                        continue;
                    }

                    // Restore email preference
                    personEntity.EmailPreference = ( EmailPreference ) snapshot.EmailPreference;

                    // Restore SMS preferences per phone number
                    if ( snapshot.Phones != null )
                    {
                        foreach ( var phoneSnapshot in snapshot.Phones )
                        {
                            var phone = phService.Get( phoneSnapshot.PhoneNumberId );
                            if ( phone != null && phone.PersonId == personEntity.Id )
                            {
                                phone.IsMessagingEnabled = phoneSnapshot.IsMessagingEnabled;
                            }
                        }
                    }

                    // Remove the tracking DefinedValue
                    dvService.Delete( trackingEntry );

                    personContext.SaveChanges();
                    restoredCount++;
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex );
                    errorCount++;
                }
            }

            // Invalidate the DefinedType cache so other code sees the removed entries
            DefinedTypeCache.Remove( definedType.Id );

            var result = $"Evaluated {reactivatedPeople.Count} reactivated people with tracking entries. Restored communications for {restoredCount}. Skipped {skippedCount} (no longer found or snapshot unavailable).";
            if ( errorCount > 0 )
            {
                result += $" {errorCount} errors occurred.";
                throw new RockJobWarningException( result );
            }

            context.Result = result;
        }
    }
}
