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
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Saves the selected check-in data as attendance
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Uses information from SMS data " )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Save SMS Attendance" )]

    [WorkflowAttribute( "Person", "Workflow attribute that contains the person to check-in.", true, "", "", 0, null,
        new string[] { "Rock.Field.Types.PersonFieldType" } )]

    [WorkflowAttribute( "SMS Keyword", "Workflow attribute that contains the keyword.", true, "", "", 1, null,
        new string[] { "Rock.Field.Types.TextFieldType" } )]

    [WorkflowAttribute( "Keyword Attribute", "Workflow attribute that contains the attribute for keywords.", true, "", "", 2, null,
        new string[] { "Rock.Field.Types.AttributeFieldType" } )]

    [WorkflowAttribute( "Group Output", "Workflow attrribute that contains the group the person was checked in to.", true, "", "", 3, null,
        new string[] { "Rock.Field.Types.GroupFieldType" } )]

    [WorkflowAttribute( "Location Output", "Workflow attrribute that contains the location the person was checked in to.", true, "", "", 4, null,
        new string[] { "Rock.Field.Types.LocationFieldType" } )]

    [WorkflowAttribute( "Schedule Output", "Workflow attrribute that contains the schedule the person was checked in to.", true, "", "", 5, null,
        new string[] { "Rock.Field.Types.ScheduleFieldType" } )]


    public class SaveSMSAttendance : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, Rock.Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            string personAliasGuid = GetAttributeValue( action, "Person", true );
            string smsKeyword = GetAttributeValue( action, "SMSKeyword", true );
            string keywordAttribute = GetAttributeValue( action, "KeywordAttribute", true );

            var attribute = AttributeCache.Get( keywordAttribute.AsGuid() );
            if ( attribute == null || attribute.EntityTypeId != EntityTypeCache.Get( typeof( Group ) ).Id )
            {
                errorMessages.Add( "Attribute must not be null and must for Group" );
            }

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            var groupServiceQueryable = groupService.Queryable();

            var groups = attributeValueService.Queryable()
                .Where( av => av.AttributeId == attribute.Id )
                .Join( groupServiceQueryable,
                av => av.EntityId,
                g => g.Id,
                ( av, g ) => g
                ).ToList();

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
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                var personAlias = personAliasService.Get( personAliasGuid.AsGuid() );

                if ( personAlias == null )
                {
                    errorMessages.Add( "No Person Found" );
                    return true;
                };

                //Save attendance
                AttendanceService attendanceService = new AttendanceService( rockContext );
                attendanceService.AddOrUpdate( personAlias.Id, Rock.RockDateTime.Now, checkinGroup.Id, checkinLocation.Id, checkinSchedule.Id, checkinLocation.CampusId );

                //Set Attributes for Group Location and Schedule
                var groupAttribute = AttributeCache.Get( GetAttributeValue( action, "GroupOutput" ).AsGuid(), rockContext );
                if ( groupAttribute != null )
                {
                    SetWorkflowAttributeValue( action, groupAttribute.Guid, checkinGroup.Guid.ToString() );
                }


                var locationAttribute = AttributeCache.Get( GetAttributeValue( action, "LocationOutput" ).AsGuid(), rockContext );
                if ( locationAttribute != null )
                {
                    SetWorkflowAttributeValue( action, locationAttribute.Guid, checkinLocation.Guid.ToString() );
                }

                var scheduleAttribute = AttributeCache.Get( GetAttributeValue( action, "ScheduleOutput" ).AsGuid(), rockContext );
                if ( scheduleAttribute != null )
                {
                    SetWorkflowAttributeValue( action, scheduleAttribute.Guid, checkinSchedule.Guid.ToString() );
                }
            }

            return true;
        }
    }
}