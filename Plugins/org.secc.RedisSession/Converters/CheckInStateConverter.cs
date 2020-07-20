using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rock;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;

namespace org.secc.RedisSession.Converters
{
    public class CheckInStateConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public CheckInStateConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {

            CheckInState state;
            if ( value is CheckInState )
            {
                state = ( CheckInState ) value;
            }
            else
            {
                return;
            }

            var localKey = Guid.NewGuid().ToString();

            var local = new CheckInStateAnalog
            {
                LocalKey = localKey,
                CheckIn = state.CheckIn,
                CheckinTypeId = state.CheckinTypeId,
                ConfiguredGroupTypes = state.ConfiguredGroupTypes,
                DeviceId = state.DeviceId,
                ManagerLoggedIn = state.ManagerLoggedIn,
                Messages = state.Messages,
                ExpiresDateTime = Rock.RockDateTime.Now.AddMinutes( 20 )
            };
            CheckInStateSessionManager.Set( local );

            var stored = new CheckInStateAnalog()
            {
                LocalKey = localKey,
                CheckIn = DehydrateStatus( state.CheckIn ),
                CheckinTypeId = state.CheckinTypeId,
                ConfiguredGroupTypes = state.ConfiguredGroupTypes,
                DeviceId = state.DeviceId,
                ManagerLoggedIn = state.ManagerLoggedIn,
                Messages = state.Messages,
                ExpiresDateTime = Rock.RockDateTime.Now.AddMinutes( 20 )
            };
            serializer.Serialize( writer, stored );
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var analog = ( CheckInStateAnalog ) serializer.Deserialize( reader, typeof( CheckInStateAnalog ) );

            var localkey = analog.LocalKey;
            var memoryAnalog = CheckInStateSessionManager.Get( localkey );

            if ( memoryAnalog != null )
            {
                //We have the state in memory
                analog = memoryAnalog;
            }
            else
            {
                //We have to recreate the state from our data
                HydrateStatus( analog.CheckIn );
            }

            var state = new CheckInState( analog.DeviceId, analog.CheckinTypeId, analog.ConfiguredGroupTypes );
            state.CheckInType = new CheckinType( analog.CheckinTypeId ?? 0 );
            state.CheckIn = analog.CheckIn;
            state.ManagerLoggedIn = analog.ManagerLoggedIn;
            state.Messages = analog.Messages;
            return state;
        }

        private void HydrateStatus( CheckInStatus status )
        {
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            PersonService personService = new PersonService( rockContext );
            LocationService locationService = new LocationService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );

            foreach ( var family in status.Families )
            {
                family.Group = groupService.Get( family.Group.Id );
                family.Group.LoadAttributes();
                foreach ( var person in family.People )
                {
                    person.Person = personService.Get( person.Person.Id );
                    person.Person.LoadAttributes();
                    foreach ( var grouptype in person.GroupTypes )
                    {
                        //We left the cached items in group type
                        foreach ( var group in grouptype.Groups )
                        {
                            group.Group = groupService.Get( group.Group.Id );
                            group.Group.LoadAttributes();
                            foreach ( var location in group.Locations )
                            {
                                location.Location = locationService.Get( location.Location.Id );
                                location.Location.LoadAttributes();
                                foreach ( var schedule in location.Schedules )
                                {
                                    schedule.Schedule = scheduleService.Get( schedule.Schedule.Id );
                                    schedule.Schedule.LoadAttributes();
                                }
                            }
                        }
                    }
                }
            }
        }

        private CheckInStatus DehydrateStatus( CheckInStatus status )
        {
            var checkinstatus = new CheckInStatus
            {
                SearchType = status.SearchType,
                SearchValue = status.SearchValue,
                Families = new List<CheckInFamily>()
            };

            foreach ( var family in status.Families )
            {
                var checkinFamily = new CheckInFamily()
                {
                    Selected = family.Selected,
                    Caption = family.Caption,
                    SubCaption = family.SubCaption,
                    AttendanceIds = family.AttendanceIds,
                    FirstNames = family.FirstNames,
                    Group = new Group() { Id = family.Group.Id },
                    People = new List<CheckInPerson>()
                };
                checkinstatus.Families.Add( checkinFamily );

                foreach ( var person in family.People )
                {
                    var checkInPerson = new CheckInPerson
                    {
                        GroupTypes = new List<CheckInGroupType>(),
                        FirstTime = person.FirstTime,
                        FamilyMember = person.FamilyMember,
                        Person = new Person { Id = person.Person.Id },
                        PreSelected = person.PreSelected,
                        SecurityCode = person.SecurityCode,
                        ExcludedByFilter = person.ExcludedByFilter,
                        Selected = person.Selected
                    };
                    checkinFamily.People.Add( checkInPerson );

                    foreach ( var grouptype in person.GroupTypes )
                    {
                        var checkinGroupType = new CheckInGroupType
                        {
                            GroupType = grouptype.GroupType,
                            Selected = grouptype.Selected,
                            PreSelected = grouptype.PreSelected,
                            ExcludedByFilter = grouptype.ExcludedByFilter,
                            Groups = new List<CheckInGroup>()
                        };
                        checkInPerson.GroupTypes.Add( checkinGroupType );

                        foreach ( var group in grouptype.Groups )
                        {
                            var checkinGroup = new CheckInGroup
                            {
                                Group = new Group { Id = group.Group.Id },
                                Selected = group.Selected,
                                PreSelected = group.PreSelected,
                                ExcludedByFilter = group.ExcludedByFilter,
                                Locations = new List<CheckInLocation>()
                            };
                            checkinGroupType.Groups.Add( group );

                            foreach ( var location in group.Locations )
                            {
                                var checkinLocation = new CheckInLocation
                                {
                                    Location = new Location { Id = location.Location.Id },
                                    Selected = location.Selected,
                                    PreSelected = location.PreSelected,
                                    ExcludedByFilter = location.ExcludedByFilter,
                                    CampusId = location.CampusId,
                                    Schedules = new List<CheckInSchedule>()
                                };
                                checkinGroup.Locations.Add( checkinLocation );

                                foreach ( var schedule in location.Schedules )
                                {
                                    var checkinSchedule = new CheckInSchedule
                                    {
                                        Schedule = new Schedule { Id = schedule.Schedule.Id },
                                        Selected = schedule.Selected,
                                        PreSelected = schedule.PreSelected,
                                        ExcludedByFilter = schedule.ExcludedByFilter,
                                        CampusId = schedule.CampusId,
                                        StartTime = schedule.StartTime
                                    };
                                    checkinLocation.Schedules.Add( checkinSchedule );
                                }
                            }
                        }
                    }
                }
            }
            return checkinstatus;
        }



        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }

        public class CheckInStateAnalog
        {
            public string LocalKey { get; set; }
            public int DeviceId { get; set; }
            public int? CheckinTypeId { get; set; }
            public CheckinType CheckinType { get; set; }
            public bool ManagerLoggedIn { get; set; }
            public List<int> ConfiguredGroupTypes { get; set; }
            public CheckInStatus CheckIn { get; set; }
            public List<CheckInMessage> Messages { get; set; }
            public DateTime ExpiresDateTime { get; set; }
        }

    }

}
