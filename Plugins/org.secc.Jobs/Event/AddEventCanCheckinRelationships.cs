using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs.Event
{
    [TextField( "Job Configuration Json",
        Description = "Job Configuration JSON Object",
        IsRequired = true,
        DefaultValue = "",
        Order = 0 )]
    [DisplayName("Create Event Check In Relationships")]
    [DisallowConcurrentExecution]
    public class AddEventCanCheckinRelationships : IJob
    {
        const string EventCanCheckinRelationshipGuid = "1758C197-8C6F-4727-A52B-37FA19603C35";
        const string EventAllowCheckinByRelationshipGuid = "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123";
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var configurationJson = dataMap.GetString( "JobConfigurationJson" );
            var config = configurationJson.FromJsonOrNull<EventCheckinConfiguration>();

            using (var rockContext = new RockContext())
            {
                var personService = new PersonService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );
                var registrantService = new RegistrationRegistrantService( rockContext );

                var registrationInstance = new RegistrationInstanceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Include(r => r.Registrations)
                    .Where( r => r.Id == config.RegistrationInstanceId )
                    .FirstOrDefault();

                if(registrationInstance == null)
                {
                    return;
                }

                foreach (var registration in registrationInstance.Registrations)
                {
                    if(!registration.PersonAliasId.HasValue)
                    {
                        return;
                    }
                    var registeredByPerson = personAliasService.GetByAliasId( registration.PersonAliasId.Value );

                    var familyMemberPersonIds = personService.GetFamilyMembers( registeredByPerson.Id, false, false )
                        .Select( m => m.PersonId )
                        .ToList();


                    var registrants = registrantService.Queryable().AsNoTracking()
                        .Include(r => r.PersonAlias.Person)
                        .Where( r => r.RegistrationId == registration.Id )
                        .ToList();

                    foreach (var registrant in registrants)
                    {
                        if(!familyMemberPersonIds.Contains(registrant.PersonAlias.PersonId))
                        {
                            BuildCanCheckinRelationship( registeredByPerson.Id, registrant.PersonAlias.PersonId, config.ExpirationDate );

                        }
                    }

                }
            }
        }

        private void BuildCanCheckinRelationship(int registeredByPersonId, int registrantPersonId, DateTime expirationDate)
        {
            var ownerRoleGuid = Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid();
            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() );

            var ownerRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == ownerRoleGuid );
            var eventCanCheckinRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == EventCanCheckinRelationshipGuid.AsGuid() );
            var eventAllowCheckinByRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == EventAllowCheckinByRelationshipGuid.AsGuid() );

            using (var relationshipContext = new RockContext())
            {
                var groupService = new GroupService( relationshipContext );
                var groupMemberService = new GroupMemberService( relationshipContext );

                
            }
        }


        public class EventCheckinConfiguration
        {
            public int WorkflowId { get; set; }
            public int RegistrationInstanceId { get; set; }
            public DateTime ExpirationDate { get; set; }
        }
    }
}
