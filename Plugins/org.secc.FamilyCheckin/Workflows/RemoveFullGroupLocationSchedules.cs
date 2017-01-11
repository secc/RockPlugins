using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Workflow;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Workflow.Action.CheckIn;
using Rock.Attribute;
using Rock.Web.Cache;
using System.Runtime.Caching;
using org.secc.FamilyCheckin.Utilities;
using static org.secc.FamilyCheckin.Utilities.KioskCountUtility;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds family members in a given family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes schedules from locations where the location is full for that schedule." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Remove Full GroupLocationSchedules" )]

    [BooleanField( "Filter Attendance Schedules", "Filter out schedules that the person has already checked into" )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP, "Volunteer Group Attribute" )]
    public class RemoveFullGroupLocationScheduels : CheckInActionComponent
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
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var filterAttendanceSchedules = GetActionAttributeValue( action, "FilterAttendanceSchedules" ).AsBoolean();

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    var volAttributeGuid = GetAttributeValue( action, "VolunteerGroupAttribute" );

                    KioskCountUtility kioskCountUtility = new KioskCountUtility( checkInState.ConfiguredGroupTypes, volAttributeGuid.AsGuid() );

                    ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
                    List<int> volunteerGroupIds = cache[volAttributeGuid + "CachedVolunteerGroups"] as List<int>;
                    if ( volunteerGroupIds == null ) //cache miss load from DB
                    {
                        var volAttribute = AttributeCache.Read( volAttributeGuid.AsGuid() );
                        AttributeValueService attributeValueService = new AttributeValueService( rockContext );
                        volunteerGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();
                        var cachePolicy = new CacheItemPolicy();
                        cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                        cache.Set( volAttributeGuid + "CachedVolunteerGroups", volAttributeGuid + "CachedVolunteerGroups", cachePolicy );
                    }

                    var locationService = new LocationService( rockContext );
                    var attendanceService = new AttendanceService( rockContext ).Queryable();
                    foreach ( var person in family.People )
                    {
                        List<int> filteredScheduleIds = new List<int>();
                        foreach ( var groupType in person.GroupTypes )
                        {
                            foreach ( var group in groupType.Groups )
                            {
                                foreach ( var location in group.Locations )
                                {
                                    //Get a new copy of the entity because otherwise the threshold data may be stale
                                    var locationEntity = locationService.Get( location.Location.Id );

                                    foreach ( var schedule in location.Schedules.ToList() )
                                    {
                                        if ( filterAttendanceSchedules && filteredScheduleIds.Contains( schedule.Schedule.Id ) )
                                        {
                                            location.Schedules.Remove( schedule );
                                            continue;
                                        }

                                        var attendanceQry = attendanceService.Where( a =>
                                             a.DidAttend == true
                                             && a.EndDateTime == null
                                             && a.ScheduleId == schedule.Schedule.Id
                                             && a.StartDateTime >= Rock.RockDateTime.Today );


                                        if ( filterAttendanceSchedules && attendanceQry.Where( a => a.PersonAlias.PersonId == person.Person.Id ).Any() )
                                        {
                                            filteredScheduleIds.Add( schedule.Schedule.Id );
                                            location.Schedules.Remove( schedule );
                                            continue;
                                        }

                                        var threshold = locationEntity.FirmRoomThreshold ?? 0;

                                        LocationScheduleCount locationScheduleCount = kioskCountUtility.GetLocationScheduleCount( location.Location.Id, schedule.Schedule.Id );
                                        if ( locationScheduleCount.TotalCount >= threshold )
                                        {
                                            location.Schedules.Remove( schedule );
                                            continue;
                                        }

                                        if ( !volunteerGroupIds.Contains( group.Group.Id ) )
                                        {
                                            threshold = Math.Min( locationEntity.FirmRoomThreshold ?? 0, locationEntity.SoftRoomThreshold ?? 0 );

                                            if ( locationScheduleCount.ChildCount >= threshold )
                                            {
                                                location.Schedules.Remove( schedule );
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    errorMessages.Add( "There is not a family that is selected" );
                }

                return false;
            }

            return false;
        }
    }
}