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
using System.Linq;
using System.Data;
using System.Data.Entity;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System;
using Rock.Communication;
using static Rock.Attribute.MetricCategoriesFieldAttribute;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using DotLiquid;

namespace org.secc.Jobs
{
    [MetricCategoriesField( "Metrics", "The metric categories to include in this email communication.", true )]
    [SlidingDateRangeField( "Date Range", "The date range to review metric entries.", true ) ]
    [CategoryField( "Schedule Categories", "The schedule categories to use for list of service times.  Note that this requires a campus attribute to be selected below.", true, "Rock.Model.Schedule" )]
    [AttributeField( Rock.SystemGuid.EntityType.SCHEDULE, "Campus Attribute", "The campus attribute to use for filtering the schedules" )]
    [GroupField( "Notification Group", "The group of people to notify about the metric entry progress", true )]
    [SystemEmailField( "Email", "The email to send to the connectors", true )]

    [DisallowConcurrentExecution]
    public class MetricsDigest : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();
            var jobService = new ServiceJobService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            CategoryService categoryService = new CategoryService( rockContext );
            MetricService metricService = new MetricService( rockContext );

            var metricCategories = MetricCategoriesFieldAttribute.GetValueAsGuidPairs( dataMap.GetString( "Metrics" ) );
            DateRange dateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) );

            var systemEmail = dataMap.GetString( "Email" ).AsGuidOrNull();
            if (!systemEmail.HasValue)
            {
                throw new Exception( "System Email is required!" );
            }

            // get job type id
            int jobId = Convert.ToInt16( context.JobDetail.Description );
            var job = jobService.Get( jobId );

            DateTime _midnightToday = RockDateTime.Today.AddDays( 1 );
            var currentDateTime = RockDateTime.Now;
            int recipients = 0;

            Group notificationGroup = groupService.Get( dataMap.GetString( "NotificationGroup" ).AsGuid() );
            List<MetricCount> metricCounts = CampusCache.All().Select(c => new MetricCount() { Campus = c, TotalEntered = 0, TotalMetrics = 0 } ).ToList();
            List<Metric> metrics = new List<Metric>();


            // If we have some reasonable data, go ahead and run the job
            if (notificationGroup != null && metricCategories.Count > 0 && dateRange.Start.HasValue && dateRange.End.HasValue)
            {
                foreach(MetricCategoryPair metricCategoryPair in metricCategories)
                {
                    Metric metric = metricService.Get( metricCategoryPair.MetricGuid );
                    metrics.Add( metric );
                    // Split this by campus partition
                    if (metric.MetricPartitions.Any(mp => mp.EntityType.Name.Contains("Campus")))
                    {
                        foreach(CampusCache campus in CampusCache.All())
                        {
                            // Check to see if we also have a schedule partition
                            if (metric.MetricPartitions.Any(mp => mp.EntityType.Name.Contains("Schedule")))
                            {
                                var services = GetServices( campus, dataMap, dateRange );
                                metricCounts.Where( mc => mc.Campus == campus ).FirstOrDefault().TotalMetrics += services.Count;
                                foreach ( var service in services ) {
                                    var hasValues = metric.MetricValues.Where( mv =>
                                        mv.MetricValuePartitions.Any( mvp => mvp.MetricPartition.EntityType.Name.Contains( "Campus" ) && mvp.EntityId == campus.Id )
                                        && mv.MetricValuePartitions.Any( mvp => mvp.MetricPartition.EntityType.Name.Contains( "Schedule" ) && mvp.EntityId == service.Id )
                                        && mv.MetricValueDateTime >= dateRange.Start.Value
                                        && mv.MetricValueDateTime <= dateRange.End.Value ).Any();
                                    if ( hasValues )
                                        metricCounts.Where( mc => mc.Campus == campus ).FirstOrDefault().TotalEntered++;
                                }
                            }
                            else
                            {
                                // Add totals for metrics and, if values are entered, metrics entered.
                                metricCounts.Where( mc => mc.Campus == campus ).FirstOrDefault().TotalMetrics++;
                                var hasValues = metric.MetricValues.Where( mv => mv.MetricValuePartitions.Any( mvp => mvp.MetricPartition.EntityType.Name.Contains( "Campus" ) && mvp.EntityId == campus.Id ) && mv.MetricValueDateTime >= dateRange.Start.Value && mv.MetricValueDateTime <= dateRange.End.Value ).Any();
                                if ( hasValues )
                                    metricCounts.Where( mc => mc.Campus == campus ).FirstOrDefault().TotalEntered++;

                            }
                        }
                    }
                }

                // Create the merge fields
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "MetricCounts", metricCounts );
                mergeFields.Add( "Metrics", metrics );
                mergeFields.Add( "DateRange", dateRange.ToString() );
                mergeFields.Add( "LastRunDate", job.LastSuccessfulRunDateTime );

                // Setup the email and send it out!
                RockEmailMessage message = new RockEmailMessage( systemEmail.Value );
                message.AdditionalMergeFields = mergeFields;
                foreach ( GroupMember member in notificationGroup.Members )
                {
                    message.AddRecipient( member.Person.Email );
                    recipients++;
                }
                message.SendSeperatelyToEachRecipient = true;
                message.Send();
            }

            context.Result = string.Format( "Sent "+ recipients + " metric entry digest emails." );
        }


        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <returns></returns>
        private List<Schedule> GetServices( CampusCache campus, JobDataMap dataMap, DateRange dateRange )
        {
            var services = new List<Schedule>();
            if ( !string.IsNullOrWhiteSpace( dataMap.GetString( "ScheduleCategories" ) ) )
            {
                List<Guid> categoryGuids = dataMap.GetString( "ScheduleCategories" ).Split( ',' ).Select( g => g.AsGuid() ).ToList();
                string campusAttributeGuid = dataMap.GetString( "CampusAttribute" );
                
                using ( var rockContext = new RockContext() )
                {
                    var attributeValueQry = new AttributeValueService( rockContext ).Queryable();
                    foreach ( Guid categoryGuid in categoryGuids )
                    {

                        var scheduleCategory = CategoryCache.Read( categoryGuid );
                        if ( scheduleCategory != null && campus != null )
                        {
                            var schedules = new ScheduleService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( s => s.CategoryId == scheduleCategory.Id )
                                .Join(
                                    attributeValueQry.Where( av => av.Attribute.Guid.ToString() == campusAttributeGuid
                                                                    && av.Value.Contains( campus.Guid.ToString() ) ),
                                    p => p.Id,
                                    av => av.EntityId,
                                    ( p, av ) => new { Schedule = p, Value = av.Value } );
                            // Check to see if the event was applicable the week for which we are entering data
                            foreach ( var schedule in schedules )
                            {
                                var occurrences = ScheduleICalHelper.GetOccurrences( schedule.Schedule.GetCalenderEvent(), dateRange.Start.Value, dateRange.End.Value );
                                if ( occurrences.Count > 0 )
                                {
                                    services.Add( schedule.Schedule );
                                }
                            }

                        }
                        else if ( scheduleCategory != null )
                        {
                            foreach ( var schedule in new ScheduleService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( s =>
                                    s.CategoryId.HasValue &&
                                    s.CategoryId.Value == scheduleCategory.Id )
                                .OrderBy( s => s.Name ) )
                            {
                                services.Add( schedule );
                            }
                        }
                    }
                }
            }
            return services.OrderBy( s => s.NextStartDateTime.HasValue ? s.NextStartDateTime.Value.Ticks : s.EffectiveEndDate.HasValue ? s.EffectiveEndDate.Value.Ticks : 0 ).ToList();
        }

    }

    public class MetricCount : Drop
    {
        public CampusCache Campus { get; set; }

        public int TotalEntered { get; set; }

        public int TotalMetrics { get; set; }
    }
}
