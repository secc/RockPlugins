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
using System.Linq;
using org.secc.Rest.Controllers;
using Rock.Model;
using Rock.Security;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9351: the rules behind the GroupApp group, roster and attendance checks: Edit and Manage
    /// Members permission, and which group member rows count as an active member or an active leader.
    /// </summary>
    public class GroupMemberRulesTests
    {
        private const int GroupId = 4242;
        private const int PersonId = 7171;

        // HasSecurityGrant

        private static bool Grant( params string[] allowed )
        {
            return GroupMemberServiceHelper.HasSecurityGrant( action => allowed.Contains( action ) );
        }

        [Fact]
        public void HasSecurityGrant_EditAndManageMembers_True()
        {
            Assert.True( Grant( Authorization.EDIT, Authorization.MANAGE_MEMBERS ) );
        }

        [Fact]
        public void HasSecurityGrant_EditOnly_False()
        {
            Assert.False( Grant( Authorization.EDIT ) );
        }

        [Fact]
        public void HasSecurityGrant_ManageMembersOnly_False()
        {
            Assert.False( Grant( Authorization.MANAGE_MEMBERS ) );
        }

        [Fact]
        public void HasSecurityGrant_ViewOnly_False()
        {
            Assert.False( Grant( Authorization.VIEW ) );
        }

        [Fact]
        public void HasSecurityGrant_Neither_False()
        {
            Assert.False( Grant() );
        }

        // ActiveMemberOf and ActiveLeaderOf

        private static GroupMember Row(
            int groupId = GroupId,
            int personId = PersonId,
            GroupMemberStatus status = GroupMemberStatus.Active,
            bool isArchived = false,
            bool isLeader = false )
        {
            return new GroupMember
            {
                GroupId = groupId,
                PersonId = personId,
                GroupMemberStatus = status,
                IsArchived = isArchived,
                GroupRole = new GroupTypeRole { IsLeader = isLeader }
            };
        }

        private static bool IsMember( GroupMember row )
        {
            return new[] { row }.Any( GroupMemberServiceHelper.ActiveMemberOf( GroupId, PersonId ).Compile() );
        }

        private static bool IsLeader( GroupMember row )
        {
            return new[] { row }.Any( GroupMemberServiceHelper.ActiveLeaderOf( GroupId, PersonId ).Compile() );
        }

        [Fact]
        public void ActiveMemberOf_ActiveMember_Matches()
        {
            Assert.True( IsMember( Row() ) );
        }

        [Fact]
        public void ActiveMemberOf_ActiveLeader_Matches()
        {
            Assert.True( IsMember( Row( isLeader: true ) ) );
        }

        [Theory]
        [InlineData( GroupMemberStatus.Inactive )]
        [InlineData( GroupMemberStatus.Pending )]
        public void ActiveMemberOf_NotActive_DoesNotMatch( GroupMemberStatus status )
        {
            Assert.False( IsMember( Row( status: status ) ) );
        }

        [Fact]
        public void ActiveMemberOf_Archived_DoesNotMatch()
        {
            Assert.False( IsMember( Row( isArchived: true ) ) );
        }

        [Fact]
        public void ActiveMemberOf_OtherPerson_DoesNotMatch()
        {
            Assert.False( IsMember( Row( personId: PersonId + 1 ) ) );
        }

        [Fact]
        public void ActiveMemberOf_OtherGroup_DoesNotMatch()
        {
            Assert.False( IsMember( Row( groupId: GroupId + 1 ) ) );
        }

        [Fact]
        public void ActiveLeaderOf_ActiveLeader_Matches()
        {
            Assert.True( IsLeader( Row( isLeader: true ) ) );
        }

        [Fact]
        public void ActiveLeaderOf_ActiveNonLeader_DoesNotMatch()
        {
            Assert.False( IsLeader( Row() ) );
        }

        [Theory]
        [InlineData( GroupMemberStatus.Inactive )]
        [InlineData( GroupMemberStatus.Pending )]
        public void ActiveLeaderOf_NotActiveLeader_DoesNotMatch( GroupMemberStatus status )
        {
            Assert.False( IsLeader( Row( status: status, isLeader: true ) ) );
        }

        [Fact]
        public void ActiveLeaderOf_OtherPerson_DoesNotMatch()
        {
            Assert.False( IsLeader( Row( personId: PersonId + 1, isLeader: true ) ) );
        }

        [Fact]
        public void ActiveLeaderOf_OtherGroup_DoesNotMatch()
        {
            Assert.False( IsLeader( Row( groupId: GroupId + 1, isLeader: true ) ) );
        }
    }
}
