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
    [RegistrationTemplateField( "Registration Template",
        Description = "The registration template that contain the instances to set relationships for.",
        IsRequired = true,
        Key = "RegistrationTemplate",
        Order = 0 )]
    [DateTimeField( "Expiration Date Time",
        Description = "When the relationship should expire.",
        IsRequired = true )]
    [DisplayName( "Create Event Check In Relationships" )]
    [DisallowConcurrentExecution]
    public class AddEventCanCheckinRelationships : IJob
    {
        const string EventCanCheckinRelationshipGuid = "1758C197-8C6F-4727-A52B-37FA19603C35";
        const string EventAllowCheckinByRelationshipGuid = "0CAF69B9-9C8D-4222-BAF8-31C54BA0C123";
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var registrationTemplateGuid = dataMap.GetString( "RegistrationTemplate" ).AsGuid();
            var expirationDateTime = dataMap.GetString( "ExpirationDateTime" ).AsDateTime();

            using (var rockContext = new RockContext())
            {
                var personService = new PersonService( rockContext );
                //var personAliasService = new PersonAliasService( rockContext );
                var registrantService = new RegistrationRegistrantService( rockContext );

                var registrationInstances = new RegistrationInstanceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Include( r => r.Registrations )
                    .Where( r => r.RegistrationTemplate.Guid == registrationTemplateGuid )
                    .Where( r => r.IsActive )
                    .ToList();

                foreach (var registrationInstance in registrationInstances)
                {
                    foreach (var registration in registrationInstance.Registrations)
                    {
                        if (!registration.PersonAliasId.HasValue)
                        {
                            continue; // no personaliasid for registration owner
                        }
                        var registeredByPerson = registration.PersonAlias.Person;

                        if (registeredByPerson.IsDeceased)
                        {
                            continue; //skip deceased record
                        }

                        var familyMemberPersonIds = personService.GetFamilyMembers( registeredByPerson.Id, true, false )
                            .Select( m => m.PersonId )
                            .ToList();


                        var registrants = registrantService.Queryable().AsNoTracking()
                            .Include( r => r.PersonAlias.Person )
                            .Where( r => r.RegistrationId == registration.Id )
                            .ToList();

                        foreach (var registrant in registrants)
                        {
                            if (registrant.PersonAlias.Person.IsDeceased)
                            {
                                continue;  //skip deceased record
                            }

                            if (!familyMemberPersonIds.Contains( registrant.PersonAlias.PersonId ))
                            {
                                try
                                {
                                    BuildCanCheckinRelationship( registeredByPerson.Id, registrant.PersonAlias.PersonId, expirationDateTime.Value );
                                }
                                catch (NullReferenceException)
                                {
                                    //could not validate group
                                }
                            }
                        } // end registrants

                    } // end registrations
                } // end instance
            } // end db context
        }

        private void BuildCanCheckinRelationship( int registeredByPersonId, int registrantPersonId, DateTime expirationDate )
        {
            var ownerRoleGuid = Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid();
            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() );

            var ownerRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == ownerRoleGuid );
            var regularCheckinRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_CAN_CHECK_IN.AsGuid() );

            var eventCanCheckinRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == EventCanCheckinRelationshipGuid.AsGuid() );
            var eventAllowCheckinByRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == EventAllowCheckinByRelationshipGuid.AsGuid() );



            var currentTime = RockDateTime.Now;
            using (var relationshipContext = new RockContext())
            {
                var groupService = new GroupService( relationshipContext );
                var groupMemberService = new GroupMemberService( relationshipContext );

                var registeredByGroupId = groupMemberService.Queryable()
                    .Where( m => m.PersonId == registeredByPersonId )
                    .Where( m => m.GroupRoleId == ownerRole.Id )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( m => m.Group.IsActive )
                    .Select( m => m.GroupId )
                    .FirstOrDefault();

                var registrantGroupId = groupMemberService.Queryable()
                    .Where( m => m.PersonId == registrantPersonId )
                    .Where( m => m.GroupRoleId == ownerRole.Id )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( m => m.Group.IsActive )
                    .Select( m => m.GroupId )
                    .FirstOrDefault();

                if (registeredByGroupId == 0)
                {
                    registeredByGroupId = CreateKnownRelationshipGroup( knownRelationshipGroupType, registeredByPersonId );
                }

                if (registrantGroupId == 0)
                {
                    registrantGroupId = CreateKnownRelationshipGroup( knownRelationshipGroupType, registrantPersonId );
                }

                var canCheckin = groupMemberService.Queryable()
                    .Where( m => m.GroupId == registeredByGroupId )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .Where( m => m.PersonId == registrantPersonId )
                    .Where( m => m.GroupRoleId == regularCheckinRole.Id || m.GroupRoleId == eventCanCheckinRole.Id )
                    .Any();

                if (canCheckin)
                {
                    return;
                }

                var checkinGroupMember = new GroupMember
                {
                    PersonId = registrantPersonId,
                    GroupId = registeredByGroupId,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    GroupRoleId = eventCanCheckinRole.Id,
                    IsSystem = false,
                    DateTimeAdded = currentTime,
                    IsArchived = false,

                };
                groupMemberService.Add( checkinGroupMember );
                var allowCheckinGroupMember = new GroupMember
                {
                    PersonId = registeredByPersonId,
                    GroupId = registrantGroupId,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    GroupRoleId = eventAllowCheckinByRole.Id,
                    IsSystem = false,
                    DateTimeAdded = currentTime,
                    IsArchived = false
                };
                groupMemberService.Add( allowCheckinGroupMember );

                relationshipContext.SaveChanges();

                checkinGroupMember.LoadAttributes( relationshipContext );
                checkinGroupMember.SetAttributeValue( "ExpirationDateTime", expirationDate );
                checkinGroupMember.SaveAttributeValue( "ExpirationDateTime", relationshipContext );

                allowCheckinGroupMember.LoadAttributes( relationshipContext );
                allowCheckinGroupMember.SetAttributeValue( "ExpirationDateTime", expirationDate );
                allowCheckinGroupMember.SaveAttributeValue( "ExpirationDateTime", relationshipContext );

                relationshipContext.SaveChanges();

            }
        }

        private int CreateKnownRelationshipGroup( GroupTypeCache gt, int personId )
        {
            var ownerRole = gt.Roles
                .Where( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid() )
                .FirstOrDefault();

            using (var groupContext = new RockContext())
            {
                var currentTime = RockDateTime.Now;
                var groupService = new GroupService( groupContext );
                var groupMemberService = new GroupMemberService( groupContext );

                var group = new Group
                {
                    IsSystem = false,
                    GroupTypeId = gt.Id,
                    Name = "Known Relationships",
                    IsActive = true,
                    IsArchived = false,
                    IsPublic = false,
                    IsSecurityRole = false,
                    Order = 0

                };
                groupService.Add( group );
                groupContext.SaveChanges();

                var groupMember = new GroupMember
                {
                    IsSystem = false,
                    IsNotified = false,
                    IsArchived = false,
                    GroupId = group.Id,
                    PersonId = personId,
                    GroupRoleId = ownerRole.Id,
                    GroupMemberStatus = GroupMemberStatus.Active,
                    DateTimeAdded = currentTime
                };
                groupMemberService.Add( groupMember );
                groupContext.SaveChanges();

                return group.Id;
            }
        }

    }
}
