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

using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.Utility;
using org.secc.LinkList.ViewModels;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.LinkList.Services
{
    /// <summary>
    /// Single source of truth for Link List data access. Used by Obsidian blocks
    /// and the public REST controller.
    /// </summary>
    public class LinkListService
    {
        private readonly RockContext _rockContext;

        public LinkListService( RockContext rockContext )
        {
            _rockContext = rockContext ?? throw new ArgumentNullException( nameof( rockContext ) );
        }

        // ---------------------------------------------------------------------
        // CORS allowed origins (admin-managed Defined Type + hardcoded fallback)
        // ---------------------------------------------------------------------

        private static readonly System.Text.RegularExpressions.Regex OriginPattern =
            new System.Text.RegularExpressions.Regex( @"^https?://[a-zA-Z0-9.-]+(:\d+)?$", System.Text.RegularExpressions.RegexOptions.Compiled );

        /// <summary>
        /// Origins allowed by the public REST endpoint's CORS reflection: the
        /// admin-managed "Link List Allowed Origins" Defined Type (read from
        /// cache, not a DB hit per request) unioned with the hardcoded
        /// bootstrap/fallback array. Defined Type entries must look like an
        /// origin (scheme://host[:port], no path) or are ignored.
        /// </summary>
        public static HashSet<string> GetAllowedOrigins()
        {
            var origins = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

            // Bootstrap / fallback (always trusted, so CORS keeps working even
            // before the Defined Type migration runs or if it is removed).
            foreach ( var o in LinkListGuids.AllowedOrigins )
            {
                origins.Add( o );
            }

            // Admin-managed Defined Type (cached; validated).
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListAllowedOrigins.AsGuid() );
            if ( definedType != null )
            {
                foreach ( var value in definedType.DefinedValues.Where( v => v.IsActive ).Select( v => v.Value ) )
                {
                    var origin = value?.Trim();
                    if ( IsValidOrigin( origin ) )
                    {
                        origins.Add( origin );
                    }
                }
            }

            return origins;
        }

        /// <summary>
        /// True when the value looks like a CORS origin: scheme://host[:port],
        /// no path, query, or trailing slash.
        /// </summary>
        public static bool IsValidOrigin( string value )
        {
            return !string.IsNullOrWhiteSpace( value ) && OriginPattern.IsMatch( value.Trim() );
        }

        // ---------------------------------------------------------------------
        // Slug normalization / validation (single source of truth)
        // ---------------------------------------------------------------------

        // Slugs are lowercase-only: Rock's SaveSlug/MakeSlugValid lowercases on
        // write, so stored slugs never contain uppercase. Every entry point
        // (editor validation, viewer, public REST) normalizes through
        // NormalizeSlug and validates with IsValidSlug so mixed-case input in a
        // URL still resolves while the canonical form stays consistent.
        private static readonly System.Text.RegularExpressions.Regex SlugPattern =
            new System.Text.RegularExpressions.Regex( @"^[a-z0-9-]+$", System.Text.RegularExpressions.RegexOptions.Compiled );

        public const int MaxSlugLength = 200;

        /// <summary>Canonical slug form: trimmed and lowercased (null stays null).</summary>
        public static string NormalizeSlug( string slug )
        {
            return slug?.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// True when the value is a valid canonical slug: 1-200 chars of
        /// lowercase letters, digits, or dashes. Callers should pass the
        /// <see cref="NormalizeSlug"/> form.
        /// </summary>
        public static bool IsValidSlug( string slug )
        {
            return !string.IsNullOrWhiteSpace( slug )
                && slug.Length <= MaxSlugLength
                && SlugPattern.IsMatch( slug );
        }

        // ---------------------------------------------------------------------
        // Channel / template lookups
        // ---------------------------------------------------------------------

        public ContentChannel GetChannel()
        {
            var channelGuid = LinkListGuids.LinkListChannel.AsGuid();
            return new ContentChannelService( _rockContext )
                .Queryable( "ContentChannelType" )
                .FirstOrDefault( c => c.Guid == channelGuid );
        }

        public AttributeMatrixTemplate GetMatrixTemplate()
        {
            var templateGuid = LinkListGuids.LinkListMatrixTemplate.AsGuid();
            return new AttributeMatrixTemplateService( _rockContext )
                .Queryable()
                .FirstOrDefault( t => t.Guid == templateGuid );
        }

        // ---------------------------------------------------------------------
        // Slug / lookup
        // ---------------------------------------------------------------------

        private IQueryable<ContentChannelItem> ChannelItemQuery()
        {
            var channelGuid = LinkListGuids.LinkListChannel.AsGuid();
            return new ContentChannelItemService( _rockContext )
                .Queryable()
                .Where( i => i.ContentChannel != null && i.ContentChannel.Guid == channelGuid );
        }

        private static string PrimarySlug( ContentChannelItem item )
        {
            if ( item == null )
            {
                return null;
            }
            return item.ContentChannelItemSlugs?
                .OrderBy( s => s.Id )
                .Select( s => s.Slug )
                .FirstOrDefault();
        }

        /// <summary>
        /// Resolves a Content Channel Item by integer id, GUID, or slug, restricted
        /// to the LinkList channel.
        /// </summary>
        public ContentChannelItem ResolveItem( string idOrSlugOrGuid )
        {
            if ( idOrSlugOrGuid.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var query = ChannelItemQuery();

            var asInt = idOrSlugOrGuid.AsIntegerOrNull();
            if ( asInt.HasValue )
            {
                var byId = query.FirstOrDefault( i => i.Id == asInt.Value );
                if ( byId != null )
                {
                    return byId;
                }
            }

            var asGuid = idOrSlugOrGuid.AsGuidOrNull();
            if ( asGuid.HasValue )
            {
                var byGuid = query.FirstOrDefault( i => i.Guid == asGuid.Value );
                if ( byGuid != null )
                {
                    return byGuid;
                }
            }

            // Slug lookup, normalized to the canonical lowercase form (stored
            // slugs are always lowercase). Caller is responsible for
            // slug-charset validation; we still hard-cap length here as a
            // defense-in-depth measure.
            var slug = NormalizeSlug( idOrSlugOrGuid );
            if ( slug.Length > MaxSlugLength )
            {
                return null;
            }

            return query
                .Where( i => i.ContentChannelItemSlugs.Any( s => s.Slug == slug ) )
                .FirstOrDefault();
        }

        // ---------------------------------------------------------------------
        // Bag mapping
        // ---------------------------------------------------------------------

        /// <summary>
        /// Summaries for the management grid. Admins see every list; everyone
        /// else sees only the lists they have EDIT on. Populates the display
        /// fields the grid needs (IsPublic, DesignName, ModifiedDateTime).
        /// </summary>
        public List<LinkListBag> QueryListSummaries( Person currentPerson, string searchTerm, int page, int pageSize, bool isAdmin )
        {
            if ( pageSize <= 0 || pageSize > 1000 )
            {
                pageSize = 500;
            }
            if ( page < 0 )
            {
                page = 0;
            }

            var query = ChannelItemQuery();

            if ( !searchTerm.IsNullOrWhiteSpace() )
            {
                var term = searchTerm.Trim();
                query = query.Where( i =>
                    i.Title.Contains( term ) ||
                    i.ContentChannelItemSlugs.Any( s => s.Slug.Contains( term ) ) );
            }

            // Page the ids at the SQL layer (one query), then load that page's
            // entities - with slugs eager-loaded - in a single round-trip. This
            // avoids the previous N+1 (one single-row re-fetch per result).
            var pagedIds = query
                .OrderBy( i => i.Title )
                .Skip( page * pageSize )
                .Take( pageSize )
                .Select( i => i.Id )
                .ToList();

            if ( pagedIds.Count == 0 )
            {
                return new List<LinkListBag>();
            }

            var channelGuid = LinkListGuids.LinkListChannel.AsGuid();
            var items = new ContentChannelItemService( _rockContext )
                .Queryable( "ContentChannelItemSlugs" )
                .Where( i => i.ContentChannel != null
                    && i.ContentChannel.Guid == channelGuid
                    && pagedIds.Contains( i.Id ) )
                .OrderBy( i => i.Title )
                .ToList();

            // Non-admins see only lists they can EDIT (the management permission).
            // Item attributes (IsPublic / Design) are read after the auth filter
            // so only rows that will be returned get hydrated - and in one
            // bulk pass, not one round-trip per row.
            var visibleItems = items
                .Where( item => isAdmin || ( currentPerson != null && item.IsAuthorized( Authorization.EDIT, currentPerson ) ) )
                .ToList();
            visibleItems.LoadAttributes( _rockContext );

            var result = new List<LinkListBag>();
            foreach ( var item in visibleItems )
            {
                result.Add( new LinkListBag
                {
                    Id = item.Id,
                    Guid = item.Guid.ToString(),
                    Title = item.Title ?? "Untitled",
                    Slug = PrimarySlug( item ),
                    IsPublic = ReadIsPublic( item ),
                    DesignName = ResolveDesignName( item ),
                    ModifiedDateTime = item.ModifiedDateTime,

                    // Mirror the exact check the List block's Delete action
                    // enforces so the grid's delete button reflects reality.
                    CanDelete = currentPerson != null
                        && item.IsAuthorized( Authorization.ADMINISTRATE, currentPerson )
                } );
            }
            return result;
        }

        /// <summary>
        /// Friendly name of the item's selected design preset, or null.
        /// </summary>
        private string ResolveDesignName( ContentChannelItem item )
        {
            var designGuid = item.GetAttributeValue( LinkListGuids.ItemAttributeKey.DesignId ).AsGuidOrNull();
            if ( !designGuid.HasValue )
            {
                return null;
            }
            return DefinedValueCache.Get( designGuid.Value )?.Value;
        }

        /// <summary>
        /// Full hydration of a Link List bag including matrix items.
        /// </summary>
        public LinkListBag GetListBag( string idOrSlugOrGuid, Person currentPerson, bool requirePublic )
        {
            var item = ResolveItem( idOrSlugOrGuid );
            return BuildBag( item, currentPerson, requirePublic );
        }

        public LinkListBag BuildBag( ContentChannelItem item, Person currentPerson, bool requirePublic )
        {
            return BuildBag( item, currentPerson, requirePublic, includeMembers: false );
        }

        public LinkListBag BuildBag( ContentChannelItem item, Person currentPerson, bool requirePublic, bool includeMembers )
        {
            if ( item == null )
            {
                return null;
            }
            item.LoadAttributes( _rockContext );

            var isPublic = ReadIsPublic( item );
            if ( requirePublic && !isPublic )
            {
                return null;
            }

            // Authorization check (skipped for unauthenticated public-endpoint
            // calls when requirePublic is true and the item has already passed
            // the IsPublic gate).
            if ( currentPerson != null && !item.IsAuthorized( Authorization.VIEW, currentPerson ) )
            {
                return null;
            }

            var bag = new LinkListBag
            {
                Id = item.Id,
                Guid = item.Guid.ToString(),
                Title = item.Title ?? "Untitled",
                Slug = PrimarySlug( item ),
                IsPublic = isPublic,
                ContentTextColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.ContentTextColor ),
                BackgroundColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.BackgroundColor ),
                ButtonColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.ButtonColor ),
                ButtonTextColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.ButtonTextColor ),
                FeaturedButtonColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.FeaturedButtonColor ),
                FeaturedButtonTextColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.FeaturedButtonTextColor ),
                TitleColor = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.TitleColor ),

                // Freeform HTML blobs are sanitized HERE - the single chokepoint
                // every consumer flows through (viewer, editor, public REST) -
                // so all of them, including third-party REST callers, get clean
                // HTML, and the ~190 existing lists are covered with no data
                // migration.
                FooterContent = LinkListHtmlSanitizer.Sanitize( item.GetAttributeValue( LinkListGuids.TypeAttributeKey.FooterContent ) ),

                // Intro is the NATIVE ContentChannelItem.Content field (legacy
                // renders {{ linkList.Content }}); the IntroContent attribute is
                // not used.
                IntroContent = LinkListHtmlSanitizer.Sanitize( item.Content ),

                // Legacy display-parity fields (WS2.5).
                CustomTitle = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.CustomTitle ),
                HideTitle = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.HideTitle ).AsBoolean(),
                HeaderImage = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.HeaderImage ),
                RoundHeaderImage = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.RoundHeaderImage ).AsBoolean(),
                BackgroundImage = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.BackgroundImage ),
                HeaderBackgroundImage = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.HeaderBackgroundImage ),
                HeaderVideo = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.HeaderVideo ),
                HeaderVideoVimeoId = item.GetAttributeValue( LinkListGuids.TypeAttributeKey.HeaderVideoVimeoId ),

                Items = LoadMatrixItems( item )
            };

            // Resolve the optional design preset as the BASE colors. Per-list
            // color attributes (above) are OVERRIDES that win; the renderers
            // use the Effective* values. The raw overrides stay on
            // ContentTextColor/etc. so the editor can detect divergence ("Custom").
            string presetContent = null, presetBackground = null, presetButton = null, presetButtonText = null;
            string presetFeaturedButton = null, presetFeaturedButtonText = null, presetTitle = null;
            var designGuid = item.GetAttributeValue( LinkListGuids.ItemAttributeKey.DesignId ).AsGuidOrNull();
            if ( designGuid.HasValue )
            {
                var design = DefinedValueCache.Get( designGuid.Value );
                if ( design != null )
                {
                    bag.DesignId = design.Guid;
                    presetContent = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.ContentTextColor );
                    presetBackground = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.BackgroundColor );
                    presetButton = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonColor );
                    presetButtonText = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonTextColor );
                    presetFeaturedButton = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonColor );
                    presetFeaturedButtonText = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonTextColor );
                    presetTitle = design.GetAttributeValue( LinkListGuids.DesignAttributeKey.TitleColor );
                }
            }

            // Override wins, else preset.
            bag.EffectiveContentTextColor = FirstNonEmpty( bag.ContentTextColor, presetContent );
            bag.EffectiveBackgroundColor = FirstNonEmpty( bag.BackgroundColor, presetBackground );
            bag.EffectiveButtonColor = FirstNonEmpty( bag.ButtonColor, presetButton );
            bag.EffectiveButtonTextColor = FirstNonEmpty( bag.ButtonTextColor, presetButtonText );
            bag.EffectiveFeaturedButtonColor = FirstNonEmpty( bag.FeaturedButtonColor, presetFeaturedButton );
            bag.EffectiveFeaturedButtonTextColor = FirstNonEmpty( bag.FeaturedButtonTextColor, presetFeaturedButtonText );

            // WS7 fix 7: title color falls back to the content text color when unset
            // (override -> preset -> content text) so existing lists don't change.
            bag.EffectiveTitleColor = FirstNonEmpty( FirstNonEmpty( bag.TitleColor, presetTitle ), bag.EffectiveContentTextColor );

            // WS12: org-wide global header/footer, resolved (content only when the
            // matching Active toggle is on) and sanitized at this single chokepoint
            // exactly like intro/footer. Read once from the channel cache - it is
            // identical for every list.
            var global = GetResolvedGlobalHeaderFooter();
            bag.GlobalHeaderContent = SanitizeOrNull( global.Header );
            bag.GlobalFooterContent = SanitizeOrNull( global.Footer );
            bag.UseIvyJournalFont = global.UseIvyJournalFont;

            if ( includeMembers )
            {
                var primaryGroup = GetPrimarySecurityGroup( item );
                if ( primaryGroup != null )
                {
                    bag.SecurityGroupName = primaryGroup.Name;
                    bag.Members = GetMembers( primaryGroup, currentPerson );
                }
                else
                {
                    bag.Members = new List<GroupMemberBag>();
                }
                bag.AlsoHasEditAccess = GetOtherEditAccess( item, primaryGroup );
            }

            return bag;
        }

        /// <summary>
        /// The item's IsPublic gate (default true for legacy items missing the
        /// attribute). Public: the click endpoint uses it as a lightweight
        /// check without building the full bag. Caller must have loaded (or
        /// accept lazy-loading of) the item's attributes.
        /// </summary>
        public bool ReadIsPublic( ContentChannelItem item )
        {
            if ( item.Attributes == null || !item.Attributes.ContainsKey( LinkListGuids.ItemAttributeKey.IsPublic ) )
            {
                // Default true for legacy items missing the attribute.
                return true;
            }
            var raw = item.GetAttributeValue( LinkListGuids.ItemAttributeKey.IsPublic );
            if ( raw.IsNullOrWhiteSpace() )
            {
                return true;
            }
            return raw.AsBoolean( true );
        }

        // ---------------------------------------------------------------------
        // Matrix items
        // ---------------------------------------------------------------------

        /// <summary>
        /// Resolves the item-level attribute key that points at this list's
        /// AttributeMatrix.
        /// </summary>
        /// <remarks>
        /// Grounded in Rock core <see cref="Rock.Field.Types.MatrixFieldType"/>:
        /// the matrix attribute's stored value is the AttributeMatrix.Guid, and
        /// its "attributematrixtemplate" qualifier holds the template *Id*
        /// (an integer), not the template Guid. The previous implementation
        /// compared the qualifier to <c>template.Guid</c> via AsGuid(), which
        /// never matched, so matrix discovery silently returned zero rows.
        ///
        /// We now resolve deterministically by the well-known LinkListMatrix
        /// key (what production actually uses) and keep a corrected,
        /// Id-based qualifier scan as a fallback.
        /// </remarks>
        private string ResolveMatrixPointerKey( ContentChannelItem item, AttributeMatrixTemplate template )
        {
            if ( item?.Attributes == null )
            {
                return null;
            }

            // 1. Deterministic: the well-known type-level attribute key.
            if ( item.Attributes.ContainsKey( LinkListGuids.TypeAttributeKey.LinkListMatrix ) )
            {
                return LinkListGuids.TypeAttributeKey.LinkListMatrix;
            }

            // 2. Fallback: any Matrix-field attribute whose template qualifier
            //    matches ours. Rock stores the qualifier value as the template
            //    Id; accept the Guid form too for defense in depth.
            if ( template == null )
            {
                return null;
            }
            var templateId = template.Id.ToString();
            var templateGuid = template.Guid.ToString();
            foreach ( var kvp in item.Attributes )
            {
                var qv = kvp.Value?.QualifierValues;
                if ( qv == null
                    || !qv.ContainsKey( Rock.Field.Types.MatrixFieldType.ATTRIBUTE_MATRIX_TEMPLATE ) )
                {
                    continue;
                }
                var bound = qv[Rock.Field.Types.MatrixFieldType.ATTRIBUTE_MATRIX_TEMPLATE]?.Value;
                if ( bound.IsNullOrWhiteSpace() )
                {
                    continue;
                }
                if ( string.Equals( bound, templateId, StringComparison.OrdinalIgnoreCase )
                    || string.Equals( bound, templateGuid, StringComparison.OrdinalIgnoreCase ) )
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the AttributeMatrix referenced by the LinkList item, returning
        /// matrix rows mapped to LinkItemBag with null-safe legacy defaults.
        /// </summary>
        private List<LinkItemBag> LoadMatrixItems( ContentChannelItem item )
        {
            var template = GetMatrixTemplate();
            if ( template == null || item.Attributes == null )
            {
                return new List<LinkItemBag>();
            }

            var pointerKey = ResolveMatrixPointerKey( item, template );
            if ( pointerKey == null )
            {
                return new List<LinkItemBag>();
            }

            var matrixGuid = item.GetAttributeValue( pointerKey ).AsGuidOrNull();
            if ( !matrixGuid.HasValue )
            {
                return new List<LinkItemBag>();
            }

            var matrix = new AttributeMatrixService( _rockContext )
                .Queryable( "AttributeMatrixItems" )
                .FirstOrDefault( m => m.Guid == matrixGuid.Value );

            if ( matrix == null )
            {
                return new List<LinkItemBag>();
            }

            var bags = new List<LinkItemBag>();
            var displayOrder = 0;

            // Production data has many lists with every row at Order = 0 (bulk
            // import). Match legacy behavior: order by Order, then by the
            // matrix item's natural id as a stable tiebreaker, and emit a
            // normalized monotonic Order so the front end renders in sequence.
            var orderedRows = matrix.AttributeMatrixItems
                .OrderBy( r => r.Order )
                .ThenBy( r => r.Id )
                .ToList();

            // Bulk-load attributes for the whole row set in one pass instead of
            // one round-trip per row (a 100-link list was 100 queries).
            orderedRows.LoadAttributes( _rockContext );

            foreach ( var row in orderedRows )
            {
                bags.Add( new LinkItemBag
                {
                    Guid = row.Guid.ToString(),
                    MatrixItemGuid = row.Guid.ToString(),
                    ItemType = SafeAttr( row, LinkListGuids.MatrixAttributeKey.ItemType, LinkListGuids.ItemTypeValue.Link ),
                    Url = SafeAttr( row, LinkListGuids.MatrixAttributeKey.Url, string.Empty ),
                    Text = SafeAttr( row, LinkListGuids.MatrixAttributeKey.LinkText, string.Empty ),
                    Target = SafeAttr( row, LinkListGuids.MatrixAttributeKey.Target, string.Empty ),
                    IndentLevel = SafeAttr( row, LinkListGuids.MatrixAttributeKey.IndentLevel, "0" ).AsInteger(),
                    IsSectionCollapsed = SafeAttr( row, LinkListGuids.MatrixAttributeKey.IsSectionCollapsed, "False" ).AsBoolean(),
                    Subtitle = SafeAttr( row, LinkListGuids.MatrixAttributeKey.Subtitle, string.Empty ),
                    Description = SafeAttr( row, LinkListGuids.MatrixAttributeKey.Description, string.Empty ),
                    IsFeatured = SafeAttr( row, LinkListGuids.MatrixAttributeKey.IsFeatured, "False" ).AsBoolean(),
                    Order = displayOrder++
                } );
            }
            return bags;
        }

        /// <summary>
        /// ROCK-7164: the matrix row with the given guid IFF it belongs to
        /// THIS item's matrix - the click endpoint's anti-spoof check (a
        /// beacon can't record clicks against another list's rows or
        /// arbitrary matrix rows). Null when the item has no matrix or the
        /// row isn't in it. One join query; attributes are NOT loaded (the
        /// caller loads them only after validation passes).
        /// </summary>
        public AttributeMatrixItem FindMatrixRow( ContentChannelItem item, Guid rowGuid )
        {
            if ( item == null )
            {
                return null;
            }

            item.LoadAttributes( _rockContext );
            var pointerKey = ResolveMatrixPointerKey( item, GetMatrixTemplate() );
            if ( pointerKey == null )
            {
                return null;
            }

            var matrixGuid = item.GetAttributeValue( pointerKey ).AsGuidOrNull();
            if ( !matrixGuid.HasValue )
            {
                return null;
            }

            return new AttributeMatrixItemService( _rockContext )
                .Queryable()
                .FirstOrDefault( r => r.Guid == rowGuid && r.AttributeMatrix.Guid == matrixGuid.Value );
        }

        /// <summary>
        /// ROCK-7164: the item's current link rows (matrix rows mapped to
        /// bags) without building the full list bag - used by the analytics
        /// action to label per-link click counts with live text/URLs.
        /// </summary>
        public List<LinkItemBag> GetLinkRows( ContentChannelItem item )
        {
            if ( item == null )
            {
                return new List<LinkItemBag>();
            }
            item.LoadAttributes( _rockContext );
            return LoadMatrixItems( item );
        }

        /// <summary>
        /// ROCK-7164: map of AttributeMatrixItem Id -> Guid for the item's
        /// matrix rows. Click interactions store the row Id (EntityId); the
        /// bags key on the row Guid - this joins the two.
        /// </summary>
        public Dictionary<int, Guid> GetMatrixRowIdMap( ContentChannelItem item )
        {
            var result = new Dictionary<int, Guid>();
            if ( item == null )
            {
                return result;
            }

            item.LoadAttributes( _rockContext );
            var pointerKey = ResolveMatrixPointerKey( item, GetMatrixTemplate() );
            if ( pointerKey == null )
            {
                return result;
            }
            var matrixGuid = item.GetAttributeValue( pointerKey ).AsGuidOrNull();
            if ( !matrixGuid.HasValue )
            {
                return result;
            }

            return new AttributeMatrixItemService( _rockContext )
                .Queryable()
                .Where( r => r.AttributeMatrix.Guid == matrixGuid.Value )
                .Select( r => new { r.Id, r.Guid } )
                .ToDictionary( r => r.Id, r => r.Guid );
        }

        /// <summary>Returns <paramref name="primary"/> if non-blank, else <paramref name="fallback"/>.</summary>
        private static string FirstNonEmpty( string primary, string fallback )
        {
            return primary.IsNullOrWhiteSpace() ? fallback : primary;
        }

        private static string SafeAttr( AttributeMatrixItem row, string key, string fallback )
        {
            if ( row.Attributes != null && row.Attributes.ContainsKey( key ) )
            {
                var v = row.GetAttributeValue( key );
                if ( !v.IsNullOrWhiteSpace() )
                {
                    return v;
                }
            }
            return fallback;
        }

        /// <summary>
        /// Replaces the link rows on the supplied item with the incoming bag
        /// list. Row identity is preserved by <see cref="LinkItemBag.Guid"/>:
        /// rows with a matching Guid are updated in place, rows without a
        /// match are created, and existing rows whose Guid is missing from
        /// the incoming list are deleted. Order is taken from the incoming
        /// list's position (ignoring <see cref="LinkItemBag.Order"/>).
        /// Lazily creates the backing AttributeMatrix when the item has none.
        /// </summary>
        public void PersistMatrixItems( ContentChannelItem item, List<LinkItemBag> incoming )
        {
            if ( item == null )
            {
                throw new ArgumentNullException( nameof( item ) );
            }
            if ( incoming == null )
            {
                incoming = new List<LinkItemBag>();
            }

            var template = GetMatrixTemplate();
            if ( template == null )
            {
                throw new InvalidOperationException( "Link List matrix template not found." );
            }

            item.LoadAttributes( _rockContext );

            // Locate the matrix-pointer attribute on the item (the well-known
            // LinkListMatrix key, or a template-bound Matrix attribute).
            var pointerKey = ResolveMatrixPointerKey( item, template );
            if ( pointerKey == null )
            {
                // No matrix-pointer attribute exists on the item. Nothing to
                // persist into. This would indicate the LinkListMatrix
                // type-level attribute is missing from the channel type.
                return;
            }

            var matrixService = new AttributeMatrixService( _rockContext );
            var matrixItemService = new AttributeMatrixItemService( _rockContext );

            // NOTE: intentionally NOT wrapped in WrapTransaction (same reason as SaveList).
            // Rock's attribute helpers (row.SaveAttributeValues / LoadAttributes) open their
            // OWN RockContext/connection. On REORDER the SaveChanges below write-locks the
            // EXISTING AttributeMatrixItem rows (their [Order] is UPDATEd); those helper
            // connections then block on the uncommitted, locked rows -> self-deadlock that
            // surfaces as "The wait operation timed out." (Add never hit this: appending only
            // INSERTs a new row, so no pre-existing committed row is write-locked.) Removing
            // the wrapper lets each SaveChanges commit and release its locks immediately. The
            // structural rebuild stays atomic on its own: the single SaveChanges below applies
            // the deletes, inserts, and reorders together; only the per-row attribute writes
            // commit individually (acceptable - a re-save re-applies them).
            {
                // Resolve or create the AttributeMatrix.
                AttributeMatrix matrix = null;
                var existingGuid = item.GetAttributeValue( pointerKey ).AsGuidOrNull();
                if ( existingGuid.HasValue )
                {
                    matrix = matrixService
                        .Queryable( "AttributeMatrixItems" )
                        .FirstOrDefault( m => m.Guid == existingGuid.Value );
                }

                if ( matrix == null )
                {
                    matrix = new AttributeMatrix
                    {
                        Guid = Guid.NewGuid(),
                        AttributeMatrixTemplateId = template.Id
                    };
                    matrixService.Add( matrix );
                    _rockContext.SaveChanges();

                    item.SetAttributeValue( pointerKey, matrix.Guid.ToString() );
                    item.SaveAttributeValues( _rockContext );
                }

                var existingRows = matrix.AttributeMatrixItems?.ToList() ?? new List<AttributeMatrixItem>();
                var incomingGuids = new HashSet<Guid>(
                    incoming
                        .Select( b => b.Guid.AsGuidOrNull() ?? b.MatrixItemGuid.AsGuidOrNull() ?? Guid.Empty )
                        .Where( g => g != Guid.Empty ) );

                // Delete removed rows.
                foreach ( var row in existingRows.Where( r => !incomingGuids.Contains( r.Guid ) ).ToList() )
                {
                    matrixItemService.Delete( row );
                }

                // Resolve every incoming row to an entity (reuse by Guid, else
                // create) and set its Order. Build the list in incoming order so
                // it stays index-aligned with the bag for the attribute pass.
                var rows = new List<AttributeMatrixItem>( incoming.Count );
                var orderIndex = 0;
                foreach ( var bagRow in incoming )
                {
                    var rowGuid = bagRow.Guid.AsGuidOrNull() ?? bagRow.MatrixItemGuid.AsGuidOrNull();
                    AttributeMatrixItem row = null;
                    if ( rowGuid.HasValue )
                    {
                        row = existingRows.FirstOrDefault( r => r.Guid == rowGuid.Value );
                    }
                    if ( row == null )
                    {
                        row = new AttributeMatrixItem
                        {
                            Guid = Guid.NewGuid(),
                            AttributeMatrixId = matrix.Id
                        };
                        matrixItemService.Add( row );
                    }
                    row.Order = orderIndex++;
                    rows.Add( row );
                }

                // One flush applies the deletes, inserts, and reorders together
                // (and assigns Ids to new rows before we write their attributes).
                _rockContext.SaveChanges();

                // Row attribute values. Rock's attribute API persists per entity on
                // its own connection; each commits individually now that this method
                // holds no wrapping transaction (see note above).
                //
                // Attributes are bulk-loaded for the whole row set in one pass
                // (not one round-trip per row). Ids exist (SaveChanges above)
                // and the qualifier lookup (AttributeMatrixTemplateId) resolves
                // in-memory via the tracked AttributeMatrix navigation.
                rows.LoadAttributes( _rockContext );

                // WS10 invariant: at most ONE link row may be featured. The UI is
                // radio-like, but enforce it server-side too (defense in depth) -
                // the first featured link wins, the rest are cleared.
                var featuredUsed = false;
                for ( var i = 0; i < rows.Count; i++ )
                {
                    var row = rows[i];
                    var bagRow = incoming[i];
                    var normalizedType = NormalizeItemType( bagRow.ItemType );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.ItemType, normalizedType );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.Url, bagRow.Url ?? string.Empty );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.LinkText, bagRow.Text ?? string.Empty );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.Target, bagRow.Target ?? string.Empty );
                    // WS7 fix 5: IndentLevel is reused as the per-link "nested" flag
                    // (1 = nested under the open section, 0 = top-level). Only links
                    // carry it; sections/separators are always 0.
                    var nested = normalizedType == LinkListGuids.ItemTypeValue.Link && bagRow.IndentLevel >= 1 ? "1" : "0";
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.IndentLevel, nested );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.IsSectionCollapsed, bagRow.IsSectionCollapsed.ToString() );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.Subtitle, bagRow.Subtitle ?? string.Empty );
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.Description, bagRow.Description ?? string.Empty );

                    var isFeatured = bagRow.IsFeatured
                        && normalizedType == LinkListGuids.ItemTypeValue.Link
                        && !featuredUsed;
                    if ( isFeatured )
                    {
                        featuredUsed = true;
                    }
                    SetMatrixAttr( row, LinkListGuids.MatrixAttributeKey.IsFeatured, isFeatured.ToString() );

                    row.SaveAttributeValues( _rockContext );
                }
            }
        }

        private static void SetMatrixAttr( AttributeMatrixItem row, string key, string value )
        {
            if ( row.Attributes != null && row.Attributes.ContainsKey( key ) )
            {
                row.SetAttributeValue( key, value ?? string.Empty );
            }
        }

        private static string NormalizeItemType( string raw )
        {
            if ( raw.IsNullOrWhiteSpace() ) return LinkListGuids.ItemTypeValue.Link;
            var lower = raw.Trim().ToLowerInvariant();
            switch ( lower )
            {
                case "section":
                case "separator":
                case "link":
                    return lower;
                default:
                    return LinkListGuids.ItemTypeValue.Link;
            }
        }

        // ---------------------------------------------------------------------
        // Design preset picker
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns the available "Link List Design" presets (active values
        /// only) for the editor's design dropdown.
        /// </summary>
        public List<DesignOptionBag> GetDesignsForPicker()
        {
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListDesign.AsGuid() );
            if ( definedType == null )
            {
                return new List<DesignOptionBag>();
            }

            return definedType.DefinedValues
                .Where( v => v.IsActive )
                .OrderBy( v => v.Order )
                .Select( v => new DesignOptionBag
                {
                    Value = v.Guid.ToString(),
                    Text = v.Value,
                    Description = v.Description,
                    ContentTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ContentTextColor ),
                    BackgroundColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.BackgroundColor ),
                    ButtonColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonColor ),
                    ButtonTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonTextColor ),
                    FeaturedButtonColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonColor ),
                    FeaturedButtonTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonTextColor ),
                    TitleColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.TitleColor )
                } )
                .ToList();
        }

        // ---------------------------------------------------------------------
        // WS13: Design preset management (admin) - CRUD the Link List Design
        // DefinedValues + their color attribute values.
        // ---------------------------------------------------------------------

        /// <summary>All design presets with their six colors and per-preset usage counts.</summary>
        public List<DesignPresetBag> GetDesignPresets()
        {
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListDesign.AsGuid() );
            if ( definedType == null )
            {
                return new List<DesignPresetBag>();
            }

            var usage = GetDesignUsageCounts();
            return definedType.DefinedValues
                .OrderBy( v => v.Order )
                .Select( v => new DesignPresetBag
                {
                    Value = v.Guid.ToString(),
                    Name = v.Value,
                    Description = v.Description,
                    Order = v.Order,
                    BackgroundColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.BackgroundColor ),
                    ContentTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ContentTextColor ),
                    TitleColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.TitleColor ),
                    ButtonColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonColor ),
                    ButtonTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.ButtonTextColor ),
                    FeaturedButtonColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonColor ),
                    FeaturedButtonTextColor = v.GetAttributeValue( LinkListGuids.DesignAttributeKey.FeaturedButtonTextColor ),
                    UsageCount = usage.TryGetValue( v.Guid, out var c ) ? c : 0
                } )
                .ToList();
        }

        /// <summary>Map of design-preset GUID -> number of LinkList items whose Design attribute references it.</summary>
        private Dictionary<Guid, int> GetDesignUsageCounts()
        {
            var result = new Dictionary<Guid, int>();
            var designAttr = AttributeCache.Get( LinkListGuids.ItemAttributeDesignId.AsGuid() );
            if ( designAttr == null )
            {
                return result;
            }
            var rows = new AttributeValueService( _rockContext )
                .Queryable()
                .Where( av => av.AttributeId == designAttr.Id && av.Value != null && av.Value != "" )
                .GroupBy( av => av.Value )
                .Select( g => new { Value = g.Key, Count = g.Count() } )
                .ToList();
            foreach ( var r in rows )
            {
                var g = r.Value.AsGuidOrNull();
                if ( g.HasValue )
                {
                    result[g.Value] = r.Count;
                }
            }
            return result;
        }

        /// <summary>
        /// Creates or updates a design preset (DefinedValue + its color attribute
        /// values) and flushes the DefinedType/DefinedValue caches so theme
        /// resolution and the editor's preset picker pick up the change.
        /// Caller MUST verify admin (channel Administrate) first.
        /// </summary>
        public DesignPresetBag SaveDesignPreset( DesignPresetBag bag )
        {
            if ( bag == null )
            {
                throw new ArgumentNullException( nameof( bag ) );
            }
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListDesign.AsGuid() );
            if ( definedType == null )
            {
                throw new InvalidOperationException( "Link List Design defined type not found." );
            }

            var service = new DefinedValueService( _rockContext );
            var guid = bag.Value.AsGuidOrNull();
            var value = guid.HasValue ? service.Get( guid.Value ) : null;
            var isNew = value == null;
            if ( isNew )
            {
                value = new DefinedValue
                {
                    DefinedTypeId = definedType.Id,
                    IsSystem = false,
                    IsActive = true,
                    Guid = Guid.NewGuid(),
                    Order = definedType.DefinedValues.Count
                };
                service.Add( value );
            }
            value.Value = ( bag.Name ?? string.Empty ).Trim();
            value.Description = bag.Description;
            _rockContext.SaveChanges();

            value.LoadAttributes( _rockContext );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.BackgroundColor, bag.BackgroundColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.ContentTextColor, bag.ContentTextColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.TitleColor, bag.TitleColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.ButtonColor, bag.ButtonColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.ButtonTextColor, bag.ButtonTextColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.FeaturedButtonColor, bag.FeaturedButtonColor );
            SetDesignAttr( value, LinkListGuids.DesignAttributeKey.FeaturedButtonTextColor, bag.FeaturedButtonTextColor );
            value.SaveAttributeValues( _rockContext );

            FlushDesignCaches();

            var usage = GetDesignUsageCounts();
            bag.Value = value.Guid.ToString();
            bag.Order = value.Order;
            bag.UsageCount = usage.TryGetValue( value.Guid, out var c ) ? c : 0;
            return bag;
        }

        /// <summary>
        /// Deletes a design preset. Refuses when the preset is still in use
        /// (default per WS7 admin fixes) so live lists aren't silently changed.
        /// Caller MUST verify admin first.
        /// </summary>
        public bool DeleteDesignPreset( Guid presetGuid, out string errorMessage )
        {
            errorMessage = null;
            var service = new DefinedValueService( _rockContext );
            var value = service.Get( presetGuid );
            if ( value == null )
            {
                errorMessage = "Preset not found.";
                return false;
            }

            var usage = GetDesignUsageCounts();
            if ( usage.TryGetValue( presetGuid, out var count ) && count > 0 )
            {
                errorMessage = $"This preset is used by {count} list(s). Reassign those lists to another preset first.";
                return false;
            }

            service.Delete( value );
            _rockContext.SaveChanges();
            FlushDesignCaches();
            return true;
        }

        private static void SetDesignAttr( DefinedValue value, string key, string color )
        {
            if ( value.Attributes != null && value.Attributes.ContainsKey( key ) )
            {
                value.SetAttributeValue( key, color ?? string.Empty );
            }
        }

        private static void FlushDesignCaches()
        {
            DefinedValueCache.Clear();
            DefinedTypeCache.Clear();
        }

        // ---------------------------------------------------------------------
        // WS13: Allowed Origins management (admin) - CRUD the "Link List Allowed
        // Origins" DefinedValues. The controller still reads through the static
        // GetAllowedOrigins() (built-in fallback unioned with these).
        // ---------------------------------------------------------------------

        public AllowedOriginsBag GetAllowedOriginsForAdmin()
        {
            var bag = new AllowedOriginsBag
            {
                BuiltIn = LinkListGuids.AllowedOrigins.ToList()
            };
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListAllowedOrigins.AsGuid() );
            if ( definedType != null )
            {
                bag.Custom = definedType.DefinedValues
                    .OrderBy( v => v.Order )
                    .Select( v => v.Value )
                    .Where( s => !s.IsNullOrWhiteSpace() )
                    .ToList();
            }
            return bag;
        }

        /// <summary>
        /// Reconciles the "Link List Allowed Origins" DefinedValues to the supplied
        /// list (validated as scheme://host[:port]) and flushes the cache. Caller
        /// MUST verify admin first.
        /// </summary>
        public bool SaveAllowedOrigins( List<string> origins, out string errorMessage )
        {
            errorMessage = null;
            var definedType = DefinedTypeCache.Get( LinkListGuids.DefinedTypeLinkListAllowedOrigins.AsGuid() );
            if ( definedType == null )
            {
                errorMessage = "Allowed Origins defined type not found.";
                return false;
            }

            var incoming = ( origins ?? new List<string>() )
                .Select( o => o?.Trim() )
                .Where( o => !string.IsNullOrWhiteSpace( o ) )
                .Distinct( StringComparer.OrdinalIgnoreCase )
                .ToList();
            foreach ( var o in incoming )
            {
                if ( !IsValidOrigin( o ) )
                {
                    errorMessage = $"'{o}' is not a valid origin. Use scheme://host[:port] with no path.";
                    return false;
                }
            }

            var service = new DefinedValueService( _rockContext );
            var existing = service.Queryable().Where( v => v.DefinedTypeId == definedType.Id ).ToList();

            foreach ( var v in existing.Where( v => !incoming.Any( i => string.Equals( i, v.Value, StringComparison.OrdinalIgnoreCase ) ) ).ToList() )
            {
                service.Delete( v );
            }
            var order = existing.Count;
            foreach ( var o in incoming.Where( i => !existing.Any( e => string.Equals( e.Value, i, StringComparison.OrdinalIgnoreCase ) ) ) )
            {
                service.Add( new DefinedValue
                {
                    DefinedTypeId = definedType.Id,
                    Value = o,
                    IsActive = true,
                    IsSystem = false,
                    Guid = Guid.NewGuid(),
                    Order = order++
                } );
            }
            _rockContext.SaveChanges();
            DefinedValueCache.Clear();
            DefinedTypeCache.Clear();
            return true;
        }

        // ---------------------------------------------------------------------
        // WS12: org-wide global header/footer (ContentChannel singleton)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns the resolved global header/footer content for display: each is
        /// non-null only when its Active toggle is on. Read from
        /// <see cref="ContentChannelCache"/> (no per-request DB hit) since the value
        /// is identical for every list. Callers still sanitize before rendering.
        /// </summary>
        public (string Header, string Footer, bool UseIvyJournalFont) GetResolvedGlobalHeaderFooter()
        {
            var channel = ContentChannelCache.Get( LinkListGuids.LinkListChannel.AsGuid() );
            if ( channel == null )
            {
                return ( null, null, false );
            }

            string header = null, footer = null;
            if ( channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalHeaderActive ).AsBoolean() )
            {
                header = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalHeaderContent );
            }
            if ( channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalFooterActive ).AsBoolean() )
            {
                footer = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalFooterContent );
            }
            var useIvy = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.UseIvyJournalFont ).AsBoolean();
            return ( header, footer, useIvy );
        }

        /// <summary>
        /// Reads the RAW (unsanitized) global header/footer settings for the admin
        /// editor, so the original markup round-trips. Display sanitizes at BuildBag.
        /// </summary>
        public GlobalSettingsBag GetGlobalSettings()
        {
            var channel = GetChannel();
            if ( channel == null )
            {
                return new GlobalSettingsBag();
            }
            channel.LoadAttributes( _rockContext );
            return new GlobalSettingsBag
            {
                HeaderContent = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalHeaderContent ),
                HeaderActive = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalHeaderActive ).AsBoolean(),
                FooterContent = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalFooterContent ),
                FooterActive = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.GlobalFooterActive ).AsBoolean(),
                UseIvyJournalFont = channel.GetAttributeValue( LinkListGuids.ChannelAttributeKey.UseIvyJournalFont ).AsBoolean()
            };
        }

        /// <summary>
        /// Writes the global header/footer settings onto the LinkList channel and
        /// flushes the channel cache so the resolved read picks them up. Caller MUST
        /// verify Administrate on the channel first. No wrapping transaction (the
        /// attribute helper manages its own context).
        /// </summary>
        public void SaveGlobalSettings( GlobalSettingsBag bag )
        {
            if ( bag == null )
            {
                return;
            }
            var channel = GetChannel();
            if ( channel == null )
            {
                throw new InvalidOperationException( "Link Lists channel not found." );
            }
            channel.LoadAttributes( _rockContext );
            SetChannelAttr( channel, LinkListGuids.ChannelAttributeKey.GlobalHeaderContent, bag.HeaderContent );
            SetChannelAttr( channel, LinkListGuids.ChannelAttributeKey.GlobalHeaderActive, bag.HeaderActive.ToString() );
            SetChannelAttr( channel, LinkListGuids.ChannelAttributeKey.GlobalFooterContent, bag.FooterContent );
            SetChannelAttr( channel, LinkListGuids.ChannelAttributeKey.GlobalFooterActive, bag.FooterActive.ToString() );
            SetChannelAttr( channel, LinkListGuids.ChannelAttributeKey.UseIvyJournalFont, bag.UseIvyJournalFont.ToString() );
            channel.SaveAttributeValues( _rockContext );

            // The resolved read above comes from ContentChannelCache; flush it so
            // every list reflects the new global settings immediately.
            ContentChannelCache.Clear();
        }

        private static void SetChannelAttr( ContentChannel channel, string key, string value )
        {
            if ( channel.Attributes != null && channel.Attributes.ContainsKey( key ) )
            {
                channel.SetAttributeValue( key, value ?? string.Empty );
            }
        }

        private static string SanitizeOrNull( string html )
        {
            return html.IsNullOrWhiteSpace() ? null : LinkListHtmlSanitizer.Sanitize( html );
        }

        // ---------------------------------------------------------------------
        // Security group + membership management
        // ---------------------------------------------------------------------

        private const int MaxAllowOrDenyOrder = 0;

        /// <summary>
        /// Returns the primary security group associated with this list. The
        /// "primary" group is identified by name prefix
        /// (<see cref="LinkListGuids.SecurityGroupNamePrefix"/>) - the
        /// auto-created list-specific group. If no prefixed group is found,
        /// falls back to the first group with an Allow/Edit AuthRule on the
        /// item. Returns null when the item has no group-based EDIT rule.
        /// </summary>
        public Group GetPrimarySecurityGroup( ContentChannelItem item )
        {
            if ( item == null )
            {
                return null;
            }

            var contentChannelItemEntityTypeId = EntityTypeCache.Get( typeof( ContentChannelItem ) )?.Id;
            if ( !contentChannelItemEntityTypeId.HasValue )
            {
                return null;
            }

            var groupQuery = new AuthService( _rockContext )
                .Queryable( "Group" )
                .Where( a =>
                    a.EntityTypeId == contentChannelItemEntityTypeId.Value
                    && a.EntityId == item.Id
                    && a.Action == Authorization.EDIT
                    && a.AllowOrDeny == "A"
                    && a.GroupId.HasValue
                    && a.Group != null )
                .OrderBy( a => a.Order )
                .Select( a => a.Group );

            var groups = groupQuery.ToList();
            if ( groups.Count == 0 )
            {
                return null;
            }

            var prefix = LinkListGuids.SecurityGroupNamePrefix;
            return groups.FirstOrDefault( g => g.Name != null && g.Name.StartsWith( prefix ) )
                ?? groups.First();
        }

        /// <summary>
        /// Returns a read-only summary of every <i>other</i> AuthRule granting
        /// EDIT on the item (groups other than the primary, plus individual
        /// persons and special roles). Used by the editor's "Also has edit
        /// access" panel.
        /// </summary>
        public List<AuthRuleBag> GetOtherEditAccess( ContentChannelItem item, Group primaryGroup )
        {
            var result = new List<AuthRuleBag>();
            if ( item == null )
            {
                return result;
            }

            var contentChannelItemEntityTypeId = EntityTypeCache.Get( typeof( ContentChannelItem ) )?.Id;
            if ( !contentChannelItemEntityTypeId.HasValue )
            {
                return result;
            }

            var rules = new AuthService( _rockContext )
                .Queryable( "Group,PersonAlias,PersonAlias.Person" )
                .Where( a =>
                    a.EntityTypeId == contentChannelItemEntityTypeId.Value
                    && a.EntityId == item.Id
                    && a.Action == Authorization.EDIT
                    && a.AllowOrDeny == "A" )
                .OrderBy( a => a.Order )
                .ToList();

            foreach ( var rule in rules )
            {
                if ( rule.GroupId.HasValue )
                {
                    if ( primaryGroup != null && rule.GroupId.Value == primaryGroup.Id )
                    {
                        continue; // already surfaced as the editable primary group
                    }
                    result.Add( new AuthRuleBag
                    {
                        Kind = "Group",
                        GroupName = rule.Group?.Name,
                        Order = rule.Order
                    } );
                }
                else if ( rule.PersonAliasId.HasValue )
                {
                    result.Add( new AuthRuleBag
                    {
                        Kind = "Person",
                        PersonName = rule.PersonAlias?.Person?.FullName,
                        Order = rule.Order
                    } );
                }
                else if ( rule.SpecialRole != SpecialRole.None )
                {
                    result.Add( new AuthRuleBag
                    {
                        Kind = "SpecialRole",
                        SpecialRoleLabel = rule.SpecialRole.ToString(),
                        Order = rule.Order
                    } );
                }
            }

            return result;
        }

        /// <summary>
        /// Builds the security-group name for a list title, clamped to the
        /// Group.Name column length (nvarchar(100)). Titles may be up to 250
        /// chars, so the title portion is truncated to fit after the prefix -
        /// otherwise SaveChanges throws "String or binary data would be
        /// truncated" (and on create that would block the creator from editing
        /// their own new list). Both create and rename go through this, so a
        /// previously-truncated name still compares equal and won't churn.
        /// </summary>
        public static string SecurityGroupName( string title )
        {
            const int maxGroupName = 100; // Group.Name column length
            var t = string.IsNullOrWhiteSpace( title ) ? "Untitled" : title.Trim();
            var room = Math.Max( 0, maxGroupName - LinkListGuids.SecurityGroupNamePrefix.Length );
            if ( t.Length > room )
            {
                t = t.Substring( 0, room );
            }
            return LinkListGuids.SecurityGroupNamePrefix + t;
        }

        /// <summary>
        /// Idempotently ensures the list has a primary security group:
        ///  - If <see cref="GetPrimarySecurityGroup"/> returns non-null, returns it unchanged.
        ///  - Otherwise, creates a SecurityRole group named
        ///    "{SecurityGroupNamePrefix}{title}", adds <paramref name="createdBy"/>
        ///    as an active member, grants EDIT on the item to that group via
        ///    an AuthRule (Order 0), and flushes the per-action authorization cache.
        /// </summary>
        public Group EnsureSecurityGroup( ContentChannelItem item, Person createdBy )
        {
            if ( item == null )
            {
                throw new ArgumentNullException( nameof( item ) );
            }

            var existing = GetPrimarySecurityGroup( item );
            if ( existing != null )
            {
                return existing;
            }

            var groupTypeId = GroupTypeCache.Get( LinkListGuids.GroupTypeSecurityRole.AsGuid() )?.Id;
            if ( !groupTypeId.HasValue )
            {
                throw new InvalidOperationException( "Default Rock SecurityRole group type is missing." );
            }

            var group = new Group
            {
                Name = SecurityGroupName( item.Title ),
                Description = "Auto-created by the SECC Link List plugin. Members of this group have edit access to the linked Link List.",
                GroupTypeId = groupTypeId.Value,
                IsSecurityRole = true,
                IsActive = true,
                IsSystem = false
            };

            new GroupService( _rockContext ).Add( group );
            _rockContext.SaveChanges();

            if ( createdBy != null )
            {
                var memberRoleId = GroupTypeCache.Get( groupTypeId.Value )?.DefaultGroupRoleId;
                if ( memberRoleId.HasValue )
                {
                    var member = new GroupMember
                    {
                        GroupId = group.Id,
                        PersonId = createdBy.Id,
                        GroupRoleId = memberRoleId.Value,
                        GroupMemberStatus = GroupMemberStatus.Active
                    };
                    new GroupMemberService( _rockContext ).Add( member );
                    _rockContext.SaveChanges();
                }
            }

            // Grant EDIT to this group on the item. Inserts a new Auth row at
            // Order 0 and shifts subsequent rules down by one. No manual cache
            // flush needed: AllowSecurityRole ends with RefreshAction for this
            // entity/action, and the GroupMember insert above already flushed
            // the role cache (GroupMember.UpdateCache runs inside SaveChanges
            // for security-role groups).
            Authorization.AllowSecurityRole( item, Authorization.EDIT, group, _rockContext );

            return group;
        }

        /// <summary>
        /// Keeps the primary security group's name aligned with the list title:
        /// renames it to "{SecurityGroupNamePrefix}{title}" when the title has
        /// changed. No-op when the list has no primary group or the name already
        /// matches. Callers should treat failures as non-fatal (the list itself
        /// is already saved).
        /// </summary>
        public void RenameSecurityGroup( ContentChannelItem item, string title )
        {
            if ( item == null )
            {
                return;
            }

            var group = GetPrimarySecurityGroup( item );
            if ( group == null )
            {
                return;
            }

            var expected = SecurityGroupName( title );

            if ( !string.Equals( group.Name, expected, StringComparison.Ordinal ) )
            {
                group.Name = expected;
                _rockContext.SaveChanges();
            }
        }

        /// <summary>
        /// Projects active members of the supplied group as <see cref="GroupMemberBag"/>.
        /// </summary>
        public List<GroupMemberBag> GetMembers( Group group, Person currentPerson )
        {
            if ( group == null )
            {
                return new List<GroupMemberBag>();
            }

            var members = new GroupMemberService( _rockContext )
                .Queryable( "Person" )
                .Where( m => m.GroupId == group.Id && m.GroupMemberStatus == GroupMemberStatus.Active )
                .OrderBy( m => m.Person.LastName )
                .ThenBy( m => m.Person.FirstName )
                .ToList();

            return members
                .Where( m => m.Person != null )
                .Select( m => new GroupMemberBag
                {
                    GroupMemberId = m.Id,
                    PersonGuid = m.Person.Guid,
                    FullName = m.Person.FullName,
                    Email = m.Person.Email,
                    PhotoUrl = m.Person.PhotoUrl,
                    IsCurrentUser = currentPerson != null && m.PersonId == currentPerson.Id
                } )
                .ToList();
        }

        /// <summary>
        /// Adds the person identified by <paramref name="personGuid"/> as an
        /// active member of the list's primary security group, lazily creating
        /// the group if needed (legacy lists with no AuthRule).
        /// Caller MUST verify the current user has EDIT on the item before calling.
        /// Returns the new (or existing) member bag, or null if the person was not found.
        /// </summary>
        public GroupMemberBag AddMember( ContentChannelItem item, Guid personGuid, Person currentPerson )
        {
            if ( item == null ) throw new ArgumentNullException( nameof( item ) );

            var group = EnsureSecurityGroup( item, currentPerson );
            var person = new PersonService( _rockContext ).Get( personGuid );
            if ( person == null )
            {
                return null;
            }

            var memberService = new GroupMemberService( _rockContext );
            var existing = memberService
                .Queryable()
                .FirstOrDefault( m => m.GroupId == group.Id && m.PersonId == person.Id );

            if ( existing == null )
            {
                var roleId = GroupTypeCache.Get( group.GroupTypeId )?.DefaultGroupRoleId;
                if ( !roleId.HasValue )
                {
                    throw new InvalidOperationException( "Security role group type has no default role." );
                }
                existing = new GroupMember
                {
                    GroupId = group.Id,
                    PersonId = person.Id,
                    GroupRoleId = roleId.Value,
                    GroupMemberStatus = GroupMemberStatus.Active
                };
                memberService.Add( existing );
                _rockContext.SaveChanges();
            }
            else if ( existing.GroupMemberStatus != GroupMemberStatus.Active )
            {
                existing.GroupMemberStatus = GroupMemberStatus.Active;
                _rockContext.SaveChanges();
            }

            // No manual cache flush: saving a security-role GroupMember triggers
            // GroupMember.UpdateCache (RoleCache.FlushItem + Authorization.Clear)
            // inside SaveChanges, so the new member's EDIT access is already live.
            return new GroupMemberBag
            {
                GroupMemberId = existing.Id,
                PersonGuid = person.Guid,
                FullName = person.FullName,
                Email = person.Email,
                PhotoUrl = person.PhotoUrl,
                IsCurrentUser = currentPerson != null && person.Id == currentPerson.Id
            };
        }

        /// <summary>
        /// Removes a member from the list's primary security group. Refuses
        /// to remove the last remaining active member (so the list never
        /// becomes orphaned). Caller MUST verify EDIT on the item.
        /// </summary>
        /// <param name="errorMessage">Out: human-readable reason on failure.</param>
        /// <returns>True on success, false on validation failure.</returns>
        public bool RemoveMember( ContentChannelItem item, int groupMemberId, out string errorMessage )
        {
            errorMessage = null;
            if ( item == null ) throw new ArgumentNullException( nameof( item ) );

            var group = GetPrimarySecurityGroup( item );
            if ( group == null )
            {
                errorMessage = "This list has no linked security group.";
                return false;
            }

            var memberService = new GroupMemberService( _rockContext );
            var member = memberService.Get( groupMemberId );
            if ( member == null || member.GroupId != group.Id )
            {
                errorMessage = "Member not found.";
                return false;
            }

            var activeCount = memberService
                .Queryable()
                .Count( m => m.GroupId == group.Id && m.GroupMemberStatus == GroupMemberStatus.Active );

            if ( activeCount <= 1 )
            {
                errorMessage = "Cannot remove the last member. Add another person first to keep edit access on this list.";
                return false;
            }

            // Deleting a security-role GroupMember flushes the role/auth caches
            // via GroupMember.UpdateCache inside SaveChanges - no manual flush.
            memberService.Delete( member );
            _rockContext.SaveChanges();

            return true;
        }
    }
}
