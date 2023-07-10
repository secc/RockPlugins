using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow.Action;

namespace org.secc.Jobs
{
    [DisplayName( "Migrate Pickleball Credits" )]
    [Description( "Migrates the pickleball credits to group fitness" )]
    [GroupField( "Pickleball Group",
        Description = "The group that contains the pickleball participants.",
        Order = 0,
        Key = AttributeKeys.PickleballGroup )]
    [TextField( "Pickleball - Sessions Attribute Key",
        Description = "Attribute Key for the Pickleball Sessions Member Attribute",
        IsRequired = false,
        DefaultValue = "Sessions",
        Order = 1,
        Key = AttributeKeys.PickleballSessionsKey )]
    [TextField( "Pickleball - Migrated On Attribute Key",
        Description = "Pickleball MIgrated On Attribute Key",
        IsRequired = false,
        DefaultValue = "MigratedOn",
        Order = 2,
        Key = AttributeKeys.PickleballMigratedOnKey )]
    [GroupField( "Group Fitness Group",
        Description = "The group that contains the Group Fitness group",
        Order = 3,
        Key = AttributeKeys.GroupFitnessGroup )]
    [TextField( "Group Fitness - Sessions Attribute Key",
        Description = "Group Fitness Sessions Attribute Key",
        IsRequired = false,
        DefaultValue = "Sessions",
        Order = 4,
        Key = AttributeKeys.GroupFitnessSessionsKey )]
    [NoteTypeField( "Note Type",
        Description = "Note type for the migration note",
        AllowMultiple = false,
        IsRequired = true,
        EntityTypeName = "Rock.Model.GroupMember",
        Order = 5,
        Key = AttributeKeys.MigrationNoteType )]


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
            internal const string MigrationNoteType = "MigrationNoteType";


        }
        public void Execute( IJobExecutionContext context )
        {
            var dataMap = context.JobDetail.JobDataMap;
            var pickleballGroupGuid = dataMap.GetString( AttributeKeys.PickleballGroup ).AsGuidOrNull();
            var pickleballSessionsKey = dataMap.GetString( AttributeKeys.PickleballSessionsKey );
            var pickleballMigratedOnKey = dataMap.GetString( AttributeKeys.PickleballMigratedOnKey );

            var groupFitnessGroupGuid = dataMap.GetString( AttributeKeys.GroupFitnessGroup ).AsGuidOrNull();
            var groupFitnessSessionKey = dataMap.GetString( AttributeKeys.GroupFitnessSessionsKey );

            var noteTypeGuid = dataMap.GetString( AttributeKeys.MigrationNoteType ).AsGuid();


            if (!pickleballGroupGuid.HasValue)
            {
                context.UpdateLastStatusMessage( "WARNING: Pickleball Group not selected" );
                return;
            }
            if (!groupFitnessGroupGuid.HasValue)
            {
                context.UpdateLastStatusMessage( "WARNING: Group Fitness Group not selected" );
                return;
            }



            var noteType = NoteTypeCache.Get( noteTypeGuid );

            var rockContext = new RockContext();

            var pickleBallGroup = new GroupService( rockContext ).Get( pickleballGroupGuid.Value );
            var groupFitnessGroup = new GroupService( rockContext ).Get( groupFitnessGroupGuid.Value );

            if (pickleBallGroup == null)
            {
                context.UpdateLastStatusMessage( "WARNING: Pickleball Group not found." );
                return;
            }

            if (groupFitnessGroup == null)
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

            foreach (var id in pickleballGroupMemberIds)
            {
                using (var migrationContext = new RockContext())
                {
                    var groupMemberService = new GroupMemberService( migrationContext );
                    var pickleballParticipant = groupMemberService.Get( id );
                    pickleballParticipant.LoadAttributes( migrationContext );

                    var pickleballSessions = pickleballParticipant.GetAttributeValue( pickleballSessionsKey ).AsInteger();
                    var isMigratable = pickleballSessions > 0 || (pickleballParticipant.IsArchived == false 
                        && pickleballParticipant.GroupMemberStatus != GroupMemberStatus.Inactive);

                    var migratedOn = pickleballParticipant.GetAttributeValue( pickleballMigratedOnKey ).AsDateTime();

                    if ( isMigratable && !migratedOn.HasValue)
                    {
                        var groupFitnessMember = groupMemberService.Queryable()
                            .Where( m => m.GroupId == groupFitnessGroup.Id )
                            .Where( m => m.PersonId == pickleballParticipant.PersonId )
                            .FirstOrDefault();

                        if (groupFitnessMember == null)
                        {
                            groupFitnessMember = new GroupMember
                            {
                                GroupId = groupFitnessGroup.Id,
                                PersonId = pickleballParticipant.PersonId,
                                GroupMemberStatus = pickleballParticipant.GroupMemberStatus,
                                GroupRoleId = pickleballParticipant.GroupRoleId,
                                DateTimeAdded = pickleballParticipant.DateTimeAdded,
                                InactiveDateTime = pickleballParticipant.InactiveDateTime,
                                IsArchived = pickleballParticipant.IsArchived,
                                ArchivedDateTime = null,
                                ArchivedByPersonAliasId = null
                            };
                            groupMemberService.Add( groupFitnessMember );
                        }
                        else if (groupFitnessMember.IsArchived)
                        {
                            groupFitnessMember.IsArchived = false;
                            groupFitnessMember.ArchivedByPersonAliasId = null;
                            groupFitnessMember.ArchivedDateTime = null;
                        }
                        else if(groupFitnessMember.GroupMemberStatus == GroupMemberStatus.Inactive && pickleballParticipant.GroupMemberStatus != GroupMemberStatus.Inactive)
                        {
                            groupFitnessMember.GroupMemberStatus = pickleballParticipant.GroupMemberStatus;
                            groupFitnessMember.InactiveDateTime = null;
                        }

                        if(pickleballParticipant.DateTimeAdded > groupFitnessMember.DateTimeAdded)
                        {
                            groupFitnessMember.DateTimeAdded = pickleballParticipant.DateTimeAdded;
                        }

                        migrationContext.SaveChanges();
                        groupFitnessMember.LoadAttributes( migrationContext );
                        var groupFitnessSessions = groupFitnessMember.GetAttributeValue( groupFitnessSessionKey ).AsInteger();
                        var totalSessions = groupFitnessSessions + pickleballSessions;

                        groupFitnessMember.SetAttributeValue( groupFitnessSessionKey,
                            totalSessions );

                        groupFitnessMember.SaveAttributeValue( groupFitnessSessionKey, migrationContext );

                        if(noteType != null)
                        {
                            var noteTitle = "Combine Session Migration";
                            var sb = new StringBuilder();
                            sb.Append( $"Added Pickleball session credits\n" );
                            sb.Append( $"Starting Group Fitness sessions: {groupFitnessSessions}\n" );
                            sb.Append($"Added Pickleball sessions: { pickleballSessions}\n ");
                            sb.Append($"New session balance: {totalSessions}");
                            AddGroupMemberNote( groupFitnessMember.Id, noteType, noteTitle, sb.ToString() );
                        }

                    }

                    pickleballParticipant.SetAttributeValue( pickleballMigratedOnKey, RockDateTime.Now );
                    pickleballParticipant.SaveAttributeValue( pickleballMigratedOnKey, migrationContext );
                    migrationContext.SaveChanges();
                    participantsProcessed++;

                    if ((participantsProcessed % 50) == 0)
                    {
                        context.UpdateLastStatusMessage( $"{participantsProcessed} of {pickleballGroupMemberIds.Count} processed." );
                    }


                }
            }

            context.UpdateLastStatusMessage( $"Migration Complete. {participantsProcessed} processed." );

        }

        private void AddGroupMemberNote( int groupMemberId, NoteTypeCache noteType, string title, string body )
        {
            using (RockContext rockContext = new RockContext())
            {
                var noteService = new NoteService( rockContext );

                var note = new Note
                {
                    NoteTypeId = noteType.Id,
                    EntityId = groupMemberId,
                    Caption = title,
                    Text = body,
                    ApprovalStatus = NoteApprovalStatus.Approved,
                    NoteUrl = $"/GroupMember/{groupMemberId}",

                };
                noteService.Add( note );
                rockContext.SaveChanges();
            }
        }
    }
}
