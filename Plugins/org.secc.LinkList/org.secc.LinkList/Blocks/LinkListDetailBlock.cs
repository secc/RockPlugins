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
                            slugService.SaveSlug( item.Id, channel.Id, bag.Slug.Trim(), existingSlug?.Id );
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
                    // Surface the innermost cause (EF wraps the real SqlException in a
                    // generic "error executing the command definition" message), and log
                    // it so it lands in Rock's exception log for diagnosis.
                    var baseEx = ex.GetBaseException();
                    ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                    return ActionBadRequest( "Save failed: " + baseEx.Message );
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
                            "The list was saved, but setting up edit access failed: "
                            + ex.GetBaseException().Message
                            + " Re-open the list and save again to finish access setup." );
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
            if ( !bag.Slug.IsNullOrWhiteSpace() )
            {
                var slug = bag.Slug.Trim();
                if ( slug.Length > 200 || !System.Text.RegularExpressions.Regex.IsMatch( slug, "^[a-z0-9-]+$" ) )
                {
                    return "Slug must be 1-200 chars of lowercase letters, digits, or dashes.";
                }
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
