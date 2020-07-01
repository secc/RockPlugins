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
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
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
    [Description( "Saves the selected check-in data as attendance" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Save Attendance Custom" )]

    [BooleanField( "Is Mobile", "If this is a mobile check-in and needs to have its attendances set as RSVP and stored in an entity set, set true.", false )]
    public class SaveAttendance : CheckInActionComponent
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
            var isMobile = GetAttributeValue( action, "IsMobile" ).AsBoolean();
            var mobileDidAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_DID_ATTEND ).Id;
            var mobileNotAttendId = DefinedValueCache.Get( Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND ).Id;

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                AttendanceCode attendanceCode = null;
                DateTime startDateTime = Rock.RockDateTime.Now;
                DateTime today = startDateTime.Date;
                DateTime tomorrow = startDateTime.AddDays( 1 );

                bool reuseCodeForFamily = checkInState.CheckInType != null && checkInState.CheckInType.ReuseSameCode;
                int securityCodeLength = checkInState.CheckInType != null ? checkInState.CheckInType.SecurityCodeAlphaNumericLength : 3;

                var attendanceCodeService = new AttendanceCodeService( rockContext );
                var attendanceService = new AttendanceService( rockContext );
                var groupMemberService = new GroupMemberService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );

                //This list is just for mobile check-in
                List<Attendance> attendances = new List<Attendance>();

                var family = checkInState.CheckIn.CurrentFamily;
                if ( family != null )
                {
                    foreach ( var person in family.GetPeople( true ) )
                    {
                        if ( reuseCodeForFamily && attendanceCode != null )
                        {
                            person.SecurityCode = attendanceCode.Code;
                        }
                        else
                        {
                            attendanceCode = AttendanceCodeService.GetNew( securityCodeLength );
                            person.SecurityCode = attendanceCode.Code;
                        }

                        foreach ( var groupType in person.GetGroupTypes( true ) )
                        {
                            foreach ( var group in groupType.GetGroups( true ) )
                            {
                                if ( groupType.GroupType.AttendanceRule == AttendanceRule.AddOnCheckIn &&
                                    groupType.GroupType.DefaultGroupRoleId.HasValue &&
                                    !groupMemberService.GetByGroupIdAndPersonId( group.Group.Id, person.Person.Id, true ).Any() )
                                {
                                    var groupMember = new GroupMember();
                                    groupMember.GroupId = group.Group.Id;
                                    groupMember.PersonId = person.Person.Id;
                                    groupMember.GroupRoleId = groupType.GroupType.DefaultGroupRoleId.Value;
                                    groupMemberService.Add( groupMember );
                                }

                                foreach ( var location in group.GetLocations( true ) )
                                {
                                    foreach ( var schedule in location.GetSchedules( true ) )
                                    {
                                        var primaryAlias = personAliasService.GetPrimaryAlias( person.Person.Id );
                                        if ( primaryAlias != null )
                                        {

                                            // If a like attendance service exists close it before creating another one.
                                            var oldAttendance = attendanceService.Queryable()
                                            .Where( a =>
                                                a.StartDateTime >= today &&
                                                a.StartDateTime < tomorrow &&
                                                a.Occurrence.LocationId == location.Location.Id &&
                                                a.Occurrence.ScheduleId == schedule.Schedule.Id &&
                                                a.Occurrence.GroupId == group.Group.Id &&
                                                a.PersonAlias.PersonId == person.Person.Id )
                                            .FirstOrDefault();

                                            if ( oldAttendance != null )
                                            {
                                                oldAttendance.EndDateTime = Rock.RockDateTime.Now;
                                                oldAttendance.DidAttend = false;
                                            }
                                            var attendance = attendanceService.AddOrUpdate( primaryAlias.Id, startDateTime.Date, group.Group.Id,
                                                    location.Location.Id, schedule.Schedule.Id, location.CampusId,
                                                    checkInState.Kiosk.Device.Id, checkInState.CheckIn.SearchType.Id,
                                                    checkInState.CheckIn.SearchValue, family.Group.Id, attendanceCode.Id );

                                            attendance.DeviceId = checkInState.Kiosk.Device.Id;
                                            attendance.SearchTypeValueId = checkInState.CheckIn.SearchType.Id;
                                            attendance.SearchValue = checkInState.CheckIn.SearchValue;
                                            attendance.CheckedInByPersonAliasId = checkInState.CheckIn.CheckedInByPersonAliasId;
                                            attendance.SearchResultGroupId = family.Group.Id;
                                            attendance.AttendanceCodeId = attendanceCode.Id;
                                            attendance.CreatedDateTime = startDateTime;
                                            attendance.StartDateTime = startDateTime;
                                            attendance.EndDateTime = null;
                                            attendance.Note = group.Notes;
                                            attendance.DidAttend = isMobile ? false : groupType.GroupType.GetAttributeValue( "SetDidAttend" ).AsBoolean();
                                            if ( isMobile )
                                            {
                                                if ( groupType.GroupType.GetAttributeValue( "SetDidAttend" ).AsBoolean() )
                                                {
                                                    attendance.QualifierValueId = mobileDidAttendId;
                                                }
                                                else
                                                {
                                                    attendance.QualifierValueId = mobileNotAttendId;
                                                }
                                            };

                                            attendanceService.Add( attendance );
                                            attendances.Add( attendance );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if ( isMobile )
                {
                    var alreadyExistingMobileCheckin = MobileCheckinRecordCache.GetActiveByFamilyGroupId( checkInState.CheckIn.CurrentFamily.Group.Id );
                    if ( alreadyExistingMobileCheckin != null )
                    {
                        //This should never run, it's just in case. Each family should only have 1 mobile check-in reservation.
                        MobileCheckinRecordCache.CancelReservation( alreadyExistingMobileCheckin, true );
                    }

                    KioskService kioskService = new KioskService( rockContext );
                    var kioskTypeId = kioskService.GetByClientName( checkInState.Kiosk.Device.Name ).KioskTypeId;
                    var kioskType = KioskTypeCache.Get( kioskTypeId.Value );
                    var campusId = kioskType.CampusId;
                    if ( campusId == null )
                    {
                        var compatableKioskType = KioskTypeCache.All().Where( kt => kt.CampusId.HasValue && kt.CheckinTemplateId == kioskType.CheckinTemplateId ).FirstOrDefault();
                        if ( compatableKioskType != null )
                        {
                            campusId = compatableKioskType.CampusId;
                        }
                        else
                        {
                            campusId = 0;
                        }
                    }

                    MobileCheckinRecordService mobileCheckinRecordService = new MobileCheckinRecordService( rockContext );
                    var mobileCheckinRecord = new MobileCheckinRecord
                    {
                        AccessKey = "MCR" + Guid.NewGuid().ToString( "N" ).Substring( 0, 12 ),
                        ReservedUntilDateTime = Rock.RockDateTime.Now.AddMinutes( kioskType.MinutesValid ?? 10 ),
                        ExpirationDateTime = Rock.RockDateTime.Now.AddMinutes( ( kioskType.MinutesValid ?? 10 ) + ( kioskType.GraceMinutes ?? 60 ) ),
                        UserName = checkInState.Kiosk.Device.Name,
                        FamilyGroupId = checkInState.CheckIn.CurrentFamily.Group.Id,
                        CampusId = campusId.Value
                    };

                    foreach ( var attendance in attendances )
                    {
                        mobileCheckinRecord.Attendances.Add( attendance );
                    }

                    mobileCheckinRecordService.Add( mobileCheckinRecord );
                }

                rockContext.SaveChanges();
                foreach ( var attendance in attendances )
                {
                    AttendanceCache.AddOrUpdate( attendance );
                }
                return true;
            }

            return false;
        }
    }
}