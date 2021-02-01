using System.Collections.Generic;
using System.Linq;
using org.secc.DevLib.Components;
using org.secc.LeagueApps.Components;
using org.secc.LeagueApps.Contracts;
using org.secc.LeagueApps.Utilities;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Web.Cache;

namespace org.secc.LeagueApps.Jobs
{
    [DisallowConcurrentExecution]

    [BooleanField( "Create New People", "Should we create a new person if they are not in Rock?", key: "CreateNew" )]
    public class ImportMembers : IJob
    {

        public void Execute( IJobExecutionContext context )
        {
            var apiClient = new APIClient();

            var settings = SettingsComponent.GetComponent<LeagueAppsSettings>();
            var connectionStatus = DefinedValueCache.Get( settings.GetAttributeValue( Constants.DefaultConnectionStatus ).AsGuid() );

            var createNew = context.JobDetail.JobDataMap.GetString( "CreateNew" ).AsBoolean();

            int lastId = 0;
            long latestUpdated = 0;

            List<Member> members = null;

            var count = 0;

            do
            {
                members = apiClient.GetPrivate<List<Member>>( "v2/sites/{siteid}/export/members-2?last-updated=" + latestUpdated.ToString() + "&last-id=" + lastId.ToString() );
                foreach ( var member in members )
                {
                    lastId = member.userId;
                    latestUpdated = member.lastUpdated;

                    var person = LeagueAppsHelper.GetPersonByApiId( member.userId );

                    if ( person == null && !createNew )
                    {
                        continue;
                    }


                    if ( person == null )
                    {
                        person = LeagueAppsHelper.CreatePersonFromMember( member, connectionStatus );
                    }

                    if ( person == null )
                    {
                        continue;
                    }


                    var family = person.PrimaryFamily;

                    if ( family == null )
                    {
                        continue;
                    }


                    family.LoadAttributes();
                    if ( family.GetAttributeValue( Constants.LeagueAppsFamilyId ).IsNullOrWhiteSpace() )
                    {
                        family.SetAttributeValue( Constants.LeagueAppsFamilyId, member.groupId );
                        family.SaveAttributeValues();
                    }

                    count++;
                }
            } while ( members != null && members.Any() );

            context.Result = "Successfully imported " + count + " participants.";
        }
    }
}
