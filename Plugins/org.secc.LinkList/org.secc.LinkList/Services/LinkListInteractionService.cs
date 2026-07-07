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
using System;

using org.secc.LinkList.SystemGuids;

using Rock;
using Rock.Model;
using Rock.Transactions;
using Rock.Web.Cache;

namespace org.secc.LinkList.Services
{
    /// <summary>
    /// ROCK-7164: records Link List page views and link clicks as Rock
    /// Interactions on the "Link Lists" channel (migration 011). Writes are
    /// queued via <see cref="InteractionTransaction"/> (bulk-inserted off the
    /// request thread, ~60s drain) and one InteractionComponent per list is
    /// auto-created on first record (EntityId = ContentChannelItem.Id).
    ///
    /// Crawler traffic is filtered automatically: InteractionTransaction drops
    /// rows whose UserAgent classifies as "Crawler" while LogCrawlers is false
    /// (the default) - callers just pass the UA through.
    ///
    /// Every public method is fail-safe: analytics must never take down a
    /// public page view, so failures log and return.
    /// </summary>
    public static class LinkListInteractionService
    {
        /// <summary>
        /// The "Link Lists" InteractionChannel id. Fixed-guid cache lookup
        /// first; falls back to the medium + channel-entity lookup, which
        /// self-heals (creates an identical channel) if the migration row was
        /// deleted. Null when neither resolves (e.g. migration 011 never ran)
        /// - callers then no-op.
        /// </summary>
        public static int? GetChannelId()
        {
            var channel = InteractionChannelCache.Get( LinkListGuids.InteractionChannelLinkLists.AsGuid() );
            if ( channel != null )
            {
                return channel.Id;
            }

            var mediumValueId = DefinedValueCache.Get( LinkListGuids.InteractionMediumLinkList.AsGuid() )?.Id;
            var contentChannelId = ContentChannelCache.Get( LinkListGuids.LinkListChannel.AsGuid() )?.Id;
            if ( !mediumValueId.HasValue || !contentChannelId.HasValue )
            {
                return null;
            }

            var componentEntityTypeId = EntityTypeCache.Get( typeof( ContentChannelItem ) )?.Id;
            return InteractionChannelCache.GetChannelIdByTypeIdAndEntityId(
                mediumValueId,
                contentChannelId,
                "Link Lists",
                componentEntityTypeId,
                interactionEntityTypeId: null );
        }

        /// <summary>Queues a "View" interaction for a list.</summary>
        /// <param name="itemId">ContentChannelItem id (becomes the component EntityId).</param>
        /// <param name="itemTitle">List title (component name on first record; interaction summary).</param>
        /// <param name="pageUrl">The viewed page - prefer the embedding page (Referer) over the API URL.</param>
        public static void RecordView( int itemId, string itemTitle, string pageUrl, string userAgent, string ipAddress, int? personAliasId )
        {
            Record( "View", itemId, itemTitle, interactionEntityId: null, data: pageUrl, summary: itemTitle, userAgent, ipAddress, personAliasId );
        }

        /// <summary>Queues a "Click" interaction for a single link row.</summary>
        /// <param name="matrixItemId">AttributeMatrixItem id of the clicked row (stable across rename/reorder).</param>
        /// <param name="linkUrl">The link's URL, read server-side (never from the client payload).</param>
        /// <param name="linkText">The link's text, read server-side.</param>
        public static void RecordClick( int itemId, string itemTitle, int matrixItemId, string linkUrl, string linkText, string userAgent, string ipAddress, int? personAliasId )
        {
            Record( "Click", itemId, itemTitle, matrixItemId, data: linkUrl, summary: linkText, userAgent, ipAddress, personAliasId );
        }

        private static void Record( string operation, int itemId, string itemTitle, int? interactionEntityId, string data, string summary, string userAgent, string ipAddress, int? personAliasId )
        {
            try
            {
                var channelId = GetChannelId();
                if ( !channelId.HasValue )
                {
                    return;
                }

                var info = new InteractionTransactionInfo
                {
                    // Deterministic: REST + block-action callers pass everything
                    // explicitly; never sniff the ambient HttpContext.
                    GetValuesFromHttpRequest = false,

                    InteractionChannelId = channelId.Value,
                    ComponentEntityId = itemId,
                    ComponentName = itemTitle,

                    InteractionOperation = operation,
                    InteractionEntityId = interactionEntityId,
                    InteractionData = data,
                    InteractionSummary = summary, // auto-truncated to 500
                    UserAgent = userAgent,        // drives the automatic crawler skip
                    IPAddress = ipAddress,
                    PersonAliasId = personAliasId
                };

                new InteractionTransaction( info ).Enqueue();
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
            }
        }
    }
}
