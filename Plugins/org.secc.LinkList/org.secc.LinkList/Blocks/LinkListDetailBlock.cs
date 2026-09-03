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

                var bag = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true, includeSlugs: true );
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

        /// <summary>
        /// Saves a list. <paramref name="loadedSlugIds"/> is the set of slug row ids the
        /// editor had on screen when it loaded, which is what lets a save DELETE a slug
        /// without clobbering one another editor added in the meantime - see
        /// <see cref="SlugReconciler.FilterDeletableIds"/>. It is request-scoped intent
        /// rather than list state, so it is a separate parameter instead of a bag field
        /// (the bag doubles as the read model serialized to anonymous callers). Null when
        /// the caller doesn't track it.
        /// </summary>
        [BlockAction]
        public BlockActionResult SaveList( LinkListBag bag, List<int> loadedSlugIds = null )
        {
            if ( RequestContext.CurrentPerson == null )
            {
                return ActionForbidden( "Authentication is required." );
            }
            if ( bag == null )
            {
                return ActionBadRequest( "Link list payload is required." );
            }
            // Slugs are canonicalized inside BuildSlugSubmission (via
            // SlugReconciler.BuildSubmission -> LinkListService.CanonicalizeSlug) to
            // exactly the text Rock will store, so what ValidateBag checks, what the
            // conflict check queries, and what every entry point resolves all match.
            // Built once here and threaded through the whole save.
            var slugSubmission = BuildSlugSubmission( bag );
            var validation = ValidateBag( bag, slugSubmission );
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

                    // Optimistic concurrency for the item ROW: reject a save whose view
                    // of the list predates someone else's, so a stale tab can't silently
                    // revert their title or intro content.
                    //
                    // Scope, precisely: Rock stamps ModifiedDateTime only when the item
                    // row itself is Added/Modified. Slugs, colours and link rows live in
                    // other tables and never touch it, so this check CANNOT see those
                    // edits - the slug case is handled instead by restricting deletions
                    // to rows the editor actually loaded (see loadedSlugIds below).
                    // A null value means a client that doesn't round-trip it: left alone.
                    // Tolerance: SQL datetime resolution and the JSON round-trip don't
                    // preserve exact ticks.
                    if ( bag.ModifiedDateTime.HasValue && item.ModifiedDateTime.HasValue
                        && Math.Abs( ( item.ModifiedDateTime.Value - bag.ModifiedDateTime.Value ).TotalSeconds ) > 1 )
                    {
                        return ActionBadRequest(
                            "This list was changed by someone else since you opened it. "
                            + "Reload the list and make your changes again." );
                    }
                }
                else
                {
                    // Create needs no channel permission (ROCK-9100). Any signed-in
                    // person who can reach this block may create a list - Rock already
                    // requires VIEW on the page and block before a block action runs -
                    // and EnsureSecurityGroup below makes the creator its editor.
                    // Channel EDIT is deliberately NOT the gate: it cascades to every
                    // item, so granting it to staff would open every list to everyone.
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

                // Discard submitted slugs whose rows another editor deleted while this
                // client had the list open, so saving a stale screen can't resurrect them.
                //
                // This has to happen BEFORE the conflict check below: if the other editor
                // moved that slug to a different list, the stale submission would collide
                // with it and the save would be refused over a slug this user never
                // touched and that is about to be discarded anyway. (The conflict check
                // itself must stay here, ahead of every write - moving it later would
                // commit the title and attributes and only then refuse.)
                //
                // Deliberately a SECOND, cheap read: the authoritative slug query lower
                // down has to stay there, because for a new list Rock's save hook mints a
                // title-derived row during the first SaveChanges that this read can't see.
                // The gap between the two reads is harmless - this filter is best-effort
                // by nature.
                var droppedSlugs = new List<string>();
                if ( !isNew )
                {
                    var currentRows = new ContentChannelItemSlugService( rockContext ).Queryable()
                        .Where( s => s.ContentChannelItemId == item.Id )
                        .Select( s => new { s.Id, s.Slug } )
                        .ToList()
                        .Select( s => new ExistingSlug { Id = s.Id, Slug = s.Slug } );

                    var filtered = SlugReconciler.FilterConcurrentlyDeleted( slugSubmission.Slugs, currentRows );
                    if ( filtered.DroppedSlugs.Count > 0 && filtered.Kept.Count == 0 )
                    {
                        // Every slug this client knew about is gone. There is nothing left
                        // to reconcile against, and proceeding would either wipe the list's
                        // current slugs or invent one from the title, so stop and say so.
                        return ActionBadRequest(
                            "This list's slugs were changed by someone else since you opened it. "
                            + "Reload the list and make your changes again." );
                    }
                    // REPLACE the submission: the delete pass keys off Slugs.Count, so a
                    // filtered copy kept on the side would leave deletes enabled against a
                    // set that no longer justifies them.
                    slugSubmission.Slugs = filtered.Kept;
                    droppedSlugs = filtered.DroppedSlugs;
                }

                // Reject any slug already used by ANOTHER list in the channel with a
                // clear, named error - Rock's SaveSlug would silently suffix instead.
                // item.Id is 0 for a new (unsaved) item, which excludes nothing here.
                // Wrapped because this runs BEFORE the save's own try: the query
                // parameterizes one value per slug, so without the count cap in
                // ValidateBag an oversized payload would surface as a raw 500.
                string conflictingSlug;
                try
                {
                    conflictingSlug = service.FindConflictingSlug(
                        slugSubmission.Slugs.Select( s => s.Slug ), item.Id );
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                    return ActionBadRequest( "Could not check the slugs for conflicts. The error has been logged." );
                }
                if ( conflictingSlug != null )
                {
                    return ActionBadRequest(
                        $"The slug '{conflictingSlug}' is already used by another list. Choose a different slug." );
                }

                item.Title = bag.Title.Trim();
                // Intro content lives in the native Content field (legacy parity).
                item.Content = bag.IntroContent ?? string.Empty;

                // Notes about slugs this save merged rather than overwrote - a row another
                // editor added and this save kept, or one they deleted and this save did
                // not resurrect. Reported together on the response so the user isn't
                // surprised by a slug list that doesn't match what was on their screen.
                var concurrentSlugNotes = new List<string>();
                if ( droppedSlugs.Count > 0 )
                {
                    concurrentSlugNotes.Add( droppedSlugs.Count == 1
                        ? $"Another editor deleted the slug '{droppedSlugs[0]}' while you had this list open, so it was not restored."
                        : $"Another editor deleted these slugs while you had this list open, so they were not restored: {string.Join( ", ", droppedSlugs )}." );
                }

                // NOTE: intentionally NOT wrapped in rockContext.WrapTransaction. Rock
                // helpers used below (ContentChannelItemSlugService.SaveSlug and the
                // attribute-value save) open their OWN RockContext/connection, which then
                // blocks on rows an outer transaction has written but not yet committed -
                // a self-deadlock that surfaces as "The wait operation timed out." So these
                // writes commit individually (as Rock's own detail blocks do). The matrix
                // rebuild stays atomic on its own: PersistMatrixItems wraps its
                // delete+upsert in a transaction.
                //
                // Because writes commit as they go, ORDER MATTERS in the slug section
                // below: it must never delete a slug row until every add has succeeded,
                // or a later failure leaves a list with dead URLs and no way back.
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

                    // Multi-slug reconciliation. Only touch slugs when the client
                    // sent slug data - a legacy client that sent neither a Slugs
                    // array nor a scalar slug leaves the list's slugs untouched.
                    var slugService = new ContentChannelItemSlugService( rockContext );
                    var slugRowsToDelete = new List<ContentChannelItemSlug>();
                    if ( slugSubmission.FullReconcile || slugSubmission.Slugs.Count > 0 )
                    {
                        // Load the item's slug rows EXPLICITLY (a query, not the lazy
                        // nav) so the diff sees the committed set - which for a NEW list
                        // already includes the slug Rock derived from the title: its save
                        // hook mints one for an item with no slug rows, and the
                        // SaveChanges above is not wrapped in a transaction, so it has
                        // already run. That hook does NOT fire for an existing item whose
                        // own row didn't change (Rock skips hooks for an Unchanged
                        // entity), which is why the title slug is also derived explicitly
                        // below rather than assumed.
                        // Tracked entities (not a projection) so this one query serves
                        // the diff, the primary flag and the deletes alike.
                        // No WrapTransaction here (same deadlock reason as above):
                        // SaveSlug opens/commits on its own as it goes.
                        var slugRows = slugService.Queryable()
                            .Where( s => s.ContentChannelItemId == item.Id )
                            .ToList();

                        var slugPlan = SlugReconciler.Reconcile(
                            slugRows.Select( s => new ExistingSlug { Id = s.Id, Slug = s.Slug } ),
                            slugSubmission.Slugs );
                        if ( !slugPlan.IsValid )
                        {
                            // ValidateBag already ran, so this is an invariant breach
                            // rather than user error - but it is still the user's set,
                            // so name the problem instead of logging "Save failed".
                            return ActionBadRequest( slugPlan.Error );
                        }

                        // Add new slugs. SaveSlug persists each row and RETURNS it,
                        // which is the only reliable way to learn the STORED text: on
                        // a collision that appeared after FindConflictingSlug ran it
                        // silently suffixes "-1", and it returns null without writing
                        // anything when Rock reduces the text to nothing.
                        var rowIdBySlug = new Dictionary<string, int>( StringComparer.OrdinalIgnoreCase );
                        foreach ( var slug in slugPlan.SlugsToAdd )
                        {
                            var savedRow = slugService.SaveSlug( item.Id, channel.Id, slug, null );
                            if ( savedRow == null )
                            {
                                // Shouldn't happen: CanonicalizeSlug output is a fixed
                                // point of Rock's MakeSlugValid, so Rock can't reduce it
                                // to nothing. Bail out before anything is deleted so the
                                // list keeps the slugs it still has. Adds that already
                                // committed stay; a retry is idempotent because the
                                // reconcile diffs by slug text.
                                throw new InvalidOperationException(
                                    $"Rock stored no row for slug '{slug}'; aborted before deleting any slugs." );
                            }
                            rowIdBySlug[slug] = savedRow.Id;
                            slugRows.Add( savedRow );
                        }

                        // Deletions are DEFERRED until after the matrix rebuild (see
                        // below): removing a slug kills a public URL for good, so it
                        // must be the last thing this save does. Only a slug-aware
                        // client that sent a non-empty set may delete - a legacy
                        // scalar-only client upserts without deleting, and an empty set
                        // means "leave the slugs to Rock" (see ValidateBag).
                        var mayDelete = slugSubmission.FullReconcile && slugSubmission.Slugs.Count > 0;
                        if ( mayDelete )
                        {
                            // Restricted to rows the editor actually loaded, so a slug
                            // another editor added since then is kept rather than read as
                            // one this user removed. Deliberate removal is unaffected.
                            var deletableIds = SlugReconciler.FilterDeletableIds(
                                slugPlan.SlugIdsToDelete, loadedSlugIds );
                            slugRowsToDelete = slugRows
                                .Where( s => deletableIds.Contains( s.Id ) )
                                .ToList();

                            // Tell the user when that actually spared something, so
                            // neither editor is surprised by a slug they didn't add.
                            var sparedSlugs = slugRows
                                .Where( s => slugPlan.SlugIdsToDelete.Contains( s.Id )
                                    && !deletableIds.Contains( s.Id ) )
                                .Select( s => s.Slug )
                                .ToList();
                            if ( sparedSlugs.Count > 0 )
                            {
                                concurrentSlugNotes.Add( sparedSlugs.Count == 1
                                    ? $"Another editor added the slug '{sparedSlugs[0]}' while you had this list open. It was kept."
                                    : $"Another editor added these slugs while you had this list open, and they were kept: {string.Join( ", ", sparedSlugs )}." );
                            }
                        }

                        if ( slugRows.Count == 0 )
                        {
                            // Derive a slug from the title, the way Rock's own content
                            // channel item block does. Rock's save hook does this too,
                            // but ONLY when the item row itself changed - hooks are
                            // skipped for an Unchanged entity - so a slug-only or no-op
                            // save on a slug-less list would otherwise leave it
                            // unreachable. Doing it here makes it deterministic.
                            var derived = slugService.SaveSlug( item.Id, channel.Id, item.Title, null );
                            if ( derived != null )
                            {
                                slugRows.Add( derived );
                            }
                        }

                        if ( slugRows.Count == 0 )
                        {
                            // Unreachable: ValidateBag rejects an empty submitted set
                            // whose title yields no slug, so either a slug was added
                            // above or one was just derived. A list with no slug is
                            // unreachable by the viewer, web component and REST, so say
                            // so rather than reporting a clean save.
                            throw new InvalidOperationException(
                                "The list has no slug and none could be derived from the title. Add a slug and save again." );
                        }

                        // Match the primary row by ID, never by text: SaveSlug may have
                        // stored a "-1"-suffixed variant of what was submitted, and a
                        // text comparison would then match nothing and silently clear
                        // every primary flag. Rows the reconcile kept map to themselves.
                        int? primaryRowId = null;
                        if ( slugPlan.PrimarySlug != null )
                        {
                            primaryRowId = rowIdBySlug.TryGetValue( slugPlan.PrimarySlug, out var addedRowId )
                                ? addedRowId
                                : slugRows
                                    .Where( r => string.Equals( r.Slug, slugPlan.PrimarySlug, StringComparison.OrdinalIgnoreCase ) )
                                    .Select( r => ( int? ) r.Id )
                                    .FirstOrDefault();
                        }
                        if ( primaryRowId == null )
                        {
                            // Nothing was submitted (Rock derived the slug from the
                            // title), so keep the list's existing primary if it has one
                            // and otherwise promote its oldest row - a list with slugs
                            // always ends up with exactly one primary. Rows queued for
                            // deletion are never candidates.
                            var doomedIds = slugRowsToDelete.Select( r => r.Id ).ToList();
                            var survivors = slugRows.Where( r => !doomedIds.Contains( r.Id ) ).ToList();
                            primaryRowId = survivors.FirstOrDefault( r => r.IsPrimary )?.Id
                                ?? survivors.OrderBy( r => r.Id ).First().Id;
                        }

                        var primaryChanged = false;
                        foreach ( var row in slugRows )
                        {
                            var shouldBePrimary = row.Id == primaryRowId.Value;
                            if ( row.IsPrimary != shouldBePrimary )
                            {
                                row.IsPrimary = shouldBePrimary;
                                primaryChanged = true;
                            }
                        }
                        if ( primaryChanged )
                        {
                            rockContext.SaveChanges();
                        }
                    }

                    // Persist link rows: upserts by row Guid, deletes rows missing
                    // from the incoming list, and reorders by list position.
                    service.PersistMatrixItems( item, bag.Items );

                    // Slug deletions go LAST. Everything above commits as it goes, so a
                    // failure anywhere earlier used to leave a slug's URL permanently
                    // dead while the user was told the save failed. Deferring them means
                    // the worst case is a slug that outlives its removal - recoverable by
                    // saving again - instead of a URL that can never be recovered.
                    if ( slugRowsToDelete.Count > 0 )
                    {
                        foreach ( var row in slugRowsToDelete )
                        {
                            slugService.Delete( row );
                        }
                        rockContext.SaveChanges();
                    }
                }
                catch ( Exception ex )
                {
                    // Full exception (including the innermost SqlException EF wraps)
                    // goes to Rock's exception log; the client gets a generic message
                    // so database/schema details never leak to the browser.
                    ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );

                    // The writes above commit as they go, so the list itself may already
                    // exist. Hand the saved bag back with a warning rather than a bare
                    // error: otherwise a new list's editor keeps a null id, the retry
                    // looks like another new list, and it is rejected forever for
                    // conflicting with the slugs the failed attempt just created.
                    if ( item.Id > 0 )
                    {
                        var partial = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true, includeSlugs: true );
                        partial.SaveWarning = "Some of your changes could not be saved. "
                            + "The error has been logged; review the list and save again.";
                        return ActionOk( partial );
                    }
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
                        // Return the saved bag with a warning (not a bare error) so the
                        // editor keeps the new list's identity - re-saving is idempotent
                        // and finishes the setup, whereas a retry that looked like a new
                        // list would collide with the slugs this attempt already created.
                        ExceptionLogService.LogException( ex, System.Web.HttpContext.Current );
                        var partial = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true, includeSlugs: true );
                        partial.SaveWarning = "The list was saved, but setting up edit access failed. "
                            + "The error has been logged. Save again to finish access setup.";
                        return ActionOk( partial );
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

                var saved = service.BuildBag( item, RequestContext.CurrentPerson, requirePublic: false, includeMembers: true, includeSlugs: true );
                saved.SaveNotice = concurrentSlugNotes.Count > 0
                    ? string.Join( " ", concurrentSlugNotes )
                    : null;
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

        private static string ValidateBag( LinkListBag bag, SlugSubmission submission )
        {
            if ( bag.Title.IsNullOrWhiteSpace() || bag.Title.Trim().Length < 1 || bag.Title.Trim().Length > 250 )
            {
                return "Title is required and must be 1-250 characters.";
            }

            // Validate EVERY submitted slug (charset), reject duplicates within the
            // submitted set, cap the count, and require the title to yield a slug when
            // the set is empty. Channel-wide uniqueness is checked separately (it needs
            // a DB query). Slug rules live in LinkListService/SlugReconciler - the
            // single source of truth.
            return SlugReconciler.ValidateSubmitted( submission.Slugs )
                ?? SlugReconciler.ValidateSubmissionSize( submission )
                ?? SlugReconciler.ValidateSubmissionAgainstTitle( submission, bag.Title );
        }

        /// <summary>
        /// The slug set this save should apply. When the client sent a Slugs
        /// collection (slug-aware editor) it drives a delete-capable reconcile;
        /// when the collection is omitted (null - a legacy scalar-only client) it
        /// becomes an upsert-only submission seeded from the scalar
        /// <see cref="LinkListBag.Slug"/> that never deletes other slugs. All
        /// canonicalization, blank-dropping and de-duplication happens in
        /// <see cref="SlugReconciler.BuildSubmission"/>.
        /// </summary>
        private static SlugSubmission BuildSlugSubmission( LinkListBag bag )
        {
            // Map the bag's slugs to the helper's POCO, preserving null (omitted)
            // vs non-null (provided) so BuildSubmission can tell them apart. Null
            // ELEMENTS are dropped first - a hand-rolled "slugs":[null] payload
            // would otherwise throw here, outside the save's try/catch.
            var provided = bag.Slugs?
                .Where( s => s != null )
                .Select( s => new SubmittedSlug
                {
                    Slug = s.Slug,
                    IsPrimary = s.IsPrimary,
                    // Carried for concurrency detection only - see SubmittedSlug.ClientRowId.
                    // 0 means the user typed this slug in the editor.
                    ClientRowId = s.Id
                } )
                .ToList();
            return SlugReconciler.BuildSubmission( provided, bag.Slug );
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
