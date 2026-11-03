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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using org.secc.Rest.Controllers;
using Rock.Data;
using Rock.Model;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9351: who may read a group through the GroupApp group, roster and attendance GETs. Each
    /// action runs on a controller built with a database context that never connects, whose current
    /// login, group lookup, member or leader check and Edit and Manage Members permission check
    /// are replaced; the work after the check is
    /// replaced too, or (GetGroup's summary) needs no database. Anything that reaches the database
    /// throws, which fails the test. GroupMemberServiceHelper.HasSecurityGrant itself reads Rock's
    /// authorization cache and is not covered here.
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
            public bool IsMemberOrLeader { get; set; }

            public bool HasGrant { get; set; }

            public readonly Group Group = new TestGroup { Id = GroupId, Name = "Sample Group", IsActive = true };

            public readonly UserLogin Login = new UserLogin { Person = new Person { Id = PersonId } };

            public int? CheckedGroupId { get; private set; }

            public int? CheckedPersonId { get; private set; }

            public Group GrantGroup { get; private set; }

            public Person GrantPerson { get; private set; }

            public Group Find( int groupId )
            {
                return groupId == GroupId ? Group : null;
            }

            public bool Check( int groupId, int personId )
            {
                CheckedGroupId = groupId;
                CheckedPersonId = personId;
                return IsMemberOrLeader;
            }

            public bool Grant( Group group, Person person )
            {
                GrantGroup = group;
                GrantPerson = person;
                return HasGrant;
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

            protected override bool IsActiveGroupMember( int groupId, int personId ) => _caller.Check( groupId, personId );

            protected override bool HasSecurityGrant( Group group, Person person ) => _caller.Grant( group, person );

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

            protected override bool IsActiveGroupMember( int groupId, int personId ) => _caller.Check( groupId, personId );

            protected override bool HasSecurityGrant( Group group, Person person ) => _caller.Grant( group, person );
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

            protected override bool IsActiveGroupLeader( int groupId, int personId ) => _caller.Check( groupId, personId );

            protected override bool HasSecurityGrant( Group group, Person person ) => _caller.Grant( group, person );

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
            Assert.Equal( GroupId, caller.CheckedGroupId );
            Assert.Equal( PersonId, caller.CheckedPersonId );
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
        public void GetGroupMembers_NotMemberWithGrant_LoadsMembers()
        {
            var caller = new Caller { HasGrant = true };
            var controller = new TestMembersController( caller );

            Assert.Equal( HttpStatusCode.OK, Run( () => controller.GetGroupMembers( GroupId ) ) );
            Assert.Equal( 1, controller.ScopeCalls );
            AssertGrantAskedAboutTheCaller( caller );
        }

        [Fact]
        public void GetGroupMembers_MemberWithoutGrant_LoadsMembers()
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
    }
}
