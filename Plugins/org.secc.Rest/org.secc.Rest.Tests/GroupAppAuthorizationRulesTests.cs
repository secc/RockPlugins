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
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using org.secc.Rest.Controllers;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Xunit;
using Authorization = Rock.Security.Authorization;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// Shares Rock's in-process authorization cache, so these tests run one at a time.
    /// </summary>
    [CollectionDefinition( Name, DisableParallelization = true )]
    public class RockAuthorizationCacheCollection
    {
        public const string Name = "Rock authorization cache";
    }

    /// <summary>
    /// ROCK-9351: the GroupApp group, roster and attendance GETs with Rock's own permission check.
    /// Unlike GroupAppAccessTests, the controllers keep their permission lookup; each test puts the
    /// caller's permission rules for the group into Rock's authorization cache, so the lookup reads
    /// them from there, and leaves an empty rule set there afterwards. The caller is not a member of the group, and
    /// the current login, group lookup, group member query and the work after the check are
    /// replaced. Anything that reaches the database throws, which fails the test.
    /// </summary>
    [Collection( RockAuthorizationCacheCollection.Name )]
    public class GroupAppAuthorizationRulesTests : IDisposable
    {
        private const int GroupId = 4242;
        private const int PersonId = 7171;
        private const int GroupEntityTypeId = 16;
        private const string AuthorizationCacheKey = "Rock.Security.Authorization";
        private const string UnusedConnection = "name=UnusedByTheseTests";

        private static readonly DateTime OccurrenceDate = new DateTime( 2026, 11, 1 );

        /// <summary>A group whose entity type id needs no cache lookup.</summary>
        private class TestGroup : Group
        {
            public override int TypeId => GroupEntityTypeId;
        }

        private static readonly Group TheGroup = new TestGroup { Id = GroupId, Name = "Sample Group", IsActive = true };

        private static readonly UserLogin TheLogin = new UserLogin { Person = new Person { Id = PersonId } };

        private static Group Find( int groupId ) => groupId == GroupId ? TheGroup : null;

        private class TestMembersController : GroupAppGroupMembersController
        {
            public int ScopeCalls { get; private set; }

            public TestMembersController() : base( new RockContext( UnusedConnection ) )
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => TheLogin;

            protected override Group FindGroup( int groupId ) => Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => false;

            protected override ScopedGroupMembers GetScopedGroupMembers( GroupMemberServiceHelper helper, Group group, Person person )
            {
                ScopeCalls++;
                return new ScopedGroupMembers { IsTableBased = true };
            }
        }

        private class TestGroupListController : GroupAppGroupListController
        {
            public TestGroupListController() : base( new RockContext( UnusedConnection ) )
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => TheLogin;

            protected override Group FindGroup( int groupId ) => Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => false;
        }

        private class TestAttendanceController : GroupAppAttendanceController
        {
            public int DataCalls { get; private set; }

            public TestAttendanceController() : base( new RockContext( UnusedConnection ) )
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => TheLogin;

            protected override Group FindGroup( int groupId ) => Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => false;

            protected override IHttpActionResult GroupAttendanceResult( Group group, DateTime occurrenceDate )
            {
                DataCalls++;
                return Ok();
            }

            protected override IHttpActionResult AttendanceSchedulesResult( Group group )
            {
                DataCalls++;
                return Ok();
            }
        }

        /// <summary>
        /// Puts one rule per action for the caller on the group into Rock's authorization cache:
        /// allowed for the actions listed, denied for the rest of View, Edit and Manage Members.
        /// A rule for the person is the first match, so Rock reads nothing else.
        /// </summary>
        private static void SetRules( params string[] allowed )
        {
            var allowedSet = new HashSet<string>( allowed );
            var actions = new Dictionary<string, List<AuthRule>>();
            foreach ( var action in new[] { Authorization.VIEW, Authorization.EDIT, Authorization.MANAGE_MEMBERS } )
            {
                actions[action] = new List<AuthRule>
                {
                    new AuthRule { EntityId = GroupId, PersonId = PersonId, AllowOrDeny = allowedSet.Contains( action ) ? 'A' : 'D' }
                };
            }

            var authorizations = new Dictionary<int, Dictionary<int, Dictionary<string, List<AuthRule>>>>
            {
                [GroupEntityTypeId] = new Dictionary<int, Dictionary<string, List<AuthRule>>> { [GroupId] = actions }
            };

            RockCache.AddOrUpdate( AuthorizationCacheKey, authorizations );
        }

        // Removing the entry would tell Rock's message bus, which these tests don't start, and the
        // next lookup would then load the rules from the database. An empty rule set holds no rules.
        public void Dispose()
        {
            RockCache.AddOrUpdate( AuthorizationCacheKey, new Dictionary<int, Dictionary<int, Dictionary<string, List<AuthRule>>>>() );
        }

        /// <summary>Runs the action, failing the test if it throws, and returns the response status.</summary>
        private static HttpStatusCode Run( Func<IHttpActionResult> action )
        {
            IHttpActionResult result = null;
            var exception = Record.Exception( () => result = action() );
            Assert.Null( exception );
            return result.ExecuteAsync( CancellationToken.None ).GetAwaiter().GetResult().StatusCode;
        }

        public static IEnumerable<object[]> WithoutEditAndManageMembers()
        {
            yield return new object[] { new[] { Authorization.VIEW } };
            yield return new object[] { new[] { Authorization.VIEW, Authorization.EDIT } };
            yield return new object[] { new[] { Authorization.VIEW, Authorization.MANAGE_MEMBERS } };
        }

        // GetGroupMembers

        [Theory]
        [MemberData( nameof( WithoutEditAndManageMembers ) )]
        public void GetGroupMembers_WithoutEditAndManageMembers_Returns403( string[] allowed )
        {
            SetRules( allowed );
            var controller = new TestMembersController();

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 0, controller.ScopeCalls );
        }

        [Fact]
        public void GetGroupMembers_EditAndManageMembersWithViewDenied_PassesCheck()
        {
            SetRules( Authorization.EDIT, Authorization.MANAGE_MEMBERS );
            var controller = new TestMembersController();

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 1, controller.ScopeCalls );
        }

        // GetGroup

        [Theory]
        [MemberData( nameof( WithoutEditAndManageMembers ) )]
        public void GetGroup_WithoutEditAndManageMembers_Returns403( string[] allowed )
        {
            SetRules( allowed );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => new TestGroupListController().GetGroup( GroupId ) ) );
        }

        [Fact]
        public void GetGroup_EditAndManageMembersWithViewDenied_ReturnsSummary()
        {
            SetRules( Authorization.EDIT, Authorization.MANAGE_MEMBERS );

            Assert.Equal( HttpStatusCode.OK, Run( () => new TestGroupListController().GetGroup( GroupId ) ) );
        }

        // Attendance/{groupId}/{occurrenceDate} and AttendanceSchedules/{groupId}

        [Theory]
        [MemberData( nameof( WithoutEditAndManageMembers ) )]
        public void GetGroupAttendance_WithoutEditAndManageMembers_Returns403( string[] allowed )
        {
            SetRules( allowed );
            var controller = new TestAttendanceController();

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 0, controller.DataCalls );
        }

        [Fact]
        public void GetGroupAttendance_EditAndManageMembersWithViewDenied_LoadsAttendance()
        {
            SetRules( Authorization.EDIT, Authorization.MANAGE_MEMBERS );
            var controller = new TestAttendanceController();

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 1, controller.DataCalls );
        }

        [Theory]
        [MemberData( nameof( WithoutEditAndManageMembers ) )]
        public void GetAttendanceSchedules_WithoutEditAndManageMembers_Returns403( string[] allowed )
        {
            SetRules( allowed );
            var controller = new TestAttendanceController();

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 0, controller.DataCalls );
        }

        [Fact]
        public void GetAttendanceSchedules_EditAndManageMembersWithViewDenied_LoadsSchedules()
        {
            SetRules( Authorization.EDIT, Authorization.MANAGE_MEMBERS );
            var controller = new TestAttendanceController();

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 1, controller.DataCalls );
        }
    }
}
