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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using org.secc.Rest.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;
using Rock.Web.Cache;

namespace org.secc.Rest.Controllers
{
    public partial class GroupAppGroupListController : ApiControllerBase
    {
        private readonly GroupMemberService _groupMemberService;

        public GroupAppGroupListController()
        {
            var rockContext = new RockContext();
            _groupMemberService = new GroupMemberService( rockContext );
        }

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
            var groupList = groupServiceHelper.GetGroups( currentUser.Person.Id, groupTypeIds );

            return Ok( groupList );

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

            bool isGroupMember = _groupMemberService.Queryable()
                .Any( gm => gm.GroupId == group.Id
                    && gm.PersonId == currentUser.Person.Id
                    && gm.IsArchived == false
                    && gm.GroupMemberStatus == GroupMemberStatus.Active );

            if ( isGroupMember || !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
            {


                if ( !getContent && !getAllowEmailParents )
                {
                    return Ok( new
                    {
                        group.Name,
                        group.TypeId,
                        group.IsActive,
                        group.IsArchived,
                        NextSchedule = group.Schedule.GetNextStartDateTime( RockDateTime.Today )
                    }
                    );
                }
                else
                {
                    group.LoadAttributes();
                    var GroupContentItems = new List<GroupContentItem>();
                    bool? EmailParentsEnabled = null;

                    if ( getContent )
                    {
                        var groupServiceHelper = new GroupServiceHelper( new RockContext() );
                        GroupContentItems = groupServiceHelper.GetGroupContentItems( groupId );
                    }

                    if ( getAllowEmailParents )
                    {
                        EmailParentsEnabled = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
                    }

                    DateTime? nextSchedule;
                    if ( group.Schedule != null )
                    {
                        nextSchedule = group.Schedule.GetNextStartDateTime( RockDateTime.Today );
                    }
                    else
                    {
                        nextSchedule = null;
                    }

                    return Ok(
                        new
                        {
                            group.Name,
                            group.TypeId,
                            group.IsActive,
                            group.IsArchived,
                            NextSchedule = nextSchedule,
                            GroupContentItems,
                            EmailParentsEnabled
                        }
                    );
                }
            }
            else
            {
                return StatusCode( HttpStatusCode.Forbidden );
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
            var groupMembers = new GroupMemberService( _rockContext )
            .Queryable( "Group, GroupRole, Group.Campus, Group.Campus.Location, Group.GroupLocations, Group.GroupLocations.Location, Group.Schedule" )
            .Where( gm => gm.PersonId == currentPersonId &&
                         groupTypeIds.Contains( gm.Group.GroupTypeId ) )
            .ToList();

            var groupList = groupMembers.Select( gm => new GroupAppGroup
            {
                Id = gm.Group?.Id ?? 0,
                GroupTypeId = gm.Group?.GroupTypeId,
                Name = gm.Group?.Name ?? string.Empty,
                IsActive = gm.Group?.IsActive ?? false,
                IsArchived = gm.Group?.IsArchived ?? false,
                IsLeader = gm.GroupRole?.IsLeader ?? false,
                LocationName = gm.Group?.GroupLocations.FirstOrDefault() == null ? // If there are no group locations, use the campus location name
                    ( gm.Group?.Campus?.Location?.Name ?? string.Empty ) : // If there's a campus name, use that
                        gm.Group.GroupLocations.FirstOrDefault()?.Location?.Name ?? // If there is a group location name, use that
                        ( gm.Group.GroupLocations.FirstOrDefault()?.Location?.Street1 ?? string.Empty ), // otherwise, use the group location address                            
                LocationAddress = gm.Group?.GroupLocations.FirstOrDefault()?.Location?.FormattedAddress ?? string.Empty,
                NextSchedule = ( bool ) ( gm.Group?.IsActive ) && ( bool ) ( !gm.Group?.IsArchived ) ? gm.Group?.Schedule?.GetNextStartDateTime( RockDateTime.Today ) ?? // if the first result is null,
                                                                                                                                                                         //construct the next occurrence from the weeklydayofweek and weeklytimeofday properties on the schedule
                    ( GetNextWeeklyOccurrence( gm.Group?.Schedule ) ) : null,
                Url = gm.Group?.IsPublic == true ?
                        GlobalAttributesCache.Value( "PublicApplicationRoot" ) + "groups/homegroups/registration/" + gm.Group?.Id
                    : null,
                GroupTracker = ( gm.Group?.GroupTypeId == 107 || gm.Group?.GroupTypeId == 109 ) && gm.Group?.CampusId == 1
            } ).Distinct().OrderByDescending( g => g.IsLeader ).ThenBy( g => g.NextSchedule ).ToList();

            return groupList;
        }

        public List<GroupContentItem> GetGroupContentItems( int groupId )
        {
            var group = new GroupService( _rockContext ).Get( groupId );
            if ( group == null )
            {
                return new List<GroupContentItem>();
            }
            group.LoadAttributes();
            var groupContentChannelItems = new ContentChannelItemService( _rockContext ).GetByGuids( group.GetAttributeValue( "NeighborhoodGroupContent" ).SplitDelimitedValues().AsGuidList() );
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

        private DateTime? GetNextWeeklyOccurrence( Schedule schedule )
        {
            if ( schedule == null || !schedule.WeeklyDayOfWeek.HasValue || !schedule.WeeklyTimeOfDay.HasValue || !schedule.IsActive )
            {
                return null;
            }

            var today = RockDateTime.Today;
            var daysUntilNextOccurrence = ( ( int ) schedule.WeeklyDayOfWeek.Value - ( int ) today.DayOfWeek + 7 ) % 7;
            if ( daysUntilNextOccurrence == 0 && schedule.WeeklyTimeOfDay.Value < RockDateTime.Now.TimeOfDay )
            {
                daysUntilNextOccurrence = 7;
            }

            var nextOccurrenceDate = today.AddDays( daysUntilNextOccurrence );
            var nextOccurrenceDateTime = nextOccurrenceDate.Add( schedule.WeeklyTimeOfDay.Value );

            // Check if the next occurrence is past the end date of the schedule
            if ( schedule.EffectiveEndDate.HasValue && nextOccurrenceDateTime > schedule.EffectiveEndDate.Value )
            {
                return null;
            }

            return nextOccurrenceDateTime;
        }
    }
}
