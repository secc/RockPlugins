using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Communication
{
    /// <summary>
    /// The one rule for which Communication Lists a person may see and subscribe to, shared by
    /// the staff wizard and the Communication Preferences block so the two can't drift apart.
    /// </summary>
    public static class CommunicationLists
    {
        /// <summary>
        /// Limits to public Communication Lists that are active. A list in
        /// <paramref name="memberOfGroupIds"/> is kept even when inactive: Rock still sends to a
        /// list's active members regardless of the list's IsActive flag, so a member must keep
        /// the ability to unsubscribe. Archived groups are already excluded by
        /// GroupService.Queryable().
        /// </summary>
        public static IQueryable<Group> WhereVisible( this IQueryable<Group> groups, ICollection<int> memberOfGroupIds = null )
        {
            var communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;
            // Copied to a List so EF translates Contains to an IN clause.
            var memberIds = memberOfGroupIds?.ToList() ?? new List<int>();

            return groups.Where( g => g.GroupTypeId == communicationListGroupTypeId
                && g.IsPublic
                && ( g.IsActive || memberIds.Contains( g.Id ) ) );
        }

        /// <summary>
        /// True when <paramref name="keyword"/> is one of the pipe-delimited keywords in
        /// <paramref name="attributeValue"/>. Whole-keyword match only, so VBS does not match VBSX.
        /// </summary>
        public static bool KeywordMatches( string attributeValue, string keyword )
        {
            if ( string.IsNullOrWhiteSpace( attributeValue ) || string.IsNullOrWhiteSpace( keyword ) )
            {
                return false;
            }

            var target = keyword.Trim();
            return attributeValue.Split( '|' )
                .Any( k => k.Trim().Equals( target, StringComparison.OrdinalIgnoreCase ) );
        }
    }
}
