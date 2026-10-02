// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using org.secc.DevLib.Components;
using org.secc.LeagueApps.Components;
using org.secc.LeagueApps.Contracts;
using org.secc.LeagueApps.Utilities;
using Rock;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.LeagueApps
{



    public class ImportData : RockJob
    {
        /// <summary>Stop looking up members for a program after this many back-to-back failures.</summary>
        private const int MaxConsecutiveMemberFailures = 10;

        /// <summary>Cap on warning lines included in the thrown job status message.</summary>
        private const int MaxWarningsInStatus = 50;

        /// <summary>Process all leagues (programs) from LeagueApps.</summary>
        public override void Execute()
        {
            RockContext dbContext = new RockContext();
            GroupService groupService = new GroupService( dbContext );
            AttributeService attributeService = new AttributeService( dbContext );
            AttributeValueService attributeValueService = new AttributeValueService( dbContext );
            DefinedValueService definedValueService = new DefinedValueService( dbContext );
            DefinedTypeService definedTypeService = new DefinedTypeService( dbContext );
            BinaryFileService binaryFileService = new BinaryFileService( dbContext );

            var warnings = new List<string>();
            var processed = 0;
            var skipped = 0;
            var totalPrograms = 0;

            try
            {
                var apiClient = new APIClient();

                var settings = SettingsComponent.GetComponent<LeagueAppsSettings>();

                //Group Attributes
                var parentGroup = groupService.Get( settings.GetAttributeValue( Constants.ParentGroup ).AsGuid() );
                var yearGroupType = GroupTypeCache.Get( settings.GetAttributeValue( Constants.YearGroupType ).AsGuid() );
                var categoryGroupType = GroupTypeCache.Get( settings.GetAttributeValue( Constants.CategoryGroupType ).AsGuid() );
                var leagueGroupType = GroupTypeCache.Get( settings.GetAttributeValue( Constants.LeagueGroupType ).AsGuid() );
                var sportsType = DefinedTypeCache.Get( Constants.SPORTS_TYPE.AsGuid() );
                var seasonsType = DefinedTypeCache.Get( Constants.SEASONS_TYPE.AsGuid() );
                var gendersType = DefinedTypeCache.Get( Constants.GENDERS_TYPE.AsGuid() );
                var groupMemberAttribute = AttributeCache.Get( settings.GetAttributeValue( Constants.LeagueGroupTeam ).AsGuid() );

                //Person Attribute
                var personattribute = AttributeCache.Get( Constants.ATTRIBUTE_PERSON_USER_ID.AsGuid() );
                var connectionStatus = DefinedValueCache.Get( settings.GetAttributeValue( Constants.DefaultConnectionStatus ).AsGuid() );

                var groupEntityType = EntityTypeCache.Get( typeof( Group ) ).Id;

                var programs = apiClient.GetPublic<List<Programs>>( "/v1/sites/{siteid}/programs/current" );
                if ( programs == null )
                {
                    // Treating a missing list as "no programs" would deactivate every league below.
                    throw new Exception( "LeagueApps returned an empty body for the current programs list." );
                }
                totalPrograms = programs.Count;
                var programNumber = 0;


                var groups = groupService.Queryable().Where( g => g.GroupTypeId == leagueGroupType.Id ).ToList();

                foreach ( Contracts.Programs program in programs )
                {
                    programNumber++;
                    // Process the program
                    Group league = null;
                    Group league2 = null;
                    Group league3 = null;
                    var startdate = program.startTime;
                    var mode = program.mode.ToLower();
                    mode = mode.First().ToString().ToUpper() + mode.Substring( 1 );
                    var grandparentgroup = string.Format( "{0}", startdate.Year );
                    if ( program.programId > 0 )
                    {
                        league = groupService.Queryable().Where( l => l.Name == grandparentgroup && l.ParentGroupId == parentGroup.Id ).FirstOrDefault();
                        if ( league != null )
                        {
                            league2 = groupService.Queryable().Where( l => l.Name == mode && l.ParentGroupId == league.Id ).FirstOrDefault();
                            if ( league2 != null )
                            {
                                league3 = groupService.Queryable().Where( l => l.ForeignId == program.programId && l.GroupTypeId == leagueGroupType.Id && l.ParentGroupId == league2.Id ).FirstOrDefault();
                            }
                        }
                    }
                    Guid guid = Guid.NewGuid();
                    Guid guid2 = Guid.NewGuid();
                    Guid guid3 = Guid.NewGuid();

                    if ( league == null )
                    {
                        // Create league grandparent Group
                        Group leagueGPG = new Group();
                        leagueGPG.Name = grandparentgroup;
                        leagueGPG.GroupTypeId = yearGroupType.Id;
                        leagueGPG.ParentGroupId = parentGroup.Id;
                        leagueGPG.IsSystem = false;
                        leagueGPG.IsActive = true;
                        leagueGPG.IsSecurityRole = false;
                        leagueGPG.Order = 0;
                        leagueGPG.Guid = guid;
                        groupService.Add( leagueGPG );

                        // Now save the league grandparent group
                        dbContext.SaveChanges();
                        league = leagueGPG;
                    }

                    if ( league2 == null )
                    {
                        // Create league parent Group
                        Group leaguePG = new Group();
                        leaguePG.Name = mode;
                        leaguePG.GroupTypeId = categoryGroupType.Id;
                        leaguePG.ParentGroupId = league.Id;
                        leaguePG.IsSystem = false;
                        leaguePG.IsActive = true;
                        leaguePG.IsSecurityRole = false;
                        leaguePG.Order = 0;
                        leaguePG.Guid = guid2;
                        groupService.Add( leaguePG );

                        // Now save the league parent group
                        dbContext.SaveChanges();
                        league2 = leaguePG;
                    }

                    if ( league3 == null )
                    {
                        // Create the league
                        Group leagueG = new Group();
                        leagueG.Name = program.name;
                        leagueG.GroupTypeId = leagueGroupType.Id;
                        leagueG.ParentGroupId = league2.Id;
                        leagueG.IsSystem = false;
                        leagueG.IsActive = true;
                        leagueG.IsSecurityRole = false;
                        leagueG.Order = 0;
                        leagueG.Description = HTMLConvertor.Convert( program.description );
                        leagueG.ForeignId = program.programId;
                        groupService.Add( leagueG );

                        // Now save the league
                        dbContext.SaveChanges();
                        league3 = leagueG;
                    }
                    else
                    {
                        groups.Remove( league3 );
                    }
                    league3.LoadAttributes();
                    var sport = definedValueService.Queryable().Where( d => d.Value == program.sport && d.DefinedTypeId == sportsType.Id ).FirstOrDefault();
                    var season = definedValueService.Queryable().Where( d => d.Value == program.season && d.DefinedTypeId == seasonsType.Id ).FirstOrDefault();
                    var groupgender = definedValueService.Queryable().Where( d => d.Value == program.gender && d.DefinedTypeId == gendersType.Id ).FirstOrDefault();

                    if ( sport != null )
                        league3.SetAttributeValue( "Sport", sport.Guid );

                    if ( season != null )
                        league3.SetAttributeValue( "Season", season.Guid );
                    league3.SetAttributeValue( "ExperienceLevel", program.experienceLevel );

                    if ( groupgender != null )
                        league3.SetAttributeValue( "Gender", groupgender.Guid );

                    if ( startdate != DateTime.MinValue )
                        league3.SetAttributeValue( "StartTime", startdate );

                    if ( program.publicRegistrationTime != DateTime.MinValue )
                        league3.SetAttributeValue( "PublicRegistrationTime", program.publicRegistrationTime );

                    if ( program.ageLimitEffectiveDate != DateTime.MinValue )
                        league3.SetAttributeValue( "AgeLimitDate", program.ageLimitEffectiveDate.Date.ToString( "d" ) );
                    league3.SetAttributeValue( "ProgramURL", program.programUrlHtml );
                    league3.SetAttributeValue( "RegisterURL", program.registerUrlHtml );
                    league3.SetAttributeValue( "ScheduleURL", program.scheduleUrlHtml );
                    league3.SetAttributeValue( "StandingsURL", program.standingsUrlHtml );
                    league3.SetAttributeValue( "ProgramLogo", program.programLogo150 );
                    league3.SaveAttributeValues();
                    dbContext.SaveChanges();

                    List<Registrations> applicants;
                    try
                    {
                        applicants = GetRegistrations( apiClient, program.programId );
                    }
                    catch ( Exception ex ) when ( !( ex is LeagueAppsAuthException ) )
                    {
                        // Don't let one bad program export abort the whole job; report it and move on.
                        warnings.Add( "Could not load registrations for program " + program.programId + " (" + program.name + "): " + ex.Message );
                        ExceptionLogService.LogException( ex );
                        skipped++;
                        continue;
                    }

                    UpdateLastStatusMessage( "Processing league " + programNumber + " of " + programs.Count + ": " + program.startTime.Year + " > " + program.mode + " > " + program.name + " (" + applicants.Count + " members)." );

                    var consecutiveMemberFailures = 0;
                    var abandoned = false;

                    foreach ( Contracts.Registrations applicant in applicants )
                    {
                        try
                        {
                            if ( !ImportApplicant( apiClient, applicant, league3, leagueGroupType, groupMemberAttribute, personattribute, connectionStatus, attributeValueService, program, warnings, ref consecutiveMemberFailures ) )
                            {
                                abandoned = true;
                                break;
                            }
                        }
                        catch ( Exception ex ) when ( !( ex is LeagueAppsAuthException ) )
                        {
                            // Isolate per-applicant failures (person create, save, attribute errors) from the rest of the run.
                            warnings.Add( "Could not import user " + applicant.userId + " into program " + program.programId + " (" + program.name + "): " + ex.Message );
                            ExceptionLogService.LogException( ex );
                        }
                    }

                    if ( abandoned )
                    {
                        skipped++;
                    }
                    else
                    {
                        processed++;
                    }
                }

                foreach ( Group sportsleague in groups )
                {
                    sportsleague.IsActive = false;
                    dbContext.SaveChanges();
                }

            }
            catch ( Exception ex )
            {
                // Keep the per-program warnings gathered before the abort; they are not otherwise in the job status.
                var message = "LeagueApps Job Failed: " + ex.Message;
                if ( warnings.Any() )
                {
                    message += Environment.NewLine + BuildSummary( processed, skipped, totalPrograms, warnings );
                }
                throw new Exception( message, ex );
            }
            finally
            {
                dbContext.SaveChanges();
            }

            if ( warnings.Any() )
            {
                throw new Exception( BuildSummary( processed, skipped, totalPrograms, warnings ) );
            }
            Result = "Successfully imported " + processed + " leagues.";
        }

        /// <summary>
        /// Pages through the registrations export for one program. The export returns up to 1000 rows per call,
        /// keyed by the (lastUpdated, id) of the last row; an empty page or empty body ends the export.
        /// The cursor is believed to be exclusive, but a repeated boundary row is dropped in case it is not.
        /// </summary>
        private static List<Registrations> GetRegistrations( APIClient apiClient, int programId )
        {
            var registrations = new List<Registrations>();
            long lastUpdated = 0;
            long lastId = 0;

            while ( true )
            {
                var page = apiClient.GetPrivate<List<Registrations>>( "/v2/sites/{siteid}/export/registrations-2?last-updated=" + lastUpdated + "&last-id=" + lastId + "&program-id=" + programId );
                if ( page != null && lastId != 0 )
                {
                    page.RemoveAll( r => r.lastUpdated == lastUpdated && r.id == lastId );
                }

                if ( page == null || !page.Any() )
                {
                    return registrations;
                }

                registrations.AddRange( page );

                var last = page.Last();
                if ( last.lastUpdated == lastUpdated && last.id == lastId )
                {
                    throw new Exception( "Registrations export did not advance past last-updated=" + lastUpdated + ", last-id=" + lastId + " after " + registrations.Count + " rows." );
                }
                lastUpdated = last.lastUpdated;
                lastId = last.id;
            }
        }

        /// <summary>
        /// Adds or updates one applicant as a member of the league group.
        /// Returns false when the rest of the program should be skipped because member lookups keep failing.
        /// </summary>
        private static bool ImportApplicant( APIClient apiClient, Registrations applicant, Group league, GroupTypeCache leagueGroupType, AttributeCache groupMemberAttribute,
            AttributeCache personattribute, DefinedValueCache connectionStatus, AttributeValueService attributeValueService, Programs program, List<string> warnings, ref int consecutiveMemberFailures )
        {
            // Use a fresh RockContext on every person/groupmember to keep things moving quickly
            using ( var rockContext = new RockContext() )
            {
                PersonService personService = new PersonService( rockContext );
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );

                Person person = null;

                // 1. Try to load the person using the LeagueApps UserId
                var attributevalue = applicant.userId.ToString();
                var personIds = attributeValueService.Queryable().Where( av => av.AttributeId == personattribute.Id &&
                    ( av.Value == attributevalue ||
                      av.Value.Contains( "|" + attributevalue + "|" ) ||
                      av.Value.StartsWith( attributevalue + "|" ) ) ).Select( av => av.EntityId );
                if ( personIds.Count() == 1 )
                {
                    person = personService.Get( personIds.FirstOrDefault().Value );
                }

                // 2. If we don't have a person match then
                //    just use the standard person match/create logic
                if ( person == null )
                {
                    Member member;
                    try
                    {
                        member = apiClient.GetPrivate<Member>( "/v2/sites/{siteid}/members/" + applicant.userId );
                    }
                    catch ( LeagueAppsApiException ex ) when ( ex.StatusCode == System.Net.HttpStatusCode.NotFound )
                    {
                        // A missing member is a data problem, and the API answered, so it resets the failure run.
                        warnings.Add( "LeagueApps has no member " + applicant.userId + " for program " + program.programId + " (" + program.name + ")." );
                        consecutiveMemberFailures = 0;
                        return true;
                    }
                    catch ( Exception ex ) when ( !( ex is LeagueAppsAuthException ) )
                    {
                        warnings.Add( "Could not load member " + applicant.userId + " for program " + program.programId + " (" + program.name + "): " + ex.Message );
                        ExceptionLogService.LogException( ex );

                        // A run of failed lookups means the API or contract is broken, not the data. Applicants already
                        // in Rock make no lookup, so they neither count toward nor reset the run.
                        // Stop hammering it for this program rather than logging once per applicant.
                        consecutiveMemberFailures++;
                        if ( consecutiveMemberFailures >= MaxConsecutiveMemberFailures )
                        {
                            warnings.Add( "Skipping the rest of program " + program.programId + " (" + program.name + ") after " + consecutiveMemberFailures + " consecutive member lookup failures." );
                            return false;
                        }
                        return true;
                    }
                    consecutiveMemberFailures = 0;

                    if ( member == null )
                    {
                        warnings.Add( "LeagueApps returned no member record for user " + applicant.userId + " in program " + program.programId + " (" + program.name + ")." );
                        return true;
                    }

                    person = LeagueAppsHelper.CreatePersonFromMember( member, connectionStatus );
                    if ( person == null )
                    {
                        warnings.Add( "Could not match or create a person for user " + applicant.userId + " in program " + program.programId + " (" + program.name + ")." );
                        return true;
                    }
                }

                // Check to see if the group member already exists
                var groupmember = groupMemberService.GetByGroupIdAndPersonId( league.Id, person.Id ).FirstOrDefault();

                if ( groupmember == null )
                {
                    var roleId = ResolveRoleId( applicant.role, leagueGroupType );
                    if ( !roleId.HasValue )
                    {
                        warnings.Add( "League group type has no '" + MapRoleName( applicant.role ) + "' role and no default role; skipped user " + applicant.userId + " in program " + program.programId + " (" + program.name + ")." );
                        return true;
                    }

                    groupmember = new GroupMember();
                    groupmember.PersonId = person.Id;
                    groupmember.GroupId = league.Id;
                    groupmember.IsSystem = false;
                    groupmember.Guid = Guid.NewGuid();
                    groupmember.GroupRoleId = roleId.Value;
                    groupmember.GroupMemberStatus = GroupMemberStatus.Active;
                    groupMemberService.Add( groupmember );
                    rockContext.SaveChanges();
                }

                // Make sure we update the team if necessary
                groupmember.LoadAttributes();
                groupmember.SetAttributeValue( groupMemberAttribute.Key, applicant.team );
                groupmember.SaveAttributeValues( rockContext );
            }
            return true;
        }

        /// <summary>Maps a LeagueApps role label (e.g. "CAPTAIN (Team A)") to the league group type role name.</summary>
        private static string MapRoleName( string leagueAppsRole )
        {
            if ( string.IsNullOrEmpty( leagueAppsRole ) )
            {
                return null;
            }

            var role = leagueAppsRole.Split( '(' )[0].Trim();

            if ( role == "CAPTAIN" )
                return "Captain";
            else if ( role == "HEAD COACH" || role == "Head Coach" )
                return "Head Coach";
            else if ( role == "ASST. COACH" || role == "Asst. Coach" )
                return "Asst. Coach";
            else
                return "Member";
        }

        /// <summary>
        /// Returns the group role for an applicant, falling back to the group type's default role when the
        /// applicant has no role or the mapped role is missing from the group type.
        /// </summary>
        private static int? ResolveRoleId( string leagueAppsRole, GroupTypeCache leagueGroupType )
        {
            var roleName = MapRoleName( leagueAppsRole );
            var role = roleName == null ? null : leagueGroupType.Roles.FirstOrDefault( r => string.Equals( r.Name, roleName, StringComparison.OrdinalIgnoreCase ) );
            return role?.Id ?? leagueGroupType.DefaultGroupRoleId;
        }

        private static string BuildSummary( int processed, int skipped, int totalPrograms, List<string> warnings )
        {
            // Every warning is already in the exception log; keep the job status message to a sane size.
            var message = "Imported " + processed + " of " + totalPrograms + " leagues";
            if ( skipped > 0 )
            {
                message += " (" + skipped + " skipped)";
            }
            message += " with " + warnings.Count + " warning(s):" + Environment.NewLine
                + string.Join( Environment.NewLine, warnings.Take( MaxWarningsInStatus ) );
            if ( warnings.Count > MaxWarningsInStatus )
            {
                message += Environment.NewLine + "... and " + ( warnings.Count - MaxWarningsInStatus ) + " more (see Exception Log).";
            }
            return message;
        }
    }
}