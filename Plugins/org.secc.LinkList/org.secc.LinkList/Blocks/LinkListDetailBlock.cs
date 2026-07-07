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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using org.secc.LinkList.Services;
using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.Utility;
using org.secc.LinkList.ViewModels;

using Rock;
using Rock.Attribute;
using Rock.Blocks;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace org.secc.LinkList.Blocks
{
    /// <summary>
    /// Detail / edit block (ROCK-8473). Edits a single Link List - including
    /// create mode (Rock's "detail block in add mode" pattern) - its link rows,
    /// theming, and editor (security-group) membership.
    /// </summary>
    [DisplayName( "Link List Detail" )]
    [Category( "SECC > Link Lists" )]
    [Description( "Create or edit a single Link List, its links, and editor access." )]
    [IconCssClass( "fa fa-pen-to-square" )]
    [SupportedSiteTypes( SiteType.Web )]

    [LinkedPage( "List Page",
        Description = "Page to return to after saving or deleting (the management list).",
        IsRequired = false,
        Order = 0,
        Key = AttributeKey.ListPage )]
    [DefinedValueField( LinkListGuids.DefinedTypeLinkListDesign,
        "Default Design",
        "Design preset applied to new lists. Falls back to SECC Default when unset.",
        false, false, "", "", 1,
        AttributeKey.DefaultDesign )]

    [Rock.SystemGuid.BlockTypeGuid( LinkListGuids.BlockTypeLinkListDetail )]
    public class LinkListDetailBlock : RockBlockType
    {
        private static class AttributeKey
        {
            public const string ListPage = "ListPage";
            public const string DefaultDesign = "DefaultDesign";
        }

        private static class PageParameterKey
        {
            public const string ListItemKey = "ListItemKey";
        }

        private static class NavigationUrlKey
        {
            public const string ListPage = "ListPage";
        }

        /// <inheritdoc/>
        public override string ObsidianFileUrl => "~/Plugins/org_secc/LinkList/linkListDetail.obs";

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var key = PageParameter( PageParameterKey.ListItemKey );
            var isAdd = key.IsNullOrWhiteSpace() || key == "0";

            // Default design preset for new lists (block setting), falling back
            // to SECC Default when the setting is unset.
            var defaultDesign = GetAttributeValue( AttributeKey.DefaultDesign ).AsGuidOrNull()
                ?? LinkListGuids.DesignSeccDefault.AsGuid();

            return new LinkListDetailConfigBox
            {
                ItemKey = isAdd ? null : key,
                IsAddMode = isAdd,
                DefaultDesignGuid = defaultDesign.ToString(),
                NavigationUrls = new Dictionary<string, string>
                {
                    [NavigationUrlKey.ListPage] = this.GetLinkedPageUrl( AttributeKey.ListPage, new Dictionary<string, string>() )
                }
            };
        }

        [BlockAction]
        public BlockActionResult GetListDetail( string itemGuid )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( !itemGuid.AsGuidOrNull().HasValue )
            {
                return ActionBadRequest( "A valid item guid is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( itemGuid );
                if ( item == null )
                {
                    return ActionNotFound();
                }
                if ( !item.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }

                var bag = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true );
                if ( bag == null )
                {
                    return ActionNotFound();
                }

                var detail = new LinkListDetailInitializationBox
                {
                    CanEdit = item.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ),
                    CanDelete = item.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson ),
                    LinkList = bag,
                    Designs = service.GetDesignsForPicker()
                };

                return ActionOk( detail );
            }
        }

        [BlockAction]
        public BlockActionResult SaveList( LinkListBag bag )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( bag == null )
            {
                return ActionBadRequest( "Link list payload is required." );
            }
            // Canonicalize the slug (trim + lowercase) before validation so a
            // mixed-case slug typed in the editor is accepted and stored in the
            // same form every other entry point resolves.
            bag.Slug = LinkListService.NormalizeSlug( bag.Slug );
            var validation = ValidateBag( bag );
            if ( validation != null )
            {
                return ActionBadRequest( validation );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var channel = service.GetChannel();
                if ( channel == null )
                {
                    return ActionInternalServerError( "Link Lists channel not found." );
                }

                ContentChannelItem item;
                var itemService = new ContentChannelItemService( rockContext );
                var isNew = false;

                if ( bag.Id.HasValue && bag.Id.Value > 0 )
                {
                    item = itemService.Get( bag.Id.Value );
                    if ( item == null || item.ContentChannelId != channel.Id )
                    {
                        return ActionNotFound();
                    }
                    if ( !item.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                    {
                        return ActionForbidden();
                    }
                }
                else
                {
                    if ( !channel.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                    {
                        return ActionForbidden();
                    }
                    isNew = true;
                    item = new ContentChannelItem
                    {
                        ContentChannelId = channel.Id,
                        ContentChannelTypeId = channel.ContentChannelTypeId,
                        Status = ContentChannelItemStatus.Approved,
                        StartDateTime = RockDateTime.Now
                    };
                    itemService.Add( item );
                }

                item.Title = bag.Title.Trim();
                // Intro content lives in the native Content field (legacy parity).
                item.Content = bag.IntroContent ?? string.Empty;

                // NOTE: intentionally NOT wrapped in rockContext.WrapTransaction. Rock
                // helpers used below (ContentChannelItemSlugService.SaveSlug and the
                // attribute-value save) open their OWN RockContext/connection, which then
                // blocks on rows an outer transaction has written but not yet committed -
                // a self-deadlock that surfaces as "The wait operation timed out." So these
                // writes commit individually (as Rock's own detail blocks do). The matrix
                // rebuild stays atomic on its own: PersistMatrixItems wraps its
                // delete+upsert in a transaction. The slug catch below rethrows only to
                // surface a clearer message, not to roll back.
                try
                {
                    rockContext.SaveChanges();

                    // Persist top-level (non-matrix) attribute values.
                    item.LoadAttributes( rockContext );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.ContentTextColor, bag.ContentTextColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.BackgroundColor, bag.BackgroundColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.ButtonColor, bag.ButtonColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.ButtonTextColor, bag.ButtonTextColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.FeaturedButtonColor, bag.FeaturedButtonColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.FeaturedButtonTextColor, bag.FeaturedButtonTextColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.TitleColor, bag.TitleColor );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.FooterContent, bag.FooterContent );

                    // Legacy display-parity attributes (WS2.5).
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.CustomTitle, bag.CustomTitle );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.HideTitle, bag.HideTitle.ToString() );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.HeaderImage, bag.HeaderImage );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.RoundHeaderImage, bag.RoundHeaderImage.ToString() );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.BackgroundImage, bag.BackgroundImage );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.HeaderBackgroundImage, bag.HeaderBackgroundImage );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.HeaderVideo, bag.HeaderVideo );
                    SetIfPresent( item, LinkListGuids.TypeAttributeKey.HeaderVideoVimeoId, bag.HeaderVideoVimeoId );

                    SetIfPresent( item, LinkListGuids.ItemAttributeKey.IsPublic, bag.IsPublic.ToString() );
                    SetIfPresent( item, LinkListGuids.ItemAttributeKey.DesignId,
                        bag.DesignId.HasValue ? bag.DesignId.Value.ToString() : string.Empty );
                    item.SaveAttributeValues( rockContext );

                    // The Obsidian ImageUploader uploads header/background images as
                    // TEMPORARY BinaryFiles. Now that they're referenced by saved
                    // attribute values, mark them permanent so Rock's cleanup job
                    // doesn't delete them. (No-op when the value is blank/unchanged.)
                    PersistBinaryFile( rockContext, bag.HeaderImage );
                    PersistBinaryFile( rockContext, bag.BackgroundImage );
                    PersistBinaryFile( rockContext, bag.HeaderBackgroundImage );
                    rockContext.SaveChanges();

                    // Slug upsert (uniqueness within the channel is enforced by Rock).
                    if ( !bag.Slug.IsNullOrWhiteSpace() )
                    {
                        var existingSlug = item.ContentChannelItemSlugs?
                            .OrderBy( s => s.Id )
                            .FirstOrDefault();
                        var slugService = new ContentChannelItemSlugService( rockContext );
                        try
                        {
                            slugService.SaveSlug( item.Id, channel.Id, bag.Slug, existingSlug?.Id );
                        }
                        catch ( Exception ex )
                        {
                            // Rethrow to roll back the transaction; surfaced below.
                            throw new InvalidOperationException( "Slug error: " + ex.Message, ex );
                        }
                    }

                    // Persist link rows: upserts by row Guid, deletes rows missing
                    // from the incoming list, and reorders by list position.
                    service.PersistMatrixItems( item, bag.Items );
                }
                catch ( Exception ex )
                {
                    // Full exception (including the innermost SqlException EF wraps)
                    // goes to Rock's exception log; the client gets a generic message
                    // so database/schema details never leak to the browser.
                    ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                    return ActionBadRequest( "Save failed. The error has been logged; contact an administrator if it persists." );
                }

                // Auto-create the list-specific security group on first save (mirrors the
                // legacy creation workflow). Done AFTER the data transaction commits:
                // Rock's Authorization plumbing opens its own DB connection, which blocks
                // on the still-uncommitted rows if run inside the transaction (SQL command
                // timeout). Idempotent, so a retry is safe.
                if ( isNew )
                {
                    try
                    {
                        service.EnsureSecurityGroup( item, RequestContext.CurrentPerson );
                    }
                    catch ( Exception ex )
                    {
                        // The list itself is saved; only the edit-access group failed.
                        // Log it and tell the user to re-save to (idempotently) create it.
                        ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                        return ActionBadRequest(
                            "The list was saved, but setting up edit access failed. "
                            + "The error has been logged. Re-open the list and save again to finish access setup." );
                    }
                }
                else
                {
                    // Keep the security group's name aligned with the (possibly
                    // changed) title. Non-fatal: a failed rename must not fail the save.
                    try
                    {
                        service.RenameSecurityGroup( item, item.Title );
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                    }
                }

                var saved = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true );
                return ActionOk( saved );
            }
        }

        [BlockAction]
        public BlockActionResult DeleteList( string itemGuid )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( !itemGuid.AsGuidOrNull().HasValue )
            {
                return ActionBadRequest( "A valid item guid is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( itemGuid );
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
        public BlockActionResult GetDesigns()
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                return ActionOk( service.GetDesignsForPicker() );
            }
        }

        /// <summary>
        /// ROCK-7164: analytics for one list over a clamped range (30/90/365
        /// days) - totals, gap-filled views/clicks per day, and per-link click
        /// counts labeled from the CURRENT matrix rows (falling back to the
        /// interaction's recorded summary/data when the row was deleted).
        /// Read-only: never creates the interaction component; no recorded
        /// activity returns an empty bag (UI shows "No activity yet").
        /// </summary>
        [BlockAction]
        public BlockActionResult GetAnalytics( string itemGuid, int days )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            var listGuid = itemGuid.AsGuidOrNull();
            if ( !listGuid.HasValue )
            {
                return ActionBadRequest( "A valid list guid is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( listGuid.Value.ToString() );
                if ( item == null )
                {
                    return ActionNotFound();
                }
                if ( !item.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }

                var clampedDays = AnalyticsSeries.ClampDays( days );
                var endDate = RockDateTime.Now.Date;
                var startDate = endDate.AddDays( -( clampedDays - 1 ) );
                var bag = new LinkListAnalyticsBag { Days = clampedDays };

                var channelId = LinkListInteractionService.GetChannelId();
                var componentId = !channelId.HasValue
                    ? null
                    : new InteractionComponentService( rockContext )
                        .Queryable()
                        .Where( c => c.InteractionChannelId == channelId.Value && c.EntityId == item.Id )
                        .Select( c => ( int? ) c.Id )
                        .FirstOrDefault();

                if ( !componentId.HasValue )
                {
                    // Nothing recorded yet (or migration missing): empty series
                    // so the chart still renders a flat zero line.
                    bag.ViewsByDay = AnalyticsSeries.FillDailySeries( null, startDate, endDate );
                    bag.ClicksByDay = AnalyticsSeries.FillDailySeries( null, startDate, endDate );
                    return ActionOk( bag );
                }

                var interactions = new InteractionService( rockContext )
                    .Queryable()
                    .AsNoTracking()
                    .Where( i => i.InteractionComponentId == componentId.Value
                        && i.InteractionDateTime >= startDate );

                // One grouped query per day-series (SQL-side day truncation).
                var viewCounts = interactions
                    .Where( i => i.Operation == "View" )
                    .GroupBy( i => DbFunctions.TruncateTime( i.InteractionDateTime ) )
                    .Select( g => new { Day = g.Key, Count = g.Count() } )
                    .ToList()
                    .Where( g => g.Day.HasValue )
                    .ToDictionary( g => g.Day.Value, g => g.Count );

                var clickCounts = interactions
                    .Where( i => i.Operation == "Click" )
                    .GroupBy( i => DbFunctions.TruncateTime( i.InteractionDateTime ) )
                    .Select( g => new { Day = g.Key, Count = g.Count() } )
                    .ToList()
                    .Where( g => g.Day.HasValue )
                    .ToDictionary( g => g.Day.Value, g => g.Count );

                bag.ViewsByDay = AnalyticsSeries.FillDailySeries( viewCounts, startDate, endDate );
                bag.ClicksByDay = AnalyticsSeries.FillDailySeries( clickCounts, startDate, endDate );
                bag.TotalViews = viewCounts.Values.Sum();
                bag.TotalClicks = clickCounts.Values.Sum();

                // Per-link clicks grouped by the matrix row id, with the most
                // recent recorded summary/data as the label fallback for rows
                // that have since been deleted.
                var perLink = interactions
                    .Where( i => i.Operation == "Click" && i.EntityId.HasValue )
                    .GroupBy( i => i.EntityId.Value )
                    .Select( g => new
                    {
                        MatrixItemId = g.Key,
                        Clicks = g.Count(),
                        LastSummary = g.OrderByDescending( i => i.InteractionDateTime ).Select( i => i.InteractionSummary ).FirstOrDefault(),
                        LastData = g.OrderByDescending( i => i.InteractionDateTime ).Select( i => i.InteractionData ).FirstOrDefault()
                    } )
                    .ToList();

                if ( perLink.Count > 0 )
                {
                    var idToGuid = service.GetMatrixRowIdMap( item );
                    var currentRows = service.GetLinkRows( item )
                        .Where( r => !r.Guid.IsNullOrWhiteSpace() )
                        .ToDictionary( r => r.Guid.AsGuid(), r => r );

                    bag.Links = perLink
                        .Select( p =>
                        {
                            var rowGuid = idToGuid.TryGetValue( p.MatrixItemId, out var g ) ? ( Guid? ) g : null;
                            var current = rowGuid.HasValue && currentRows.TryGetValue( rowGuid.Value, out var row ) ? row : null;
                            return new LinkClickCountBag
                            {
                                MatrixItemGuid = rowGuid?.ToString(),
                                Text = current?.Text ?? p.LastSummary,
                                Url = current?.Url ?? p.LastData,
                                Clicks = p.Clicks,
                                IsDeleted = current == null
                            };
                        } )
                        .OrderByDescending( l => l.Clicks )
                        .ToList();
                }

                return ActionOk( bag );
            }
        }

        [BlockAction]
        public BlockActionResult AddMember( string itemGuid, string personGuid )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            var listGuid = itemGuid.AsGuidOrNull();
            var pGuid = personGuid.AsGuidOrNull();
            if ( !listGuid.HasValue || !pGuid.HasValue )
            {
                return ActionBadRequest( "A valid list guid and person guid are required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( listGuid.Value.ToString() );
                if ( item == null )
                {
                    return ActionNotFound();
                }
                if ( !item.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }

                var bag = service.AddMember( item, pGuid.Value, RequestContext.CurrentPerson );
                if ( bag == null )
                {
                    return ActionBadRequest( "Person not found." );
                }
                return ActionOk( bag );
            }
        }

        [BlockAction]
        public BlockActionResult RemoveMember( string itemGuid, int groupMemberId )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            var listGuid = itemGuid.AsGuidOrNull();
            if ( !listGuid.HasValue || groupMemberId <= 0 )
            {
                return ActionBadRequest( "A valid list guid and group member id are required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var service = new LinkListService( rockContext );
                var item = service.ResolveItem( listGuid.Value.ToString() );
                if ( item == null )
                {
                    return ActionNotFound();
                }
                if ( !item.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                {
                    return ActionForbidden();
                }

                if ( !service.RemoveMember( item, groupMemberId, out var error ) )
                {
                    return ActionBadRequest( error );
                }
                return ActionOk();
            }
        }

        private static string ValidateBag( LinkListBag bag )
        {
            if ( bag.Title.IsNullOrWhiteSpace() || bag.Title.Trim().Length < 1 || bag.Title.Trim().Length > 250 )
            {
                return "Title is required and must be 1-250 characters.";
            }
            if ( !bag.Slug.IsNullOrWhiteSpace() && !LinkListService.IsValidSlug( bag.Slug ) )
            {
                return "Slug must be 1-200 chars of letters, digits, or dashes.";
            }
            return null;
        }

        private static void SetIfPresent( ContentChannelItem item, string key, string value )
        {
            if ( item.Attributes != null && item.Attributes.ContainsKey( key ) )
            {
                item.SetAttributeValue( key, value ?? string.Empty );
            }
        }

        /// <summary>
        /// Marks the BinaryFile referenced by an Image attribute value (a file
        /// GUID) as non-temporary so Rock's cleanup job keeps it. Safe to call
        /// with a blank/non-GUID value (no-op) and idempotent on re-save.
        /// </summary>
        private static void PersistBinaryFile( RockContext rockContext, string fileGuidValue )
        {
            var fileGuid = fileGuidValue.AsGuidOrNull();
            if ( !fileGuid.HasValue )
            {
                return;
            }

            var binaryFile = new BinaryFileService( rockContext ).Get( fileGuid.Value );
            if ( binaryFile != null && binaryFile.IsTemporary )
            {
                binaryFile.IsTemporary = false;
            }
        }
    }
}
