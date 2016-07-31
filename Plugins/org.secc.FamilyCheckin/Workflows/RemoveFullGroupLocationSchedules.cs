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

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Finds family members in a given family
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Removes schedules from locations where the location is full for that schedule." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Remove Full GroupLocationSchedules" )]
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
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.Families.Where( f => f.Selected ).FirstOrDefault();
                if ( family != null )
                {
                    var twelve = Rock.RockDateTime.Today.AddYears( -12 );
                    var locationService = new LocationService( rockContext );
                    var attendanceService = new AttendanceService( rockContext ).Queryable();
                    foreach ( var person in family.People )
                    {
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
                                        var attendanceQry = attendanceService.Where( a =>
                                             a.DidAttend == true
                                             && a.EndDateTime == null
                                             && a.ScheduleId == schedule.Schedule.Id
                                             && a.LocationId == location.Location.Id
                                             && a.CreatedDateTime >= Rock.RockDateTime.Today );

                                        var threshold = locationEntity.FirmRoomThreshold ?? 0;

                                        if ( attendanceQry.Count() >= threshold )
                                        {
                                            location.Schedules.Remove( schedule );
                                        }

                                        if ( ( person.Person.Age ?? 0 ) < 13 )
                                        {
                                            threshold = Math.Min( locationEntity.FirmRoomThreshold ?? 0, locationEntity.SoftRoomThreshold ?? 0 );

                                            if ( attendanceQry.Where( a => a.PersonAlias.Person.BirthDate > twelve ).Count() >= threshold )
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