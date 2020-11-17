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
using org.secc.Rise.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Rise.Response
{
    [Url( "groups" )]
    public class RiseGroup : RiseBase
    {
        [JsonProperty( "name" )]
        public string Name { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        [JsonIgnore]
        public List<RiseUser> Users
        {
            get
            {
                return ClientManager.GetSet<RiseUser>( this ).ToList();
            }
        }


        public void SyncGroupMembers()
        {
            SyncGroupMembers( GetRockGroup() );
        }

        public void SyncGroupMembers( Group group )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            var people = groupMemberService.Queryable()
                .Where( gm => gm.GroupId == group.Id )
                .Select( gm => gm.Person )
                .ToList();

            //This is slow. We can optimize later.
            foreach ( var person in people )
            {
                person.LoadAttributes();
            }

            people = people
                .Where( p => p.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ).IsNotNullOrWhiteSpace() )
                .ToList();

            var riseUsers = Users; //caching the request

            var usersToRemove = riseUsers
                .Where( u => !people.Select( p => p.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ) ).Contains( u.Id ) )
                .ToList();

            foreach ( var user in usersToRemove )
            {
                ClientManager.Delete<RiseUser>( this, user.Id );
            }

            var peopleToAdd = people
                .Where( p => !riseUsers.Select( u => u.Id ).Contains( p.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ) ) )
                .ToList();

            foreach ( var person in peopleToAdd )
            {
                ClientManager.Put<RiseUser>( this, person.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID ) );
            }
        }

        private Group GetRockGroup()
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attribueValueService = new AttributeValueService( rockContext );

            var attributeId = AttributeCache.Get( Constants.GROUP_ATTRIBUTE_KEY_RISEID ).Id;

            var attributeValue = attribueValueService.Queryable()
                .Where( av => av.AttributeId == attributeId && av.Value == this.Id )
                .FirstOrDefault();

            if ( attributeValue == null )
            {
                return null;
            }

            GroupService groupService = new GroupService( rockContext );
            return groupService.Get( attributeValue.EntityId ?? 0 );
        }
    }
}
