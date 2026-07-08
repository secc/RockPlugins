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
using System.ComponentModel;

using org.secc.LinkList.Services;
using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.ViewModels;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace org.secc.LinkList.Blocks
{
    [DisplayName( "Link List Viewer" )]
    [Category( "SECC > Link Lists" )]
    [Description( "Display a Link List using slug-based routing." )]
    [IconCssClass( "fa fa-external-link" )]
    [SupportedSiteTypes( SiteType.Web )]
    [Rock.SystemGuid.BlockTypeGuid( LinkListGuids.BlockTypeLinkListViewer )]
    [CustomDropdownListField( "Slug Source",
        Description = "'Url' reads the route parameter `slug`; 'Manual' forces a fixed slug.",
        ListSource = "Url,Manual",
        IsRequired = true,
        DefaultValue = "Url",
        Order = 0,
        Key = AttributeKey.SlugSource )]
    [TextField( "Manual Slug",
        Description = "Used only when Slug Source is Manual.",
        IsRequired = false,
        Order = 1,
        Key = AttributeKey.ManualSlug )]
    [LinkedPage( "Not Found Page",
        Description = "Page to redirect to when no list matches the slug. When unset, falls back to the legacy default of /page/255.",
        IsRequired = false,
        Order = 2,
        Key = AttributeKey.NotFoundPage )]
    public class LinkListViewerBlock : RockBlockType
    {
        private static class AttributeKey
        {
            public const string SlugSource = "SlugSource";
            public const string ManualSlug = "ManualSlug";
            public const string NotFoundPage = "NotFoundPage";
        }

        /// <inheritdoc/>
        public override string ObsidianFileUrl => "~/Plugins/org_secc/LinkList/linkListViewer.obs";

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var notFoundUrl = this.GetLinkedPageUrl( AttributeKey.NotFoundPage, new Dictionary<string, string>() );
            if ( notFoundUrl.IsNullOrWhiteSpace() )
            {
                // Legacy default: unknown slugs redirect to page 255. Other
                // installs override this with the "Not Found Page" setting.
                notFoundUrl = "/page/255";
            }

            return new LinkListViewerConfigBox
            {
                NotFoundUrl = notFoundUrl
            };
        }

        [BlockAction]
        public BlockActionResult GetListBySlug()
        {
            var slugSource = GetAttributeValue( AttributeKey.SlugSource );
            var slug = "Manual".Equals( slugSource, StringComparison.OrdinalIgnoreCase )
                ? GetAttributeValue( AttributeKey.ManualSlug )
                : PageParameter( "slug" );

            if ( slug.IsNullOrWhiteSpace() )
            {
                return ActionOk( EmptyDetail() );
            }

            // Defense-in-depth slug validation (length + charset), on the
            // canonical lowercase form so mixed-case URLs still resolve.
            slug = LinkListService.NormalizeSlug( slug );
            if ( !LinkListService.IsValidSlug( slug ) )
            {
                return ActionOk( EmptyDetail() );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( slug );
                if ( item == null )
                {
                    return ActionOk( EmptyDetail() );
                }

                // Public viewer: a public list shows to anyone; a non-public list
                // shows only to a user who can EDIT it (in-Rock preview). Everyone
                // else - anonymous or logged-in without edit - gets not-found.
                var currentPerson = RequestContext.CurrentPerson;
                var canEdit = currentPerson != null
                    && item.IsAuthorized( Authorization.EDIT, currentPerson );
                var bag = service.BuildBag( item, currentPerson, requirePublic: !canEdit );
                if ( bag == null )
                {
                    return ActionOk( EmptyDetail() );
                }

                // ROCK-7164: in-Rock page view. Skipped for an editor's
                // preview of a NON-public list; editors viewing public lists
                // still count (accepted small inflation). This is the one
                // place the person is captured - the REST path is anonymous.
                if ( !( canEdit && !bag.IsPublic ) )
                {
                    LinkListInteractionService.RecordView(
                        item.Id,
                        item.Title,
                        pageUrl: RequestContext.RequestUri?.ToString(),
                        userAgent: RequestContext.ClientInformation?.UserAgent,
                        ipAddress: RequestContext.ClientInformation?.IpAddress,
                        personAliasId: currentPerson?.PrimaryAliasId );
                }

                return ActionOk( new LinkListDetailInitializationBox
                {
                    CanEdit = false,
                    CanDelete = false,
                    LinkList = bag
                } );
            }
        }

        private static LinkListDetailInitializationBox EmptyDetail()
        {
            return new LinkListDetailInitializationBox
            {
                CanEdit = false,
                CanDelete = false,
                LinkList = null
            };
        }
    }
}
