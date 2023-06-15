using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [DisplayName("Migrate Pickleball Credits")]
    [Description("Migrates the pickleball credits to group fitness")]
    [GroupField("Pickleball Group", 
        Description = "The group that contains the pickleball participants.",
        Order = 0,
        Key = AttributeKeys.PickleballGroup)]
    [TextField("Pickleball - Sessions Attribute Key", 
        Description = "Attribute Key for the Pickleball Sessions Member Attribute",
        IsRequired = false,
        DefaultValue = "Sessions",
        Order = 1,
        Key = AttributeKeys.PickleballSessionsKey )]
    [TextField("Pickleball - Migrated On Attribute Key",
        Description = "Pickleball MIgrated On Attribute Key",
        IsRequired = false,
        DefaultValue = "MigratedOn",
        Order = 2,
        Key = AttributeKeys.PickleballMigratedOnKey )]
    [GroupField("Group Fitness Group",
        Description = "The group that contains the Group Fitness group",
        Order = 3,
        Key = AttributeKeys.GroupFitnessGroup)]
    [TextField("Group Fitness - Sessions Attribute Key",
        Description = "Group Fitness Sessions Attribute Key",
        IsRequired = false,
        DefaultValue = "Sessions",
        Order = 4, 
        Key = AttributeKeys.GroupFitnessSessionsKey)]
    

    [DisallowConcurrentExecution]
    public class MigratePickleballCredits : IJob
    {
        internal class AttributeKeys
        {
            internal const string PickleballGroup = "Pickleball";
            internal const string PickleballSessionsKey = "PickleballSessions";
            internal const string PickleballMigratedOnKey = "PickleballMigratedOn";
            internal const string GroupFitnessGroup = "GroupFitness";
            internal const string GroupFitnessSessionsKey = "GroupFitnessSessions";


        }
        public void Execute( IJobExecutionContext context )
        {
            var dataMap = context.JobDetail.JobDataMap;
            var pickleballGroupGuid = dataMap.GetString( AttributeKeys.PickleballGroup ).AsGuidOrNull();
            var pickleballSessionsKey = dataMap.GetString( AttributeKeys.PickleballSessionsKey );
            var pickleballMigratedOnKey = dataMap.GetString( AttributeKeys.PickleballMigratedOnKey );
            
            var groupFitnessGroupGuid = dataMap.GetString( AttributeKeys.GroupFitnessGroup ).AsGuidOrNull();
            var groupFitnessSessionKey = dataMap.GetString( AttributeKeys.GroupFitnessSessionsKey );

            if(!pickleballGroupGuid.HasValue)
            {
                context.UpdateLastStatusMessage( "WARNING: Pickleball Group not selected" );
                return;
            }
            if(!groupFitnessGroupGuid.HasValue)
            {
                context.UpdateLastStatusMessage( "WARNING: Group Fitness Group not selected" );
                return;
            }

            var rockContext = new RockContext();

            var pickleBallGroup = new GroupService( rockContext ).Get( pickleballGroupGuid.Value );
            var groupFitnessGroup = new GroupService( rockContext ).Get( groupFitnessGroupGuid.Value );

            if(pickleBallGroup == null)
            {
                context.UpdateLastStatusMessage( "WARNING: Pickleball Group not found." );
                return;
            }

            if(groupFitnessGroup == null)
            {
                context.UpdateLastStatusMessage( "WARNING: Group Fitness Group not found." );
                return;
            }


            var pickleballGroupMemberIds = new GroupMemberService( rockContext ).Queryable().AsNoTracking()
                .Where( g => g.GroupId == pickleBallGroup.Id )
                .Select( g => g.Id )
                .ToList();

            context.UpdateLastStatusMessage( $"{pickleballGroupMemberIds.Count()} Pickleball participants found." );

            var participantsProcessed = 0;
            
            foreach ( var id in pickleballGroupMemberIds )
            {
                using( var migrationContext = new RockContext() )
                {
                    var groupMemberService = new GroupMemberService( migrationContext );
                    var pickleballParticipant = groupMemberService.Get( id );
                    pickleballParticipant.LoadAttributes( migrationContext );

                    var pickleballSessions = pickleballParticipant.GetAttributeValue( pickleballSessionsKey ).AsInteger();
                    var migratedOn = pickleballParticipant.GetAttributeValue( pickleballMigratedOnKey ).AsDateTime();

                    if(pickleballSessions > 0 && !migratedOn.HasValue)
                    {
                        var groupFitnessMember = groupMemberService.Queryable()
                            .Where( m => m.GroupId == groupFitnessGroup.Id )
                            .Where( m => m.PersonId == pickleballParticipant.PersonId )
                            .FirstOrDefault();

                        if(groupFitnessMember == null)
                        {
                            groupFitnessMember = new GroupMember
                            {
                                GroupId = groupFitnessGroup.Id,
                                PersonId = pickleballParticipant.PersonId,
                                GroupMemberStatus = pickleballParticipant.GroupMemberStatus,
                                GroupRoleId = pickleballParticipant.GroupRoleId,
                                DateTimeAdded = Rock.RockDateTime.Now
                            };
                            groupMemberService.Add( groupFitnessMember );
                        }
                        else if(groupFitnessMember.IsArchived)
                        {
                            groupFitnessMember.IsArchived = false;
                            groupFitnessMember.ArchivedByPersonAliasId = null;
                            groupFitnessMember.ArchivedDateTime = null;
                        }

                        migrationContext.SaveChanges();
                        groupFitnessMember.LoadAttributes( migrationContext );
                        var groupFitnessSessions = groupFitnessMember.GetAttributeValue( groupFitnessSessionKey ).AsInteger();

                        groupFitnessMember.SetAttributeValue( groupFitnessSessionKey,
                            groupFitnessSessions + pickleballSessions );

                        groupFitnessMember.SaveAttributeValue( groupFitnessSessionKey, migrationContext );

                    }

                    pickleballParticipant.SetAttributeValue( pickleballMigratedOnKey, RockDateTime.Now );
                    pickleballParticipant.SaveAttributeValue( pickleballMigratedOnKey, migrationContext );
                    migrationContext.SaveChanges();
                    participantsProcessed++;

                    if((participantsProcessed % 50) == 0)
                    {
                        context.UpdateLastStatusMessage( $"{participantsProcessed} of {pickleballGroupMemberIds.Count} processed." );
                    }
                }
            }

            context.UpdateLastStatusMessage( $"Migration Complete. {participantsProcessed} processed." );

        }
    }
}
