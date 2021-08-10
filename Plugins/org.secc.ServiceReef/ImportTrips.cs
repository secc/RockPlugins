// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.Data;
using System.Linq;
using org.secc.PersonMatch;
using Quartz;
using RestSharp;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.ServiceReef
{
    [EncryptedTextField( "Service Reef API Key", "Key for authenticating to the ServiceReef API", true, "", "ServiceReef API" )]
    [EncryptedTextField( "Service Reef API Secret", "Secret for authenticating to the ServiceReef API", true, "", "ServiceReef API" )]
    [TextField( "Service Reef API URL", "Service Reef API URL.", true, "", "ServiceReef API" )]
    [GroupField( "Parent Group", "Select the parent level group to use. �The structure of the missions trips will be created under this group with the Year of the trip as a child and the trips themselves as the grandchild.", true, "", "Group Structure" )]
    [GroupTypeField( "Year Group Type", "Select the group type to use for the yearly group level (children).", true, "", "Group Structure" )]
    [GroupTypeField( "Trip Group Type", "Select the group type to use for the trip level (grandchildren).", true, "", "Group Structure" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Default Connection Status", "The connection status to use for newly created people.", true, category: "Job Settings" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "ServiceReef UserId", "Select the person attribute to be populated with the ServiceReef UserId", required: true, allowMultiple: false, category: "Person Attributes" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "ServiceReef Profile URL", "Select the person attribute to be populated with the ServiceReef Profile URL", required: true, allowMultiple: false, category: "Person Attributes" )]
    [SlidingDateRangeField( "Date Range", "The range of dates to import.", false, "Previous|2|Day||", category: "Job Settings" )]
    [DisallowConcurrentExecution]

    public class ImportTrips : IJob
    {
        /// <summary>Process all trips (events) from Service Reef.</summary>
        /// <param name="message">The message that is returned depending on the result.</param>
        /// <param name="state">The state of the process.</param>
        /// <returns><see cref="WorkerResultStatus"/></returns>
        public void Execute( IJobExecutionContext context )
        {
            RockContext dbContext = new RockContext();
            PersonService personService = new PersonService( dbContext );
            PersonAliasService personAliasService = new PersonAliasService( dbContext );
            GroupService groupService = new GroupService( dbContext );
            GroupMemberService groupMemberService = new GroupMemberService( dbContext );
            AttributeService attributeService = new AttributeService( dbContext );
            AttributeValueService attributeValueService = new AttributeValueService( dbContext );
            GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( dbContext );
            DefinedValueService definedValueService = new DefinedValueService( dbContext );
            DefinedTypeService definedTypeService = new DefinedTypeService( dbContext );
            LocationService locationService = new LocationService( dbContext );

            // Get the datamap for loading attributes
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            String warnings = string.Empty;
            var total = 1;
            var processed = 0;

            try
            {
                DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-1||" );

                String SRApiKey = Encryption.DecryptString( dataMap.GetString( "ServiceReefAPIKey" ) );
                String SRApiSecret = Encryption.DecryptString( dataMap.GetString( "ServiceReefAPISecret" ) );
                String SRApiUrl = dataMap.GetString( "ServiceReefAPIURL" );
                DefinedValueCache connectionStatus = DefinedValueCache.Get( dataMap.GetString( "DefaultConnectionStatus" ).AsGuid(), dbContext );
                if ( SRApiUrl.Last() != '/' )
                    SRApiUrl += "/";

                Group group = groupService.Get( dataMap.GetString( "ParentGroup" ).AsGuid() );
                GroupTypeCache parentGroupType = GroupTypeCache.Get( dataMap.Get( "YearGroupType" ).ToString().AsGuid(), dbContext );
                GroupTypeCache groupType = GroupTypeCache.Get( dataMap.Get( "TripGroupType" ).ToString().AsGuid(), dbContext );
                Rock.Model.Attribute attribute = attributeService.Get( dataMap.GetString( "ServiceReefUserId" ).AsGuid() );
                Rock.Model.Attribute attribute2 = attributeService.Get( dataMap.GetString( "ServiceReefProfileURL" ).AsGuid() );
                var entitytype = EntityTypeCache.Get( typeof( Group ) ).Id;

                // Setup the ServiceReef API Client
                var client = new RestClient( SRApiUrl );
                client.Authenticator = new HMACAuthenticator( SRApiKey, SRApiSecret );

                // Get all events from ServiceReef
                var request = new RestRequest( "v1/events", Method.GET );
                request.AddParameter( "pageSize", 100 );
                if ( dateRange.Start.HasValue )
                {
                    request.AddParameter( "startDate", dateRange.Start.Value.ToString( "o" ) );
                }
                if ( dateRange.End.HasValue )
                {
                    request.AddParameter( "endDate", dateRange.End.Value.ToString( "o" ) );
                }
                request.AddParameter( "page", 1 );

                while ( total > processed )
                {
                    var response = client.Execute<Contracts.Events>( request );

                    if ( response.StatusCode != System.Net.HttpStatusCode.OK )
                    {
                        throw new Exception( "ServiceReef API Response: " + response.StatusDescription + " Content Length: " + response.ContentLength );
                    }

                    if ( response.Data != null && response.Data.PageInfo != null )
                    {
                        total = response.Data.PageInfo.TotalRecords;

                        foreach ( Contracts.Events.Result result in response.Data.Results )
                        {
                            // Process the event
                            Group trip = null;
                            Group trip2 = null;
                            var startdate = result.StartDate;
                            var parentgroup = string.Format( "{0} Mission Trips", startdate.Year );

                            if ( result.EventId > 0 )
                            {
                                trip = groupService.Queryable().Where( t => t.Name == parentgroup ).FirstOrDefault();
                                trip2 = groupService.Queryable().Where( t => t.ForeignId == result.EventId && t.GroupTypeId == groupType.Id ).FirstOrDefault();
                            }
                            Guid guid = Guid.NewGuid();
                            Guid guid2 = Guid.NewGuid();

                            if ( trip == null )
                            {
                                // Create trip parent Group
                                Group tripPG = new Group();
                                tripPG.Name = parentgroup;
                                tripPG.GroupTypeId = parentGroupType.Id;
                                tripPG.ParentGroupId = group.Id;
                                tripPG.IsSystem = false;
                                tripPG.IsActive = true;
                                tripPG.IsSecurityRole = false;
                                tripPG.Order = 0;
                                tripPG.Guid = guid;
                                groupService.Add( tripPG );

                                // Now save the trip parent group
                                dbContext.SaveChanges();
                                trip = tripPG;
                            }

                            if ( trip2 == null )
                            {
                                // Create the trip
                                Group tripG = new Group();
                                tripG.Name = result.Name;
                                tripG.GroupTypeId = groupType.Id;
                                tripG.ParentGroupId = trip.Id;
                                tripG.IsSystem = false;
                                tripG.IsActive = true;
                                tripG.IsSecurityRole = false;
                                tripG.Order = 0;
                                tripG.ForeignId = result.EventId;
                                groupService.Add( tripG );

                                // Now save the trip
                                dbContext.SaveChanges();
                                trip2 = tripG;
                            }
                            trip2.LoadAttributes();

                            if ( startdate != DateTime.MinValue )
                            {
                                trip2.SetAttributeValue( "Year", startdate.Year.ToString() );
                                trip2.SetAttributeValue( "Month", startdate.ToString( "MMMM" ) );
                                trip2.SaveAttributeValues();
                            }
                            dbContext.SaveChanges();
                            var eventRequest = new RestRequest( "v1/events/{eventId}", Method.GET );
                            eventRequest.AddUrlSegment( "eventId", result.EventId.ToString() );
                            var eventResult = client.Execute<Contracts.Event>( eventRequest );

                            if ( eventResult.Data != null && eventResult.Data.Categories.Count > 0 )
                            {
                                foreach ( Contracts.Event.CategorySimple categorysimple in eventResult.Data.Categories )
                                {
                                    var option = categorysimple.Options.FirstOrDefault();

                                    if ( option != null )
                                    {
                                        trip2.SetAttributeValue( categorysimple.Name.RemoveAllNonAlphaNumericCharacters(), option.Name );
                                        trip2.SaveAttributeValues();
                                    }
                                }
                                dbContext.SaveChanges();
                            }
                            var amount = 1;
                            var handled = 0;
                            String rest = String.Format( "v1/events/{0}/participants", result.EventId );
                            var request2 = new RestRequest( rest, Method.GET );
                            request2.AddParameter( "pageSize", 100 );
                            request2.AddParameter( "page", 1 );

                            // We haven't processed this before so get busy!
                            while ( amount > handled )
                            {
                                var response2 = client.Execute<Contracts.Participants>( request2 );

                                if ( response2.StatusCode != System.Net.HttpStatusCode.OK )
                                {
                                    throw new Exception( "ServiceReef API Response: " + response2.StatusDescription + " Content Length: " + response2.ContentLength );
                                }

                                if ( response2.Data != null && response2.Data.PageInfo != null )
                                {
                                    amount = response2.Data.PageInfo.TotalRecords;

                                    foreach ( Contracts.Participants.Result result2 in response2.Data.Results )
                                    {
                                        Person person = null;
                                        if ( result2.RegistrationStatus != "Draft" )
                                        {
                                            if ( result2.UserId > 0 )
                                            {
                                                var memberRequest = new RestRequest( "v1/members/{userId}", Method.GET );
                                                memberRequest.AddUrlSegment( "userId", result2.UserId.ToString() );
                                                var memberResult = client.Execute<Contracts.Member>( memberRequest );

                                                if ( memberResult.Data != null && memberResult.Data.ArenaId > 0 )
                                                {

                                                    Person personMatch = personAliasService.Queryable().Where( pa => pa.AliasPersonId == memberResult.Data.ArenaId ).Select( pa => pa.Person ).FirstOrDefault();

                                                    if ( personMatch != null )
                                                    {
                                                        person = personMatch;
                                                    }
                                                }
                                            }

                                            // 2. If we didn't get a person match via their Alias Id
                                            //    then use their ServiceReef UserId attribute
                                            if ( person == null && attribute != null )
                                            {
                                                var personIds = attributeValueService.Queryable().Where( av => av.AttributeId == attribute.Id && av.Value == result2.UserId.ToString() ).Select( av => av.EntityId );
                                                if ( personIds.Count() == 1 )
                                                {
                                                    person = personService.Get( personIds.FirstOrDefault().Value );
                                                }
                                            }

                                            // 3. If we STILL don't have a person match then
                                            //    just use the standard person match/create logic
                                            if ( person == null )
                                            {
                                                String street1 = null;
                                                String postalCode = null;

                                                if ( result2.Address != null )
                                                {
                                                    street1 = result2.Address.Address1;
                                                    postalCode = result2.Address.Zip;
                                                }
                                                var email = result2.Email.Trim();
                                                List<Person> matches = null;
                                                Location homelocation = new Location();

                                                if ( !email.IsValidEmail() )
                                                {
                                                    email = null;
                                                    matches = personService.Queryable()
                                                    .Where( p => p.FirstName == result2.FirstName.Trim() && p.LastName == result2.LastName.Trim() ).ToList();

                                                    if ( matches.Count() > 0 && !string.IsNullOrEmpty( street1 ) && !string.IsNullOrEmpty( postalCode ) )
                                                    {
                                                        homelocation.Street1 = street1;
                                                        homelocation.PostalCode = postalCode;
                                                        locationService.Verify( homelocation, true );
                                                    }

                                                    foreach ( Person match in matches )
                                                    {
                                                        Boolean addressMatches = false;
                                                        // Check the address
                                                        if ( !string.IsNullOrEmpty( street1 ) && !string.IsNullOrEmpty( postalCode ) )
                                                        {
                                                            if ( match.GetHomeLocation( dbContext ) != null )
                                                            {
                                                                if ( match.GetHomeLocation( dbContext ).Street1 == street1 && match.GetHomeLocation( dbContext ).PostalCode.Split( '-' )[0] == postalCode )
                                                                {
                                                                    addressMatches = true;
                                                                }

                                                            }
                                                        }

                                                        if ( !addressMatches )
                                                        {
                                                            matches.Remove( person );
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    matches = personService.GetByMatch( result2.FirstName.Trim(), result2.LastName.Trim(), null, email, null, street1, postalCode ).ToList();

                                                }

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
                                                    Guid guid3 = Guid.NewGuid();
                                                    person = new Person();
                                                    person.FirstName = result2.FirstName.Trim();
                                                    person.LastName = result2.LastName.Trim();
                                                    if ( email.IsValidEmail() )
                                                    {
                                                        person.Email = email;
                                                    }
                                                    person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                                                    person.ConnectionStatusValueId = connectionStatus.Id;
                                                    person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                                                    person.IsSystem = false;
                                                    person.IsDeceased = false;
                                                    person.IsEmailActive = true;
                                                    person.Guid = guid3;
                                                    var masteranswer = result2.MasterApplicationAnswers.Find( a => a.Question == "Gender" );
                                                    var answer = result2.ApplicationAnswers.Find( a => a.Question == "Gender" );
                                                    var flag = true;

                                                    if ( masteranswer != null )
                                                    {
                                                        var gender = masteranswer.Answer;

                                                        if ( !String.IsNullOrEmpty( gender ) )
                                                        {
                                                            gender.Trim();

                                                            if ( gender == "Male" || gender == "male" )
                                                                person.Gender = Gender.Male;
                                                            else
                                                                person.Gender = Gender.Female;
                                                            flag = false;
                                                        }

                                                    }

                                                    if ( answer != null && flag == true )
                                                    {
                                                        var gender2 = answer.Answer;

                                                        if ( !String.IsNullOrEmpty( gender2 ) )
                                                        {
                                                            gender2.Trim();

                                                            if ( gender2 == "Male" || gender2 == "male" )
                                                                person.Gender = Gender.Male;
                                                            else
                                                                person.Gender = Gender.Female;
                                                        }
                                                        else
                                                        {
                                                            person.Gender = Gender.Unknown;
                                                        }
                                                    }
                                                    else if ( flag == true )
                                                    {
                                                        person.Gender = Gender.Unknown;
                                                    }
                                                    Group family = PersonService.SaveNewPerson( person, dbContext );
                                                    GroupLocation location = new GroupLocation();
                                                    location.GroupLocationTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME ).Id;
                                                    location.Location = new Location()
                                                    {
                                                        Street1 = result2.Address.Address1,
                                                        Street2 = result2.Address.Address2,
                                                        City = result2.Address.City,
                                                        State = result2.Address.State,
                                                        PostalCode = result2.Address.Zip,
                                                        Country = result2.Address.Country
                                                    };
                                                    location.IsMappedLocation = true;
                                                    family.CampusId = CampusCache.All().FirstOrDefault().Id;
                                                    family.GroupLocations.Add( location );

                                                    dbContext.SaveChanges();
                                                }
                                            }
                                            Guid guid4 = Guid.NewGuid();
                                            Guid guid5 = Guid.NewGuid();
                                            var userid = result2.UserId;
                                            var profileurl = result2.ProfileUrl;
                                            person.LoadAttributes();

                                            if ( userid > 0 )
                                            {
                                                person.SetAttributeValue( attribute.Key, result2.UserId.ToString() );
                                            }

                                            if ( !String.IsNullOrEmpty( profileurl ) )
                                            {
                                                person.SetAttributeValue( attribute2.Key, result2.ProfileUrl );
                                            }
                                            person.SaveAttributeValues();
                                            dbContext.SaveChanges();
                                            var member = groupService.GroupHasMember( trip2.Guid, person.Id );

                                            if ( member == false )
                                            {
                                                Guid guid6 = Guid.NewGuid();
                                                GroupMember participant = new GroupMember();
                                                participant.PersonId = person.Id;
                                                participant.GroupId = trip2.Id;
                                                participant.IsSystem = false;
                                                participant.Guid = guid6;
                                                var grouprole = groupTypeRoleService.Queryable().Where( r => r.GroupTypeId == trip2.GroupTypeId ).FirstOrDefault().Id;
                                                participant.GroupRoleId = groupType.DefaultGroupRoleId.GetValueOrDefault( grouprole );

                                                if ( result2.RegistrationStatus == "Approved" )
                                                    participant.GroupMemberStatus = GroupMemberStatus.Active;
                                                else if ( result2.RegistrationStatus == "Cancelled" )
                                                    participant.GroupMemberStatus = GroupMemberStatus.Inactive;
                                                else
                                                    participant.GroupMemberStatus = GroupMemberStatus.Pending;

                                                groupMemberService.Add( participant );
                                                dbContext.SaveChanges();
                                            }
                                        }
                                        handled++;
                                    }
                                }
                                else
                                {
                                    amount = 0;
                                }
                                var pageParam2 = request2.Parameters.Where( p => p.Name == "page" ).FirstOrDefault();
                                pageParam2.Value = ( int ) pageParam2.Value + 1;
                            }
                            processed++;
                        }
                    }
                    else
                    {
                        total = 0;
                    }
                    // Update the page number for the next request
                    var pageParam = request.Parameters.Where( p => p.Name == "page" ).FirstOrDefault();
                    pageParam.Value = ( int ) pageParam.Value + 1;
                }
            }
            catch ( Exception ex )
            {
                throw new Exception( "ServiceReef Job Failed", ex );
            }
            finally
            {
                dbContext.SaveChanges();
            }

            if ( warnings.Length > 0 )
            {
                throw new Exception( warnings );
            }
            context.Result = "Successfully imported " + processed + " trips.";
        }
    }
}