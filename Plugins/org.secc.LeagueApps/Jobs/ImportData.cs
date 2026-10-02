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
            GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( dbContext );
            DefinedValueService definedValueService = new DefinedValueService( dbContext );
            DefinedTypeService definedTypeService = new DefinedTypeService( dbContext );
            BinaryFileService binaryFileService = new BinaryFileService( dbContext );

            var warnings = new List<string>();
            var processed = 0;

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


                var groups = groupService.Queryable().Where( g => g.GroupTypeId == leagueGroupType.Id ).ToList();

                foreach ( Contracts.Programs program in programs )
                {
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
                        // An empty body deserializes to null; treat that as a program with no registrations.
                        applicants = apiClient.GetPrivate<List<Registrations>>( "/v2/sites/{siteid}/export/registrations-2?last-updated=0&last-id=0&program-id=" + program.programId )
                            ?? new List<Registrations>();
                    }
                    catch ( LeagueAppsAuthException )
                    {
                        // Every later call would fail the same way; fail fast instead of warning per program.
                        throw;
                    }
                    catch ( Exception ex )
                    {
                        // Don't let one bad program export abort the whole job; report it and move on.
                        warnings.Add( "Could not load registrations for program " + program.programId + " (" + program.name + "): " + ex.Message );
                        ExceptionLogService.LogException( ex );
                        processed++;
                        continue;
                    }

                    UpdateLastStatusMessage( "Processing league " + ( processed + 1 ) + " of " + programs.Count + ": " + program.startTime.Year + " > " + program.mode + " > " + program.name + " (" + applicants.Count + " members)." );

                    var consecutiveMemberFailures = 0;

                    foreach ( Contracts.Registrations applicant in applicants )
                    {
                        // Use a fresh RockContext on every person/groupmember to keep things moving quickly
                        using ( var rockContext = new RockContext() )
                        {
                            PersonService personService = new PersonService( rockContext );
                            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                            LocationService locationService = new LocationService( rockContext );

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
                                catch ( LeagueAppsAuthException )
                                {
                                    throw;
                                }
                                catch ( Exception ex )
                                {
                                    warnings.Add( "Could not load member " + applicant.userId + " for program " + program.programId + " (" + program.name + "): " + ex.Message );
                                    ExceptionLogService.LogException( ex );

                                    // A run of identical failures means the API or contract is broken, not the data.
                                    // Stop hammering it for this program rather than logging once per applicant.
                                    consecutiveMemberFailures++;
                                    if ( consecutiveMemberFailures >= MaxConsecutiveMemberFailures )
                                    {
                                        warnings.Add( "Skipping the rest of program " + program.programId + " (" + program.name + ") after " + consecutiveMemberFailures + " consecutive member lookup failures." );
                                        break;
                                    }
                                    continue;
                                }
                                consecutiveMemberFailures = 0;

                                if ( member == null )
                                {
                                    warnings.Add( "LeagueApps returned no member record for user " + applicant.userId + " in program " + program.programId + " (" + program.name + ")." );
                                    continue;
                                }

                                person = LeagueAppsHelper.CreatePersonFromMember( member, connectionStatus );
                            }

                            // Check to see if the group member already exists
                            var groupmember = groupMemberService.GetByGroupIdAndPersonId( league3.Id, person.Id ).FirstOrDefault();

                            if ( groupmember == null )
                            {
                                Guid guid5 = Guid.NewGuid();
                                groupmember = new GroupMember();
                                groupmember.PersonId = person.Id;
                                groupmember.GroupId = league3.Id;
                                groupmember.IsSystem = false;
                                groupmember.Guid = guid5;

                                if ( !String.IsNullOrEmpty( applicant.role ) )
                                {
                                    var role = applicant.role.Split( '(' )[0].Trim();

                                    if ( role == "FREEAGENT" || role == "PLAYER" )
                                        role = "Member";
                                    else if ( role == "CAPTAIN" )
                                        role = "Captain";
                                    else if ( role == "HEAD COACH" || role == "Head Coach" )
                                        role = "Head Coach";
                                    else if ( role == "ASST. COACH" || role == "Asst. Coach" )
                                        role = "Asst. Coach";
                                    else
                                        role = "Member";
                                    var grouprole = groupTypeRoleService.Queryable().Where( r => r.GroupTypeId == leagueGroupType.Id && r.Name == role ).FirstOrDefault().Id;
                                    groupmember.GroupRoleId = grouprole;
                                }
                                else
                                {
                                    groupmember.GroupRoleId = leagueGroupType.DefaultGroupRoleId.Value;
                                }
                                groupmember.GroupMemberStatus = GroupMemberStatus.Active;
                                groupMemberService.Add( groupmember );
                                rockContext.SaveChanges();
                            }

                            // Make sure we update the team if necessary
                            groupmember.LoadAttributes();
                            groupmember.SetAttributeValue( groupMemberAttribute.Key, applicant.team );
                            groupmember.SaveAttributeValues( rockContext );
                        }
                    }
                    processed++;
                }

                foreach ( Group sportsleague in groups )
                {
                    sportsleague.IsActive = false;
                    dbContext.SaveChanges();
                }

            }
            catch ( Exception ex )
            {
                throw new Exception( "LeagueApps Job Failed", ex );
            }
            finally
            {
                dbContext.SaveChanges();
            }

            if ( warnings.Any() )
            {
                // Every warning is already in the exception log; keep the job status message to a sane size.
                var message = "Imported " + processed + " leagues with " + warnings.Count + " warning(s):" + Environment.NewLine
                    + string.Join( Environment.NewLine, warnings.Take( MaxWarningsInStatus ) );
                if ( warnings.Count > MaxWarningsInStatus )
                {
                    message += Environment.NewLine + "... and " + ( warnings.Count - MaxWarningsInStatus ) + " more (see Exception Log).";
                }
                throw new Exception( message );
            }
            Result = "Successfully imported " + processed + " leagues.";
        }
    }
}