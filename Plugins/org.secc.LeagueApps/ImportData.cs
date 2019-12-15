using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Security;
using Rock.Model;
using Rock.Data;
using System.IO;
using Rock.Web.Cache;
using System.Linq;
using org.secc.PersonMatch;

namespace org.secc.LeagueApps
{
    [EncryptedTextField("LeagueApps Site Id", "Id for LeagueApps site", true, "", "LeagueApps")]
    [EncryptedTextField("LeagueApps Client Id", "Client Id for authenticating to the LeagueApps API", true, "", "LeagueApps API")]
    [FileField(Rock.SystemGuid.BinaryFiletype.DEFAULT, "LeagueApps Service Account File", "PKCS12 file containing private key for authenticating to the LeagueApps API", true, category: "LeagueApps API")]
    [GroupField("Parent Group", "Select the grand parent level group to use.  The structure of the sports leagues will be created under this group with the year of the league as a child, the category of the league as a grandchild, and the league itself as the great-grandchild.", true, "", "Group Structure")]
    [GroupTypeField("Year Group Type", "Select the group type to use for the yearly group level (children).", true, "", "Group Structure")]
    [GroupTypeField("Category Group Type", "Select the group type to use for the category group level (grandchildren).", true, "", "Group Structure")]
    [GroupTypeField("League Group Type", "Select the group type to use for the league level (great-grandchildren).", true, "", "Group Structure")]
    [DefinedTypeField("Sports Type", "The sport type containing the sports to use for all league groups", true, category: "Job Settings")]
    [DefinedTypeField("Seasons Type", "The season type containing the seasons to use for all league groups", true, category: "Job Settings")]
    [DefinedTypeField("Genders Type", "The gender type containing the gender values to use for all league groups", true, category: "Job Settings")]
    [DefinedValueField(Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Default Connection Status", "The connection status to use for newly created people.", true, category: "Job Settings")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "LeagueApps UserId", "Select the person attribute to be populated with the LeagueApps UserId", required: true, allowMultiple: false, category: "Person Attribute")]
    [AttributeField(Rock.SystemGuid.EntityType.GROUP_MEMBER, "League Group Team", "Select the group member attribute to be populated with the league group team", required: true, allowMultiple: false, category: "Group Member Attribute")]
    [DisallowConcurrentExecution]

    public class ImportData : IJob
    {
        /// <summary>Process all leagues (programs) from LeagueApps.</summary>
        /// <param name="message">The message that is returned depending on the result.</param>
        /// <param name="state">The state of the process.</param>
        /// <returns><see cref="WorkerResultStatus"/></returns>
        public void Execute(IJobExecutionContext context)
        {
            RockContext dbContext = new RockContext();
            GroupService groupService = new GroupService(dbContext);
            AttributeService attributeService = new AttributeService(dbContext);
            AttributeValueService attributeValueService = new AttributeValueService(dbContext);
            GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService(dbContext);
            DefinedValueService definedValueService = new DefinedValueService(dbContext);
            DefinedTypeService definedTypeService = new DefinedTypeService(dbContext);
            LocationService locationService = new LocationService(dbContext);
            BinaryFileService binaryFileService = new BinaryFileService(dbContext);

            // Get the datamap for loading attributes
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            String warnings = string.Empty;
            var processed = 0;

            try
            {
                String siteid = Encryption.DecryptString(dataMap.GetString("LeagueAppsSiteId"));
                String clientid = Encryption.DecryptString(dataMap.GetString("LeagueAppsClientId"));
                DefinedValueCache connectionStatus = DefinedValueCache.Get(dataMap.GetString("DefaultConnectionStatus").AsGuid(), dbContext);
                var p12File = binaryFileService.Get(dataMap.GetString("LeagueAppsServiceAccountFile").AsGuid());


                Group group = groupService.Get(dataMap.GetString("ParentGroup").AsGuid());
                GroupTypeCache grandparentGroupType = GroupTypeCache.Get(dataMap.Get("YearGroupType").ToString().AsGuid(), dbContext);
                GroupTypeCache parentGroupType = GroupTypeCache.Get(dataMap.Get("CategoryGroupType").ToString().AsGuid(), dbContext);
                GroupTypeCache groupType = GroupTypeCache.Get(dataMap.Get("LeagueGroupType").ToString().AsGuid(), dbContext);
                DefinedTypeCache sports = DefinedTypeCache.Get(dataMap.Get("SportsType").ToString().AsGuid(), dbContext);
                DefinedTypeCache seasons = DefinedTypeCache.Get(dataMap.Get("SeasonsType").ToString().AsGuid(), dbContext);
                DefinedTypeCache genders = DefinedTypeCache.Get(dataMap.Get("GendersType").ToString().AsGuid(), dbContext);
                Rock.Model.Attribute personattribute = attributeService.Get(dataMap.GetString("LeagueAppsUserId").AsGuid());
                Rock.Model.Attribute groupmemberattribute = attributeService.Get(dataMap.GetString("LeagueGroupTeam").AsGuid());
                var entitytype = EntityTypeCache.Get(typeof(Group)).Id;

                var client = new RestClient("https://public.leagueapps.io");

                // Get all programs from LeagueApps
                var request = new RestRequest("/v1/sites/{siteid}/programs/current", Method.GET);
                request.AddUrlSegment("siteid", siteid);
                request.AddHeader("la-api-key", clientid);

                var response = client.Get(request);

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception("LeagueApps API Response: " + response.StatusDescription + " Content Length: " + response.ContentLength);
                }

                if (response.Content != null)
                {
                    var export = response.Content.ToString();
                    var programs = JsonConvert.DeserializeObject<List<Contracts.Programs>>(export);
                    var groups = groupService.Queryable().Where(g => g.GroupTypeId == groupType.Id).ToList();

                    foreach(Contracts.Programs program in programs)
                    {

                        // Process the program
                        Group league = null;
                        Group league2 = null;
                        Group league3 = null;
                        var startdate = program.startTime;
                        var mode = program.mode.ToLower();
                        mode = mode.First().ToString().ToUpper() + mode.Substring(1);
                        var grandparentgroup = string.Format("{0}", startdate.Year);
                        if (program.programId > 0)
                        {
                            league = groupService.Queryable().Where(l => l.Name == grandparentgroup && l.ParentGroupId == group.Id ).FirstOrDefault();
                            if ( league != null )
                            {
                                league2 = groupService.Queryable().Where( l => l.Name == mode && l.ParentGroupId == league.Id ).FirstOrDefault();
                                if ( league2 != null)
                                {
                                    league3 = groupService.Queryable().Where( l => l.ForeignId == program.programId && l.GroupTypeId == groupType.Id && l.ParentGroupId == league2.Id ).FirstOrDefault();
                                }
                            }                           
                        }
                        Guid guid = Guid.NewGuid();
                        Guid guid2 = Guid.NewGuid();
                        Guid guid3 = Guid.NewGuid();

                        if(league == null)
                        {
                            // Create league grandparent Group
                            Group leagueGPG = new Group();
                            leagueGPG.Name = grandparentgroup;
                            leagueGPG.GroupTypeId = grandparentGroupType.Id;
                            leagueGPG.ParentGroupId = group.Id;
                            leagueGPG.IsSystem = false;
                            leagueGPG.IsActive = true;
                            leagueGPG.IsSecurityRole = false;
                            leagueGPG.Order = 0;
                            leagueGPG.Guid = guid;
                            groupService.Add(leagueGPG);

                            // Now save the league grandparent group
                            dbContext.SaveChanges();
                            league = leagueGPG;
                        }

                        if (league2 == null)
                        {
                            // Create league parent Group
                            Group leaguePG = new Group();
                            leaguePG.Name = mode;
                            leaguePG.GroupTypeId = parentGroupType.Id;
                            leaguePG.ParentGroupId = league.Id;
                            leaguePG.IsSystem = false;
                            leaguePG.IsActive = true;
                            leaguePG.IsSecurityRole = false;
                            leaguePG.Order = 0;
                            leaguePG.Guid = guid2;
                            groupService.Add(leaguePG);

                            // Now save the league parent group
                            dbContext.SaveChanges();
                            league2 = leaguePG;
                        }

                        if (league3 == null)
                        {
                            // Create the league
                            Group leagueG = new Group();
                            leagueG.Name = program.name;
                            leagueG.GroupTypeId = groupType.Id;
                            leagueG.ParentGroupId = league2.Id;
                            leagueG.IsSystem = false;
                            leagueG.IsActive = true;
                            leagueG.IsSecurityRole = false;
                            leagueG.Order = 0;
                            leagueG.Description = HTMLConvertor.Convert(program.description);
                            leagueG.ForeignId = program.programId;
                            groupService.Add(leagueG);

                            // Now save the league
                            dbContext.SaveChanges();
                            league3 = leagueG;
                        }
                        else
                        {
                            groups.Remove(league3);
                        }
                        league3.LoadAttributes();
                        var sport = definedValueService.Queryable().Where(d => d.Value == program.sport && d.DefinedTypeId == sports.Id).FirstOrDefault();
                        var season = definedValueService.Queryable().Where(d => d.Value == program.season && d.DefinedTypeId == seasons.Id).FirstOrDefault();
                        var groupgender = definedValueService.Queryable().Where(d => d.Value == program.gender && d.DefinedTypeId == genders.Id).FirstOrDefault();

                        if (!sport.IsNull())
                            league3.SetAttributeValue("Sport", sport.Guid);

                        if (!season.IsNull())
                            league3.SetAttributeValue("Season", season.Guid);
                        league3.SetAttributeValue("ExperienceLevel", program.experienceLevel);

                        if (!groupgender.IsNull())
                            league3.SetAttributeValue("Gender", groupgender.Guid);

                        if (startdate != DateTime.MinValue)
                            league3.SetAttributeValue("StartTime", startdate);

                        if (program.publicRegistrationTime != DateTime.MinValue)
                            league3.SetAttributeValue("PublicRegistrationTime", program.publicRegistrationTime);

                        if (program.ageLimitEffectiveDate != DateTime.MinValue)
                            league3.SetAttributeValue("AgeLimitDate", program.ageLimitEffectiveDate.Date.ToString("d"));
                        league3.SetAttributeValue("ProgramURL", program.programUrlHtml);
                        league3.SetAttributeValue("RegisterURL", program.registerUrlHtml);
                        league3.SetAttributeValue("ScheduleURL", program.scheduleUrlHtml);
                        league3.SetAttributeValue("StandingsURL", program.standingsUrlHtml);
                        league3.SetAttributeValue("ProgramLogo", program.programLogo150);
                        league3.SaveAttributeValues();
                        dbContext.SaveChanges();
                        APIClient.RunAsync(p12File, clientid, true, "/v2/sites/" + siteid + "/export/registrations-2?last-updated=0&last-id=0&program-id=" + program.programId).GetAwaiter().GetResult();
                        var applicants = APIClient.names;

                        context.UpdateLastStatusMessage( "Processing league " + (processed + 1) + " of " + programs.Count + ": " + program.startTime.Year + " > " + program.mode + " > " + program.name + " ("+applicants.Count +" members)." );

                        foreach (Contracts.Registrations applicant in applicants)
                        {
                            // Use a fresh RockContext on every person/groupmember to keep things moving quickly
                            using ( var rockContext = new RockContext() )
                            {
                                PersonService personService = new PersonService( rockContext );
                                GroupMemberService groupMemberService = new GroupMemberService( rockContext );

                                Person person = null;
                                
                                // 1. Try to load the person using the LeagueApps UserId
                                var personIds = attributeValueService.Queryable().Where( av => av.AttributeId == personattribute.Id && av.Value == applicant.userId.ToString() ).Select( av => av.EntityId );
                                if ( personIds.Count() == 1 )
                                {
                                    person = personService.Get( personIds.FirstOrDefault().Value );
                                }

                                // 2. If we don't have a person match then
                                //    just use the standard person match/create logic
                                if ( person == null )
                                {
                                    APIClient.RunAsync( p12File, clientid, false, "/v2/sites/" + siteid + "/members/" + applicant.userId ).GetAwaiter().GetResult();
                                    var member = APIClient.user;
                                    var street1 = member.address1;
                                    var postalCode = member.zipCode;

                                    var email = string.IsNullOrEmpty( member.email ) ? String.Empty : member.email.Trim();

                                    if ( email != String.Empty )
                                    {
                                        if ( !email.IsValidEmail() )
                                            email = String.Empty;
                                    }

                                    List<Person> matches = personService.GetByMatch( member.firstName.Trim(), member.lastName.Trim(), null, email, null, street1, postalCode ).ToList();

                                    if ( matches.Count > 1 )
                                    {
                                        // Find the oldest member record in the list
                                        person = matches.Where( p => p.ConnectionStatusValue.Value == "Member" ).OrderBy( p => p.Id ).FirstOrDefault();

                                        if ( person == null )
                                        {
                                            // Find the oldest attendee record in the list
                                            person = matches.Where( p => p.ConnectionStatusValue.Value == "Attendee" ).OrderBy( p => p.Id ).FirstOrDefault();
                                            if ( person == null )
                                            {
                                                person = matches.OrderBy( p => p.Id ).First();
                                            }
                                        }
                                    }
                                    else if ( matches.Count == 1 )
                                    {
                                        person = matches.First();
                                    }
                                    else
                                    {
                                        // Create the person
                                        Guid guid4 = Guid.NewGuid();
                                        person = new Person();
                                        person.FirstName = member.firstName.Trim();
                                        person.LastName = member.lastName.Trim();

                                        if ( email != String.Empty )
                                            person.Email = email;
                                        person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                                        person.ConnectionStatusValueId = connectionStatus.Id;
                                        person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                                        person.IsSystem = false;
                                        person.IsDeceased = false;
                                        person.Guid = guid4;
                                        var gender = member.gender;

                                        if ( !String.IsNullOrEmpty( gender ) )
                                        {
                                            gender.Trim();

                                            if ( gender == "Male" || gender == "male" )
                                                person.Gender = Gender.Male;
                                            else if ( gender == "Female" || gender == "female" )
                                                person.Gender = Gender.Female;
                                            else
                                                person.Gender = Gender.Unknown;
                                        }
                                        else
                                        {
                                            person.Gender = Gender.Unknown;
                                        }
                                        Group family = PersonService.SaveNewPerson( person, rockContext );
                                        GroupLocation location = new GroupLocation();
                                        location.GroupLocationTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME ).Id;
                                        location.Location = new Location()
                                        {
                                            Street1 = member.address1,
                                            Street2 = member.address2,
                                            City = member.city,
                                            State = member.state,
                                            PostalCode = member.zipCode,
                                            Country = member.country
                                        };
                                        location.IsMappedLocation = true;
                                        family.CampusId = CampusCache.All().FirstOrDefault().Id;
                                        family.GroupLocations.Add( location );
                                        rockContext.SaveChanges();
                                    }
                                    person.LoadAttributes();
                                    person.SetAttributeValue( personattribute.Key, applicant.userId.ToString() );
                                    person.SaveAttributeValues();
                                    rockContext.SaveChanges();
                                }

                                // Check to see if the group member already exists
                                var groupmember = groupService.GroupHasMember( league3.Guid, person.Id );

                                if ( groupmember == false )
                                {
                                    Guid guid5 = Guid.NewGuid();
                                    GroupMember participant = new GroupMember();
                                    participant.PersonId = person.Id;
                                    participant.GroupId = league3.Id;
                                    participant.IsSystem = false;
                                    participant.Guid = guid5;

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
                                        var grouprole = groupTypeRoleService.Queryable().Where( r => r.GroupTypeId == groupType.Id && r.Name == role ).FirstOrDefault().Id;
                                        participant.GroupRoleId = grouprole;
                                    }
                                    else
                                    {
                                        participant.GroupRoleId = groupType.DefaultGroupRoleId.Value;
                                    }
                                    participant.GroupMemberStatus = GroupMemberStatus.Active;
                                    groupMemberService.Add( participant );
                                    participant.LoadAttributes();
                                    participant.SetAttributeValue( groupmemberattribute.Key, applicant.team );
                                    participant.SaveAttributeValues();
                                    rockContext.SaveChanges();
                                }
                            }
                        }
                        processed++;
                    }

                    foreach(Group sportsleague in groups)
                    {
                        sportsleague.IsActive = false;
                        dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("LeagueApps Job Failed", ex);
            }
            finally
            {
                dbContext.SaveChanges();
            }

            if (warnings.Length > 0)
            {
                throw new Exception(warnings);
            }
            context.Result = "Successfully imported " + processed + " leagues.";
        }
    }
}