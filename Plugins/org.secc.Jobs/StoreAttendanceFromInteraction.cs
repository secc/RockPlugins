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
using System;
using Rock.Web.Cache;
using System.Collections.Generic;

namespace org.secc.Jobs
{
    [InteractionChannelField( "Interaction Channel", "The interaction channel which contains the interactions.", true, Rock.SystemGuid.InteractionChannel.WIFI_PRESENCE ) ]
    [KeyValueListField( "Component Campus Mapping", "A mapping between component and campus (use the campus name).", true, "", "Component Name", "Campus") ]
    [TextField( "Operation", "The interaction operation to use for populating attendance.", false, "Present" )]
    [GroupTypeField( "Group Type", "The group type to use for logging attendance against (The campus, and Location Schedules on on active groups of this type will be used).", true) ]
    [SlidingDateRangeField( "Date Range", "The date range in which the interactions were made.", true, "Last|365|Day||" )]
    [DisallowConcurrentExecution]
    public class StoreAttendanceFromInteraction : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            InteractionChannelService channelService = new InteractionChannelService( rockContext );
            InteractionComponentService componentService = new InteractionComponentService( rockContext );
            InteractionService interactionService = new InteractionService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );
            LocationService locationService = new LocationService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );
            AttendanceOccurrenceService attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            // Load the channel
            InteractionChannelCache channel = InteractionChannelCache.Get( dataMap.GetString( "InteractionChannel" ).AsGuid() );

            // Setup 
            int campusLocationTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.LOCATION_TYPE_CAMPUS ).Id;
            var groupType = GroupTypeCache.Get( dataMap.GetString( "GroupType" ).AsGuid() );
            var groupLocations = groupService.GetByGroupTypeId( groupType.Id ).Where( g => g.IsActive == true ).SelectMany(g => g.GroupLocations).ToList();
            string operation = !string.IsNullOrWhiteSpace(dataMap.GetString( "Operation" )) ? dataMap.GetString( "Operation" ) : null;

            // Fetch the job so we can get the last run date/time
            int jobId = Convert.ToInt16( context.JobDetail.Description );
            var jobService = new ServiceJobService( rockContext );
            var job = jobService.Get( jobId );

            DateTime lastRun = job?.LastSuccessfulRunDateTime ?? DateTime.MinValue;
            var componentCampusMapping = dataMap.GetString( "ComponentCampusMapping" ).AsDictionaryOrNull();
            DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-1||" );

            // Flip the component campus mapping around and translate to ids
            Dictionary<int, List<int>> campusComponentIds= new Dictionary<int, List<int>>();
            foreach( CampusCache campus in CampusCache.All() )
            {
                var componentNames = componentCampusMapping.Where( ccm => ccm.Value == campus.Name ).Select( c => c.Key.ToLower() );
                campusComponentIds[ campus.Id ] = componentService.Queryable().Where( cs => componentNames.Contains( cs.Name.ToLower() ) && cs.ChannelId == channel.Id ).Select( c => c.Id ).ToList();
            }

            foreach ( GroupLocation gl in groupLocations )
            {
                if ( gl.Group.CampusId.HasValue )
                {
                    Location location = gl.Location;
                    List<int> componentIds = campusComponentIds[gl.Group.CampusId.Value];

                    foreach ( Schedule schedule in gl.Schedules )
                    {
                        var occurrences = schedule.GetOccurrences( dateRange.Start.Value, dateRange.End.Value );
                        foreach ( var occurrence in occurrences )
                        {
                            DateTime startDate = occurrence.Period.StartTime.Value;
                            DateTime endDate = occurrence.Period.EndTime.Value;

                            var peopleAttended = interactionService.Queryable().Where(
                                i => componentIds.Contains( i.InteractionComponentId ) &&
                                        i.InteractionDateTime <= endDate &&
                                        i.InteractionEndDateTime >= startDate &&
                                        i.PersonAliasId != null &&
                                        ( i.CreatedDateTime > lastRun || i.PersonalDevice.ModifiedDateTime > lastRun || i.PersonalDevice.CreatedDateTime > lastRun ) &&
                                        ( operation == null || i.Operation == operation )
                            ).Select( i => i.PersonAliasId ).Distinct();
                            int newAttendance = 0;

                            var occurrenceModel = attendanceOccurrenceService.Get( occurrence.Period.StartTime.Value.Date, gl.GroupId, location.Id, schedule.Id );

                            // Make sure we don't already have an attendance Record
                            var existingAttendees = attendanceOccurrenceService.Queryable().Where( ao => DbFunctions.TruncateTime( ao.OccurrenceDate ) == occurrence.Period.StartTime.Value.Date && ao.ScheduleId == schedule.Id && ao.GroupId == gl.GroupId && ao.LocationId == location.Id ).SelectMany(a => a.Attendees).Where(a => a.DidAttend == true ).Select( a => a.PersonAliasId );
                            foreach ( int personAliasId in peopleAttended.Except( existingAttendees ) )
                            {
                                // Check to see if an occurrence exists already
                                if ( occurrenceModel == null )
                                {
                                    var attendance = attendanceService.AddOrUpdate( personAliasId, occurrence.Period.StartTime.Value, gl.GroupId, location.Id, schedule.Id, gl.Group.CampusId );

                                    attendance.EndDateTime = occurrence.Period?.EndTime?.Value;
                                    attendance.DidAttend = true;
                                    attendance.CampusId = gl.Group.CampusId;
                                    occurrenceModel = attendance.Occurrence;
                                }
                                else
                                {
                                    Attendance attendance = new Attendance();
                                    attendance.PersonAliasId = personAliasId;
                                    attendance.OccurrenceId = occurrenceModel.Id;
                                    attendance.StartDateTime = occurrence.Period.StartTime.Value;
                                    attendance.EndDateTime = occurrence.Period?.EndTime?.Value;
                                    attendance.DidAttend = true;
                                    attendance.CampusId = gl.Group.CampusId;
                                    attendanceService.Add( attendance );
                                }

                                newAttendance++;
                            }
                            if ( newAttendance > 0 )
                            {
                                rockContext.SaveChanges();
                                context.Result += string.Format( "{0} people attended {1} on {2}.\n", newAttendance, gl.Group.Campus.Name, occurrence.Period.StartTime.Value.ToString( "MM/dd/yyyy h:mm tt" ) );
                            }
                        }
                    }
                }
            }
        }
    }
}
