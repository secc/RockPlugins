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
using System.Reflection;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using Xunit;

namespace org.secc.Connection.Tests
{
    /// <summary>
    /// The group, role and member status the Volunteer Signup Form - Connections block puts on a new
    /// connection request, for each way a signup link names them (URL GroupId and GroupTypeRoleId, or a
    /// RoleRequests JSON list), and whether an opportunity takes signups.
    /// </summary>
    public class SignupPlacementTests
    {
        private const int SecurityRoleGroupTypeId = 1;
        private const int ServingTeamTypeId = 50;
        private const int OtherTypeId = 51;
        private const int MemberRoleId = 100;
        private const int LeaderRoleId = 101;

        private const int ServingGroupId = 10;
        private const int OtherServingGroupId = 11;
        private const int InactiveGroupId = 12;
        private const int ArchivedGroupId = 13;
        private const int SecurityRoleGroupId = 2;
        private const int SecurityTypeGroupId = 14;
        private const int OtherTypeGroupId = 15;
        private const int UnlistedServingGroupId = 16;
        private const int MissingGroupId = 99;

        private static readonly Guid MemberRoleGuid = new Guid( "6e1b0b3a-2f4d-4a51-9c3e-0d7a1f2b3c40" );
        private static readonly Guid LeaderRoleGuid = new Guid( "6e1b0b3a-2f4d-4a51-9c3e-0d7a1f2b3c41" );

        private const string NotTakingSignupsMessage = "This opportunity isn't taking signups right now.";
        private const string DoesNotExistMessage = "The requested opportunity does not exist.";

        static SignupPlacementTests()
        {
            // The block reads the phone types when it is created, and the security role check reads the
            // security role group type. Rock's cache is in memory, so these entries stand in for the database.
            DefinedValueCache.Get( Cacheable( new DefinedValue { Id = 12, Guid = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid(), Value = "Mobile" } ) );
            DefinedValueCache.Get( Cacheable( new DefinedValue { Id = 13, Guid = Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid(), Value = "Home" } ) );
            GroupTypeCache.Get( Cacheable( new GroupType { Id = SecurityRoleGroupTypeId, Guid = Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid(), Name = "Security Role" } ) );
        }

        /// <summary>
        /// Gives an entity what Rock's cache would otherwise read from the database when caching it: its
        /// entity type Id and its (empty) attributes.
        /// </summary>
        private static T Cacheable<T>( T entity ) where T : Rock.Data.Entity<T>, new()
        {
            typeof( Rock.Data.Entity<T> )
                .GetField( "_typeId", BindingFlags.Instance | BindingFlags.NonPublic )
                .SetValue( entity, ( int? ) 1000 );
            var hasAttributes = ( Rock.Attribute.IHasAttributes ) entity;
            hasAttributes.Attributes = new Dictionary<string, AttributeCache>();
            hasAttributes.AttributeValues = new Dictionary<string, AttributeValueCache>();
            return entity;
        }

        private class TestBlock : VolunteerSignupFormConnections
        {
            public Dictionary<int, Group> Groups { get; } = new Dictionary<int, Group>();

            public List<int> LoadedGroupIds { get; } = new List<int>();

            public ConnectionOpportunity Opportunity { get; set; }

            public List<int> LoadedOpportunityIds { get; } = new List<int>();

            public List<GroupRequirementStatus> RequirementResults { get; } = new List<GroupRequirementStatus>();

            public List<int> RequirementCheckGroupIds { get; } = new List<int>();

            public List<int> MemberAttributeIndexes { get; } = new List<int>();

            public TestBlock()
            {
                UseNoUrlKeys();
                CreateControls();
            }

            protected override RockContext CreateRockContext()
            {
                // Never connects: every database read the tested paths make is replaced below.
                return new RockContext( "name=UnusedByTheseTests" );
            }

            protected override ConnectionOpportunity LoadOpportunity( RockContext rockContext, int opportunityId )
            {
                LoadedOpportunityIds.Add( opportunityId );
                return Opportunity != null && Opportunity.Id == opportunityId ? Opportunity : null;
            }

            protected override Group LoadAssignedGroup( RockContext rockContext, int groupId )
            {
                Group group;
                return Groups.TryGetValue( groupId, out group ) ? group : null;
            }

            protected override GroupTypeRole LoadGroupTypeRole( Guid groupTypeRoleGuid )
            {
                if ( groupTypeRoleGuid == MemberRoleGuid )
                {
                    return new GroupTypeRole { Id = MemberRoleId, Guid = MemberRoleGuid };
                }

                return groupTypeRoleGuid == LeaderRoleGuid ? new GroupTypeRole { Id = LeaderRoleId, Guid = LeaderRoleGuid } : null;
            }

            protected override string GetRoleName( RockContext rockContext, int groupRoleId )
            {
                return groupRoleId == MemberRoleId ? "Member" : null;
            }

            protected override IEnumerable<GroupRequirementStatus> CheckGroupRequirements( RockContext rockContext, Group group, int personId, int? groupRoleId )
            {
                RequirementCheckGroupIds.Add( group.Id );
                return RequirementResults;
            }

            protected override Dictionary<string, AttributeValueCache> GetGroupMemberAttributes( RockContext rockContext = null, int RepeaterIndex = 0 )
            {
                MemberAttributeIndexes.Add( RepeaterIndex );
                return new Dictionary<string, AttributeValueCache> { { "Shift", new AttributeValueCache { Value = "Early" } } };
            }

            public void Connect()
            {
                btnConnect_Click( new LinkButton { Text = "Connect" }, EventArgs.Empty );
            }

            public void Show( int opportunityId )
            {
                ShowDetail( opportunityId );
            }

            public bool Build( ConnectionRequest connectionRequest, ConnectionOpportunity opportunity, int index )
            {
                return BuildConnectionRequest( new RockContext( "name=UnusedByTheseTests" ), connectionRequest, opportunity, index,
                    new Person { Id = 5, FirstName = "Pat", NickName = "Pat", LastName = "Doe" }, "Pat", false, false );
            }

            public string ErrorText => nbErrorMessage.Visible ? nbErrorMessage.Text : null;

            public bool SignupPanelVisible => pnlSignup.Visible;

            public bool RequirementMessageShown => lRequirementBlockMessage.Visible;

            public string RequirementMessage => lRequirementBlockMessage.Text;

            protected override Group LoadGroup( int groupId )
            {
                LoadedGroupIds.Add( groupId );
                Group group;
                return Groups.TryGetValue( groupId, out group ) ? group : null;
            }

            public (int GroupId, int GroupMemberRoleId, GroupMemberStatus GroupMemberStatus)? Resolve( ConnectionOpportunity opportunity, ConnectionRoleRequest roleRequest )
            {
                return ResolvePlacement( opportunity, roleRequest );
            }

            public ConnectionRequest Apply( ConnectionOpportunity opportunity, int index )
            {
                var connectionRequest = new ConnectionRequest();
                ApplyPlacement( connectionRequest, opportunity, index );
                return connectionRequest;
            }

            public static bool IsOpen( ConnectionOpportunity opportunity )
            {
                return IsOpenForSignup( opportunity );
            }

            /// <summary>
            /// Binds the role repeater with one item per role request, holding the controls the .ascx declares in
            /// its item template, and sets the hidden group field to the given value (what a posted form could carry).
            /// </summary>
            public void BindRepeater( int itemCount, string postedGroupId )
            {
                rptGroupRoleAttributes = new Repeater { ItemTemplate = new RoleItemTemplate() };
                rptGroupRoleAttributes.DataSource = Enumerable.Range( 0, itemCount ).ToList();
                rptGroupRoleAttributes.DataBind();
                foreach ( RepeaterItem item in rptGroupRoleAttributes.Items )
                {
                    ( ( HiddenField ) item.FindControl( "hdnGroupId" ) ).Value = postedGroupId;
                }
            }
        }

        private class RoleItemTemplate : ITemplate
        {
            public void InstantiateIn( Control container )
            {
                container.Controls.Add( new RockLiteral { ID = "ltRole" } );
                container.Controls.Add( new HiddenField { ID = "hdnGroupId" } );
                container.Controls.Add( new PlaceHolder { ID = "phAttributes" } );
            }
        }

        private static Group NewGroup( int id, int groupTypeId, bool isActive = true, bool isArchived = false, bool isSecurityRole = false )
        {
            return new Group
            {
                Id = id,
                Name = "Group " + id,
                GroupTypeId = groupTypeId,
                IsActive = isActive,
                IsArchived = isArchived,
                IsSecurityRole = isSecurityRole
            };
        }

        private static TestBlock NewBlock()
        {
            var block = new TestBlock();
            foreach ( var group in new[]
            {
                NewGroup( ServingGroupId, ServingTeamTypeId ),
                NewGroup( OtherServingGroupId, ServingTeamTypeId ),
                NewGroup( InactiveGroupId, ServingTeamTypeId, isActive: false ),
                NewGroup( ArchivedGroupId, ServingTeamTypeId, isArchived: true ),
                NewGroup( SecurityRoleGroupId, ServingTeamTypeId, isSecurityRole: true ),
                NewGroup( SecurityTypeGroupId, SecurityRoleGroupTypeId ),
                NewGroup( OtherTypeGroupId, OtherTypeId ),
                NewGroup( UnlistedServingGroupId, ServingTeamTypeId )
            } )
            {
                block.Groups.Add( group.Id, group );
            }

            return block;
        }

        /// <summary>
        /// An active opportunity of an active connection type, with a Serving Team member config (Pending)
        /// and a config for the security role group type, neither using all groups of its type. Every test
        /// group except the unlisted Serving Team group is on its group list, so only the group checks keep
        /// those groups out. Each list entry's Id differs from its group's Id.
        /// </summary>
        private static ConnectionOpportunity NewOpportunity()
        {
            var opportunity = new ConnectionOpportunity
            {
                Id = 1,
                IsActive = true,
                ConnectionType = new ConnectionType { Id = 1, IsActive = true }
            };
            opportunity.ConnectionOpportunityGroupConfigs.Add( new ConnectionOpportunityGroupConfig
            {
                GroupTypeId = ServingTeamTypeId,
                GroupMemberRoleId = MemberRoleId,
                GroupMemberStatus = GroupMemberStatus.Pending
            } );
            opportunity.ConnectionOpportunityGroupConfigs.Add( new ConnectionOpportunityGroupConfig
            {
                GroupTypeId = SecurityRoleGroupTypeId,
                GroupMemberRoleId = MemberRoleId,
                GroupMemberStatus = GroupMemberStatus.Active
            } );
            foreach ( var groupId in new[] { ServingGroupId, OtherServingGroupId, InactiveGroupId, ArchivedGroupId, SecurityRoleGroupId, SecurityTypeGroupId, OtherTypeGroupId } )
            {
                opportunity.ConnectionOpportunityGroups.Add( new ConnectionOpportunityGroup { Id = 500 + groupId, GroupId = groupId } );
            }
            return opportunity;
        }

        private static ConnectionOpportunityGroupConfig ServingTeamConfig( ConnectionOpportunity opportunity )
        {
            return opportunity.ConnectionOpportunityGroupConfigs.First( gc => gc.GroupTypeId == ServingTeamTypeId );
        }

        private static ConnectionRequest ApplyFromUrl( TestBlock block, int groupId, int roleId )
        {
            block.TestPageParameters["GroupId"] = groupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = roleId.ToString();
            return block.Apply( NewOpportunity(), 0 );
        }

        private static void AssertNoPlacement( ConnectionRequest connectionRequest )
        {
            Assert.Null( connectionRequest.AssignedGroupId );
            Assert.Null( connectionRequest.AssignedGroupMemberRoleId );
            Assert.Null( connectionRequest.AssignedGroupMemberStatus );
        }

        [Fact]
        public void UrlGroupAndRole_ConfiguredGroup_IsPlacedWithConfigStatus()
        {
            var request = ApplyFromUrl( NewBlock(), ServingGroupId, MemberRoleId );

            Assert.Equal( ServingGroupId, request.AssignedGroupId );
            Assert.Equal( MemberRoleId, request.AssignedGroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Pending, request.AssignedGroupMemberStatus );
        }

        [Fact]
        public void UrlGroupAndRole_OtherListedGroupOfConfiguredTypeAndRole_IsPlaced()
        {
            var request = ApplyFromUrl( NewBlock(), OtherServingGroupId, MemberRoleId );

            Assert.Equal( OtherServingGroupId, request.AssignedGroupId );
        }

        [Fact]
        public void UrlGroupAndRole_GroupNotOnTheOpportunitysList_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), UnlistedServingGroupId, MemberRoleId ) );
        }

        [Fact]
        public void GroupNotOnTheList_IsPlaced_WhenTheMatchingConfigUsesAllGroupsOfType()
        {
            var opportunity = NewOpportunity();
            ServingTeamConfig( opportunity ).UseAllGroupsOfType = true;

            var placement = NewBlock().Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = UnlistedServingGroupId, GroupTypeRoleId = MemberRoleId } );

            Assert.True( placement.HasValue );
            Assert.Equal( UnlistedServingGroupId, placement.Value.GroupId );
            Assert.Equal( MemberRoleId, placement.Value.GroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Pending, placement.Value.GroupMemberStatus );
        }

        [Fact]
        public void GroupNotOnTheList_IsPlacedWithItsRolesStatus_WhenAnotherConfigOfItsTypeUsesAllGroups()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionOpportunityGroupConfigs.Add( new ConnectionOpportunityGroupConfig
            {
                GroupTypeId = ServingTeamTypeId,
                GroupMemberRoleId = LeaderRoleId,
                GroupMemberStatus = GroupMemberStatus.Active,
                UseAllGroupsOfType = true
            } );

            var placement = NewBlock().Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = UnlistedServingGroupId, GroupTypeRoleId = MemberRoleId } );

            Assert.True( placement.HasValue );
            Assert.Equal( UnlistedServingGroupId, placement.Value.GroupId );
            Assert.Equal( MemberRoleId, placement.Value.GroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Pending, placement.Value.GroupMemberStatus );
        }

        [Fact]
        public void GroupNotOnTheList_IsNotPlaced_WhenOnlyConfigsOfOtherTypesUseAllGroups()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionOpportunityGroupConfigs.Add( new ConnectionOpportunityGroupConfig
            {
                GroupTypeId = OtherTypeId,
                GroupMemberRoleId = MemberRoleId,
                GroupMemberStatus = GroupMemberStatus.Active
            } );
            foreach ( var config in opportunity.ConnectionOpportunityGroupConfigs.Where( gc => gc.GroupTypeId != ServingTeamTypeId ) )
            {
                config.UseAllGroupsOfType = true;
            }

            var placement = NewBlock().Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = UnlistedServingGroupId, GroupTypeRoleId = MemberRoleId } );

            Assert.Null( placement );
        }

        [Fact]
        public void GroupNotOnTheList_IsNotPlaced_ForARoleWithoutAConfig_EvenWhenItsTypeUsesAllGroups()
        {
            var opportunity = NewOpportunity();
            ServingTeamConfig( opportunity ).UseAllGroupsOfType = true;

            Assert.Null( NewBlock().Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = UnlistedServingGroupId, GroupTypeRoleId = LeaderRoleId } ) );
        }

        [Fact]
        public void InactiveGroup_IsNotPlaced_WhenTheMatchingConfigUsesAllGroupsOfType()
        {
            var opportunity = NewOpportunity();
            ServingTeamConfig( opportunity ).UseAllGroupsOfType = true;

            Assert.Null( NewBlock().Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = InactiveGroupId, GroupTypeRoleId = MemberRoleId } ) );
        }

        [Fact]
        public void UrlGroupAndRoleGuid_IsPlacedWithThatRole()
        {
            var block = NewBlock();
            block.TestPageParameters["GroupId"] = ServingGroupId.ToString();
            block.TestPageParameters["GroupTypeRole"] = MemberRoleGuid.ToString();

            var request = block.Apply( NewOpportunity(), 0 );

            Assert.Equal( ServingGroupId, request.AssignedGroupId );
            Assert.Equal( MemberRoleId, request.AssignedGroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Pending, request.AssignedGroupMemberStatus );
        }

        /// <summary>
        /// The shape the CardWizard template sends: group Id as a string and the role as a Guid, no role Id.
        /// </summary>
        [Fact]
        public void RoleRequestsJson_RoleGuid_IsResolvedAndChecked()
        {
            var block = NewBlock();
            block.TestPageParameters["RoleRequests"] = new[]
            {
                new { GroupId = ServingGroupId.ToString(), GroupTypeRole = MemberRoleGuid.ToString() },
                new { GroupId = OtherServingGroupId.ToString(), GroupTypeRole = LeaderRoleGuid.ToString() },
                new { GroupId = UnlistedServingGroupId.ToString(), GroupTypeRole = MemberRoleGuid.ToString() }
            }.ToJson();
            var opportunity = NewOpportunity();

            var first = block.Apply( opportunity, 0 );
            Assert.Equal( ServingGroupId, first.AssignedGroupId );
            Assert.Equal( MemberRoleId, first.AssignedGroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Pending, first.AssignedGroupMemberStatus );

            AssertNoPlacement( block.Apply( opportunity, 1 ) );
            AssertNoPlacement( block.Apply( opportunity, 2 ) );
        }

        [Fact]
        public void UrlGroupAndRole_SecurityRoleGroup_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), SecurityRoleGroupId, MemberRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_GroupOfSecurityRoleType_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), SecurityTypeGroupId, MemberRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_InactiveGroup_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), InactiveGroupId, MemberRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_ArchivedGroup_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), ArchivedGroupId, MemberRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_GroupThatDoesNotExist_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), MissingGroupId, MemberRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_ConfiguredTypeButOtherRole_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), ServingGroupId, LeaderRoleId ) );
        }

        [Fact]
        public void UrlGroupAndRole_ConfiguredRoleButGroupOfOtherType_IsNotPlaced()
        {
            AssertNoPlacement( ApplyFromUrl( NewBlock(), OtherTypeGroupId, MemberRoleId ) );
        }

        /// <summary>
        /// A role request that reaches placement with no role. (A URL GroupId alone never does: the role requests
        /// fill in the group type's default role first, which the local check covers.)
        /// </summary>
        [Fact]
        public void RoleRequestWithoutRoleId_IsNotPlacedAndNoGroupIsLoaded()
        {
            var block = NewBlock();
            var request = block.Resolve( NewOpportunity(), new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = ServingGroupId } );

            Assert.Null( request );
            Assert.Empty( block.LoadedGroupIds );
        }

        [Fact]
        public void NoGroup_IsNotPlaced()
        {
            var block = NewBlock();
            AssertNoPlacement( block.Apply( NewOpportunity(), 0 ) );
            Assert.Empty( block.LoadedGroupIds );
        }

        [Fact]
        public void StatusComesFromTheConfigMatchingGroupTypeAndRole()
        {
            var block = NewBlock();
            var opportunity = NewOpportunity();
            opportunity.ConnectionOpportunityGroupConfigs.Add( new ConnectionOpportunityGroupConfig
            {
                GroupTypeId = OtherTypeId,
                GroupMemberRoleId = MemberRoleId,
                GroupMemberStatus = GroupMemberStatus.Inactive
            } );

            var placement = block.Resolve( opportunity, new VolunteerSignupFormConnections.ConnectionRoleRequest { GroupId = OtherTypeGroupId, GroupTypeRoleId = MemberRoleId } );

            Assert.True( placement.HasValue );
            Assert.Equal( OtherTypeGroupId, placement.Value.GroupId );
            Assert.Equal( MemberRoleId, placement.Value.GroupMemberRoleId );
            Assert.Equal( GroupMemberStatus.Inactive, placement.Value.GroupMemberStatus );
        }

        [Fact]
        public void RoleRequestsJson_EachEntryIsCheckedOnItsOwn()
        {
            var block = NewBlock();
            block.TestPageParameters["RoleRequests"] = new[]
            {
                new { GroupId = ServingGroupId, GroupTypeRoleId = MemberRoleId },
                new { GroupId = SecurityRoleGroupId, GroupTypeRoleId = MemberRoleId },
                new { GroupId = InactiveGroupId, GroupTypeRoleId = MemberRoleId },
                new { GroupId = OtherServingGroupId, GroupTypeRoleId = LeaderRoleId },
                new { GroupId = OtherServingGroupId, GroupTypeRoleId = MemberRoleId }
            }.ToJson();
            var opportunity = NewOpportunity();

            var first = block.Apply( opportunity, 0 );
            Assert.Equal( ServingGroupId, first.AssignedGroupId );
            Assert.Equal( GroupMemberStatus.Pending, first.AssignedGroupMemberStatus );

            AssertNoPlacement( block.Apply( opportunity, 1 ) );
            AssertNoPlacement( block.Apply( opportunity, 2 ) );
            AssertNoPlacement( block.Apply( opportunity, 3 ) );

            var last = block.Apply( opportunity, 4 );
            Assert.Equal( OtherServingGroupId, last.AssignedGroupId );
            Assert.Equal( MemberRoleId, last.AssignedGroupMemberRoleId );
        }

        [Fact]
        public void PostedHiddenFields_AreNotUsed_ForAConfiguredRequest()
        {
            var block = NewBlock();
            block.TestPageParameters["GroupId"] = ServingGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            block.BindRepeater( 1, SecurityRoleGroupId.ToString() );

            var request = block.Apply( NewOpportunity(), 0 );

            Assert.Equal( ServingGroupId, request.AssignedGroupId );
            Assert.Equal( MemberRoleId, request.AssignedGroupMemberRoleId );
        }

        [Fact]
        public void PostedHiddenFields_AreNotUsed_ForARequestWithoutPlacement()
        {
            var block = NewBlock();
            block.TestPageParameters["GroupId"] = SecurityRoleGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            block.BindRepeater( 1, ServingGroupId.ToString() );

            AssertNoPlacement( block.Apply( NewOpportunity(), 0 ) );
        }

        [Fact]
        public void ActiveOpportunityOfActiveType_IsOpenForSignup()
        {
            Assert.True( TestBlock.IsOpen( NewOpportunity() ) );
        }

        [Fact]
        public void InactiveOpportunity_IsNotOpenForSignup()
        {
            var opportunity = NewOpportunity();
            opportunity.IsActive = false;

            Assert.False( TestBlock.IsOpen( opportunity ) );
        }

        [Fact]
        public void OpportunityOfInactiveConnectionType_IsNotOpenForSignup()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionType.IsActive = false;

            Assert.False( TestBlock.IsOpen( opportunity ) );
        }

        [Fact]
        public void MissingOpportunityOrConnectionType_IsNotOpenForSignup()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionType = null;

            Assert.False( TestBlock.IsOpen( opportunity ) );
            Assert.False( TestBlock.IsOpen( null ) );
        }

        private static TestBlock BlockWithOpportunity( ConnectionOpportunity opportunity )
        {
            var block = NewBlock();
            block.Opportunity = opportunity;
            block.TestPageParameters["OpportunityId"] = "1";
            return block;
        }

        private static GroupRequirementStatus Requirement( string typeName, MeetsGroupRequirement meets )
        {
            return new GroupRequirementStatus
            {
                GroupRequirement = new GroupRequirement
                {
                    GroupRequirementType = new GroupRequirementType { Name = typeName, NegativeLabel = "Not " + typeName, RequirementCheckType = RequirementCheckType.Sql }
                },
                MeetsGroupRequirement = meets
            };
        }

        [Fact]
        public void Connect_InactiveOpportunity_ShowsNotTakingSignupsAndStops()
        {
            var opportunity = NewOpportunity();
            opportunity.IsActive = false;
            var block = BlockWithOpportunity( opportunity );

            block.Connect();

            Assert.Equal( new[] { 1 }, block.LoadedOpportunityIds );
            Assert.Contains( NotTakingSignupsMessage, block.ErrorText );
            Assert.False( block.SignupPanelVisible );
        }

        [Fact]
        public void Connect_OpportunityOfInactiveConnectionType_ShowsNotTakingSignupsAndStops()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionType.IsActive = false;
            var block = BlockWithOpportunity( opportunity );

            block.Connect();

            Assert.Contains( NotTakingSignupsMessage, block.ErrorText );
            Assert.False( block.SignupPanelVisible );
        }

        [Fact]
        public void Connect_MissingOpportunity_ShowsDoesNotExistAndStops()
        {
            var block = BlockWithOpportunity( null );

            block.Connect();

            Assert.Contains( DoesNotExistMessage, block.ErrorText );
        }

        /// <summary>
        /// The test opportunity's connection type has no default status, so Connect stops right after the
        /// opportunity check without creating anyone.
        /// </summary>
        [Fact]
        public void Connect_OpenOpportunity_IsNotRefused()
        {
            var block = BlockWithOpportunity( NewOpportunity() );

            block.Connect();

            Assert.Equal( new[] { 1 }, block.LoadedOpportunityIds );
            Assert.True( string.IsNullOrEmpty( block.ErrorText ) );
            Assert.True( block.SignupPanelVisible );
        }

        [Fact]
        public void ShowDetail_InactiveOpportunity_ShowsNotTakingSignups()
        {
            var opportunity = NewOpportunity();
            opportunity.IsActive = false;
            var block = BlockWithOpportunity( opportunity );

            block.Show( 1 );

            Assert.Equal( new[] { 1 }, block.LoadedOpportunityIds );
            Assert.Contains( NotTakingSignupsMessage, block.ErrorText );
            Assert.False( block.SignupPanelVisible );
        }

        [Fact]
        public void ShowDetail_OpportunityOfInactiveConnectionType_ShowsNotTakingSignups()
        {
            var opportunity = NewOpportunity();
            opportunity.ConnectionType.IsActive = false;
            var block = BlockWithOpportunity( opportunity );

            block.Show( 1 );

            Assert.Contains( NotTakingSignupsMessage, block.ErrorText );
            Assert.False( block.SignupPanelVisible );
        }

        [Fact]
        public void ShowDetail_MissingOpportunity_ShowsDoesNotExist()
        {
            var block = BlockWithOpportunity( null );

            block.Show( 1 );

            Assert.Contains( DoesNotExistMessage, block.ErrorText );
            Assert.False( block.SignupPanelVisible );
        }

        [Fact]
        public void Build_ConfiguredRequest_IsPlacedWithMemberAttributes()
        {
            var block = NewBlock();
            block.TestPageParameters["GroupId"] = ServingGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            var request = new ConnectionRequest();

            Assert.True( block.Build( request, NewOpportunity(), 0 ) );

            Assert.Equal( ServingGroupId, request.AssignedGroupId );
            Assert.Equal( MemberRoleId, request.AssignedGroupMemberRoleId );
            Assert.Equal( new[] { 0 }, block.MemberAttributeIndexes );
            Assert.Contains( "Early", request.AssignedGroupMemberAttributeValues );
            Assert.Empty( block.RequirementCheckGroupIds );
        }

        [Fact]
        public void Build_UsesTheRoleRequestAtTheGivenIndex()
        {
            var block = NewBlock();
            block.TestPageParameters["RoleRequests"] = new[]
            {
                new { GroupId = SecurityRoleGroupId, GroupTypeRoleId = MemberRoleId },
                new { GroupId = OtherServingGroupId, GroupTypeRoleId = MemberRoleId }
            }.ToJson();
            var request = new ConnectionRequest();

            Assert.True( block.Build( request, NewOpportunity(), 1 ) );

            Assert.Equal( OtherServingGroupId, request.AssignedGroupId );
            Assert.Equal( new[] { 1 }, block.MemberAttributeIndexes );
        }

        [Fact]
        public void Build_RequirementNotMet_BlocksThePlacedRequest()
        {
            var block = NewBlock();
            block.TestBlockSettings["SignupBlockingRequirementTypes"] = "Over 18";
            block.RequirementResults.Add( Requirement( "Over 18", MeetsGroupRequirement.NotMet ) );
            block.TestPageParameters["GroupId"] = ServingGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            var request = new ConnectionRequest();

            Assert.False( block.Build( request, NewOpportunity(), 0 ) );

            Assert.Equal( new[] { ServingGroupId }, block.RequirementCheckGroupIds );
            Assert.True( block.RequirementMessageShown );
            Assert.Contains( "Pat isn't eligible for the Member role", block.RequirementMessage );
            Assert.Contains( "Not Over 18", block.RequirementMessage );
            Assert.True( block.SignupPanelVisible );
            Assert.Empty( block.MemberAttributeIndexes );
        }

        [Fact]
        public void Build_RequirementMet_KeepsThePlacedRequest()
        {
            var block = NewBlock();
            block.TestBlockSettings["SignupBlockingRequirementTypes"] = "Over 18";
            block.RequirementResults.Add( Requirement( "Over 18", MeetsGroupRequirement.Meets ) );
            block.TestPageParameters["GroupId"] = ServingGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            var request = new ConnectionRequest();

            Assert.True( block.Build( request, NewOpportunity(), 0 ) );

            Assert.Equal( new[] { ServingGroupId }, block.RequirementCheckGroupIds );
            Assert.False( block.RequirementMessageShown );
            Assert.Equal( ServingGroupId, request.AssignedGroupId );
        }

        [Fact]
        public void Build_RequestWithoutPlacement_SkipsRequirementsAndMemberAttributes()
        {
            var block = NewBlock();
            block.TestBlockSettings["SignupBlockingRequirementTypes"] = "Over 18";
            block.RequirementResults.Add( Requirement( "Over 18", MeetsGroupRequirement.NotMet ) );
            block.TestPageParameters["GroupId"] = SecurityRoleGroupId.ToString();
            block.TestPageParameters["GroupTypeRoleId"] = MemberRoleId.ToString();
            var request = new ConnectionRequest();

            Assert.True( block.Build( request, NewOpportunity(), 0 ) );

            AssertNoPlacement( request );
            Assert.Null( request.AssignedGroupMemberAttributeValues );
            Assert.Empty( block.RequirementCheckGroupIds );
            Assert.Empty( block.MemberAttributeIndexes );
        }
    }
}
