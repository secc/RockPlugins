using Rock;
using System;
using System.Collections.Generic;
using System.Linq;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Data;
using Rock.Model;

namespace org.secc.FamilyCheckin.Utilities
{
    public class KioskCountUtility
    {
        public Guid VolunteerAttributeGuid { get; private set; }
        public List<int> ConfiguredGroupTypes { get; private set; }
        private List<GroupType> _groupTypes;
        public List<GroupType> GroupTypes
        {
            get
            {
                if ( _groupTypes == null )
                {
                    RockContext rockContext = new RockContext();

                    IQueryable<GroupType> groupTypeService = new GroupTypeService( rockContext ).Queryable( "Groups,Groups.GroupLocations,Groups.GroupLocations.Schedules" );

                    _groupTypes = groupTypeService
                        .Where( gt => ConfiguredGroupTypes.Contains( gt.Id ) ).ToList();
                }
                return _groupTypes;
            }
        }
        private List<int> _volunteerGroupIds;
        public List<int> VolunteerGroupIds
        {
            get
            {
                if ( _volunteerGroupIds == null )
                {
                    var volAttribute = AttributeCache.Read( VolunteerAttributeGuid );

                    AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
                    _volunteerGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();
                }
                return _volunteerGroupIds;
            }
        }

        private List<int> _childGroupIds;
        public List<int> ChildGroupIds
        {
            get
            {
                if ( _childGroupIds == null )
                {
                    var volAttribute = AttributeCache.Read( VolunteerAttributeGuid );

                    AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
                    _childGroupIds = attributeValueService.Queryable().Where( av => av.AttributeId == volAttribute.Id && av.Value == "False" ).Select( av => av.EntityId.Value ).ToList();
                }
                return _childGroupIds;
            }
        }

        private Guid? _deactivatedDefinedTypeGuid;

        private List<GroupLocationSchedule> _groupLocationSchedules;
        public List<GroupLocationSchedule> GroupLocationSchedules
        {
            get
            {
                if ( _groupLocationSchedules == null )
                {
                    var groupLocations = GroupTypes
                        .SelectMany( gt => gt.Groups.Where( g => g.IsActive ) )
                        .SelectMany( g => g.GroupLocations );

                    _groupLocationSchedules = new List<GroupLocationSchedule>();

                    foreach ( var gl in groupLocations )
                    {
                        foreach ( var s in gl.Schedules )
                        {
                            _groupLocationSchedules.Add( new GroupLocationSchedule( gl, s, true ) );
                        }
                    }

                    if ( _deactivatedDefinedTypeGuid != null )
                    {
                        RockContext rockContext = new RockContext();
                        GroupLocationService groupLocationService = new GroupLocationService( rockContext );
                        ScheduleService scheduleService = new ScheduleService( rockContext );

                        var dtDeactivated = DefinedTypeCache.Read( _deactivatedDefinedTypeGuid ?? new Guid() );
                        var dvDeactivated = dtDeactivated.DefinedValues;
                        _groupLocationSchedules.AddRange( dvDeactivated.Select( dv => dv.Value.Split( '|' ) )
                            .Select( s => new GroupLocationSchedule(
                                groupLocationService.Get( s[0].AsInteger() ),
                                scheduleService.Get( s[1].AsInteger() ),
                                false )
                            ).ToList()
                        );
                    }

                }
                return _groupLocationSchedules;
            }
        }

        public KioskCountUtility( List<int> _configuredGroupTypes, Guid _volunteerAttributeGuid )
        {
            _KioskCountUtility( _configuredGroupTypes, _volunteerAttributeGuid, null );
        }

        public KioskCountUtility( List<int> _configuredGroupTypes, Guid _volunteerAttributeGuid, Guid DeactivatedDefinedTypeGuid )
        {
            _KioskCountUtility( _configuredGroupTypes, _volunteerAttributeGuid, DeactivatedDefinedTypeGuid );
        }

        private void _KioskCountUtility( List<int> _configuredGroupTypes, Guid _volunteerAttributeGuid, Guid? DeactivatedDefinedTypeGuid )
        {

            ConfiguredGroupTypes = _configuredGroupTypes;
            VolunteerAttributeGuid = _volunteerAttributeGuid;
            _deactivatedDefinedTypeGuid = DeactivatedDefinedTypeGuid; //+1 for orriginality

        }

        public LocationScheduleCount GetLocationScheduleCount( int LocationId, int ScheduleId )
        {
            var locationScheduleCount = new LocationScheduleCount();
            var kgas = KioskLocationAttendance.Read( LocationId ).Groups.Where( g => g.Schedules.Where( s => s.ScheduleId == ScheduleId ).Any() ).ToList();

            locationScheduleCount.ChildCount = kgas.Where( kga => ChildGroupIds.Contains( kga.GroupId ) )
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            locationScheduleCount.VolunteerCount = kgas.Where( kga => VolunteerGroupIds.Contains( kga.GroupId ) )
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            locationScheduleCount.ReservedCount = 0;

            locationScheduleCount.TotalCount = kgas
                .SelectMany( kga => kga.Schedules.Where( s => s.ScheduleId == ScheduleId ) )
                .Select( kgs => kgs.CurrentCount ).Sum();

            return locationScheduleCount;
        }
    }

    public class GroupLocationSchedule
    {
        public GroupLocation GroupLocation { get; private set; }
        public Schedule Schedule { get; private set; }
        public bool Active { get; private set; }

        internal GroupLocationSchedule( GroupLocation GroupLocation, Schedule Schedule, bool Active )
        {
            this.GroupLocation = GroupLocation;
            this.Schedule = Schedule;
            this.Active = Active;
        }
    }

    public class LocationScheduleCount
    {
        public int ChildCount { get; internal set; }
        public int VolunteerCount { get; internal set; }
        public int ReservedCount { get; internal set; }
        public int TotalCount { get; internal set; }


    }
}
