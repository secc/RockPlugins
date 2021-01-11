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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using Newtonsoft.Json;
using org.secc.PersonMatch;
using org.secc.Rise.Components;
using org.secc.Rise.Utilities;
using org.secc.xAPI.Component;
using org.secc.xAPI.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Rise.Response
{
    [Url( "users" )]
    public class RiseUser : RiseBase
    {

        [JsonProperty( "email" )]
        public string Email { get; set; }

        [JsonProperty( "role" )]
        public string Role { get; set; }

        [JsonProperty( "firstName" )]
        public string FirstName { get; set; }

        [JsonProperty( "lastName" )]
        public string LastName { get; set; }

        [JsonProperty( "learnerReportUrl" )]
        public string LearnerReportUrl { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        [JsonIgnore]
        public List<RiseGroup> Groups
        {
            get
            {
                return ClientManager.GetSet<RiseGroup>( this ).ToList();
            }
        }

        [JsonIgnore]
        public List<RiseLearnerCourse> CourseReports
        {
            get
            {
                return ClientManager.GetSet<RiseLearnerCourse>( this.Id ).ToList();
            }
        }

        public void SyncGroupMembership()
        {
            SyncGroupMembership( GetRockPerson() );
        }

        public void SyncGroupMembership( Person person )
        {
            var riseGroups = Groups;
            person.LoadAttributes();

            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var grouptypeId = Constants.GetRiseGroupTypeId();

            var groups = groupMemberService.Queryable()
                .Where( gm => gm.PersonId == person.Id
                    && gm.Group.GroupTypeId == grouptypeId )
                .Select( gm => gm.Group )
                .ToList();

            //Not fast. Fix if needed.
            groups.ForEach( g => g.LoadAttributes() );

            var groupsToRemove = riseGroups
               .Where( r => !groups.Select( g => g.GetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID ) ).Contains( r.Id ) )
               .ToList();

            foreach ( var riseGroup in groupsToRemove )
            {
                ClientManager.Delete<RiseUser>( riseGroup, Id );
            }

            var groupsToAdd = groups
                .Where( g => !riseGroups.Select( r => r.Id ).Contains( g.GetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID ) ) )
                .ToList();


            foreach ( var group in groupsToAdd )
            {
                var riseGroup = new RiseGroup { Id = group.GetAttributeValue( Constants.GROUP_ATTRIBUTE_KEY_RISEID ) };
                ClientManager.Put<RiseUser>( riseGroup, person.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ) );
            }
        }

        public Person GetRockPerson()
        {
            Person person = GetPerson( this.Id );

            if ( person != null )
            {
                return person;
            }

            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            var people = personService.GetByMatch( FirstName, LastName, null, Email );
            if ( people.Count() == 1 )
            {
                person = people.FirstOrDefault();

                person.LoadAttributes();
                person.SetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID, Id );
                person.SaveAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID );

                SaveUserCreated( person );

                return person;
            }
            else //Webprospect 
            {
                person = new Person
                {
                    FirstName = FirstName,
                    NickName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    EmailPreference = EmailPreference.EmailAllowed,
                    RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id,
                    ConnectionStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT ).Id,
                    RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING ).Id,
                };

                personService.Add( person );
                PersonService.SaveNewPerson( person, rockContext );
                rockContext.SaveChanges();

                person.LoadAttributes();
                person.SetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID, Id );
                person.SaveAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID );

                SaveUserCreated( person );

                return person;
            }
        }

        internal void SaveUserCreated( Person person )
        {
            RockContext rockContext = new RockContext();
            ExperienceService experienceService = new ExperienceService( rockContext );

            var verb = xAPI.Utilities.VerbHelper.GetOrCreateVerb( "http://activitystrea.ms/schema/1.0/join" );

            var experience = new Experience
            {
                PersonAliasId = person.PrimaryAliasId ?? 0,
                VerbValueId = verb.Id,
                xObject = new ExperienceObject
                {
                    EntityTypeId = EntityTypeCache.Get( typeof( xAPIComponent ) ).Id,
                    ObjectId = EntityTypeCache.Get( typeof( RiseComponent ) ).Id.ToString()
                },
                Result = new ExperienceResult
                {
                    WasSuccess = true,
                    IsComplete = true
                }
            };

            experienceService.Add( experience );
            rockContext.SaveChanges();

            var context = experience.AddQualifier( "context" );
            var experiences = context.AddQualifier( "experiences" );
            experiences.AddQualifier(
                xAPI.Utilities.ExtensionHelper.GetOrCreateExtension( "http://id.tincanapi.com/extension/datetime" ),
                RockDateTime.Now.ToString() );
        }

        public static Person GetPerson( string riseUserId )
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attribueValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );

            var attributeId = AttributeCache.Get( Constants.PERSON_ATTRIBUTE_RISEID ).Id;

            var attributeValue = attribueValueService.Queryable()
                .Where( av => av.AttributeId == attributeId && av.Value == riseUserId )
                .FirstOrDefault();

            if ( attributeValue != null )
            {
                var person = personService.Get( attributeValue.EntityId ?? 0 );
                if ( person != null )
                {
                    return person;
                }
            }
            return null;
        }
    }
}