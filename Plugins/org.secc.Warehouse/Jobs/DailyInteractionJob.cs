using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using org.secc.Warehouse.Model;
using Quartz;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock;

namespace org.secc.Warehouse.Jobs
{
    /// <summary>
    /// Job to add daily interactions to the database
    /// </summary>

    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,"Member Defined Values","",true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Attendee Defined Values", "", true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Prospect Defined Values", "", true)]
    [DataViewField("Staff DataView","",true,"Rock.Model.Person")]
    [DisallowConcurrentExecution]
    public class DailyInteractionJob : IJob
    {
        JobDataMap dataMap;

        /// <summary>
        /// Job that will add daily interactions to the database.
        /// 
        /// Called by the <see cref="IScheduler" /> when a
        /// <see cref="ITrigger" /> fires that is associated with
        /// the <see cref="IJob" />.
        /// </summary>
        public void Execute( IJobExecutionContext context )
        {
            dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            int jobId = Convert.ToInt16( context.JobDetail.Description );
            var jobService = new ServiceJobService( rockContext );
            var job = jobService.Get( jobId );

            InteractionService interactionService = new InteractionService( rockContext );

            DateTime? lastRun = job?.LastSuccessfulRunDateTime;

            //Get first interaction datetime if the job has never successfully run.
            if ( !lastRun.HasValue )
            {
                lastRun = interactionService.Queryable()
                      .OrderBy( i => i.InteractionDateTime )
                      .Select( i => i.InteractionDateTime )
                      .FirstOrDefault();
            }

            if ( !lastRun.HasValue )
            {
                return;
            }

            while ( lastRun.Value.Date < Rock.RockDateTime.Today )
            {
                WarehouseInteractionForDate( lastRun.Value.Date );
                lastRun = lastRun.Value.Date.AddDays( 1 );
            }
        }

        private void WarehouseInteractionForDate( DateTime dayOf )
        {
            var rockContext = new RockContext();
            InteractionService interactionService = new InteractionService( rockContext );
            InteractionComponentService interactionComponentService = new InteractionComponentService( rockContext );

            var pageEntityTypeId = EntityTypeCache.GetId( typeof( Page ) );

            var interactionComponentsQry = interactionComponentService.Queryable()
                .Where( ic => ic.InteractionChannel.ComponentEntityTypeId == pageEntityTypeId );

            var debug = interactionComponentsQry.ToList();

            var nextDay = dayOf.AddDays( 1 );

            var interactionsQry = interactionService.Queryable()
                .Where( i => i.InteractionDateTime >= dayOf && i.InteractionDateTime < nextDay );

            var debug2 = interactionsQry.ToList();

            var dataViewAttributeGuid = dataMap.GetString("StaffDataView").AsGuid();
            var dataViewService = new DataViewService(rockContext);
            var staffIds = new List<int>();
            if (dataViewAttributeGuid != Guid.Empty)
            {
                var dataView = dataViewService.Get(dataViewAttributeGuid);
                if (dataView != null)
                {
                    var qry = dataView.GetQuery(new DataViewGetQueryArgs { DatabaseTimeoutSeconds = 30 } );
                    if (qry != null)
                    {
                        staffIds = qry.Select(e => e.Id).ToList();
                    }
                }
            }

            var memberDefinedValues = dataMap.GetString("MemberDefinedValues").SplitDelimitedValues();
            var memberDefinedValueGuids = memberDefinedValues.Select(Guid.Parse).ToList();
            var memberDefinedValueQry = new DefinedValueService(rockContext).Queryable().Where(a => memberDefinedValueGuids.Contains(a.Guid));
            var memberDefinedValueIds = memberDefinedValueQry.Select(s => s.Id).ToList();

            var attendeeDefinedValues = dataMap.GetString("AttendeeDefinedValues").SplitDelimitedValues();
            var attendeeDefinedValueGuids = memberDefinedValues.Select(Guid.Parse).ToList();
            var attendeeDefinedValueQry = new DefinedValueService(rockContext).Queryable().Where(a => attendeeDefinedValueGuids.Contains(a.Guid));
            var attendeeDefinedValueIds = attendeeDefinedValueQry.Select(s => s.Id).ToList();

            var interactionCounts = interactionComponentsQry
                .GroupJoin(interactionsQry,
                    ic => ic.Id,
                    i => i.InteractionComponentId,
                    (ic, i) => new
                    {
                        PageId = ic.EntityId,
                        Total = i.Count(),
                        StaffVisitors = i.Where(i2 => i2.PersonAlias != null && staffIds.Contains(i2.PersonAlias.PersonId)).Select(i2 => i2.PersonAlias.PersonId).Distinct().Count(),
                        MemberVisitors = i.Where(i2 => i2.PersonAlias != null && memberDefinedValueIds.Contains(i2.PersonAlias.Person.ConnectionStatusValueId ?? 0) && !staffIds.Contains(i2.PersonAlias.PersonId)).Select(i2 => i2.PersonAlias.PersonId).Distinct().Count(),
                        AttendeeVisitors = i.Where(i2 => i2.PersonAlias != null && attendeeDefinedValueIds.Contains(i2.PersonAlias.Person.ConnectionStatusValueId ?? 0) && !staffIds.Contains(i2.PersonAlias.PersonId)).Select(i2 => i2.PersonAlias.PersonId).Distinct().Count(),
                        ProspectVisitors = i.Where(i2 => i2.PersonAlias != null && !staffIds.Contains(i2.PersonAlias.PersonId) && !memberDefinedValueIds.Contains(i2.PersonAlias.PersonId) && !attendeeDefinedValueIds.Contains(i2.PersonAlias.PersonId)).Select(i2 => i2.PersonAlias.PersonId).Distinct().Count(),
                        AnonymousVisitors = i.Where(i2 => i2.PersonAlias == null).Select(i2 => i2.InteractionSessionId).Distinct().Count()
                    }
                )
                .ToList();

            var pageIds = interactionCounts.Select(p => p.PageId).Distinct();

            foreach (var pageId in pageIds)
            {
                if (pageId == null)
                {
                    continue;
                }

                var loopRockContext = new RockContext();
                DailyInteractionService dailyInteractionService = new DailyInteractionService(loopRockContext);

                var dailyInteraction = new DailyInteraction
                {
                    Date = dayOf,
                    Visits = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.Total),
                    PageId = pageId.Value,
                    StaffVisitors = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.StaffVisitors),
                    MemberVisitors = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.MemberVisitors),
                    AttendeeVisitors = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.AttendeeVisitors),
                    ProspectVisitors = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.ProspectVisitors),
                    AnonymousVisitors = interactionCounts.Where(v => v.PageId == pageId).Sum(v => v.AnonymousVisitors)
                };

                dailyInteractionService.Add(dailyInteraction);
                loopRockContext.SaveChanges();
            }

        }
    }
}
