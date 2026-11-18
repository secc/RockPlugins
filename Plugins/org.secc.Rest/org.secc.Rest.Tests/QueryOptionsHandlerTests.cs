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
using System.Collections.Generic;
using System.Net.Http;
using org.secc.Rest.Handlers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9331: which decoded query pairs, and which GroupMembers requests, QueryOptionsHandler answers with 400.
    /// </summary>
    public class QueryOptionsHandlerTests
    {
        private static bool Check( params string[] keysAndValues )
        {
            var pairs = new List<KeyValuePair<string, string>>();
            for ( var i = 0; i < keysAndValues.Length; i += 2 )
            {
                pairs.Add( new KeyValuePair<string, string>( keysAndValues[i], keysAndValues[i + 1] ) );
            }

            return QueryOptionsHandler.ReferencesUsers( pairs );
        }

        [Theory]
        [InlineData( "$expand", "Users" )]
        [InlineData( "$expand", "users" )]
        [InlineData( "$expand", "USERS" )]
        [InlineData( "$expand", "Aliases,Users" )]
        [InlineData( "$expand", "Users,Aliases" )]
        [InlineData( "$expand", "Person/Users" )]
        [InlineData( "$expand", "Rock.Model.Person/Users" )]
        [InlineData( "$expand", " Users" )]
        [InlineData( "$expand", "\tUsers" )]
        [InlineData( "$expand", "Users " )]
        [InlineData( "$expand", "Users/Person" )]
        [InlineData( "$expand", "Users($select=Id)" )]
        [InlineData( "$expand", "5Users" )]
        [InlineData( "$expand", "Users_" )]
        [InlineData( "$expand", "$Users" )]
        [InlineData( "$expand", "\u200BUsers" )]
        [InlineData( "$expand", "\u00E9Users" )]
        [InlineData( "$expand", "Users\u00E9" )]
        [InlineData( "$select", "Users" )]
        [InlineData( "$select", "Id,Users/Id" )]
        [InlineData( "$orderby", "Users/Count" )]
        [InlineData( "$orderby", "'Users'" )]
        [InlineData( "$filter", "Users/any()" )]
        [InlineData( "$filter", "Users/any(u:u/UserName eq 'a')" )]
        [InlineData( "$filter", "LastName eq 'a' and Users/any()" )]
        [InlineData( "$filter", "LastName eq 'O''Brien' and Users/any()" )]
        [InlineData( "$filter", "LastName eq \"Users\"" )]
        [InlineData( "$filter", "isof(Users,'a')" )]
        [InlineData( "$Expand", "Users" )]
        [InlineData( "$EXPAND", "Users" )]
        [InlineData( "$FILTER", "Users/any()" )]
        public void OptionReferringToUsers_IsRejected( string key, string value )
        {
            Assert.True( Check( key, value ) );
        }

        [Theory]
        [InlineData( "$expand", "PhoneNumbers" )]
        [InlineData( "$expand", "Members/Person" )]
        [InlineData( "$expand", "UsersGroup" )]
        [InlineData( "$expand", "AUsers" )]
        [InlineData( "$expand", "ZUsers" )]
        [InlineData( "$expand", "aUsers" )]
        [InlineData( "$expand", "zUsers" )]
        [InlineData( "$select", "Id,UserName" )]
        [InlineData( "$orderby", "LastName desc" )]
        [InlineData( "$filter", "LastName eq 'Users'" )]
        [InlineData( "$FILTER", "LastName eq 'Users'" )]
        [InlineData( "$filter", "'Users' eq LastName" )]
        [InlineData( "$filter", "Name eq 'O''Users'" )]
        [InlineData( "$filter", "Name eq 'a''b''Users'" )]
        [InlineData( "$filter", "LastName eq 'Users" )]
        [InlineData( "$filter", "" )]
        [InlineData( "$top", "Users" )]
        [InlineData( "$skip", "Users" )]
        [InlineData( "$inlinecount", "Users" )]
        [InlineData( "expand", "Users" )]
        [InlineData( "name", "Users" )]
        [InlineData( "Users", "1" )]
        [InlineData( "", "Users" )]
        public void OtherOptions_AreAccepted( string key, string value )
        {
            Assert.False( Check( key, value ) );
        }

        [Fact]
        public void NoPairs_AreAccepted()
        {
            Assert.False( Check() );
        }

        [Fact]
        public void NullKeyOrValue_IsAccepted()
        {
            Assert.False( Check( null, "Users" ) );
            Assert.False( Check( "$expand", null ) );
        }

        [Fact]
        public void EveryPair_IsCheckedOnItsOwn()
        {
            // An unclosed quote in one $filter value does not carry into the next $filter value.
            Assert.True( Check( "$filter", "'", "$filter", "Users/any()" ) );
            Assert.True( Check( "$expand", "Users", "$expand", "Aliases" ) );
            Assert.True( Check( "$expand", "Aliases", "$expand", "Users" ) );
            Assert.True( Check( "$top", "1", "$select", "Id", "$orderby", "Users/Count" ) );
            Assert.False( Check( "$filter", "LastName eq 'Users'", "$expand", "PhoneNumbers", "name", "Users" ) );
        }

        [Fact]
        public void StringLiterals_AreSkippedOnlyInFilter()
        {
            Assert.False( QueryOptionsHandler.ContainsUsersWord( "LastName eq 'Users'", true ) );
            Assert.True( QueryOptionsHandler.ContainsUsersWord( "LastName eq 'Users'", false ) );
            Assert.True( Check( "$select", "'Users'" ) );
            Assert.True( Check( "$expand", "'Users'" ) );
        }

        private static bool CheckGroupMembers( string controllerName, string query )
        {
            using ( var request = new HttpRequestMessage( HttpMethod.Get, "http://localhost/api/x" + query ) )
            {
                return QueryOptionsHandler.IsGroupMembersIncludeDeceased( controllerName, request );
            }
        }

        [Theory]
        [InlineData( "GroupMembers", "?IncludeDeceased=true" )]
        [InlineData( "groupmembers", "?IncludeDeceased=true" )]
        [InlineData( "GROUPMEMBERS", "?IncludeDeceased=true" )]
        [InlineData( "GroupMembers", "?includedeceased=TRUE" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=false" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=0" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=yes" )]
        [InlineData( "GroupMembers", "?$top=50&IncludeDeceased=true" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=false&IncludeDeceased=true" )]
        [InlineData( "GroupMembers", "?Include%44eceased=1" )]
        [InlineData( "GroupMembers", "?%49ncludeDeceased=1" )]
        [InlineData( "GroupMembers", "?IncludeDeceased=true&a[b=1" )]
        public void GroupMembersWithIncludeDeceased_IsRejected( string controllerName, string query )
        {
            Assert.True( CheckGroupMembers( controllerName, query ) );
        }

        [Theory]
        [InlineData( "People", "?includeDeceased=true" )]
        [InlineData( "Groups", "?IncludeDeceased=true" )]
        [InlineData( "GroupMemberHistoricals", "?IncludeDeceased=true" )]
        [InlineData( null, "?IncludeDeceased=true" )]
        [InlineData( "GroupMemberſ", "?IncludeDeceased=true" )]
        [InlineData( "GroupMem\u00ADbers", "?IncludeDeceased=true" )]
        [InlineData( "GroupAppGroupMembers", "?IncludeDeceased=true" )]
        [InlineData( "GroupMembersX", "?IncludeDeceased=true" )]
        [InlineData( "GroupMembers", "" )]
        [InlineData( "GroupMembers", "?$top=50" )]
        [InlineData( "GroupMembers", "?IncludeDeceased" )]
        [InlineData( "GroupMembers", "?IncludeDeceasedX=1" )]
        [InlineData( "GroupMembers", "?xIncludeDeceased=1" )]
        [InlineData( "GroupMembers", "?Include+Deceased=1" )]
        [InlineData( "GroupMembers", "?IncludeDeceased[]=1" )]
        [InlineData( "GroupMembers", "?a=1?IncludeDeceased=1" )]
        public void OtherControllerOrQuery_IsAccepted( string controllerName, string query )
        {
            Assert.False( CheckGroupMembers( controllerName, query ) );
        }

        [Fact]
        public void GroupMembersQueryThatCannotBeRead_IsAccepted()
        {
            // Reading the query of a relative URI throws; the GroupMembers controller reads it the same way.
            using ( var request = new HttpRequestMessage( HttpMethod.Get, new System.Uri( "api/GroupMembers?IncludeDeceased=true", System.UriKind.Relative ) ) )
            {
                Assert.ThrowsAny<System.InvalidOperationException>( () => request.RequestUri.Query );
                Assert.False( QueryOptionsHandler.IsGroupMembersIncludeDeceased( "GroupMembers", request ) );
            }
        }
    }
}
