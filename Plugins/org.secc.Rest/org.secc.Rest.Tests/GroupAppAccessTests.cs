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
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using org.secc.Rest.Controllers;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Xunit;
using Authorization = Rock.Security.Authorization;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9351: who may read a group through the GroupApp group, roster and attendance GETs. Each
    /// action runs on a controller built with a database context that never connects, whose current
    /// login, group lookup and per-action permission lookup are replaced, and whose group member
    /// query runs the controller's own condition over in-memory rows; the work after the check is
    /// replaced too, or (GetGroup's summary) needs no database. Anything that reaches the database
    /// or Rock's caches throws, which fails the test.
    /// </summary>
    public class GroupAppAccessTests
    {
        private const int GroupId = 4242;
        private const int PersonId = 7171;
        private const string UnusedConnection = "name=UnusedByTheseTests";

        /// <summary>A group whose entity type id needs no cache lookup.</summary>
        private class TestGroup : Group
        {
            public override int TypeId => 16;
        }

        /// <summary>The caller's standing with the group, and what the replaced checks were asked.</summary>
        private class Caller
        {
            /// <summary>The group member rows the controller's member or leader query runs over.</summary>
            public List<GroupMember> Rows { get; } = new List<GroupMember>();

            /// <summary>An active leader of the group, which counts as an active member too.</summary>
            public bool IsMemberOrLeader
            {
                set
                {
                    if ( value )
                    {
                        Rows.Add( Row() );
                    }
                }
            }

            /// <summary>The actions the caller is authorized for on the group.</summary>
            public HashSet<string> Allowed { get; set; } = new HashSet<string>();

            /// <summary>Edit and Manage Members on the group.</summary>
            public bool HasGrant
            {
                set { Allowed = value ? new HashSet<string> { Authorization.EDIT, Authorization.MANAGE_MEMBERS } : new HashSet<string>(); }
            }

            public readonly Group Group = new TestGroup { Id = GroupId, Name = "Sample Group", IsActive = true };

            public readonly UserLogin Login = new UserLogin { Person = new Person { Id = PersonId } };

            public int MemberChecks { get; private set; }

            public ISecured GrantGroup { get; private set; }

            public Person GrantPerson { get; private set; }

            public Group Find( int groupId )
            {
                return groupId == GroupId ? Group : null;
            }

            public bool AnyRow( Expression<Func<GroupMember, bool>> predicate )
            {
                MemberChecks++;
                return Rows.AsQueryable().Any( predicate );
            }

            public bool Authorized( ISecured entity, string action, Person person )
            {
                GrantGroup = entity;
                GrantPerson = person;
                return Allowed.Contains( action );
            }
        }

        private class TestMembersController : GroupAppGroupMembersController
        {
            private readonly Caller _caller;

            public int ScopeCalls { get; private set; }

            public TestMembersController( Caller caller ) : base( new RockContext( UnusedConnection ) )
            {
                _caller = caller;
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => _caller.Login;

            protected override Group FindGroup( int groupId ) => _caller.Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => _caller.AnyRow( predicate );

            protected override bool Authorized( ISecured entity, string action, Person person ) => _caller.Authorized( entity, action, person );

            // A table-based scope with no table: the action answers NotAssignedToTable without
            // building the roster, which needs the database.
            protected override ScopedGroupMembers GetScopedGroupMembers( GroupMemberServiceHelper helper, Group group, Person person )
            {
                ScopeCalls++;
                Assert.Same( _caller.Group, group );
                Assert.Same( _caller.Login.Person, person );
                return new ScopedGroupMembers { IsTableBased = true };
            }
        }

        private class TestGroupListController : GroupAppGroupListController
        {
            private readonly Caller _caller;

            public TestGroupListController( Caller caller ) : base( new RockContext( UnusedConnection ) )
            {
                _caller = caller;
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => _caller.Login;

            protected override Group FindGroup( int groupId ) => _caller.Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => _caller.AnyRow( predicate );

            protected override bool Authorized( ISecured entity, string action, Person person ) => _caller.Authorized( entity, action, person );
        }

        private class TestAttendanceController : GroupAppAttendanceController
        {
            private readonly Caller _caller;

            public int AttendanceCalls { get; private set; }

            public int ScheduleCalls { get; private set; }

            public DateTime? AttendanceDate { get; private set; }

            public TestAttendanceController( Caller caller ) : base( new RockContext( UnusedConnection ) )
            {
                _caller = caller;
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override UserLogin GetCurrentUserLogin() => _caller.Login;

            protected override Group FindGroup( int groupId ) => _caller.Find( groupId );

            protected override bool AnyGroupMember( Expression<Func<GroupMember, bool>> predicate ) => _caller.AnyRow( predicate );

            protected override bool Authorized( ISecured entity, string action, Person person ) => _caller.Authorized( entity, action, person );

            protected override IHttpActionResult GroupAttendanceResult( Group group, DateTime occurrenceDate )
            {
                AttendanceCalls++;
                Assert.Same( _caller.Group, group );
                AttendanceDate = occurrenceDate;
                return Ok();
            }

            protected override IHttpActionResult AttendanceSchedulesResult( Group group )
            {
                ScheduleCalls++;
                Assert.Same( _caller.Group, group );
                return Ok();
            }
        }

        private static readonly DateTime OccurrenceDate = new DateTime( 2026, 11, 1 );

        /// <summary>A group member row; by default the caller as an active leader of the group.</summary>
        private static GroupMember Row(
            int groupId = GroupId,
            int personId = PersonId,
            GroupMemberStatus status = GroupMemberStatus.Active,
            bool isLeader = true )
        {
            return new GroupMember
            {
                GroupId = groupId,
                PersonId = personId,
                GroupMemberStatus = status,
                GroupRole = new GroupTypeRole { IsLeader = isLeader }
            };
        }

        /// <summary>Runs the action, failing the test if it throws, and returns the response status.</summary>
        private static HttpStatusCode Run( Func<IHttpActionResult> action )
        {
            IHttpActionResult result = null;
            var exception = Record.Exception( () => result = action() );
            Assert.Null( exception );
            return result.ExecuteAsync( CancellationToken.None ).GetAwaiter().GetResult().StatusCode;
        }

        private static void AssertChecksAskedAboutTheCaller( Caller caller )
        {
            Assert.Equal( 1, caller.MemberChecks );
        }

        private static void AssertGrantAskedAboutTheCaller( Caller caller )
        {
            Assert.Same( caller.Group, caller.GrantGroup );
            Assert.Same( caller.Login.Person, caller.GrantPerson );
        }

        // GetGroupMembers

        [Fact]
        public void GetGroupMembers_NotMemberWithoutGrant_Returns403WithoutLoadingMembers()
        {
            var caller = new Caller();
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 0, controller.ScopeCalls );
            AssertChecksAskedAboutTheCaller( caller );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupMembers_NotMemberWithGrant_PassesCheck()
        {
            var caller = new Caller { HasGrant = true };
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 1, controller.ScopeCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupMembers_MemberWithoutGrant_PassesCheck()
        {
            var caller = new Caller { IsMemberOrLeader = true };
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 1, controller.ScopeCalls );
            AssertChecksAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupMembers_UnknownGroup_Returns404()
        {
            var controller = new TestMembersController( new Caller { IsMemberOrLeader = true, HasGrant = true } );

            Assert.Equal( HttpStatusCode.NotFound, Run( () => controller.GetGroupMembers( GroupId + 1 ) ) );
            Assert.Equal( 0, controller.ScopeCalls );
        }

        // GetGroup

        [Fact]
        public void GetGroup_NotMemberWithoutGrant_Returns403()
        {
            var caller = new Caller();
            var controller = new TestGroupListController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroup( GroupId ) ) );
            AssertChecksAskedAboutTheCaller( caller );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroup_NotMemberWithGrant_ReturnsSummary()
        {
            var caller = new Caller { HasGrant = true };
            var controller = new TestGroupListController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroup( GroupId ) ) );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroup_MemberWithoutGrant_ReturnsSummary()
        {
            var caller = new Caller { IsMemberOrLeader = true };
            var controller = new TestGroupListController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroup( GroupId ) ) );
            AssertChecksAskedAboutTheCaller( caller );
        }

        // Attendance/{groupId}/{occurrenceDate}

        [Fact]
        public void GetGroupAttendance_NotLeaderWithoutGrant_Returns403WithoutLoadingAttendance()
        {
            var caller = new Caller();
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 0, controller.AttendanceCalls );
            AssertChecksAskedAboutTheCaller( caller );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupAttendance_NotLeaderWithGrant_LoadsAttendance()
        {
            var caller = new Caller { HasGrant = true };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 1, controller.AttendanceCalls );
            Assert.Equal( OccurrenceDate, controller.AttendanceDate );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupAttendance_LeaderWithoutGrant_LoadsAttendance()
        {
            var caller = new Caller { IsMemberOrLeader = true };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 1, controller.AttendanceCalls );
            AssertChecksAskedAboutTheCaller( caller );
        }

        // AttendanceSchedules/{groupId}

        [Fact]
        public void GetAttendanceSchedules_NotLeaderWithoutGrant_Returns403WithoutLoadingSchedules()
        {
            var caller = new Caller();
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 0, controller.ScheduleCalls );
            AssertChecksAskedAboutTheCaller( caller );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetAttendanceSchedules_NotLeaderWithGrant_LoadsSchedules()
        {
            var caller = new Caller { HasGrant = true };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 1, controller.ScheduleCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetAttendanceSchedules_LeaderWithoutGrant_LoadsSchedules()
        {
            var caller = new Caller { IsMemberOrLeader = true };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 1, controller.ScheduleCalls );
            AssertChecksAskedAboutTheCaller( caller );
        }

        // Only Edit and Manage Members together count. View alone, or either of the two alone, does not.

        public static IEnumerable<object[]> PartialPermissions()
        {
            yield return new object[] { new[] { Authorization.VIEW } };
            yield return new object[] { new[] { Authorization.EDIT } };
            yield return new object[] { new[] { Authorization.MANAGE_MEMBERS } };
            yield return new object[] { new[] { Authorization.VIEW, Authorization.EDIT } };
            yield return new object[] { new[] { Authorization.VIEW, Authorization.MANAGE_MEMBERS } };
        }

        [Theory]
        [MemberData( nameof( PartialPermissions ) )]
        public void GetGroupMembers_NotMemberWithPartialPermission_Returns403( string[] allowed )
        {
            var caller = new Caller { Allowed = new HashSet<string>( allowed ) };
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 0, controller.ScopeCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Theory]
        [MemberData( nameof( PartialPermissions ) )]
        public void GetGroup_NotMemberWithPartialPermission_Returns403( string[] allowed )
        {
            var caller = new Caller { Allowed = new HashSet<string>( allowed ) };
            var controller = new TestGroupListController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroup( GroupId ) ) );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Theory]
        [MemberData( nameof( PartialPermissions ) )]
        public void GetGroupAttendance_NotLeaderWithPartialPermission_Returns403( string[] allowed )
        {
            var caller = new Caller { Allowed = new HashSet<string>( allowed ) };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 0, controller.AttendanceCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Theory]
        [MemberData( nameof( PartialPermissions ) )]
        public void GetAttendanceSchedules_NotLeaderWithPartialPermission_Returns403( string[] allowed )
        {
            var caller = new Caller { Allowed = new HashSet<string>( allowed ) };
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 0, controller.ScheduleCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        // Which group member rows count. The summary and roster take any active member; attendance
        // takes only an active leader. A row with the group and person ids swapped, or one that is
        // not active, counts for neither.

        [Fact]
        public void GetGroupMembers_ActiveNonLeaderMember_PassesCheck()
        {
            var caller = new Caller();
            caller.Rows.Add( Row( isLeader: false ) );
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 1, controller.ScopeCalls );
        }

        [Fact]
        public void GetGroup_ActiveNonLeaderMember_ReturnsSummary()
        {
            var caller = new Caller();
            caller.Rows.Add( Row( isLeader: false ) );
            var controller = new TestGroupListController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroup( GroupId ) ) );
        }

        [Fact]
        public void GetGroupAttendance_ActiveNonLeaderMember_Returns403()
        {
            var caller = new Caller();
            caller.Rows.Add( Row( isLeader: false ) );
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) );
            Assert.Equal( 0, controller.AttendanceCalls );
        }

        [Fact]
        public void GetAttendanceSchedules_ActiveNonLeaderMember_Returns403()
        {
            var caller = new Caller();
            caller.Rows.Add( Row( isLeader: false ) );
            var controller = new TestAttendanceController( caller );

            Assert.Equal( HttpStatusCode.Forbidden, Run( () => controller.GetAttendanceSchedules( GroupId ) ) );
            Assert.Equal( 0, controller.ScheduleCalls );
        }

        public static IEnumerable<object[]> RowsThatDoNotCount()
        {
            yield return new object[] { "inactive leader", GroupId, PersonId, GroupMemberStatus.Inactive };
            yield return new object[] { "pending leader", GroupId, PersonId, GroupMemberStatus.Pending };
            yield return new object[] { "ids swapped", PersonId, GroupId, GroupMemberStatus.Active };
            yield return new object[] { "another group", GroupId + 1, PersonId, GroupMemberStatus.Active };
            yield return new object[] { "another person", GroupId, PersonId + 1, GroupMemberStatus.Active };
        }

        private static Caller CallerWithRow( int groupId, int personId, GroupMemberStatus status )
        {
            var caller = new Caller();
            caller.Rows.Add( Row( groupId, personId, status ) );
            return caller;
        }

        [Theory]
        [MemberData( nameof( RowsThatDoNotCount ) )]
        public void GetGroupMembers_RowThatDoesNotCount_Returns403( string row, int groupId, int personId, GroupMemberStatus status )
        {
            var controller = new TestMembersController( CallerWithRow( groupId, personId, status ) );

            Assert.True( Run( () => controller.GetGroupMembers( GroupId ) ) == HttpStatusCode.Forbidden, row );
            Assert.Equal( 0, controller.ScopeCalls );
        }

        [Theory]
        [MemberData( nameof( RowsThatDoNotCount ) )]
        public void GetGroup_RowThatDoesNotCount_Returns403( string row, int groupId, int personId, GroupMemberStatus status )
        {
            var controller = new TestGroupListController( CallerWithRow( groupId, personId, status ) );

            Assert.True( Run( () => controller.GetGroup( GroupId ) ) == HttpStatusCode.Forbidden, row );
        }

        [Theory]
        [MemberData( nameof( RowsThatDoNotCount ) )]
        public void GetGroupAttendance_RowThatDoesNotCount_Returns403( string row, int groupId, int personId, GroupMemberStatus status )
        {
            var controller = new TestAttendanceController( CallerWithRow( groupId, personId, status ) );

            Assert.True( Run( () => controller.GetGroupAttendance( GroupId, OccurrenceDate ) ) == HttpStatusCode.Forbidden, row );
            Assert.Equal( 0, controller.AttendanceCalls );
        }

        [Theory]
        [MemberData( nameof( RowsThatDoNotCount ) )]
        public void GetAttendanceSchedules_RowThatDoesNotCount_Returns403( string row, int groupId, int personId, GroupMemberStatus status )
        {
            var controller = new TestAttendanceController( CallerWithRow( groupId, personId, status ) );

            Assert.True( Run( () => controller.GetAttendanceSchedules( GroupId ) ) == HttpStatusCode.Forbidden, row );
            Assert.Equal( 0, controller.ScheduleCalls );
        }
    }
}
