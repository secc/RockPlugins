using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.secc.DevLib.Components;
using org.secc.LeagueApps.Components;
using org.secc.LeagueApps.Contracts;
using org.secc.LeagueApps.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.LeagueApps.Jobs
{
    [BooleanField( "Create New People", "Should we create a new person if they are not in Rock?", key: "CreateNew" )]
    public class ImportMembers : RockJob
    {

        public override void Execute()
        {
            var apiClient = new APIClient();

            var settings = SettingsComponent.GetComponent<LeagueAppsSettings>();
            var connectionStatus = DefinedValueCache.Get( settings.GetAttributeValue( Constants.DefaultConnectionStatus ).AsGuid() );

            var createNew = GetAttributeValue( "CreateNew" ).AsBoolean();

            int lastId = 0;
            long latestUpdated = 0;

            List<Member> members = null;

            var count = 0;
            var errors = new List<string>();
            Exception fetchError = null;

            do
            {
                try
                {
                    members = apiClient.GetPrivate<List<Member>>( "v2/sites/{siteid}/export/members-2?last-updated=" + latestUpdated.ToString() + "&last-id=" + lastId.ToString() );
                }
                catch ( Exception ex )
                {
                    // Stop paging but keep the work already done so the job status reports it.
                    fetchError = ex;
                    ExceptionLogService.LogException( ex, null );
                    break;
                }

                // An empty body means no more pages.
                if ( members == null )
                {
                    break;
                }

                foreach ( var member in members )
                {
                    lastId = member.userId;
                    latestUpdated = member.lastUpdated;

                    try
                    {
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
                    catch ( Exception ex )
                    {
                        var errorMsg = $"UserId: {member.userId}, Name: {member.firstName ?? "Unknown"} {member.lastName ?? "Unknown"} - Error: {ex.Message}";
                        errors.Add( errorMsg );
                        ExceptionLogService.LogException( ex, null );
                    }
                }
            } while ( members != null && members.Any() );

            var resultMsg = new StringBuilder();
            resultMsg.AppendFormat( "Successfully imported {0} participant(s).", count );

            if ( errors.Any() )
            {
                resultMsg.AppendFormat( " {0} error(s) occurred: ", errors.Count );
                resultMsg.Append( string.Join( "; ", errors ) );
            }

            if ( fetchError != null )
            {
                // Fail the job, but carry the partial result in the message so progress is not lost.
                throw new Exception( resultMsg + " Import aborted after last-id " + lastId + ": " + fetchError.Message, fetchError );
            }

            Result = resultMsg.ToString();
        }
    }
}
