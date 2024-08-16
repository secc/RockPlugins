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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using System;
using org.secc.Rest.Models;
using Rock;

namespace org.secc.Rest.Controllers
{
    public partial class GroupAppGroupListController : ApiControllerBase
    {
        /// <summary>
        /// Returns the groups for which the current user is a group member with a role including "leader" from the group types defined in the "Group App Group Type
        /// </summary>
        /// <returns>List<Group></returns> 
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/GroupList/" )]
        public IHttpActionResult GetGroupList()
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }

            var rockContext = new RockContext();
            rockContext.Configuration.ProxyCreationEnabled = false;

            var groupServiceHelper = new GroupServiceHelper( rockContext );
            var groupTypeIds = groupServiceHelper.GetGroupTypeIdsFromDefinedType( "Group App Group Types" );
            var groupsAsLeader = groupServiceHelper.GetGroups( currentUser.Person.Id, groupTypeIds );

            return Ok( groupsAsLeader );

        }

        /// <summary>
        /// Returns the groups for which the current user is a group member with a role including "leader" from the group types defined in the "Group App Group Type
        /// </summary>
        /// <returns>List<Group></returns> 
        [System.Web.Http.Route( "api/GroupApp/GetGroup/{groupId}" )]
        public IHttpActionResult GetGroup( int groupId, bool getContent = false, bool getAllowEmailParents = false )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
            {
                return StatusCode( HttpStatusCode.Unauthorized );
            }
            var group = new GroupService( new RockContext() ).Get( groupId );
            if ( group == null )
            {
                return NotFound();
            }
            if ( !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
            {
                return StatusCode( HttpStatusCode.Forbidden );
            }

            if ( !getContent && !getAllowEmailParents )
            {
                return Ok( new
                {
                    group.Name,
                    group.TypeId,
                    group.IsActive,
                    group.IsArchived
                }
                );
            }
            else
            {
                group.LoadAttributes();
                var groupContentItems = new List<GroupContentItem>();
                bool? emailParentsEnabled = null;

                if ( getContent )
                {
                    var groupServiceHelper = new GroupServiceHelper( new RockContext() );
                    groupContentItems = groupServiceHelper.GetGroupContentItems( groupId );
                }

                if ( getAllowEmailParents )
                {
                    emailParentsEnabled = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
                }

                return Ok(
                    new
                    {
                        group.Name,
                        group.TypeId,
                        group.IsActive,
                        group.IsArchived,
                        groupContentItems,
                        emailParentsEnabled
                    }
                );
            }
        }
    }

    public class GroupContentItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public Guid? SeriesParallaxBackground { get; set; }
        public Guid? LeaderGuide { get; set; }
        public Guid? ParticipantGuide { get; set; }
        public Guid? IceBreakers { get; set; }

    }

    public class GroupServiceHelper
    {
        private readonly RockContext _rockContext;

        public GroupServiceHelper( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        public List<int> GetGroupTypeIdsFromDefinedType( string definedTypeName )
        {
            var definedTypeService = new DefinedTypeService( _rockContext );
            var groupTypeDefinedType = definedTypeService
                .Queryable()
                .FirstOrDefault( dt => dt.Name == definedTypeName );

            if ( groupTypeDefinedType == null )
                return new List<int>();

            var definedValueService = new DefinedValueService( _rockContext );
            var groupTypeDefinedValues = definedValueService
                .Queryable()
                .Where( dv => dv.DefinedTypeId == groupTypeDefinedType.Id )
                .ToList();

            return groupTypeDefinedValues.Select( dv => int.Parse( dv.Value ) ).ToList();
        }

        public List<GroupAppGroup> GetGroups( int currentPersonId, List<int> groupTypeIds )
        {
            return new GroupMemberService( _rockContext )
                .Queryable( "Group, GroupRole, Schedule" )
                .Where( gm => gm.PersonId == currentPersonId &&
                             groupTypeIds.Contains( gm.Group.GroupTypeId )
                             && gm.Group.IsActive && !gm.Group.IsArchived)
                .Select( gm => new GroupAppGroup
                {
                    Id = gm.Group.Id,
                    Name = gm.Group.Name,
                    IsActive = gm.Group.IsActive,
                    IsArchived = gm.Group.IsArchived,
                    IsLeader = gm.GroupRole.IsLeader
                } )
                .Distinct()
                .ToList();
        }

        public List<GroupContentItem> GetGroupContentItems( int groupId )
        {
            var group = new GroupService( _rockContext ).Get( groupId );
            if ( group == null )
            {
                return new List<GroupContentItem>();
            }
            group.LoadAttributes();
            var groupContentChannelItems = new ContentChannelItemService( _rockContext).GetByGuids(group.GetAttributeValue( "NeighborhoodGroupContent" ).SplitDelimitedValues().AsGuidList());
            var groupContentItems = new List<GroupContentItem>();

            foreach ( var item in groupContentChannelItems )
            {
                if ( item.StartDateTime < Rock.RockDateTime.Now && ( item.ExpireDateTime > Rock.RockDateTime.Now || item.ExpireDateTime == null ) )
                {
                    item.LoadAttributes();
                    var groupContentItem = new GroupContentItem
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Content = item.Content,
                        StartDateTime = item.StartDateTime,
                        EndDateTime = item.ExpireDateTime,
                        SeriesParallaxBackground = item.GetAttributeValue( "SeriesParallaxBackground" ).AsGuidOrNull(),
                        LeaderGuide = item.GetAttributeValue( "LeaderGuide" ).AsGuidOrNull(),
                        ParticipantGuide = item.GetAttributeValue( "ParticipantGuide" ).AsGuidOrNull(),
                        IceBreakers = item.GetAttributeValue( "IceBreakers" ).AsGuidOrNull()
                    };
                    
                    groupContentItems.Add( groupContentItem );
                }
            }


            return groupContentItems;
        }
    }
}
