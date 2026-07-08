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
using System.Linq;

using org.secc.LinkList.Services;
using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.ViewModels;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.ViewModels.Core.Grid;

namespace org.secc.LinkList.Blocks
{
    /// <summary>
    /// Management landing block (ROCK-8474). Lists the Link Lists the current
    /// person can manage in a Grid, with create / edit / delete that navigate
    /// to the Detail block.
    /// </summary>
    [DisplayName( "Link List List" )]
    [Category( "SECC > Link Lists" )]
    [Description( "Lists the Link Lists the current person can manage, with create, edit, and delete." )]
    [IconCssClass( "fa fa-list" )]
    [SupportedSiteTypes( SiteType.Web )]

    [LinkedPage( "Detail Page",
        Description = "Page used to view, edit, and create a Link List.",
        IsRequired = false,
        Order = 0,
        Key = AttributeKey.DetailPage )]
    [TextField( "Admin Security Role(s)",
        Description = "Comma-delimited list of security role GUIDs whose members can see and edit ALL lists (in addition to Rock Administrators).",
        IsRequired = false,
        Order = 1,
        Key = AttributeKey.AdminSecurityRoles )]

    [Rock.SystemGuid.BlockTypeGuid( LinkListGuids.BlockTypeLinkListList )]
    public class LinkListListBlock : RockBlockType
    {
        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
            public const string AdminSecurityRoles = "AdminSecurityRoles";
        }

        private static class NavigationUrlKey
        {
            public const string DetailPage = "DetailPage";
        }

        /// <inheritdoc/>
        public override string ObsidianFileUrl => "~/Plugins/org_secc/LinkList/linkListList.obs";

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            // Mirror the auth checks the block actions enforce so the UI only
            // offers what will actually succeed: Add requires EDIT on the
            // LinkList channel (SaveList's create path), global settings need
            // ADMINISTRATE on the channel. Delete is per-item (ADMINISTRATE on
            // the item), surfaced per row via the grid's canDelete field; this
            // flag only controls whether the delete column renders at all.
            var canManageGlobal = false;
            var canAdd = false;
            if ( RequestContext.CurrentPerson != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    var channel = new LinkListService( rockContext ).GetChannel();
                    canManageGlobal = channel != null
                        && channel.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );
                    canAdd = channel != null
                        && channel.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
                }
            }

            return new LinkListListInitializationBox
            {
                NavigationUrls = GetBoxNavigationUrls(),
                IsAddEnabled = canAdd,
                IsDeleteEnabled = RequestContext.CurrentPerson != null,
                IsBlockVisible = RequestContext.CurrentPerson != null,
                CanManageGlobalSettings = canManageGlobal
            };
        }

        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            return new Dictionary<string, string>
            {
                [NavigationUrlKey.DetailPage] = this.GetLinkedPageUrl(
                    AttributeKey.DetailPage,
                    new Dictionary<string, string> { { "ListItemKey", "((Key))" } } )
            };
        }

        [BlockAction]
        public BlockActionResult GetGridData()
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var bags = service.QueryListSummaries(
                    RequestContext.CurrentPerson,
                    searchTerm: null,
                    page: 0,
                    pageSize: 500,
                    isAdmin: IsAdmin( rockContext ) );

                var rows = bags
                    .Select( b => new Dictionary<string, object>
                    {
                        ["guid"] = b.Guid,
                        ["title"] = b.Title,
                        ["slug"] = b.Slug,
                        ["isPublic"] = b.IsPublic,
                        ["design"] = b.DesignName,
                        ["modifiedDateTime"] = b.ModifiedDateTime,
                        ["canDelete"] = b.CanDelete
                    } )
                    .ToList();

                return ActionOk( new GridDataBag { Rows = rows } );
            }
        }

        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( !key.AsGuidOrNull().HasValue )
            {
                return ActionBadRequest( "A valid list key is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( key );
                if ( item == null )
                {
                    return ActionNotFound();
                }
                if ( !item.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }

                var itemService = new ContentChannelItemService( rockContext );
                if ( !itemService.CanDelete( item, out var error ) )
                {
                    return ActionBadRequest( error );
                }
                itemService.Delete( item );
                rockContext.SaveChanges();

                return ActionOk();
            }
        }

        [BlockAction]
        public BlockActionResult GetGlobalSettings()
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var channel = service.GetChannel();
                if ( channel == null )
                {
                    return ActionInternalServerError( "Link Lists channel not found." );
                }
                if ( !channel.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }
                return ActionOk( service.GetGlobalSettings() );
            }
        }

        [BlockAction]
        public BlockActionResult SaveGlobalSettings( GlobalSettingsBag bag )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( bag == null )
            {
                return ActionBadRequest( "Global settings payload is required." );
            }
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var channel = service.GetChannel();
                if ( channel == null )
                {
                    return ActionInternalServerError( "Link Lists channel not found." );
                }
                if ( !channel.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }
                service.SaveGlobalSettings( bag );
                return ActionOk();
            }
        }

        // ---- WS13: Design preset management (admin, channel Administrate) ----

        [BlockAction]
        public BlockActionResult GetDesignPresets()
        {
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                if ( !IsChannelAdmin( service ) )
                {
                    return ActionForbidden();
                }
                return ActionOk( service.GetDesignPresets() );
            }
        }

        [BlockAction]
        public BlockActionResult SaveDesignPreset( DesignPresetBag bag )
        {
            if ( bag == null || bag.Name.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "A preset name is required." );
            }
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                if ( !IsChannelAdmin( service ) )
                {
                    return ActionForbidden();
                }
                return ActionOk( service.SaveDesignPreset( bag ) );
            }
        }

        [BlockAction]
        public BlockActionResult DeleteDesignPreset( string value )
        {
            var presetGuid = value.AsGuidOrNull();
            if ( !presetGuid.HasValue )
            {
                return ActionBadRequest( "A valid preset id is required." );
            }
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                if ( !IsChannelAdmin( service ) )
                {
                    return ActionForbidden();
                }
                if ( !service.DeleteDesignPreset( presetGuid.Value, out var error ) )
                {
                    return ActionBadRequest( error );
                }
                return ActionOk();
            }
        }

        // ---- WS13: Allowed Origins management (admin, channel Administrate) ----

        [BlockAction]
        public BlockActionResult GetAllowedOrigins()
        {
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                if ( !IsChannelAdmin( service ) )
                {
                    return ActionForbidden();
                }
                return ActionOk( service.GetAllowedOriginsForAdmin() );
            }
        }

        [BlockAction]
        public BlockActionResult SaveAllowedOrigins( List<string> origins )
        {
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                if ( !IsChannelAdmin( service ) )
                {
                    return ActionForbidden();
                }
                if ( !service.SaveAllowedOrigins( origins, out var error ) )
                {
                    return ActionBadRequest( error );
                }
                return ActionOk();
            }
        }

        /// <summary>
        /// True when the current person has ADMINISTRATE on the LinkList channel
        /// (the gate for every admin-only settings tab).
        /// </summary>
        private bool IsChannelAdmin( LinkListService service )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return false;
            }
            var channel = service.GetChannel();
            return channel != null && channel.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson );
        }

        /// <summary>
        /// True when the current person is a Rock Administrator or a member of
        /// one of the configured Admin Security Role(s).
        /// </summary>
        private bool IsAdmin( RockContext rockContext )
        {
            var person = RequestContext.CurrentPerson;
            if ( person == null )
            {
                return false;
            }

            var roleGuids = new List<Guid>
            {
                Rock.SystemGuid.Group.GROUP_ADMINISTRATORS.AsGuid()
            };

            var configured = GetAttributeValue( AttributeKey.AdminSecurityRoles );
            if ( !configured.IsNullOrWhiteSpace() )
            {
                roleGuids.AddRange( configured
                    .SplitDelimitedValues()
                    .Select( s => s.AsGuidOrNull() )
                    .Where( g => g.HasValue )
                    .Select( g => g.Value ) );
            }

            return new GroupMemberService( rockContext )
                .Queryable()
                .Any( m => m.GroupMemberStatus == GroupMemberStatus.Active
                    && m.PersonId == person.Id
                    && roleGuids.Contains( m.Group.Guid ) );
        }
    }
}
