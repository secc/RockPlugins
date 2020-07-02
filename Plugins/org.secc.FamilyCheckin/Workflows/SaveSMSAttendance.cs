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
using System.ComponentModel.Composition;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Saves the selected check-in data as attendance
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Uses information from SMS data to match keywords to group attribute values." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Save SMS Attendance" )]

    [WorkflowAttribute( "Person", "Workflow attribute that contains the person to check-in.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.PersonFieldType" } )]

    [WorkflowAttribute( "SMS Keyword", "Workflow attribute that contains the keyword.", true, "", "", 1, null,
        new string[] { "Rock.Field.Types.TextFieldType" } )]

    [CustomRadioListField( "Keyword Search Type", "Method for finding correct group. Ignore schedule will select the first group with matching keyword. Closest schedule will select the closest schedule looking forward 2 hours and back by 1 week. During schedule will only check-in to groups during the schedule or check-in window.",
        "0^Ignore Schedule,1^Closest Schedule,2^During Schedule", true, "0", "", 2 )]

    [WorkflowAttribute( "Keyword Attribute", "Workflow attribute that contains the attribute for keywords.", true, "", "", 3, null,
        new string[] { "Rock.Field.Types.AttributeFieldType" } )]

    [CustomRadioListField( "Member Options", "How to handle attendance based on membership.",
        "0^Only Add Attendance,1^Add Attendance And Member If Not In Group,2^Add Attendance Only If Member", true, "0", "", 4 )]

    [WorkflowAttribute( "Group Output", "Workflow attrribute that contains the group the person was checked in to.", true, "", "", 5, null,
        new string[] { "Rock.Field.Types.GroupFieldType" } )]

    [WorkflowAttribute( "Location Output", "Workflow attrribute that contains the location the person was checked in to.", true, "", "", 6, null,
        new string[] { "Rock.Field.Types.LocationFieldType" } )]

    [WorkflowAttribute( "Schedule Output", "Workflow attrribute that contains the schedule the person was checked in to.", true, "", "", 7, null,
        new string[] { "Rock.Field.Types.ScheduleFieldType" } )]


    public class SaveSMSAttendance : ActionComponent
    {
        private List<Group> groups;
        private WorkflowAction action;
        private List<string> errorMessages;
        private RockContext rockContext;
        private string personAliasGuid;
        private string smsKeyword;
        private string keywordAttribute;
        private string keywordSearchType;


        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext wfrockContext, Rock.Model.WorkflowAction wfaction, Object entity, out List<string> wferrorMessages )
        {
            errorMessages = new List<string>();
            wferrorMessages = errorMessages;
            action = wfaction;
            rockContext = wfrockContext;

            personAliasGuid = GetAttributeValue( action, "Person", true );
            smsKeyword = GetAttributeValue( action, "SMSKeyword", true );
            keywordAttribute = GetAttributeValue( action, "KeywordAttribute", true );
            keywordSearchType = GetAttributeValue( action, "KeywordSearchType", true );

            var attribute = AttributeCache.Get( keywordAttribute.AsGuid() );
            if ( attribute == null || attribute.EntityTypeId != EntityTypeCache.Get( typeof( Group ) ).Id )
            {
                errorMessages.Add( "Attribute must not be null and must for Group" );
            }

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            var groupServiceQueryable = groupService.Queryable();

            groups = attributeValueService.Queryable()
                .Where( av => av.AttributeId == attribute.Id )
                .Join( groupServiceQueryable,
                av => av.EntityId,
                g => g.Id,
                ( av, g ) => g
                ).ToList();


            switch ( keywordSearchType )
            {
                case "0":
                    IgnoreSchedule();
                    break;
                case "1":
                    ClosestSchedule();
                    break;
                case "2":
                    DuringSchedule();
                    break;
                default:
                    break;
            }

            return true;
        }

        private void DuringSchedule()
        {
            Group checkinGroup = null;
            Location checkinLocation = null;
            Schedule checkinSchedule = null;


            foreach ( var group in groups )
            {
                if ( checkinGroup != null )
                {
                    break;
                }

                var groupLocations = group.GroupLocations;
                foreach ( var groupLocation in groupLocations )
                {
                    if ( checkinGroup != null )
                    {
                        break;
                    }

                    var locationDateTime = RockDateTime.Now;
                    if ( groupLocation.Location.CampusId.HasValue )
                    {
                        locationDateTime = CampusCache.Get( groupLocation.Location.CampusId.Value )?.CurrentDateTime ?? RockDateTime.Now;
                    }

                    var schedules = groupLocation.Schedules;
                    foreach ( var schedule in schedules )
                    {
                        if ( checkinGroup != null )
                        {
                            break;
                        }

                        if ( schedule.WasScheduleOrCheckInActive( locationDateTime ) )
                        {
                            checkinSchedule = schedule;
                            checkinLocation = groupLocation.Location;
                            checkinGroup = group;
                            break;
                        }
                    }
                }
            }

            if ( checkinGroup != null && checkinSchedule != null && checkinLocation != null )
            {
                SaveAttendance( checkinGroup, checkinLocation, checkinSchedule );
            }
        }

        private void IgnoreSchedule()
        {
            var group = groups.FirstOrDefault();
            if ( group == null )
            {
                return;
            }

            Location location = null;
            Schedule schedule = null;
            if ( group.GroupLocations.Any() )
            {
                location = group.GroupLocations.FirstOrDefault().Location;
                schedule = group.GroupLocations.FirstOrDefault().Schedules.FirstOrDefault();
            }

            if ( schedule == null )
            {
                schedule = group.Schedule;
            }

            if ( group != null )
            {
                SaveAttendance( group, location, schedule );
            }
        }


        private void ClosestSchedule()
        {
            var distances = new List<GLSDistance>();
            foreach ( var group in groups )
            {
                if ( group.Schedule != null )
                {
                    distances.Add( new GLSDistance { Group = group, Schedule = group.Schedule, Distance = GetScheduleDistance( group.Schedule ) } );
                }
                foreach ( var groupLocation in group.GroupLocations )
                {
                    foreach ( var schedule in groupLocation.Schedules )
                    {
                        distances.Add( new GLSDistance
                        {
                            Group = group,
                            Location = groupLocation.Location,
                            Schedule = schedule,
                            Distance = GetScheduleDistance( schedule )
                        } );
                    }
                }
            }

            var closest = distances.OrderBy( d => d.Distance ).FirstOrDefault();
            if ( closest != null )
            {
                SaveAttendance( closest.Group, closest.Location, closest.Schedule );
            }
        }



        private int GetScheduleDistance( Schedule schedule )
        {
            if ( schedule.iCalendarContent.IsNotNullOrWhiteSpace() )
            {
                var nextStart = schedule.GetNextStartDateTime( Rock.RockDateTime.Now );
                if ( nextStart.HasValue )
                {
                    var future = nextStart.Value - Rock.RockDateTime.Now;
                    var minutes = future.TotalMinutes;
                    if ( minutes <= 120 )
                    {
                        return ( int ) minutes;
                    }
                }

                var lastStart = schedule.GetNextStartDateTime( Rock.RockDateTime.Now.AddDays( -7 ) );
                if ( lastStart.HasValue )
                {
                    var past = Rock.RockDateTime.Now - lastStart.Value;
                    var minutes = past.TotalMinutes;
                    return ( int ) minutes;
                }
            }

            if ( schedule.WeeklyDayOfWeek.HasValue && schedule.WeeklyTimeOfDay.HasValue )
            {
                if ( Rock.RockDateTime.Now.DayOfWeek == schedule.WeeklyDayOfWeek
                    && ( schedule.WeeklyTimeOfDay - Rock.RockDateTime.Now.TimeOfDay ).Value.Minutes <= 120 )
                {
                    return ( schedule.WeeklyTimeOfDay - Rock.RockDateTime.Now.TimeOfDay ).Value.Minutes;
                }
                var lastDow = schedule.WeeklyDayOfWeek.Value.ConvertToInt();
                var today = Rock.RockDateTime.Now.DayOfWeek.ConvertToInt();
                var days = 7;

                if ( lastDow > today )
                {
                    days = today + 7 - lastDow;
                }
                else
                {
                    days = today - lastDow;
                }
                var minutes = days * 1440;

                minutes += ( Rock.RockDateTime.Now.TimeOfDay - schedule.WeeklyTimeOfDay.Value ).Minutes;
                return minutes;
            }

            return int.MaxValue;
        }

        private void SaveAttendance( Group group, Location location, Schedule schedule )
        {
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            var personAlias = personAliasService.Get( personAliasGuid.AsGuid() );

            if ( personAlias == null )
            {
                errorMessages.Add( "No Person Found" );
                return;
            };

            var memberOptions = keywordSearchType = GetAttributeValue( action, "MemberOptions", true );

            if ( memberOptions != "0" ) //Not just add attendance
            {

                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                // get group member
                var groupMember = groupMemberService.Queryable()
                                        .Where( m => m.GroupId == group.Id
                                             && m.PersonId == personAlias.PersonId )
                                        .FirstOrDefault();
                if ( groupMember == null )
                {
                    if ( memberOptions == "1" ) //Create new member
                    {


                        if ( group != null )
                        {
                            groupMember = new GroupMember();
                            groupMember.GroupId = group.Id;
                            groupMember.PersonId = personAlias.PersonId;
                            groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                            groupMember.GroupRole = group.GroupType.DefaultGroupRole;
                            groupMemberService.Add( groupMember );
                            rockContext.SaveChanges();
                        }
                    }
                    else //Must be in group
                    {
                        action.AddLogEntry( string.Format( "{0} was not a member of the group {1} and the action was not configured to add them.", personAlias.Person.FullName, group.Name ) );
                        return;
                    }
                }
            }


            //Save attendance
            AttendanceService attendanceService = new AttendanceService( rockContext );
            attendanceService.AddOrUpdate( personAlias.Id, Rock.RockDateTime.Now, group.Id, location?.Id, schedule?.Id, location?.CampusId );

            //Set Attributes for Group Location and Schedule
            var groupAttribute = AttributeCache.Get( GetAttributeValue( action, "GroupOutput" ).AsGuid(), rockContext );
            if ( groupAttribute != null )
            {
                SetWorkflowAttributeValue( action, groupAttribute.Guid, group.Guid.ToString() );
            }

            var locationAttribute = AttributeCache.Get( GetAttributeValue( action, "LocationOutput" ).AsGuid(), rockContext );
            if ( locationAttribute != null && location != null )
            {
                SetWorkflowAttributeValue( action, locationAttribute.Guid, location.Guid.ToString() );
            }

            var scheduleAttribute = AttributeCache.Get( GetAttributeValue( action, "ScheduleOutput" ).AsGuid(), rockContext );
            if ( scheduleAttribute != null && schedule != null )
            {
                SetWorkflowAttributeValue( action, scheduleAttribute.Guid, schedule.Guid.ToString() );
            }
        }

        private class GLSDistance
        {
            public int Distance { get; set; }
            public Group Group { get; set; }
            public Location Location { get; set; }
            public Schedule Schedule { get; set; }
        }
    }
}