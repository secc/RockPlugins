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

namespace org.secc.Jobs
{
    [InteractionChannelField( "Interaction Channel", "The interaction channel which contains the interactions.", true, Rock.SystemGuid.InteractionChannel.WIFI_PRESENCE ) ]
    [KeyValueListField( "Component Campus Mapping", "A mapping between component and campus (use the campus name).", true, "", "Component Name", "Campus") ]
    [TextField( "Operation", "The interaction operation to use for populating attendance.", false, "Present" )]
    [GroupTypeField( "Group Type", "The group type to use for logging attendance against (The campus, and Location Schedules on on active groups of this type will be used).", true) ]
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
            GroupService groupService = new GroupService( rockContext );

            // Load the channel
            InteractionChannelCache channel = InteractionChannelCache.Read( dataMap.GetString( "InteractionChannel" ).AsGuid() );

            // Setup 
            int campusLocationTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.LOCATION_TYPE_CAMPUS ).Id;
            var groupType = GroupTypeCache.Read( dataMap.GetString( "GroupType" ).AsGuid() );
            var groups = groupService.GetByGroupTypeId( groupType.Id );
            string operation = !string.IsNullOrWhiteSpace(dataMap.GetString( "Operation" )) ? dataMap.GetString( "Operation" ) : null;

            // Fetch the job so we can get the last run date/time
            int jobId = Convert.ToInt16( context.JobDetail.Description );
            var jobService = new ServiceJobService( rockContext );
            var job = jobService.Get( jobId );

            DateTime lastRun = job?.LastSuccessfulRunDateTime ?? DateTime.MinValue;

            var componentCampusMapping = dataMap.GetString( "ComponentCampusMapping" ).AsDictionaryOrNull();
            foreach(var componentName in componentCampusMapping.Keys)
            {
                var component = componentService.Queryable().Where( cs => cs.Name.ToLower() == componentName.ToLower() && cs.ChannelId == channel.Id ).FirstOrDefault();
                CampusCache campus = CampusCache.All().Where( c => c.Name == componentCampusMapping[componentName] ).FirstOrDefault();

                if ( campus != null && component != null )
                {
                    Group group = groups.Where( g => g.IsActive == true && g.CampusId == campus.Id ).FirstOrDefault();
                    if ( group?.GroupLocations != null)
                    { 
                        foreach( var gl in group?.GroupLocations)
                        {
                            Location location = gl.Location;
                            foreach( Schedule schedule in gl.Schedules)
                            {
                                var occurrences = schedule.GetOccurrences( DateTime.MinValue, DateTime.Now );
                                foreach ( var occurrence in occurrences )
                                {
                                    DateTime startDate = occurrence.Period.StartTime.Value;
                                    DateTime endDate = occurrence.Period.EndTime.Value;

                                    var peopleAttended = interactionService.Queryable().Where( 
                                        i => i.InteractionComponentId == component.Id && 
                                                i.InteractionDateTime <= endDate && 
                                                i.InteractionEndDateTime >= startDate && 
                                                i.PersonAliasId != null && 
                                                ( i.CreatedDateTime > lastRun || i.PersonalDevice.ModifiedDateTime > lastRun || i.PersonalDevice.CreatedDateTime > lastRun ) && 
                                                ( operation == null || i.Operation == operation ) 
                                    ).Select( i => i.PersonAliasId ).Distinct();
                                    int newAttendance = 0;
                                    foreach ( int personAliasId in peopleAttended )
                                    {
                                        // Make sure we don't already have an attendance Record
                                        if ( !attendanceService.Queryable().Any( a => a.ScheduleId == schedule.Id && a.PersonAliasId == personAliasId && a.GroupId == group.Id && a.LocationId == location.Id && a.DidAttend == true ) )
                                        {
                                            Attendance attendance = new Attendance()
                                            {
                                                PersonAliasId = personAliasId,
                                                CampusId = campus.Id,
                                                GroupId = group.Id,
                                                LocationId = location.Id,
                                                ScheduleId = schedule.Id,
                                                StartDateTime = occurrence.Period.StartTime.Value,
                                                EndDateTime = occurrence.Period?.EndTime?.Value,
                                                DidAttend = true
                                            };
                                            attendanceService.Add( attendance );
                                            newAttendance++;
                                        }
                                    }
                                    if ( newAttendance > 0 )
                                    {
                                        rockContext.SaveChanges();
                                        context.Result += string.Format( "{0} people attended {1} on {2} (Component {3}).\n", newAttendance, campus.Name, occurrence.Period.StartTime.Value.ToString( "MM/dd/yyyy h:mm tt" ), component.Name );
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
